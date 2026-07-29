namespace LibraryManagement.Core.DTOs;

public sealed record DashboardSummaryDto(
    int TotalBooks,
    int TotalBookCopies,
    int AvailableBookCopies,
    int BorrowedBookCopies,
    int OverdueBookCopies,
    int ActiveReaders,
    int TodayBorrowedBooks,
    int TodayReturnedBooks,
    decimal OutstandingFineAmount);
