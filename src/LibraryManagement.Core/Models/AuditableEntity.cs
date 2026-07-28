namespace LibraryManagement.Core.Models;

public abstract class AuditableEntity : CreatedEntity
{
    public DateTime UpdatedAt { get; set; }
}
