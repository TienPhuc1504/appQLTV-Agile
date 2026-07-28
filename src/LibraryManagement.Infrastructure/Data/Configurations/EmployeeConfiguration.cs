using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");
        builder.HasKey(employee => employee.Id);
        builder.Property(employee => employee.EmployeeCode).HasMaxLength(20).IsRequired();
        builder.Property(employee => employee.FullName).HasMaxLength(150).IsRequired();
        builder.Property(employee => employee.PhoneNumber).HasMaxLength(20);
        builder.Property(employee => employee.Email).HasMaxLength(254);
        builder.Property(employee => employee.Address).HasMaxLength(500);
        builder.Property(employee => employee.Username).HasMaxLength(50).IsRequired();
        builder.Property(employee => employee.PasswordHash).HasMaxLength(100).IsRequired();
        builder.Property(employee => employee.IsActive).HasDefaultValue(true);
        builder.HasIndex(employee => employee.EmployeeCode).IsUnique();
        builder.HasIndex(employee => employee.Username).IsUnique();
        builder.HasIndex(employee => employee.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL");

        builder.HasOne(employee => employee.Role)
            .WithMany(role => role.Employees)
            .HasForeignKey(employee => employee.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
