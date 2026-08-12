using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Messages;
using LibraryManagement.App.Navigation;
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
    private readonly IAppNavigationService _navigationService;
    private readonly IMessenger _messenger;
    private readonly IAppThemeService _themeService;
    private readonly ILogger<MainViewModel> _logger;
    private IRefreshableViewModel? _activeRefreshTarget;
    private bool _disposed;

    public MainViewModel(
        ICurrentUserService currentUserService,
        IAuthenticationService authenticationService,
        IAppDialogService dialogService,
        IAppNavigationService navigationService,
        IMessenger messenger,
        IAppThemeService themeService,
        ILogger<MainViewModel> logger)
    {
        _currentUserService = currentUserService;
        _authenticationService = authenticationService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        _messenger = messenger;
        _themeService = themeService;
        _logger = logger;
        _currentUserService.CurrentUserChanged += OnCurrentUserChanged;
        _navigationService.NavigationStateChanged += OnNavigationStateChanged;
        _themeService.ThemeChanged += OnThemeChanged;
    }

    public string ApplicationName => "Quản lý thư viện";

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

    public bool CanManageEmployees =>
        _authenticationService.CheckPermission(Permission.ManageEmployees);

    public bool CanViewActivityLogs =>
        _authenticationService.CheckPermission(Permission.ViewActivityLogs);

    public IRefreshableViewModel? ActiveRefreshTarget => _activeRefreshTarget;

    public bool IsRefreshAvailable => _activeRefreshTarget is not null;

    public void SetActiveRefreshTarget(IRefreshableViewModel? refreshTarget)
    {
        if (ReferenceEquals(_activeRefreshTarget, refreshTarget))
        {
            return;
        }

        _activeRefreshTarget = refreshTarget;
        OnPropertyChanged(nameof(ActiveRefreshTarget));
        OnPropertyChanged(nameof(IsRefreshAvailable));
    }

    public void ClearActiveRefreshTarget(IRefreshableViewModel refreshTarget)
    {
        ArgumentNullException.ThrowIfNull(refreshTarget);

        if (ReferenceEquals(_activeRefreshTarget, refreshTarget))
        {
            SetActiveRefreshTarget(null);
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigateBack))]
    private void NavigateBack()
    {
        _navigationService.GoBack();
    }

    private bool CanNavigateBack() => _navigationService.CanGoBack;

    [RelayCommand(CanExecute = nameof(CanNavigateForward))]
    private void NavigateForward()
    {
        _navigationService.GoForward();
    }

    private bool CanNavigateForward() => _navigationService.CanGoForward;

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
        _navigationService.NavigationStateChanged -= OnNavigationStateChanged;
        _themeService.ThemeChanged -= OnThemeChanged;
        _activeRefreshTarget = null;
        _disposed = true;
    }

    private void OnCurrentUserChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(CurrentEmployeeName));
        OnPropertyChanged(nameof(CurrentEmployeeRole));
        OnPropertyChanged(nameof(CanManageSystemSettings));
        OnPropertyChanged(nameof(CanManageEmployees));
        OnPropertyChanged(nameof(CanViewActivityLogs));
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ThemeButtonText));
    }

    private void OnNavigationStateChanged(object? sender, EventArgs e)
    {
        NavigateBackCommand.NotifyCanExecuteChanged();
        NavigateForwardCommand.NotifyCanExecuteChanged();
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể hoàn tất thao tác trên cửa sổ chính.");
        return "Không thể hoàn tất thao tác. Vui lòng thử lại.";
    }
}
