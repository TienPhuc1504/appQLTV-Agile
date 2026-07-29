using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IReaderService
{
    Task<PagedResult<ReaderListItemDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ReaderListItemDto>> SearchAsync(
        ReaderSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ReaderDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        ReaderUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        ReaderUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> LockAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UnlockAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> RenewCardAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<DateOnly> GetSuggestedExpirationDateAsync(
        DateOnly registeredAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReaderBorrowHistoryDto>> GetBorrowingHistoryAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReaderFineDto>> GetOutstandingFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ValidateBorrowEligibilityAsync(
        int readerId,
        DateOnly? evaluationDate = null,
        CancellationToken cancellationToken = default);
}
