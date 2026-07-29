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

public sealed class FineServiceTests
{
    [Fact]
    public async Task PayFine_WithPartialAmount_ShouldUpdateStatusAndHistory()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                30000m,
                PaymentMethod.Cash,
                "Thanh toán một phần"),
            TestContext.Current.CancellationToken);
        FineState state = await harness.GetFineStateAsync(1);

        result.Succeeded.Should().BeTrue();
        state.Fine.PaidAmount.Should().Be(30000m);
        state.Fine.Status.Should().Be(FineStatus.PartiallyPaid);
        state.Payments.Should().ContainSingle();
        state.Payments.Single().Amount.Should().Be(30000m);
        state.ActivityCount.Should().Be(1);
    }

    [Fact]
    public async Task PayFine_WithRemainingAmount_ShouldMarkPaid()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                90000m,
                PaymentMethod.BankTransfer),
            TestContext.Current.CancellationToken);
        FineState state = await harness.GetFineStateAsync(1);

        result.Succeeded.Should().BeTrue();
        state.Fine.PaidAmount.Should().Be(state.Fine.Amount);
        state.Fine.Status.Should().Be(FineStatus.Paid);
    }

    [Fact]
    public async Task PayFine_OverOutstandingAmount_ShouldFail()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                90001m,
                PaymentMethod.Cash),
            TestContext.Current.CancellationToken);
        FineState state = await harness.GetFineStateAsync(1);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("số tiền còn lại");
        state.Fine.PaidAmount.Should().Be(0m);
        state.Fine.Status.Should().Be(FineStatus.Unpaid);
        state.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task WaiveFine_WithUnpaidFine_ShouldMarkWaived()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.WaiveFineAsync(
            new FineWaiveRequest(1, "Được quản trị viên phê duyệt"),
            TestContext.Current.CancellationToken);
        FineState state = await harness.GetFineStateAsync(1);

        result.Succeeded.Should().BeTrue();
        state.Fine.Status.Should().Be(FineStatus.Waived);
        state.Fine.OutstandingAmount.Should().Be(0m);
        state.ActivityCount.Should().Be(1);
    }

    [Fact]
    public async Task WaiveFine_AfterPartialPayment_ShouldPreservePaymentAndClearOutstanding()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();
        await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                30000m,
                PaymentMethod.Cash),
            TestContext.Current.CancellationToken);

        OperationResult result = await service.WaiveFineAsync(
            new FineWaiveRequest(1, "Miễn phần tiền còn lại"),
            TestContext.Current.CancellationToken);
        FineState state = await harness.GetFineStateAsync(1);
        FineDetailDto? detail = await service.GetByIdAsync(
            1,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        state.Fine.PaidAmount.Should().Be(30000m);
        state.Fine.Status.Should().Be(FineStatus.Waived);
        state.Fine.OutstandingAmount.Should().Be(0m);
        detail!.OutstandingAmount.Should().Be(0m);
    }

    [Fact]
    public async Task CreateFine_WithValidRelation_ShouldPersistFine()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.CreateFineAsync(
            new FineCreateRequest(
                1,
                1,
                FineType.Other,
                25000m,
                "Phí xử lý khác"),
            TestContext.Current.CancellationToken);
        Fine? created = await harness.GetLatestFineAsync();

        result.Succeeded.Should().BeTrue();
        created.Should().NotBeNull();
        created!.ReaderId.Should().Be(1);
        created.BorrowSlipDetailId.Should().Be(1);
        created.Amount.Should().Be(25000m);
        created.Status.Should().Be(FineStatus.Unpaid);
    }

    [Fact]
    public async Task GetOutstandingAmount_AfterPartialPayment_ShouldBeCorrect()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();
        await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                30000m,
                PaymentMethod.Card),
            TestContext.Current.CancellationToken);

        decimal outstanding = await service.GetOutstandingAmountAsync(
            2,
            TestContext.Current.CancellationToken);

        outstanding.Should().Be(60000m);
    }

    [Fact]
    public async Task FineQueries_ShouldReturnListDetailAndPayments()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();
        await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                10000m,
                PaymentMethod.Cash),
            TestContext.Current.CancellationToken);

        PagedResult<FineListItemDto> page = await service.GetAllAsync(
            new FineSearchRequest(
                Keyword: "TP202607-001",
                Status: FineStatus.PartiallyPaid),
            TestContext.Current.CancellationToken);
        FineDetailDto? detail = await service.GetByIdAsync(
            1,
            TestContext.Current.CancellationToken);
        IReadOnlyList<FineListItemDto> readerFines =
            await service.GetReaderFinesAsync(
                2,
                TestContext.Current.CancellationToken);

        page.Items.Should().ContainSingle();
        detail.Should().NotBeNull();
        detail!.Payments.Should().ContainSingle();
        readerFines.Should().Contain(item => item.Id == 1);
    }

    [Fact]
    public async Task PayFine_WithoutPermission_ShouldFail()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync(canManageFines: false);
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();

        OperationResult result = await service.PayFineAsync(
            new FinePaymentRequest(
                1,
                10000m,
                PaymentMethod.Cash),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền quản lý tiền phạt");
        (await harness.GetFineStateAsync(1)).Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task PayFine_WhenCancelled_ShouldPropagateCancellation()
    {
        await using FineServiceHarness harness =
            await FineServiceHarness.CreateAsync();
        IFineService service =
            harness.Provider.GetRequiredService<IFineService>();
        using var cancellationSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
        await cancellationSource.CancelAsync();

        Func<Task> action = () => service.PayFineAsync(
            new FinePaymentRequest(
                1,
                10000m,
                PaymentMethod.Cash),
            cancellationSource.Token);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class FineServiceHarness : IAsyncDisposable
    {
        private FineServiceHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<FineServiceHarness> CreateAsync(
            bool canManageFines = true)
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
                new FineAuthenticationServiceStub(canManageFines));
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
            return new FineServiceHarness(provider, runtimeDirectory);
        }

        public async Task<FineState> GetFineStateAsync(int fineId)
        {
            FineState? result = null;
            await ExecuteDbAsync(async dbContext =>
            {
                Fine fine = await dbContext.Fines
                    .AsNoTracking()
                    .SingleAsync(item => item.Id == fineId);
                FinePayment[] payments = await dbContext.FinePayments
                    .AsNoTracking()
                    .Where(item => item.FineId == fineId)
                    .ToArrayAsync();
                int activityCount = await dbContext.ActivityLogs.CountAsync(
                    item => item.EntityName == nameof(Fine)
                        && item.EntityId == fineId.ToString());
                result = new FineState(fine, payments, activityCount);
            });
            return result!;
        }

        public async Task<Fine?> GetLatestFineAsync()
        {
            Fine? result = null;
            await ExecuteDbAsync(async dbContext =>
            {
                result = await dbContext.Fines
                    .AsNoTracking()
                    .OrderByDescending(item => item.Id)
                    .FirstOrDefaultAsync();
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

    private sealed record FineState(
        Fine Fine,
        IReadOnlyList<FinePayment> Payments,
        int ActivityCount);

    private sealed class FineAuthenticationServiceStub(bool canManageFines)
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
            int employeeId,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OperationResult.Success());
        }

        public CurrentUser? GetCurrentUser()
        {
            return new CurrentUser(
                1,
                "NV0001",
                "Quản trị viên kiểm thử",
                "fine.test",
                "Administrator");
        }

        public bool CheckPermission(Permission permission)
        {
            return canManageFines
                && permission == Permission.ManageFines;
        }
    }
}
