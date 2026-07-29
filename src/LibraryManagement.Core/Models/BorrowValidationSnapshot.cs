using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Models;

public sealed record BorrowValidationSnapshot(
    BorrowReaderSnapshot? Reader,
    int ActiveBorrowedCopyCount,
    bool HasOverdueBorrow,
    decimal OutstandingFineAmount,
    IReadOnlyList<BorrowCopySnapshot> BookCopies,
    IReadOnlyDictionary<string, string> Settings);

public sealed record BorrowReaderSnapshot(
    int Id,
    string ReaderCode,
    string FullName,
    ReaderStatus Status,
    DateOnly ExpirationDate);

public sealed record BorrowCopySnapshot(
    int Id,
    string CopyCode,
    string BookCode,
    string BookTitle,
    bool IsBookActive,
    BookCopyStatus Status);
