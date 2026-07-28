namespace LibraryManagement.App.Dialogs;

public interface IAppDialogService
{
    Task ShowMessageAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Xác nhận",
        string cancelText = "Hủy",
        CancellationToken cancellationToken = default);
}
