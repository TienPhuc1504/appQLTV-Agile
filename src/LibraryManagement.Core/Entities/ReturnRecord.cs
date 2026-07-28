using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class ReturnRecord : CreatedEntity
{
    public int BorrowSlipDetailId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly ReturnDate { get; set; }

    public PhysicalCondition ReturnedCondition { get; set; }

    public int OverdueDays { get; set; }

    public string? Notes { get; set; }

    public BorrowSlipDetail BorrowSlipDetail { get; set; } = null!;

    public Employee Employee { get; set; } = null!;
}
