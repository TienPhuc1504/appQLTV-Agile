using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly object _syncRoot = new();
    private CurrentUser? _currentUser;

    public CurrentUser? CurrentUser
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentUser;
            }
        }
    }

    public bool IsAuthenticated => CurrentUser is not null;

    public event EventHandler? CurrentUserChanged;

    public void SetCurrentUser(CurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);

        lock (_syncRoot)
        {
            _currentUser = currentUser;
        }

        CurrentUserChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        bool changed;

        lock (_syncRoot)
        {
            changed = _currentUser is not null;
            _currentUser = null;
        }

        if (changed)
        {
            CurrentUserChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsInRole(string roleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        return string.Equals(
            CurrentUser?.RoleName,
            roleName,
            StringComparison.OrdinalIgnoreCase);
    }
}
