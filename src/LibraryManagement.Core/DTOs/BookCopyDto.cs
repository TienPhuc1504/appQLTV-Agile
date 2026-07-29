using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BookCopyDto(
    int Id,
    string CopyCode,
    int BookId,
    string BookCode,
    string BookTitle,
    string? ShelfLocation,
    DateOnly ImportedAt,
    PhysicalCondition PhysicalCondition,
    BookCopyStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
