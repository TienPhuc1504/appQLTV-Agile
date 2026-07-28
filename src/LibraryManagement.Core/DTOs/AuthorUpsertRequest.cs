namespace LibraryManagement.Core.DTOs;

public sealed record AuthorUpsertRequest(
    string FullName,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? Biography,
    bool IsActive = true);
