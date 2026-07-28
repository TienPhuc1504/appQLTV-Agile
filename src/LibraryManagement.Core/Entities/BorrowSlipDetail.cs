using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class BorrowSlipDetail : AuditableEntity
{
    public int BorrowSlipId { get; set; }

    public int BookCopyId { get; set; }

    public DateOnly ExpectedReturnDate { get; set; }

    public DateOnly? ActualReturnDate { get; set; }

    public int RenewalCount { get; set; }

    public BorrowSlipDetailStatus Status { get; set; }

    public string? Notes { get; set; }

    public BorrowSlip BorrowSlip { get; set; } = null!;

    public BookCopy BookCopy { get; set; } = null!;

    public ReturnRecord? ReturnRecord { get; set; }

    public ICollection<Fine> Fines { get; set; } = [];

    public bool CanBeReturned =>
        Status is BorrowSlipDetailStatus.Borrowing or BorrowSlipDetailStatus.Overdue;
}
