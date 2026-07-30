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

public sealed class ReturnRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IReturnRepository
{
    private static readonly string[] FineSettingKeys =
    [
        SystemSettingKeys.OverdueFinePerDay,
        SystemSettingKeys.LostBookFineMultiplier,
        SystemSettingKeys.DamagedBookFineMultiplier
    ];

    public async Task<IReadOnlyList<ReturnLookupDto>> SearchOutstandingAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        string normalizedKeyword = keyword.Trim();
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<BorrowSlipDetail> query = dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(detail =>
                detail.Status == BorrowSlipDetailStatus.Borrowing
                || detail.Status == BorrowSlipDetailStatus.Overdue);
        if (normalizedKeyword.Length > 0)
        {
            string pattern = CreateLikePattern(normalizedKeyword);
            query = query.Where(detail =>
                EF.Functions.Like(
                        detail.BorrowSlip.BorrowCode,
                        pattern,
                        @"\")
                || EF.Functions.Like(
                    detail.BookCopy.CopyCode,
                    pattern,
                    @"\"));
        }

        List<ReturnSearchRow> rows = await query
            .OrderByDescending(detail => detail.BorrowSlip.BorrowDate)
            .ThenBy(detail => detail.BookCopy.CopyCode)
            .Take(200)
            .Select(detail => new ReturnSearchRow(
                detail.BorrowSlipId,
                detail.BorrowSlip.BorrowCode,
                detail.BorrowSlip.ReaderId,
                detail.BorrowSlip.Reader.ReaderCode,
                detail.BorrowSlip.Reader.FullName,
                detail.BorrowSlip.BorrowDate,
                detail.Id,
                detail.BookCopyId,
                detail.BookCopy.CopyCode,
                detail.BookCopy.Book.BookCode,
                detail.BookCopy.Book.Title,
                detail.ExpectedReturnDate,
                detail.Status))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => new
            {
                row.BorrowSlipId,
                row.BorrowCode,
                row.ReaderId,
                row.ReaderCode,
                row.ReaderName,
                row.BorrowDate
            })
            .Take(20)
            .Select(group => new ReturnLookupDto(
                group.Key.BorrowSlipId,
                group.Key.BorrowCode,
                group.Key.ReaderId,
                group.Key.ReaderCode,
                group.Key.ReaderName,
                group.Key.BorrowDate,
                group.Select(row => new ReturnableBookDto(
                        row.BorrowSlipDetailId,
                        row.BookCopyId,
                        row.CopyCode,
                        row.BookCode,
                        row.BookTitle,
                        row.ExpectedReturnDate,
                        row.Status))
                    .ToArray()))
            .ToArray();
    }

    public async Task<ReturnTransactionSnapshot> GetSnapshotAsync(
        IReadOnlyCollection<int> borrowSlipDetailIds,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await CreateSnapshotAsync(
            dbContext,
            borrowSlipDetailIds,
            cancellationToken);
    }

    public async Task<IReturnTransaction> BeginTransactionAsync(
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
            return new ReturnTransaction(dbContext, transaction);
        }
        catch
        {
            await dbContext.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> UpdateBorrowSlipStatusAsync(
        int borrowSlipId,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
        BorrowSlip? borrowSlip = await dbContext.BorrowSlips
            .Include(item => item.Details)
            .SingleOrDefaultAsync(
                item => item.Id == borrowSlipId,
                cancellationToken);
        if (borrowSlip is null)
        {
            return false;
        }

        UpdateSlipState(borrowSlip, evaluationDate);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static async Task<ReturnTransactionSnapshot> CreateSnapshotAsync(
        LibraryDbContext dbContext,
        IReadOnlyCollection<int> borrowSlipDetailIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(borrowSlipDetailIds);
        List<ReturnDetailSnapshot> details =
            await dbContext.BorrowSlipDetails
                .AsNoTracking()
                .Where(detail => borrowSlipDetailIds.Contains(detail.Id))
                .Select(detail => new ReturnDetailSnapshot(
                    detail.Id,
                    detail.BorrowSlipId,
                    detail.BorrowSlip.BorrowCode,
                    detail.BorrowSlip.ReaderId,
                    detail.BorrowSlip.Reader.ReaderCode,
                    detail.BookCopyId,
                    detail.BookCopy.CopyCode,
                    detail.BookCopy.Book.Title,
                    detail.BookCopy.Book.Price,
                    detail.BorrowSlip.BorrowDate,
                    detail.ExpectedReturnDate,
                    detail.BorrowSlip.Status,
                    detail.Status,
                    detail.BookCopy.Status,
                    detail.ReturnRecord != null,
                    detail.Fines
                        .Where(fine => fine.FineType == FineType.Overdue)
                        .Sum(fine => (decimal?)fine.Amount) ?? 0m,
                    detail.Fines
                        .Where(fine => fine.FineType == FineType.Damaged)
                        .Sum(fine => (decimal?)fine.Amount) ?? 0m,
                    detail.Fines
                        .Where(fine => fine.FineType == FineType.Lost)
                        .Sum(fine => (decimal?)fine.Amount) ?? 0m))
                .ToListAsync(cancellationToken);
        Dictionary<string, string> settings =
            await dbContext.SystemSettings
                .AsNoTracking()
                .Where(setting => FineSettingKeys.Contains(setting.Key))
                .ToDictionaryAsync(
                    setting => setting.Key,
                    setting => setting.Value,
                    StringComparer.Ordinal,
                    cancellationToken);
        return new ReturnTransactionSnapshot(details, settings);
    }

    private static void UpdateSlipState(
        BorrowSlip borrowSlip,
        DateOnly evaluationDate)
    {
        if (borrowSlip.Status == BorrowSlipStatus.Cancelled)
        {
            return;
        }

        foreach (BorrowSlipDetail detail in borrowSlip.Details.Where(
                     detail =>
                         detail.Status == BorrowSlipDetailStatus.Borrowing
                         && detail.ExpectedReturnDate < evaluationDate))
        {
            detail.Status = BorrowSlipDetailStatus.Overdue;
        }

        bool allReturned = borrowSlip.Details.Count > 0
            && borrowSlip.Details.All(IsTerminal);
        if (allReturned)
        {
            borrowSlip.Status = BorrowSlipStatus.Completed;
            return;
        }

        if (borrowSlip.Details.Any(
                detail => detail.Status == BorrowSlipDetailStatus.Overdue))
        {
            borrowSlip.Status = BorrowSlipStatus.Overdue;
            return;
        }

        borrowSlip.Status = borrowSlip.Details.Any(IsTerminal)
            ? BorrowSlipStatus.PartiallyReturned
            : BorrowSlipStatus.Active;
    }

    private static bool IsTerminal(BorrowSlipDetail detail)
    {
        return detail.Status is
            BorrowSlipDetailStatus.Returned
            or BorrowSlipDetailStatus.Damaged
            or BorrowSlipDetailStatus.Lost;
    }

    private static string CreateLikePattern(string keyword)
    {
        string escapedKeyword = keyword
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapedKeyword}%";
    }

    private sealed record ReturnSearchRow(
        int BorrowSlipId,
        string BorrowCode,
        int ReaderId,
        string ReaderCode,
        string ReaderName,
        DateOnly BorrowDate,
        int BorrowSlipDetailId,
        int BookCopyId,
        string CopyCode,
        string BookCode,
        string BookTitle,
        DateOnly ExpectedReturnDate,
        BorrowSlipDetailStatus Status);

    private sealed class ReturnTransaction(
        LibraryDbContext dbContext,
        SqliteTransaction transaction)
        : IReturnTransaction
    {
        private bool _committed;

        public Task<ReturnTransactionSnapshot> GetSnapshotAsync(
            IReadOnlyCollection<int> borrowSlipDetailIds,
            CancellationToken cancellationToken = default)
        {
            return CreateSnapshotAsync(
                dbContext,
                borrowSlipDetailIds,
                cancellationToken);
        }

        public async Task PersistAsync(
            ReturnPersistenceCommand command,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            ArgumentNullException.ThrowIfNull(activityLog);
            int[] detailIds = command.Items
                .Select(item => item.BorrowSlipDetailId)
                .ToArray();
            List<BorrowSlipDetail> details =
                await dbContext.BorrowSlipDetails
                    .Include(detail => detail.ReturnRecord)
                    .Include(detail => detail.BookCopy)
                    .Include(detail => detail.BorrowSlip)
                        .ThenInclude(borrowSlip => borrowSlip.Details)
                    .Where(detail => detailIds.Contains(detail.Id))
                    .ToListAsync(cancellationToken);
            if (details.Count != detailIds.Length
                || details.Any(detail =>
                    !detail.CanBeReturned
                    || detail.ReturnRecord is not null
                    || detail.BookCopy.Status != BookCopyStatus.Borrowed))
            {
                throw new ReturnConflictException(
                    "Một hoặc nhiều bản sách đã được trả hoặc dữ liệu vừa thay đổi.");
            }

            Dictionary<int, BorrowSlipDetail> detailById =
                details.ToDictionary(detail => detail.Id);
            foreach (ReturnPersistenceItem item in command.Items)
            {
                BorrowSlipDetail detail = detailById[item.BorrowSlipDetailId];
                detail.ActualReturnDate = command.ReturnDate;
                detail.Status = item.DetailStatus;
                detail.Notes = item.Notes;
                detail.BookCopy.PhysicalCondition = item.ReturnedCondition;
                detail.BookCopy.Status = item.BookCopyStatus;
                dbContext.ReturnRecords.Add(item.ReturnRecord);
                dbContext.Fines.AddRange(item.Fines);
            }

            foreach (BorrowSlip borrowSlip in details
                         .Select(detail => detail.BorrowSlip)
                         .DistinctBy(item => item.Id))
            {
                UpdateSlipState(borrowSlip, command.ReturnDate);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            ReturnRecord firstRecord = command.Items.First().ReturnRecord;
            activityLog.EntityId = firstRecord.Id.ToString(
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
