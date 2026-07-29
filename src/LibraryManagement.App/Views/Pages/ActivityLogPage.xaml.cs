using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class ActivityLogPage : Page
{
    private readonly ActivityLogViewModel _viewModel;

    public ActivityLogPage(ActivityLogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
