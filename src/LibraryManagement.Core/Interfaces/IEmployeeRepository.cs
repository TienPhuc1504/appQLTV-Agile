using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IEmployeeRepository
{
    Task<PagedResult<Employee>> SearchAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Employee?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetActiveRolesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> EmployeeCodeExistsAsync(
        string employeeCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(
        string username,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string email,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAdministratorsAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        Employee employee,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default);
}
