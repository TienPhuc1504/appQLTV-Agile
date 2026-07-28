using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Fine : AuditableEntity
{
    public string FineCode { get; set; } = string.Empty;

    public int ReaderId { get; set; }

    public int BorrowSlipDetailId { get; set; }

    public FineType FineType { get; set; }

    public decimal Amount { get; set; }

    public decimal PaidAmount { get; set; }

    public FineStatus Status { get; set; }

    public string Reason { get; set; } = string.Empty;

    public int CreatedByEmployeeId { get; set; }

    public Reader Reader { get; set; } = null!;

    public BorrowSlipDetail BorrowSlipDetail { get; set; } = null!;

    public Employee CreatedByEmployee { get; set; } = null!;

    public ICollection<FinePayment> Payments { get; set; } = [];

    public decimal OutstandingAmount => Math.Max(0m, Amount - PaidAmount);
}
