using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibraryManagement.App.Navigation;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.App.ViewModels;

public sealed partial class DashboardViewModel :
    BaseViewModel,
    IRefreshableViewModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<DashboardViewModel> _logger;

    public DashboardViewModel(
        IDashboardService dashboardService,
        IAuthenticationService authenticationService,
        ILogger<DashboardViewModel> logger)
    {
        _dashboardService = dashboardService;
        _authenticationService = authenticationService;
        _logger = logger;
    }

    IAsyncRelayCommand IRefreshableViewModel.RefreshCommand => LoadCommand;

    [ObservableProperty]
    public partial DashboardSummaryDto Summary { get; private set; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0m);

    [ObservableProperty]
    public partial int MaxMonthlyBorrowCount { get; private set; } = 1;

    public ObservableCollection<MonthlyBorrowStatisticDto>
        MonthlyBorrowStatistics
    { get; } = [];

    public ObservableCollection<MostBorrowedBookDto>
        MostBorrowedBooks
    { get; } = [];

    public ObservableCollection<MostBorrowedCategoryDto>
        MostBorrowedCategories
    { get; } = [];

    public ObservableCollection<RecentActivityDto> RecentActivities { get; } =
        [];

    public ObservableCollection<BorrowedBookReportItemDto>
        BorrowedBooks
    { get; } = [];

    public ObservableCollection<OverdueBookReportItemDto>
        OverdueBooks
    { get; } = [];

    public ObservableCollection<OutstandingFineReportItemDto>
        OutstandingFines
    { get; } = [];

    public bool HasMonthlyStatistics => MonthlyBorrowStatistics.Count > 0;

    public bool HasMostBorrowedBooks => MostBorrowedBooks.Count > 0;

    public bool HasNoMostBorrowedBooks => !HasMostBorrowedBooks;

    public bool HasMostBorrowedCategories => MostBorrowedCategories.Count > 0;

    public bool HasNoMostBorrowedCategories => !HasMostBorrowedCategories;

    public bool HasRecentActivities => RecentActivities.Count > 0;

    public bool HasNoRecentActivities => !HasRecentActivities;

    public bool HasNoBorrowedBooks => BorrowedBooks.Count == 0;

    public bool HasNoOverdueBooks => OverdueBooks.Count == 0;

    public bool HasNoOutstandingFines => OutstandingFines.Count == 0;

    public bool CanViewRecentActivities =>
        _authenticationService.CheckPermission(Permission.ViewActivityLogs);

    [ObservableProperty]
    public partial int BorrowedPageNumber { get; private set; } = 1;

    [ObservableProperty]
    public partial int BorrowedTotalPages { get; private set; } = 1;

    [ObservableProperty]
    public partial int BorrowedTotalCount { get; private set; }

    [ObservableProperty]
    public partial int OverduePageNumber { get; private set; } = 1;

    [ObservableProperty]
    public partial int OverdueTotalPages { get; private set; } = 1;

    [ObservableProperty]
    public partial int OverdueTotalCount { get; private set; }

    [ObservableProperty]
    public partial int FinePageNumber { get; private set; } = 1;

    [ObservableProperty]
    public partial int FineTotalPages { get; private set; } = 1;

    [ObservableProperty]
    public partial int FineTotalCount { get; private set; }

    [RelayCommand]
    private Task LoadAsync(CancellationToken cancellationToken)
    {
        BorrowedPageNumber = 1;
        OverduePageNumber = 1;
        FinePageNumber = 1;
        return ExecuteBusyAsync(
            LoadDashboardCoreAsync,
            "Đang tải dữ liệu tổng quan...",
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousBorrowedPage))]
    private Task PreviousBorrowedPageAsync(
        CancellationToken cancellationToken)
    {
        return LoadBorrowedReportAsync(
            BorrowedPageNumber - 1,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextBorrowedPage))]
    private Task NextBorrowedPageAsync(CancellationToken cancellationToken)
    {
        return LoadBorrowedReportAsync(
            BorrowedPageNumber + 1,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousOverduePage))]
    private Task PreviousOverduePageAsync(
        CancellationToken cancellationToken)
    {
        return LoadOverdueReportAsync(
            OverduePageNumber - 1,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextOverduePage))]
    private Task NextOverduePageAsync(CancellationToken cancellationToken)
    {
        return LoadOverdueReportAsync(
            OverduePageNumber + 1,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPreviousFinePage))]
    private Task PreviousFinePageAsync(CancellationToken cancellationToken)
    {
        return LoadFineReportAsync(
            FinePageNumber - 1,
            cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextFinePage))]
    private Task NextFinePageAsync(CancellationToken cancellationToken)
    {
        return LoadFineReportAsync(
            FinePageNumber + 1,
            cancellationToken);
    }

    protected override string GetFriendlyErrorMessage(Exception exception)
    {
        _logger.LogError(exception, "Không thể tải dữ liệu Dashboard.");
        return exception is UnauthorizedAccessException
            ? exception.Message
            : "Không thể tải dữ liệu tổng quan. Vui lòng thử lại.";
    }

    private async Task LoadDashboardCoreAsync(
        CancellationToken cancellationToken)
    {
        Task<DashboardSummaryDto> summaryTask =
            _dashboardService.GetDashboardSummaryAsync(cancellationToken);
        Task<IReadOnlyList<MonthlyBorrowStatisticDto>> monthlyTask =
            _dashboardService.GetMonthlyBorrowStatisticsAsync(
                12,
                cancellationToken);
        Task<IReadOnlyList<MostBorrowedBookDto>> booksTask =
            _dashboardService.GetMostBorrowedBooksAsync(
                5,
                cancellationToken);
        Task<IReadOnlyList<MostBorrowedCategoryDto>> categoriesTask =
            _dashboardService.GetMostBorrowedCategoriesAsync(
                5,
                cancellationToken);
        Task<IReadOnlyList<RecentActivityDto>> activitiesTask =
            CanViewRecentActivities
                ? _dashboardService.GetRecentActivitiesAsync(
                    10,
                    cancellationToken)
                : Task.FromResult<IReadOnlyList<RecentActivityDto>>([]);
        Task<PagedResult<BorrowedBookReportItemDto>> borrowedBooksTask =
            _dashboardService.GetBorrowedBooksReportAsync(
                BorrowedPageNumber,
                ReportPageSize,
                cancellationToken);
        Task<PagedResult<OverdueBookReportItemDto>> overdueBooksTask =
            _dashboardService.GetOverdueBooksReportAsync(
                OverduePageNumber,
                ReportPageSize,
                cancellationToken);
        Task<PagedResult<OutstandingFineReportItemDto>> finesTask =
            _dashboardService.GetOutstandingFinesReportAsync(
                FinePageNumber,
                ReportPageSize,
                cancellationToken);

        await Task.WhenAll(
            summaryTask,
            monthlyTask,
            booksTask,
            categoriesTask,
            activitiesTask,
            borrowedBooksTask,
            overdueBooksTask,
            finesTask);

        Summary = await summaryTask;
        ReplaceItems(
            MonthlyBorrowStatistics,
            await monthlyTask);
        ReplaceItems(MostBorrowedBooks, await booksTask);
        ReplaceItems(MostBorrowedCategories, await categoriesTask);
        ReplaceItems(RecentActivities, await activitiesTask);
        ApplyBorrowedPage(await borrowedBooksTask);
        ApplyOverduePage(await overdueBooksTask);
        ApplyFinePage(await finesTask);

        MaxMonthlyBorrowCount = Math.Max(
            1,
            MonthlyBorrowStatistics
                .Select(item => item.BorrowCount)
                .DefaultIfEmpty()
                .Max());
        NotifyCollectionState();
    }

    private Task LoadBorrowedReportAsync(
        int requestedPageNumber,
        CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<BorrowedBookReportItemDto> result =
                    await _dashboardService.GetBorrowedBooksReportAsync(
                        requestedPageNumber,
                        ReportPageSize,
                        token);
                ApplyBorrowedPage(result);
                NotifyCollectionState();
            },
            "Đang tải báo cáo sách đang mượn...",
            cancellationToken);
    }

    private Task LoadOverdueReportAsync(
        int requestedPageNumber,
        CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<OverdueBookReportItemDto> result =
                    await _dashboardService.GetOverdueBooksReportAsync(
                        requestedPageNumber,
                        ReportPageSize,
                        token);
                ApplyOverduePage(result);
                NotifyCollectionState();
            },
            "Đang tải báo cáo sách quá hạn...",
            cancellationToken);
    }

    private Task LoadFineReportAsync(
        int requestedPageNumber,
        CancellationToken cancellationToken)
    {
        return ExecuteBusyAsync(
            async token =>
            {
                PagedResult<OutstandingFineReportItemDto> result =
                    await _dashboardService.GetOutstandingFinesReportAsync(
                        requestedPageNumber,
                        ReportPageSize,
                        token);
                ApplyFinePage(result);
                NotifyCollectionState();
            },
            "Đang tải báo cáo tiền phạt...",
            cancellationToken);
    }

    private void ApplyBorrowedPage(
        PagedResult<BorrowedBookReportItemDto> result)
    {
        ReplaceItems(BorrowedBooks, result.Items);
        BorrowedPageNumber = result.PageNumber;
        BorrowedTotalPages = result.TotalPages;
        BorrowedTotalCount = result.TotalCount;
        PreviousBorrowedPageCommand.NotifyCanExecuteChanged();
        NextBorrowedPageCommand.NotifyCanExecuteChanged();
    }

    private void ApplyOverduePage(
        PagedResult<OverdueBookReportItemDto> result)
    {
        ReplaceItems(OverdueBooks, result.Items);
        OverduePageNumber = result.PageNumber;
        OverdueTotalPages = result.TotalPages;
        OverdueTotalCount = result.TotalCount;
        PreviousOverduePageCommand.NotifyCanExecuteChanged();
        NextOverduePageCommand.NotifyCanExecuteChanged();
    }

    private void ApplyFinePage(
        PagedResult<OutstandingFineReportItemDto> result)
    {
        ReplaceItems(OutstandingFines, result.Items);
        FinePageNumber = result.PageNumber;
        FineTotalPages = result.TotalPages;
        FineTotalCount = result.TotalCount;
        PreviousFinePageCommand.NotifyCanExecuteChanged();
        NextFinePageCommand.NotifyCanExecuteChanged();
    }

    private bool CanGoToPreviousBorrowedPage() => BorrowedPageNumber > 1;

    private bool CanGoToNextBorrowedPage() =>
        BorrowedPageNumber < BorrowedTotalPages;

    private bool CanGoToPreviousOverduePage() => OverduePageNumber > 1;

    private bool CanGoToNextOverduePage() =>
        OverduePageNumber < OverdueTotalPages;

    private bool CanGoToPreviousFinePage() => FinePageNumber > 1;

    private bool CanGoToNextFinePage() => FinePageNumber < FineTotalPages;

    private const int ReportPageSize = 20;

    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();
        foreach (T item in source)
        {
            target.Add(item);
        }
    }

    private void NotifyCollectionState()
    {
        OnPropertyChanged(nameof(HasMonthlyStatistics));
        OnPropertyChanged(nameof(HasMostBorrowedBooks));
        OnPropertyChanged(nameof(HasNoMostBorrowedBooks));
        OnPropertyChanged(nameof(HasMostBorrowedCategories));
        OnPropertyChanged(nameof(HasNoMostBorrowedCategories));
        OnPropertyChanged(nameof(HasRecentActivities));
        OnPropertyChanged(nameof(HasNoRecentActivities));
        OnPropertyChanged(nameof(HasNoBorrowedBooks));
        OnPropertyChanged(nameof(HasNoOverdueBooks));
        OnPropertyChanged(nameof(HasNoOutstandingFines));
    }
}
