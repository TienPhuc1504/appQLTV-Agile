namespace LibraryManagement.Core.Models;

public sealed record AuthenticationResult(
    bool Succeeded,
    CurrentUser? User,
    string? ErrorMessage)
{
    public static AuthenticationResult Success(CurrentUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new AuthenticationResult(true, user, null);
    }

    public static AuthenticationResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new AuthenticationResult(false, null, errorMessage);
    }
}
