using System.Windows;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Navigation;

public sealed class AppNavigationService : IAppNavigationService
{
    private NavigationView? _navigationView;

    public void Initialize(NavigationView navigationView)
    {
        ArgumentNullException.ThrowIfNull(navigationView);
        _navigationView = navigationView;
    }

    public bool Navigate<TPage>(object? dataContext = null)
        where TPage : FrameworkElement
    {
        NavigationView navigationView = GetNavigationView();
        return navigationView.Navigate(typeof(TPage), dataContext);
    }

    public bool GoBack()
    {
        return GetNavigationView().GoBack();
    }

    private NavigationView GetNavigationView()
    {
        return _navigationView
            ?? throw new InvalidOperationException(
                "Dịch vụ điều hướng chưa được liên kết với NavigationView.");
    }
}
