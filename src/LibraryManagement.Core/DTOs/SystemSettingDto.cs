namespace LibraryManagement.Core.DTOs;

public sealed record SystemSettingDto(
    int Id,
    string Key,
    string Value,
    string? Description,
    string UpdatedByEmployeeName,
    DateTime UpdatedAt);
