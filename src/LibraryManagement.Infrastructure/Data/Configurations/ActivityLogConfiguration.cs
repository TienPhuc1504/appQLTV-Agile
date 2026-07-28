using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLogs");
        builder.HasKey(activityLog => activityLog.Id);
        builder.Property(activityLog => activityLog.Action).HasMaxLength(100).IsRequired();
        builder.Property(activityLog => activityLog.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(activityLog => activityLog.EntityId).HasMaxLength(100);
        builder.Property(activityLog => activityLog.Description).HasMaxLength(2000).IsRequired();
        builder.HasIndex(activityLog => activityLog.CreatedAt);
        builder.HasIndex(activityLog => new { activityLog.EmployeeId, activityLog.Action });

        builder.HasOne(activityLog => activityLog.Employee)
            .WithMany(employee => employee.ActivityLogs)
            .HasForeignKey(activityLog => activityLog.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
