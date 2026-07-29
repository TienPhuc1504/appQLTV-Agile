using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderUpsertRequest(
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
    string? Notes);
