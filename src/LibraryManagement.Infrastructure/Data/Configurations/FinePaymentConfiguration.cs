using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class FinePaymentConfiguration : IEntityTypeConfiguration<FinePayment>
{
    public void Configure(EntityTypeBuilder<FinePayment> builder)
    {
        builder.ToTable(
            "FinePayments",
            table => table.HasCheckConstraint(
                "CK_FinePayments_Amount",
                "\"Amount\" > 0"));
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Amount).HasMoneyConversion();
        builder.Property(payment => payment.Notes).HasMaxLength(1000);
        builder.HasIndex(payment => new { payment.FineId, payment.PaymentDate });

        builder.HasOne(payment => payment.Fine)
            .WithMany(fine => fine.Payments)
            .HasForeignKey(payment => payment.FineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(payment => payment.Employee)
            .WithMany(employee => employee.FinePayments)
            .HasForeignKey(payment => payment.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
