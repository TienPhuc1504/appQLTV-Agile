using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

internal static class ReturnServiceAuthorization
{
    public const string AccessDeniedMessage =
        "Bạn không có quyền xử lý trả sách.";

    public static void DemandReadAccess(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        if (!authenticationService.CheckPermission(Permission.ManageBorrowing))
        {
            throw new UnauthorizedAccessException(AccessDeniedMessage);
        }
    }

    public static OperationResult? GetWriteFailure(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        return authenticationService.CheckPermission(Permission.ManageBorrowing)
            ? null
            : OperationResult.Failure(AccessDeniedMessage);
    }
}
