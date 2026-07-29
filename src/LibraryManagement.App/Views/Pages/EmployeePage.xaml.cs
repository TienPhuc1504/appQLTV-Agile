using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LibraryManagement.App.ViewModels;

namespace LibraryManagement.App.Views.Pages;

public partial class EmployeePage : Page
{
    private readonly EmployeeViewModel _viewModel;
    private bool _isSubscribed;

    public EmployeePage(EmployeeViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isSubscribed)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _isSubscribed = true;
        }

        await _viewModel.LoadCommand.ExecuteAsync(null);
    }

    private async void OnSelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        await _viewModel.LoadSelectedCommand.ExecuteAsync(null);
        PasswordInput.Clear();
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox)
        {
            _viewModel.NewPassword = passwordBox.Password;
        }
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EmployeeViewModel.NewPassword)
            && string.IsNullOrEmpty(_viewModel.NewPassword)
            && PasswordInput.Password.Length > 0)
        {
            PasswordInput.Clear();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_isSubscribed)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _isSubscribed = false;
        }

        PasswordInput.Clear();
    }
}
