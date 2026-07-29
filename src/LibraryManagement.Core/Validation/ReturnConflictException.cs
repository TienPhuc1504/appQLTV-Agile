namespace LibraryManagement.Core.Validation;

public sealed class ReturnConflictException : Exception
{
    public ReturnConflictException(string message)
        : base(message)
    {
    }
}
