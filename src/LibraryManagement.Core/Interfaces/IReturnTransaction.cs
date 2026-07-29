using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IReturnTransaction : IAsyncDisposable
{
    Task<ReturnTransactionSnapshot> GetSnapshotAsync(
        IReadOnlyCollection<int> borrowSlipDetailIds,
        CancellationToken cancellationToken = default);

    Task PersistAsync(
        ReturnPersistenceCommand command,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);
}
