using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentAssertions;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Messages;
using LibraryManagement.App.Navigation;
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
    public async Task ShellCommands_ShouldTrackNavigationRefreshAndLogoutState()
    {
        var authenticationService = new StubAuthenticationService();
        var navigationService = new StubNavigationService();
        var messenger = new WeakReferenceMessenger();
        var viewModel = new MainViewModel(
            new CurrentUserService(),
            authenticationService,
            new ConfirmingDialogService(),
            navigationService,
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

        viewModel.NavigateBackCommand.CanExecute(null).Should().BeFalse();
        viewModel.NavigateForwardCommand.CanExecute(null).Should().BeFalse();

        navigationService.CanGoBack = true;
        navigationService.CanGoForward = true;
        navigationService.RaiseNavigationStateChanged();

        viewModel.NavigateBackCommand.CanExecute(null).Should().BeTrue();
        viewModel.NavigateForwardCommand.CanExecute(null).Should().BeTrue();
        viewModel.NavigateBackCommand.Execute(null);
        viewModel.NavigateForwardCommand.Execute(null);
        navigationService.GoBackCallCount.Should().Be(1);
        navigationService.GoForwardCallCount.Should().Be(1);

        var refreshTarget = new StubRefreshableViewModel(canExecute: true);
        viewModel.SetActiveRefreshTarget(refreshTarget);
        viewModel.IsRefreshAvailable.Should().BeTrue();
        viewModel.ActiveRefreshTarget.Should().BeSameAs(refreshTarget);
        await refreshTarget.RefreshCommand.ExecuteAsync(null);
        refreshTarget.RefreshCallCount.Should().Be(1);

        var disabledRefreshTarget = new StubRefreshableViewModel(canExecute: false);
        disabledRefreshTarget.RefreshCommand.CanExecute(null).Should().BeFalse();
        viewModel.SetActiveRefreshTarget(disabledRefreshTarget);
        viewModel.ClearActiveRefreshTarget(refreshTarget);
        viewModel.ActiveRefreshTarget.Should().BeSameAs(disabledRefreshTarget);
        viewModel.ClearActiveRefreshTarget(disabledRefreshTarget);
        viewModel.IsRefreshAvailable.Should().BeFalse();

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

    private sealed class StubNavigationService : IAppNavigationService
    {
        public event EventHandler? NavigationStateChanged;

        public bool CanGoBack { get; set; }

        public bool CanGoForward { get; set; }

        public int GoBackCallCount { get; private set; }

        public int GoForwardCallCount { get; private set; }

        public void Initialize(Wpf.Ui.Controls.NavigationView navigationView)
        {
        }

        public bool Navigate<TPage>(object? dataContext = null)
            where TPage : System.Windows.FrameworkElement
        {
            return false;
        }

        public bool GoBack()
        {
            GoBackCallCount++;
            return true;
        }

        public bool GoForward()
        {
            GoForwardCallCount++;
            return true;
        }

        public void RaiseNavigationStateChanged()
        {
            NavigationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class StubRefreshableViewModel : IRefreshableViewModel
    {
        public StubRefreshableViewModel(bool canExecute)
        {
            RefreshCommand = new AsyncRelayCommand(
                () =>
                {
                    RefreshCallCount++;
                    return Task.CompletedTask;
                },
                () => canExecute);
        }

        public IAsyncRelayCommand RefreshCommand { get; }

        public bool IsBusy => false;

        public int RefreshCallCount { get; private set; }
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
