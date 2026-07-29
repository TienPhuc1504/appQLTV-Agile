using LibraryManagement.Core.Entities;

namespace LibraryManagement.Core.Interfaces;

public interface ISystemSettingRepository
{
    Task<IReadOnlyList<SystemSetting>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<SystemSetting?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        SystemSetting setting,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);
}
