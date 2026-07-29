using System.Data;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class BorrowRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IBorrowRepository
{
    private static readonly string[] BorrowSettingKeys =
    [
        SystemSettingKeys.MaximumBorrowedBooks,
        SystemSettingKeys.DefaultBorrowDays,
        SystemSettingKeys.MaximumOutstandingFineAmount
    ];

    private static readonly string[] RenewalSettingKeys =
    [
        SystemSettingKeys.MaximumRenewalCount,
        SystemSettingKeys.RenewalDays
    ];

    public async Task<BorrowValidationSnapshot> GetValidationSnapshotAsync(
        int readerId,
        IReadOnlyCollection<int> bookCopyIds,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await CreateValidationSnapshotAsync(
            dbContext,
            readerId,
            bookCopyIds,
            evaluationDate,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keys);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SystemSettings
            .AsNoTracking()
            .Where(setting => keys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken);
    }

    public async Task<IBorrowTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await dbContext.Database.OpenConnectionAsync(cancellationToken);
            var connection =
                (SqliteConnection)dbContext.Database.GetDbConnection();
            SqliteTransaction transaction = await Task.Run(
                () => connection.BeginTransaction(
                    IsolationLevel.Serializable,
                    deferred: false),
                cancellationToken);
            dbContext.Database.UseTransaction(transaction);
            return new BorrowTransaction(dbContext, transaction);
        }
        catch
        {
            await dbContext.DisposeAsync();
            throw;
        }
    }

    public async Task<BorrowSlipDto?> GetBorrowSlipAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        BorrowSlip? borrowSlip = await dbContext.BorrowSlips
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Reader)
            .Include(item => item.Employee)
            .Include(item => item.Details)
                .ThenInclude(detail => detail.BookCopy)
                    .ThenInclude(copy => copy.Book)
            .SingleOrDefaultAsync(
                item => item.Id == borrowSlipId,
                cancellationToken);
        return borrowSlip is null ? null : MapBorrowSlip(borrowSlip);
    }

    public async Task<PagedResult<BorrowSlipListItemDto>>
        GetActiveBorrowSlipsAsync(
            BorrowSlipSearchRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<BorrowSlip> query = dbContext.BorrowSlips
            .AsNoTracking()
            .Where(item =>
                item.Status == BorrowSlipStatus.Active
                || item.Status == BorrowSlipStatus.PartiallyReturned
                || item.Status == BorrowSlipStatus.Overdue);

        if (request.ReaderId.HasValue)
        {
            query = query.Where(
                item => item.ReaderId == request.ReaderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = CreateLikePattern(request.Keyword.Trim());
            query = query.Where(item =>
                EF.Functions.Like(item.BorrowCode, pattern, @"\")
                || EF.Functions.Like(item.Reader.ReaderCode, pattern, @"\")
                || EF.Functions.Like(item.Reader.FullName, pattern, @"\"));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePageNumber = Math.Min(request.PageNumber, totalPages);
        List<BorrowSlipListItemDto> items = await query
            .OrderByDescending(item => item.BorrowDate)
            .ThenByDescending(item => item.Id)
            .Skip((effectivePageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new BorrowSlipListItemDto(
                item.Id,
                item.BorrowCode,
                item.ReaderId,
                item.Reader.ReaderCode,
                item.Reader.FullName,
                item.BorrowDate,
                item.ExpectedReturnDate,
                item.Details.Count,
                item.Details.Count(detail =>
                    detail.Status == BorrowSlipDetailStatus.Borrowing
                    || detail.Status == BorrowSlipDetailStatus.Overdue),
                item.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<BorrowSlipListItemDto>(
            items,
            totalCount,
            effectivePageNumber,
            request.PageSize);
    }

    public async Task<IReadOnlyList<BorrowSlipDetailDto>>
        GetReaderActiveBorrowsAsync(
            int readerId,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(detail =>
                detail.BorrowSlip.ReaderId == readerId
                && (detail.Status == BorrowSlipDetailStatus.Borrowing
                    || detail.Status == BorrowSlipDetailStatus.Overdue))
            .OrderBy(detail => detail.ExpectedReturnDate)
            .ThenBy(detail => detail.Id)
            .Select(detail => new BorrowSlipDetailDto(
                detail.Id,
                detail.BookCopyId,
                detail.BookCopy.CopyCode,
                detail.BookCopy.Book.BookCode,
                detail.BookCopy.Book.Title,
                detail.ExpectedReturnDate,
                detail.ActualReturnDate,
                detail.RenewalCount,
                detail.Status))
            .ToListAsync(cancellationToken);
    }

    private static async Task<BorrowValidationSnapshot>
        CreateValidationSnapshotAsync(
            LibraryDbContext dbContext,
            int readerId,
            IReadOnlyCollection<int> bookCopyIds,
            DateOnly evaluationDate,
            CancellationToken cancellationToken)
    {
        BorrowReaderSnapshot? reader = await dbContext.Readers
            .AsNoTracking()
            .Where(item => item.Id == readerId)
            .Select(item => new BorrowReaderSnapshot(
                item.Id,
                item.ReaderCode,
                item.FullName,
                item.Status,
                item.ExpirationDate))
            .SingleOrDefaultAsync(cancellationToken);

        int activeBorrowedCopyCount = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .CountAsync(
                detail =>
                    detail.BorrowSlip.ReaderId == readerId
                    && (detail.Status == BorrowSlipDetailStatus.Borrowing
                        || detail.Status == BorrowSlipDetailStatus.Overdue),
                cancellationToken);
        bool hasOverdueBorrow = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .AnyAsync(
                detail =>
                    detail.BorrowSlip.ReaderId == readerId
                    && (detail.Status == BorrowSlipDetailStatus.Overdue
                        || (detail.Status == BorrowSlipDetailStatus.Borrowing
                            && detail.ExpectedReturnDate < evaluationDate)),
                cancellationToken);
        decimal outstandingFineAmount = await dbContext.Fines
            .AsNoTracking()
            .Where(fine =>
                fine.ReaderId == readerId
                && (fine.Status == FineStatus.Unpaid
                    || fine.Status == FineStatus.PartiallyPaid))
            .Select(fine => (decimal?)(fine.Amount - fine.PaidAmount))
            .SumAsync(cancellationToken)
            ?? 0m;
        List<BorrowCopySnapshot> bookCopies = await dbContext.BookCopies
            .AsNoTracking()
            .Where(copy => bookCopyIds.Contains(copy.Id))
            .Select(copy => new BorrowCopySnapshot(
                copy.Id,
                copy.CopyCode,
                copy.Book.BookCode,
                copy.Book.Title,
                copy.Book.IsActive,
                copy.Status))
            .ToListAsync(cancellationToken);
        Dictionary<string, string> settings = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(setting => BorrowSettingKeys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken);

        return new BorrowValidationSnapshot(
            reader,
            activeBorrowedCopyCount,
            hasOverdueBorrow,
            outstandingFineAmount,
            bookCopies,
            settings);
    }

    private static async Task<RenewalSnapshot?> CreateRenewalSnapshotAsync(
        LibraryDbContext dbContext,
        int borrowSlipDetailId,
        CancellationToken cancellationToken)
    {
        RenewalSnapshotData? data = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(detail => detail.Id == borrowSlipDetailId)
            .Select(detail => new RenewalSnapshotData(
                detail.Id,
                detail.BorrowSlipId,
                detail.BorrowSlip.BorrowCode,
                detail.BookCopy.CopyCode,
                detail.BookCopy.Book.Title,
                detail.BorrowSlip.Reader.Status,
                detail.BorrowSlip.Reader.ExpirationDate,
                detail.BorrowSlip.Status,
                detail.Status,
                detail.BookCopy.Status,
                detail.ExpectedReturnDate,
                detail.ActualReturnDate,
                detail.RenewalCount))
            .SingleOrDefaultAsync(cancellationToken);
        if (data is null)
        {
            return null;
        }

        Dictionary<string, string> settings = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(setting => RenewalSettingKeys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                StringComparer.Ordinal,
                cancellationToken);
        return new RenewalSnapshot(
            data.BorrowSlipDetailId,
            data.BorrowSlipId,
            data.BorrowCode,
            data.CopyCode,
            data.BookTitle,
            data.ReaderStatus,
            data.ReaderExpirationDate,
            data.BorrowSlipStatus,
            data.DetailStatus,
            data.BookCopyStatus,
            data.ExpectedReturnDate,
            data.ActualReturnDate,
            data.RenewalCount,
            settings);
    }

    private static BorrowSlipDto MapBorrowSlip(BorrowSlip borrowSlip)
    {
        BorrowSlipDetailDto[] details = borrowSlip.Details
            .OrderBy(detail => detail.Id)
            .Select(detail => new BorrowSlipDetailDto(
                detail.Id,
                detail.BookCopyId,
                detail.BookCopy.CopyCode,
                detail.BookCopy.Book.BookCode,
                detail.BookCopy.Book.Title,
                detail.ExpectedReturnDate,
                detail.ActualReturnDate,
                detail.RenewalCount,
                detail.Status))
            .ToArray();
        return new BorrowSlipDto(
            borrowSlip.Id,
            borrowSlip.BorrowCode,
            borrowSlip.ReaderId,
            borrowSlip.Reader.ReaderCode,
            borrowSlip.Reader.FullName,
            borrowSlip.EmployeeId,
            borrowSlip.Employee.FullName,
            borrowSlip.BorrowDate,
            borrowSlip.ExpectedReturnDate,
            borrowSlip.Status,
            borrowSlip.Notes,
            details,
            borrowSlip.CreatedAt,
            borrowSlip.UpdatedAt);
    }

    private static string CreateLikePattern(string keyword)
    {
        string escapedKeyword = keyword
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapedKeyword}%";
    }

    private sealed record RenewalSnapshotData(
        int BorrowSlipDetailId,
        int BorrowSlipId,
        string BorrowCode,
        string CopyCode,
        string BookTitle,
        ReaderStatus ReaderStatus,
        DateOnly ReaderExpirationDate,
        BorrowSlipStatus BorrowSlipStatus,
        BorrowSlipDetailStatus DetailStatus,
        BookCopyStatus BookCopyStatus,
        DateOnly ExpectedReturnDate,
        DateOnly? ActualReturnDate,
        int RenewalCount);

    private sealed class BorrowTransaction(
        LibraryDbContext dbContext,
        SqliteTransaction transaction)
        : IBorrowTransaction
    {
        private bool _committed;

        public Task<BorrowValidationSnapshot> GetValidationSnapshotAsync(
            int readerId,
            IReadOnlyCollection<int> bookCopyIds,
            DateOnly evaluationDate,
            CancellationToken cancellationToken = default)
        {
            return CreateValidationSnapshotAsync(
                dbContext,
                readerId,
                bookCopyIds,
                evaluationDate,
                cancellationToken);
        }

        public Task<RenewalSnapshot?> GetRenewalSnapshotAsync(
            int borrowSlipDetailId,
            CancellationToken cancellationToken = default)
        {
            return CreateRenewalSnapshotAsync(
                dbContext,
                borrowSlipDetailId,
                cancellationToken);
        }

        public async Task PersistAsync(
            BorrowSlip borrowSlip,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(borrowSlip);
            ArgumentNullException.ThrowIfNull(activityLog);
            int[] bookCopyIds = borrowSlip.Details
                .Select(detail => detail.BookCopyId)
                .ToArray();
            List<BookCopy> copies = await dbContext.BookCopies
                .Where(copy => bookCopyIds.Contains(copy.Id))
                .ToListAsync(cancellationToken);
            if (copies.Count != bookCopyIds.Length
                || copies.Any(copy => copy.Status != BookCopyStatus.Available))
            {
                throw new BorrowConflictException(
                    "Một hoặc nhiều bản sách vừa được người khác mượn.");
            }

            foreach (BookCopy copy in copies)
            {
                copy.Status = BookCopyStatus.Borrowed;
            }

            dbContext.BorrowSlips.Add(borrowSlip);
            await dbContext.SaveChangesAsync(cancellationToken);

            activityLog.EntityId = borrowSlip.Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            dbContext.ActivityLogs.Add(activityLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task PersistRenewalAsync(
            int borrowSlipDetailId,
            DateOnly newExpectedReturnDate,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activityLog);
            BorrowSlipDetail? detail = await dbContext.BorrowSlipDetails
                .Include(item => item.BookCopy)
                .Include(item => item.BorrowSlip)
                    .ThenInclude(borrowSlip => borrowSlip.Details)
                .SingleOrDefaultAsync(
                    item => item.Id == borrowSlipDetailId,
                    cancellationToken);
            if (detail is null
                || detail.Status != BorrowSlipDetailStatus.Borrowing
                || detail.ActualReturnDate is not null
                || detail.BookCopy.Status != BookCopyStatus.Borrowed)
            {
                throw new BorrowConflictException(
                    "Sách mượn đã được trả hoặc dữ liệu vừa thay đổi.");
            }

            detail.RenewalCount++;
            detail.ExpectedReturnDate = newExpectedReturnDate;
            detail.BorrowSlip.ExpectedReturnDate =
                detail.BorrowSlip.Details.Max(
                    item => item.ExpectedReturnDate);
            activityLog.EntityId = detail.Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            dbContext.ActivityLogs.Add(activityLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task CommitAsync(
            CancellationToken cancellationToken = default)
        {
            await transaction.CommitAsync(cancellationToken);
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                await transaction.RollbackAsync();
            }

            await transaction.DisposeAsync();
            await dbContext.DisposeAsync();
        }
    }
}
