using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Models;

public sealed record FineTransactionSnapshot(
    int Id,
    string FineCode,
    decimal Amount,
    decimal PaidAmount,
    FineStatus Status);

public sealed record FineCreationSnapshot(
    bool ReaderExists,
    bool BorrowSlipDetailExists,
    bool DetailBelongsToReader);
