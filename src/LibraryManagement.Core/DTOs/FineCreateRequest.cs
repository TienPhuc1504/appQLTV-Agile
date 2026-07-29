using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record FineCreateRequest(
    int ReaderId,
    int BorrowSlipDetailId,
    FineType FineType,
    decimal Amount,
    string Reason);
