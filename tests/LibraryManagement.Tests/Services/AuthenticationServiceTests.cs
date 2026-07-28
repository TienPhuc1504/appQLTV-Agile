using FluentAssertions;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using LibraryManagement.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace LibraryManagement.Tests.Services;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_WithValidAdministrator_ShouldCreateSessionAndLogActivity()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);

        AuthenticationResult result = await harness.Service.LoginAsync(
            "ADMIN",
            "Admin@123",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.RoleName.Should().Be("Administrator");
        harness.CurrentUserService.CurrentUser.Should().Be(result.User);

        await using LibraryDbContext dbContext = harness.CreateDbContext();
        Employee employee = await dbContext.Employees
            .SingleAsync(
                item => item.Username == "admin",
                TestContext.Current.CancellationToken);
        employee.LastLoginAt.Should().NotBeNull();
        (await dbContext.ActivityLogs.CountAsync(
            item => item.EmployeeId == employee.Id && item.Action == "Login",
            TestContext.Current.CancellationToken)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldNotCreateSession()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);

        AuthenticationResult result = await harness.Service.LoginAsync(
            "admin",
            "Wrong@123",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tên đăng nhập hoặc mật khẩu không đúng.");
        harness.CurrentUserService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithPasswordSurroundedByWhitespace_ShouldFail()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);

        AuthenticationResult result = await harness.Service.LoginAsync(
            "admin",
            " Admin@123 ",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        harness.CurrentUserService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WhenEmployeeIsLocked_ShouldFail()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await using (LibraryDbContext dbContext = harness.CreateDbContext())
        {
            Employee employee = await dbContext.Employees.SingleAsync(
                item => item.Username == "librarian1",
                TestContext.Current.CancellationToken);
            employee.IsActive = false;
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        AuthenticationResult result = await harness.Service.LoginAsync(
            "librarian1",
            "Librarian@123",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bị khóa");
        harness.CurrentUserService.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPermission_ForLibrarian_ShouldEnforceRolePolicy()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "librarian1",
            "Librarian@123",
            TestContext.Current.CancellationToken);

        harness.Service.CheckPermission(Permission.ManageBooks).Should().BeTrue();
        harness.Service.CheckPermission(Permission.ManageSystemSettings).Should().BeFalse();
        harness.Service.CheckPermission(Permission.ManageAccounts).Should().BeFalse();
    }

    [Fact]
    public async Task Logout_AfterSuccessfulLogin_ShouldClearSessionAndPermissions()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "admin",
            "Admin@123",
            TestContext.Current.CancellationToken);

        harness.Service.Logout();

        harness.Service.GetCurrentUser().Should().BeNull();
        harness.CurrentUserService.IsAuthenticated.Should().BeFalse();
        harness.Service.CheckPermission(Permission.ManageAccounts).Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ShouldUpdateHash()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "librarian1",
            "Librarian@123",
            TestContext.Current.CancellationToken);

        OperationResult result = await harness.Service.ChangePasswordAsync(
            "Librarian@123",
            "NewLibrary@456",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        await using LibraryDbContext dbContext = harness.CreateDbContext();
        string passwordHash = await dbContext.Employees
            .Where(item => item.Username == "librarian1")
            .Select(item => item.PasswordHash)
            .SingleAsync(TestContext.Current.CancellationToken);
        BCrypt.Net.BCrypt.Verify("NewLibrary@456", passwordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_ByLibrarian_ShouldBeDenied()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "librarian1",
            "Librarian@123",
            TestContext.Current.CancellationToken);

        OperationResult result = await harness.Service.ResetPasswordAsync(
            3,
            "ResetLibrary@789",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có quyền");
    }

    [Fact]
    public async Task ResetPasswordAsync_WhenAdministratorRoleWasRevoked_ShouldBeDenied()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "admin",
            "Admin@123",
            TestContext.Current.CancellationToken);
        await using (LibraryDbContext dbContext = harness.CreateDbContext())
        {
            Employee administrator = await dbContext.Employees.SingleAsync(
                item => item.Username == "admin",
                TestContext.Current.CancellationToken);
            administrator.RoleId = 2;
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        OperationResult result = await harness.Service.ResetPasswordAsync(
            2,
            "ResetLibrary@789",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không có quyền");
    }

    [Fact]
    public async Task ResetPasswordAsync_ByAdministrator_ShouldUpdateTargetPassword()
    {
        await using AuthenticationTestHarness harness =
            await AuthenticationTestHarness.CreateAsync(
                TestContext.Current.CancellationToken);
        await harness.Service.LoginAsync(
            "admin",
            "Admin@123",
            TestContext.Current.CancellationToken);

        OperationResult result = await harness.Service.ResetPasswordAsync(
            2,
            "ResetLibrary@789",
            TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        await using LibraryDbContext dbContext = harness.CreateDbContext();
        string passwordHash = await dbContext.Employees
            .Where(item => item.Id == 2)
            .Select(item => item.PasswordHash)
            .SingleAsync(TestContext.Current.CancellationToken);
        BCrypt.Net.BCrypt.Verify("ResetLibrary@789", passwordHash).Should().BeTrue();
    }

    private sealed class AuthenticationTestHarness : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<LibraryDbContext> _options;

        private AuthenticationTestHarness(
            SqliteConnection connection,
            DbContextOptions<LibraryDbContext> options)
        {
            _connection = connection;
            _options = options;
            CurrentUserService = new CurrentUserService();
            Service = new AuthenticationService(
                new TestDbContextFactory(options),
                new BcryptPasswordHasher(4),
                CurrentUserService,
                NullLogger<AuthenticationService>.Instance);
        }

        public CurrentUserService CurrentUserService { get; }

        public AuthenticationService Service { get; }

        public static async Task<AuthenticationTestHarness> CreateAsync(
            CancellationToken cancellationToken)
        {
            SQLitePCL.Batteries_V2.Init();
            var connection = new SqliteConnection(
                "Data Source=:memory:;Foreign Keys=True");
            await connection.OpenAsync(cancellationToken);

            DbContextOptions<LibraryDbContext> options =
                new DbContextOptionsBuilder<LibraryDbContext>()
                    .UseSqlite(connection)
                    .EnableDetailedErrors()
                    .Options;
            var harness = new AuthenticationTestHarness(connection, options);

            await using LibraryDbContext dbContext = harness.CreateDbContext();
            await dbContext.Database.MigrateAsync(cancellationToken);
            return harness;
        }

        public LibraryDbContext CreateDbContext()
        {
            return new LibraryDbContext(_options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<LibraryDbContext> options)
        : IDbContextFactory<LibraryDbContext>
    {
        public LibraryDbContext CreateDbContext()
        {
            return new LibraryDbContext(options);
        }
    }
}
