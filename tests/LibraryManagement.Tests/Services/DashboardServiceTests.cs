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

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetDashboardSummary_WithSeedData_ShouldCalculateTotals()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        DashboardSummaryDto summary =
            await service.GetDashboardSummaryAsync(
                TestContext.Current.CancellationToken);

        summary.TotalBooks.Should().Be(10);
        summary.TotalBookCopies.Should().Be(27);
        summary.AvailableBookCopies.Should().Be(25);
        summary.BorrowedBookCopies.Should().Be(2);
        summary.OverdueBookCopies.Should().Be(1);
        summary.ActiveReaders.Should().Be(10);
        summary.TodayBorrowedBooks.Should().Be(0);
        summary.TodayReturnedBooks.Should().Be(0);
        summary.OutstandingFineAmount.Should().Be(95000m);
    }

    [Fact]
    public async Task GetMonthlyBorrowStatistics_ShouldFillMonthsWithoutData()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        IReadOnlyList<MonthlyBorrowStatisticDto> result =
            await service.GetMonthlyBorrowStatisticsAsync(
                3,
                TestContext.Current.CancellationToken);

        result.Should().HaveCount(3);
        result.Select(item => (item.Year, item.Month, item.BorrowCount))
            .Should()
            .Equal(
                (2026, 5, 0),
                (2026, 6, 2),
                (2026, 7, 1));
    }

    [Fact]
    public async Task DashboardReports_ShouldReturnOperationalData()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        PagedResult<BorrowedBookReportItemDto> borrowedBooks =
            await service.GetBorrowedBooksReportAsync(
                cancellationToken: TestContext.Current.CancellationToken);
        PagedResult<OverdueBookReportItemDto> overdueBooks =
            await service.GetOverdueBooksReportAsync(
                cancellationToken: TestContext.Current.CancellationToken);
        PagedResult<OutstandingFineReportItemDto> outstandingFines =
            await service.GetOutstandingFinesReportAsync(
                cancellationToken: TestContext.Current.CancellationToken);

        borrowedBooks.TotalCount.Should().Be(2);
        borrowedBooks.Items.Should().Contain(
            item => item.BorrowCode == "PM202607-001");
        overdueBooks.Items.Should().ContainSingle();
        overdueBooks.Items.Single().BorrowCode.Should().Be("PM202607-002");
        overdueBooks.Items.Single().OverdueDays.Should().Be(19);
        outstandingFines.TotalCount.Should().Be(2);
        outstandingFines.Items.Sum(item => item.OutstandingAmount)
            .Should()
            .Be(95000m);
    }

    [Fact]
    public async Task BorrowedBooksReport_ShouldPageAtDatabase()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        PagedResult<BorrowedBookReportItemDto> firstPage =
            await service.GetBorrowedBooksReportAsync(
                1,
                1,
                TestContext.Current.CancellationToken);
        PagedResult<BorrowedBookReportItemDto> secondPage =
            await service.GetBorrowedBooksReportAsync(
                2,
                1,
                TestContext.Current.CancellationToken);

        firstPage.TotalCount.Should().Be(2);
        firstPage.TotalPages.Should().Be(2);
        firstPage.Items.Should().ContainSingle();
        secondPage.PageNumber.Should().Be(2);
        secondPage.Items.Should().ContainSingle();
        secondPage.Items.Single().BorrowSlipDetailId.Should()
            .NotBe(firstPage.Items.Single().BorrowSlipDetailId);
    }

    [Fact]
    public async Task DashboardStatistics_ShouldIgnoreCancelledBorrowSlips()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        await harness.AddCancelledBorrowAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        DashboardSummaryDto summary =
            await service.GetDashboardSummaryAsync(
                TestContext.Current.CancellationToken);
        IReadOnlyList<MonthlyBorrowStatisticDto> monthlyStatistics =
            await service.GetMonthlyBorrowStatisticsAsync(
                1,
                TestContext.Current.CancellationToken);
        IReadOnlyList<MostBorrowedBookDto> books =
            await service.GetMostBorrowedBooksAsync(
                10,
                TestContext.Current.CancellationToken);
        PagedResult<BorrowedBookReportItemDto> borrowedBooks =
            await service.GetBorrowedBooksReportAsync(
                cancellationToken: TestContext.Current.CancellationToken);

        summary.TodayBorrowedBooks.Should().Be(0);
        monthlyStatistics.Single().BorrowCount.Should().Be(1);
        books.Single(item => item.BookId == 1).BorrowCount.Should().Be(1);
        borrowedBooks.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task DashboardRankingsAndActivities_ShouldBeLimitedAndSorted()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        IReadOnlyList<MostBorrowedBookDto> books =
            await service.GetMostBorrowedBooksAsync(
                2,
                TestContext.Current.CancellationToken);
        IReadOnlyList<MostBorrowedCategoryDto> categories =
            await service.GetMostBorrowedCategoriesAsync(
                2,
                TestContext.Current.CancellationToken);
        IReadOnlyList<RecentActivityDto> activities =
            await service.GetRecentActivitiesAsync(
                2,
                TestContext.Current.CancellationToken);

        books.Should().HaveCount(2);
        books.Should().OnlyContain(item => item.BorrowCount == 1);
        categories.Should().HaveCount(2);
        categories[0].BorrowCount.Should()
            .BeGreaterThanOrEqualTo(categories[1].BorrowCount);
        activities.Should().HaveCount(2);
        activities[0].CreatedAt.Should()
            .BeOnOrAfter(activities[1].CreatedAt);
    }

    [Fact]
    public async Task DashboardQueries_WithoutReportPermission_ShouldFail()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync(hasPermission: false);
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        Func<Task> action = () => service.GetDashboardSummaryAsync(
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền xem báo cáo*");
    }

    [Fact]
    public async Task RecentActivities_WithBasicReportPermission_ShouldFail()
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync(
                canViewActivities: false);
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        Func<Task> action = () => service.GetRecentActivitiesAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền xem nhật ký hoạt động*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public async Task GetMonthlyBorrowStatistics_WithInvalidCount_ShouldFail(
        int monthCount)
    {
        await using DashboardServiceHarness harness =
            await DashboardServiceHarness.CreateAsync();
        IDashboardService service =
            harness.Provider.GetRequiredService<IDashboardService>();

        Func<Task> action = () =>
            service.GetMonthlyBorrowStatisticsAsync(
                monthCount,
                TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("*Số tháng thống kê*");
    }

    private sealed class DashboardServiceHarness : IAsyncDisposable
    {
        private DashboardServiceHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<DashboardServiceHarness> CreateAsync(
            bool hasPermission = true,
            bool canViewActivities = true)
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
            services.AddSingleton<TimeProvider>(
                new FixedTimeProvider(
                    new DateTimeOffset(
                        2026,
                        7,
                        29,
                        8,
                        0,
                        0,
                        TimeSpan.FromHours(7))));
            services.AddSingleton<IAuthenticationService>(
                new DashboardAuthenticationServiceStub(
                    hasPermission,
                    canViewActivities));
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
            return new DashboardServiceHarness(provider, runtimeDirectory);
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

        public async Task AddCancelledBorrowAsync()
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync();
            var borrowSlip = new BorrowSlip
            {
                BorrowCode = $"PM-HUY-{Guid.NewGuid():N}"[..30],
                ReaderId = 1,
                EmployeeId = 2,
                BorrowDate = new DateOnly(2026, 7, 29),
                ExpectedReturnDate = new DateOnly(2026, 8, 12),
                Status = BorrowSlipStatus.Cancelled
            };
            borrowSlip.Details.Add(
                new BorrowSlipDetail
                {
                    BookCopyId = 2,
                    ExpectedReturnDate = new DateOnly(2026, 8, 12),
                    Status = BorrowSlipDetailStatus.Borrowing
                });
            dbContext.BorrowSlips.Add(borrowSlip);
            await dbContext.SaveChangesAsync();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone { get; } =
            TimeZoneInfo.CreateCustomTimeZone(
                "DashboardTests",
                TimeSpan.FromHours(7),
                "DashboardTests",
                "DashboardTests");

        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    }

    private sealed class DashboardAuthenticationServiceStub(
        bool hasPermission,
        bool canViewActivities)
        : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                AuthenticationResult.Failure(
                    "Không dùng trong kiểm thử."));
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
                "Người dùng kiểm thử",
                "dashboard.test",
                "Administrator");
        }

        public bool CheckPermission(Permission permission)
        {
            return permission switch
            {
                Permission.ViewBasicReports or Permission.ViewAllReports =>
                    hasPermission,
                Permission.ViewActivityLogs => canViewActivities,
                _ => false
            };
        }
    }
}
