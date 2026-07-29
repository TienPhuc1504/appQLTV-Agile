using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class ActivityLogRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IActivityLogRepository
{
    public async Task<PagedResult<ActivityLogDto>> SearchAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<ActivityLog> query = dbContext.ActivityLogs;
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = $"%{EscapeLikePattern(request.Keyword)}%";
            query = query.Where(
                log => EF.Functions.Like(
                        log.Description,
                        pattern,
                        "\\")
                    || EF.Functions.Like(
                        log.EntityName,
                        pattern,
                        "\\")
                    || (log.EntityId != null
                        && EF.Functions.Like(
                            log.EntityId,
                            pattern,
                            "\\")));
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(
                log => log.EmployeeId == request.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (request.From.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= request.To.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePage = Math.Min(request.PageNumber, totalPages);
        ActivityLogDto[] items = await query
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .ThenByDescending(log => log.Id)
            .Skip((effectivePage - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(
                log => new ActivityLogDto(
                    log.Id,
                    log.EmployeeId,
                    log.Employee.EmployeeCode,
                    log.Employee.FullName,
                    log.Action,
                    log.EntityName,
                    log.EntityId,
                    log.Description,
                    log.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return new PagedResult<ActivityLogDto>(
            items,
            totalCount,
            effectivePage,
            request.PageSize);
    }

    public async Task AddAsync(
        ActivityLog activityLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activityLog);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ActivityLogs.Add(activityLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
