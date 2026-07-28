using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Messages;
using LibraryManagement.App.Themes;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryManagement.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task LogoutCommand_WhenConfirmed_ShouldSendMessageAfterBusyStateEnds()
    {
        var authenticationService = new StubAuthenticationService();
        var messenger = new WeakReferenceMessenger();
        var viewModel = new MainViewModel(
            new CurrentUserService(),
            authenticationService,
            new ConfirmingDialogService(),
            messenger,
            new StubThemeService(),
            NullLogger<MainViewModel>.Instance);
        var recipient = new object();
        bool messageReceived = false;
        bool? wasBusyWhenMessageWasReceived = null;
        messenger.Register<LogoutCompletedMessage>(
            recipient,
            (_, _) =>
            {
                messageReceived = true;
                wasBusyWhenMessageWasReceived = viewModel.IsBusy;
            });

        await viewModel.LogoutCommand.ExecuteAsync(null);

        authenticationService.LogoutCallCount.Should().Be(1);
        messageReceived.Should().BeTrue();
        wasBusyWhenMessageWasReceived.Should().BeFalse();
    }

    private sealed class ConfirmingDialogService : IAppDialogService
    {
        public Task ShowMessageAsync(
            string title,
            string message,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string confirmText = "Xác nhận",
            string cancelText = "Hủy",
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubThemeService : IAppThemeService
    {
        public AppTheme CurrentTheme => AppTheme.Light;

        public event EventHandler? ThemeChanged
        {
            add { }
            remove { }
        }

        public void Apply(AppTheme theme)
        {
        }

        public AppTheme Toggle() => AppTheme.Dark;
    }

    private sealed class StubAuthenticationService : IAuthenticationService
    {
        public int LogoutCallCount { get; private set; }

        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure("Không sử dụng."));
        }

        public void Logout()
        {
            LogoutCallCount++;
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

        public CurrentUser? GetCurrentUser() => null;

        public bool CheckPermission(Permission permission) => false;
    }
}
