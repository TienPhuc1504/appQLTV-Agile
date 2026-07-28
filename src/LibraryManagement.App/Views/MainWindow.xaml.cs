using LibraryManagement.App.Navigation;
using LibraryManagement.App.ViewModels;
using LibraryManagement.App.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Views;

public partial class MainWindow : FluentWindow
{
    private readonly IAppNavigationService _navigationService;

    public MainWindow(
        MainViewModel viewModel,
        IServiceProvider serviceProvider,
        IAppNavigationService navigationService,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService)
    {
        InitializeComponent();
        DataContext = viewModel;
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
        _navigationService.Navigate<FoundationPage>();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        Closed -= OnClosed;
    }
}
