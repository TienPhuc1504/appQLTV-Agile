using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BorrowSlipDetailConfiguration : IEntityTypeConfiguration<BorrowSlipDetail>
{
    public void Configure(EntityTypeBuilder<BorrowSlipDetail> builder)
    {
        builder.ToTable(
            "BorrowSlipDetails",
            table => table.HasCheckConstraint(
                "CK_BorrowSlipDetails_RenewalCount",
                "\"RenewalCount\" >= 0"));
        builder.HasKey(detail => detail.Id);
        builder.Property(detail => detail.Notes).HasMaxLength(1000);
        builder.HasIndex(detail => new { detail.BorrowSlipId, detail.BookCopyId }).IsUnique();
        builder.HasIndex(detail => new { detail.BookCopyId, detail.Status });
        builder.HasIndex(detail => detail.ExpectedReturnDate);

        builder.HasOne(detail => detail.BorrowSlip)
            .WithMany(borrowSlip => borrowSlip.Details)
            .HasForeignKey(detail => detail.BorrowSlipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(detail => detail.BookCopy)
            .WithMany(bookCopy => bookCopy.BorrowSlipDetails)
            .HasForeignKey(detail => detail.BookCopyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
