using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

public sealed class DashboardService(
    IDashboardRepository dashboardRepository,
    IAuthenticationService authenticationService,
    TimeProvider timeProvider)
    : IDashboardService
{
    public Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        DemandAccess();
        return dashboardRepository.GetSummaryAsync(
            GetToday(),
            cancellationToken);
    }

    public async Task<IReadOnlyList<MonthlyBorrowStatisticDto>>
        GetMonthlyBorrowStatisticsAsync(
            int monthCount = 12,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        if (monthCount is < 1 or > 24)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monthCount),
                "Số tháng thống kê phải từ 1 đến 24.");
        }

        DateOnly endMonth = GetFirstDayOfMonth(GetToday());
        DateOnly startMonth = endMonth.AddMonths(1 - monthCount);
        IReadOnlyList<MonthlyBorrowStatisticDto> statistics =
            await dashboardRepository.GetMonthlyBorrowStatisticsAsync(
                startMonth,
                endMonth,
                cancellationToken);
        Dictionary<(int Year, int Month), int> counts = statistics.ToDictionary(
            item => (item.Year, item.Month),
            item => item.BorrowCount);
        var completedStatistics =
            new List<MonthlyBorrowStatisticDto>(monthCount);
        for (int index = 0; index < monthCount; index++)
        {
            DateOnly month = startMonth.AddMonths(index);
            counts.TryGetValue(
                (month.Year, month.Month),
                out int borrowCount);
            completedStatistics.Add(
                new MonthlyBorrowStatisticDto(
                    month.Year,
                    month.Month,
                    borrowCount));
        }

        return completedStatistics;
    }

    public Task<IReadOnlyList<MostBorrowedBookDto>>
        GetMostBorrowedBooksAsync(
            int count = 5,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        ValidateResultCount(count);
        return dashboardRepository.GetMostBorrowedBooksAsync(
            count,
            cancellationToken);
    }

    public Task<IReadOnlyList<MostBorrowedCategoryDto>>
        GetMostBorrowedCategoriesAsync(
            int count = 5,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        ValidateResultCount(count);
        return dashboardRepository.GetMostBorrowedCategoriesAsync(
            count,
            cancellationToken);
    }

    public Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        DashboardServiceAuthorization.DemandActivityLogAccess(
            authenticationService);
        ValidateResultCount(count);
        return dashboardRepository.GetRecentActivitiesAsync(
            count,
            cancellationToken);
    }

    public Task<PagedResult<BorrowedBookReportItemDto>>
        GetBorrowedBooksReportAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        ValidatePaging(pageNumber, pageSize);
        return dashboardRepository.GetBorrowedBooksReportAsync(
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PagedResult<OverdueBookReportItemDto>>
        GetOverdueBooksReportAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        ValidatePaging(pageNumber, pageSize);
        return dashboardRepository.GetOverdueBooksReportAsync(
            GetToday(),
            pageNumber,
            pageSize,
            cancellationToken);
    }

    public Task<PagedResult<OutstandingFineReportItemDto>>
        GetOutstandingFinesReportAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
    {
        DemandAccess();
        ValidatePaging(pageNumber, pageSize);
        return dashboardRepository.GetOutstandingFinesReportAsync(
            pageNumber,
            pageSize,
            cancellationToken);
    }

    private static DateOnly GetFirstDayOfMonth(DateOnly date) =>
        new(date.Year, date.Month, 1);

    private static void ValidateResultCount(int count)
    {
        if (count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(count),
                "Số lượng kết quả phải từ 1 đến 100.");
        }
    }

    private static void ValidatePaging(int pageNumber, int pageSize)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageNumber),
                "Số trang phải lớn hơn hoặc bằng 1.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Số dòng mỗi trang phải từ 1 đến 100.");
        }
    }

    private DateOnly GetToday() =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    private void DemandAccess() =>
        DashboardServiceAuthorization.DemandReportAccess(
            authenticationService);
}
