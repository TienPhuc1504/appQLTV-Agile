using FluentAssertions;
using LibraryManagement.Core.Entities;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LibraryManagement.Tests.Repositories;

public sealed class LibraryDbContextModelTests
{
    [Fact]
    public void Model_ShouldContainAllRequiredEntityTypes()
    {
        using LibraryDbContext dbContext = CreateDbContext();

        Type[] expectedEntityTypes =
        [
            typeof(Role),
            typeof(Employee),
            typeof(Reader),
            typeof(Author),
            typeof(Category),
            typeof(Publisher),
            typeof(Book),
            typeof(BookAuthor),
            typeof(BookCategory),
            typeof(BookCopy),
            typeof(BorrowSlip),
            typeof(BorrowSlipDetail),
            typeof(ReturnRecord),
            typeof(Fine),
            typeof(FinePayment),
            typeof(SystemSetting),
            typeof(ActivityLog)
        ];

        Type[] actualEntityTypes = dbContext.Model
            .GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .ToArray();

        actualEntityTypes.Should().Contain(expectedEntityTypes);
    }

    [Fact]
    public void HistoricalRelationships_ShouldNotUseCascadeDelete()
    {
        using LibraryDbContext dbContext = CreateDbContext();

        Type[] historicalEntityTypes =
        [
            typeof(BorrowSlip),
            typeof(BorrowSlipDetail),
            typeof(ReturnRecord),
            typeof(Fine),
            typeof(FinePayment),
            typeof(ActivityLog)
        ];

        IEnumerable<IReadOnlyForeignKey> foreignKeys = dbContext.Model
            .GetEntityTypes()
            .Where(entityType => historicalEntityTypes.Contains(entityType.ClrType))
            .SelectMany(entityType => entityType.GetForeignKeys());

        foreignKeys.Should().OnlyContain(
            foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void BusinessCodesAndUsernames_ShouldHaveUniqueIndexes()
    {
        using LibraryDbContext dbContext = CreateDbContext();

        AssertUniqueIndex<Employee>(dbContext, nameof(Employee.EmployeeCode));
        AssertUniqueIndex<Employee>(dbContext, nameof(Employee.Username));
        AssertUniqueIndex<Reader>(dbContext, nameof(Reader.ReaderCode));
        AssertUniqueIndex<Book>(dbContext, nameof(Book.BookCode));
        AssertUniqueIndex<BookCopy>(dbContext, nameof(BookCopy.CopyCode));
        AssertUniqueIndex<BorrowSlip>(dbContext, nameof(BorrowSlip.BorrowCode));
        AssertUniqueIndex<Fine>(dbContext, nameof(Fine.FineCode));
        AssertUniqueIndex<SystemSetting>(dbContext, nameof(SystemSetting.Key));
    }

    private static LibraryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        return new LibraryDbContext(options);
    }

    private static void AssertUniqueIndex<TEntity>(
        LibraryDbContext dbContext,
        string propertyName)
        where TEntity : class
    {
        IEntityType entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException(
                $"Không tìm thấy metadata của {typeof(TEntity).Name}.");

        bool hasUniqueIndex = entityType
            .GetIndexes()
            .Any(index =>
                index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([propertyName]));

        hasUniqueIndex.Should().BeTrue(
            $"{typeof(TEntity).Name}.{propertyName} phải có unique index");
    }
}
