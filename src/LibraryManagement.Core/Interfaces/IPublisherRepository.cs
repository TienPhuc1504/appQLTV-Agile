using LibraryManagement.Core.Entities;

namespace LibraryManagement.Core.Interfaces;

public interface IPublisherRepository
{
    Task<IReadOnlyList<Publisher>> SearchAsync(
        string? keyword,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<Publisher?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        string name,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Publisher publisher,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Publisher publisher,
        CancellationToken cancellationToken = default);
}
