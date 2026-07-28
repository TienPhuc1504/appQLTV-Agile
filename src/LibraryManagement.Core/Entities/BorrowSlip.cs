using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class BorrowSlip : AuditableEntity
{
    public string BorrowCode { get; set; } = string.Empty;

    public int ReaderId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly BorrowDate { get; set; }

    public DateOnly ExpectedReturnDate { get; set; }

    public BorrowSlipStatus Status { get; set; }

    public string? Notes { get; set; }

    public Reader Reader { get; set; } = null!;

    public Employee Employee { get; set; } = null!;

    public ICollection<BorrowSlipDetail> Details { get; set; } = [];
}
