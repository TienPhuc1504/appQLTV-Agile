using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface ICurrentUserService
{
    CurrentUser? CurrentUser { get; }

    bool IsAuthenticated { get; }

    event EventHandler? CurrentUserChanged;

    void SetCurrentUser(CurrentUser currentUser);

    void Clear();

    bool IsInRole(string roleName);
}
