using LibraryManagement.Core.Entities;

namespace LibraryManagement.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> SearchAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Category category,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Category category,
        CancellationToken cancellationToken = default);
}
