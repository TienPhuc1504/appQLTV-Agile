using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record EmployeeDetailDto(
    int Id,
    string EmployeeCode,
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string Username,
    int RoleId,
    string RoleName,
    bool IsActive,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
