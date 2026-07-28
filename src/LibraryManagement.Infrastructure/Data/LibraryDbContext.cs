using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Data;

public sealed class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<Reader> Readers => Set<Reader>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<BorrowSlip> BorrowSlips => Set<BorrowSlip>();

    public DbSet<BorrowSlipDetail> BorrowSlipDetails => Set<BorrowSlipDetail>();

    public DbSet<ReturnRecord> ReturnRecords => Set<ReturnRecord>();

    public DbSet<Fine> Fines => Set<Fine>();

    public DbSet<FinePayment> FinePayments => Set<FinePayment>();

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInformation();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
        SeedData.Apply(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyAuditInformation()
    {
        DateTime utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<CreatedEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = utcNow;
            }

            if (entry.Entity is not AuditableEntity auditableEntity)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                auditableEntity.UpdatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditableEntity.UpdatedAt = utcNow;
                entry.Property(nameof(CreatedEntity.CreatedAt)).IsModified = false;
            }
        }
    }
}
