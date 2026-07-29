using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BorrowSlipDto(
    int Id,
    string BorrowCode,
    int ReaderId,
    string ReaderCode,
    string ReaderName,
    int EmployeeId,
    string EmployeeName,
    DateOnly BorrowDate,
    DateOnly ExpectedReturnDate,
    BorrowSlipStatus Status,
    string? Notes,
    IReadOnlyList<BorrowSlipDetailDto> Details,
    DateTime CreatedAt,
    DateTime UpdatedAt);
