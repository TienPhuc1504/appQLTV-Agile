using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable(
            "Books",
            table =>
            {
                table.HasCheckConstraint("CK_Books_PublicationYear", "\"PublicationYear\" > 0");
                table.HasCheckConstraint("CK_Books_PageCount", "\"PageCount\" > 0");
                table.HasCheckConstraint("CK_Books_Price", "\"Price\" >= 0");
            });
        builder.HasKey(book => book.Id);
        builder.Property(book => book.BookCode).HasMaxLength(20).IsRequired();
        builder.Property(book => book.ISBN).HasMaxLength(20);
        builder.Property(book => book.Title).HasMaxLength(300).IsRequired();
        builder.Property(book => book.Language).HasMaxLength(50);
        builder.Property(book => book.Price).HasMoneyConversion();
        builder.Property(book => book.CoverImagePath).HasMaxLength(500);
        builder.Property(book => book.Description).HasMaxLength(4000);
        builder.Property(book => book.IsActive).HasDefaultValue(true);
        builder.HasIndex(book => book.BookCode).IsUnique();
        builder.HasIndex(book => book.ISBN)
            .IsUnique()
            .HasFilter("\"ISBN\" IS NOT NULL");
        builder.HasIndex(book => book.Title);

        builder.HasOne(book => book.Publisher)
            .WithMany(publisher => publisher.Books)
            .HasForeignKey(book => book.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
