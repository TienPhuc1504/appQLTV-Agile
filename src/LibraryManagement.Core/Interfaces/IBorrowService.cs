using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IBorrowService
{
    Task<BorrowPolicyDto> GetBorrowPolicyAsync(
        CancellationToken cancellationToken = default);

    Task<OperationResult> ValidateBorrowRequestAsync(
        BorrowCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ValidateReaderEligibilityAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> CreateBorrowSlipAsync(
        BorrowCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<BorrowSlipDto?> GetBorrowSlipAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BorrowSlipListItemDto>> GetActiveBorrowSlipsAsync(
        BorrowSlipSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BorrowSlipDetailDto>> GetReaderActiveBorrowsAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<OperationResult> RenewBorrowedBookAsync(
        int borrowSlipDetailId,
        CancellationToken cancellationToken = default);
}
