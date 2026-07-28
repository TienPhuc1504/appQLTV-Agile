using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Author : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public DateOnly? DateOfBirth { get; set; }

    public string? Nationality { get; set; }

    public string? Biography { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}
