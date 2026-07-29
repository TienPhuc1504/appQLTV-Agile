using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Infrastructure.Services;

public sealed class ActivityLogService(
    IActivityLogRepository activityLogRepository,
    IAuthenticationService authenticationService)
    : IActivityLogService
{
    public Task LogAsync(
        string action,
        string entityName,
        string? entityId,
        string description,
        CancellationToken cancellationToken = default)
    {
        CurrentUser currentUser = authenticationService.GetCurrentUser()
            ?? throw new UnauthorizedAccessException(
                "Phiên đăng nhập không hợp lệ.");
        string validatedAction = DomainValidator.MaximumLength(
            DomainValidator.Required(action, "hành động"),
            100,
            "Hành động");
        string validatedEntityName = DomainValidator.MaximumLength(
            DomainValidator.Required(entityName, "tên đối tượng"),
            100,
            "Tên đối tượng");
        string validatedDescription = DomainValidator.MaximumLength(
            DomainValidator.Required(description, "mô tả"),
            2000,
            "Mô tả");
        string? validatedEntityId = DomainValidator.OptionalMaximumLength(
            entityId,
            100,
            "Mã đối tượng");
        return activityLogRepository.AddAsync(
            new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = validatedAction,
                EntityName = validatedEntityName,
                EntityId = validatedEntityId,
                Description = validatedDescription
            },
            cancellationToken);
    }

    public Task<PagedResult<ActivityLogDto>> GetAllAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(request, cancellationToken);
    }

    public Task<PagedResult<ActivityLogDto>> SearchAsync(
        ActivityLogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        AdministrationServiceAuthorization.Demand(
            authenticationService,
            Permission.ViewActivityLogs,
            "Bạn không có quyền xem nhật ký hoạt động.");
        if (request.From.HasValue
            && request.To.HasValue
            && request.From > request.To)
        {
            throw new DomainValidationException(
                "Thời gian bắt đầu không được lớn hơn thời gian kết thúc.");
        }

        var normalizedRequest = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                200,
                "Từ khóa"),
            EmployeeId = request.EmployeeId > 0
                ? request.EmployeeId
                : null,
            Action = DomainValidator.OptionalMaximumLength(
                request.Action,
                100,
                "Hành động"),
            PageNumber = Math.Max(1, request.PageNumber),
            PageSize = Math.Clamp(request.PageSize, 1, 100)
        };
        return activityLogRepository.SearchAsync(
            normalizedRequest,
            cancellationToken);
    }
}
