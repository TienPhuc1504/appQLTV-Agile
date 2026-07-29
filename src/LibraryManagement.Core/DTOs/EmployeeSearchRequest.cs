namespace LibraryManagement.Core.DTOs;

public sealed record EmployeeSearchRequest(
    string? Keyword = null,
    int? RoleId = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20);
