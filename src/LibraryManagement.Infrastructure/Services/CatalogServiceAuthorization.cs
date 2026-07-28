using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

internal static class CatalogServiceAuthorization
{
    public const string AccessDeniedMessage =
        "Bạn không có quyền quản lý danh mục sách.";

    public static void DemandReadAccess(IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);

        if (!authenticationService.CheckPermission(Permission.ManageBooks))
        {
            throw new UnauthorizedAccessException(AccessDeniedMessage);
        }
    }

    public static OperationResult? GetWriteFailure(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        return authenticationService.CheckPermission(Permission.ManageBooks)
            ? null
            : OperationResult.Failure(AccessDeniedMessage);
    }
}
