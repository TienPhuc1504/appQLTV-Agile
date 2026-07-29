namespace LibraryManagement.Core.DTOs;

public sealed record ActivityLogSearchRequest(
    string? Keyword = null,
    int? EmployeeId = null,
    string? Action = null,
    DateTime? From = null,
    DateTime? To = null,
    int PageNumber = 1,
    int PageSize = 20);
