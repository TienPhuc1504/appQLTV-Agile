using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

internal static class FineServiceAuthorization
{
    public const string AccessDeniedMessage =
        "Bạn không có quyền quản lý tiền phạt.";

    public static void DemandReadAccess(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        if (!authenticationService.CheckPermission(Permission.ManageFines))
        {
            throw new UnauthorizedAccessException(AccessDeniedMessage);
        }
    }

    public static OperationResult? GetWriteFailure(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        return authenticationService.CheckPermission(Permission.ManageFines)
            ? null
            : OperationResult.Failure(AccessDeniedMessage);
    }
}
