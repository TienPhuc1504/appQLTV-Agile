using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.DTOs;

public sealed record FineSearchRequest(
    string? Keyword = null,
    int? ReaderId = null,
    FineType? FineType = null,
    FineStatus? Status = null,
    int PageNumber = 1,
    int PageSize = 20);
