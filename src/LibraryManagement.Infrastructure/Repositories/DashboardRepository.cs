using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class DashboardRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IDashboardRepository
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(
        DateOnly referenceDate,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int totalBooks = await dbContext.Books
            .AsNoTracking()
            .CountAsync(book => book.IsActive, cancellationToken);
        int totalBookCopies = await dbContext.BookCopies
            .AsNoTracking()
            .CountAsync(
                copy => copy.Status != BookCopyStatus.Inactive,
                cancellationToken);
        int availableBookCopies = await dbContext.BookCopies
            .AsNoTracking()
            .CountAsync(
                copy => copy.Status == BookCopyStatus.Available,
                cancellationToken);
        int borrowedBookCopies = await dbContext.BookCopies
            .AsNoTracking()
            .CountAsync(
                copy => copy.Status == BookCopyStatus.Borrowed,
                cancellationToken);
        int overdueBookCopies = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .CountAsync(
                detail => detail.ActualReturnDate == null
                    && (detail.Status == BorrowSlipDetailStatus.Overdue
                        || (detail.Status == BorrowSlipDetailStatus.Borrowing
                            && detail.ExpectedReturnDate < referenceDate)),
                cancellationToken);
        int activeReaders = await dbContext.Readers
            .AsNoTracking()
            .CountAsync(
                reader => reader.Status == ReaderStatus.Active,
                cancellationToken);
        int todayBorrowedBooks = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .CountAsync(
                detail => detail.BorrowSlip.BorrowDate == referenceDate
                    && detail.BorrowSlip.Status
                        != BorrowSlipStatus.Cancelled,
                cancellationToken);
        int todayReturnedBooks = await dbContext.ReturnRecords
            .AsNoTracking()
            .CountAsync(
                record => record.ReturnDate == referenceDate,
                cancellationToken);
        decimal outstandingFineAmount = await dbContext.Fines
            .AsNoTracking()
            .Where(
                fine => fine.Status == FineStatus.Unpaid
                    || fine.Status == FineStatus.PartiallyPaid)
            .Select(
                fine => (decimal?)(fine.Amount - fine.PaidAmount))
            .SumAsync(cancellationToken)
            ?? 0m;

        return new DashboardSummaryDto(
            totalBooks,
            totalBookCopies,
            availableBookCopies,
            borrowedBookCopies,
            overdueBookCopies,
            activeReaders,
            todayBorrowedBooks,
            todayReturnedBooks,
            outstandingFineAmount);
    }

    public async Task<IReadOnlyList<MonthlyBorrowStatisticDto>>
        GetMonthlyBorrowStatisticsAsync(
            DateOnly startMonth,
            DateOnly endMonth,
            CancellationToken cancellationToken = default)
    {
        DateOnly endExclusive = endMonth.AddMonths(1);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var dailyStatistics = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(
                detail => detail.BorrowSlip.BorrowDate >= startMonth
                    && detail.BorrowSlip.BorrowDate < endExclusive
                    && detail.BorrowSlip.Status
                        != BorrowSlipStatus.Cancelled)
            .GroupBy(
                detail => detail.BorrowSlip.BorrowDate)
            .Select(
                group => new
                {
                    Date = group.Key,
                    BorrowCount = group.Count()
                })
            .ToArrayAsync(cancellationToken);

        return dailyStatistics
            .GroupBy(item => new { item.Date.Year, item.Date.Month })
            .Select(
                group => new MonthlyBorrowStatisticDto(
                    group.Key.Year,
                    group.Key.Month,
                    group.Sum(item => item.BorrowCount)))
            .OrderBy(item => item.Year)
            .ThenBy(item => item.Month)
            .ToArray();
    }

    public async Task<IReadOnlyList<MostBorrowedBookDto>>
        GetMostBorrowedBooksAsync(
            int count,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(
                detail => detail.BorrowSlip.Status
                    != BorrowSlipStatus.Cancelled)
            .GroupBy(
                detail => new
                {
                    detail.BookCopy.BookId,
                    detail.BookCopy.Book.BookCode,
                    detail.BookCopy.Book.Title
                })
            .Select(
                group => new
                {
                    group.Key.BookId,
                    group.Key.BookCode,
                    group.Key.Title,
                    BorrowCount = group.Count()
                })
            .OrderByDescending(item => item.BorrowCount)
            .ThenBy(item => item.Title)
            .Take(count)
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(
                item => new MostBorrowedBookDto(
                    item.BookId,
                    item.BookCode,
                    item.Title,
                    item.BorrowCount))
            .ToArray();
    }

    public async Task<IReadOnlyList<MostBorrowedCategoryDto>>
        GetMostBorrowedCategoriesAsync(
            int count,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(
                detail => detail.BorrowSlip.Status
                    != BorrowSlipStatus.Cancelled)
            .SelectMany(
                detail => detail.BookCopy.Book.BookCategories,
                (_, bookCategory) => bookCategory.Category)
            .GroupBy(category => new { category.Id, category.Name })
            .Select(
                group => new
                {
                    group.Key.Id,
                    group.Key.Name,
                    BorrowCount = group.Count()
                })
            .OrderByDescending(item => item.BorrowCount)
            .ThenBy(item => item.Name)
            .Take(count)
            .ToArrayAsync(cancellationToken);

        return rows
            .Select(
                item => new MostBorrowedCategoryDto(
                    item.Id,
                    item.Name,
                    item.BorrowCount))
            .ToArray();
    }

    public async Task<IReadOnlyList<RecentActivityDto>>
        GetRecentActivitiesAsync(
            int count,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ActivityLogs
            .AsNoTracking()
            .OrderByDescending(activity => activity.CreatedAt)
            .ThenByDescending(activity => activity.Id)
            .Take(count)
            .Select(
                activity => new RecentActivityDto(
                    activity.Id,
                    activity.Employee.FullName,
                    activity.Action,
                    activity.EntityName,
                    activity.EntityId,
                    activity.Description,
                    activity.CreatedAt))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<PagedResult<BorrowedBookReportItemDto>>
        GetBorrowedBooksReportAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<BorrowSlipDetail> query =
            dbContext.BorrowSlipDetails
                .AsNoTracking()
            .Where(
                detail => detail.ActualReturnDate == null
                    && detail.BorrowSlip.Status
                        != BorrowSlipStatus.Cancelled
                    && (detail.Status == BorrowSlipDetailStatus.Borrowing
                        || detail.Status == BorrowSlipDetailStatus.Overdue));
        int totalCount = await query.CountAsync(cancellationToken);
        int effectivePageNumber = GetEffectivePageNumber(
            totalCount,
            pageNumber,
            pageSize);
        BorrowedBookReportItemDto[] items = await query
            .OrderBy(detail => detail.ExpectedReturnDate)
            .ThenBy(detail => detail.BorrowSlip.BorrowCode)
            .Skip((effectivePageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(
                detail => new BorrowedBookReportItemDto(
                    detail.Id,
                    detail.BorrowSlip.BorrowCode,
                    detail.BorrowSlip.Reader.ReaderCode,
                    detail.BorrowSlip.Reader.FullName,
                    detail.BookCopy.CopyCode,
                    detail.BookCopy.Book.Title,
                    detail.BorrowSlip.BorrowDate,
                    detail.ExpectedReturnDate,
                    detail.Status))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<BorrowedBookReportItemDto>(
            items,
            totalCount,
            effectivePageNumber,
            pageSize);
    }

    public async Task<PagedResult<OverdueBookReportItemDto>>
        GetOverdueBooksReportAsync(
            DateOnly referenceDate,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<BorrowSlipDetail> query =
            dbContext.BorrowSlipDetails
                .AsNoTracking()
            .Where(
                detail => detail.ActualReturnDate == null
                    && detail.BorrowSlip.Status
                        != BorrowSlipStatus.Cancelled
                    && detail.ExpectedReturnDate < referenceDate
                    && (detail.Status == BorrowSlipDetailStatus.Borrowing
                        || detail.Status == BorrowSlipDetailStatus.Overdue));
        int totalCount = await query.CountAsync(cancellationToken);
        int effectivePageNumber = GetEffectivePageNumber(
            totalCount,
            pageNumber,
            pageSize);
        OverdueBookReportItemDto[] items = await query
            .OrderBy(detail => detail.ExpectedReturnDate)
            .ThenBy(detail => detail.BorrowSlip.BorrowCode)
            .Skip((effectivePageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(
                detail => new OverdueBookReportItemDto(
                    detail.Id,
                    detail.BorrowSlip.BorrowCode,
                    detail.BorrowSlip.Reader.ReaderCode,
                    detail.BorrowSlip.Reader.FullName,
                    detail.BookCopy.CopyCode,
                    detail.BookCopy.Book.Title,
                    detail.ExpectedReturnDate,
                    referenceDate.DayNumber
                        - detail.ExpectedReturnDate.DayNumber))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<OverdueBookReportItemDto>(
            items,
            totalCount,
            effectivePageNumber,
            pageSize);
    }

    public async Task<PagedResult<OutstandingFineReportItemDto>>
        GetOutstandingFinesReportAsync(
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Fine> query = dbContext.Fines
            .AsNoTracking()
            .Where(
                fine => fine.Status == FineStatus.Unpaid
                    || fine.Status == FineStatus.PartiallyPaid);
        int totalCount = await query.CountAsync(cancellationToken);
        int effectivePageNumber = GetEffectivePageNumber(
            totalCount,
            pageNumber,
            pageSize);
        OutstandingFineReportItemDto[] items = await query
            .OrderByDescending(
                fine => (double)(fine.Amount - fine.PaidAmount))
            .ThenBy(fine => fine.FineCode)
            .Skip((effectivePageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(
                fine => new OutstandingFineReportItemDto(
                    fine.Id,
                    fine.FineCode,
                    fine.Reader.ReaderCode,
                    fine.Reader.FullName,
                    fine.FineType,
                    fine.Amount,
                    fine.PaidAmount,
                    fine.Amount - fine.PaidAmount,
                    fine.Status,
                    fine.CreatedAt))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<OutstandingFineReportItemDto>(
            items,
            totalCount,
            effectivePageNumber,
            pageSize);
    }

    private static int GetEffectivePageNumber(
        int totalCount,
        int requestedPageNumber,
        int pageSize)
    {
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Math.Min(requestedPageNumber, totalPages);
    }
}
