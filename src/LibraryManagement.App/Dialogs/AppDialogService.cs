using Wpf.Ui;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Dialogs;

public sealed class AppDialogService(IContentDialogService contentDialogService)
    : IAppDialogService
{
    public async Task ShowMessageAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default)
    {
        ValidateText(title, nameof(title));
        ValidateText(message, nameof(message));

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Đóng"
        };

        await contentDialogService.ShowAsync(dialog, cancellationToken);
    }

    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Xác nhận",
        string cancelText = "Hủy",
        CancellationToken cancellationToken = default)
    {
        ValidateText(title, nameof(title));
        ValidateText(message, nameof(message));
        ValidateText(confirmText, nameof(confirmText));
        ValidateText(cancelText, nameof(cancelText));

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText
        };

        ContentDialogResult result =
            await contentDialogService.ShowAsync(dialog, cancellationToken);
        return result == ContentDialogResult.Primary;
    }

    private static void ValidateText(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
    }
}
