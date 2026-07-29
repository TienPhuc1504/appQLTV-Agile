using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderListItemDto(
    int Id,
    string ReaderCode,
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Email,
    ReaderType ReaderType,
    DateOnly ExpirationDate,
    ReaderStatus Status);
