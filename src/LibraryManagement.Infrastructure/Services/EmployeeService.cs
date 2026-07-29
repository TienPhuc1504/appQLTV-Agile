using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Security;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class EmployeeService(
    IEmployeeRepository employeeRepository,
    IAuthenticationService authenticationService,
    IPasswordHasher passwordHasher,
    ILogger<EmployeeService> logger)
    : IEmployeeService
{
    private const string AccessDeniedMessage =
        "Bạn không có quyền quản lý nhân viên.";

    public async Task<PagedResult<EmployeeListItemDto>> GetAllAsync(
        EmployeeSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DemandAccess();
        var normalizedRequest = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                150,
                "Từ khóa"),
            RoleId = request.RoleId > 0 ? request.RoleId : null,
            PageNumber = Math.Max(1, request.PageNumber),
            PageSize = Math.Clamp(request.PageSize, 1, 100)
        };
        PagedResult<Employee> result = await employeeRepository.SearchAsync(
            normalizedRequest,
            cancellationToken);
        return new PagedResult<EmployeeListItemDto>(
            result.Items.Select(MapListItem).ToArray(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize);
    }

    public async Task<EmployeeDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        DemandAccess();
        Employee? employee = id <= 0
            ? null
            : await employeeRepository.GetByIdAsync(id, cancellationToken);
        return employee is null ? null : MapDetail(employee);
    }

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        DemandAccess();
        IReadOnlyList<Role> roles =
            await employeeRepository.GetActiveRolesAsync(cancellationToken);
        return roles
            .Select(
                role => new RoleDto(
                    role.Id,
                    role.Name,
                    role.Description,
                    role.IsActive))
            .ToArray();
    }

    public async Task<OperationResult> CreateAsync(
        EmployeeUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure = GetAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            ValidatedEmployeeInput input = EmployeeValidator.Validate(
                request,
                requirePassword: true,
                DateOnly.FromDateTime(DateTime.Today));
            Role? role = (await employeeRepository.GetActiveRolesAsync(
                    cancellationToken))
                .SingleOrDefault(item => item.Id == input.RoleId);
            if (role is null)
            {
                return OperationResult.Failure(
                    "Vai trò không tồn tại hoặc đã ngừng hoạt động.");
            }

            OperationResult? duplicateFailure =
                await GetDuplicateFailureAsync(
                    input,
                    excludingId: null,
                    cancellationToken);
            if (duplicateFailure is not null)
            {
                return duplicateFailure;
            }

            CurrentUser actor = authenticationService.GetCurrentUser()!;
            var employee = new Employee
            {
                EmployeeCode = input.EmployeeCode,
                FullName = input.FullName,
                DateOfBirth = input.DateOfBirth,
                Gender = input.Gender,
                PhoneNumber = input.PhoneNumber,
                Email = input.Email,
                Address = input.Address,
                Username = input.Username,
                PasswordHash = passwordHasher.Hash(input.Password!),
                RoleId = input.RoleId,
                IsActive = true
            };
            await employeeRepository.SaveAsync(
                employee,
                CreateActivity(
                    actor.EmployeeId,
                    "EmployeeCreated",
                    input.EmployeeCode,
                    $"Tạo nhân viên {input.EmployeeCode} - {input.FullName}."),
                cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể tạo nhân viên.");
            return OperationResult.Failure(
                "Không thể tạo nhân viên. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
        catch (AdministrationConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        EmployeeUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure = GetAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            Employee? employee = id <= 0
                ? null
                : await employeeRepository.GetByIdAsync(
                    id,
                    cancellationToken);
            if (employee is null)
            {
                return OperationResult.Failure("Nhân viên không tồn tại.");
            }

            ValidatedEmployeeInput input = EmployeeValidator.Validate(
                request,
                requirePassword: false,
                DateOnly.FromDateTime(DateTime.Today));
            if (!string.Equals(
                    employee.EmployeeCode,
                    input.EmployeeCode,
                    StringComparison.Ordinal))
            {
                return OperationResult.Failure(
                    "Không thể thay đổi mã nhân viên sau khi đã tạo.");
            }

            if (employee.RoleId != input.RoleId)
            {
                return OperationResult.Failure(
                    "Vui lòng sử dụng chức năng đổi vai trò.");
            }

            OperationResult? duplicateFailure =
                await GetDuplicateFailureAsync(
                    input,
                    id,
                    cancellationToken);
            if (duplicateFailure is not null)
            {
                return duplicateFailure;
            }

            employee.FullName = input.FullName;
            employee.DateOfBirth = input.DateOfBirth;
            employee.Gender = input.Gender;
            employee.PhoneNumber = input.PhoneNumber;
            employee.Email = input.Email;
            employee.Address = input.Address;
            employee.Username = input.Username;
            CurrentUser actor = authenticationService.GetCurrentUser()!;
            await employeeRepository.SaveAsync(
                employee,
                CreateActivity(
                    actor.EmployeeId,
                    "EmployeeUpdated",
                    employee.Id.ToString(),
                    $"Cập nhật nhân viên {employee.EmployeeCode}."),
                cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể cập nhật nhân viên {Id}.", id);
            return OperationResult.Failure(
                "Không thể cập nhật nhân viên. Vui lòng kiểm tra dữ liệu.");
        }
        catch (AdministrationConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    public Task<OperationResult> LockAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return ChangeLockStateAsync(id, lockAccount: true, cancellationToken);
    }

    public Task<OperationResult> UnlockAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return ChangeLockStateAsync(id, lockAccount: false, cancellationToken);
    }

    public Task<OperationResult> ResetPasswordAsync(
        int id,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            AdministrationServiceAuthorization.GetFailure(
                authenticationService,
                Permission.ManageAccounts,
                "Bạn không có quyền reset mật khẩu.");
        return accessFailure is not null
            ? Task.FromResult(accessFailure)
            : authenticationService.ResetPasswordAsync(
                id,
                newPassword,
                cancellationToken);
    }

    public async Task<OperationResult> ChangeRoleAsync(
        int id,
        int roleId,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure = GetAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        CurrentUser actor = authenticationService.GetCurrentUser()!;
        if (actor.EmployeeId == id)
        {
            return OperationResult.Failure(
                "Không thể thay đổi vai trò của tài khoản đang đăng nhập.");
        }

        Employee? employee = id <= 0
            ? null
            : await employeeRepository.GetByIdAsync(id, cancellationToken);
        Role? role = roleId <= 0
            ? null
            : (await employeeRepository.GetActiveRolesAsync(cancellationToken))
                .SingleOrDefault(item => item.Id == roleId);
        if (employee is null)
        {
            return OperationResult.Failure("Nhân viên không tồn tại.");
        }

        if (role is null)
        {
            return OperationResult.Failure(
                "Vai trò không tồn tại hoặc đã ngừng hoạt động.");
        }

        if (employee.Role.Name == RoleNames.Administrator
            && role.Name != RoleNames.Administrator
            && employee.IsActive
            && await employeeRepository.CountActiveAdministratorsAsync(
                cancellationToken) <= 1)
        {
            return OperationResult.Failure(
                "Hệ thống phải có ít nhất một Administrator đang hoạt động.");
        }

        if (employee.RoleId == role.Id)
        {
            return OperationResult.Success();
        }

        employee.RoleId = role.Id;
        try
        {
            await employeeRepository.SaveAsync(
                employee,
                CreateActivity(
                    actor.EmployeeId,
                    "EmployeeRoleChanged",
                    employee.Id.ToString(),
                    $"Đổi vai trò nhân viên {employee.EmployeeCode} thành {role.Name}."),
                cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi vai trò nhân viên {Id}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật vai trò nhân viên.");
        }
        catch (AdministrationConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    private async Task<OperationResult> ChangeLockStateAsync(
        int id,
        bool lockAccount,
        CancellationToken cancellationToken)
    {
        OperationResult? accessFailure = GetAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        CurrentUser actor = authenticationService.GetCurrentUser()!;
        if (lockAccount && actor.EmployeeId == id)
        {
            return OperationResult.Failure(
                "Không thể khóa tài khoản đang đăng nhập.");
        }

        Employee? employee = id <= 0
            ? null
            : await employeeRepository.GetByIdAsync(id, cancellationToken);
        if (employee is null)
        {
            return OperationResult.Failure("Nhân viên không tồn tại.");
        }

        if (lockAccount == !employee.IsActive)
        {
            return OperationResult.Success();
        }

        if (lockAccount
            && employee.Role.Name == RoleNames.Administrator
            && await employeeRepository.CountActiveAdministratorsAsync(
                cancellationToken) <= 1)
        {
            return OperationResult.Failure(
                "Hệ thống phải có ít nhất một Administrator đang hoạt động.");
        }

        employee.IsActive = !lockAccount;
        try
        {
            await employeeRepository.SaveAsync(
                employee,
                CreateActivity(
                    actor.EmployeeId,
                    lockAccount ? "EmployeeLocked" : "EmployeeUnlocked",
                    employee.Id.ToString(),
                    $"{(lockAccount ? "Khóa" : "Mở khóa")} tài khoản {employee.EmployeeCode}."),
                cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi trạng thái nhân viên {Id}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật trạng thái tài khoản.");
        }
        catch (AdministrationConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    private async Task<OperationResult?> GetDuplicateFailureAsync(
        ValidatedEmployeeInput input,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        if (await employeeRepository.EmployeeCodeExistsAsync(
                input.EmployeeCode,
                excludingId,
                cancellationToken))
        {
            return OperationResult.Failure("Mã nhân viên đã tồn tại.");
        }

        if (await employeeRepository.UsernameExistsAsync(
                input.Username,
                excludingId,
                cancellationToken))
        {
            return OperationResult.Failure("Tên đăng nhập đã tồn tại.");
        }

        return input.Email is not null
            && await employeeRepository.EmailExistsAsync(
                input.Email,
                excludingId,
                cancellationToken)
                ? OperationResult.Failure("Email đã tồn tại.")
                : null;
    }

    private void DemandAccess()
    {
        AdministrationServiceAuthorization.Demand(
            authenticationService,
            Permission.ManageEmployees,
            AccessDeniedMessage);
    }

    private OperationResult? GetAccessFailure()
    {
        return AdministrationServiceAuthorization.GetFailure(
            authenticationService,
            Permission.ManageEmployees,
            AccessDeniedMessage);
    }

    private static ActivityLog CreateActivity(
        int employeeId,
        string action,
        string entityId,
        string description)
    {
        return new ActivityLog
        {
            EmployeeId = employeeId,
            Action = action,
            EntityName = nameof(Employee),
            EntityId = entityId,
            Description = description
        };
    }

    private static EmployeeListItemDto MapListItem(Employee employee)
    {
        return new EmployeeListItemDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Username,
            employee.Role.Name,
            employee.PhoneNumber,
            employee.Email,
            employee.IsActive,
            employee.LastLoginAt);
    }

    private static EmployeeDetailDto MapDetail(Employee employee)
    {
        return new EmployeeDetailDto(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.DateOfBirth,
            employee.Gender,
            employee.PhoneNumber,
            employee.Email,
            employee.Address,
            employee.Username,
            employee.RoleId,
            employee.Role.Name,
            employee.IsActive,
            employee.LastLoginAt,
            employee.CreatedAt,
            employee.UpdatedAt);
    }
}
