using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class CategoryService(
    ICategoryRepository categoryRepository,
    IAuthenticationService authenticationService,
    ILogger<CategoryService> logger)
    : ICategoryService
{
    public Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(null, includeInactive, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        IReadOnlyList<Category> categories =
            await categoryRepository.SearchAsync(
                keyword,
                includeInactive,
                cancellationToken);
        return categories.Select(Map).ToArray();
    }

    public async Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        if (id <= 0)
        {
            return null;
        }

        Category? category =
            await categoryRepository.GetByIdAsync(id, cancellationToken);
        return category is null ? null : Map(category);
    }

    public async Task<OperationResult> CreateAsync(
        CategoryUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            CategoryInput input = Validate(request);
            if (await categoryRepository.NameExistsAsync(
                    input.Name,
                    cancellationToken: cancellationToken))
            {
                return OperationResult.Failure("Tên thể loại đã tồn tại.");
            }

            var category = new Category
            {
                Name = input.Name,
                Description = input.Description,
                IsActive = input.IsActive
            };
            await categoryRepository.AddAsync(category, cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể tạo thể loại {CategoryName}.", request.Name);
            return OperationResult.Failure(
                "Không thể lưu thể loại. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        CategoryUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        if (id <= 0)
        {
            return OperationResult.Failure("Thể loại không tồn tại.");
        }

        try
        {
            CategoryInput input = Validate(request);
            Category? category =
                await categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category is null)
            {
                return OperationResult.Failure("Thể loại không tồn tại.");
            }

            if (await categoryRepository.NameExistsAsync(
                    input.Name,
                    id,
                    cancellationToken))
            {
                return OperationResult.Failure("Tên thể loại đã tồn tại.");
            }

            category.Name = input.Name;
            category.Description = input.Description;
            category.IsActive = input.IsActive;
            await categoryRepository.UpdateAsync(category, cancellationToken);
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
                "Không thể cập nhật thể loại có mã {CategoryId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật thể loại. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
    }

    public async Task<OperationResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Category? category =
            await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return OperationResult.Failure("Thể loại không tồn tại.");
        }

        try
        {
            category.IsActive = isActive;
            await categoryRepository.UpdateAsync(category, cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi trạng thái thể loại có mã {CategoryId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật trạng thái thể loại.");
        }
    }

    private static CategoryInput Validate(CategoryUpsertRequest request)
    {
        string name = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Name, "tên thể loại"),
            100,
            "Tên thể loại");
        string? description = DomainValidator.OptionalMaximumLength(
            request.Description,
            500,
            "Mô tả");
        return new CategoryInput(name, description, request.IsActive);
    }

    private static CategoryDto Map(Category category)
    {
        return new CategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
    }

    private sealed record CategoryInput(
        string Name,
        string? Description,
        bool IsActive);
}
