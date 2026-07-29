using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReturnPreviewDto(
    int BorrowSlipDetailId,
    int OverdueDays,
    PhysicalCondition ReturnedCondition,
    decimal OverdueFineAmount,
    decimal ConditionFineAmount)
{
    public decimal TotalFineAmount =>
        OverdueFineAmount + ConditionFineAmount;
}
