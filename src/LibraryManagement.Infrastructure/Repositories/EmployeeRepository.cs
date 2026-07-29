using System.Data;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Security;
using LibraryManagement.Core.Validation;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibraryManagement.Infrastructure.Repositories;

public sealed class EmployeeRepository(
    IDbContextFactory<LibraryDbContext> dbContextFactory)
    : IEmployeeRepository
{
    public async Task<PagedResult<Employee>> SearchAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Employee> query = dbContext.Employees;
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            string pattern = $"%{EscapeLikePattern(request.Keyword)}%";
            query = query.Where(
                employee => EF.Functions.Like(
                        employee.EmployeeCode,
                        pattern,
                        "\\")
                    || EF.Functions.Like(
                        employee.FullName,
                        pattern,
                        "\\")
                    || EF.Functions.Like(
                        employee.Username,
                        pattern,
                        "\\"));
        }

        if (request.RoleId.HasValue)
        {
            query = query.Where(
                employee => employee.RoleId == request.RoleId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(
                employee => employee.IsActive == request.IsActive.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int totalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int effectivePage = Math.Min(request.PageNumber, totalPages);
        Employee[] items = await query
            .AsNoTracking()
            .Include(employee => employee.Role)
            .OrderBy(employee => employee.EmployeeCode)
            .Skip((effectivePage - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArrayAsync(cancellationToken);
        return new PagedResult<Employee>(
            items,
            totalCount,
            effectivePage,
            request.PageSize);
    }

    public async Task<Employee?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Employees
            .AsNoTracking()
            .Include(employee => employee.Role)
            .SingleOrDefaultAsync(
                employee => employee.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetActiveRolesAsync(
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.Id)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> EmployeeCodeExistsAsync(
        string employeeCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            employee => EF.Functions.Collate(
                    employee.EmployeeCode,
                    "NOCASE")
                == employeeCode
                && (!excludingId.HasValue
                    || employee.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(
        string username,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            employee => EF.Functions.Collate(employee.Username, "NOCASE")
                == username
                && (!excludingId.HasValue
                    || employee.Id != excludingId.Value),
            cancellationToken);
    }

    public Task<bool> EmailExistsAsync(
        string email,
        int? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        return ExistsAsync(
            employee => employee.Email != null
                && EF.Functions.Collate(employee.Email, "NOCASE") == email
                && (!excludingId.HasValue
                    || employee.Id != excludingId.Value),
            cancellationToken);
    }

    public async Task<int> CountActiveAdministratorsAsync(
        CancellationToken cancellationToken = default)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Employees.CountAsync(
            employee => employee.IsActive
                && employee.Role.Name == RoleNames.Administrator,
            cancellationToken);
    }

    public async Task SaveAsync(
        Employee employee,
        ActivityLog activityLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(activityLog);
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using IDbContextTransaction transaction =
            await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
        if (employee.Id == 0)
        {
            dbContext.Employees.Add(employee);
        }
        else
        {
            await EnsureAdministratorRemainsAsync(
                dbContext,
                employee,
                cancellationToken);
            dbContext.Entry(employee).State = EntityState.Modified;
        }

        dbContext.ActivityLogs.Add(activityLog);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> ExistsAsync(
        System.Linq.Expressions.Expression<Func<Employee, bool>> predicate,
        CancellationToken cancellationToken)
    {
        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Employees.AnyAsync(
            predicate,
            cancellationToken);
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static async Task EnsureAdministratorRemainsAsync(
        LibraryDbContext dbContext,
        Employee updatedEmployee,
        CancellationToken cancellationToken)
    {
        var currentState = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => employee.Id == updatedEmployee.Id)
            .Select(
                employee => new
                {
                    employee.IsActive,
                    employee.RoleId,
                    RoleName = employee.Role.Name
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (currentState is null
            || !currentState.IsActive
            || currentState.RoleName != RoleNames.Administrator)
        {
            return;
        }

        bool removesActiveAdministrator = !updatedEmployee.IsActive;
        if (!removesActiveAdministrator
            && updatedEmployee.RoleId != currentState.RoleId)
        {
            string? targetRoleName = await dbContext.Roles
                .Where(role => role.Id == updatedEmployee.RoleId)
                .Select(role => role.Name)
                .SingleOrDefaultAsync(cancellationToken);
            removesActiveAdministrator =
                targetRoleName != RoleNames.Administrator;
        }

        if (!removesActiveAdministrator)
        {
            return;
        }

        int activeAdministratorCount = await dbContext.Employees.CountAsync(
            employee => employee.IsActive
                && employee.Role.Name == RoleNames.Administrator,
            cancellationToken);
        if (activeAdministratorCount <= 1)
        {
            throw new AdministrationConflictException(
                "Hệ thống phải có ít nhất một Administrator đang hoạt động.");
        }
    }
}
