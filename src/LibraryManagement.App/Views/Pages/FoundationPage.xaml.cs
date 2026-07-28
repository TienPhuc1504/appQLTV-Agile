using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class FoundationPage : Page
{
    public FoundationPage(FoundationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
