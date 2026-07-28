using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class AuthorRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IAuthorRepository
{
    public async Task<IReadOnlyList<Author>> SearchAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Author> query = dbContext.Authors.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(author => author.IsActive);
        }

        string? normalizedKeyword = NormalizeKeyword(keyword);
        if (normalizedKeyword is not null)
        {
            string searchPattern = CreateLikePattern(normalizedKeyword);
            query = query.Where(author =>
                EF.Functions.Like(author.FullName, searchPattern, @"\")
                || (author.Nationality != null
                    && EF.Functions.Like(
                        author.Nationality,
                        searchPattern,
                        @"\")));
        }

        return await query
            .OrderBy(author => author.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Author?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Authors
            .AsNoTracking()
            .SingleOrDefaultAsync(author => author.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string fullName,
        DateOnly? dateOfBirth,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Authors.AnyAsync(
            author =>
                EF.Functions.Collate(author.FullName, "NOCASE") == fullName
                && author.DateOfBirth == dateOfBirth
                && (!excludingId.HasValue || author.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task AddAsync(
        Author author,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Author author,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(author);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Authors.Update(author);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeKeyword(string? keyword)
    {
        return string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
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
