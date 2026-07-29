using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Models;

public sealed record RenewalSnapshot(
    int BorrowSlipDetailId,
    int BorrowSlipId,
    string BorrowCode,
    string CopyCode,
    string BookTitle,
    ReaderStatus ReaderStatus,
    DateOnly ReaderExpirationDate,
    BorrowSlipStatus BorrowSlipStatus,
    BorrowSlipDetailStatus DetailStatus,
    BookCopyStatus BookCopyStatus,
    DateOnly ExpectedReturnDate,
    DateOnly? ActualReturnDate,
    int RenewalCount,
    IReadOnlyDictionary<string, string> Settings);
