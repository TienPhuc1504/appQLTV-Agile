using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.App.Views.Pages;

public partial class ReaderPage : Page
{
    private readonly ReaderViewModel _viewModel;

    public ReaderPage(ReaderViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnReaderGridSorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (!Enum.TryParse(
                e.Column.SortMemberPath,
                ignoreCase: false,
                out ReaderSortField sortField))
        {
            return;
        }

        e.Handled = true;
        await _viewModel.ApplySortAsync(sortField);
        if (sender is DataGrid dataGrid)
        {
            foreach (DataGridColumn column in dataGrid.Columns)
            {
                if (!ReferenceEquals(column, e.Column))
                {
                    column.SortDirection = null;
                }
            }
        }

        e.Column.SortDirection = _viewModel.SortDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;
    }
}
