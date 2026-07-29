using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

internal static class AdministrationServiceAuthorization
{
    public static void Demand(
        IAuthenticationService authenticationService,
        Permission permission,
        string message)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        if (!authenticationService.CheckPermission(permission))
        {
            throw new UnauthorizedAccessException(message);
        }
    }

    public static OperationResult? GetFailure(
        IAuthenticationService authenticationService,
        Permission permission,
        string message)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        return authenticationService.CheckPermission(permission)
            ? null
            : OperationResult.Failure(message);
    }
}
