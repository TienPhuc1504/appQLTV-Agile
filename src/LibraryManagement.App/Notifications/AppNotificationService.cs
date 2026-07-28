using Wpf.Ui;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Notifications;

public sealed class AppNotificationService(ISnackbarService snackbarService)
    : IAppNotificationService
{
    public void Show(
        string title,
        string message,
        NotificationSeverity severity = NotificationSeverity.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        snackbarService.Show(
            title,
            message,
            GetAppearance(severity),
            GetIcon(severity),
            TimeSpan.FromSeconds(3));
    }

    private static ControlAppearance GetAppearance(NotificationSeverity severity)
    {
        return severity switch
        {
            NotificationSeverity.Success => ControlAppearance.Success,
            NotificationSeverity.Warning => ControlAppearance.Caution,
            NotificationSeverity.Error => ControlAppearance.Danger,
            _ => ControlAppearance.Info
        };
    }

    private static IconElement GetIcon(NotificationSeverity severity)
    {
        SymbolRegular symbol = severity switch
        {
            NotificationSeverity.Success => SymbolRegular.CheckmarkCircle24,
            NotificationSeverity.Warning => SymbolRegular.Warning24,
            NotificationSeverity.Error => SymbolRegular.ErrorCircle24,
            _ => SymbolRegular.Info24
        };

        return new SymbolIcon(symbol);
    }
}
