using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Security;
using LibraryManagement.Core.Validation;
using LibraryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class AuthenticationService(
    IDbContextFactory<LibraryDbContext> dbContextFactory,
    IPasswordHasher passwordHasher,
    ICurrentUserService currentUserService,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private const string InvalidCredentialsMessage =
        "Tên đăng nhập hoặc mật khẩu không đúng.";

    private const string DummyPasswordHash =
        "$2a$12$PIo15XwwVaJM3R6rcweNauhTHdvEVyxL1dYHEfm4Iu.wyFTSGrDcq";

    public async Task<AuthenticationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        string normalizedUsername;
        string validatedPassword;

        try
        {
            normalizedUsername = CredentialValidator.NormalizeUsername(username);
            validatedPassword = CredentialValidator.ValidateLoginPassword(password);
        }
        catch (DomainValidationException exception)
        {
            return AuthenticationResult.Failure(exception.Message);
        }

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Employee? employee = await dbContext.Employees
            .Include(item => item.Role)
            .SingleOrDefaultAsync(
                item => EF.Functions.Collate(item.Username, "NOCASE")
                    == normalizedUsername,
                cancellationToken);

        if (employee is null)
        {
            passwordHasher.Verify(validatedPassword, DummyPasswordHash);
            logger.LogWarning(
                "Đăng nhập thất bại cho tên đăng nhập {Username}.",
                normalizedUsername);
            return AuthenticationResult.Failure(InvalidCredentialsMessage);
        }

        if (!passwordHasher.Verify(validatedPassword, employee.PasswordHash))
        {
            logger.LogWarning(
                "Đăng nhập thất bại cho nhân viên {EmployeeId}.",
                employee.Id);
            return AuthenticationResult.Failure(InvalidCredentialsMessage);
        }

        if (!employee.IsActive)
        {
            logger.LogWarning(
                "Tài khoản bị khóa đã thử đăng nhập: {EmployeeId}.",
                employee.Id);
            return AuthenticationResult.Failure(
                "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
        }

        if (!employee.Role.IsActive)
        {
            logger.LogWarning(
                "Nhân viên thuộc vai trò không hoạt động đã thử đăng nhập: {EmployeeId}.",
                employee.Id);
            return AuthenticationResult.Failure(
                "Vai trò của tài khoản hiện không hoạt động.");
        }

        DateTime loginTime = DateTime.UtcNow;
        employee.LastLoginAt = loginTime;
        dbContext.ActivityLogs.Add(
            new ActivityLog
            {
                EmployeeId = employee.Id,
                Action = "Login",
                EntityName = nameof(Employee),
                EntityId = employee.Id.ToString(),
                Description = "Đăng nhập thành công.",
                CreatedAt = loginTime
            });
        await dbContext.SaveChangesAsync(cancellationToken);

        var currentUser = new CurrentUser(
            employee.Id,
            employee.EmployeeCode,
            employee.FullName,
            employee.Username,
            employee.Role.Name);
        currentUserService.SetCurrentUser(currentUser);

        logger.LogInformation(
            "Nhân viên {EmployeeId} đăng nhập thành công.",
            employee.Id);
        return AuthenticationResult.Success(currentUser);
    }

    public void Logout()
    {
        int? employeeId = currentUserService.CurrentUser?.EmployeeId;
        currentUserService.Clear();

        if (employeeId.HasValue)
        {
            logger.LogInformation(
                "Nhân viên {EmployeeId} đã đăng xuất.",
                employeeId.Value);
        }
    }

    public async Task<OperationResult> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        CurrentUser? currentUser = currentUserService.CurrentUser;
        if (currentUser is null)
        {
            return OperationResult.Failure(
                "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        string validatedCurrentPassword;
        string validatedNewPassword;

        try
        {
            validatedCurrentPassword =
                CredentialValidator.ValidateLoginPassword(currentPassword);
            validatedNewPassword =
                CredentialValidator.ValidateNewPassword(newPassword);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Employee? employee = await dbContext.Employees
            .Include(item => item.Role)
            .SingleOrDefaultAsync(
                item => item.Id == currentUser.EmployeeId,
                cancellationToken);

        if (employee is null || !employee.IsActive || !employee.Role.IsActive)
        {
            return OperationResult.Failure(
                "Tài khoản không tồn tại, đã bị khóa hoặc vai trò không hoạt động.");
        }

        if (!passwordHasher.Verify(
                validatedCurrentPassword,
                employee.PasswordHash))
        {
            return OperationResult.Failure("Mật khẩu hiện tại không đúng.");
        }

        if (passwordHasher.Verify(validatedNewPassword, employee.PasswordHash))
        {
            return OperationResult.Failure(
                "Mật khẩu mới phải khác mật khẩu hiện tại.");
        }

        employee.PasswordHash = passwordHasher.Hash(validatedNewPassword);
        dbContext.ActivityLogs.Add(
            CreateSecurityActivity(
                currentUser.EmployeeId,
                employee.Id,
                "ChangePassword",
                "Đổi mật khẩu tài khoản."));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Nhân viên {EmployeeId} đã đổi mật khẩu.",
            employee.Id);
        return OperationResult.Success();
    }

    public async Task<OperationResult> ResetPasswordAsync(
        int employeeId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        CurrentUser? currentUser = currentUserService.CurrentUser;
        if (currentUser is null)
        {
            return OperationResult.Failure(
                "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại.");
        }

        await using LibraryDbContext dbContext =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Employee? actor = await dbContext.Employees
            .Include(item => item.Role)
            .SingleOrDefaultAsync(
                item => item.Id == currentUser.EmployeeId,
                cancellationToken);

        if (actor is null || !actor.IsActive || !actor.Role.IsActive)
        {
            currentUserService.Clear();
            return OperationResult.Failure(
                "Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
        }

        if (!PermissionPolicy.IsGranted(
                actor.Role.Name,
                Permission.ManageAccounts))
        {
            logger.LogWarning(
                "Nhân viên {EmployeeId} không có quyền reset mật khẩu.",
                actor.Id);
            return OperationResult.Failure(
                "Bạn không có quyền reset mật khẩu tài khoản.");
        }

        if (employeeId <= 0)
        {
            return OperationResult.Failure("Nhân viên không hợp lệ.");
        }

        string validatedNewPassword;
        try
        {
            validatedNewPassword =
                CredentialValidator.ValidateNewPassword(newPassword);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }

        Employee? employee = await dbContext.Employees
            .SingleOrDefaultAsync(
                item => item.Id == employeeId,
                cancellationToken);

        if (employee is null)
        {
            return OperationResult.Failure("Không tìm thấy nhân viên.");
        }

        employee.PasswordHash = passwordHasher.Hash(validatedNewPassword);
        dbContext.ActivityLogs.Add(
            CreateSecurityActivity(
                actor.Id,
                employee.Id,
                "ResetPassword",
                "Reset mật khẩu tài khoản."));
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Nhân viên {ActorEmployeeId} đã reset mật khẩu cho nhân viên {TargetEmployeeId}.",
            actor.Id,
            employee.Id);
        return OperationResult.Success();
    }

    public CurrentUser? GetCurrentUser()
    {
        return currentUserService.CurrentUser;
    }

    public bool CheckPermission(Permission permission)
    {
        return PermissionPolicy.IsGranted(
            currentUserService.CurrentUser?.RoleName,
            permission);
    }

    private static ActivityLog CreateSecurityActivity(
        int actorEmployeeId,
        int targetEmployeeId,
        string action,
        string description)
    {
        return new ActivityLog
        {
            EmployeeId = actorEmployeeId,
            Action = action,
            EntityName = nameof(Employee),
            EntityId = targetEmployeeId.ToString(),
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
}
