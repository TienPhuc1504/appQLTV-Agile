using LibraryManagement.Core.Interfaces;

namespace LibraryManagement.Infrastructure.Services;

public sealed class BookCoverStorageService : IBookCoverStorageService
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaximumFileSize = 10 * 1024 * 1024;
    private readonly string _storageDirectory;

    public BookCoverStorageService(string storageDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        _storageDirectory = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(storageDirectory));
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<string> SaveAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        string fullSourcePath = Path.GetFullPath(sourceFilePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new FileNotFoundException(
                "Không tìm thấy tệp ảnh bìa đã chọn.",
                fullSourcePath);
        }

        string extension = Path.GetExtension(fullSourcePath);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                "Ảnh bìa phải có định dạng JPG, JPEG, PNG hoặc WEBP.");
        }

        var fileInfo = new FileInfo(fullSourcePath);
        if (fileInfo.Length > MaximumFileSize)
        {
            throw new InvalidOperationException(
                "Dung lượng ảnh bìa không được vượt quá 10 MB.");
        }

        string destinationPath = Path.Combine(
            _storageDirectory,
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        try
        {
            await using FileStream source = new(
                fullSourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using FileStream destination = new(
                destinationPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                FileOptions.Asynchronous);
            await source.CopyToAsync(destination, cancellationToken);
            return destinationPath;
        }
        catch
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            throw;
        }
    }

    public Task DeleteAsync(
        string storedFilePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storedFilePath))
        {
            return Task.CompletedTask;
        }

        string fullPath = Path.GetFullPath(storedFilePath);
        string relativePath = Path.GetRelativePath(_storageDirectory, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal)
            || Path.IsPathRooted(relativePath))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
