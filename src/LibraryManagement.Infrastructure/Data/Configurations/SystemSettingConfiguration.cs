using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.HasKey(setting => setting.Id);
        builder.Property(setting => setting.Key).HasMaxLength(100).IsRequired();
        builder.Property(setting => setting.Value).HasMaxLength(500).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(500);
        builder.HasIndex(setting => setting.Key).IsUnique();

        builder.HasOne(setting => setting.UpdatedByEmployee)
            .WithMany(employee => employee.UpdatedSystemSettings)
            .HasForeignKey(setting => setting.UpdatedByEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
