using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record FineDetailDto(
    int Id,
    string FineCode,
    int ReaderId,
    string ReaderCode,
    string ReaderName,
    int BorrowSlipDetailId,
    string CopyCode,
    string BookTitle,
    FineType FineType,
    decimal Amount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FineStatus Status,
    string Reason,
    IReadOnlyList<FinePaymentDto> Payments,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record FinePaymentDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    decimal Amount,
    DateTime PaymentDate,
    PaymentMethod PaymentMethod,
    string? Notes,
    DateTime CreatedAt);
