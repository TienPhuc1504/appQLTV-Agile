using LibraryManagement.App.Navigation;
using LibraryManagement.App.ViewModels;
using LibraryManagement.App.Views.Pages;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Views;

public partial class MainWindow : FluentWindow
{
    private readonly IAppNavigationService _navigationService;
    private readonly MainViewModel _viewModel;
    private IRefreshableViewModel? _activeRefreshTarget;

    public MainWindow(
        MainViewModel viewModel,
        IServiceProvider serviceProvider,
        IAppNavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        _navigationService = navigationService;

        RootNavigation.SetServiceProvider(serviceProvider);
        _navigationService.Initialize(RootNavigation);
        snackbarService.SetSnackbarPresenter(RootSnackbarPresenter);
        contentDialogService.SetDialogHost(RootContentDialogHost);

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        _navigationService.Navigate<DashboardPage>();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_activeRefreshTarget is not null)
        {
            _viewModel.ClearActiveRefreshTarget(_activeRefreshTarget);
            _activeRefreshTarget = null;
        }

        Loaded -= OnLoaded;
        Closed -= OnClosed;
    }

    private void OnNavigated(
        NavigationView sender,
        NavigatedEventArgs args)
    {
        IRefreshableViewModel? refreshTarget =
            (args.Page as FrameworkElement)?.DataContext
            as IRefreshableViewModel;
        _activeRefreshTarget = refreshTarget;
        _viewModel.SetActiveRefreshTarget(refreshTarget);
    }
}
