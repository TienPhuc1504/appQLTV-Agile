using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record ReaderSearchRequest(
    string? Keyword = null,
    ReaderStatus? Status = null,
    ReaderType? ReaderType = null,
    int PageNumber = 1,
    int PageSize = 20,
    ReaderSortField SortBy = ReaderSortField.FullName,
    bool SortDescending = false);
