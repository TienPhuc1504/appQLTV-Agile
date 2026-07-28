namespace LibraryManagement.Core.DTOs;

public sealed record CategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
