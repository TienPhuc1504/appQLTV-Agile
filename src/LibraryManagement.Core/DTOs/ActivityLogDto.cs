namespace LibraryManagement.Core.DTOs;

public sealed record ActivityLogDto(
    int Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string Action,
    string EntityName,
    string? EntityId,
    string Description,
    DateTime CreatedAt);
