using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class ReturnPage : Page
{
    public ReturnPage(ReturnViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
