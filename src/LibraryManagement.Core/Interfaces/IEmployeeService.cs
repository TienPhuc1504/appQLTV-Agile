using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeListItemDto>> GetAllAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        EmployeeUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        EmployeeUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> LockAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UnlockAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(
        int id,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ChangeRoleAsync(
        int id,
        int roleId,
        CancellationToken cancellationToken = default);
}
