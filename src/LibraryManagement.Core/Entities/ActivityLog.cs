using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class ActivityLog : CreatedEntity
{
    public int EmployeeId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string Description { get; set; } = string.Empty;

    public Employee Employee { get; set; } = null!;
}
