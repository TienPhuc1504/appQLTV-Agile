namespace LibraryManagement.Infrastructure.Initialization;

public sealed class DatabaseInitializationException : Exception
{
    public DatabaseInitializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
