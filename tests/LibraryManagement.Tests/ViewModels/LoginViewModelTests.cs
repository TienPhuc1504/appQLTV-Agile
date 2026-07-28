using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using LibraryManagement.App.Messages;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryManagement.Tests.ViewModels;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_WithValidCredentials_ShouldRememberUsernameAndSendMessage()
    {
        var user = new CurrentUser(
            1,
            "NV0001",
            "Quản trị hệ thống",
            "admin",
            "Administrator");
        var authenticationService = new StubAuthenticationService(
            AuthenticationResult.Success(user));
        var preferenceService = new StubLoginPreferenceService();
        var messenger = new WeakReferenceMessenger();
        var viewModel = new LoginViewModel(
            authenticationService,
            preferenceService,
            messenger,
            NullLogger<LoginViewModel>.Instance)
        {
            Username = " admin ",
            Password = "Admin@123",
            RememberUsername = true
        };
        var recipient = new object();
        AuthenticationSucceededMessage? receivedMessage = null;
        bool? wasBusyWhenMessageWasReceived = null;
        messenger.Register<AuthenticationSucceededMessage>(
            recipient,
            (_, message) =>
            {
                receivedMessage = message;
                wasBusyWhenMessageWasReceived = viewModel.IsBusy;
            });

        await viewModel.LoginCommand.ExecuteAsync(null);

        authenticationService.LoginCallCount.Should().Be(1);
        authenticationService.LastUsername.Should().Be("admin");
        preferenceService.SavedUsername.Should().Be("admin");
        viewModel.Password.Should().BeEmpty();
        receivedMessage.Should().NotBeNull();
        receivedMessage!.User.Should().Be(user);
        wasBusyWhenMessageWasReceived.Should().BeFalse();
    }

    [Fact]
    public async Task LoginCommand_WithMissingFields_ShouldNotCallService()
    {
        var authenticationService = new StubAuthenticationService(
            AuthenticationResult.Failure("Không hợp lệ."));
        var viewModel = new LoginViewModel(
            authenticationService,
            new StubLoginPreferenceService(),
            new WeakReferenceMessenger(),
            NullLogger<LoginViewModel>.Instance);

        await viewModel.LoginCommand.ExecuteAsync(null);

        authenticationService.LoginCallCount.Should().Be(0);
        viewModel.HasErrors.Should().BeTrue();
        viewModel.ErrorMessage.Should().Contain("kiểm tra lại");
        viewModel.UsernameValidationMessage.Should().Be("Vui lòng nhập tên đăng nhập.");
        viewModel.PasswordValidationMessage.Should().Be("Vui lòng nhập mật khẩu.");
    }

    [Fact]
    public async Task InitializeCommand_WithRememberedUsername_ShouldPopulateForm()
    {
        var preferenceService = new StubLoginPreferenceService
        {
            UsernameToLoad = "librarian1"
        };
        var viewModel = new LoginViewModel(
            new StubAuthenticationService(
                AuthenticationResult.Failure("Không sử dụng.")),
            preferenceService,
            new WeakReferenceMessenger(),
            NullLogger<LoginViewModel>.Instance);

        await viewModel.InitializeCommand.ExecuteAsync(null);

        viewModel.Username.Should().Be("librarian1");
        viewModel.RememberUsername.Should().BeTrue();
    }

    private sealed class StubLoginPreferenceService : ILoginPreferenceService
    {
        public string? UsernameToLoad { get; init; }

        public string? SavedUsername { get; private set; }

        public Task<string?> GetRememberedUsernameAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UsernameToLoad);
        }

        public Task SaveRememberedUsernameAsync(
            string? username,
            CancellationToken cancellationToken = default)
        {
            SavedUsername = username;
            return Task.CompletedTask;
        }
    }

    private sealed class StubAuthenticationService(AuthenticationResult loginResult)
        : IAuthenticationService
    {
        public int LoginCallCount { get; private set; }

        public string? LastUsername { get; private set; }

        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            LoginCallCount++;
            LastUsername = username;
            return Task.FromResult(loginResult);
        }

        public void Logout()
        {
        }

        public Task<OperationResult> ChangePasswordAsync(
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> ResetPasswordAsync(
            int employeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser() => loginResult.User;

        public bool CheckPermission(Permission permission) => false;
    }
}
