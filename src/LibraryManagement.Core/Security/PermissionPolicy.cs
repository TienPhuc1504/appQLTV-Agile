using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Security;

public static class PermissionPolicy
{
    private static readonly IReadOnlySet<Permission> LibrarianPermissions =
        new HashSet<Permission>
        {
            Permission.ManageBooks,
            Permission.ManageBookCopies,
            Permission.ManageReaders,
            Permission.ManageBorrowing,
            Permission.ManageFines,
            Permission.ViewBasicReports
        };

    public static bool IsGranted(string? roleName, Permission permission)
    {
        if (string.Equals(
                roleName,
                RoleNames.Administrator,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
                roleName,
                RoleNames.Librarian,
                StringComparison.OrdinalIgnoreCase)
            && LibrarianPermissions.Contains(permission);
    }
}
