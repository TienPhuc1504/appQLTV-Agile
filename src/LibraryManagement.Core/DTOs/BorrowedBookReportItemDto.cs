using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BorrowedBookReportItemDto(
    int BorrowSlipDetailId,
    string BorrowCode,
    string ReaderCode,
    string ReaderName,
    string CopyCode,
    string BookTitle,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    BorrowSlipDetailStatus Status);
