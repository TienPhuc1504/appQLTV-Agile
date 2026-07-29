using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IFineTransaction : IAsyncDisposable
{
    Task<FineTransactionSnapshot?> GetSnapshotAsync(
        int fineId,
        CancellationToken cancellationToken = default);

    Task PersistFineAsync(
        Fine fine,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task PersistPaymentAsync(
        FinePayment payment,
        decimal newPaidAmount,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task PersistWaiverAsync(
        int fineId,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);
}
