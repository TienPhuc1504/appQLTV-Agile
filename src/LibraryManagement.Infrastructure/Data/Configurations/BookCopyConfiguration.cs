using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("BookCopies");
        builder.HasKey(bookCopy => bookCopy.Id);
        builder.Property(bookCopy => bookCopy.CopyCode).HasMaxLength(30).IsRequired();
        builder.Property(bookCopy => bookCopy.ShelfLocation).HasMaxLength(100);
        builder.Property(bookCopy => bookCopy.Notes).HasMaxLength(1000);
        builder.HasIndex(bookCopy => bookCopy.CopyCode).IsUnique();
        builder.HasIndex(bookCopy => new { bookCopy.BookId, bookCopy.Status });

        builder.HasOne(bookCopy => bookCopy.Book)
            .WithMany(book => book.BookCopies)
            .HasForeignKey(bookCopy => bookCopy.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
