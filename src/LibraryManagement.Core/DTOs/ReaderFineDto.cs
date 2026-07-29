using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderFineDto(
    int Id,
    string FineCode,
    FineType FineType,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FineStatus Status,
    string Reason,
    DateTime CreatedAt);
