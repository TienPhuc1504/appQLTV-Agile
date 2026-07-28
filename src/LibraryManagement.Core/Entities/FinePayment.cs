using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class FinePayment : CreatedEntity
{
    public int FineId { get; set; }

    public int EmployeeId { get; set; }

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string? Notes { get; set; }

    public Fine Fine { get; set; } = null!;

    public Employee Employee { get; set; } = null!;
}
