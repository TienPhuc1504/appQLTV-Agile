using System.Globalization;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class SystemSettingService(
    ISystemSettingRepository settingRepository,
    IAuthenticationService authenticationService,
    ILogger<SystemSettingService> logger)
    : ISystemSettingService
{
    public async Task<string?> GetValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string normalizedKey = DomainValidator.MaximumLength(
            DomainValidator.Required(key, "khóa cài đặt"),
            100,
            "Khóa cài đặt");
        return (await settingRepository.GetByKeyAsync(
            normalizedKey,
            cancellationToken))?.Value;
    }

    public async Task<int?> GetIntValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string? value = await GetValueAsync(key, cancellationToken);
        return int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int result)
                ? result
                : null;
    }

    public async Task<decimal?> GetDecimalValueAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        string? value = await GetValueAsync(key, cancellationToken);
        return decimal.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal result)
                ? result
                : null;
    }

    public async Task<OperationResult> UpdateAsync(
        SystemSettingUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            AdministrationServiceAuthorization.GetFailure(
                authenticationService,
                Permission.ManageSystemSettings,
                "Bạn không có quyền thay đổi cài đặt hệ thống.");
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            SystemSettingUpdateRequest input =
                SystemSettingValidator.Validate(request);
            SystemSetting? setting = await settingRepository.GetByKeyAsync(
                input.Key,
                cancellationToken);
            if (setting is null)
            {
                return OperationResult.Failure(
                    "Cài đặt hệ thống không tồn tại.");
            }

            if (setting.Value == input.Value)
            {
                return OperationResult.Success();
            }

            CurrentUser actor = authenticationService.GetCurrentUser()!;
            setting.Value = input.Value;
            setting.UpdatedByEmployeeId = actor.EmployeeId;
            setting.UpdatedAt = DateTime.UtcNow;
            await settingRepository.SaveAsync(
                setting,
                new ActivityLog
                {
                    EmployeeId = actor.EmployeeId,
                    Action = "SystemSettingUpdated",
                    EntityName = nameof(SystemSetting),
                    EntityId = setting.Key,
                    Description =
                        $"Cập nhật cài đặt {setting.Key} thành {setting.Value}."
                },
                cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể cập nhật cài đặt {Key}.",
                request.Key);
            return OperationResult.Failure(
                "Không thể cập nhật cài đặt hệ thống.");
        }
    }

    public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        AdministrationServiceAuthorization.Demand(
            authenticationService,
            Permission.ManageSystemSettings,
            "Bạn không có quyền xem cài đặt hệ thống.");
        IReadOnlyList<SystemSetting> settings =
            await settingRepository.GetAllAsync(cancellationToken);
        return settings.Select(
                setting => new SystemSettingDto(
                    setting.Id,
                    setting.Key,
                    setting.Value,
                    setting.Description,
                    setting.UpdatedByEmployee.FullName,
                    setting.UpdatedAt))
            .ToArray();
    }
}
