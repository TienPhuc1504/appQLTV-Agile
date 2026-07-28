using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");
        builder.HasKey(publisher => publisher.Id);
        builder.Property(publisher => publisher.Name).HasMaxLength(200).IsRequired();
        builder.Property(publisher => publisher.Address).HasMaxLength(500);
        builder.Property(publisher => publisher.PhoneNumber).HasMaxLength(20);
        builder.Property(publisher => publisher.Email).HasMaxLength(254);
        builder.Property(publisher => publisher.Website).HasMaxLength(300);
        builder.Property(publisher => publisher.IsActive).HasDefaultValue(true);
        builder.HasIndex(publisher => publisher.Name).IsUnique();
    }
}
