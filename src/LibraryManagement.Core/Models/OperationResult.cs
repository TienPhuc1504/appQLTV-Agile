namespace LibraryManagement.Core.Models;

public sealed record OperationResult(bool Succeeded, string? ErrorMessage)
{
    public static OperationResult Success() => new(true, null);

    public static OperationResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new OperationResult(false, errorMessage);
    }
}
