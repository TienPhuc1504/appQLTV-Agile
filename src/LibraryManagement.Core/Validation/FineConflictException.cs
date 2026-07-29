namespace LibraryManagement.Core.Validation;

public sealed class FineConflictException : Exception
{
    public FineConflictException(string message)
        : base(message)
    {
    }
}
