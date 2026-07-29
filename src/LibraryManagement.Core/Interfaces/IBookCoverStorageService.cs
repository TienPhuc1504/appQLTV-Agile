namespace LibraryManagement.Core.Interfaces;

public interface IBookCoverStorageService
{
    Task<string> SaveAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storedFilePath,
        CancellationToken cancellationToken = default);
}
