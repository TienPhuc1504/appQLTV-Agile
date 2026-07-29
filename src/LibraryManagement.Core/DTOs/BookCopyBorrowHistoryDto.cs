using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BookCopyBorrowHistoryDto(
    int BorrowSlipDetailId,
    string BorrowCode,
    string ReaderCode,
    string ReaderName,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    DateOnly? ActualReturnDate,
    int RenewalCount,
    BorrowSlipDetailStatus Status);
