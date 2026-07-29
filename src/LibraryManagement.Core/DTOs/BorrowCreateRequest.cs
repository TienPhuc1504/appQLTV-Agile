namespace LibraryManagement.Core.DTOs;

public sealed record BorrowCreateRequest(
    int ReaderId,
    IReadOnlyCollection<int> BookCopyIds,
    string? Notes = null);
