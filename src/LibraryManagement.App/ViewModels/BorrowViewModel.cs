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
    private const int SelectionPageSize = 100;

    private readonly IReaderService _readerService;
    private readonly IBookCopyService _bookCopyService;
    private readonly IBorrowService _borrowService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<BorrowViewModel> _logger;
    private int _readerSelectionVersion;

    public Guid InstanceId { get; } = Guid.NewGuid();

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
        ErrorsChanged += (_, _) =>
        {
            ConfirmBorrowCommand.NotifyCanExecuteChanged();
            LogCommandState("ValidationChanged");
        };
        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(IsBusy))
            {
                AddCopyCommand.NotifyCanExecuteChanged();
                ConfirmBorrowCommand.NotifyCanExecuteChanged();
                LogCommandState("IsBusyChanged");
            }
        };
        _logger.LogDebug(
            "BorrowViewModel được tạo. InstanceId={InstanceId}.",
            InstanceId);
    }

    public ObservableCollection<ReaderListItemDto> ReaderResults { get; } = [];

    public ObservableCollection<BookCopyDto> AvailableCopies { get; } = [];

    public ObservableCollection<BookCopyDto> SelectedCopies { get; } = [];

    [ObservableProperty]
    public partial string ReaderSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ReaderListItemDto? SelectedReader { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCopyCommand))]
    public partial BookCopyDto? SelectedAvailableCopy { get; set; }

    [ObservableProperty]
    public partial bool IsReaderEligible { get; set; }

    [ObservableProperty]
    public partial string EligibilityMessage { get; set; } =
        "Chưa kiểm tra điều kiện mượn.";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCopyCommand))]
    [NotifyDataErrorInfo]
    [MaxLength(300, ErrorMessage = "Từ khóa bản sách không được vượt quá 300 ký tự.")]
    public partial string CopyCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BookCopyDto? SelectedCartCopy { get; set; }

    [ObservableProperty]
    public partial string ReaderEmptyMessage { get; set; } = "Chưa có độc giả.";

    [ObservableProperty]
    public partial string AvailableCopyEmptyMessage { get; set; } =
        "Chưa có bản sách khả dụng.";

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(ConfirmBorrowCommand))]
    [MaxLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmBorrowCommand))]
    public partial DateTime BorrowDate { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmBorrowCommand))]
    public partial DateTime ExpectedReturnDate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingBorrowCapacity))]
    [NotifyPropertyChangedFor(nameof(SelectedCopySummary))]
    [NotifyCanExecuteChangedFor(nameof(AddCopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmBorrowCommand))]
    public partial int MaximumBorrowedBooks { get; set; }

    [ObservableProperty]
    public partial int DefaultBorrowDays { get; set; }

    [ObservableProperty]
    public partial decimal MaximumOutstandingFineAmount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingBorrowCapacity))]
    [NotifyPropertyChangedFor(nameof(SelectedCopySummary))]
    [NotifyCanExecuteChangedFor(nameof(AddCopyCommand))]
    [NotifyCanExecuteChangedFor(nameof(ConfirmBorrowCommand))]
    public partial int ActiveBorrowedCopyCount { get; set; }

    public int RemainingBorrowCapacity =>
        Math.Max(0, MaximumBorrowedBooks - ActiveBorrowedCopyCount);

    public string SelectedCopySummary =>
        $"{SelectedCopies.Count}/{RemainingBorrowCapacity} bản sách trong phiếu";

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                Task<BorrowPolicyDto> policyTask =
                    _borrowService.GetBorrowPolicyAsync(token);
                Task<PagedResult<ReaderListItemDto>> readerTask =
                    _readerService.SearchAsync(
                        new ReaderSearchRequest(PageSize: SelectionPageSize),
                        token);
                Task<PagedResult<BookCopyDto>> copyTask =
                    _bookCopyService.SearchAsync(
                        new BookCopySearchRequest(
                            Status: BookCopyStatus.Available,
                            PageSize: SelectionPageSize),
                        token);

                await Task.WhenAll(policyTask, readerTask, copyTask);

                BorrowPolicyDto policy = await policyTask;
                MaximumBorrowedBooks = policy.MaximumBorrowedBooks;
                DefaultBorrowDays = policy.DefaultBorrowDays;
                MaximumOutstandingFineAmount =
                    policy.MaximumOutstandingFineAmount;
                BorrowDate = DateTime.Today;
                ExpectedReturnDate =
                    DateTime.Today.AddDays(policy.DefaultBorrowDays);
                ReplaceReaderResults(
                    (await readerTask).Items,
                    keyword: null);
                ReplaceAvailableCopies(
                    (await copyTask).Items,
                    keyword: null);
                OnPropertyChanged(nameof(SelectedCopySummary));
                _logger.LogDebug(
                    "Đã tải chính sách mượn. InstanceId={InstanceId}, "
                    + "MaximumBorrowedBooks={MaximumBorrowedBooks}, "
                    + "DefaultBorrowDays={DefaultBorrowDays}, "
                    + "MaximumOutstandingFineAmount={MaximumOutstandingFineAmount}.",
                    InstanceId,
                    MaximumBorrowedBooks,
                    DefaultBorrowDays,
                    MaximumOutstandingFineAmount);
                LogCommandState("PolicyLoaded");
            },
            "Đang tải chính sách mượn sách...",
            cancellationToken);
    }

    [RelayCommand]
    private async Task SearchReaderAsync(CancellationToken cancellationToken)
    {
        string keyword = ReaderSearchText.Trim();
        _logger.LogDebug(
            "Bắt đầu tìm độc giả. InstanceId={InstanceId}, Keyword={Keyword}.",
            InstanceId,
            keyword);
        await ExecuteBusyAsync(
            async token =>
            {
                PagedResult<ReaderListItemDto> result =
                    await _readerService.SearchAsync(
                        new ReaderSearchRequest(
                            Keyword: keyword.Length == 0 ? null : keyword,
                            PageSize: SelectionPageSize),
                        token);
                ReplaceReaderResults(
                    result.Items,
                    keyword);

                _logger.LogDebug(
                    "Hoàn tất tìm độc giả. InstanceId={InstanceId}, "
                    + "Keyword={Keyword}, ResultCount={ResultCount}.",
                    InstanceId,
                    keyword,
                    ReaderResults.Count);
            },
            "Đang tìm độc giả...",
            cancellationToken);

        if (!HasError && keyword.Length > 0 && ReaderResults.Count == 1)
        {
            SelectedReader = ReaderResults[0];
        }
    }

    [RelayCommand]
    private Task SearchBookCopiesAsync(CancellationToken cancellationToken)
    {
        string keyword = CopyCode.Trim();
        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<BookCopyDto> result =
                    await _bookCopyService.SearchAsync(
                        new BookCopySearchRequest(
                            Keyword: keyword.Length == 0 ? null : keyword,
                            Status: BookCopyStatus.Available,
                            PageSize: SelectionPageSize),
                        token);
                ReplaceAvailableCopies(result.Items, keyword);
            },
            "Đang lọc bản sách có sẵn...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanCheckReader))]
    private Task CheckReaderEligibilityAsync(
        CancellationToken cancellationToken)
    {
        int selectionVersion = _readerSelectionVersion;
        int readerId = SelectedReader!.Id;
        return ValidateSelectedReaderAsync(
            readerId,
            selectionVersion,
            cancellationToken);
    }

    private Task ValidateSelectedReaderAsync(
        int readerId,
        int selectionVersion,
        CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                OperationResult result =
                    await _borrowService.ValidateReaderEligibilityAsync(
                        readerId,
                        token);
                IReadOnlyList<BorrowSlipDetailDto> activeBorrows =
                    result.Succeeded
                        ? await _borrowService.GetReaderActiveBorrowsAsync(
                            readerId,
                            token)
                        : [];
                if (selectionVersion != _readerSelectionVersion
                    || SelectedReader?.Id != readerId)
                {
                    return;
                }

                ActiveBorrowedCopyCount = activeBorrows.Count;
                IsReaderEligible = result.Succeeded;
                EligibilityMessage = result.Succeeded
                    ? $"Độc giả đủ điều kiện, có thể mượn thêm "
                        + $"{RemainingBorrowCapacity} bản sách."
                    : result.ErrorMessage
                        ?? "Độc giả không đủ điều kiện mượn sách.";
                _logger.LogDebug(
                    "Hoàn tất eligibility. InstanceId={InstanceId}, "
                    + "ReaderId={ReaderId}, Succeeded={Succeeded}, "
                    + "ActiveBorrowedCopyCount={ActiveBorrowedCopyCount}, "
                    + "RemainingBorrowCapacity={RemainingBorrowCapacity}, "
                    + "Message={Message}.",
                    InstanceId,
                    readerId,
                    result.Succeeded,
                    ActiveBorrowedCopyCount,
                    RemainingBorrowCapacity,
                    EligibilityMessage);
                LogCommandState("EligibilityCompleted");
            },
            "Đang kiểm tra điều kiện mượn...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanAddCopy))]
    private Task AddCopyAsync(CancellationToken cancellationToken)
    {
        string normalizedCode = CopyCode.Trim();
        BookCopyDto? selectedCopy = SelectedAvailableCopy;
        if (selectedCopy is null && normalizedCode.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn hoặc nhập mã bản sách.";
            return Task.CompletedTask;
        }

        _logger.LogDebug(
            "Bắt đầu tìm bản sách. InstanceId={InstanceId}, CopyCode={CopyCode}.",
            InstanceId,
            normalizedCode);
        return ExecuteBusyAsync(
            async token =>
            {
                BookCopyDto? copy = selectedCopy;
                if (copy is null)
                {
                    PagedResult<BookCopyDto> result =
                        await _bookCopyService.SearchAsync(
                            new BookCopySearchRequest(
                                Keyword: normalizedCode,
                                Status: BookCopyStatus.Available,
                                PageSize: SelectionPageSize),
                            token);
                    copy = result.Items.FirstOrDefault(
                        item => string.Equals(
                            item.CopyCode,
                            normalizedCode,
                            StringComparison.OrdinalIgnoreCase));
                }

                if (copy is null)
                {
                    ErrorMessage =
                        "Mã bản sách không tồn tại hoặc hiện không có sẵn.";
                    return;
                }

                if (copy.Status != BookCopyStatus.Available)
                {
                    ErrorMessage =
                        $"Bản sách {copy.CopyCode} hiện không có sẵn.";
                    return;
                }

                if (SelectedCopies.Any(item => item.Id == copy.Id))
                {
                    ErrorMessage = "Bản sách đã có trong danh sách mượn.";
                    return;
                }

                if (SelectedCopies.Count >= RemainingBorrowCapacity)
                {
                    ErrorMessage =
                        "Độc giả đã đạt số lượng bản sách có thể mượn thêm.";
                    return;
                }

                SelectedCopies.Add(copy);
                AvailableCopies.Remove(copy);
                SelectedAvailableCopy = null;
                CopyCode = string.Empty;
                OnPropertyChanged(nameof(SelectedCopySummary));
                ConfirmBorrowCommand.NotifyCanExecuteChanged();
                _logger.LogDebug(
                    "Đã thêm bản sách vào phiếu. InstanceId={InstanceId}, "
                    + "CopyCode={CopyCode}, SelectedCount={SelectedCount}.",
                    InstanceId,
                    copy.CopyCode,
                    SelectedCopies.Count);
                LogCommandState("CopyAdded");
            },
            "Đang thêm bản sách...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanRemoveCopy))]
    private void RemoveCopy()
    {
        BookCopyDto copy = SelectedCartCopy!;
        SelectedCopies.Remove(copy);
        SelectedCartCopy = null;
        RestoreAvailableCopy(copy);
        OnPropertyChanged(nameof(SelectedCopySummary));
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
        LogCommandState("CopyRemoved");
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
                await ReloadSelectionListsAsync(token);
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
        int selectionVersion = ++_readerSelectionVersion;
        IsReaderEligible = false;
        ActiveBorrowedCopyCount = 0;
        EligibilityMessage = value is null
            ? "Chưa chọn độc giả."
            : "Đang kiểm tra điều kiện mượn...";
        RestoreAllSelectedCopies();
        OnPropertyChanged(nameof(SelectedCopySummary));
        CheckReaderEligibilityCommand.NotifyCanExecuteChanged();
        AddCopyCommand.NotifyCanExecuteChanged();
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
        _logger.LogDebug(
            "Độc giả được chọn thay đổi. InstanceId={InstanceId}, "
            + "ReaderId={ReaderId}, ReaderCode={ReaderCode}, "
            + "SelectionVersion={SelectionVersion}.",
            InstanceId,
            value?.Id,
            value?.ReaderCode,
            selectionVersion);
        LogCommandState("SelectedReaderChanged");

        if (value is not null)
        {
            _ = ValidateSelectedReaderAsync(
                value.Id,
                selectionVersion,
                CancellationToken.None);
        }
    }

    partial void OnIsReaderEligibleChanged(bool value)
    {
        AddCopyCommand.NotifyCanExecuteChanged();
        ConfirmBorrowCommand.NotifyCanExecuteChanged();
        LogCommandState("IsReaderEligibleChanged");
    }

    partial void OnReaderSearchTextChanged(string value)
    {
        _logger.LogDebug(
            "Từ khóa độc giả thay đổi. InstanceId={InstanceId}, Keyword={Keyword}.",
            InstanceId,
            value);
    }

    partial void OnCopyCodeChanged(string value)
    {
        LogCommandState("CopyCodeChanged");
    }

    partial void OnBorrowDateChanged(DateTime value)
    {
        LogCommandState("BorrowDateChanged");
    }

    partial void OnExpectedReturnDateChanged(DateTime value)
    {
        LogCommandState("ExpectedReturnDateChanged");
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
        AvailableCopies.Clear();
        SelectedAvailableCopy = null;
        SelectedCartCopy = null;
        CopyCode = string.Empty;
        Notes = null;
        IsReaderEligible = false;
        ActiveBorrowedCopyCount = 0;
        EligibilityMessage = "Chưa kiểm tra điều kiện mượn.";
        BorrowDate = DateTime.Today;
        ExpectedReturnDate = DateTime.Today.AddDays(DefaultBorrowDays);
        ClearValidation();
        ErrorMessage = null;
        OnPropertyChanged(nameof(SelectedCopySummary));
    }

    private bool CanCheckReader() => SelectedReader is not null;

    private bool CanAddCopy() =>
        SelectedReader is not null
        && IsReaderEligible
        && !IsBusy
        && (SelectedAvailableCopy is not null
            || !string.IsNullOrWhiteSpace(CopyCode))
        && SelectedCopies.Count < RemainingBorrowCapacity;

    private bool CanRemoveCopy() => SelectedCartCopy is not null;

    private bool CanConfirmBorrow() =>
        SelectedReader is not null
        && IsReaderEligible
        && !IsBusy
        && SelectedCopies.Count > 0
        && SelectedCopies.Count <= RemainingBorrowCapacity
        && SelectedCopies.All(
            copy => copy.Status == BookCopyStatus.Available)
        && ExpectedReturnDate.Date > BorrowDate.Date
        && !HasErrors;

    private void ReplaceReaderResults(
        IReadOnlyList<ReaderListItemDto> readers,
        string? keyword)
    {
        int? selectedReaderId = SelectedReader?.Id;
        ReaderResults.Clear();
        foreach (ReaderListItemDto reader in readers)
        {
            ReaderResults.Add(reader);
        }

        ReaderEmptyMessage = string.IsNullOrWhiteSpace(keyword)
            ? "Chưa có độc giả."
            : "Không tìm thấy độc giả phù hợp.";
        ReaderListItemDto? retainedSelection = selectedReaderId.HasValue
            ? ReaderResults.FirstOrDefault(
                reader => reader.Id == selectedReaderId.Value)
            : null;
        SelectedReader = retainedSelection;
    }

    private void ReplaceAvailableCopies(
        IReadOnlyList<BookCopyDto> copies,
        string? keyword)
    {
        HashSet<int> selectedCopyIds =
            SelectedCopies.Select(copy => copy.Id).ToHashSet();
        AvailableCopies.Clear();
        foreach (BookCopyDto copy in copies.Where(
                     copy =>
                         copy.Status == BookCopyStatus.Available
                         && !selectedCopyIds.Contains(copy.Id)))
        {
            AvailableCopies.Add(copy);
        }

        SelectedAvailableCopy = null;
        AvailableCopyEmptyMessage = string.IsNullOrWhiteSpace(keyword)
            ? "Chưa có bản sách khả dụng."
            : "Không tìm thấy bản sách phù hợp.";
    }

    private async Task ReloadSelectionListsAsync(CancellationToken token)
    {
        PagedResult<ReaderListItemDto> readers =
            await _readerService.SearchAsync(
                new ReaderSearchRequest(PageSize: SelectionPageSize),
                token);
        PagedResult<BookCopyDto> copies =
            await _bookCopyService.SearchAsync(
                new BookCopySearchRequest(
                    Status: BookCopyStatus.Available,
                    PageSize: SelectionPageSize),
                token);
        ReplaceReaderResults(
            readers.Items,
            keyword: null);
        ReplaceAvailableCopies(copies.Items, keyword: null);
    }

    private void RestoreAllSelectedCopies()
    {
        foreach (BookCopyDto copy in SelectedCopies.ToArray())
        {
            RestoreAvailableCopy(copy);
        }

        SelectedCopies.Clear();
        SelectedCartCopy = null;
    }

    private void RestoreAvailableCopy(BookCopyDto copy)
    {
        if (copy.Status != BookCopyStatus.Available
            || AvailableCopies.Any(item => item.Id == copy.Id)
            || !MatchesCopyFilter(copy, CopyCode))
        {
            return;
        }

        int insertIndex = 0;
        while (insertIndex < AvailableCopies.Count
               && string.Compare(
                   AvailableCopies[insertIndex].CopyCode,
                   copy.CopyCode,
                   StringComparison.CurrentCultureIgnoreCase) < 0)
        {
            insertIndex++;
        }

        AvailableCopies.Insert(insertIndex, copy);
    }

    private static bool MatchesCopyFilter(BookCopyDto copy, string keyword)
    {
        string normalizedKeyword = keyword.Trim();
        return normalizedKeyword.Length == 0
            || copy.CopyCode.Contains(
                normalizedKeyword,
                StringComparison.CurrentCultureIgnoreCase)
            || copy.BookTitle.Contains(
                normalizedKeyword,
                StringComparison.CurrentCultureIgnoreCase)
            || (copy.ShelfLocation?.Contains(
                normalizedKeyword,
                StringComparison.CurrentCultureIgnoreCase) ?? false);
    }

    private void LogCommandState(string reason)
    {
        bool allCopiesAvailable = SelectedCopies.All(
            copy => copy.Status == BookCopyStatus.Available);
        bool borrowDateValid = ExpectedReturnDate.Date > BorrowDate.Date;
        _logger.LogDebug(
            "Trạng thái command mượn. InstanceId={InstanceId}, "
            + "Reason={Reason}, HasSelectedReader={HasSelectedReader}, "
            + "IsReaderEligible={IsReaderEligible}, "
            + "ReaderSearchText={ReaderSearchText}, CopyCode={CopyCode}, "
            + "SelectedCount={SelectedCount}, "
            + "RemainingBorrowCapacity={RemainingBorrowCapacity}, "
            + "AreAllSelectedCopiesAvailable={AreAllSelectedCopiesAvailable}, "
            + "IsBorrowDateValid={IsBorrowDateValid}, "
            + "HasValidationErrors={HasValidationErrors}, IsBusy={IsBusy}, "
            + "CanAddCopy={CanAddCopy}, CanConfirmBorrow={CanConfirmBorrow}.",
            InstanceId,
            reason,
            SelectedReader is not null,
            IsReaderEligible,
            ReaderSearchText,
            CopyCode,
            SelectedCopies.Count,
            RemainingBorrowCapacity,
            allCopiesAvailable,
            borrowDateValid,
            HasErrors,
            IsBusy,
            CanAddCopy(),
            CanConfirmBorrow());
    }
}
