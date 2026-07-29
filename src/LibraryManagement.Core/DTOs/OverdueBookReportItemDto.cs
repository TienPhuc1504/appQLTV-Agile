namespace LibraryManagement.Core.DTOs;

public sealed record OverdueBookReportItemDto(
    int BorrowSlipDetailId,
    string BorrowCode,
    string ReaderCode,
    string ReaderName,
    string CopyCode,
    string BookTitle,
    DateOnly ExpectedReturnDate,
    int OverdueDays);
