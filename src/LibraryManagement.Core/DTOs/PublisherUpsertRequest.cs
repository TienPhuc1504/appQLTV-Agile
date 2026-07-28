namespace LibraryManagement.Core.DTOs;

public sealed record PublisherUpsertRequest(
    string Name,
    string? Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    bool IsActive = true);
