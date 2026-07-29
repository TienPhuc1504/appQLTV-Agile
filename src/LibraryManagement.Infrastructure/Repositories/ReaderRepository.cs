using System.Globalization;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class ReaderRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IReaderRepository
{
    public async Task<PagedResult<Reader>> SearchAsync(
        ReaderSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Reader> query = ApplyFilters(dbContext.Readers, request);
        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePageNumber = Math.Min(request.PageNumber, totalPages);
        List<Reader> readers = await ApplyOrdering(
                query.AsNoTracking(),
                request)
            .Skip((effectivePageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Reader>(
            readers,
            totalCount,
            effectivePageNumber,
            request.PageSize);
    }

    public async Task<Reader?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Readers
            .AsNoTracking()
            .SingleOrDefaultAsync(reader => reader.Id == id, cancellationToken);
    }

    public async Task<bool> ReaderCodeExistsAsync(
        string readerCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Readers.AnyAsync(
            reader =>
                EF.Functions.Collate(reader.ReaderCode, "NOCASE") == readerCode
                && (!excludingId.HasValue || reader.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReaderBorrowHistoryDto>>
        GetBorrowingHistoryAsync(
            int readerId,
            CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Where(detail => detail.BorrowSlip.ReaderId == readerId)
            .OrderByDescending(detail => detail.BorrowSlip.BorrowDate)
            .ThenByDescending(detail => detail.Id)
            .Select(detail => new ReaderBorrowHistoryDto(
                detail.Id,
                detail.BorrowSlip.BorrowCode,
                detail.BorrowSlip.BorrowDate,
                detail.ExpectedReturnDate,
                detail.ActualReturnDate,
                detail.BookCopy.CopyCode,
                detail.BookCopy.Book.BookCode,
                detail.BookCopy.Book.Title,
                detail.RenewalCount,
                detail.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReaderFineDto>> GetOutstandingFinesAsync(
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
            .OrderByDescending(fine => fine.CreatedAt)
            .Select(fine => new ReaderFineDto(
                fine.Id,
                fine.FineCode,
                fine.FineType,
                fine.Amount,
                fine.PaidAmount,
                fine.Status == FineStatus.Waived
                    ? 0m
                    : fine.Amount - fine.PaidAmount,
                fine.Status,
                fine.Reason,
                fine.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int?> GetReaderCardValidityMonthsAsync(
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        string? value = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(
                setting =>
                    setting.Key == SystemSettingKeys.ReaderCardValidityMonths)
            .Select(setting => setting.Value)
            .SingleOrDefaultAsync(cancellationToken);

        return int.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out int months)
            ? months
            : null;
    }

    public async Task AddAsync(
        Reader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Readers.Add(reader);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Reader reader,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Entry(reader).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<Reader> ApplyFilters(
        IQueryable<Reader> query,
        ReaderSearchRequest request)
    {
        if (request.Status.HasValue)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            query = request.Status.Value switch
            {
                ReaderStatus.Active => query.Where(
                    reader =>
                        reader.Status == ReaderStatus.Active
                        && reader.ExpirationDate >= today),
                ReaderStatus.Expired => query.Where(
                    reader =>
                        reader.Status == ReaderStatus.Expired
                        || (reader.Status == ReaderStatus.Active
                            && reader.ExpirationDate < today)),
                _ => query.Where(
                    reader => reader.Status == request.Status.Value)
            };
        }

        if (request.ReaderType.HasValue)
        {
            query = query.Where(
                reader => reader.ReaderType == request.ReaderType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = CreateLikePattern(request.Keyword.Trim());
            query = query.Where(reader =>
                EF.Functions.Like(reader.ReaderCode, pattern, @"\")
                || EF.Functions.Like(reader.FullName, pattern, @"\")
                || (reader.PhoneNumber != null
                    && EF.Functions.Like(reader.PhoneNumber, pattern, @"\"))
                || (reader.Email != null
                    && EF.Functions.Like(reader.Email, pattern, @"\")));
        }

        return query;
    }

    private static IOrderedQueryable<Reader> ApplyOrdering(
        IQueryable<Reader> query,
        ReaderSearchRequest request)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        IOrderedQueryable<Reader> orderedQuery =
            (request.SortBy, request.SortDescending) switch
            {
                (ReaderSortField.ReaderCode, false) =>
                    query.OrderBy(reader => reader.ReaderCode),
                (ReaderSortField.ReaderCode, true) =>
                    query.OrderByDescending(reader => reader.ReaderCode),
                (ReaderSortField.ReaderType, false) =>
                    query.OrderBy(reader => reader.ReaderType),
                (ReaderSortField.ReaderType, true) =>
                    query.OrderByDescending(reader => reader.ReaderType),
                (ReaderSortField.ExpirationDate, false) =>
                    query.OrderBy(reader => reader.ExpirationDate),
                (ReaderSortField.ExpirationDate, true) =>
                    query.OrderByDescending(reader => reader.ExpirationDate),
                (ReaderSortField.Status, false) =>
                    query.OrderBy(
                        reader =>
                            reader.Status == ReaderStatus.Active
                            && reader.ExpirationDate < today
                                ? ReaderStatus.Expired
                                : reader.Status),
                (ReaderSortField.Status, true) =>
                    query.OrderByDescending(
                        reader =>
                            reader.Status == ReaderStatus.Active
                            && reader.ExpirationDate < today
                                ? ReaderStatus.Expired
                                : reader.Status),
                (ReaderSortField.FullName, true) =>
                    query.OrderByDescending(reader => reader.FullName),
                _ => query.OrderBy(reader => reader.FullName)
            };

        return orderedQuery
            .ThenBy(reader => reader.ReaderCode)
            .ThenBy(reader => reader.Id);
    }

    private static string CreateLikePattern(string keyword)
    {
        string escapedKeyword = keyword
            .Replace(@"\", @"\\", StringComparison.Ordinal)
            .Replace("%", @"\%", StringComparison.Ordinal)
            .Replace("_", @"\_", StringComparison.Ordinal);
        return $"%{escapedKeyword}%";
    }
}
