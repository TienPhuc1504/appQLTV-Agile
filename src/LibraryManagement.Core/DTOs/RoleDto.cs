namespace LibraryManagement.Core.DTOs;

public sealed record RoleDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive);
