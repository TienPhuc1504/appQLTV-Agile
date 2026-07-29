using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IReturnService
{
    Task<IReadOnlyList<ReturnLookupDto>> SearchOutstandingAsync(
        string keyword,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ReturnBookAsync(
        ReturnBookRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationResult> ReturnMultipleBooksAsync(
        ReturnMultipleBooksRequest request,
        CancellationToken cancellationToken = default);

    int CalculateOverdueDays(
        DateOnly expectedReturnDate,
        DateOnly actualReturnDate);

    Task<ReturnPreviewDto> CalculateFineAsync(
        int borrowSlipDetailId,
        PhysicalCondition returnedCondition,
        DateOnly returnDate,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateBorrowSlipStatusAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default);
}
