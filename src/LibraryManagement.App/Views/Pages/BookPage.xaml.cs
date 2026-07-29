using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class BookPage : Page
{
    private readonly BookViewModel _viewModel;

    public BookPage(BookViewModel viewModel)
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
