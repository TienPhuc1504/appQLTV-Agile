using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BorrowSlipListItemDto(
    int Id,
    string BorrowCode,
    int ReaderId,
    string ReaderCode,
    string ReaderName,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    int BorrowedCopyCount,
    int RemainingCopyCount,
    BorrowSlipStatus Status);
