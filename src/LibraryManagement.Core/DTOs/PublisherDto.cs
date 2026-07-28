namespace LibraryManagement.Core.DTOs;

public sealed record PublisherDto(
    int Id,
    string Name,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
