using FluentAssertions;
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

public sealed class ReturnServiceTests
{
    private static readonly DateOnly Today =
        DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public async Task CalculateOverdueDays_ShouldNeverReturnNegativeValue()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        service.CalculateOverdueDays(Today, Today.AddDays(-1))
            .Should()
            .Be(0);
        service.CalculateOverdueDays(Today, Today.AddDays(4))
            .Should()
            .Be(4);
    }

    [Fact]
    public async Task ReturnBook_OnTime_ShouldNotCreateFine()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good, " Bình thường "),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.Detail.Status.Should().Be(BorrowSlipDetailStatus.Returned);
        state.Detail.ActualReturnDate.Should().Be(Today);
        state.BookCopy.Status.Should().Be(BookCopyStatus.Available);
        state.BookCopy.PhysicalCondition.Should().Be(PhysicalCondition.Good);
        state.ReturnRecord.Should().NotBeNull();
        state.ReturnRecord!.OverdueDays.Should().Be(0);
        state.ReturnRecord.Notes.Should().Be("Bình thường");
        state.Fines.Should().BeEmpty();
        state.BorrowSlip.Status.Should().Be(BorrowSlipStatus.Completed);
        state.ActivityCount.Should().Be(1);
    }

    [Fact]
    public async Task ReturnBook_Overdue_ShouldCreateCorrectFine()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-10),
            Today.AddDays(-3));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.ReturnRecord!.OverdueDays.Should().Be(3);
        state.Fines.Should().ContainSingle();
        state.Fines.Single().FineType.Should().Be(FineType.Overdue);
        state.Fines.Single().Amount.Should().Be(15000m);
        state.Fines.Single().Status.Should().Be(FineStatus.Unpaid);
    }

    [Fact]
    public async Task ReturnBook_WithExistingOverdueFine_ShouldOnlyCreateDifference()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-10),
            Today.AddDays(-3));
        await harness.AddExistingFineAsync(
            detailId,
            FineType.Overdue,
            10000m);
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        ReturnPreviewDto preview = await service.CalculateFineAsync(
            detailId,
            PhysicalCondition.Good,
            Today,
            TestContext.Current.CancellationToken);
        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        preview.OverdueFineAmount.Should().Be(5000m);
        state.Fines.Should().HaveCount(2);
        state.Fines.Sum(fine => fine.Amount).Should().Be(15000m);
    }

    [Fact]
    public async Task ReturnBook_Damaged_ShouldCreateDamageFine()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        decimal bookPrice = await harness.GetBookPriceForDetailAsync(detailId);
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Damaged),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.Detail.Status.Should().Be(BorrowSlipDetailStatus.Damaged);
        state.BookCopy.Status.Should().Be(BookCopyStatus.Damaged);
        state.Fines.Should().ContainSingle();
        state.Fines.Single().FineType.Should().Be(FineType.Damaged);
        state.Fines.Single().Amount.Should().Be(bookPrice * 0.5m);
    }

    [Fact]
    public async Task ReturnBook_Lost_ShouldCreateLostFine()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        decimal bookPrice = await harness.GetBookPriceForDetailAsync(detailId);
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Lost),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.Detail.Status.Should().Be(BorrowSlipDetailStatus.Lost);
        state.BookCopy.Status.Should().Be(BookCopyStatus.Lost);
        state.Fines.Should().ContainSingle();
        state.Fines.Single().FineType.Should().Be(FineType.Lost);
        state.Fines.Single().Amount.Should().Be(bookPrice * 2m);
    }

    [Fact]
    public async Task ReturnBook_OverdueAndDamaged_ShouldCreateTwoFines()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-10),
            Today.AddDays(-2));
        decimal bookPrice = await harness.GetBookPriceForDetailAsync(detailId);
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Damaged),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.Fines.Should().HaveCount(2);
        state.Fines.Should().Contain(
            item => item.FineType == FineType.Overdue
                && item.Amount == 10000m);
        state.Fines.Should().Contain(
            item => item.FineType == FineType.Damaged
                && item.Amount == bookPrice * 0.5m);
    }

    [Fact]
    public async Task ReturnBook_Twice_ShouldFailWithoutDuplicateRecord()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        (await service.ReturnBookAsync(
                new ReturnBookRequest(detailId, PhysicalCondition.Good),
                TestContext.Current.CancellationToken))
            .Succeeded.Should().BeTrue();
        OperationResult secondResult = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good),
            TestContext.Current.CancellationToken);

        secondResult.Succeeded.Should().BeFalse();
        secondResult.ErrorMessage.Should().Contain("đã được trả");
        (await harness.CountReturnRecordsAsync(detailId)).Should().Be(1);
    }

    [Fact]
    public async Task ReturnMultipleBooks_ShouldUpdatePartialThenCompletedStatus()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int[] detailIds = await harness.CreateBorrowSlipWithDetailsAsync(
            [2, 4],
            Today.AddDays(-5),
            Today.AddDays(2));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult firstResult = await service.ReturnMultipleBooksAsync(
            new ReturnMultipleBooksRequest(
                [new ReturnBookRequest(detailIds[0], PhysicalCondition.Good)],
                Today),
            TestContext.Current.CancellationToken);
        BorrowSlipStatus partialStatus =
            await harness.GetBorrowSlipStatusForDetailAsync(detailIds[0]);
        OperationResult secondResult = await service.ReturnMultipleBooksAsync(
            new ReturnMultipleBooksRequest(
                [new ReturnBookRequest(detailIds[1], PhysicalCondition.Worn)],
                Today),
            TestContext.Current.CancellationToken);
        BorrowSlipStatus completedStatus =
            await harness.GetBorrowSlipStatusForDetailAsync(detailIds[0]);

        firstResult.Succeeded.Should().BeTrue();
        partialStatus.Should().Be(BorrowSlipStatus.PartiallyReturned);
        secondResult.Succeeded.Should().BeTrue();
        completedStatus.Should().Be(BorrowSlipStatus.Completed);
    }

    [Fact]
    public async Task ReturnMultipleBooks_WithReturnDateBeforeBorrow_ShouldFail()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-3),
            Today.AddDays(1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnMultipleBooksAsync(
            new ReturnMultipleBooksRequest(
                [new ReturnBookRequest(detailId, PhysicalCondition.Good)],
                Today.AddDays(-4)),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ngày mượn");
        (await harness.CountReturnRecordsAsync(detailId)).Should().Be(0);
    }

    [Fact]
    public async Task UpdateBorrowSlipStatus_WithOverdueDetail_ShouldMarkOverdue()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-10),
            Today.AddDays(-1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.UpdateBorrowSlipStatusAsync(
            await harness.GetBorrowSlipIdForDetailAsync(detailId),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeTrue();
        state.Detail.Status.Should().Be(BorrowSlipDetailStatus.Overdue);
        state.BorrowSlip.Status.Should().Be(BorrowSlipStatus.Overdue);
    }

    [Fact]
    public async Task ReturnBook_WhenPersistenceFails_ShouldRollbackEverything()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync(currentEmployeeId: 999);
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good),
            TestContext.Current.CancellationToken);
        ReturnState state = await harness.GetReturnStateAsync(detailId);

        result.Succeeded.Should().BeFalse();
        state.Detail.Status.Should().Be(BorrowSlipDetailStatus.Borrowing);
        state.Detail.ActualReturnDate.Should().BeNull();
        state.BookCopy.Status.Should().Be(BookCopyStatus.Borrowed);
        state.ReturnRecord.Should().BeNull();
        state.Fines.Should().BeEmpty();
    }

    [Fact]
    public async Task ReturnBook_WithoutPermission_ShouldFail()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync(canManageBorrowing: false);
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        OperationResult result = await service.ReturnBookAsync(
            new ReturnBookRequest(detailId, PhysicalCondition.Good),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền xử lý trả sách");
        (await harness.CountReturnRecordsAsync(detailId)).Should().Be(0);
    }

    [Fact]
    public async Task SearchOutstanding_WithBorrowCode_ShouldReturnBooks()
    {
        await using ReturnServiceHarness harness =
            await ReturnServiceHarness.CreateAsync();
        int detailId = await harness.CreateBorrowSlipAsync(
            [2],
            Today.AddDays(-5),
            Today.AddDays(1));
        string borrowCode =
            await harness.GetBorrowCodeForDetailAsync(detailId);
        IReturnService service =
            harness.Provider.GetRequiredService<IReturnService>();

        IReadOnlyList<ReturnLookupDto> results =
            await service.SearchOutstandingAsync(
                borrowCode,
                TestContext.Current.CancellationToken);

        results.Should().ContainSingle();
        results.Single().BorrowCode.Should().Be(borrowCode);
        results.Single().Books.Should().ContainSingle(
            item => item.BorrowSlipDetailId == detailId);
    }

    private sealed class ReturnServiceHarness : IAsyncDisposable
    {
        private ReturnServiceHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<ReturnServiceHarness> CreateAsync(
            int currentEmployeeId = 1,
            bool canManageBorrowing = true)
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
                new ReturnAuthenticationServiceStub(
                    currentEmployeeId,
                    canManageBorrowing));
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
                await factory.CreateDbContextAsync();
            await dbContext.Database.EnsureCreatedAsync();
            return new ReturnServiceHarness(provider, runtimeDirectory);
        }

        public async Task<int> CreateBorrowSlipAsync(
            IReadOnlyCollection<int> bookCopyIds,
            DateOnly borrowDate,
            DateOnly expectedReturnDate)
        {
            int[] detailIds = await CreateBorrowSlipWithDetailsAsync(
                bookCopyIds,
                borrowDate,
                expectedReturnDate);
            return detailIds.Single();
        }

        public async Task<int[]> CreateBorrowSlipWithDetailsAsync(
            IReadOnlyCollection<int> bookCopyIds,
            DateOnly borrowDate,
            DateOnly expectedReturnDate)
        {
            int[] result = [];
            await ExecuteDbAsync(async dbContext =>
            {
                List<BookCopy> copies = await dbContext.BookCopies
                    .Where(copy => bookCopyIds.Contains(copy.Id))
                    .ToListAsync();
                foreach (BookCopy copy in copies)
                {
                    copy.Status = BookCopyStatus.Borrowed;
                }

                var borrowSlip = new BorrowSlip
                {
                    BorrowCode = $"TEST-{Guid.NewGuid():N}"[..30],
                    ReaderId = 4,
                    EmployeeId = 1,
                    BorrowDate = borrowDate,
                    ExpectedReturnDate = expectedReturnDate,
                    Status = BorrowSlipStatus.Active,
                    Details = bookCopyIds.Select(bookCopyId =>
                        new BorrowSlipDetail
                        {
                            BookCopyId = bookCopyId,
                            ExpectedReturnDate = expectedReturnDate,
                            Status = BorrowSlipDetailStatus.Borrowing
                        }).ToArray()
                };
                dbContext.BorrowSlips.Add(borrowSlip);
                await dbContext.SaveChangesAsync();
                result = borrowSlip.Details.Select(item => item.Id).ToArray();
            });
            return result;
        }

        public async Task<ReturnState> GetReturnStateAsync(int detailId)
        {
            ReturnState? result = null;
            await ExecuteDbAsync(async dbContext =>
            {
                BorrowSlipDetail detail = await dbContext.BorrowSlipDetails
                    .AsNoTracking()
                    .Include(item => item.BorrowSlip)
                    .Include(item => item.BookCopy)
                    .SingleAsync(item => item.Id == detailId);
                ReturnRecord? returnRecord = await dbContext.ReturnRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        item => item.BorrowSlipDetailId == detailId);
                Fine[] fines = await dbContext.Fines
                    .AsNoTracking()
                    .Where(item => item.BorrowSlipDetailId == detailId)
                    .OrderBy(item => item.Id)
                    .ToArrayAsync();
                int activityCount = await dbContext.ActivityLogs.CountAsync(
                    item => item.Action == "BooksReturned");
                result = new ReturnState(
                    detail,
                    detail.BookCopy,
                    detail.BorrowSlip,
                    returnRecord,
                    fines,
                    activityCount);
            });
            return result!;
        }

        public async Task<decimal> GetBookPriceForDetailAsync(int detailId)
        {
            decimal result = 0m;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.BorrowSlipDetails
                    .Where(item => item.Id == detailId)
                    .Select(item => item.BookCopy.Book.Price)
                    .SingleAsync();
            });
            return result;
        }

        public async Task AddExistingFineAsync(
            int detailId,
            FineType fineType,
            decimal amount)
        {
            await ExecuteDbAsync(async dbContext =>
            {
                int readerId = await dbContext.BorrowSlipDetails
                    .Where(item => item.Id == detailId)
                    .Select(item => item.BorrowSlip.ReaderId)
                    .SingleAsync();
                dbContext.Fines.Add(new Fine
                {
                    FineCode = $"EXIST-{Guid.NewGuid():N}"[..30],
                    ReaderId = readerId,
                    BorrowSlipDetailId = detailId,
                    FineType = fineType,
                    Amount = amount,
                    PaidAmount = 0m,
                    Status = FineStatus.Unpaid,
                    Reason = "Tiền phạt đã tạo trước khi trả sách.",
                    CreatedByEmployeeId = 1
                });
                await dbContext.SaveChangesAsync();
            });
        }

        public async Task<int> CountReturnRecordsAsync(int detailId)
        {
            int result = 0;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.ReturnRecords.CountAsync(
                    item => item.BorrowSlipDetailId == detailId);
            });
            return result;
        }

        public async Task<int> GetBorrowSlipIdForDetailAsync(int detailId)
        {
            int result = 0;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.BorrowSlipDetails
                    .Where(item => item.Id == detailId)
                    .Select(item => item.BorrowSlipId)
                    .SingleAsync();
            });
            return result;
        }

        public async Task<BorrowSlipStatus> GetBorrowSlipStatusForDetailAsync(
            int detailId)
        {
            BorrowSlipStatus result = default;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.BorrowSlipDetails
                    .Where(item => item.Id == detailId)
                    .Select(item => item.BorrowSlip.Status)
                    .SingleAsync();
            });
            return result;
        }

        public async Task<string> GetBorrowCodeForDetailAsync(int detailId)
        {
            string result = string.Empty;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.BorrowSlipDetails
                    .Where(item => item.Id == detailId)
                    .Select(item => item.BorrowSlip.BorrowCode)
                    .SingleAsync();
            });
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
            Func<LibraryDbContext, Task> action)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync();
            await action(dbContext);
        }
    }

    private sealed record ReturnState(
        BorrowSlipDetail Detail,
        BookCopy BookCopy,
        BorrowSlip BorrowSlip,
        ReturnRecord? ReturnRecord,
        IReadOnlyList<Fine> Fines,
        int ActivityCount);

    private sealed class ReturnAuthenticationServiceStub(
        int employeeId,
        bool canManageBorrowing)
        : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure("Không dùng trong kiểm thử."));
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
            int targetEmployeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser()
        {
            return new CurrentUser(
                employeeId,
                $"NV{employeeId:0000}",
                "Nhân viên kiểm thử",
                "return.test",
                "Administrator");
        }

        public bool CheckPermission(Permission permission)
        {
            return canManageBorrowing
                && permission == Permission.ManageBorrowing;
        }
    }
}
