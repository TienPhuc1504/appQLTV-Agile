using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Initialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Integration;

public sealed class BorrowRuntimeIntegrationTests
{
    [Fact]
    public async Task RuntimeServices_WithTemporarySqlite_ShouldCompleteBorrowFlow()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using BorrowRuntimeHarness harness =
            await BorrowRuntimeHarness.CreateAsync(cancellationToken);

        IAuthenticationService authenticationService =
            harness.Provider.GetRequiredService<IAuthenticationService>();
        AuthenticationResult loginResult =
            await authenticationService.LoginAsync(
                "admin",
                "Admin@123",
                cancellationToken);
        loginResult.Succeeded.Should().BeTrue();

        IReaderService readerService =
            harness.Provider.GetRequiredService<IReaderService>();
        PagedResult<ReaderListItemDto> readerResult =
            await readerService.SearchAsync(
                new ReaderSearchRequest(Keyword: "  DG0004  "),
                cancellationToken);
        ReaderListItemDto reader = readerResult.Items.Should()
            .ContainSingle()
            .Which;

        IBorrowService borrowService =
            harness.Provider.GetRequiredService<IBorrowService>();
        OperationResult eligibility =
            await borrowService.ValidateReaderEligibilityAsync(
                reader.Id,
                cancellationToken);
        eligibility.Succeeded.Should().BeTrue();

        IBookCopyService bookCopyService =
            harness.Provider.GetRequiredService<IBookCopyService>();
        PagedResult<BookCopyDto> copyResult =
            await bookCopyService.SearchAsync(
                new BookCopySearchRequest(
                    Keyword: "  BS001-02  ",
                    Status: BookCopyStatus.Available),
                cancellationToken);
        BookCopyDto copy = copyResult.Items.Should()
            .ContainSingle(item => item.CopyCode == "BS001-02")
            .Which;

        OperationResult createResult =
            await borrowService.CreateBorrowSlipAsync(
                new BorrowCreateRequest(
                    reader.Id,
                    [copy.Id],
                    "Integration test runtime flow"),
                cancellationToken);
        createResult.Succeeded.Should().BeTrue();

        IDbContextFactory<LibraryDbContext> dbContextFactory =
            harness.Provider.GetRequiredService<
                IDbContextFactory<LibraryDbContext>>();
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        BorrowSlip borrowSlip = await dbContext.BorrowSlips
            .AsNoTracking()
            .Include(item => item.Details)
            .SingleAsync(
                item =>
                    item.ReaderId == reader.Id
                    && item.Notes == "Integration test runtime flow",
                cancellationToken);
        borrowSlip.Details.Should().ContainSingle(
            detail => detail.BookCopyId == copy.Id);
        BookCopyStatus persistedCopyStatus = await dbContext.BookCopies
            .AsNoTracking()
            .Where(item => item.Id == copy.Id)
            .Select(item => item.Status)
            .SingleAsync(cancellationToken);
        persistedCopyStatus.Should().Be(BookCopyStatus.Borrowed);
        bool activityLogExists = await dbContext.ActivityLogs
            .AsNoTracking()
            .AnyAsync(
                log =>
                    log.Action == "BorrowCreated"
                    && log.EntityName == nameof(BorrowSlip),
                cancellationToken);
        activityLogExists.Should().BeTrue();
    }

    private sealed class BorrowRuntimeHarness : IAsyncDisposable
    {
        private readonly string _runtimeDirectory;

        private BorrowRuntimeHarness(
            string runtimeDirectory,
            ServiceProvider provider)
        {
            _runtimeDirectory = runtimeDirectory;
            Provider = provider;
        }

        public ServiceProvider Provider { get; }

        public static async Task<BorrowRuntimeHarness> CreateAsync(
            CancellationToken cancellationToken)
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                "BorrowRuntime",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            string databasePath = Path.Combine(
                runtimeDirectory,
                "LibraryManagement.db");
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryDatabase"] =
                            $"Data Source={databasePath};Foreign Keys=True",
                        ["Security:BCryptWorkFactor"] = "4",
                        ["Storage:LoginPreferencesFile"] = Path.Combine(
                            runtimeDirectory,
                            "login-preferences.json"),
                        ["Storage:BookCoversDirectory"] = Path.Combine(
                            runtimeDirectory,
                            "BookCovers")
                    })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddInfrastructure(configuration);
            ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
            var harness = new BorrowRuntimeHarness(
                runtimeDirectory,
                provider);

            try
            {
                IDatabaseInitializer initializer =
                    provider.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeAsync(cancellationToken);
                return harness;
            }
            catch
            {
                await harness.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_runtimeDirectory))
            {
                Directory.Delete(_runtimeDirectory, recursive: true);
            }
        }
    }
}
