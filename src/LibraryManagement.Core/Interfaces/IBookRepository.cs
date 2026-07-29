using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBookRepository
{
    Task<PagedResult<Book>> SearchAsync(
        BookSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Book?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> BookCodeExistsAsync(
        string bookCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsbnExistsAsync(
        string isbn,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ReferenceDataExistsAsync(
        int publisherId,
        IReadOnlyCollection<int> authorIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Book book,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Book book,
        IReadOnlyCollection<int> authorIds,
        IReadOnlyCollection<int> categoryIds,
        CancellationToken cancellationToken = default);
}
