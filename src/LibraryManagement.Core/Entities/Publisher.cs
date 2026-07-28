using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Publisher : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Book> Books { get; set; } = [];
}
