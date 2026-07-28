namespace LibraryManagement.Infrastructure.Initialization;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
