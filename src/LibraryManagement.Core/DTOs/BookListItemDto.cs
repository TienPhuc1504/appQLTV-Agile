namespace LibraryManagement.Core.DTOs;

public sealed record BookListItemDto(
    int Id,
    string BookCode,
    string? ISBN,
    string Title,
    string PublisherName,
    int PublicationYear,
    decimal Price,
    string? CoverImagePath,
    bool IsActive,
    int TotalCopies,
    int AvailableCopies,
    string Authors,
    string Categories);
