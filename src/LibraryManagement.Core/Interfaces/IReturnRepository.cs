using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IReturnRepository
{
    Task<IReadOnlyList<ReturnLookupDto>> SearchOutstandingAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    Task<ReturnTransactionSnapshot> GetSnapshotAsync(
        IReadOnlyCollection<int> borrowSlipDetailIds,
        CancellationToken cancellationToken = default);

    Task<IReturnTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<bool> UpdateBorrowSlipStatusAsync(
        int borrowSlipId,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default);
}
