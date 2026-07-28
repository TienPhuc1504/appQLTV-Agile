namespace LibraryManagement.Core.Entities;

public sealed class BookCategory
{
    public int BookId { get; set; }

    public int CategoryId { get; set; }

    public Book Book { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
