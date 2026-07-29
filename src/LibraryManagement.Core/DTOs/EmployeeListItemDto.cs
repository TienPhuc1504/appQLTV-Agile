namespace LibraryManagement.Core.DTOs;

public sealed record EmployeeListItemDto(
    int Id,
    string EmployeeCode,
    string FullName,
    string Username,
    string RoleName,
    string? PhoneNumber,
    string? Email,
    bool IsActive,
    DateTime? LastLoginAt);
