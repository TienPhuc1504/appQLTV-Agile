using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BorrowSlipConfiguration : IEntityTypeConfiguration<BorrowSlip>
{
    public void Configure(EntityTypeBuilder<BorrowSlip> builder)
    {
        builder.ToTable(
            "BorrowSlips",
            table => table.HasCheckConstraint(
                "CK_BorrowSlips_ExpectedReturnDate",
                "\"ExpectedReturnDate\" >= \"BorrowDate\""));
        builder.HasKey(borrowSlip => borrowSlip.Id);
        builder.Property(borrowSlip => borrowSlip.BorrowCode).HasMaxLength(30).IsRequired();
        builder.Property(borrowSlip => borrowSlip.Notes).HasMaxLength(1000);
        builder.HasIndex(borrowSlip => borrowSlip.BorrowCode).IsUnique();
        builder.HasIndex(borrowSlip => new { borrowSlip.ReaderId, borrowSlip.Status });
        builder.HasIndex(borrowSlip => borrowSlip.BorrowDate);

        builder.HasOne(borrowSlip => borrowSlip.Reader)
            .WithMany(reader => reader.BorrowSlips)
            .HasForeignKey(borrowSlip => borrowSlip.ReaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(borrowSlip => borrowSlip.Employee)
            .WithMany(employee => employee.BorrowSlips)
            .HasForeignKey(borrowSlip => borrowSlip.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
