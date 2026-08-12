using System.Windows;
using LibraryManagement.App.Controls;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Navigation;

public sealed class AppNavigationService : IAppNavigationService
{
    private NavigationView? _navigationView;

    public event EventHandler? NavigationStateChanged;

    public bool CanGoBack => _navigationView?.CanGoBack == true;

    public bool CanGoForward =>
        _navigationView is LibraryNavigationView { CanGoForward: true };

    public void Initialize(NavigationView navigationView)
    {
        ArgumentNullException.ThrowIfNull(navigationView);

        if (_navigationView is not null)
        {
            _navigationView.Navigated -= OnNavigated;
        }

        _navigationView = navigationView;
        _navigationView.Navigated += OnNavigated;
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
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

    public bool GoForward()
    {
        return GetNavigationView().GoForward();
    }

    private void OnNavigated(
        NavigationView sender,
        NavigatedEventArgs args)
    {
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private NavigationView GetNavigationView()
    {
        return _navigationView
            ?? throw new InvalidOperationException(
                "Dịch vụ điều hướng chưa được liên kết với NavigationView.");
    }
}
