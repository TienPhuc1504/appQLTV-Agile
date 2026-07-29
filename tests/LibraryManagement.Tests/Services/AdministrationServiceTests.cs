using FluentAssertions;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Security;
using LibraryManagement.Core.Validation;
using LibraryManagement.Infrastructure;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagement.Tests.Services;

public sealed class AdministrationServiceTests
{
    [Fact]
    public async Task CreateEmployee_WithValidInput_ShouldHashPasswordAndLog()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        IEmployeeService service =
            harness.Provider.GetRequiredService<IEmployeeService>();

        OperationResult result = await service.CreateAsync(
            CreateEmployeeRequest(),
            TestContext.Current.CancellationToken);
        Employee employee = await harness.GetEmployeeAsync("NV0099");
        int logCount = await harness.CountActivitiesAsync(
            "EmployeeCreated");

        result.Succeeded.Should().BeTrue();
        employee.PasswordHash.Should().NotBe("Employee@123");
        harness.Provider.GetRequiredService<IPasswordHasher>()
            .Verify("Employee@123", employee.PasswordHash)
            .Should()
            .BeTrue();
        logCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateEmployee_WithDuplicateUsername_ShouldFail()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        IEmployeeService service =
            harness.Provider.GetRequiredService<IEmployeeService>();
        EmployeeUpsertRequest request = CreateEmployeeRequest() with
        {
            Username = "ADMIN"
        };

