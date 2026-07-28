using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    void Logout();

    Task<OperationResult> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(
        int employeeId,
        string newPassword,
        CancellationToken cancellationToken = default);

    CurrentUser? GetCurrentUser();

    bool CheckPermission(Permission permission);
}
