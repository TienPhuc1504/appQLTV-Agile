using Wpf.Ui.Appearance;

namespace LibraryManagement.App.Themes;

public sealed class AppThemeService : IAppThemeService
{
    public AppTheme CurrentTheme =>
        ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? AppTheme.Dark
            : AppTheme.Light;

    public event EventHandler? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        ApplicationTheme targetTheme = theme == AppTheme.Dark
            ? ApplicationTheme.Dark
            : ApplicationTheme.Light;

        if (ApplicationThemeManager.GetAppTheme() == targetTheme)
        {
            return;
        }

        ApplicationThemeManager.Apply(targetTheme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public AppTheme Toggle()
    {
        AppTheme nextTheme = CurrentTheme == AppTheme.Dark
            ? AppTheme.Light
            : AppTheme.Dark;
        Apply(nextTheme);
        return nextTheme;
    }
}
