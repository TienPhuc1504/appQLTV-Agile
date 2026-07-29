using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IFineRepository
{
    Task<PagedResult<FineListItemDto>> GetAllAsync(
        FineSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<FineDetailDto?> GetByIdAsync(
        int fineId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FineListItemDto>> GetReaderFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetOutstandingAmountAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<FineCreationSnapshot> GetCreationSnapshotAsync(
        int readerId,
        int borrowSlipDetailId,
        CancellationToken cancellationToken = default);

    Task<IFineTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}
