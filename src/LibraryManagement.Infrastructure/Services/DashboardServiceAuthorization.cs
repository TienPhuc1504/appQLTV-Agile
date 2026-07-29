using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;

namespace LibraryManagement.Infrastructure.Services;

internal static class DashboardServiceAuthorization
{
    public const string AccessDeniedMessage =
        "Bạn không có quyền xem báo cáo thư viện.";

    public const string ActivityLogAccessDeniedMessage =
        "Bạn không có quyền xem nhật ký hoạt động.";

    public static void DemandReportAccess(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        bool hasAccess =
            authenticationService.CheckPermission(
                Permission.ViewBasicReports)
            || authenticationService.CheckPermission(
                Permission.ViewAllReports);
        if (!hasAccess)
        {
            throw new UnauthorizedAccessException(AccessDeniedMessage);
        }
    }

    public static void DemandActivityLogAccess(
        IAuthenticationService authenticationService)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        if (!authenticationService.CheckPermission(Permission.ViewActivityLogs))
        {
            throw new UnauthorizedAccessException(
                ActivityLogAccessDeniedMessage);
        }
    }
}
