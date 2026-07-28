namespace LibraryManagement.Core.Interfaces;

public interface ILoginPreferenceService
{
    Task<string?> GetRememberedUsernameAsync(
        CancellationToken cancellationToken = default);

    Task SaveRememberedUsernameAsync(
        string? username,
        CancellationToken cancellationToken = default);
}
