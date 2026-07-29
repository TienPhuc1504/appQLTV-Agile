using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

internal static class ReaderServiceAuthorization
{
    public const string AccessDeniedMessage =
        "Bạn không có quyền quản lý độc giả.";

    public static void DemandReadAccess(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        if (!authenticationService.CheckPermission(Permission.ManageReaders))
        {
            throw new UnauthorizedAccessException(AccessDeniedMessage);
        }
    }

    public static OperationResult? GetWriteFailure(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        return authenticationService.CheckPermission(Permission.ManageReaders)
            ? null
            : OperationResult.Failure(AccessDeniedMessage);
    }
}
