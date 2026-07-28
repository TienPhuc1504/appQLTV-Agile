using System.Windows;

namespace LibraryManagement.App.Navigation;

public interface IAppNavigationService
{
    void Initialize(Wpf.Ui.Controls.NavigationView navigationView);

    bool Navigate<TPage>(object? dataContext = null)
        where TPage : FrameworkElement;

    bool GoBack();
}
