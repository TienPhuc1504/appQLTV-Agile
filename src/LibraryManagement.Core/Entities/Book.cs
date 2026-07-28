using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Book : AuditableEntity
{
    public string BookCode { get; set; } = string.Empty;

    public string? ISBN { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PublisherId { get; set; }

    public int PublicationYear { get; set; }

    public string? Language { get; set; }

    public int PageCount { get; set; }

    public decimal Price { get; set; }

    public string? CoverImagePath { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Publisher Publisher { get; set; } = null!;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];

    public ICollection<BookCategory> BookCategories { get; set; } = [];

    public ICollection<BookCopy> BookCopies { get; set; } = [];
}
