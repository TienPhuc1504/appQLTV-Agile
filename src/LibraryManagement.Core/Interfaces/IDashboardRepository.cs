using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyBorrowStatisticDto>> GetMonthlyBorrowStatisticsAsync(
        DateOnly startMonth,
        DateOnly endMonth,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MostBorrowedBookDto>> GetMostBorrowedBooksAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MostBorrowedCategoryDto>> GetMostBorrowedCategoriesAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(
        int count,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BorrowedBookReportItemDto>> GetBorrowedBooksReportAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OverdueBookReportItemDto>> GetOverdueBooksReportAsync(
        DateOnly referenceDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OutstandingFineReportItemDto>>
        GetOutstandingFinesReportAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
}
