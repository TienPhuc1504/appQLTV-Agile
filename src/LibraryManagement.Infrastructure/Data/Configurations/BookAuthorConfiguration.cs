using LibraryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibraryManagement.Infrastructure.Data.Configurations;

public sealed class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");
        builder.HasKey(bookAuthor => new { bookAuthor.BookId, bookAuthor.AuthorId });

        builder.HasOne(bookAuthor => bookAuthor.Book)
            .WithMany(book => book.BookAuthors)
            .HasForeignKey(bookAuthor => bookAuthor.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bookAuthor => bookAuthor.Author)
            .WithMany(author => author.BookAuthors)
            .HasForeignKey(bookAuthor => bookAuthor.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
