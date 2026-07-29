using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IFineService
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

    Task<OperationResult> CreateFineAsync(
        FineCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> PayFineAsync(
        FinePaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> WaiveFineAsync(
        FineWaiveRequest request,
        CancellationToken cancellationToken = default);

    Task<decimal> GetOutstandingAmountAsync(
        int readerId,
        CancellationToken cancellationToken = default);
}
