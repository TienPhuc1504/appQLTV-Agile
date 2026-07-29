using LibraryManagement.Core.Entities;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBookCopyRepository
{
    Task<PagedResult<BookCopy>> SearchAsync(
        BookCopySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<BookCopy?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BorrowSlipDetail>> GetBorrowHistoryAsync(
        int bookCopyId,
        CancellationToken cancellationToken = default);

    Task<bool> CopyCodeExistsAsync(
        string copyCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveBookExistsAsync(
        int bookId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BookCopy bookCopy,
        CancellationToken cancellationToken = default);
}
