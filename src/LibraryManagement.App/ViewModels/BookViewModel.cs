using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Dialogs;
using LibraryManagement.App.Models;
using LibraryManagement.App.Notifications;
using LibraryManagement.App.Services;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class BookViewModel : BaseViewModel, IDisposable
{
    private readonly IBookService _bookService;
    private readonly IAuthorService _authorService;
    private readonly ICategoryService _categoryService;
    private readonly IPublisherService _publisherService;
    private readonly IBookCoverPickerService _coverPickerService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<BookViewModel> _logger;
    private CancellationTokenSource? _searchDelayCancellation;
    private bool _disposed;
    private bool _editingBookIsActive = true;

    public BookViewModel(
        IBookService bookService,
        IAuthorService authorService,
        ICategoryService categoryService,
        IPublisherService publisherService,
        IBookCoverPickerService coverPickerService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<BookViewModel> logger)
    {
        _bookService = bookService;
        _authorService = authorService;
        _categoryService = categoryService;
        _publisherService = publisherService;
        _coverPickerService = coverPickerService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
        PublicationYear = DateTime.Today.Year;
    }

    public ObservableCollection<BookListItemDto> Books { get; } = [];

    public ObservableCollection<LookupItem> Publishers { get; } = [];

    public ObservableCollection<LookupItem> Categories { get; } = [];

    public ObservableCollection<SelectableLookupItem> Authors { get; } = [];

    public ObservableCollection<SelectableLookupItem> BookCategories { get; } = [];

    public IReadOnlyList<int> PageSizes { get; } = [10, 20, 50, 100];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial LookupItem? CategoryFilter { get; set; }

    [ObservableProperty]
    public partial LookupItem? PublisherFilter { get; set; }

    [ObservableProperty]
    public partial bool IncludeInactive { get; set; }

    [ObservableProperty]
    public partial BookListItemDto? SelectedBook { get; set; }

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
    public partial int? EditingBookId { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập mã sách.")]
    [MaxLength(20, ErrorMessage = "Mã sách không được vượt quá 20 ký tự.")]
    [RegularExpression(
        @"^[\p{L}\p{N}._-]+$",
        ErrorMessage = "Mã sách chứa ký tự không hợp lệ.")]
    public partial string BookCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(BookViewModel), nameof(ValidateIsbn))]
    public partial string? ISBN { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng nhập tên sách.")]
    [MaxLength(300, ErrorMessage = "Tên sách không được vượt quá 300 ký tự.")]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Vui lòng chọn nhà xuất bản.")]
    public partial LookupItem? SelectedPublisher { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1000, 9999, ErrorMessage = "Năm xuất bản không hợp lệ.")]
    public partial int PublicationYear { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(50, ErrorMessage = "Ngôn ngữ không được vượt quá 50 ký tự.")]
    public partial string? Language { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0.")]
    public partial int PageCount { get; set; } = 1;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(
        typeof(decimal),
        "0",
        "79228162514264337593543950335",
        ErrorMessage = "Giá sách không được nhỏ hơn 0.")]
    public partial decimal Price { get; set; }

    [ObservableProperty]
    public partial string? CoverImageSourcePath { get; set; }

    [ObservableProperty]
    public partial string? CoverImagePreviewPath { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
    public partial string? Description { get; set; }

    public string PageSummary => $"Trang {PageNumber}/{TotalPages}";

    public static ValidationResult? ValidateIsbn(
        string? value,
        ValidationContext context)
    {
        try
        {
            BookValidator.Isbn(value);
            return ValidationResult.Success;
        }
        catch (DomainValidationException exception)
        {
            return new ValidationResult(exception.Message);
        }
    }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                await LoadLookupsAsync(token);
                await RefreshBooksAsync(token);
            },
            "Đang tải danh sách sách...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        CancelPendingSearch();
        return ExecuteBusyAsync(
            RefreshBooksAsync,
            "Đang tìm kiếm sách...",
            cancellationToken);
    }

    [RelayCommand]
    private void New()
    {
        ClearEditor();
        SelectedTabIndex = 1;
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task EditAsync(CancellationToken cancellationToken)
    {
        int id = SelectedBook!.Id;
        return ExecuteBusyAsync(
            async token =>
            {
                BookDetailDto? detail = await _bookService.GetByIdAsync(id, token);
                if (detail is null)
                {
                    ErrorMessage = "Sách không tồn tại.";
                    return;
                }

                EditingBookId = detail.Id;
                BookCode = detail.BookCode;
                ISBN = detail.ISBN;
                Title = detail.Title;
                SelectedPublisher =
                    Publishers.FirstOrDefault(item => item.Id == detail.PublisherId);
                PublicationYear = detail.PublicationYear;
                Language = detail.Language;
                PageCount = detail.PageCount;
                Price = detail.Price;
                CoverImageSourcePath = null;
                CoverImagePreviewPath = detail.CoverImagePath;
                Description = detail.Description;
                _editingBookIsActive = detail.IsActive;
                foreach (SelectableLookupItem item in Authors)
                {
                    item.IsSelected = detail.AuthorIds.Contains(item.Id);
                }

                foreach (SelectableLookupItem item in BookCategories)
                {
                    item.IsSelected = detail.CategoryIds.Contains(item.Id);
                }

                ClearValidation();
                ErrorMessage = null;
                SelectedTabIndex = 1;
            },
            "Đang tải chi tiết sách...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SaveAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin sách.";
            return Task.CompletedTask;
        }

        int[] authorIds = Authors
            .Where(item => item.IsSelected)
            .Select(item => item.Id)
            .ToArray();
        int[] categoryIds = BookCategories
            .Where(item => item.IsSelected)
            .Select(item => item.Id)
            .ToArray();
        if (authorIds.Length == 0 || categoryIds.Length == 0)
        {
            ErrorMessage =
                "Vui lòng chọn ít nhất một tác giả và một thể loại.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                var request = new BookUpsertRequest(
                    BookCode,
                    ISBN,
                    Title,
                    SelectedPublisher!.Id,
                    PublicationYear,
                    Language,
                    PageCount,
                    Price,
                    CoverImageSourcePath,
                    Description,
                    authorIds,
                    categoryIds,
                    _editingBookIsActive);
                OperationResult result = EditingBookId.HasValue
                    ? await _bookService.UpdateAsync(
                        EditingBookId.Value,
                        request,
                        token)
                    : await _bookService.CreateAsync(request, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lưu thành công",
                    "Thông tin sách đã được cập nhật.",
                    NotificationSeverity.Success);
                ClearEditor();
                await RefreshBooksAsync(token);
                SelectedTabIndex = 0;
            },
            "Đang lưu sách...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanEdit))]
    private Task DeactivateAsync(CancellationToken cancellationToken)
    {
        BookListItemDto selected = SelectedBook!;
        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Ngừng lưu hành sách",
                    $"Bạn có chắc muốn ngừng lưu hành “{selected.Title}”?",
                    "Ngừng lưu hành",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result =
                    await _bookService.DeactivateAsync(selected.Id, token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Cập nhật thành công",
                    "Sách đã được ngừng lưu hành.",
                    NotificationSeverity.Success);
                SelectedBook = null;
                await RefreshBooksAsync(token);
            },
            "Đang cập nhật sách...",
            cancellationToken);
    }

    [RelayCommand]
    private void ChooseCover()
    {
        string? selectedPath = _coverPickerService.PickCoverImage();
        if (selectedPath is null)
        {
            return;
        }

        CoverImageSourcePath = selectedPath;
        CoverImagePreviewPath = selectedPath;
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return ExecuteBusyAsync(RefreshBooksAsync, "Đang chuyển trang...", cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return ExecuteBusyAsync(RefreshBooksAsync, "Đang chuyển trang...", cancellationToken);
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
        _logger.LogError(exception, "Không thể xử lý chức năng quản lý sách.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý dữ liệu sách. Vui lòng thử lại.";
    }

    partial void OnSearchTextChanged(string value)
    {
        ScheduleSearch();
    }

    partial void OnCategoryFilterChanged(LookupItem? value)
    {
        _ = RunImmediateFilterAsync();
    }

    partial void OnPublisherFilterChanged(LookupItem? value)
    {
        _ = RunImmediateFilterAsync();
    }

    partial void OnIncludeInactiveChanged(bool value)
    {
        _ = RunImmediateFilterAsync();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageNumber = 1;
        _ = RunImmediateFilterAsync();
    }

    partial void OnSelectedBookChanged(BookListItemDto? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeactivateCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<PublisherDto> publishers =
            await _publisherService.GetAllAsync(false, cancellationToken);
        IReadOnlyList<AuthorDto> authors =
            await _authorService.GetAllAsync(false, cancellationToken);
        IReadOnlyList<CategoryDto> categories =
            await _categoryService.GetAllAsync(false, cancellationToken);

        Publishers.Clear();
        Categories.Clear();
        Authors.Clear();
        BookCategories.Clear();
        foreach (PublisherDto item in publishers)
        {
            Publishers.Add(new LookupItem(item.Id, item.Name));
        }

        foreach (CategoryDto item in categories)
        {
            Categories.Add(new LookupItem(item.Id, item.Name));
            BookCategories.Add(new SelectableLookupItem(item.Id, item.Name));
        }

        foreach (AuthorDto item in authors)
        {
            Authors.Add(new SelectableLookupItem(item.Id, item.FullName));
        }
    }

    private async Task RefreshBooksAsync(CancellationToken cancellationToken)
    {
        PagedResult<BookListItemDto> result = await _bookService.SearchAsync(
            new BookSearchRequest(
                SearchText,
                CategoryFilter?.Id,
                PublisherFilter?.Id,
                IncludeInactive ? null : true,
                PageNumber,
                PageSize),
            cancellationToken);
        Books.Clear();
        foreach (BookListItemDto item in result.Items)
        {
            Books.Add(item);
        }

        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages;
        PageNumber = Math.Min(result.PageNumber, result.TotalPages);
    }

    private void ClearEditor()
    {
        EditingBookId = null;
        _editingBookIsActive = true;
        BookCode = string.Empty;
        ISBN = null;
        Title = string.Empty;
        SelectedPublisher = null;
        PublicationYear = DateTime.Today.Year;
        Language = null;
        PageCount = 1;
        Price = 0;
        CoverImageSourcePath = null;
        CoverImagePreviewPath = null;
        Description = null;
        foreach (SelectableLookupItem item in Authors)
        {
            item.IsSelected = false;
        }

        foreach (SelectableLookupItem item in BookCategories)
        {
            item.IsSelected = false;
        }

        ClearValidation();
        ErrorMessage = null;
    }

    private bool CanEdit() => SelectedBook is not null;

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
                RefreshBooksAsync,
                "Đang tìm kiếm sách...",
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
            RefreshBooksAsync,
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
