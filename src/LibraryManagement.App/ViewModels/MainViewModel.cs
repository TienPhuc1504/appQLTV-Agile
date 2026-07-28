using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Messages;
using LibraryManagement.App.Themes;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class MainViewModel : BaseViewModel, IDisposable
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IAppDialogService _dialogService;
    private readonly IMessenger _messenger;
    private readonly IAppThemeService _themeService;
    private readonly ILogger<MainViewModel> _logger;
    private bool _disposed;

    public MainViewModel(
        ICurrentUserService currentUserService,
        IAuthenticationService authenticationService,
        IAppDialogService dialogService,
        IMessenger messenger,
        IAppThemeService themeService,
        ILogger<MainViewModel> logger)
    {
        _currentUserService = currentUserService;
        _authenticationService = authenticationService;
        _dialogService = dialogService;
        _messenger = messenger;
        _themeService = themeService;
        _logger = logger;
        _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    public string ApplicationName => "LibraryManagement";

    public string CurrentEmployeeName =>
        _currentUserService.CurrentUser?.FullName ?? "Chưa đăng nhập";

    public string CurrentEmployeeRole =>
        _currentUserService.CurrentUser?.RoleName ?? "Phiên làm việc chưa xác thực";

    public string ThemeButtonText =>
        _themeService.CurrentTheme == AppTheme.Dark
            ? "Chuyển sang giao diện sáng"
            : "Chuyển sang giao diện tối";

    public bool CanManageSystemSettings =>
        _authenticationService.CheckPermission(Permission.ManageSystemSettings);

    [RelayCommand]
    private void ToggleTheme()
    {
        _themeService.Toggle();
    }

    [RelayCommand]
    private async Task LogoutAsync(CancellationToken cancellationToken)
    {
        bool logoutCompleted = false;

        await ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Đăng xuất",
                    "Bạn có chắc chắn muốn đăng xuất khỏi ứng dụng?",
                    "Đăng xuất",
                    "Ở lại",
                    token);

                if (!confirmed)
                {
                    return;
                }

                _authenticationService.Logout();
                logoutCompleted = true;
            },
            "Đang đăng xuất...",
            cancellationToken);

        if (logoutCompleted)
        {
            _messenger.Send(new LogoutCompletedMessage());
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _currentUserService.CurrentUserChanged -= OnCurrentUserChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        _disposed = true;
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentEmployeeName));
        OnPropertyChanged(nameof(CurrentEmployeeRole));
        OnPropertyChanged(nameof(CanManageSystemSettings));
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể hoàn tất thao tác trên cửa sổ chính.");
        return "Không thể hoàn tất thao tác. Vui lòng thử lại.";
    }
}
