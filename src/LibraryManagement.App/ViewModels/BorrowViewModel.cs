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

public sealed partial class BorrowViewModel : BaseViewModel
{
    private readonly IReaderService _readerService;
    private readonly IBookCopyService _bookCopyService;
    private readonly IBorrowService _borrowService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<BorrowViewModel> _logger;

    public BorrowViewModel(
        IReaderService readerService,
        IBookCopyService bookCopyService,
        IBorrowService borrowService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<BorrowViewModel> logger)
    {
        _readerService = readerService;
        _bookCopyService = bookCopyService;
        _borrowService = borrowService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
        BorrowDate = DateTime.Today;
        ExpectedReturnDate = DateTime.Today;
    }

    public ObservableCollection<ReaderListItemDto> ReaderResults { get; } = [];

    public ObservableCollection<BookCopyDto> SelectedCopies { get; } = [];

    [ObservableProperty]
    public partial string ReaderSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ReaderListItemDto? SelectedReader { get; set; }

    [ObservableProperty]
    public partial bool IsReaderEligible { get; set; }

    [ObservableProperty]
    public partial string EligibilityMessage { get; set; } =
        "Chưa kiểm tra điều kiện mượn.";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(30, ErrorMessage = "Mã bản sách không được vượt quá 30 ký tự.")]
    public partial string CopyCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BookCopyDto? SelectedCartCopy { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    public partial DateTime BorrowDate { get; set; }

    [ObservableProperty]
    public partial DateTime ExpectedReturnDate { get; set; }

    [ObservableProperty]
    public partial int MaximumBorrowedBooks { get; set; }

    [ObservableProperty]
    public partial int DefaultBorrowDays { get; set; }

    [ObservableProperty]
    public partial decimal MaximumOutstandingFineAmount { get; set; }

    public string SelectedCopySummary =>
        $"{SelectedCopies.Count}/{MaximumBorrowedBooks} bản sách trong phiếu";

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                BorrowPolicyDto policy =
                    await _borrowService.GetBorrowPolicyAsync(token);
                MaximumBorrowedBooks = policy.MaximumBorrowedBooks;
                DefaultBorrowDays = policy.DefaultBorrowDays;
                MaximumOutstandingFineAmount =
                    policy.MaximumOutstandingFineAmount;
                BorrowDate = DateTime.Today;
                ExpectedReturnDate =
                    DateTime.Today.AddDays(policy.DefaultBorrowDays);
                OnPropertyChanged(nameof(SelectedCopySummary));
            },
            "Đang tải chính sách mượn sách...",
            cancellationToken);
    }

    [RelayCommand]
    private Task SearchReaderAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ReaderSearchText))
        {
            ErrorMessage = "Vui lòng nhập mã hoặc tên độc giả.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<ReaderListItemDto> result =
                    await _readerService.SearchAsync(
                        new ReaderSearchRequest(
                            Keyword: ReaderSearchText,
                            PageSize: 20),
                        token);
                SelectedReader = null;
                ReaderResults.Clear();
                foreach (ReaderListItemDto reader in result.Items)
                {
                    ReaderResults.Add(reader);
                }

                if (ReaderResults.Count == 0)
                {
                    ErrorMessage = "Không tìm thấy độc giả phù hợp.";
                }
            },
            "Đang tìm độc giả...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanCheckReader))]
    private Task CheckReaderEligibilityAsync(
        CancellationToken cancellationToken)
    {
        int readerId = SelectedReader!.Id;
        return ExecuteBusyAsync(
            async token =>
            {
                OperationResult result =
                    await _borrowService.ValidateReaderEligibilityAsync(
                        readerId,
                        token);
                IsReaderEligible = result.Succeeded;
                EligibilityMessage = result.Succeeded
                    ? "Độc giả đủ điều kiện mượn sách."
                    : result.ErrorMessage
                        ?? "Độc giả không đủ điều kiện mượn sách.";
            },
            "Đang kiểm tra điều kiện mượn...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanAddCopy))]
    private Task AddCopyAsync(CancellationToken cancellationToken)
    {
        string normalizedCode = CopyCode.Trim();
        if (normalizedCode.Length == 0)
        {
            ErrorMessage = "Vui lòng nhập mã bản sách.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<BookCopyDto> result =
                    await _bookCopyService.SearchAsync(
                        new BookCopySearchRequest(
                            Keyword: normalizedCode,
                            Status: BookCopyStatus.Available,
                            PageSize: 20),
                        token);
                BookCopyDto? copy = result.Items.FirstOrDefault(
                    item => string.Equals(
                        item.CopyCode,
                        normalizedCode,
                        StringComparison.OrdinalIgnoreCase));
                if (copy is null)
                {
                    ErrorMessage =
                        "Mã bản sách không tồn tại hoặc hiện không có sẵn.";
                    return;
                }

                if (SelectedCopies.Any(item => item.Id == copy.Id))
                {
                    ErrorMessage = "Bản sách đã có trong danh sách mượn.";
                    return;
                }

                if (SelectedCopies.Count >= MaximumBorrowedBooks)
                {
                    ErrorMessage =
                        $"Mỗi độc giả chỉ được mượn tối đa "
                        + $"{MaximumBorrowedBooks} bản sách.";
                    return;
                }

                SelectedCopies.Add(copy);
                CopyCode = string.Empty;
                OnPropertyChanged(nameof(SelectedCopySummary));
                ConfirmBorrowCommand.NotifyCanExecuteChanged();
            },
            "Đang thêm bản sách...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveCopy))]
    private void RemoveCopy()
    {
        SelectedCopies.Remove(SelectedCartCopy!);
        SelectedCartCopy = null;
        OnPropertyChanged(nameof(SelectedCopySummary));
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanConfirmBorrow))]
    private Task ConfirmBorrowAsync(CancellationToken cancellationToken)
    {
        if (!Validate())
        {
            ErrorMessage = "Vui lòng kiểm tra lại thông tin phiếu mượn.";
            return Task.CompletedTask;
        }

        ReaderListItemDto reader = SelectedReader!;
        int[] bookCopyIds = SelectedCopies
            .Select(copy => copy.Id)
            .ToArray();
        return ExecuteBusyAsync(
            async token =>
            {
                var request = new BorrowCreateRequest(
                    reader.Id,
                    bookCopyIds,
                    Notes);
                OperationResult validationResult =
                    await _borrowService.ValidateBorrowRequestAsync(
                        request,
                        token);
                if (!validationResult.Succeeded)
                {
                    IsReaderEligible = false;
                    EligibilityMessage = validationResult.ErrorMessage
                        ?? "Yêu cầu mượn sách không hợp lệ.";
                    ErrorMessage = validationResult.ErrorMessage;
                    return;
                }

                bool confirmed = await _dialogService.ConfirmAsync(
                    "Xác nhận lập phiếu mượn",
                    $"Lập phiếu mượn {bookCopyIds.Length} bản sách cho "
                    + $"“{reader.FullName}”?",
                    "Lập phiếu",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result =
                    await _borrowService.CreateBorrowSlipAsync(
                        request,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Lập phiếu thành công",
                    "Phiếu mượn đã được tạo và trạng thái bản sách đã cập nhật.",
                    NotificationSeverity.Success);
                ClearBorrowForm();
            },
            "Đang lập phiếu mượn...",
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể xử lý nghiệp vụ mượn sách.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý phiếu mượn. Vui lòng thử lại.";
    }

    partial void OnSelectedReaderChanged(ReaderListItemDto? value)
    {
        IsReaderEligible = false;
        EligibilityMessage = value is null
            ? "Chưa chọn độc giả."
            : "Vui lòng kiểm tra điều kiện mượn.";
        SelectedCopies.Clear();
        OnPropertyChanged(nameof(SelectedCopySummary));
        CheckReaderEligibilityCommand.NotifyCanExecuteChanged();
        AddCopyCommand.NotifyCanExecuteChanged();
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsReaderEligibleChanged(bool value)
    {
        AddCopyCommand.NotifyCanExecuteChanged();
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedCartCopyChanged(BookCopyDto? value)
    {
        RemoveCopyCommand.NotifyCanExecuteChanged();
    }

    private void ClearBorrowForm()
    {
        SelectedReader = null;
        ReaderResults.Clear();
        ReaderSearchText = string.Empty;
        SelectedCopies.Clear();
        SelectedCartCopy = null;
        CopyCode = string.Empty;
        Notes = null;
        IsReaderEligible = false;
        EligibilityMessage = "Chưa kiểm tra điều kiện mượn.";
        BorrowDate = DateTime.Today;
        ExpectedReturnDate = DateTime.Today.AddDays(DefaultBorrowDays);
        ClearValidation();
        ErrorMessage = null;
        OnPropertyChanged(nameof(SelectedCopySummary));
    }

    private bool CanCheckReader() => SelectedReader is not null;

    private bool CanAddCopy() =>
        SelectedReader is not null && IsReaderEligible;

    private bool CanRemoveCopy() => SelectedCartCopy is not null;

    private bool CanConfirmBorrow() =>
        SelectedReader is not null
        && IsReaderEligible
        && SelectedCopies.Count > 0;
}
