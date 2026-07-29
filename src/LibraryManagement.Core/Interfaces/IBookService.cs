using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBookService
{
    Task<PagedResult<BookListItemDto>> SearchAsync(
        BookSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<BookDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopyDto>> GetAvailableCopiesAsync(
        int bookId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        BookUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        BookUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);
}
