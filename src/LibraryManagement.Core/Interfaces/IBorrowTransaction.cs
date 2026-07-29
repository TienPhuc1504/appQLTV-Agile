using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBorrowTransaction : IAsyncDisposable
{
    Task<BorrowValidationSnapshot> GetValidationSnapshotAsync(
        int readerId,
        IReadOnlyCollection<int> bookCopyIds,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default);

    Task<RenewalSnapshot?> GetRenewalSnapshotAsync(
        int borrowSlipDetailId,
        CancellationToken cancellationToken = default);

    Task PersistAsync(
        BorrowSlip borrowSlip,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task PersistRenewalAsync(
        int borrowSlipDetailId,
        DateOnly newExpectedReturnDate,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);
}
