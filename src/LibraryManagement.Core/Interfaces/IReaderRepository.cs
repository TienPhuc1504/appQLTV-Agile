using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Models;

namespace LibraryManagement.Core.Interfaces;

public interface IReaderRepository
{
    Task<PagedResult<Reader>> SearchAsync(
        ReaderSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<Reader?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> ReaderCodeExistsAsync(
        string readerCode,
        int? excludingId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReaderBorrowHistoryDto>> GetBorrowingHistoryAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReaderFineDto>> GetOutstandingFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default);

    Task<int?> GetReaderCardValidityMonthsAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Reader reader,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Reader reader,
        CancellationToken cancellationToken = default);
}
