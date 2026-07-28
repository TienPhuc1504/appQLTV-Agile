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
}
