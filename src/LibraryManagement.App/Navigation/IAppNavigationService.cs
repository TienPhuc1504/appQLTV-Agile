using System.Windows;

namespace LibraryManagement.App.Navigation;

public interface IAppNavigationService
{
    event EventHandler? NavigationStateChanged;

    bool CanGoBack { get; }

    bool CanGoForward { get; }

    void Initialize(Wpf.Ui.Controls.NavigationView navigationView);

    bool Navigate<TPage>(object? dataContext = null)
        where TPage : FrameworkElement;

    bool GoBack();

    bool GoForward();
}
