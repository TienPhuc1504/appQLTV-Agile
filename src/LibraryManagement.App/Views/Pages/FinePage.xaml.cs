using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class FinePage : Page
{
    private readonly FineViewModel _viewModel;

    public FinePage(FineViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnFineSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        await _viewModel.LoadSelectedFineCommand.ExecuteAsync(null);
    }
}
