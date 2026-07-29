using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Models;

public sealed record ReturnTransactionSnapshot(
    IReadOnlyList<ReturnDetailSnapshot> Details,
    IReadOnlyDictionary<string, string> Settings);

public sealed record ReturnDetailSnapshot(
    int BorrowSlipDetailId,
    int BorrowSlipId,
    string BorrowCode,
    int ReaderId,
    string ReaderCode,
    int BookCopyId,
    string CopyCode,
    string BookTitle,
    decimal BookPrice,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    BorrowSlipStatus BorrowSlipStatus,
    BorrowSlipDetailStatus DetailStatus,
    BookCopyStatus BookCopyStatus,
    bool HasReturnRecord,
    decimal ExistingOverdueFineAmount,
    decimal ExistingDamagedFineAmount,
    decimal ExistingLostFineAmount);
