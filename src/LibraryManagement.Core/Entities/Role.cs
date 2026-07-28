using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Entities;

public sealed class Role : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = [];
}
