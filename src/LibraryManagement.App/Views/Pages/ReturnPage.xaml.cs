using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class ReturnPage : Page
{
    private readonly ReturnViewModel _viewModel;

    public ReturnPage(ReturnViewModel viewModel)
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
