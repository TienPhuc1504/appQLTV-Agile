using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IAuthorService
{
    Task<IReadOnlyList<AuthorDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AuthorDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<AuthorDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        AuthorUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        AuthorUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
