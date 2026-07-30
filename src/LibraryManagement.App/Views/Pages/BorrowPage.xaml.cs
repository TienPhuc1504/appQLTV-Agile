using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.Views.Pages;

public partial class BorrowPage : Page
{
    private readonly BorrowViewModel _viewModel;
    private readonly ILogger<BorrowPage> _logger;

    public BorrowPage(
        BorrowViewModel viewModel,
        ILogger<BorrowPage> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        DataContext = viewModel;
        _logger.LogDebug(
            "BorrowPage được tạo với BorrowViewModel InstanceId={InstanceId}.",
            viewModel.InstanceId);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _logger.LogDebug(
            "BorrowPage Loaded. InstanceId={InstanceId}, "
            + "DataContextMatches={DataContextMatches}.",
            _viewModel.InstanceId,
            ReferenceEquals(DataContext, _viewModel));
        await _viewModel.LoadCommand.ExecuteAsync(null);
    }
}
