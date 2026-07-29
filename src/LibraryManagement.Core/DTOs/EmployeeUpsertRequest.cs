using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record EmployeeUpsertRequest(
    string EmployeeCode,
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Email,
    string? Address,
    string Username,
    int RoleId,
    string? InitialPassword = null);
