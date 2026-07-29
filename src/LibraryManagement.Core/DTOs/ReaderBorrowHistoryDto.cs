using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderBorrowHistoryDto(
    int BorrowSlipDetailId,
    string BorrowCode,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    DateOnly? ActualReturnDate,
    string CopyCode,
    string BookCode,
    string BookTitle,
    int RenewalCount,
    BorrowSlipDetailStatus Status);
