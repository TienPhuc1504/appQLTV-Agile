using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Models;
using LibraryManagement.App.Notifications;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class ReaderViewModel : BaseViewModel, IDisposable
{
    private readonly IReaderService _readerService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<ReaderViewModel> _logger;
    private CancellationTokenSource? _searchDelayCancellation;
    private bool _initializing = true;
    private bool _disposed;

    public ReaderViewModel(
        IReaderService readerService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<ReaderViewModel> logger)
    {
        _readerService = readerService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
        SelectedGender = Genders[0];
        SelectedReaderType = ReaderTypes[0];
        SelectedStatusFilter = StatusFilters[0];
        SelectedTypeFilter = TypeFilters[0];
        RegisteredAt = DateTime.Today;
        ExpirationDate = DateTime.Today;
        _initializing = false;
    }

    public ObservableCollection<ReaderListItemDto> Readers { get; } = [];

    public ObservableCollection<ReaderBorrowHistoryDto> BorrowHistory { get; } = [];

    public ObservableCollection<ReaderFineDto> OutstandingFines { get; } = [];

    public IReadOnlyList<EnumOption<Gender>> Genders { get; } =
    [
        new(Gender.Other, "Khác"),
        new(Gender.Male, "Nam"),
        new(Gender.Female, "Nữ")
    ];

    public IReadOnlyList<EnumOption<ReaderType>> ReaderTypes { get; } =
    [
        new(ReaderType.Student, "Sinh viên"),
        new(ReaderType.Lecturer, "Giảng viên"),
        new(ReaderType.Adult, "Người lớn"),
        new(ReaderType.Child, "Trẻ em"),
        new(ReaderType.Other, "Khác")
    ];

    public IReadOnlyList<EnumFilterOption<ReaderStatus>> StatusFilters { get; } =
    [
        new(null, "Tất cả trạng thái"),
        new(ReaderStatus.Active, "Đang hoạt động"),
        new(ReaderStatus.Locked, "Đã khóa"),
        new(ReaderStatus.Expired, "Hết hạn"),
        new(ReaderStatus.Inactive, "Ngừng hoạt động")
    ];

    public IReadOnlyList<EnumFilterOption<ReaderType>> TypeFilters { get; } =
    [
        new(null, "Tất cả loại độc giả"),
        new(ReaderType.Student, "Sinh viên"),
        new(ReaderType.Lecturer, "Giảng viên"),
        new(ReaderType.Adult, "Người lớn"),
        new(ReaderType.Child, "Trẻ em"),
        new(ReaderType.Other, "Khác")
    ];

    public IReadOnlyList<int> PageSizes { get; } = [10, 20, 50, 100];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EnumFilterOption<ReaderStatus>? SelectedStatusFilter { get; set; }

    [ObservableProperty]
    public partial EnumFilterOption<ReaderType>? SelectedTypeFilter { get; set; }

    [ObservableProperty]
    public partial ReaderListItemDto? SelectedReader { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageSummary))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageNumber { get; set; } = 1;

    [ObservableProperty]
    public partial int PageSize { get; set; } = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageSummary))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial ReaderSortField SortBy { get; set; } =
        ReaderSortField.FullName;

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditIdentity))]
    [NotifyPropertyChangedFor(nameof(CanEditCardDates))]
    public partial int? EditingReaderId { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mã độc giả.")]
    [MaxLength(20, ErrorMessage = "Mã độc giả không được vượt quá 20 ký tự.")]
    [RegularExpression(
        @"^[\p{L}\p{N}._-]+$",
        ErrorMessage = "Mã độc giả chứa ký tự không hợp lệ.")]
    public partial string ReaderCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên độc giả.")]
    [MaxLength(150, ErrorMessage = "Tên độc giả không được vượt quá 150 ký tự.")]
    public partial string FullName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReaderViewModel), nameof(ValidateDateOfBirth))]
    public partial DateTime? DateOfBirth { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public partial EnumOption<Gender> SelectedGender { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReaderViewModel), nameof(ValidatePhoneNumber))]
    public partial string? PhoneNumber { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReaderViewModel), nameof(ValidateEmail))]
    public partial string? Email { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
    public partial string? Address { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng chọn loại độc giả.")]
    public partial EnumOption<ReaderType> SelectedReaderType { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReaderViewModel), nameof(ValidateRegisteredAt))]
    public partial DateTime RegisteredAt { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(ReaderViewModel), nameof(ValidateExpirationDate))]
    public partial DateTime ExpirationDate { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(
        500,
        ErrorMessage = "Đường dẫn ảnh đại diện không được vượt quá 500 ký tự.")]
    public partial string? AvatarPath { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    public partial string SelectedReaderName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial decimal OutstandingFineTotal { get; set; }

    public string PageSummary => $"Trang {PageNumber}/{TotalPages}";

    public bool CanEditIdentity => !EditingReaderId.HasValue;

    public bool CanEditCardDates => !EditingReaderId.HasValue;

    public static ValidationResult? ValidateDateOfBirth(
        DateTime? value,
        ValidationContext context)
    {
        return value?.Date > DateTime.Today
            ? new ValidationResult(
                "Ngày sinh không được lớn hơn ngày hiện tại.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidateRegisteredAt(
        DateTime value,
        ValidationContext context)
    {
        return value.Date > DateTime.Today
            ? new ValidationResult(
                "Ngày đăng ký không được lớn hơn ngày hiện tại.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidateExpirationDate(
        DateTime value,
        ValidationContext context)
    {
        var viewModel = (ReaderViewModel)context.ObjectInstance;
        return value.Date <= viewModel.RegisteredAt.Date
            ? new ValidationResult(
                "Ngày hết hạn phải lớn hơn ngày đăng ký.")
            : ValidationResult.Success;
    }

    public static ValidationResult? ValidatePhoneNumber(
        string? value,
        ValidationContext context)
    {
        return ValidateDomainValue(
            () => DomainValidator.OptionalPhoneNumber(value));
    }

    public static ValidationResult? ValidateEmail(
        string? value,
        ValidationContext context)
    {
        return ValidateDomainValue(
            () => DomainValidator.OptionalEmail(value));
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                if (!EditingReaderId.HasValue)
                {
                    await SetSuggestedExpirationDateAsync(token);
                }

                await RefreshReadersAsync(token);
            },
            "Đang tải danh sách độc giả...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        CancelPendingSearch();
        return ExecuteBusyAsync(
            RefreshReadersAsync,
            "Đang tìm kiếm độc giả...",
            cancellationToken);
    }

    [RelayCommand]
    private Task NewAsync(CancellationToken cancellationToken)
    {
        ClearEditor();
        SelectedTabIndex = 1;
        return ExecuteBusyAsync(
            SetSuggestedExpirationDateAsync,
            "Đang chuẩn bị biểu mẫu...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task EditAsync(CancellationToken cancellationToken)
    {
        int id = SelectedReader!.Id;
        return ExecuteBusyAsync(
            async token =>
            {
                ReaderDetailDto? detail =
                    await _readerService.GetByIdAsync(id, token);
                if (detail is null)
                {
                    ErrorMessage = "Độc giả không tồn tại.";
                    return;
                }

                EditingReaderId = detail.Id;
                ReaderCode = detail.ReaderCode;
                FullName = detail.FullName;
                DateOfBirth = detail.DateOfBirth?.ToDateTime(
                    TimeOnly.MinValue);
                SelectedGender = Genders.First(
                    item => item.Value == detail.Gender);
                PhoneNumber = detail.PhoneNumber;
                Email = detail.Email;
                Address = detail.Address;
                SelectedReaderType = ReaderTypes.First(
                    item => item.Value == detail.ReaderType);
                RegisteredAt = detail.RegisteredAt.ToDateTime(
                    TimeOnly.MinValue);
                ExpirationDate = detail.ExpirationDate.ToDateTime(
                    TimeOnly.MinValue);
                AvatarPath = detail.AvatarPath;
                Notes = detail.Notes;
                ClearValidation();
                ErrorMessage = null;
                SelectedTabIndex = 1;
            },
            "Đang tải thông tin độc giả...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin độc giả.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                var request = new ReaderUpsertRequest(
                    ReaderCode,
                    FullName,
                    DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(DateOfBirth.Value)
                        : null,
                    SelectedGender.Value,
                    PhoneNumber,
                    Email,
                    Address,
                    SelectedReaderType.Value,
                    DateOnly.FromDateTime(RegisteredAt),
                    DateOnly.FromDateTime(ExpirationDate),
                    AvatarPath,
                    Notes);
                OperationResult result = EditingReaderId.HasValue
                    ? await _readerService.UpdateAsync(
                        EditingReaderId.Value,
                        request,
                        token)
                    : await _readerService.CreateAsync(request, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lưu thành công",
                    "Thông tin độc giả đã được cập nhật.",
                    NotificationSeverity.Success);
                ClearEditor();
                await SetSuggestedExpirationDateAsync(token);
                await RefreshReadersAsync(token);
                SelectedTabIndex = 0;
            },
            "Đang lưu độc giả...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanLock))]
    private Task LockAsync(CancellationToken cancellationToken)
    {
        ReaderListItemDto reader = SelectedReader!;
        return ChangeStateAsync(
            reader,
            "Khóa độc giả",
            $"Bạn có chắc muốn khóa độc giả “{reader.FullName}”?",
            _readerService.LockAsync,
            "Độc giả đã được khóa.",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private Task UnlockAsync(CancellationToken cancellationToken)
    {
        ReaderListItemDto reader = SelectedReader!;
        return ChangeStateAsync(
            reader,
            "Mở khóa độc giả",
            $"Bạn có chắc muốn mở khóa độc giả “{reader.FullName}”?",
            _readerService.UnlockAsync,
            "Độc giả đã được mở khóa.",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task RenewCardAsync(CancellationToken cancellationToken)
    {
        ReaderListItemDto reader = SelectedReader!;
        return ChangeStateAsync(
            reader,
            "Gia hạn thẻ",
            $"Gia hạn thẻ cho độc giả “{reader.FullName}” theo thời hạn hệ thống?",
            _readerService.RenewCardAsync,
            "Thẻ độc giả đã được gia hạn.",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task ViewHistoryAsync(CancellationToken cancellationToken)
    {
        ReaderListItemDto reader = SelectedReader!;
        return ExecuteBusyAsync(
            async token =>
            {
                IReadOnlyList<ReaderBorrowHistoryDto> history =
                    await _readerService.GetBorrowingHistoryAsync(
                        reader.Id,
                        token);
                BorrowHistory.Clear();
                foreach (ReaderBorrowHistoryDto item in history)
                {
                    BorrowHistory.Add(item);
                }

                SelectedReaderName = reader.FullName;
                SelectedTabIndex = 2;
            },
            "Đang tải lịch sử mượn...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task ViewFinesAsync(CancellationToken cancellationToken)
    {
        ReaderListItemDto reader = SelectedReader!;
        return ExecuteBusyAsync(
            async token =>
            {
                IReadOnlyList<ReaderFineDto> fines =
                    await _readerService.GetOutstandingFinesAsync(
                        reader.Id,
                        token);
                OutstandingFines.Clear();
                foreach (ReaderFineDto item in fines)
                {
                    OutstandingFines.Add(item);
                }

                OutstandingFineTotal =
                    OutstandingFines.Sum(item => item.OutstandingAmount);
                SelectedReaderName = reader.FullName;
                SelectedTabIndex = 3;
            },
            "Đang tải tiền phạt...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return ExecuteBusyAsync(
            RefreshReadersAsync,
            "Đang chuyển trang...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return ExecuteBusyAsync(
            RefreshReadersAsync,
            "Đang chuyển trang...",
            cancellationToken);
    }

    public Task ApplySortAsync(
        ReaderSortField sortField,
        CancellationToken cancellationToken = default)
    {
        if (SortBy == sortField)
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortBy = sortField;
            SortDescending = false;
        }

        PageNumber = 1;
        return ExecuteBusyAsync(
            RefreshReadersAsync,
            "Đang sắp xếp danh sách...",
            cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancelPendingSearch();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(
            exception,
            "Không thể xử lý chức năng quản lý độc giả.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý dữ liệu độc giả. Vui lòng thử lại.";
    }

    partial void OnSearchTextChanged(string value)
    {
        ScheduleSearch();
    }

    partial void OnSelectedStatusFilterChanged(
        EnumFilterOption<ReaderStatus>? value)
    {
        if (!_initializing)
        {
            _ = RunImmediateFilterAsync();
        }
    }

    partial void OnSelectedTypeFilterChanged(
        EnumFilterOption<ReaderType>? value)
    {
        if (!_initializing)
        {
            _ = RunImmediateFilterAsync();
        }
    }

    partial void OnPageSizeChanged(int value)
    {
        PageNumber = 1;
        _ = RunImmediateFilterAsync();
    }

    partial void OnSelectedReaderChanged(ReaderListItemDto? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        LockCommand.NotifyCanExecuteChanged();
        UnlockCommand.NotifyCanExecuteChanged();
        RenewCardCommand.NotifyCanExecuteChanged();
        ViewHistoryCommand.NotifyCanExecuteChanged();
        ViewFinesCommand.NotifyCanExecuteChanged();
    }

    private async Task ChangeStateAsync(
        ReaderListItemDto reader,
        string title,
        string message,
        Func<int, CancellationToken, Task<OperationResult>> operation,
        string successMessage,
        CancellationToken cancellationToken)
    {
        await ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    title,
                    message,
                    title,
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result =
                    await operation(reader.Id, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Cập nhật thành công",
                    successMessage,
                    NotificationSeverity.Success);
                SelectedReader = null;
                await RefreshReadersAsync(token);
            },
            "Đang cập nhật độc giả...",
            cancellationToken);
    }

    private async Task RefreshReadersAsync(CancellationToken cancellationToken)
    {
        PagedResult<ReaderListItemDto> result =
            await _readerService.SearchAsync(
                new ReaderSearchRequest(
                    SearchText,
                    SelectedStatusFilter?.Value,
                    SelectedTypeFilter?.Value,
                    PageNumber,
                    PageSize,
                    SortBy,
                    SortDescending),
                cancellationToken);
        Readers.Clear();
        foreach (ReaderListItemDto item in result.Items)
        {
            Readers.Add(item);
        }

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        PageNumber = Math.Min(result.PageNumber, result.TotalPages);
    }

    private async Task SetSuggestedExpirationDateAsync(
        CancellationToken cancellationToken)
    {
        DateOnly registeredAt = DateOnly.FromDateTime(RegisteredAt);
        DateOnly expirationDate =
            await _readerService.GetSuggestedExpirationDateAsync(
                registeredAt,
                cancellationToken);
        ExpirationDate = expirationDate.ToDateTime(TimeOnly.MinValue);
    }

    private void ClearEditor()
    {
        EditingReaderId = null;
        ReaderCode = string.Empty;
        FullName = string.Empty;
        DateOfBirth = null;
        SelectedGender = Genders[0];
        PhoneNumber = null;
        Email = null;
        Address = null;
        SelectedReaderType = ReaderTypes[0];
        RegisteredAt = DateTime.Today;
        ExpirationDate = DateTime.Today;
        AvatarPath = null;
        Notes = null;
        ClearValidation();
        ErrorMessage = null;
    }

    private bool CanEdit() => SelectedReader is not null;

    private bool CanLock() =>
        SelectedReader is not null
        && SelectedReader.Status is ReaderStatus.Active or ReaderStatus.Expired;

    private bool CanUnlock() =>
        SelectedReader?.Status == ReaderStatus.Locked;

    private bool CanGoPrevious() => PageNumber > 1;

    private bool CanGoNext() => PageNumber < TotalPages;

    private void ScheduleSearch()
    {
        if (_disposed)
        {
            return;
        }

        CancelPendingSearch();
        _searchDelayCancellation = new CancellationTokenSource();
        _ = RunDelayedSearchAsync(_searchDelayCancellation.Token);
    }

    private async Task RunDelayedSearchAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(400, cancellationToken);
            PageNumber = 1;
            await ExecuteBusyAsync(
                RefreshReadersAsync,
                "Đang tìm kiếm độc giả...",
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Task RunImmediateFilterAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        PageNumber = 1;
        return ExecuteBusyAsync(
            RefreshReadersAsync,
            "Đang lọc danh sách...",
            CancellationToken.None);
    }

    private void CancelPendingSearch()
    {
        _searchDelayCancellation?.Cancel();
        _searchDelayCancellation?.Dispose();
        _searchDelayCancellation = null;
    }

    private static ValidationResult? ValidateDomainValue(
        Func<string?> validator)
    {
        try
        {
            validator();
            return ValidationResult.Success;
        }
        catch (DomainValidationException exception)
        {
            return new ValidationResult(exception.Message);
        }
    }
}
