using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class ReaderConfiguration : IEntityTypeConfiguration<Reader>
{
    public void Configure(EntityTypeBuilder<Reader> builder)
    {
        builder.ToTable(
            "Readers",
            table => table.HasCheckConstraint(
                "CK_Readers_ExpirationDate",
                "\"ExpirationDate\" > \"RegisteredAt\""));
        builder.HasKey(reader => reader.Id);
        builder.Property(reader => reader.ReaderCode).HasMaxLength(20).IsRequired();
        builder.Property(reader => reader.FullName).HasMaxLength(150).IsRequired();
        builder.Property(reader => reader.PhoneNumber).HasMaxLength(20);
        builder.Property(reader => reader.Email).HasMaxLength(254);
        builder.Property(reader => reader.Address).HasMaxLength(500);
        builder.Property(reader => reader.AvatarPath).HasMaxLength(500);
        builder.Property(reader => reader.Notes).HasMaxLength(1000);
        builder.HasIndex(reader => reader.ReaderCode).IsUnique();
        builder.HasIndex(reader => reader.Email);
        builder.HasIndex(reader => reader.Status);
    }
}
