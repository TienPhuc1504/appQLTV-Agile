using System.Collections.ObjectModel;
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

public sealed partial class ReturnViewModel : BaseViewModel
{
    private readonly IReturnService _returnService;
    private readonly IBorrowService _borrowService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<ReturnViewModel> _logger;

    public ReturnViewModel(
        IReturnService returnService,
        IBorrowService borrowService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<ReturnViewModel> logger)
    {
        _returnService = returnService;
        _borrowService = borrowService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public ObservableCollection<ReturnLookupDto> SearchResults { get; } = [];

    public ObservableCollection<ReturnItemSelectionViewModel> ReturnItems { get; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ReturnLookupDto? SelectedBorrowSlip { get; set; }

    public decimal TotalEstimatedFine =>
        ReturnItems
            .Where(item => item.IsSelected)
            .Sum(item => item.EstimatedFineAmount);

    public int SelectedBookCount =>
        ReturnItems.Count(item => item.IsSelected);

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            ErrorMessage =
                "Vui lòng nhập mã phiếu mượn hoặc mã bản sách.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                IReadOnlyList<ReturnLookupDto> results =
                    await _returnService.SearchOutstandingAsync(
                        SearchText,
                        token);
                ApplySearchResults(results);

                if (SearchResults.Count == 0)
                {
                    ErrorMessage =
                        "Không tìm thấy sách đang mượn phù hợp.";
                }
                else if (SearchResults.Count == 1)
                {
                    SelectedBorrowSlip = SearchResults[0];
                }
            },
            "Đang tìm phiếu mượn...",
            cancellationToken);
    }

    [RelayCommand]
    private Task RenewSelectedAsync(CancellationToken cancellationToken)
    {
        ReturnItemSelectionViewModel[] selectedItems =
            ReturnItems.Where(item => item.IsSelected).ToArray();
        if (selectedItems.Length != 1)
        {
            ErrorMessage =
                "Vui lòng chọn đúng một bản sách cần gia hạn.";
            return Task.CompletedTask;
        }

        ReturnItemSelectionViewModel selectedItem = selectedItems[0];
        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Xác nhận gia hạn",
                    $"Gia hạn bản sách {selectedItem.Book.CopyCode} – "
                    + $"{selectedItem.Book.BookTitle}?",
                    "Gia hạn",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result =
                    await _borrowService.RenewBorrowedBookAsync(
                        selectedItem.Book.BorrowSlipDetailId,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Gia hạn thành công",
                    "Ngày hẹn trả và số lần gia hạn đã được cập nhật.",
                    NotificationSeverity.Success);
                IReadOnlyList<ReturnLookupDto> refreshedResults =
                    await _returnService.SearchOutstandingAsync(
                        SearchText,
                        token);
                ApplySearchResults(refreshedResults);
            },
            "Đang gia hạn sách...",
            cancellationToken);
    }

    [RelayCommand]
    private Task PreviewFinesAsync(CancellationToken cancellationToken)
    {
        ReturnItemSelectionViewModel[] selectedItems =
            ReturnItems.Where(item => item.IsSelected).ToArray();
        if (selectedItems.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất một bản sách cần trả.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            token => CalculatePreviewsAsync(selectedItems, token),
            "Đang tính tiền phạt dự kiến...",
            cancellationToken);
    }

    [RelayCommand]
    private Task ConfirmReturnAsync(CancellationToken cancellationToken)
    {
        ReturnItemSelectionViewModel[] selectedItems =
            ReturnItems.Where(item => item.IsSelected).ToArray();
        if (selectedItems.Length == 0)
        {
            ErrorMessage = "Vui lòng chọn ít nhất một bản sách cần trả.";
            return Task.CompletedTask;
        }

        ReturnLookupDto borrowSlip = SelectedBorrowSlip!;
        return ExecuteBusyAsync(
            async token =>
            {
                await CalculatePreviewsAsync(selectedItems, token);
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Xác nhận trả sách",
                    $"Xử lý trả {selectedItems.Length} bản sách của độc giả "
                    + $"“{borrowSlip.ReaderName}”? Tiền phạt dự kiến: "
                    + $"{TotalEstimatedFine:N0} đ.",
                    "Xác nhận trả",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                var request = new ReturnMultipleBooksRequest(
                    selectedItems
                        .Select(item => new ReturnBookRequest(
                            item.Book.BorrowSlipDetailId,
                            item.ReturnedCondition,
                            item.Notes))
                        .ToArray(),
                    DateOnly.FromDateTime(DateTime.Today));
                OperationResult result =
                    await _returnService.ReturnMultipleBooksAsync(
                        request,
                        token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Trả sách thành công",
                    "Dữ liệu trả sách, trạng thái bản sách và tiền phạt đã được cập nhật.",
                    NotificationSeverity.Success);
                ClearForm();
            },
            "Đang xử lý trả sách...",
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể xử lý nghiệp vụ trả sách.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : exception is Core.Validation.DomainValidationException
                ? exception.Message
                : "Không thể xử lý trả sách. Vui lòng thử lại.";
    }

    partial void OnSelectedBorrowSlipChanged(ReturnLookupDto? value)
    {
        ClearReturnItems();
        if (value is not null)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            foreach (ReturnableBookDto book in value.Books)
            {
                var item = new ReturnItemSelectionViewModel(
                    book,
                    Math.Max(
                        0,
                        today.DayNumber - book.ExpectedReturnDate.DayNumber));
                item.PropertyChanged += OnReturnItemPropertyChanged;
                ReturnItems.Add(item);
            }
        }

        NotifySummaryChanged();
    }

    private async Task CalculatePreviewsAsync(
        IReadOnlyCollection<ReturnItemSelectionViewModel> selectedItems,
        CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        Task<ReturnPreviewDto>[] tasks = selectedItems
            .Select(item => _returnService.CalculateFineAsync(
                item.Book.BorrowSlipDetailId,
                item.ReturnedCondition,
                today,
                cancellationToken))
            .ToArray();
        ReturnPreviewDto[] previews = await Task.WhenAll(tasks);
        Dictionary<int, ReturnPreviewDto> previewByDetailId =
            previews.ToDictionary(preview => preview.BorrowSlipDetailId);
        foreach (ReturnItemSelectionViewModel item in selectedItems)
        {
            ReturnPreviewDto preview =
                previewByDetailId[item.Book.BorrowSlipDetailId];
            item.OverdueDays = preview.OverdueDays;
            item.OverdueFineAmount = preview.OverdueFineAmount;
            item.ConditionFineAmount = preview.ConditionFineAmount;
        }

        NotifySummaryChanged();
    }

    private void ClearForm()
    {
        SearchText = string.Empty;
        SelectedBorrowSlip = null;
        SearchResults.Clear();
        ClearReturnItems();
        ErrorMessage = null;
        NotifySummaryChanged();
    }

    private void ApplySearchResults(
        IReadOnlyList<ReturnLookupDto> results)
    {
        SelectedBorrowSlip = null;
        SearchResults.Clear();
        foreach (ReturnLookupDto result in results)
        {
            SearchResults.Add(result);
        }

        if (SearchResults.Count == 1)
        {
            SelectedBorrowSlip = SearchResults[0];
        }
    }

    private void OnReturnItemPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is
            nameof(ReturnItemSelectionViewModel.IsSelected)
            or nameof(ReturnItemSelectionViewModel.EstimatedFineAmount))
        {
            NotifySummaryChanged();
        }
    }

    private void ClearReturnItems()
    {
        foreach (ReturnItemSelectionViewModel item in ReturnItems)
        {
            item.PropertyChanged -= OnReturnItemPropertyChanged;
        }

        ReturnItems.Clear();
    }

    private void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(TotalEstimatedFine));
        OnPropertyChanged(nameof(SelectedBookCount));
    }
}

public sealed partial class ReturnItemSelectionViewModel : ObservableObject
{
    private static readonly IReadOnlyList<PhysicalCondition> Conditions =
    [
        PhysicalCondition.New,
        PhysicalCondition.Good,
        PhysicalCondition.Worn,
        PhysicalCondition.Damaged,
        PhysicalCondition.Lost
    ];

    public ReturnItemSelectionViewModel(
        ReturnableBookDto book,
        int overdueDays)
    {
        Book = book;
        OverdueDays = overdueDays;
    }

    public ReturnableBookDto Book { get; }

    public IReadOnlyList<PhysicalCondition> AvailableConditions => Conditions;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial PhysicalCondition ReturnedCondition { get; set; } =
        PhysicalCondition.Good;

    [ObservableProperty]
    public partial string? Notes { get; set; }

    [ObservableProperty]
    public partial int OverdueDays { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedFineAmount))]
    public partial decimal OverdueFineAmount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstimatedFineAmount))]
    public partial decimal ConditionFineAmount { get; set; }

    public decimal EstimatedFineAmount =>
        OverdueFineAmount + ConditionFineAmount;

    partial void OnReturnedConditionChanged(PhysicalCondition value)
    {
        ConditionFineAmount = 0m;
    }
}
