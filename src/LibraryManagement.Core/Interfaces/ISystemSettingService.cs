using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface ISystemSettingService
{
    Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<int?> GetIntValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<decimal?> GetDecimalValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        SystemSettingUpdateRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
