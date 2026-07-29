using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBorrowRepository
{
    Task<BorrowValidationSnapshot> GetValidationSnapshotAsync(
        int readerId,
        IReadOnlyCollection<int> bookCopyIds,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetSettingsAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default);

    Task<IBorrowTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<BorrowSlipDto?> GetBorrowSlipAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BorrowSlipListItemDto>> GetActiveBorrowSlipsAsync(
        BorrowSlipSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BorrowSlipDetailDto>> GetReaderActiveBorrowsAsync(
        int readerId,
        CancellationToken cancellationToken = default);
}
