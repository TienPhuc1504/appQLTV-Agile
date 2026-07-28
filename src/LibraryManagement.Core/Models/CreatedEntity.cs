namespace LibraryManagement.Core.Models;

public abstract class CreatedEntity : EntityBase
{
    public DateTime CreatedAt { get; set; }
}
