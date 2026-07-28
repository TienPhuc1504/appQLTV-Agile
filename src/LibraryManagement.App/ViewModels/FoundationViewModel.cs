using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;

namespace LibraryManagement.App.ViewModels;

public sealed partial class FoundationViewModel : BaseViewModel
{
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;

    public FoundationViewModel(
        IAppDialogService dialogService,
        IAppNotificationService notificationService)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập nội dung kiểm tra.")]
    [MinLength(3, ErrorMessage = "Nội dung kiểm tra phải có ít nhất 3 ký tự.")]
    public partial string VerificationText { get; set; } = "Nền tảng MVVM";

    [RelayCommand]
    private async Task VerifyAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại dữ liệu.";
            return;
        }

        await ExecuteBusyAsync(
            async token =>
            {
                await Task.Delay(350, token);
                _notificationService.Show(
                    "Kiểm tra thành công",
                    "Loading, validation và snackbar đang hoạt động.",
                    NotificationSeverity.Success);
            },
            "Đang kiểm tra nền tảng...",
            cancellationToken);
    }

    [RelayCommand]
    private Task ShowDialogAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            token => _dialogService.ShowMessageAsync(
                "Nền tảng ứng dụng",
                "ContentDialog đã được kết nối qua Dependency Injection.",
                token),
            "Đang mở hộp thoại...",
            cancellationToken);
    }
}
