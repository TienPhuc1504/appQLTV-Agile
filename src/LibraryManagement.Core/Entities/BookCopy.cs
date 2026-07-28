using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class BookCopy : AuditableEntity
{
    public string CopyCode { get; set; } = string.Empty;

    public int BookId { get; set; }

    public string? ShelfLocation { get; set; }

    public DateOnly ImportedAt { get; set; }

    public PhysicalCondition PhysicalCondition { get; set; }

    public BookCopyStatus Status { get; set; }

    public string? Notes { get; set; }

    public Book Book { get; set; } = null!;

    public ICollection<BorrowSlipDetail> BorrowSlipDetails { get; set; } = [];
}
