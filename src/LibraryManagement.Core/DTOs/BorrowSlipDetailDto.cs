using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BorrowSlipDetailDto(
    int Id,
    int BookCopyId,
    string CopyCode,
    string BookCode,
    string BookTitle,
    DateOnly ExpectedReturnDate,
    DateOnly? ActualReturnDate,
    int RenewalCount,
    BorrowSlipDetailStatus Status);
