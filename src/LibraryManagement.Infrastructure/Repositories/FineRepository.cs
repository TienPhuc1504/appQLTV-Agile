using System.Data;
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

public sealed class FineRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IFineRepository
{
    public async Task<PagedResult<FineListItemDto>> GetAllAsync(
        FineSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Fine> query = ApplyFilters(
            dbContext.Fines.AsNoTracking(),
            request);
        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePage = Math.Min(request.PageNumber, totalPages);
        IQueryable<Fine> pagedQuery = query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((effectivePage - 1) * request.PageSize)
            .Take(request.PageSize);
        List<FineListItemDto> items = await ProjectListItem(pagedQuery)
            .ToListAsync(cancellationToken);
        return new PagedResult<FineListItemDto>(
            items,
            totalCount,
            effectivePage,
            request.PageSize);
    }

    public async Task<FineDetailDto?> GetByIdAsync(
        int fineId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Fine? fine = await dbContext.Fines
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Reader)
            .Include(item => item.BorrowSlipDetail)
                .ThenInclude(detail => detail.BookCopy)
                    .ThenInclude(copy => copy.Book)
            .Include(item => item.Payments)
                .ThenInclude(payment => payment.Employee)
            .SingleOrDefaultAsync(
                item => item.Id == fineId,
                cancellationToken);
        return fine is null ? null : MapDetail(fine);
    }

    public async Task<IReadOnlyList<FineListItemDto>> GetReaderFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Fine> query = dbContext.Fines
            .AsNoTracking()
            .Where(fine => fine.ReaderId == readerId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id);
        return await ProjectListItem(query)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetOutstandingAmountAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Fines
            .AsNoTracking()
            .Where(fine =>
                fine.ReaderId == readerId
                && (fine.Status == FineStatus.Unpaid
                    || fine.Status == FineStatus.PartiallyPaid))
            .Select(fine => (decimal?)(fine.Amount - fine.PaidAmount))
            .SumAsync(cancellationToken)
            ?? 0m;
    }

    public async Task<FineCreationSnapshot> GetCreationSnapshotAsync(
        int readerId,
        int borrowSlipDetailId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        bool readerExists = await dbContext.Readers
            .AsNoTracking()
            .AnyAsync(item => item.Id == readerId, cancellationToken);
        int? detailReaderId = await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(item => item.Id == borrowSlipDetailId)
            .Select(item => (int?)item.BorrowSlip.ReaderId)
            .SingleOrDefaultAsync(cancellationToken);
        return new FineCreationSnapshot(
            readerExists,
            detailReaderId.HasValue,
            detailReaderId == readerId);
    }

    public async Task<IFineTransaction> BeginTransactionAsync(
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
            return new FineTransaction(dbContext, transaction);
        }
        catch
        {
            await dbContext.DisposeAsync();
            throw;
        }
    }

    private static IQueryable<Fine> ApplyFilters(
        IQueryable<Fine> query,
        FineSearchRequest request)
    {
        if (request.ReaderId.HasValue)
        {
            query = query.Where(
                fine => fine.ReaderId == request.ReaderId.Value);
        }

        if (request.FineType.HasValue)
        {
            query = query.Where(
                fine => fine.FineType == request.FineType.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(
                fine => fine.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = CreateLikePattern(request.Keyword.Trim());
            query = query.Where(fine =>
                EF.Functions.Like(fine.FineCode, pattern, @"\")
                || EF.Functions.Like(
                    fine.Reader.ReaderCode,
                    pattern,
                    @"\")
                || EF.Functions.Like(
                    fine.Reader.FullName,
                    pattern,
                    @"\"));
        }

        return query;
    }

    private static IQueryable<FineListItemDto> ProjectListItem(
        IQueryable<Fine> query)
    {
        return query.Select(fine => new FineListItemDto(
            fine.Id,
            fine.FineCode,
            fine.ReaderId,
            fine.Reader.ReaderCode,
            fine.Reader.FullName,
            fine.BorrowSlipDetail.BookCopy.CopyCode,
            fine.BorrowSlipDetail.BookCopy.Book.Title,
            fine.FineType,
            fine.Amount,
            fine.PaidAmount,
            fine.Status == FineStatus.Waived
                ? 0m
                : fine.Amount - fine.PaidAmount,
            fine.Status,
            fine.CreatedAt));
    }

    private static FineDetailDto MapDetail(Fine fine)
    {
        FinePaymentDto[] payments = fine.Payments
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Select(payment => new FinePaymentDto(
                payment.Id,
                payment.EmployeeId,
                payment.Employee.FullName,
                payment.Amount,
                payment.PaymentDate,
                payment.PaymentMethod,
                payment.Notes,
                payment.CreatedAt))
            .ToArray();
        return new FineDetailDto(
            fine.Id,
            fine.FineCode,
            fine.ReaderId,
            fine.Reader.ReaderCode,
            fine.Reader.FullName,
            fine.BorrowSlipDetailId,
            fine.BorrowSlipDetail.BookCopy.CopyCode,
            fine.BorrowSlipDetail.BookCopy.Book.Title,
            fine.FineType,
            fine.Amount,
            fine.PaidAmount,
            fine.OutstandingAmount,
            fine.Status,
            fine.Reason,
            payments,
            fine.CreatedAt,
            fine.UpdatedAt);
    }

    private static string CreateLikePattern(string keyword)
    {
        string escapedKeyword = keyword
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapedKeyword}%";
    }

    private sealed class FineTransaction(
        LibraryDbContext dbContext,
        SqliteTransaction transaction)
        : IFineTransaction
    {
        private bool _committed;

        public async Task<FineTransactionSnapshot?> GetSnapshotAsync(
            int fineId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.Fines
                .AsNoTracking()
                .Where(fine => fine.Id == fineId)
                .Select(fine => new FineTransactionSnapshot(
                    fine.Id,
                    fine.FineCode,
                    fine.Amount,
                    fine.PaidAmount,
                    fine.Status))
                .SingleOrDefaultAsync(cancellationToken);
        }

        public async Task PersistFineAsync(
            Fine fine,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(fine);
            ArgumentNullException.ThrowIfNull(activityLog);
            dbContext.Fines.Add(fine);
            await dbContext.SaveChangesAsync(cancellationToken);
            activityLog.EntityId = fine.Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            dbContext.ActivityLogs.Add(activityLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task PersistPaymentAsync(
            FinePayment payment,
            decimal newPaidAmount,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(payment);
            ArgumentNullException.ThrowIfNull(activityLog);
            Fine? fine = await dbContext.Fines.SingleOrDefaultAsync(
                item => item.Id == payment.FineId,
                cancellationToken);
            if (fine is null
                || fine.Status is FineStatus.Paid or FineStatus.Waived
                || fine.PaidAmount + payment.Amount != newPaidAmount
                || newPaidAmount > fine.Amount)
            {
                throw new FineConflictException(
                    "Khoản phạt đã được xử lý hoặc dữ liệu vừa thay đổi.");
            }

            fine.PaidAmount = newPaidAmount;
            fine.Status = fine.PaidAmount == fine.Amount
                ? FineStatus.Paid
                : FineStatus.PartiallyPaid;
            dbContext.FinePayments.Add(payment);
            activityLog.EntityId = fine.Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            dbContext.ActivityLogs.Add(activityLog);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task PersistWaiverAsync(
            int fineId,
            ActivityLog activityLog,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(activityLog);
            Fine? fine = await dbContext.Fines.SingleOrDefaultAsync(
                item => item.Id == fineId,
                cancellationToken);
            if (fine is null
                || fine.Status is FineStatus.Paid or FineStatus.Waived)
            {
                throw new FineConflictException(
                    "Khoản phạt đã được xử lý hoặc dữ liệu vừa thay đổi.");
            }

            fine.Status = FineStatus.Waived;
            activityLog.EntityId = fine.Id.ToString(
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
