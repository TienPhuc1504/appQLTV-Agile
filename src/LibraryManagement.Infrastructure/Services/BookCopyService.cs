using System.Text.RegularExpressions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed partial class BookCopyService(
    IBookCopyRepository bookCopyRepository,
    IAuthenticationService authenticationService,
    ILogger<BookCopyService> logger)
    : IBookCopyService
{
    public async Task<PagedResult<BookCopyDto>> SearchAsync(
        BookCopySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        int pageNumber = Math.Max(1, request.PageNumber);
        int pageSize = request.PageSize is 10 or 20 or 50 or 100
            ? request.PageSize
            : 20;
        BookCopySearchRequest normalized = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                300,
                "Từ khóa"),
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        PagedResult<BookCopy> result = await bookCopyRepository.SearchAsync(
            normalized,
            cancellationToken);
        return new PagedResult<BookCopyDto>(
            result.Items.Select(Map).ToArray(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize);
    }

    public async Task<BookCopyDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        BookCopy? copy = id <= 0
            ? null
            : await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        return copy is null ? null : Map(copy);
    }

    public async Task<IReadOnlyList<BookCopyBorrowHistoryDto>> GetBorrowHistoryAsync(
        int bookCopyId,
        CancellationToken cancellationToken = default)
    {
        CatalogServiceAuthorization.DemandReadAccess(authenticationService);
        if (bookCopyId <= 0)
        {
            return [];
        }

        IReadOnlyList<BorrowSlipDetail> details =
            await bookCopyRepository.GetBorrowHistoryAsync(
                bookCopyId,
                cancellationToken);
        return details.Select(detail => new BookCopyBorrowHistoryDto(
            detail.Id,
            detail.BorrowSlip.BorrowCode,
            detail.BorrowSlip.Reader.ReaderCode,
            detail.BorrowSlip.Reader.FullName,
            detail.BorrowSlip.BorrowDate,
            detail.ExpectedReturnDate,
            detail.ActualReturnDate,
            detail.RenewalCount,
            detail.Status)).ToArray();
    }

    public async Task<OperationResult> CreateAsync(
        BookCopyUpsertRequest request,
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
            BookCopyInput input = await ValidateAsync(
                request,
                null,
                cancellationToken);
            if (input.Status == BookCopyStatus.Borrowed)
            {
                return OperationResult.Failure(
                    "Không thể tạo bản sách trực tiếp ở trạng thái đang mượn.");
            }

            var copy = new BookCopy
            {
                CopyCode = input.CopyCode,
                BookId = input.BookId,
                ShelfLocation = input.ShelfLocation,
                ImportedAt = input.ImportedAt,
                PhysicalCondition = input.PhysicalCondition,
                Status = input.Status,
                Notes = input.Notes
            };
            await bookCopyRepository.AddAsync(copy, cancellationToken);
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
                "Không thể tạo bản sách {CopyCode}.",
                request.CopyCode);
            return OperationResult.Failure(
                "Không thể lưu bản sách. Vui lòng kiểm tra mã bản sách trùng lặp.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        BookCopyUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        BookCopy? copy = id <= 0
            ? null
            : await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        if (copy is null)
        {
            return OperationResult.Failure("Bản sách không tồn tại.");
        }

        try
        {
            BookCopyInput input = await ValidateAsync(request, id, cancellationToken);
            if (copy.Status != BookCopyStatus.Borrowed
                && input.Status == BookCopyStatus.Borrowed)
            {
                return OperationResult.Failure(
                    "Trạng thái đang mượn chỉ được cập nhật qua nghiệp vụ mượn sách.");
            }

            if (copy.Status == BookCopyStatus.Borrowed
                && input.Status != BookCopyStatus.Borrowed)
            {
                return OperationResult.Failure(
                    "Bản sách đang được mượn phải được xử lý qua nghiệp vụ trả sách.");
            }

            if (!string.Equals(
                    copy.CopyCode,
                    input.CopyCode,
                    StringComparison.Ordinal)
                || copy.BookId != input.BookId)
            {
                return OperationResult.Failure(
                    "Không thể thay đổi mã bản sách hoặc đầu sách sau khi đã tạo.");
            }

            copy.ShelfLocation = input.ShelfLocation;
            copy.ImportedAt = input.ImportedAt;
            copy.PhysicalCondition = input.PhysicalCondition;
            copy.Status = input.Status;
            copy.Notes = input.Notes;
            await bookCopyRepository.UpdateAsync(copy, cancellationToken);
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
                "Không thể cập nhật bản sách có mã {BookCopyId}.",
                id);
            return OperationResult.Failure("Không thể cập nhật bản sách.");
        }
    }

    public async Task<OperationResult> ChangeStatusAsync(
        int id,
        BookCopyStatus status,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            CatalogServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        if (!Enum.IsDefined(status))
        {
            return OperationResult.Failure("Trạng thái bản sách không hợp lệ.");
        }

        BookCopy? copy = id <= 0
            ? null
            : await bookCopyRepository.GetByIdAsync(id, cancellationToken);
        if (copy is null)
        {
            return OperationResult.Failure("Bản sách không tồn tại.");
        }

        if (copy.Status == BookCopyStatus.Borrowed
            || status == BookCopyStatus.Borrowed)
        {
            return OperationResult.Failure(
                "Trạng thái đang mượn chỉ được thay đổi qua nghiệp vụ mượn – trả.");
        }

        if ((copy.PhysicalCondition == PhysicalCondition.Lost
                && status != BookCopyStatus.Lost)
            || (copy.PhysicalCondition == PhysicalCondition.Damaged
                && status != BookCopyStatus.Damaged))
        {
            return OperationResult.Failure(
                "Vui lòng cập nhật tình trạng vật lý trước khi thay đổi trạng thái bản sách.");
        }

        copy.Status = status;
        if (status == BookCopyStatus.Damaged)
        {
            copy.PhysicalCondition = PhysicalCondition.Damaged;
        }
        else if (status == BookCopyStatus.Lost)
        {
            copy.PhysicalCondition = PhysicalCondition.Lost;
        }

        await bookCopyRepository.UpdateAsync(copy, cancellationToken);
        return OperationResult.Success();
    }

    private async Task<BookCopyInput> ValidateAsync(
        BookCopyUpsertRequest request,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        string copyCode = DomainValidator.MaximumLength(
            DomainValidator.Required(request.CopyCode, "mã bản sách"),
            30,
            "Mã bản sách");
        if (!CodeRegex().IsMatch(copyCode))
        {
            throw new DomainValidationException(
                "Mã bản sách chỉ được chứa chữ cái, chữ số, dấu chấm, gạch ngang và gạch dưới.");
        }

        if (request.BookId <= 0
            || !await bookCopyRepository.ActiveBookExistsAsync(
                request.BookId,
                cancellationToken))
        {
            throw new DomainValidationException(
                "Sách không tồn tại hoặc đã ngừng lưu hành.");
        }

        if (request.ImportedAt > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new DomainValidationException(
                "Ngày nhập không được lớn hơn ngày hiện tại.");
        }

        if (!Enum.IsDefined(request.PhysicalCondition)
            || !Enum.IsDefined(request.Status))
        {
            throw new DomainValidationException(
                "Tình trạng hoặc trạng thái bản sách không hợp lệ.");
        }

        if ((request.Status == BookCopyStatus.Lost)
            != (request.PhysicalCondition == PhysicalCondition.Lost))
        {
            throw new DomainValidationException(
                "Bản sách bị mất phải có trạng thái và tình trạng vật lý tương ứng.");
        }

        if ((request.Status == BookCopyStatus.Damaged)
            != (request.PhysicalCondition == PhysicalCondition.Damaged))
        {
            throw new DomainValidationException(
                "Bản sách hư hỏng phải có trạng thái và tình trạng vật lý tương ứng.");
        }

        string normalizedCode = copyCode.ToUpperInvariant();
        if (await bookCopyRepository.CopyCodeExistsAsync(
                normalizedCode,
                excludingId,
                cancellationToken))
        {
            throw new DomainValidationException("Mã bản sách đã tồn tại.");
        }

        return new BookCopyInput(
            normalizedCode,
            request.BookId,
            DomainValidator.OptionalMaximumLength(
                request.ShelfLocation,
                100,
                "Vị trí kệ"),
            request.ImportedAt,
            request.PhysicalCondition,
            request.Status,
            DomainValidator.OptionalMaximumLength(
                request.Notes,
                1000,
                "Ghi chú"));
    }

    private static BookCopyDto Map(BookCopy copy)
    {
        return new BookCopyDto(
            copy.Id,
            copy.CopyCode,
            copy.BookId,
            copy.Book.BookCode,
            copy.Book.Title,
            copy.ShelfLocation,
            copy.ImportedAt,
            copy.PhysicalCondition,
            copy.Status,
            copy.Notes,
            copy.CreatedAt,
            copy.UpdatedAt);
    }

    [GeneratedRegex(@"^[\p{L}\p{N}._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();

    private sealed record BookCopyInput(
        string CopyCode,
        int BookId,
        string? ShelfLocation,
        DateOnly ImportedAt,
        PhysicalCondition PhysicalCondition,
        BookCopyStatus Status,
        string? Notes);
}
