using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBookCopyService
{
    Task<PagedResult<BookCopyDto>> SearchAsync(
        BookCopySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<BookCopyDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookCopyBorrowHistoryDto>> GetBorrowHistoryAsync(
        int bookCopyId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        BookCopyUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        BookCopyUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ChangeStatusAsync(
        int id,
        BookCopyStatus status,
        CancellationToken cancellationToken = default);
}
