using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class BookServiceTests
{
    [Fact]
    public async Task BookService_CreateAndSearch_ShouldPersistRelationships()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookService service = harness.Provider.GetRequiredService<IBookService>();
        ReferenceIds references = await harness.GetReferenceIdsAsync(cancellationToken);

        OperationResult createResult = await service.CreateAsync(
            new BookUpsertRequest(
                "KT-SACH-001",
                "9780306406157",
                "Kiểm thử dịch vụ sách",
                references.PublisherId,
                2025,
                "Tiếng Việt",
                250,
                125000m,
                null,
                "Sách dùng trong kiểm thử.",
                [references.AuthorId],
                [references.CategoryId]),
            cancellationToken);
        PagedResult<BookListItemDto> searchResult = await service.SearchAsync(
            new BookSearchRequest(
                Keyword: "kiểm thử dịch vụ",
                PageNumber: 1,
                PageSize: 10),
            cancellationToken);
        BookListItemDto created = searchResult.Items.Single();
        BookDetailDto? detail =
            await service.GetByIdAsync(created.Id, cancellationToken);

        createResult.Succeeded.Should().BeTrue();
        searchResult.TotalCount.Should().Be(1);
        detail.Should().NotBeNull();
        detail!.AuthorIds.Should().ContainSingle()
            .Which.Should().Be(references.AuthorId);
        detail.CategoryIds.Should().ContainSingle()
            .Which.Should().Be(references.CategoryId);
        detail.ISBN.Should().Be("9780306406157");
    }

    [Fact]
    public async Task BookService_WithDuplicateBookCode_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookService service = harness.Provider.GetRequiredService<IBookService>();
        ReferenceIds references = await harness.GetReferenceIdsAsync(cancellationToken);
        BookUpsertRequest request = new(
            "KT-TRUNG-001",
            null,
            "Sách thứ nhất",
            references.PublisherId,
            2024,
            null,
            100,
            0,
            null,
            null,
            [references.AuthorId],
            [references.CategoryId]);

        OperationResult first = await service.CreateAsync(request, cancellationToken);
        OperationResult duplicate = await service.CreateAsync(
            request with { Title = "Sách thứ hai" },
            cancellationToken);

        first.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();
        duplicate.ErrorMessage.Should().Be("Mã sách đã tồn tại.");
    }

    [Fact]
    public async Task BookService_UpdateAndDeactivate_ShouldPreserveRelationships()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookService service = harness.Provider.GetRequiredService<IBookService>();
        ReferenceIds references = await harness.GetReferenceIdsAsync(cancellationToken);
        var createRequest = new BookUpsertRequest(
            "KT-CAPNHAT-001",
            null,
            "Sách trước cập nhật",
            references.PublisherId,
            2024,
            null,
            120,
            50000,
            null,
            null,
            [references.AuthorId],
            [references.CategoryId]);
        (await service.CreateAsync(createRequest, cancellationToken))
            .Succeeded.Should().BeTrue();
        BookListItemDto created = (await service.SearchAsync(
                new BookSearchRequest(Keyword: "KT-CAPNHAT-001"),
                cancellationToken))
            .Items.Single();

        OperationResult updateResult = await service.UpdateAsync(
            created.Id,
            createRequest with { Title = "Sách sau cập nhật" },
            cancellationToken);
        OperationResult deactivateResult =
            await service.DeactivateAsync(created.Id, cancellationToken);
        BookDetailDto? detail =
            await service.GetByIdAsync(created.Id, cancellationToken);

        updateResult.Succeeded.Should().BeTrue();
        deactivateResult.Succeeded.Should().BeTrue();
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("Sách sau cập nhật");
        detail.IsActive.Should().BeFalse();
        detail.AuthorIds.Should().ContainSingle()
            .Which.Should().Be(references.AuthorId);
        detail.CategoryIds.Should().ContainSingle()
            .Which.Should().Be(references.CategoryId);
    }

    [Fact]
    public async Task BookSearch_WithPageSize_ShouldReturnDatabasePage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookService service = harness.Provider.GetRequiredService<IBookService>();

        PagedResult<BookListItemDto> result = await service.SearchAsync(
            new BookSearchRequest(PageNumber: 999, PageSize: 10),
            cancellationToken);

        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().BeGreaterThanOrEqualTo(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task BookCoverStorage_SaveAndDelete_ShouldManageStoredFile()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCoverStorageService storage =
            harness.Provider.GetRequiredService<IBookCoverStorageService>();
        string sourcePath = Path.Combine(
            Path.GetTempPath(),
            $"book-cover-{Guid.NewGuid():N}.png");
        await File.WriteAllBytesAsync(
            sourcePath,
            [137, 80, 78, 71],
            cancellationToken);

        try
        {
            string storedPath = await storage.SaveAsync(sourcePath, cancellationToken);

            File.Exists(storedPath).Should().BeTrue();
            Path.GetExtension(storedPath).Should().Be(".png");

            await storage.DeleteAsync(storedPath, cancellationToken);
            File.Exists(storedPath).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
            }
        }
    }

    [Fact]
    public async Task BookCoverStorage_WhenCancelled_ShouldNotLeavePartialFile()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string runtimeDirectory = Path.Combine(
            Path.GetTempPath(),
            "LibraryManagement.Tests",
            Guid.NewGuid().ToString("N"));
        string storageDirectory = Path.Combine(runtimeDirectory, "BookCovers");
        string sourcePath = Path.Combine(runtimeDirectory, "source.png");
        Directory.CreateDirectory(runtimeDirectory);
        await File.WriteAllBytesAsync(
            sourcePath,
            new byte[1024 * 1024],
            cancellationToken);
        var storage = new BookCoverStorageService(storageDirectory);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        try
        {
            Func<Task> action = () =>
                storage.SaveAsync(sourcePath, cancellation.Token);

            await action.Should().ThrowAsync<OperationCanceledException>();
            Directory.EnumerateFiles(storageDirectory).Should().BeEmpty();
        }
        finally
        {
            if (Directory.Exists(runtimeDirectory))
            {
                Directory.Delete(runtimeDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BookCopyService_CreateBorrowedDirectly_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCopyService service =
            harness.Provider.GetRequiredService<IBookCopyService>();

        OperationResult result = await service.CreateAsync(
            new BookCopyUpsertRequest(
                "KT-BS-001",
                1,
                "Kệ KT-01",
                DateOnly.FromDateTime(DateTime.Today),
                PhysicalCondition.Good,
                BookCopyStatus.Borrowed,
                null),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("đang mượn");
    }

    [Fact]
    public async Task BookCopyService_CreateAndFilter_ShouldReturnPagedResult()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCopyService service =
            harness.Provider.GetRequiredService<IBookCopyService>();

        OperationResult createResult = await service.CreateAsync(
            new BookCopyUpsertRequest(
                "KT-BS-002",
                1,
                "Kệ KT-02",
                DateOnly.FromDateTime(DateTime.Today),
                PhysicalCondition.New,
                BookCopyStatus.Available,
                null),
            cancellationToken);
        PagedResult<BookCopyDto> result = await service.SearchAsync(
            new BookCopySearchRequest(
                Keyword: "kt-bs-002",
                Status: BookCopyStatus.Available,
                PageNumber: 1,
                PageSize: 10),
            cancellationToken);

        createResult.Succeeded.Should().BeTrue();
        result.TotalCount.Should().Be(1);
        result.Items.Single().CopyCode.Should().Be("KT-BS-002");
    }

    [Fact]
    public async Task BookCopyService_Update_ShouldNotModifyParentBookAuditData()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCopyService copyService =
            harness.Provider.GetRequiredService<IBookCopyService>();
        IBookService bookService =
            harness.Provider.GetRequiredService<IBookService>();
        BookDetailDto before =
            (await bookService.GetByIdAsync(1, cancellationToken))!;
        BookCopyDto copy = (await copyService.SearchAsync(
                new BookCopySearchRequest(
                    BookId: 1,
                    Status: BookCopyStatus.Available,
                    PageSize: 10),
                cancellationToken))
            .Items.First();

        OperationResult result = await copyService.UpdateAsync(
            copy.Id,
            new BookCopyUpsertRequest(
                copy.CopyCode,
                copy.BookId,
                "Kệ đã cập nhật",
                copy.ImportedAt,
                copy.PhysicalCondition,
                copy.Status,
                copy.Notes),
            cancellationToken);
        BookDetailDto after =
            (await bookService.GetByIdAsync(1, cancellationToken))!;

        result.Succeeded.Should().BeTrue();
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task BookCopyService_UpdateIdentity_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCopyService service =
            harness.Provider.GetRequiredService<IBookCopyService>();
        BookCopyDto copy = (await service.SearchAsync(
                new BookCopySearchRequest(
                    BookId: 1,
                    Status: BookCopyStatus.Available,
                    PageSize: 10),
                cancellationToken))
            .Items.First();

        OperationResult result = await service.UpdateAsync(
            copy.Id,
            new BookCopyUpsertRequest(
                $"{copy.CopyCode}-MOI",
                copy.BookId,
                copy.ShelfLocation,
                copy.ImportedAt,
                copy.PhysicalCondition,
                copy.Status,
                copy.Notes),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Không thể thay đổi mã bản sách");
    }

    [Fact]
    public async Task BookCopyService_WithInconsistentLostState_ShouldFail()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookCopyService service =
            harness.Provider.GetRequiredService<IBookCopyService>();

        OperationResult lostStatus = await service.CreateAsync(
            new BookCopyUpsertRequest(
                "KT-BS-MAT-001",
                1,
                null,
                DateOnly.FromDateTime(DateTime.Today),
                PhysicalCondition.Good,
                BookCopyStatus.Lost,
                null),
            cancellationToken);
        OperationResult lostCondition = await service.CreateAsync(
            new BookCopyUpsertRequest(
                "KT-BS-MAT-002",
                1,
                null,
                DateOnly.FromDateTime(DateTime.Today),
                PhysicalCondition.Lost,
                BookCopyStatus.Available,
                null),
            cancellationToken);

        lostStatus.Succeeded.Should().BeFalse();
        lostCondition.Succeeded.Should().BeFalse();
        lostStatus.ErrorMessage.Should().Contain("bị mất");
        lostCondition.ErrorMessage.Should().Contain("bị mất");
    }

    [Fact]
    public async Task BookAndCopyServices_ShouldReturnAvailabilityAndBorrowHistory()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using var harness = await BookServiceHarness.CreateAsync(cancellationToken);
        IBookService bookService =
            harness.Provider.GetRequiredService<IBookService>();
        IBookCopyService copyService =
            harness.Provider.GetRequiredService<IBookCopyService>();

        IReadOnlyList<BookCopyDto> availableCopies =
            await bookService.GetAvailableCopiesAsync(1, cancellationToken);
        IReadOnlyList<BookCopyBorrowHistoryDto> history =
            await copyService.GetBorrowHistoryAsync(1, cancellationToken);

        availableCopies.Should().OnlyContain(
            copy => copy.Status == BookCopyStatus.Available);
        history.Should().NotBeEmpty();
        history[0].BorrowCode.Should().NotBeNullOrWhiteSpace();
        history[0].ReaderCode.Should().NotBeNullOrWhiteSpace();
    }

    private sealed class BookServiceHarness : IAsyncDisposable
    {
        private BookServiceHarness(ServiceProvider provider, string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<BookServiceHarness> CreateAsync(
            CancellationToken cancellationToken)
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            string databasePath = Path.Combine(runtimeDirectory, "Library.db");
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
            services.AddSingleton<IAuthenticationService>(
                new AllowBookAuthenticationService());
            ServiceProvider provider = services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true
                });
            IDbContextFactory<LibraryDbContext> factory =
                provider.GetRequiredService<IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return new BookServiceHarness(provider, runtimeDirectory);
        }

        public async Task<ReferenceIds> GetReferenceIdsAsync(
            CancellationToken cancellationToken)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(cancellationToken);
            return new ReferenceIds(
                await dbContext.Publishers
                    .Where(item => item.IsActive)
                    .Select(item => item.Id)
                    .FirstAsync(cancellationToken),
                await dbContext.Authors
                    .Where(item => item.IsActive)
                    .Select(item => item.Id)
                    .FirstAsync(cancellationToken),
                await dbContext.Categories
                    .Where(item => item.IsActive)
                    .Select(item => item.Id)
                    .FirstAsync(cancellationToken));
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

    private sealed record ReferenceIds(
        int PublisherId,
        int AuthorId,
        int CategoryId);

    private sealed class AllowBookAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure("Không sử dụng trong kiểm thử."));
        }

        public void Logout()
        {
        }

        public Task<OperationResult> ChangePasswordAsync(
            string currentPassword,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public Task<OperationResult> ResetPasswordAsync(
            int employeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser() => null;

        public bool CheckPermission(Permission permission)
        {
            return permission == Permission.ManageBooks;
        }
    }
}
