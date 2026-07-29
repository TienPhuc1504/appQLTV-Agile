using FluentAssertions;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Security;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class DatabaseBackupServiceTests
{
    [Fact]
    public async Task BackupAndRestore_ByAdministrator_ShouldRestoreOriginalData()
    {
        await using DatabaseBackupHarness harness =
            await DatabaseBackupHarness.CreateAsync();
        harness.SetCurrentUser(RoleNames.Administrator);
        IDatabaseBackupService service =
            harness.Provider.GetRequiredService<IDatabaseBackupService>();
        string backupPath = Path.Combine(
            harness.RuntimeDirectory,
            "library-backup.db");

        DatabaseTransferResult backupResult = await service.BackupAsync(
            backupPath,
            TestContext.Current.CancellationToken);
        await harness.AddCategoryAsync("Dữ liệu sau sao lưu");

        DatabaseTransferResult restoreResult = await service.RestoreAsync(
            backupPath,
            TestContext.Current.CancellationToken);

        backupResult.Succeeded.Should().BeTrue();
        File.Exists(backupPath).Should().BeTrue();
        restoreResult.Succeeded.Should().BeTrue();
        restoreResult.RecoveryBackupPath.Should().NotBeNullOrWhiteSpace();
        File.Exists(restoreResult.RecoveryBackupPath!).Should().BeTrue();
        (await harness.CategoryExistsAsync("Dữ liệu sau sao lưu"))
            .Should()
            .BeFalse();
    }

    [Fact]
    public async Task Backup_ByLibrarian_ShouldBeDenied()
    {
        await using DatabaseBackupHarness harness =
            await DatabaseBackupHarness.CreateAsync();
        harness.SetCurrentUser(RoleNames.Librarian);
        IDatabaseBackupService service =
            harness.Provider.GetRequiredService<IDatabaseBackupService>();
        string backupPath = Path.Combine(
            harness.RuntimeDirectory,
            "denied-backup.db");

        DatabaseTransferResult result = await service.BackupAsync(
            backupPath,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có quyền");
        File.Exists(backupPath).Should().BeFalse();
    }

    [Fact]
    public async Task Backup_WhenActivityLogFails_ShouldStillReportSuccess()
    {
        await using DatabaseBackupHarness harness =
            await DatabaseBackupHarness.CreateAsync(
                activityLogService: new CancelledActivityLogService());
        harness.SetCurrentUser(RoleNames.Administrator);
        IDatabaseBackupService service =
            harness.Provider.GetRequiredService<IDatabaseBackupService>();
        string backupPath = Path.Combine(
            harness.RuntimeDirectory,
            "activity-log-failure-backup.db");

        DatabaseTransferResult result = await service.BackupAsync(
            backupPath,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        File.Exists(backupPath).Should().BeTrue();
    }

    [Fact]
    public async Task Restore_WithForeignSqliteDatabase_ShouldFailValidation()
    {
        await using DatabaseBackupHarness harness =
            await DatabaseBackupHarness.CreateAsync();
        harness.SetCurrentUser(RoleNames.Administrator);
        string foreignDatabasePath = Path.Combine(
            harness.RuntimeDirectory,
            "foreign.db");
        await CreateForeignDatabaseAsync(foreignDatabasePath);
        await harness.AddCategoryAsync("Dữ liệu phải được giữ nguyên");
        IDatabaseBackupService service =
            harness.Provider.GetRequiredService<IDatabaseBackupService>();

        DatabaseTransferResult result = await service.RestoreAsync(
            foreignDatabasePath,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should()
            .Be("File được chọn không phải database của LibraryManagement.");
        (await harness.CategoryExistsAsync("Dữ liệu phải được giữ nguyên"))
            .Should()
            .BeTrue();
    }

    private static async Task CreateForeignDatabaseAsync(string filePath)
    {
        await using var connection =
            new SqliteConnection($"Data Source={filePath}");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE Example (Id INTEGER PRIMARY KEY);";
        await command.ExecuteNonQueryAsync(
            TestContext.Current.CancellationToken);
    }

    private sealed class DatabaseBackupHarness : IAsyncDisposable
    {
        private DatabaseBackupHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        public string RuntimeDirectory { get; }

        public static async Task<DatabaseBackupHarness> CreateAsync(
            IActivityLogService? activityLogService = null)
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            string databasePath =
                Path.Combine(runtimeDirectory, "LibraryManagement.db");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryDatabase"] =
                            $"Data Source={databasePath};Foreign Keys=True",
                        ["Security:BCryptWorkFactor"] = "4",
                        ["Storage:LoginPreferencesFile"] =
                            Path.Combine(runtimeDirectory, "login.json"),
                        ["Storage:BookCoversDirectory"] =
                            Path.Combine(runtimeDirectory, "BookCovers")
                    })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            if (activityLogService is not null)
            {
                services.AddSingleton(activityLogService);
            }

            ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
            IDbContextFactory<LibraryDbContext> factory =
                provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(
                    TestContext.Current.CancellationToken);
            await dbContext.Database.EnsureCreatedAsync(
                TestContext.Current.CancellationToken);
            return new DatabaseBackupHarness(provider, runtimeDirectory);
        }

        public void SetCurrentUser(string roleName)
        {
            Provider.GetRequiredService<ICurrentUserService>().SetCurrentUser(
                new CurrentUser(
                    1,
                    "NV0001",
                    "Người dùng kiểm thử",
                    "test.user",
                    roleName));
        }

        public async Task AddCategoryAsync(string name)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(
                    TestContext.Current.CancellationToken);
            dbContext.Categories.Add(
                new Category
                {
                    Name = name,
                    IsActive = true
                });
            await dbContext.SaveChangesAsync(
                TestContext.Current.CancellationToken);
        }

        public async Task<bool> CategoryExistsAsync(string name)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(
                    TestContext.Current.CancellationToken);
            return await dbContext.Categories
                .AsNoTracking()
                .AnyAsync(
                    category => category.Name == name,
                    TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(RuntimeDirectory))
            {
                Directory.Delete(RuntimeDirectory, recursive: true);
            }
        }
    }

    private sealed class CancelledActivityLogService : IActivityLogService
    {
        public Task LogAsync(
            string action,
            string entityName,
            string? entityId,
            string description,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException(
                new OperationCanceledException("Mô phỏng lỗi ActivityLog."));
        }

        public Task<PagedResult<LibraryManagement.Core.DTOs.ActivityLogDto>>
            GetAllAsync(
                LibraryManagement.Core.DTOs.ActivityLogSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PagedResult<LibraryManagement.Core.DTOs.ActivityLogDto>>
            SearchAsync(
                LibraryManagement.Core.DTOs.ActivityLogSearchRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
