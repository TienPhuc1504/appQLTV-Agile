using LibraryManagement.Core.Constants;
using LibraryManagement.Core.Enums;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Initialization;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<LibraryDbContext> _dbContextFactory;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(
        IDbContextFactory<LibraryDbContext> dbContextFactory,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Bắt đầu khởi tạo cơ sở dữ liệu.");

            await using LibraryDbContext dbContext =
                await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            EnsureDatabaseDirectoryExists(dbContext.Database.GetDbConnection().DataSource);
            await dbContext.Database.MigrateAsync(cancellationToken);

            await LogRuntimeDatabaseSnapshotAsync(
                dbContext,
                cancellationToken);
            _logger.LogInformation("Khởi tạo cơ sở dữ liệu thành công.");
        }
        catch (SqliteException exception)
        {
            _logger.LogError(
                exception,
                "Không thể truy cập hoặc khởi tạo cơ sở dữ liệu SQLite.");

            throw new DatabaseInitializationException(
                "Không thể khởi tạo cơ sở dữ liệu SQLite.",
                exception);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Khởi tạo cơ sở dữ liệu thất bại.");

            throw new DatabaseInitializationException(
                "Khởi tạo cơ sở dữ liệu thất bại.",
                exception);
        }
    }

    private static void EnsureDatabaseDirectoryExists(string dataSource)
    {
        string? databaseDirectory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }
    }

    private async Task LogRuntimeDatabaseSnapshotAsync(
        LibraryDbContext dbContext,
        CancellationToken cancellationToken)
    {
        string dataSource = Path.GetFullPath(
            dbContext.Database.GetDbConnection().DataSource);
        int readerCount = await dbContext.Readers
            .AsNoTracking()
            .CountAsync(cancellationToken);
        bool hasDg0004 = await dbContext.Readers
            .AsNoTracking()
            .AnyAsync(
                reader => reader.ReaderCode == "DG0004",
                cancellationToken);
        BookCopyStatus? bs00102Status = await dbContext.BookCopies
            .AsNoTracking()
            .Where(copy => copy.CopyCode == "BS001-02")
            .Select(copy => (BookCopyStatus?)copy.Status)
            .SingleOrDefaultAsync(cancellationToken);
        string[] requiredSettingKeys =
        [
            SystemSettingKeys.MaximumBorrowedBooks,
            SystemSettingKeys.DefaultBorrowDays,
            SystemSettingKeys.MaximumRenewalCount,
            SystemSettingKeys.OverdueFinePerDay,
            SystemSettingKeys.MaximumOutstandingFineAmount
        ];
        Dictionary<string, string> settings = await dbContext.SystemSettings
            .AsNoTracking()
            .Where(setting => requiredSettingKeys.Contains(setting.Key))
            .ToDictionaryAsync(
                setting => setting.Key,
                setting => setting.Value,
                cancellationToken);

        _logger.LogInformation(
            "Runtime database: ConnectionString={ConnectionString}, "
            + "DataSource={DataSource}, ReaderCount={ReaderCount}, "
            + "HasDG0004={HasDG0004}, HasBS00102={HasBS00102}, "
            + "BS00102Status={BS00102Status}, Settings={Settings}.",
            dbContext.Database.GetConnectionString(),
            dataSource,
            readerCount,
            hasDg0004,
            bs00102Status.HasValue,
            bs00102Status?.ToString() ?? "NotFound",
            string.Join(
                ", ",
                requiredSettingKeys.Select(
                    key => $"{key}="
                        + (settings.TryGetValue(key, out string? value)
                            ? value
                            : "<missing>"))));
    }
}
