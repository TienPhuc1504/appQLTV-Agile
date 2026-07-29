using System.Globalization;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class DatabaseBackupService : IDatabaseBackupService, IDisposable
{
    private const string DatabaseEntityName = "Database";
    private readonly string _databaseFilePath;
    private readonly IAuthenticationService _authenticationService;
    private readonly IActivityLogService _activityLogService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _disposed;

    public DatabaseBackupService(
        string connectionString,
        IAuthenticationService authenticationService,
        IActivityLogService activityLogService,
        TimeProvider timeProvider,
        ILogger<DatabaseBackupService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _authenticationService = authenticationService;
        _activityLogService = activityLogService;
        _timeProvider = timeProvider;
        _logger = logger;

        _databaseFilePath = GetDatabaseFilePath(connectionString);
    }

    public async Task<DatabaseTransferResult> BackupAsync(
        string destinationFilePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        DatabaseTransferResult? authorizationFailure = GetAuthorizationFailure();
        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        string destinationPath;
        try
        {
            destinationPath = NormalizeFilePath(
                destinationFilePath,
                "Vui lòng chọn vị trí lưu bản sao lưu.");
        }
        catch (ArgumentException exception)
        {
            return DatabaseTransferResult.Failure(exception.Message);
        }

        if (PathsAreEqual(destinationPath, _databaseFilePath))
        {
            return DatabaseTransferResult.Failure(
                "Không thể ghi đè trực tiếp lên database đang sử dụng.");
        }

        await _operationLock.WaitAsync(cancellationToken);
        string temporaryPath =
            $"{destinationPath}.tmp-{Guid.NewGuid():N}";
        try
        {
            string? directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await Task.Run(
                () => CopyDatabase(_databaseFilePath, temporaryPath),
                cancellationToken);
            File.Move(temporaryPath, destinationPath, overwrite: true);

            _logger.LogInformation(
                "Đã sao lưu database tới {DestinationPath}.",
                destinationPath);
            await TryLogActivityAsync(
                "DatabaseBackedUp",
                $"Đã sao lưu cơ sở dữ liệu tới {destinationPath}.");

            return DatabaseTransferResult.Success();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqliteException exception)
        {
            _logger.LogError(exception, "Sao lưu database SQLite thất bại.");
            return DatabaseTransferResult.Failure(
                GetSqliteErrorMessage(exception, "Không thể sao lưu cơ sở dữ liệu."));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Sao lưu database thất bại.");
            return DatabaseTransferResult.Failure(
                "Không thể sao lưu cơ sở dữ liệu. Vui lòng kiểm tra quyền truy cập thư mục.");
        }
        finally
        {
            TryDeleteFile(temporaryPath);
            _operationLock.Release();
        }
    }

    public async Task<DatabaseTransferResult> RestoreAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        DatabaseTransferResult? authorizationFailure = GetAuthorizationFailure();
        if (authorizationFailure is not null)
        {
            return authorizationFailure;
        }

        string sourcePath;
        try
        {
            sourcePath = NormalizeFilePath(
                sourceFilePath,
                "Vui lòng chọn file database cần phục hồi.");
        }
        catch (ArgumentException exception)
        {
            return DatabaseTransferResult.Failure(exception.Message);
        }

        if (!File.Exists(sourcePath))
        {
            return DatabaseTransferResult.Failure(
                "File sao lưu không tồn tại.");
        }

        if (PathsAreEqual(sourcePath, _databaseFilePath))
        {
            return DatabaseTransferResult.Failure(
                "File phục hồi đang là database được ứng dụng sử dụng.");
        }

        await _operationLock.WaitAsync(cancellationToken);
        string? recoveryBackupPath = null;
        try
        {
            await Task.Run(
                () => ValidateLibraryDatabase(sourcePath),
                cancellationToken);

            recoveryBackupPath = BuildRecoveryBackupPath(sourcePath);
            await Task.Run(
                () =>
                {
                    CopyDatabase(_databaseFilePath, recoveryBackupPath);
                    CopyDatabase(sourcePath, _databaseFilePath);
                },
                cancellationToken);

            _logger.LogWarning(
                "Đã phục hồi database từ {SourcePath}. Bản khôi phục an toàn: {RecoveryPath}.",
                sourcePath,
                recoveryBackupPath);
            await TryLogActivityAsync(
                "DatabaseRestored",
                $"Đã phục hồi cơ sở dữ liệu từ {sourcePath}.");

            return DatabaseTransferResult.Success(recoveryBackupPath);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqliteException exception)
        {
            await TryRecoverDatabaseAsync(recoveryBackupPath);
            _logger.LogError(exception, "Phục hồi database SQLite thất bại.");
            return DatabaseTransferResult.Failure(
                GetSqliteErrorMessage(
                    exception,
                    "Không thể phục hồi cơ sở dữ liệu. File sao lưu có thể không hợp lệ."));
        }
        catch (InvalidDataException exception)
        {
            _logger.LogWarning(exception, "File phục hồi không hợp lệ.");
            return DatabaseTransferResult.Failure(exception.Message);
        }
        catch (Exception exception)
        {
            await TryRecoverDatabaseAsync(recoveryBackupPath);
            _logger.LogError(exception, "Phục hồi database thất bại.");
            return DatabaseTransferResult.Failure(
                "Không thể phục hồi cơ sở dữ liệu. Database hiện tại đã được giữ an toàn nếu có thể.");
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _operationLock.Dispose();
        _disposed = true;
    }

    private static string GetDatabaseFilePath(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || string.Equals(
                builder.DataSource,
                ":memory:",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chức năng sao lưu yêu cầu SQLite database dạng file.");
        }

        return Path.GetFullPath(builder.DataSource);
    }

    private static string NormalizeFilePath(
        string filePath,
        string requiredMessage)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(requiredMessage, nameof(filePath));
        }

        try
        {
            return Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(filePath.Trim()));
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or NotSupportedException
                or PathTooLongException)
        {
            throw new ArgumentException(
                "Đường dẫn file database không hợp lệ.",
                nameof(filePath),
                exception);
        }
    }

    private static bool PathsAreEqual(string firstPath, string secondPath)
    {
        return string.Equals(
            Path.GetFullPath(firstPath),
            Path.GetFullPath(secondPath),
            StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDatabase(string sourcePath, string destinationPath)
    {
        string? destinationDirectory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        var sourceBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        var destinationBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = destinationPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            ForeignKeys = true
        };

        using var sourceConnection =
            new SqliteConnection(sourceBuilder.ConnectionString);
        using var destinationConnection =
            new SqliteConnection(destinationBuilder.ConnectionString);
        sourceConnection.Open();
        destinationConnection.Open();
        sourceConnection.BackupDatabase(destinationConnection);
    }

    private static void ValidateLibraryDatabase(string filePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        };
        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();

        using SqliteCommand integrityCommand = connection.CreateCommand();
        integrityCommand.CommandText = "PRAGMA integrity_check;";
        string? integrityResult =
            Convert.ToString(
                integrityCommand.ExecuteScalar(),
                CultureInfo.InvariantCulture);
        if (!string.Equals(
                integrityResult,
                "ok",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "File sao lưu bị lỗi hoặc không phải SQLite database hợp lệ.");
        }

        using SqliteCommand schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText =
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name IN (
                  'Roles',
                  'Employees',
                  'Readers',
                  'Authors',
                  'Categories',
                  'Publishers',
                  'Books',
                  'BookAuthors',
                  'BookCategories',
                  'BookCopies',
                  'BorrowSlips',
                  'BorrowSlipDetails',
                  'ReturnRecords',
                  'Fines',
                  'FinePayments',
                  'SystemSettings',
                  'ActivityLogs');
            """;
        long requiredTableCount = Convert.ToInt64(
            schemaCommand.ExecuteScalar(),
            CultureInfo.InvariantCulture);
        if (requiredTableCount != 17)
        {
            throw new InvalidDataException(
                "File được chọn không phải database của LibraryManagement.");
        }
    }

    private DatabaseTransferResult? GetAuthorizationFailure()
    {
        return _authenticationService.CheckPermission(Permission.BackupAndRestore)
            ? null
            : DatabaseTransferResult.Failure(
                "Bạn không có quyền sao lưu hoặc phục hồi cơ sở dữ liệu.");
    }

    private string BuildRecoveryBackupPath(string sourcePath)
    {
        string directory = Path.GetDirectoryName(_databaseFilePath)
            ?? AppContext.BaseDirectory;
        string fileName = Path.GetFileNameWithoutExtension(_databaseFilePath);
        string extension = Path.GetExtension(_databaseFilePath);
        string timestamp = _timeProvider
            .GetLocalNow()
            .ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

        string recoveryBackupPath;
        do
        {
            string uniqueSuffix = Guid.NewGuid()
                .ToString("N", CultureInfo.InvariantCulture)[..8];
            recoveryBackupPath = Path.Combine(
                directory,
                $"{fileName}.before-restore-{timestamp}-{uniqueSuffix}{extension}");
        }
        while (File.Exists(recoveryBackupPath)
               || PathsAreEqual(recoveryBackupPath, sourcePath));

        return recoveryBackupPath;
    }

    private async Task TryRecoverDatabaseAsync(string? recoveryBackupPath)
    {
        if (string.IsNullOrWhiteSpace(recoveryBackupPath)
            || !File.Exists(recoveryBackupPath))
        {
            return;
        }

        try
        {
            await Task.Run(
                () => CopyDatabase(recoveryBackupPath, _databaseFilePath));
            _logger.LogWarning(
                "Đã khôi phục database hiện tại từ bản an toàn {RecoveryPath}.",
                recoveryBackupPath);
        }
        catch (Exception exception)
        {
            _logger.LogCritical(
                exception,
                "Không thể khôi phục database từ bản an toàn {RecoveryPath}.",
                recoveryBackupPath);
        }
    }

    private async Task TryLogActivityAsync(
        string action,
        string description)
    {
        try
        {
            await _activityLogService.LogAsync(
                action,
                DatabaseEntityName,
                null,
                description,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Không thể ghi ActivityLog cho thao tác {Action}.",
                action);
        }
    }

    private static string GetSqliteErrorMessage(
        SqliteException exception,
        string defaultMessage)
    {
        return exception.SqliteErrorCode is 5 or 6
            ? "Database đang được sử dụng bởi tiến trình khác. Vui lòng đóng tiến trình đó và thử lại."
            : defaultMessage;
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // File tạm sẽ được hệ điều hành dọn dẹp nếu không thể xóa ngay.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
