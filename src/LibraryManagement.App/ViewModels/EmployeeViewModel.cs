using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Notifications;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class EmployeeViewModel : BaseViewModel
{
    private readonly IEmployeeService _employeeService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<EmployeeViewModel> _logger;
    private int _originalRoleId;

    public EmployeeViewModel(
        IEmployeeService employeeService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<EmployeeViewModel> logger)
    {
        _employeeService = employeeService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public ObservableCollection<EmployeeListItemDto> Employees { get; } = [];

    public ObservableCollection<RoleDto> Roles { get; } = [];

    public IReadOnlyList<Gender> Genders { get; } =
        Enum.GetValues<Gender>();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EmployeeListItemDto? SelectedEmployee { get; set; }

    [ObservableProperty]
    public partial int EditingId { get; private set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mã nhân viên.")]
    [MaxLength(20, ErrorMessage = "Mã nhân viên không được vượt quá 20 ký tự.")]
    public partial string EmployeeCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên nhân viên.")]
    [MaxLength(150, ErrorMessage = "Tên nhân viên không được vượt quá 150 ký tự.")]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime? DateOfBirth { get; set; }

    [ObservableProperty]
    public partial Gender Gender { get; set; }

    [ObservableProperty]
    public partial string? PhoneNumber { get; set; }

    [ObservableProperty]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [NotifyDataErrorInfo]
    public partial string? Email { get; set; }

    [ObservableProperty]
    public partial string? Address { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự.")]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int SelectedRoleId { get; set; }

    [ObservableProperty]
    public partial string NewPassword { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PageNumber { get; private set; } = 1;

    [ObservableProperty]
    public partial int TotalPages { get; private set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; private set; }

    public bool IsEditing => EditingId > 0;

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return ExecuteBusyAsync(
            async token =>
            {
                IReadOnlyList<RoleDto> roles =
                    await _employeeService.GetRolesAsync(token);
                ReplaceItems(Roles, roles);
                await LoadPageCoreAsync(token);
            },
            "Đang tải danh sách nhân viên...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tìm nhân viên...",
            cancellationToken);
    }

    [RelayCommand]
    private Task LoadSelectedAsync(CancellationToken cancellationToken)
    {
        int? id = SelectedEmployee?.Id;
        return id.HasValue
            ? ExecuteBusyAsync(
                token => LoadDetailAsync(id.Value, token),
                "Đang tải thông tin nhân viên...",
                cancellationToken)
            : Task.CompletedTask;
    }

    [RelayCommand]
    private void New()
    {
        SelectedEmployee = null;
        EditingId = 0;
        _originalRoleId = 0;
        EmployeeCode = string.Empty;
        FullName = string.Empty;
        DateOfBirth = null;
        Gender = Gender.Other;
        PhoneNumber = null;
        Email = null;
        Address = null;
        Username = string.Empty;
        SelectedRoleId = Roles.FirstOrDefault()?.Id ?? 0;
        NewPassword = string.Empty;
        ClearValidation();
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsEditing));
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại dữ liệu nhân viên.";
            return Task.CompletedTask;
        }

        if (SelectedRoleId <= 0)
        {
            ErrorMessage = "Vui lòng chọn vai trò.";
            return Task.CompletedTask;
        }

        if (!IsEditing && string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu ban đầu.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                int roleId = IsEditing ? _originalRoleId : SelectedRoleId;
                var request = new EmployeeUpsertRequest(
                    EmployeeCode,
                    FullName,
                    DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(DateOfBirth.Value)
                        : null,
                    Gender,
                    PhoneNumber,
                    Email,
                    Address,
                    Username,
                    roleId,
                    IsEditing ? null : NewPassword);
                OperationResult result = IsEditing
                    ? await _employeeService.UpdateAsync(
                        EditingId,
                        request,
                        token)
                    : await _employeeService.CreateAsync(request, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lưu thành công",
                    "Thông tin nhân viên đã được cập nhật.",
                    NotificationSeverity.Success);
                await LoadPageCoreAsync(token);
                New();
            },
            "Đang lưu nhân viên...",
            cancellationToken);
    }

    [RelayCommand]
    private Task LockAsync(CancellationToken cancellationToken)
    {
        return ChangeLockStateAsync(true, cancellationToken);
    }

    [RelayCommand]
    private Task UnlockAsync(CancellationToken cancellationToken)
    {
        return ChangeLockStateAsync(false, cancellationToken);
    }

    [RelayCommand]
    private Task ResetPasswordAsync(CancellationToken cancellationToken)
    {
        if (!IsEditing || string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage =
                "Vui lòng chọn nhân viên và nhập mật khẩu mới.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                OperationResult result =
                    await _employeeService.ResetPasswordAsync(
                        EditingId,
                        NewPassword,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                NewPassword = string.Empty;
                _notificationService.Show(
                    "Reset mật khẩu thành công",
                    "Mật khẩu tài khoản đã được thay đổi.",
                    NotificationSeverity.Success);
            },
            "Đang reset mật khẩu...",
            cancellationToken);
    }

    [RelayCommand]
    private Task ChangeRoleAsync(CancellationToken cancellationToken)
    {
        if (!IsEditing || SelectedRoleId <= 0)
        {
            ErrorMessage = "Vui lòng chọn nhân viên và vai trò.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                OperationResult result = await _employeeService.ChangeRoleAsync(
                    EditingId,
                    SelectedRoleId,
                    token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _originalRoleId = SelectedRoleId;
                _notificationService.Show(
                    "Đổi vai trò thành công",
                    "Vai trò của nhân viên đã được cập nhật.",
                    NotificationSeverity.Success);
                await LoadPageCoreAsync(token);
            },
            "Đang đổi vai trò...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tải trang trước...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return ExecuteBusyAsync(
            LoadPageCoreAsync,
            "Đang tải trang sau...",
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể xử lý dữ liệu nhân viên.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý dữ liệu nhân viên. Vui lòng thử lại.";
    }

    private async Task LoadPageCoreAsync(CancellationToken cancellationToken)
    {
        PagedResult<EmployeeListItemDto> result =
            await _employeeService.GetAllAsync(
                new EmployeeSearchRequest(
                    Keyword: SearchText,
                    PageNumber: PageNumber,
                    PageSize: 20),
                cancellationToken);
        ReplaceItems(Employees, result.Items);
        PageNumber = result.PageNumber;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        NotifyPaging();
    }

    private async Task LoadDetailAsync(
        int id,
        CancellationToken cancellationToken)
    {
        EmployeeDetailDto? employee =
            await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            ErrorMessage = "Nhân viên không tồn tại.";
            return;
        }

        EditingId = employee.Id;
        _originalRoleId = employee.RoleId;
        EmployeeCode = employee.EmployeeCode;
        FullName = employee.FullName;
        DateOfBirth = employee.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
        Gender = employee.Gender;
        PhoneNumber = employee.PhoneNumber;
        Email = employee.Email;
        Address = employee.Address;
        Username = employee.Username;
        SelectedRoleId = employee.RoleId;
        NewPassword = string.Empty;
        ClearValidation();
        OnPropertyChanged(nameof(IsEditing));
    }

    private async Task ChangeLockStateAsync(
        bool lockAccount,
        CancellationToken cancellationToken)
    {
        if (!IsEditing)
        {
            ErrorMessage = "Vui lòng chọn nhân viên.";
            return;
        }

        bool confirmed = await _dialogService.ConfirmAsync(
            lockAccount ? "Khóa tài khoản" : "Mở khóa tài khoản",
            $"Bạn có chắc chắn muốn {(lockAccount ? "khóa" : "mở khóa")} tài khoản này?",
            "Xác nhận",
            "Hủy",
            cancellationToken);
        if (!confirmed)
        {
            return;
        }

        await ExecuteBusyAsync(
            async token =>
            {
                OperationResult result = lockAccount
                    ? await _employeeService.LockAsync(EditingId, token)
                    : await _employeeService.UnlockAsync(EditingId, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Cập nhật thành công",
                    $"Tài khoản đã được {(lockAccount ? "khóa" : "mở khóa")}.",
                    NotificationSeverity.Success);
                await LoadPageCoreAsync(token);
            },
            "Đang cập nhật tài khoản...",
            cancellationToken);
    }

    private bool CanGoPrevious() => PageNumber > 1;

    private bool CanGoNext() => PageNumber < TotalPages;

    private void NotifyPaging()
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> items)
    {
        target.Clear();
        foreach (T item in items)
        {
            target.Add(item);
        }
    }
}
