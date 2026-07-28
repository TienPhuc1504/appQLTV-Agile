using FluentAssertions;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.Tests.ViewModels;

public sealed class FoundationViewModelTests
{
    [Fact]
    public async Task VerifyCommand_ShouldNotNotifyWhenValidationFails()
    {
        var dialogService = new FakeDialogService();
        var notificationService = new FakeNotificationService();
        var viewModel = new FoundationViewModel(dialogService, notificationService)
        {
            VerificationText = string.Empty
        };

        await viewModel.VerifyCommand.ExecuteAsync(null);

        viewModel.HasErrors.Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("Vui lòng kiểm tra lại dữ liệu.");
        notificationService.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyCommand_ShouldNotifyAfterSuccessfulValidation()
    {
        var dialogService = new FakeDialogService();
        var notificationService = new FakeNotificationService();
        var viewModel = new FoundationViewModel(dialogService, notificationService)
        {
            VerificationText = "MVVM"
        };

        await viewModel.VerifyCommand.ExecuteAsync(null);

        viewModel.HasErrors.Should().BeFalse();
        viewModel.ErrorMessage.Should().BeNull();
        notificationService.CallCount.Should().Be(1);
        notificationService.LastSeverity.Should().Be(NotificationSeverity.Success);
    }

    private sealed class FakeDialogService : IAppDialogService
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

    private sealed class FakeNotificationService : IAppNotificationService
    {
        public int CallCount { get; private set; }

        public NotificationSeverity LastSeverity { get; private set; }

        public void Show(
            string title,
            string message,
            NotificationSeverity severity = NotificationSeverity.Information)
        {
            CallCount++;
            LastSeverity = severity;
        }
    }
}