        OperationResult result = await service.CreateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tên đăng nhập đã tồn tại.");
    }

    [Fact]
    public async Task LockEmployee_WhenTargetIsCurrentUser_ShouldFail()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        IEmployeeService service =
            harness.Provider.GetRequiredService<IEmployeeService>();

        OperationResult result = await service.LockAsync(
            1,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tài khoản đang đăng nhập");
    }

    [Fact]
    public async Task LockEmployee_WhenTargetIsLastAdministrator_ShouldFail()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(2, RoleNames.Administrator);
        IEmployeeService service =
            harness.Provider.GetRequiredService<IEmployeeService>();

        OperationResult result = await service.LockAsync(
            1,
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ít nhất một Administrator");
    }

    [Fact]
    public async Task EmployeeRepository_WhenLastAdministratorIsRemoved_ShouldFailAtomically()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        IEmployeeRepository repository =
            harness.Provider.GetRequiredService<IEmployeeRepository>();
        Employee administrator = await harness.GetEmployeeAsync("NV0001");
        administrator.IsActive = false;
        var activity = new ActivityLog
        {
            EmployeeId = 2,
            Action = "RepositoryGuardTest",
            EntityName = nameof(Employee),
            EntityId = administrator.Id.ToString(),
            Description = "Kiểm tra bảo vệ Administrator cuối cùng."
        };

        Func<Task> action = () => repository.SaveAsync(
            administrator,
            activity,
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<AdministrationConflictException>()
            .WithMessage("*ít nhất một Administrator*");
        (await harness.GetEmployeeAsync("NV0001")).IsActive.Should().BeTrue();
        (await harness.CountActivitiesAsync("RepositoryGuardTest"))
            .Should()
            .Be(0);
    }

    [Fact]
    public async Task EmployeeManagement_ByLibrarian_ShouldBeDenied()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(2, RoleNames.Librarian);
        IEmployeeService service =
            harness.Provider.GetRequiredService<IEmployeeService>();

        OperationResult result = await service.CreateAsync(
            CreateEmployeeRequest(),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền quản lý nhân viên");
    }

    [Fact]
    public async Task UpdateSystemSetting_WithValidValue_ShouldPersistAndLog()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        ISystemSettingService service =
            harness.Provider.GetRequiredService<ISystemSettingService>();

        OperationResult result = await service.UpdateAsync(
            new SystemSettingUpdateRequest(
                SystemSettingKeys.DefaultBorrowDays,
                "21"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        (await service.GetIntValueAsync(
            SystemSettingKeys.DefaultBorrowDays,
            TestContext.Current.CancellationToken)).Should().Be(21);
        (await harness.CountActivitiesAsync("SystemSettingUpdated"))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task UpdateSystemSetting_WithInvalidValue_ShouldFail()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        ISystemSettingService service =
            harness.Provider.GetRequiredService<ISystemSettingService>();

        OperationResult result = await service.UpdateAsync(
            new SystemSettingUpdateRequest(
                SystemSettingKeys.DefaultBorrowDays,
                "0"),
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("phải từ 1 đến 365");
    }

    [Fact]
    public async Task ActivityLogSearch_ByLibrarian_ShouldBeDenied()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(2, RoleNames.Librarian);
        IActivityLogService service =
            harness.Provider.GetRequiredService<IActivityLogService>();

        Func<Task> action = () => service.SearchAsync(
            new ActivityLogSearchRequest(),
            TestContext.Current.CancellationToken);

        await action.Should()
            .ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*quyền xem nhật ký hoạt động*");
    }

    [Fact]
    public async Task ActivityLogSearch_ShouldFilterAndPage()
    {
        await using AdministrationHarness harness =
            await AdministrationHarness.CreateAsync();
        harness.SetCurrentUser(1, RoleNames.Administrator);
        IActivityLogService service =
            harness.Provider.GetRequiredService<IActivityLogService>();
        await service.LogAsync(
            "AdministrationTest",
            "TestEntity",
            "1",
            "Hoạt động kiểm thử quản trị.",
            TestContext.Current.CancellationToken);

        PagedResult<ActivityLogDto> result = await service.SearchAsync(
            new ActivityLogSearchRequest(
                Action: "AdministrationTest",
                PageSize: 1),
            TestContext.Current.CancellationToken);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Description.Should()
            .Be("Hoạt động kiểm thử quản trị.");
    }

    private static EmployeeUpsertRequest CreateEmployeeRequest()
    {
        return new EmployeeUpsertRequest(
            "NV0099",
            "Nhân viên kiểm thử",
            new DateOnly(1998, 5, 10),
            Gender.Other,
            "0901234567",
            "employee99@library.local",
            "Thành phố Hồ Chí Minh",
            "employee99",
            2,
            "Employee@123");
    }

    private sealed class AdministrationHarness : IAsyncDisposable
    {
        private AdministrationHarness(
            ServiceProvider provider,
            string runtimeDirectory)
        {
            Provider = provider;
            RuntimeDirectory = runtimeDirectory;
        }

        public ServiceProvider Provider { get; }

        private string RuntimeDirectory { get; }

        public static async Task<AdministrationHarness> CreateAsync()
        {
            string runtimeDirectory = Path.Combine(
                Path.GetTempPath(),
                "LibraryManagement.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(runtimeDirectory);
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryDatabase"] =
                            $"Data Source={Path.Combine(runtimeDirectory, "Library.db")};Foreign Keys=True",
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
            return new AdministrationHarness(provider, runtimeDirectory);
        }

        public void SetCurrentUser(int employeeId, string roleName)
        {
            Provider.GetRequiredService<ICurrentUserService>().SetCurrentUser(
                new CurrentUser(
                    employeeId,
                    $"NV{employeeId:0000}",
                    "Người dùng kiểm thử",
                    "test.user",
                    roleName));
        }

        public async Task<Employee> GetEmployeeAsync(string employeeCode)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync();
            return await dbContext.Employees
                .AsNoTracking()
                .SingleAsync(item => item.EmployeeCode == employeeCode);
        }

        public async Task<int> CountActivitiesAsync(string action)
        {
            IDbContextFactory<LibraryDbContext> factory =
                Provider.GetRequiredService<
                    IDbContextFactory<LibraryDbContext>>();
            await using LibraryDbContext dbContext =
                await factory.CreateDbContextAsync();
            return await dbContext.ActivityLogs.CountAsync(
                item => item.Action == action);
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
}
