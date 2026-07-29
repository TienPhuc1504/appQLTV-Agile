using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IDatabaseBackupService
{
    Task<DatabaseTransferResult> BackupAsync(
        string destinationFilePath,
        CancellationToken cancellationToken = default);

    Task<DatabaseTransferResult> RestoreAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);
}
