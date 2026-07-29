using FluentAssertions;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class BorrowServiceTests
{
    [Fact]
    public async Task CreateBorrowSlip_WithValidRequest_ShouldCommitAllChanges()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(4, [2], "Mượn để học tập."),
            cancellationToken);
        BorrowSlip created =
            await harness.GetLatestBorrowSlipAsync(cancellationToken);
        BookCopy copy =
            await harness.GetBookCopyAsync(2, cancellationToken);
        ActivityLog activity =
            await harness.GetLatestBorrowActivityAsync(cancellationToken);

        result.Succeeded.Should().BeTrue();
        created.ReaderId.Should().Be(4);
        created.EmployeeId.Should().Be(1);
        created.Status.Should().Be(BorrowSlipStatus.Active);
        created.BorrowDate.Should().Be(
            DateOnly.FromDateTime(DateTime.Today));
        created.ExpectedReturnDate.Should().Be(
            DateOnly.FromDateTime(DateTime.Today).AddDays(14));
        created.Details.Should().ContainSingle();
        created.Details.Single().BookCopyId.Should().Be(2);
        created.Details.Single().Status.Should()
            .Be(BorrowSlipDetailStatus.Borrowing);
        copy.Status.Should().Be(BookCopyStatus.Borrowed);
        activity.EmployeeId.Should().Be(1);
        activity.EntityName.Should().Be(nameof(BorrowSlip));
        activity.EntityId.Should().Be(
            created.Id.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithLockedReader_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        await harness.SetReaderStatusAsync(
            4,
            ReaderStatus.Locked,
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(4, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bị khóa");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithExpiredCard_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        await harness.SetReaderExpirationAsync(
            4,
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(4, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Thẻ độc giả đã hết hạn.");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WhenMaximumReached_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        await harness.SetSettingAsync(
            SystemSettingKeys.MaximumBorrowedBooks,
            "1",
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(1, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tối đa 1");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithUnavailableCopy_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(4, [1]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có sẵn");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithInactiveBook_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        await harness.SetBookActiveAsync(
            1,
            isActive: false,
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(4, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có sẵn");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithOverdueBook_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(2, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("sách quá hạn");
    }

    [Fact]
    public async Task ValidateBorrowRequest_WithOutstandingFine_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result =
            await service.ValidateBorrowRequestAsync(
                new BorrowCreateRequest(3, [2]),
                cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().ContainEquivalentOf("tiền phạt");
    }

    [Fact]
    public async Task CreateBorrowSlip_ConcurrentSameCopy_ShouldAllowOnlyOne()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        Task<OperationResult> first = service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(4, [2]),
            cancellationToken);
        Task<OperationResult> second = service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(5, [2]),
            cancellationToken);
        OperationResult[] results =
            await Task.WhenAll(first, second);

        results.Count(item => item.Succeeded).Should().Be(1);
        results.Count(item => !item.Succeeded).Should().Be(1);
        (await harness.CountBorrowDetailsForCopyAsync(2, cancellationToken))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task CreateBorrowSlip_WhenPersistenceFails_ShouldRollbackCopyAndSlip()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(
                cancellationToken,
                currentEmployeeId: 999);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();
        int borrowSlipCountBefore =
            await harness.CountBorrowSlipsAsync(cancellationToken);

        OperationResult result = await service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(4, [2]),
            cancellationToken);
        BookCopy copy =
            await harness.GetBookCopyAsync(2, cancellationToken);
        int borrowSlipCountAfter =
            await harness.CountBorrowSlipsAsync(cancellationToken);

        result.Succeeded.Should().BeFalse();
        copy.Status.Should().Be(BookCopyStatus.Available);
        borrowSlipCountAfter.Should().Be(borrowSlipCountBefore);
    }

    [Fact]
    public async Task BorrowService_WithoutPermission_ShouldDenyWrite()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(
                cancellationToken,
                canManageBorrowing: false);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(4, [2]),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền quản lý mượn sách");
    }

    [Fact]
    public async Task BorrowService_WithoutCurrentUser_ShouldDenyWrite()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(
                cancellationToken,
                hasCurrentUser: false);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.CreateBorrowSlipAsync(
            new BorrowCreateRequest(4, [2]),
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Phiên đăng nhập");
    }

    [Fact]
    public async Task BorrowQueries_ShouldReturnCreatedSlipAndActiveDetails()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();
        (await service.CreateBorrowSlipAsync(
                new BorrowCreateRequest(4, [2]),
                cancellationToken))
            .Succeeded.Should().BeTrue();
        BorrowSlip latest =
            await harness.GetLatestBorrowSlipAsync(cancellationToken);

        BorrowSlipDto? detail =
            await service.GetBorrowSlipAsync(latest.Id, cancellationToken);
        PagedResult<BorrowSlipListItemDto> active =
            await service.GetActiveBorrowSlipsAsync(
                new BorrowSlipSearchRequest(ReaderId: 4),
                cancellationToken);
        IReadOnlyList<BorrowSlipDetailDto> readerBorrows =
            await service.GetReaderActiveBorrowsAsync(
                4,
                cancellationToken);

        detail.Should().NotBeNull();
        detail!.Details.Should().ContainSingle();
        active.Items.Should().ContainSingle();
        readerBorrows.Should().ContainSingle();
        readerBorrows[0].BookCopyId.Should().Be(2);
    }

    [Fact]
    public async Task RenewBorrowedBook_WithValidData_ShouldExtendDueDate()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        DateOnly originalDueDate =
            DateOnly.FromDateTime(DateTime.Today).AddDays(2);
        await harness.PrepareRenewalDetailAsync(
            1,
            originalDueDate,
            renewalCount: 0,
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.RenewBorrowedBookAsync(
            1,
            cancellationToken);
        BorrowSlipDetail renewed =
            await harness.GetBorrowDetailAsync(1, cancellationToken);

        result.Succeeded.Should().BeTrue();
        renewed.RenewalCount.Should().Be(1);
        renewed.ExpectedReturnDate.Should().Be(originalDueDate.AddDays(7));
        (await harness.CountActivitiesAsync(
                "BorrowRenewed",
                cancellationToken))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task RenewBorrowedBook_WhenMaximumReached_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        DateOnly originalDueDate =
            DateOnly.FromDateTime(DateTime.Today).AddDays(2);
        await harness.PrepareRenewalDetailAsync(
            1,
            originalDueDate,
            renewalCount: 2,
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.RenewBorrowedBookAsync(
            1,
            cancellationToken);
        BorrowSlipDetail unchanged =
            await harness.GetBorrowDetailAsync(1, cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tối đa 2 lần");
        unchanged.RenewalCount.Should().Be(2);
        unchanged.ExpectedReturnDate.Should().Be(originalDueDate);
    }

    [Fact]
    public async Task RenewBorrowedBook_WhenOverdue_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await BorrowServiceHarness.CreateAsync(cancellationToken);
        DateOnly overdueDate =
            DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        await harness.PrepareRenewalDetailAsync(
            1,
            overdueDate,
            renewalCount: 0,
            cancellationToken);
        IBorrowService service =
            harness.Provider.GetRequiredService<IBorrowService>();

        OperationResult result = await service.RenewBorrowedBookAsync(
            1,
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quá hạn");
    }

    private sealed class BorrowServiceHarness : IAsyncDisposable
    {
        private BorrowServiceHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<BorrowServiceHarness> CreateAsync(
            CancellationToken cancellationToken,
            bool canManageBorrowing = true,
            bool hasCurrentUser = true,
            int currentEmployeeId = 1)
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            string databasePath =
                Path.Combine(runtimeDirectory, "Library.db");
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
                new BorrowAuthenticationServiceStub(
                    canManageBorrowing,
                    hasCurrentUser,
                    currentEmployeeId));
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
                await factory.CreateDbContextAsync(cancellationToken);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            return new BorrowServiceHarness(provider, runtimeDirectory);
        }

        public async Task SetReaderStatusAsync(
            int readerId,
            ReaderStatus status,
            CancellationToken cancellationToken)
        {
            await ExecuteDbAsync(
                async dbContext =>
                {
                    Reader reader = await dbContext.Readers.SingleAsync(
                        item => item.Id == readerId,
                        cancellationToken);
                    reader.Status = status;
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);
        }

        public async Task SetReaderExpirationAsync(
            int readerId,
            DateOnly expirationDate,
            CancellationToken cancellationToken)
        {
            await ExecuteDbAsync(
                async dbContext =>
                {
                    Reader reader = await dbContext.Readers.SingleAsync(
                        item => item.Id == readerId,
                        cancellationToken);
                    reader.ExpirationDate = expirationDate;
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);
        }

        public async Task SetSettingAsync(
            string key,
            string value,
            CancellationToken cancellationToken)
        {
            await ExecuteDbAsync(
                async dbContext =>
                {
                    SystemSetting setting =
                        await dbContext.SystemSettings.SingleAsync(
                            item => item.Key == key,
                            cancellationToken);
                    setting.Value = value;
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);
        }

        public async Task SetBookActiveAsync(
            int bookId,
            bool isActive,
            CancellationToken cancellationToken)
        {
            await ExecuteDbAsync(
                async dbContext =>
                {
                    Book book = await dbContext.Books.SingleAsync(
                        item => item.Id == bookId,
                        cancellationToken);
                    book.IsActive = isActive;
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);
        }

        public async Task<BorrowSlip> GetLatestBorrowSlipAsync(
            CancellationToken cancellationToken)
        {
            BorrowSlip? result = null;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.BorrowSlips
                        .AsNoTracking()
                        .Include(item => item.Details)
                        .OrderByDescending(item => item.Id)
                        .FirstAsync(cancellationToken);
                },
                cancellationToken);
            return result!;
        }

        public async Task<BookCopy> GetBookCopyAsync(
            int bookCopyId,
            CancellationToken cancellationToken)
        {
            BookCopy? result = null;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.BookCopies
                        .AsNoTracking()
                        .SingleAsync(
                            item => item.Id == bookCopyId,
                            cancellationToken);
                },
                cancellationToken);
            return result!;
        }

        public async Task<ActivityLog> GetLatestBorrowActivityAsync(
            CancellationToken cancellationToken)
        {
            ActivityLog? result = null;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.ActivityLogs
                        .AsNoTracking()
                        .Where(item => item.Action == "BorrowCreated")
                        .OrderByDescending(item => item.Id)
                        .FirstAsync(cancellationToken);
                },
                cancellationToken);
            return result!;
        }

        public async Task<int> CountBorrowDetailsForCopyAsync(
            int bookCopyId,
            CancellationToken cancellationToken)
        {
            int result = 0;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.BorrowSlipDetails.CountAsync(
                        item =>
                            item.BookCopyId == bookCopyId
                            && item.Id > 3,
                        cancellationToken);
                },
                cancellationToken);
            return result;
        }

        public async Task<int> CountBorrowSlipsAsync(
            CancellationToken cancellationToken)
        {
            int result = 0;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.BorrowSlips.CountAsync(
                        cancellationToken);
                },
                cancellationToken);
            return result;
        }

        public async Task PrepareRenewalDetailAsync(
            int detailId,
            DateOnly expectedReturnDate,
            int renewalCount,
            CancellationToken cancellationToken)
        {
            await ExecuteDbAsync(
                async dbContext =>
                {
                    BorrowSlipDetail detail =
                        await dbContext.BorrowSlipDetails
                            .Include(item => item.BookCopy)
                            .Include(item => item.BorrowSlip)
                                .ThenInclude(item => item.Reader)
                            .SingleAsync(
                                item => item.Id == detailId,
                                cancellationToken);
                    detail.Status = BorrowSlipDetailStatus.Borrowing;
                    detail.ActualReturnDate = null;
                    detail.ExpectedReturnDate = expectedReturnDate;
                    detail.RenewalCount = renewalCount;
                    detail.BookCopy.Status = BookCopyStatus.Borrowed;
                    detail.BorrowSlip.Status = BorrowSlipStatus.Active;
                    detail.BorrowSlip.ExpectedReturnDate = expectedReturnDate;
                    detail.BorrowSlip.Reader.Status = ReaderStatus.Active;
                    detail.BorrowSlip.Reader.ExpirationDate =
                        DateOnly.FromDateTime(DateTime.Today).AddMonths(6);
                    await dbContext.SaveChangesAsync(cancellationToken);
                },
                cancellationToken);
        }

        public async Task<BorrowSlipDetail> GetBorrowDetailAsync(
            int detailId,
            CancellationToken cancellationToken)
        {
            BorrowSlipDetail? result = null;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.BorrowSlipDetails
                        .AsNoTracking()
                        .SingleAsync(
                            item => item.Id == detailId,
                            cancellationToken);
                },
                cancellationToken);
            return result!;
        }

        public async Task<int> CountActivitiesAsync(
            string action,
            CancellationToken cancellationToken)
        {
            int result = 0;
            await ExecuteDbAsync(
                async dbContext =>
                {
                    result = await dbContext.ActivityLogs.CountAsync(
                        item => item.Action == action,
                        cancellationToken);
                },
                cancellationToken);
            return result;
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

        private async Task ExecuteDbAsync(
            Func<LibraryDbContext, Task> action,
            CancellationToken cancellationToken)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(cancellationToken);
            await action(dbContext);
        }
    }

    private sealed class BorrowAuthenticationServiceStub(
        bool canManageBorrowing,
        bool hasCurrentUser,
        int currentEmployeeId)
        : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure(
                    "Không sử dụng trong kiểm thử."));
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

        public CurrentUser? GetCurrentUser()
        {
            return hasCurrentUser
                ? new CurrentUser(
                    currentEmployeeId,
                    $"NV{currentEmployeeId:0000}",
                    "Nhân viên kiểm thử",
                    "test.user",
                    "Administrator")
                : null;
        }

        public bool CheckPermission(Permission permission)
        {
            return canManageBorrowing
                && permission == Permission.ManageBorrowing;
        }
    }
}
