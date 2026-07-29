using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReturnLookupDto(
    int BorrowSlipId,
    string BorrowCode,
    int ReaderId,
    string ReaderCode,
    string ReaderName,
    DateOnly BorrowDate,
    IReadOnlyList<ReturnableBookDto> Books);

public sealed record ReturnableBookDto(
    int BorrowSlipDetailId,
    int BookCopyId,
    string CopyCode,
    string BookCode,
    string BookTitle,
    DateOnly ExpectedReturnDate,
    BorrowSlipDetailStatus Status);
