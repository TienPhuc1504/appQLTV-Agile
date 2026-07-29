using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record OutstandingFineReportItemDto(
    int FineId,
    string FineCode,
    string ReaderCode,
    string ReaderName,
    FineType FineType,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FineStatus Status,
    DateTime CreatedAt);
