using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class SystemSettingRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : ISystemSettingRepository
{
    public async Task<IReadOnlyList<SystemSetting>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SystemSettings
            .AsNoTracking()
            .Include(setting => setting.UpdatedByEmployee)
            .OrderBy(setting => setting.Id)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SystemSetting?> GetByKeyAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.SystemSettings
            .AsNoTracking()
            .Include(setting => setting.UpdatedByEmployee)
            .SingleOrDefaultAsync(
                setting => setting.Key == key,
                cancellationToken);
    }

    public async Task SaveAsync(
        SystemSetting setting,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(setting);
        ArgumentNullException.ThrowIfNull(activityLog);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Entry(setting).State = EntityState.Modified;
        dbContext.ActivityLogs.Add(activityLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
