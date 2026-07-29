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

public sealed partial class FineViewModel : BaseViewModel
{
    private readonly IFineService _fineService;
    private readonly IAppDialogService _dialogService;
    private readonly IAppNotificationService _notificationService;
    private readonly ILogger<FineViewModel> _logger;

    public FineViewModel(
        IFineService fineService,
        IAppDialogService dialogService,
        IAppNotificationService notificationService,
        ILogger<FineViewModel> logger)
    {
        _fineService = fineService;
        _dialogService = dialogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public ObservableCollection<FineListItemDto> Fines { get; } = [];

    public ObservableCollection<FinePaymentDto> Payments { get; } = [];

    public IReadOnlyList<FineStatus?> StatusOptions { get; } =
        [null, .. Enum.GetValues<FineStatus>().Cast<FineStatus?>()];

    public IReadOnlyList<FineType?> FineTypeOptions { get; } =
        [null, .. Enum.GetValues<FineType>().Cast<FineType?>()];

    public IReadOnlyList<PaymentMethod> PaymentMethods { get; } =
        Enum.GetValues<PaymentMethod>();

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FineStatus? SelectedStatus { get; set; }

    [ObservableProperty]
    public partial FineType? SelectedFineType { get; set; }

    [ObservableProperty]
    public partial FineListItemDto? SelectedFine { get; set; }

    [ObservableProperty]
    public partial FineDetailDto? FineDetail { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(
        typeof(decimal),
        "0.01",
        "999999999999",
        ErrorMessage = "Số tiền thanh toán phải lớn hơn 0.")]
    public partial decimal PaymentAmount { get; set; }

    [ObservableProperty]
    public partial PaymentMethod SelectedPaymentMethod { get; set; } =
        PaymentMethod.Cash;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(
        1000,
        ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
    public partial string? PaymentNotes { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [MaxLength(
        500,
        ErrorMessage = "Lý do miễn phạt không được vượt quá 500 ký tự.")]
    public partial string WaiverReason { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PageNumber { get; set; } = 1;

    [ObservableProperty]
    public partial int PageSize { get; set; } = 20;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial int TotalPages { get; set; } = 1;

    public bool CanPaySelectedFine =>
        FineDetail is not null
        && FineDetail.Status is FineStatus.Unpaid or FineStatus.PartiallyPaid;

    public bool CanWaiveSelectedFine => CanPaySelectedFine;

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return LoadPageAsync(cancellationToken);
    }

    [RelayCommand]
    private Task SearchAsync(CancellationToken cancellationToken)
    {
        PageNumber = 1;
        return LoadPageAsync(cancellationToken);
    }

    [RelayCommand]
    private Task LoadSelectedFineAsync(CancellationToken cancellationToken)
    {
        int? fineId = SelectedFine?.Id;
        if (!fineId.HasValue)
        {
            FineDetail = null;
            Payments.Clear();
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            token => LoadDetailCoreAsync(fineId.Value, token),
            "Đang tải chi tiết tiền phạt...",
            cancellationToken);
    }

    [RelayCommand]
    private Task PayAsync(CancellationToken cancellationToken)
    {
        if (!CanPaySelectedFine)
        {
            ErrorMessage =
                "Vui lòng chọn khoản phạt chưa thanh toán.";
            return Task.CompletedTask;
        }

        if (PaymentAmount <= 0)
        {
            ErrorMessage = "Số tiền thanh toán phải lớn hơn 0.";
            return Task.CompletedTask;
        }

        if (PaymentNotes?.Trim().Length > 1000)
        {
            ErrorMessage =
                "Ghi chú không được vượt quá 1000 ký tự.";
            return Task.CompletedTask;
        }

        FineDetailDto detail = FineDetail!;
        if (PaymentAmount > detail.OutstandingAmount)
        {
            ErrorMessage =
                "Số tiền thanh toán không được lớn hơn số tiền còn lại.";
            return Task.CompletedTask;
        }

        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Xác nhận thanh toán",
                    $"Thu {PaymentAmount:N0} đ cho khoản phạt "
                    + $"{detail.FineCode}?",
                    "Thanh toán",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result = await _fineService.PayFineAsync(
                    new FinePaymentRequest(
                        detail.Id,
                        PaymentAmount,
                        SelectedPaymentMethod,
                        PaymentNotes),
                    token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Thanh toán thành công",
                    "Khoản phạt và lịch sử thanh toán đã được cập nhật.",
                    NotificationSeverity.Success);
                PaymentAmount = 0m;
                PaymentNotes = null;
                ClearValidation();
                await ReloadAfterMutationAsync(detail.Id, token);
            },
            "Đang thanh toán tiền phạt...",
            cancellationToken);
    }

    [RelayCommand]
    private Task WaiveAsync(CancellationToken cancellationToken)
    {
        if (!CanWaiveSelectedFine)
        {
            ErrorMessage =
                "Vui lòng chọn khoản phạt chưa được xử lý.";
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(WaiverReason))
        {
            ErrorMessage = "Vui lòng nhập lý do miễn phạt.";
            return Task.CompletedTask;
        }

        if (WaiverReason.Trim().Length > 500)
        {
            ErrorMessage =
                "Lý do miễn phạt không được vượt quá 500 ký tự.";
            return Task.CompletedTask;
        }

        FineDetailDto detail = FineDetail!;
        return ExecuteBusyAsync(
            async token =>
            {
                bool confirmed = await _dialogService.ConfirmAsync(
                    "Xác nhận miễn phạt",
                    $"Miễn số tiền còn lại {detail.OutstandingAmount:N0} đ "
                    + $"của khoản phạt {detail.FineCode}?",
                    "Miễn phạt",
                    "Hủy",
                    token);
                if (!confirmed)
                {
                    return;
                }

                OperationResult result = await _fineService.WaiveFineAsync(
                    new FineWaiveRequest(detail.Id, WaiverReason),
                    token);
                if (!result.Succeeded)
                {
                    ErrorMessage = result.ErrorMessage;
                    return;
                }

                _notificationService.Show(
                    "Đã miễn phạt",
                    "Trạng thái khoản phạt đã được cập nhật.",
                    NotificationSeverity.Success);
                WaiverReason = string.Empty;
                ClearValidation();
                await ReloadAfterMutationAsync(detail.Id, token);
            },
            "Đang miễn tiền phạt...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private Task PreviousPageAsync(CancellationToken cancellationToken)
    {
        PageNumber--;
        return LoadPageAsync(cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private Task NextPageAsync(CancellationToken cancellationToken)
    {
        PageNumber++;
        return LoadPageAsync(cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể xử lý tiền phạt.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể xử lý tiền phạt. Vui lòng thử lại.";
    }

    partial void OnFineDetailChanged(FineDetailDto? value)
    {
        OnPropertyChanged(nameof(CanPaySelectedFine));
        OnPropertyChanged(nameof(CanWaiveSelectedFine));
        PayCommand.NotifyCanExecuteChanged();
        WaiveCommand.NotifyCanExecuteChanged();
    }

    private Task LoadPageAsync(CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<FineListItemDto> result =
                    await _fineService.GetAllAsync(
                        new FineSearchRequest(
                            Keyword: SearchText,
                            FineType: SelectedFineType,
                            Status: SelectedStatus,
                            PageNumber: PageNumber,
                            PageSize: PageSize),
                        token);
                FineDetail = null;
                SelectedFine = null;
                Payments.Clear();
                Fines.Clear();
                foreach (FineListItemDto fine in result.Items)
                {
                    Fines.Add(fine);
                }

                TotalCount = result.TotalCount;
                PageNumber = result.PageNumber;
                TotalPages = result.TotalPages;
                NotifyPagingCommands();
            },
            "Đang tải danh sách tiền phạt...",
            cancellationToken);
    }

    private async Task LoadDetailCoreAsync(
        int fineId,
        CancellationToken cancellationToken)
    {
        FineDetail = await _fineService.GetByIdAsync(
            fineId,
            cancellationToken);
        Payments.Clear();
        if (FineDetail is not null)
        {
            foreach (FinePaymentDto payment in FineDetail.Payments)
            {
                Payments.Add(payment);
            }

            PaymentAmount = FineDetail.OutstandingAmount;
        }
    }

    private async Task ReloadAfterMutationAsync(
        int fineId,
        CancellationToken cancellationToken)
    {
        PagedResult<FineListItemDto> result =
            await _fineService.GetAllAsync(
                new FineSearchRequest(
                    Keyword: SearchText,
                    FineType: SelectedFineType,
                    Status: SelectedStatus,
                    PageNumber: PageNumber,
                    PageSize: PageSize),
                cancellationToken);
        Fines.Clear();
        foreach (FineListItemDto fine in result.Items)
        {
            Fines.Add(fine);
        }

        TotalCount = result.TotalCount;
        PageNumber = result.PageNumber;
        TotalPages = result.TotalPages;
        SelectedFine = Fines.FirstOrDefault(item => item.Id == fineId);
        await LoadDetailCoreAsync(fineId, cancellationToken);
        NotifyPagingCommands();
    }

    private bool CanGoPrevious() => PageNumber > 1;

    private bool CanGoNext() => PageNumber < TotalPages;

    private void NotifyPagingCommands()
    {
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }
}
