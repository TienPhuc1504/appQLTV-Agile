namespace LibraryManagement.Core.Validation;

public sealed class BorrowConflictException : Exception
{
    public BorrowConflictException(string message)
        : base(message)
    {
    }
}
