using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Models;
using LibraryManagement.App.Notifications;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class BookCopyViewModel : BaseViewModel, IDisposable
{
    private readonly IBookCopyService _bookCopyService;
    private readonly IBookService _bookService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<BookCopyViewModel> _logger;
    private CancellationTokenSource? _searchDelayCancellation;
    private bool _disposed;

    public BookCopyViewModel(
        IBookCopyService bookCopyService,
        IBookService bookService,
        IAppNotificationService notificationService,
        ILogger<BookCopyViewModel> logger)
    {
        _bookCopyService = bookCopyService;
        _bookService = bookService;
        _notificationService = notificationService;
        _logger = logger;
        ImportedAt = DateTime.Today;
        SelectedPhysicalCondition = PhysicalConditions[0];
        SelectedStatus = Statuses[0];
    }

    public ObservableCollection<BookCopyDto> Copies { get; } = [];

    public ObservableCollection<BookCopyBorrowHistoryDto> BorrowHistory { get; } = [];

    public ObservableCollection<LookupItem> Books { get; } = [];

    public IReadOnlyList<EnumOption<BookCopyStatus>> Statuses { get; } =
    [
        new(BookCopyStatus.Available, "Có sẵn"),
        new(BookCopyStatus.Borrowed, "Đang mượn"),
        new(BookCopyStatus.Damaged, "Hư hỏng"),
        new(BookCopyStatus.Lost, "Bị mất"),
        new(BookCopyStatus.Maintenance, "Bảo trì"),
        new(BookCopyStatus.Inactive, "Ngừng sử dụng")
    ];

    public IReadOnlyList<EnumOption<PhysicalCondition>> PhysicalConditions { get; } =
    [
        new(PhysicalCondition.New, "Mới"),
        new(PhysicalCondition.Good, "Tốt"),
        new(PhysicalCondition.Worn, "Cũ"),
        new(PhysicalCondition.Damaged, "Hư hỏng"),
        new(PhysicalCondition.Lost, "Bị mất")
    ];

    public IReadOnlyList<int> PageSizes { get; } = [10, 20, 50, 100];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LookupItem? BookFilter { get; set; }

    [ObservableProperty]
    public partial EnumOption<BookCopyStatus>? StatusFilter { get; set; }

    [ObservableProperty]
    public partial BookCopyDto? SelectedCopy { get; set; }

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
    public partial int SelectedTabIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditIdentity))]
    public partial int? EditingCopyId { get; set; }

    public bool CanEditIdentity => !EditingCopyId.HasValue;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mã bản sách.")]
    [MaxLength(30, ErrorMessage = "Mã bản sách không được vượt quá 30 ký tự.")]
    [RegularExpression(
        @"^[\p{L}\p{N}._-]+$",
        ErrorMessage = "Mã bản sách chứa ký tự không hợp lệ.")]
    public partial string CopyCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng chọn sách.")]
    public partial LookupItem? SelectedBook { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(100, ErrorMessage = "Vị trí kệ không được vượt quá 100 ký tự.")]
    public partial string? ShelfLocation { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(
        typeof(BookCopyViewModel),
        nameof(ValidateImportedAt))]
    public partial DateTime ImportedAt { get; set; }

    [ObservableProperty]
    public partial EnumOption<PhysicalCondition> SelectedPhysicalCondition { get; set; }

    [ObservableProperty]
    public partial EnumOption<BookCopyStatus> SelectedStatus { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public partial string? Notes { get; set; }

    public string PageSummary => $"Trang {PageNumber}/{TotalPages}";

    public static ValidationResult? ValidateImportedAt(
        DateTime value,
        ValidationContext context)
    {
        return value.Date > DateTime.Today
            ? new ValidationResult("Ngày nhập không được lớn hơn ngày hiện tại.")
            : ValidationResult.Success;
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                await LoadBooksAsync(token);
                await RefreshCopiesAsync(token);
            },
            "Đang tải danh sách bản sách...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        CancelPendingSearch();
        return ExecuteBusyAsync(
            RefreshCopiesAsync,
            "Đang tìm kiếm bản sách...",
            cancellationToken);
    }

    [RelayCommand]
    private void New()
    {
        ClearEditor();
        SelectedTabIndex = 1;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private void Edit()
    {
        BookCopyDto copy = SelectedCopy!;
        EditingCopyId = copy.Id;
        CopyCode = copy.CopyCode;
        SelectedBook = Books.FirstOrDefault(item => item.Id == copy.BookId);
        ShelfLocation = copy.ShelfLocation;
        ImportedAt = copy.ImportedAt.ToDateTime(TimeOnly.MinValue);
        SelectedPhysicalCondition = PhysicalConditions.First(
            item => item.Value == copy.PhysicalCondition);
        SelectedStatus = Statuses.First(item => item.Value == copy.Status);
        Notes = copy.Notes;
        ClearValidation();
        ErrorMessage = null;
        SelectedTabIndex = 1;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task ViewHistoryAsync(CancellationToken cancellationToken)
    {
        int bookCopyId = SelectedCopy!.Id;
        return ExecuteBusyAsync(
            async token =>
            {
                IReadOnlyList<BookCopyBorrowHistoryDto> history =
                    await _bookCopyService.GetBorrowHistoryAsync(bookCopyId, token);
                BorrowHistory.Clear();
                foreach (BookCopyBorrowHistoryDto item in history)
                {
                    BorrowHistory.Add(item);
                }

                SelectedTabIndex = 2;
            },
            "Đang tải lịch sử mượn...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin bản sách.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                var request = new BookCopyUpsertRequest(
                    CopyCode,
                    SelectedBook!.Id,
                    ShelfLocation,
                    DateOnly.FromDateTime(ImportedAt),
                    SelectedPhysicalCondition.Value,
                    SelectedStatus.Value,
                    Notes);
                OperationResult result = EditingCopyId.HasValue
                    ? await _bookCopyService.UpdateAsync(
                        EditingCopyId.Value,
                        request,
                        token)
                    : await _bookCopyService.CreateAsync(request, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lưu thành công",
                    "Thông tin bản sách đã được cập nhật.",
                    NotificationSeverity.Success);
                ClearEditor();
                await RefreshCopiesAsync(token);
                SelectedTabIndex = 0;
            },
            "Đang lưu bản sách...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return ExecuteBusyAsync(
            RefreshCopiesAsync,
            "Đang chuyển trang...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return ExecuteBusyAsync(
            RefreshCopiesAsync,
            "Đang chuyển trang...",
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
        _logger.LogError(exception, "Không thể xử lý chức năng quản lý bản sách.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý dữ liệu bản sách. Vui lòng thử lại.";
    }

    partial void OnSearchTextChanged(string value)
    {
        ScheduleSearch();
    }

    partial void OnBookFilterChanged(LookupItem? value)
    {
        _ = RunImmediateFilterAsync();
    }

    partial void OnStatusFilterChanged(EnumOption<BookCopyStatus>? value)
    {
        _ = RunImmediateFilterAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageNumber = 1;
        _ = RunImmediateFilterAsync();
    }

    partial void OnSelectedCopyChanged(BookCopyDto? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        ViewHistoryCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadBooksAsync(CancellationToken cancellationToken)
    {
        PagedResult<BookListItemDto> result = await _bookService.SearchAsync(
            new BookSearchRequest(PageNumber: 1, PageSize: 100),
            cancellationToken);
        Books.Clear();
        foreach (BookListItemDto book in result.Items)
        {
            Books.Add(new LookupItem(book.Id, $"{book.BookCode} - {book.Title}"));
        }
    }

    private async Task RefreshCopiesAsync(CancellationToken cancellationToken)
    {
        PagedResult<BookCopyDto> result = await _bookCopyService.SearchAsync(
            new BookCopySearchRequest(
                SearchText,
                BookFilter?.Id,
                StatusFilter?.Value,
                PageNumber,
                PageSize),
            cancellationToken);
        Copies.Clear();
        foreach (BookCopyDto item in result.Items)
        {
            Copies.Add(item);
        }

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        PageNumber = Math.Min(result.PageNumber, result.TotalPages);
    }

    private void ClearEditor()
    {
        EditingCopyId = null;
        CopyCode = string.Empty;
        SelectedBook = null;
        ShelfLocation = null;
        ImportedAt = DateTime.Today;
        SelectedPhysicalCondition = PhysicalConditions[0];
        SelectedStatus = Statuses[0];
        Notes = null;
        ClearValidation();
        ErrorMessage = null;
    }

    private bool CanEdit() => SelectedCopy is not null;

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

    private async Task RunDelayedSearchAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(400, cancellationToken);
            PageNumber = 1;
            await ExecuteBusyAsync(
                RefreshCopiesAsync,
                "Đang tìm kiếm bản sách...",
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
            RefreshCopiesAsync,
            "Đang lọc danh sách...",
            CancellationToken.None);
    }

    private void CancelPendingSearch()
    {
        _searchDelayCancellation?.Cancel();
        _searchDelayCancellation?.Dispose();
        _searchDelayCancellation = null;
    }
}
