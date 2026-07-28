using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class PublisherRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IPublisherRepository
{
    public async Task<IReadOnlyList<Publisher>> SearchAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Publisher> query = dbContext.Publishers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(publisher => publisher.IsActive);
        }

        string? normalizedKeyword = NormalizeKeyword(keyword);
        if (normalizedKeyword is not null)
        {
            string searchPattern = CreateLikePattern(normalizedKeyword);
            query = query.Where(publisher =>
                EF.Functions.Like(publisher.Name, searchPattern, @"\")
                || (publisher.Email != null
                    && EF.Functions.Like(
                        publisher.Email,
                        searchPattern,
                        @"\"))
                || (publisher.PhoneNumber != null
                    && EF.Functions.Like(
                        publisher.PhoneNumber,
                        searchPattern,
                        @"\")));
        }

        return await query
            .OrderBy(publisher => publisher.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Publisher?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Publishers
            .AsNoTracking()
            .SingleOrDefaultAsync(publisher => publisher.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(
        string name,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Publishers.AnyAsync(
            publisher =>
                EF.Functions.Collate(publisher.Name, "NOCASE") == name
                && (!excludingId.HasValue || publisher.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task AddAsync(
        Publisher publisher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Publishers.Add(publisher);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Publisher publisher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publisher);

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Publishers.Update(publisher);
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
