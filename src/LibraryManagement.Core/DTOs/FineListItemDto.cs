using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record FineListItemDto(
    int Id,
    string FineCode,
    int ReaderId,
    string ReaderCode,
    string ReaderName,
    string CopyCode,
    string BookTitle,
    FineType FineType,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FineStatus Status,
    DateTime CreatedAt);
