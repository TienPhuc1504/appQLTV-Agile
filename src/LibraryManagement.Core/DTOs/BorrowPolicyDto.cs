namespace LibraryManagement.Core.DTOs;

public sealed record BorrowPolicyDto(
    int MaximumBorrowedBooks,
    int DefaultBorrowDays,
    decimal MaximumOutstandingFineAmount);
