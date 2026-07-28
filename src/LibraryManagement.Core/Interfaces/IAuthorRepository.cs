using LibraryManagement.Core.Entities;

namespace LibraryManagement.Core.Interfaces;

public interface IAuthorRepository
{
    Task<IReadOnlyList<Author>> SearchAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<Author?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string fullName,
        DateOnly? dateOfBirth,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Author author,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Author author,
        CancellationToken cancellationToken = default);
}
