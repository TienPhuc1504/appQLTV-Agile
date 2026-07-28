namespace LibraryManagement.Core.DTOs;

public sealed record CategoryUpsertRequest(
    string Name,
    string? Description,
    bool IsActive = true);
