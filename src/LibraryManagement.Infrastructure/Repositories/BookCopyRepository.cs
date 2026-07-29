using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class BookCopyRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IBookCopyRepository
{
    public async Task<PagedResult<BookCopy>> SearchAsync(
        BookCopySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<BookCopy> query = dbContext.BookCopies;

        if (request.BookId.HasValue)
        {
            query = query.Where(copy => copy.BookId == request.BookId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(copy => copy.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = CreateLikePattern(request.Keyword.Trim());
            query = query.Where(copy =>
                EF.Functions.Like(copy.CopyCode, pattern, @"\")
                || EF.Functions.Like(copy.Book.Title, pattern, @"\")
                || (copy.ShelfLocation != null
                    && EF.Functions.Like(copy.ShelfLocation, pattern, @"\")));
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePageNumber = Math.Min(request.PageNumber, totalPages);
        List<BookCopy> items = await query
            .AsNoTracking()
            .Include(copy => copy.Book)
            .OrderBy(copy => copy.CopyCode)
            .Skip((effectivePageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<BookCopy>(
            items,
            totalCount,
            effectivePageNumber,
            request.PageSize);
    }

    public async Task<BookCopy?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.BookCopies
            .AsNoTracking()
            .Include(copy => copy.Book)
            .SingleOrDefaultAsync(copy => copy.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BorrowSlipDetail>> GetBorrowHistoryAsync(
        int bookCopyId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.BorrowSlipDetails
            .AsNoTracking()
            .Include(detail => detail.BorrowSlip)
                .ThenInclude(slip => slip.Reader)
            .Where(detail => detail.BookCopyId == bookCopyId)
            .OrderByDescending(detail => detail.BorrowSlip.BorrowDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CopyCodeExistsAsync(
        string copyCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.BookCopies.AnyAsync(
            copy =>
                EF.Functions.Collate(copy.CopyCode, "NOCASE") == copyCode
                && (!excludingId.HasValue || copy.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<bool> ActiveBookExistsAsync(
        int bookId,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Books.AnyAsync(
            book => book.Id == bookId && book.IsActive,
            cancellationToken);
    }

    public async Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookCopy);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.BookCopies.Add(bookCopy);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookCopy);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Entry(bookCopy).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
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
