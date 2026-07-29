using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderDetailDto(
    int Id,
    string ReaderCode,
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Email,
    string? Address,
    ReaderType ReaderType,
    DateOnly RegisteredAt,
    DateOnly ExpirationDate,
    string? AvatarPath,
    ReaderStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
