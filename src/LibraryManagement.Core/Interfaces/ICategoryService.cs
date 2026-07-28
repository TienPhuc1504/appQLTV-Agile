using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<CategoryDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        CategoryUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        CategoryUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
