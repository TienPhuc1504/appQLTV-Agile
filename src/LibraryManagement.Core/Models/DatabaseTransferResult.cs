namespace LibraryManagement.Core.Models;

public sealed record DatabaseTransferResult(
    bool Succeeded,
    string? ErrorMessage,
    string? RecoveryBackupPath)
{
    public static DatabaseTransferResult Success(
        string? recoveryBackupPath = null)
    {
        return new DatabaseTransferResult(true, null, recoveryBackupPath);
    }

    public static DatabaseTransferResult Failure(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        return new DatabaseTransferResult(false, errorMessage, null);
    }
}
