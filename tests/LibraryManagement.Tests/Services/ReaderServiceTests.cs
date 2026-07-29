using FluentAssertions;
using LibraryManagement.Core.DTOs;
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

public sealed class ReaderServiceTests
{
    [Fact]
    public async Task ReaderService_CreateAndSearch_ShouldPersistNormalizedData()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        OperationResult createResult = await service.CreateAsync(
            CreateRequest(" kt-dg-001 ", "Độc giả kiểm thử"),
            cancellationToken);
        PagedResult<ReaderListItemDto> searchResult =
            await service.SearchAsync(
                new ReaderSearchRequest(Keyword: "kt-dg-001"),
                cancellationToken);

        createResult.Succeeded.Should().BeTrue();
        searchResult.TotalCount.Should().Be(1);
        searchResult.Items.Single().ReaderCode.Should().Be("KT-DG-001");
        searchResult.Items.Single().FullName.Should().Be("Độc giả kiểm thử");
    }

    [Fact]
    public async Task ReaderService_WithDuplicateCode_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        OperationResult first = await service.CreateAsync(
            CreateRequest("KT-DG-002", "Độc giả thứ nhất"),
            cancellationToken);
        OperationResult duplicate = await service.CreateAsync(
            CreateRequest("kt-dg-002", "Độc giả thứ hai"),
            cancellationToken);

        first.Succeeded.Should().BeTrue();
        duplicate.Succeeded.Should().BeFalse();
        duplicate.ErrorMessage.Should().Be("Mã độc giả đã tồn tại.");
    }

    [Fact]
    public async Task ReaderService_WithInvalidDates_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        OperationResult result = await service.CreateAsync(
            CreateRequest("KT-DG-003", "Độc giả sai ngày") with
            {
                RegisteredAt = today,
                ExpirationDate = today
            },
            cancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be(
            "Ngày hết hạn phải lớn hơn ngày đăng ký.");
    }

    [Fact]
    public async Task ReaderService_LockAndUnlock_ShouldControlEligibility()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        OperationResult before =
            await service.ValidateBorrowEligibilityAsync(
                1,
                cancellationToken: cancellationToken);
        OperationResult lockResult =
            await service.LockAsync(1, cancellationToken);
        OperationResult whileLocked =
            await service.ValidateBorrowEligibilityAsync(
                1,
                cancellationToken: cancellationToken);
        OperationResult unlockResult =
            await service.UnlockAsync(1, cancellationToken);
        OperationResult after =
            await service.ValidateBorrowEligibilityAsync(
                1,
                cancellationToken: cancellationToken);

        before.Succeeded.Should().BeTrue();
        lockResult.Succeeded.Should().BeTrue();
        whileLocked.Succeeded.Should().BeFalse();
        whileLocked.ErrorMessage.Should().Contain("bị khóa");
        unlockResult.Succeeded.Should().BeTrue();
        after.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ReaderService_ExpiredCard_ShouldNotBeEligible()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        ReaderUpsertRequest request =
            CreateRequest("KT-DG-004", "Độc giả hết hạn") with
            {
                RegisteredAt = today.AddYears(-2),
                ExpirationDate = today.AddYears(-1)
            };
        (await service.CreateAsync(request, cancellationToken))
            .Succeeded.Should().BeTrue();
        ReaderListItemDto reader = (await service.SearchAsync(
                new ReaderSearchRequest(Keyword: "KT-DG-004"),
                cancellationToken))
            .Items.Single();

        OperationResult result =
            await service.ValidateBorrowEligibilityAsync(
                reader.Id,
                cancellationToken: cancellationToken);
        PagedResult<ReaderListItemDto> expiredReaders =
            await service.SearchAsync(
                new ReaderSearchRequest(
                    Keyword: reader.ReaderCode,
                    Status: ReaderStatus.Expired),
                cancellationToken);
        PagedResult<ReaderListItemDto> activeReaders =
            await service.SearchAsync(
                new ReaderSearchRequest(
                    Keyword: reader.ReaderCode,
                    Status: ReaderStatus.Active),
                cancellationToken);

        reader.Status.Should().Be(ReaderStatus.Expired);
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Thẻ độc giả đã hết hạn.");
        expiredReaders.Items.Should().ContainSingle();
        activeReaders.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ReaderService_RenewCard_ShouldUseSystemSetting()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        ReaderDetailDto before =
            (await service.GetByIdAsync(1, cancellationToken))!;

        OperationResult result =
            await service.RenewCardAsync(1, cancellationToken);
        ReaderDetailDto after =
            (await service.GetByIdAsync(1, cancellationToken))!;

        result.Succeeded.Should().BeTrue();
        after.ExpirationDate.Should().Be(
            before.ExpirationDate.AddMonths(12));
    }

    [Fact]
    public async Task ReaderService_UpdateIdentityAndCardDates_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        ReaderDetailDto reader =
            (await service.GetByIdAsync(1, cancellationToken))!;
        ReaderUpsertRequest request = ToRequest(reader);

        OperationResult identityResult = await service.UpdateAsync(
            reader.Id,
            request with { ReaderCode = "MA-MOI" },
            cancellationToken);
        OperationResult cardDateResult = await service.UpdateAsync(
            reader.Id,
            request with
            {
                ExpirationDate = request.ExpirationDate.AddMonths(1)
            },
            cancellationToken);

        identityResult.Succeeded.Should().BeFalse();
        identityResult.ErrorMessage.Should().Contain("mã độc giả");
        cardDateResult.Succeeded.Should().BeFalse();
        cardDateResult.ErrorMessage.Should().Contain("gia hạn thẻ");
    }

    [Fact]
    public async Task ReaderService_Update_ShouldPreserveIdentityCardAndStatus()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        ReaderDetailDto before =
            (await service.GetByIdAsync(1, cancellationToken))!;

        OperationResult result = await service.UpdateAsync(
            before.Id,
            ToRequest(before) with
            {
                FullName = "Tên độc giả đã cập nhật",
                Address = "Địa chỉ mới"
            },
            cancellationToken);
        ReaderDetailDto after =
            (await service.GetByIdAsync(1, cancellationToken))!;

        result.Succeeded.Should().BeTrue();
        after.FullName.Should().Be("Tên độc giả đã cập nhật");
        after.Address.Should().Be("Địa chỉ mới");
        after.ReaderCode.Should().Be(before.ReaderCode);
        after.RegisteredAt.Should().Be(before.RegisteredAt);
        after.ExpirationDate.Should().Be(before.ExpirationDate);
        after.Status.Should().Be(before.Status);
        after.CreatedAt.Should().Be(before.CreatedAt);
    }

    [Fact]
    public async Task ReaderService_HistoryAndFines_ShouldReturnSeedData()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        IReadOnlyList<ReaderBorrowHistoryDto> history =
            await service.GetBorrowingHistoryAsync(2, cancellationToken);
        IReadOnlyList<ReaderFineDto> fines =
            await service.GetOutstandingFinesAsync(2, cancellationToken);

        history.Should().ContainSingle();
        history[0].Status.Should().Be(BorrowSlipDetailStatus.Overdue);
        fines.Should().ContainSingle();
        fines[0].OutstandingAmount.Should().Be(90000m);
    }

    [Fact]
    public async Task ReaderService_Search_ShouldFilterAndPageInDatabase()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        PagedResult<ReaderListItemDto> result =
            await service.SearchAsync(
                new ReaderSearchRequest(
                    Status: ReaderStatus.Active,
                    ReaderType: ReaderType.Student,
                    PageNumber: 999,
                    PageSize: 2),
                cancellationToken);

        result.TotalCount.Should().Be(5);
        result.Items.Should().ContainSingle();
        result.Items.Should().OnlyContain(
            reader =>
                reader.Status == ReaderStatus.Active
                && reader.ReaderType == ReaderType.Student);
        result.PageNumber.Should().Be(result.TotalPages);
    }

    [Fact]
    public async Task ReaderService_Search_ShouldSortBeforePaging()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        PagedResult<ReaderListItemDto> result =
            await service.SearchAsync(
                new ReaderSearchRequest(
                    PageNumber: 1,
                    PageSize: 3,
                    SortBy: ReaderSortField.ReaderCode,
                    SortDescending: true),
                cancellationToken);

        result.Items.Select(reader => reader.ReaderCode)
            .Should()
            .Equal("DG0010", "DG0009", "DG0008");
    }

    [Fact]
    public async Task ReaderService_Search_ShouldSupportEverySortField()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        foreach (ReaderSortField sortField in Enum.GetValues<ReaderSortField>())
        {
            PagedResult<ReaderListItemDto> result =
                await service.SearchAsync(
                    new ReaderSearchRequest(
                        PageSize: 3,
                        SortBy: sortField,
                        SortDescending: true),
                    cancellationToken);

            result.Items.Should().HaveCount(3);
        }
    }

    [Fact]
    public async Task ReaderService_ExpiredLockedCard_ShouldRequireRenewalBeforeUnlock()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        ReaderUpsertRequest request =
            CreateRequest("KT-DG-005", "Độc giả khóa hết hạn") with
            {
                RegisteredAt = today.AddYears(-2),
                ExpirationDate = today.AddYears(-1)
            };
        (await service.CreateAsync(request, cancellationToken))
            .Succeeded.Should().BeTrue();
        ReaderListItemDto reader = (await service.SearchAsync(
                new ReaderSearchRequest(Keyword: "KT-DG-005"),
                cancellationToken))
            .Items.Single();

        (await service.LockAsync(reader.Id, cancellationToken))
            .Succeeded.Should().BeTrue();
        OperationResult unlockBeforeRenewal =
            await service.UnlockAsync(reader.Id, cancellationToken);
        OperationResult renewal =
            await service.RenewCardAsync(reader.Id, cancellationToken);
        OperationResult unlockAfterRenewal =
            await service.UnlockAsync(reader.Id, cancellationToken);

        unlockBeforeRenewal.Succeeded.Should().BeFalse();
        unlockBeforeRenewal.ErrorMessage.Should().Contain("gia hạn");
        renewal.Succeeded.Should().BeTrue();
        unlockAfterRenewal.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ReaderService_RenewCard_WithInvalidSetting_ShouldFail()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        await harness.SetReaderCardValidityMonthsAsync(
            "0",
            cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();
        ReaderDetailDto before =
            (await service.GetByIdAsync(1, cancellationToken))!;

        OperationResult result =
            await service.RenewCardAsync(1, cancellationToken);
        ReaderDetailDto after =
            (await service.GetByIdAsync(1, cancellationToken))!;

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cấu hình thời hạn thẻ");
        after.ExpirationDate.Should().Be(before.ExpirationDate);
    }

    [Fact]
    public async Task ReaderService_InactiveReader_ShouldNotBeLockedOrRenewed()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(cancellationToken);
        await harness.SetReaderStatusAsync(
            1,
            ReaderStatus.Inactive,
            cancellationToken);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        OperationResult lockResult =
            await service.LockAsync(1, cancellationToken);
        OperationResult renewResult =
            await service.RenewCardAsync(1, cancellationToken);

        lockResult.Succeeded.Should().BeFalse();
        renewResult.Succeeded.Should().BeFalse();
        lockResult.ErrorMessage.Should().Contain("ngừng hoạt động");
        renewResult.ErrorMessage.Should().Contain("ngừng hoạt động");
    }

    [Fact]
    public async Task ReaderService_WithoutPermission_ShouldDenyReadAndWrite()
    {
        CancellationToken cancellationToken =
            TestContext.Current.CancellationToken;
        await using var harness =
            await ReaderServiceHarness.CreateAsync(
                cancellationToken,
                canManageReaders: false);
        IReaderService service =
            harness.Provider.GetRequiredService<IReaderService>();

        Func<Task> readAction = () =>
            service.SearchAsync(
                new ReaderSearchRequest(),
                cancellationToken);
        OperationResult writeResult = await service.CreateAsync(
            CreateRequest("KT-DG-CAM", "Độc giả không được tạo"),
            cancellationToken);

        await readAction.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền quản lý độc giả*");
        writeResult.Succeeded.Should().BeFalse();
        writeResult.ErrorMessage.Should().Contain("quyền quản lý độc giả");
    }

    private static ReaderUpsertRequest CreateRequest(
        string readerCode,
        string fullName)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        return new ReaderUpsertRequest(
            readerCode,
            fullName,
            today.AddYears(-20),
            Gender.Other,
            "0901234567",
            "reader.test@example.com",
            "Thành phố Hồ Chí Minh",
            ReaderType.Student,
            today,
            today.AddMonths(12),
            null,
            "Dữ liệu kiểm thử.");
    }

    private static ReaderUpsertRequest ToRequest(ReaderDetailDto reader)
    {
        return new ReaderUpsertRequest(
            reader.ReaderCode,
            reader.FullName,
            reader.DateOfBirth,
            reader.Gender,
            reader.PhoneNumber,
            reader.Email,
            reader.Address,
            reader.ReaderType,
            reader.RegisteredAt,
            reader.ExpirationDate,
            reader.AvatarPath,
            reader.Notes);
    }

    private sealed class ReaderServiceHarness : IAsyncDisposable
    {
        private ReaderServiceHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<ReaderServiceHarness> CreateAsync(
            CancellationToken cancellationToken,
            bool canManageReaders = true)
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            string databasePath = Path.Combine(
                runtimeDirectory,
                "Library.db");
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
                new ReaderAuthenticationServiceStub(canManageReaders));
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
            return new ReaderServiceHarness(provider, runtimeDirectory);
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

        public async Task SetReaderCardValidityMonthsAsync(
            string value,
            CancellationToken cancellationToken)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(cancellationToken);
            var setting = await dbContext.SystemSettings.SingleAsync(
                item => item.Key == "ReaderCardValidityMonths",
                cancellationToken);
            setting.Value = value;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task SetReaderStatusAsync(
            int readerId,
            ReaderStatus status,
            CancellationToken cancellationToken)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync(cancellationToken);
            var reader = await dbContext.Readers.SingleAsync(
                item => item.Id == readerId,
                cancellationToken);
            reader.Status = status;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class ReaderAuthenticationServiceStub(bool canManageReaders)
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

        public CurrentUser? GetCurrentUser() => null;

        public bool CheckPermission(Permission permission)
        {
            return canManageReaders
                && permission == Permission.ManageReaders;
        }
    }
}
