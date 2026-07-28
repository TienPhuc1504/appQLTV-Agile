using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class ReturnRecordConfiguration : IEntityTypeConfiguration<ReturnRecord>
{
    public void Configure(EntityTypeBuilder<ReturnRecord> builder)
    {
        builder.ToTable(
            "ReturnRecords",
            table => table.HasCheckConstraint(
                "CK_ReturnRecords_OverdueDays",
                "\"OverdueDays\" >= 0"));
        builder.HasKey(returnRecord => returnRecord.Id);
        builder.Property(returnRecord => returnRecord.Notes).HasMaxLength(1000);
        builder.HasIndex(returnRecord => returnRecord.BorrowSlipDetailId).IsUnique();
        builder.HasIndex(returnRecord => returnRecord.ReturnDate);

        builder.HasOne(returnRecord => returnRecord.BorrowSlipDetail)
            .WithOne(detail => detail.ReturnRecord)
            .HasForeignKey<ReturnRecord>(returnRecord => returnRecord.BorrowSlipDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(returnRecord => returnRecord.Employee)
            .WithMany(employee => employee.ReturnRecords)
            .HasForeignKey(returnRecord => returnRecord.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
