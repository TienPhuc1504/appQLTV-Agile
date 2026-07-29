namespace LibraryManagement.Core.DTOs;

public sealed record BookSearchRequest(
    string? Keyword = null,
    int? CategoryId = null,
    int? PublisherId = null,
    bool? IsActive = true,
    int PageNumber = 1,
    int PageSize = 20);
