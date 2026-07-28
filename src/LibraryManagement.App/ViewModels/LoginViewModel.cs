using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibraryManagement.App.Messages;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILoginPreferenceService _loginPreferenceService;
    private readonly IMessenger _messenger;
    private readonly ILogger<LoginViewModel> _logger;

    public LoginViewModel(
        IAuthenticationService authenticationService,
        ILoginPreferenceService loginPreferenceService,
        IMessenger messenger,
        ILogger<LoginViewModel> logger)
    {
        _authenticationService = authenticationService;
        _loginPreferenceService = loginPreferenceService;
        _messenger = messenger;
        _logger = logger;
        ErrorsChanged += OnErrorsChanged;
    }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    [RegularExpression(
        @"^[\p{L}\p{N}._-]+$",
        ErrorMessage = "Tên đăng nhập chứa ký tự không hợp lệ.")]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool RememberUsername { get; set; }

    public string? UsernameValidationMessage =>
        GetFirstValidationMessage(nameof(Username));

    public string? PasswordValidationMessage =>
        GetFirstValidationMessage(nameof(Password));

    [RelayCommand]
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string? rememberedUsername =
            await _loginPreferenceService.GetRememberedUsernameAsync(
                cancellationToken);

        if (string.IsNullOrWhiteSpace(rememberedUsername))
        {
            return;
        }

        Username = rememberedUsername;
        RememberUsername = true;
    }

    [RelayCommand]
    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        AuthenticationSucceededMessage? succeededMessage = null;

        await ExecuteBusyAsync(
            async token =>
            {
                Username = Username.Trim();

                if (!Validate())
                {
                    ErrorMessage = "Vui lòng kiểm tra lại thông tin đăng nhập.";
                    return;
                }

                AuthenticationResult result =
                    await _authenticationService.LoginAsync(
                        Username,
                        Password,
                        token);

                if (!result.Succeeded || result.User is null)
                {
                    ErrorMessage = result.ErrorMessage
                        ?? "Không thể đăng nhập. Vui lòng thử lại.";
                    return;
                }

                await _loginPreferenceService.SaveRememberedUsernameAsync(
                    RememberUsername ? Username : null,
                    token);

                Password = string.Empty;
                succeededMessage =
                    new AuthenticationSucceededMessage(result.User);
            },
            "Đang đăng nhập...",
            cancellationToken);

        if (succeededMessage is not null)
        {
            _messenger.Send(succeededMessage);
        }
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Đã xảy ra lỗi trong quá trình đăng nhập.");
        return "Không thể đăng nhập lúc này. Vui lòng thử lại.";
    }

    private string? GetFirstValidationMessage(string propertyName)
    {
        return GetErrors(propertyName)
            .OfType<ValidationResult>()
            .Select(result => result.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message));
    }

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Username))
        {
            OnPropertyChanged(nameof(UsernameValidationMessage));
        }
        else if (e.PropertyName == nameof(Password))
        {
            OnPropertyChanged(nameof(PasswordValidationMessage));
        }
    }
}
