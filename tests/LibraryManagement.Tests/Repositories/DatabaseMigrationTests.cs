using FluentAssertions;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.Entities;
using LibraryManagement.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Tests.Repositories;

public sealed class DatabaseMigrationTests
{
    [Fact]
    public async Task InitialMigration_ShouldCreateSchemaAndSeedExpectedData()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync(cancellationToken);
        await using LibraryDbContext dbContext = CreateDbContext(connection);

        await dbContext.Database.MigrateAsync(cancellationToken);

        (await dbContext.Roles.CountAsync(cancellationToken)).Should().Be(2);
        (await dbContext.Employees.CountAsync(cancellationToken)).Should().Be(3);
        (await dbContext.Categories.CountAsync(cancellationToken)).Should().Be(5);
        (await dbContext.Authors.CountAsync(cancellationToken)).Should().Be(5);
        (await dbContext.Publishers.CountAsync(cancellationToken)).Should().Be(3);
        (await dbContext.Books.CountAsync(cancellationToken)).Should().Be(10);
        (await dbContext.BookCopies.CountAsync(cancellationToken)).Should().Be(27);
        (await dbContext.Readers.CountAsync(cancellationToken)).Should().Be(10);
        (await dbContext.BorrowSlips.CountAsync(cancellationToken)).Should().Be(3);
        (await dbContext.Fines.CountAsync(cancellationToken)).Should().Be(2);
        (await dbContext.SystemSettings.CountAsync(cancellationToken)).Should().Be(9);
        (await dbContext.SystemSettings.SingleAsync(
                setting =>
                    setting.Key == SystemSettingKeys.MaximumOutstandingFineAmount,
                cancellationToken))
            .Value.Should().Be("0");
    }

    [Fact]
    public async Task SeededEmployeePasswords_ShouldBeBcryptHashes()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync(cancellationToken);
        await using LibraryDbContext dbContext = CreateDbContext(connection);
        await dbContext.Database.MigrateAsync(cancellationToken);

        Employee administrator = await dbContext.Employees
            .SingleAsync(employee => employee.Username == "admin", cancellationToken);
        Employee librarian = await dbContext.Employees
            .SingleAsync(employee => employee.Username == "librarian1", cancellationToken);

        administrator.PasswordHash.Should().StartWith("$2");
        librarian.PasswordHash.Should().StartWith("$2");
        BCrypt.Net.BCrypt.Verify("Admin@123", administrator.PasswordHash).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("Librarian@123", librarian.PasswordHash).Should().BeTrue();
    }

    [Theory]
    [InlineData(nameof(Reader), 1)]
    [InlineData(nameof(Book), 1)]
    [InlineData(nameof(BookCopy), 1)]
    [InlineData(nameof(Employee), 2)]
    public async Task EntityWithHistoricalData_ShouldNotBeDeletable(
        string entityName,
        int entityId)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync(cancellationToken);
        await using LibraryDbContext dbContext = CreateDbContext(connection);
        await dbContext.Database.MigrateAsync(cancellationToken);

        object entity = entityName switch
        {
            nameof(Reader) => await dbContext.Readers
                .SingleAsync(item => item.Id == entityId, cancellationToken),
            nameof(Book) => await dbContext.Books
                .SingleAsync(item => item.Id == entityId, cancellationToken),
            nameof(BookCopy) => await dbContext.BookCopies
                .SingleAsync(item => item.Id == entityId, cancellationToken),
            nameof(Employee) => await dbContext.Employees
                .SingleAsync(item => item.Id == entityId, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(entityName),
                entityName,
                "Loại entity kiểm thử không được hỗ trợ.")
        };

        dbContext.Remove(entity);

        Func<Task> action = () => dbContext.SaveChangesAsync(cancellationToken);

        await action.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task MoneyValues_ShouldRoundTripWithoutPrecisionLoss()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqliteConnection connection =
            await CreateOpenConnectionAsync(cancellationToken);
        await using LibraryDbContext dbContext = CreateDbContext(connection);
        await dbContext.Database.MigrateAsync(cancellationToken);

        Fine fine = await dbContext.Fines
            .SingleAsync(entity => entity.Id == 2, cancellationToken);
        FinePayment payment = await dbContext.FinePayments
            .SingleAsync(cancellationToken);

        fine.Amount.Should().Be(10000m);
        fine.PaidAmount.Should().Be(5000m);
        payment.Amount.Should().Be(5000m);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync(
        CancellationToken cancellationToken)
    {
        SQLitePCL.Batteries_V2.Init();

        var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static LibraryDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(connection)
            .EnableDetailedErrors()
            .Options;

        return new LibraryDbContext(options);
    }
}
