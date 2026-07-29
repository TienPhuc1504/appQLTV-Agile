namespace LibraryManagement.Core.DTOs;

public sealed record RecentActivityDto(
    int Id,
    string EmployeeName,
    string Action,
    string EntityName,
    string? EntityId,
    string Description,
    DateTime CreatedAt);
