using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");
        builder.HasKey(author => author.Id);
        builder.Property(author => author.FullName).HasMaxLength(150).IsRequired();
        builder.Property(author => author.Nationality).HasMaxLength(100);
        builder.Property(author => author.Biography).HasMaxLength(4000);
        builder.Property(author => author.IsActive).HasDefaultValue(true);
        builder.HasIndex(author => author.FullName);
    }
}
