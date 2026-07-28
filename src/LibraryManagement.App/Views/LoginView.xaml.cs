using System.ComponentModel;
using System.Windows;
using LibraryManagement.App.ViewModels;
using Wpf.Ui.Controls;

namespace LibraryManagement.App.Views;

public partial class LoginView : FluentWindow
{
    private readonly LoginViewModel _viewModel;

    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        Loaded += OnLoaded;
        Closed += OnClosed;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeCommand.ExecuteAsync(null);

        if (string.IsNullOrWhiteSpace(_viewModel.Username))
        {
            UsernameTextBox.Focus();
        }
        else
        {
            PasswordInput.Focus();
        }
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.PasswordBox passwordBox)
        {
            _viewModel.Password = passwordBox.Password;
        }
    }

    private void OnViewModelPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.Password)
            && string.IsNullOrEmpty(_viewModel.Password)
            && PasswordInput.Password.Length > 0)
        {
            PasswordInput.Clear();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Loaded -= OnLoaded;
        Closed -= OnClosed;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        PasswordInput.Clear();
    }
}
