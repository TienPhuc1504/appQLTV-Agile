using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class FineConfiguration : IEntityTypeConfiguration<Fine>
{
    public void Configure(EntityTypeBuilder<Fine> builder)
    {
        builder.ToTable(
            "Fines",
            table =>
            {
                table.HasCheckConstraint("CK_Fines_Amount", "\"Amount\" >= 0");
                table.HasCheckConstraint("CK_Fines_PaidAmount", "\"PaidAmount\" >= 0");
                table.HasCheckConstraint(
                    "CK_Fines_PaidAmountNotGreaterThanAmount",
                    "\"PaidAmount\" <= \"Amount\"");
            });
        builder.HasKey(fine => fine.Id);
        builder.Property(fine => fine.FineCode).HasMaxLength(30).IsRequired();
        builder.Property(fine => fine.Amount).HasMoneyConversion();
        builder.Property(fine => fine.PaidAmount).HasMoneyConversion();
        builder.Property(fine => fine.Reason).HasMaxLength(1000).IsRequired();
        builder.Ignore(fine => fine.OutstandingAmount);
        builder.HasIndex(fine => fine.FineCode).IsUnique();
        builder.HasIndex(fine => new { fine.ReaderId, fine.Status });

        builder.HasOne(fine => fine.Reader)
            .WithMany(reader => reader.Fines)
            .HasForeignKey(fine => fine.ReaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fine => fine.BorrowSlipDetail)
            .WithMany(detail => detail.Fines)
            .HasForeignKey(fine => fine.BorrowSlipDetailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fine => fine.CreatedByEmployee)
            .WithMany(employee => employee.CreatedFines)
            .HasForeignKey(fine => fine.CreatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
