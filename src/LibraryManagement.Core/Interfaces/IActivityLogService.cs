using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        string description,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ActivityLogDto>> GetAllAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ActivityLogDto>> SearchAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default);
}
