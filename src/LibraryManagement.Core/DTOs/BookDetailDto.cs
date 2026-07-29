namespace LibraryManagement.Core.DTOs;

public sealed record BookDetailDto(
    int Id,
    string BookCode,
    string? ISBN,
    string Title,
    int PublisherId,
    string PublisherName,
    int PublicationYear,
    string? Language,
    int PageCount,
    decimal Price,
    string? CoverImagePath,
    string? Description,
    bool IsActive,
    IReadOnlyList<int> AuthorIds,
    IReadOnlyList<string> AuthorNames,
    IReadOnlyList<int> CategoryIds,
    IReadOnlyList<string> CategoryNames,
    int TotalCopies,
    int AvailableCopies,
    DateTime CreatedAt,
    DateTime UpdatedAt);
