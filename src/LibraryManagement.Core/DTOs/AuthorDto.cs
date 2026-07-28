namespace LibraryManagement.Core.DTOs;

public sealed record AuthorDto(
    int Id,
    string FullName,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? Biography,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
