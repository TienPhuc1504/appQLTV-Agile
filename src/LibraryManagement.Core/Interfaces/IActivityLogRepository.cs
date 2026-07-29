using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IActivityLogRepository
{
    Task<PagedResult<ActivityLogDto>> SearchAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);
}
