namespace LibraryManagement.Core.DTOs;

public sealed record BorrowSlipSearchRequest(
    string? Keyword = null,
    int? ReaderId = null,
    int PageNumber = 1,
    int PageSize = 20);
