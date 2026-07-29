namespace LibraryManagement.Core.DTOs;

public sealed record BookUpsertRequest(
    string BookCode,
    string? ISBN,
    string Title,
    int PublisherId,
    int PublicationYear,
    string? Language,
    int PageCount,
    decimal Price,
    string? CoverImageSourcePath,
    string? Description,
    IReadOnlyCollection<int> AuthorIds,
    IReadOnlyCollection<int> CategoryIds,
    bool IsActive = true);
