using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record BookCopySearchRequest(
    string? Keyword = null,
    int? BookId = null,
    BookCopyStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
