using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("BookCategories");
        builder.HasKey(bookCategory => new { bookCategory.BookId, bookCategory.CategoryId });

        builder.HasOne(bookCategory => bookCategory.Book)
            .WithMany(book => book.BookCategories)
            .HasForeignKey(bookCategory => bookCategory.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bookCategory => bookCategory.Category)
            .WithMany(category => category.BookCategories)
            .HasForeignKey(bookCategory => bookCategory.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
