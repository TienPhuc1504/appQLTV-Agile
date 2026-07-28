using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class SystemSetting : EntityBase
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int UpdatedByEmployeeId { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Employee UpdatedByEmployee { get; set; } = null!;
}
