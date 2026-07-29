using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MonthlyBorrowStatisticDto>>
        GetMonthlyBorrowStatisticsAsync(
            int monthCount = 12,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MostBorrowedBookDto>> GetMostBorrowedBooksAsync(
        int count = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MostBorrowedCategoryDto>>
        GetMostBorrowedCategoriesAsync(
            int count = 5,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivitiesAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BorrowedBookReportItemDto>> GetBorrowedBooksReportAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OverdueBookReportItemDto>> GetOverdueBooksReportAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<PagedResult<OutstandingFineReportItemDto>>
        GetOutstandingFinesReportAsync(
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);
}
