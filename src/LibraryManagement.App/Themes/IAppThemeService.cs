namespace LibraryManagement.App.Themes;

public interface IAppThemeService
{
    AppTheme CurrentTheme { get; }

    event EventHandler? ThemeChanged;

    void Apply(AppTheme theme);

    AppTheme Toggle();
}
