namespace LibraryManagement.Core.DTOs;

public sealed record MonthlyBorrowStatisticDto(
    int Year,
    int Month,
    int BorrowCount)
{
    public string MonthLabel => $"Tháng {Month:00}/{Year}";
}
