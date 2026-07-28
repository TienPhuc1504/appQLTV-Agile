using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IPublisherService
{
    Task<IReadOnlyList<PublisherDto>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublisherDto>> SearchAsync(
        string? keyword,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<PublisherDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateAsync(
        PublisherUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateAsync(
        int id,
        PublisherUpsertRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> SetActiveAsync(
        int id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
