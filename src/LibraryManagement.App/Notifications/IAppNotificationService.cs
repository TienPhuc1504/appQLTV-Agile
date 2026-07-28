namespace LibraryManagement.App.Notifications;

public interface IAppNotificationService
{
    void Show(
        string title,
        string message,
        NotificationSeverity severity = NotificationSeverity.Information);
}
