using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class BookRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IBookRepository
{
    public async Task<PagedResult<Book>> SearchAsync(
        BookSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Book> query = ApplyFilters(dbContext.Books, request);
        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePageNumber = Math.Min(request.PageNumber, totalPages);
        List<Book> books = await query
            .AsNoTracking()
            .AsSplitQuery()
            .Include(book => book.Publisher)
            .Include(book => book.BookAuthors)
                .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(book => book.BookCategories)
                .ThenInclude(bookCategory => bookCategory.Category)
            .Include(book => book.BookCopies)
            .OrderBy(book => book.Title)
            .ThenBy(book => book.BookCode)
            .Skip((effectivePageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Book>(
            books,
            totalCount,
            effectivePageNumber,
            request.PageSize);
    }

    public async Task<Book?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Books
            .AsNoTracking()
            .AsSplitQuery()
            .Include(book => book.Publisher)
            .Include(book => book.BookAuthors)
                .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(book => book.BookCategories)
                .ThenInclude(bookCategory => bookCategory.Category)
            .Include(book => book.BookCopies)
            .SingleOrDefaultAsync(book => book.Id == id, cancellationToken);
    }

    public Task<bool> BookCodeExistsAsync(
        string bookCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return ValueExistsAsync(
            book => EF.Functions.Collate(book.BookCode, "NOCASE") == bookCode
                && (!excludingId.HasValue || book.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> IsbnExistsAsync(
        string isbn,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return ValueExistsAsync(
            book => book.ISBN == isbn
                && (!excludingId.HasValue || book.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<bool> ReferenceDataExistsAsync(
        int publisherId,
        IReadOnlyCollection<int> authorIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        bool publisherExists = await dbContext.Publishers.AnyAsync(
            publisher => publisher.Id == publisherId && publisher.IsActive,
            cancellationToken);
        if (!publisherExists)
        {
            return false;
        }

        int authorCount = await dbContext.Authors.CountAsync(
            author => authorIds.Contains(author.Id) && author.IsActive,
            cancellationToken);
        int categoryCount = await dbContext.Categories.CountAsync(
            category => categoryIds.Contains(category.Id) && category.IsActive,
            cancellationToken);
        return authorCount == authorIds.Count && categoryCount == categoryIds.Count;
    }

    public async Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Book book,
        IReadOnlyCollection<int> authorIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(book);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Book? current = await dbContext.Books
            .Include(item => item.BookAuthors)
            .Include(item => item.BookCategories)
            .SingleOrDefaultAsync(item => item.Id == book.Id, cancellationToken);
        if (current is null)
        {
            throw new InvalidOperationException("Sách không tồn tại.");
        }

        dbContext.Entry(current).CurrentValues.SetValues(book);
        current.BookAuthors.Clear();
        foreach (int authorId in authorIds)
        {
            current.BookAuthors.Add(
                new BookAuthor { BookId = current.Id, AuthorId = authorId });
        }

        current.BookCategories.Clear();
        foreach (int categoryId in categoryIds)
        {
            current.BookCategories.Add(
                new BookCategory { BookId = current.Id, CategoryId = categoryId });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> ValueExistsAsync(
        System.Linq.Expressions.Expression<Func<Book, bool>> predicate,
        CancellationToken cancellationToken)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Books.AnyAsync(predicate, cancellationToken);
    }

    private static IQueryable<Book> ApplyFilters(
        IQueryable<Book> query,
        BookSearchRequest request)
    {
        if (request.IsActive.HasValue)
        {
            query = query.Where(book => book.IsActive == request.IsActive.Value);
        }

        if (request.PublisherId.HasValue)
        {
            query = query.Where(book => book.PublisherId == request.PublisherId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(book => book.BookCategories.Any(
                relation => relation.CategoryId == request.CategoryId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = CreateLikePattern(request.Keyword.Trim());
            query = query.Where(book =>
                EF.Functions.Like(book.BookCode, pattern, @"\")
                || EF.Functions.Like(book.Title, pattern, @"\")
                || (book.ISBN != null
                    && EF.Functions.Like(book.ISBN, pattern, @"\"))
                || book.BookAuthors.Any(relation =>
                    EF.Functions.Like(relation.Author.FullName, pattern, @"\")));
        }

        return query;
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
