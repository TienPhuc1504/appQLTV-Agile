using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BookCopyUpsertRequest(
    string CopyCode,
    int BookId,
    string? ShelfLocation,
    DateOnly ImportedAt,
    PhysicalCondition PhysicalCondition,
    BookCopyStatus Status,
    string? Notes);
