using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class ReaderService(
    IReaderRepository readerRepository,
    IAuthenticationService authenticationService,
    ILogger<ReaderService> logger)
    : IReaderService
{
    public Task<PagedResult<ReaderListItemDto>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(
            new ReaderSearchRequest(
                PageNumber: pageNumber,
                PageSize: pageSize),
            cancellationToken);
    }

    public async Task<PagedResult<ReaderListItemDto>> SearchAsync(
        ReaderSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        ReaderSearchRequest normalizedRequest = NormalizeSearchRequest(request);
        PagedResult<Reader> result =
            await readerRepository.SearchAsync(
                normalizedRequest,
                cancellationToken);
        return new PagedResult<ReaderListItemDto>(
            result.Items.Select(MapListItem).ToArray(),
            result.TotalCount,
            result.PageNumber,
            result.PageSize);
    }

    public async Task<ReaderDetailDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        Reader? reader = id <= 0
            ? null
            : await readerRepository.GetByIdAsync(id, cancellationToken);
        return reader is null ? null : MapDetail(reader);
    }

    public async Task<OperationResult> CreateAsync(
        ReaderUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            ReaderServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            ReaderInput input = Validate(request);
            if (await readerRepository.ReaderCodeExistsAsync(
                    input.ReaderCode,
                    cancellationToken: cancellationToken))
            {
                return OperationResult.Failure("Mã độc giả đã tồn tại.");
            }

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            var reader = new Reader
            {
                ReaderCode = input.ReaderCode,
                FullName = input.FullName,
                DateOfBirth = input.DateOfBirth,
                Gender = input.Gender,
                PhoneNumber = input.PhoneNumber,
                Email = input.Email,
                Address = input.Address,
                ReaderType = input.ReaderType,
                RegisteredAt = input.RegisteredAt,
                ExpirationDate = input.ExpirationDate,
                AvatarPath = input.AvatarPath,
                Status = input.ExpirationDate < today
                    ? ReaderStatus.Expired
                    : ReaderStatus.Active,
                Notes = input.Notes
            };
            await readerRepository.AddAsync(reader, cancellationToken);
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
                "Không thể tạo độc giả {ReaderCode}.",
                request.ReaderCode);
            return OperationResult.Failure(
                "Không thể lưu độc giả. Vui lòng kiểm tra dữ liệu trùng lặp.");
        }
    }

    public async Task<OperationResult> UpdateAsync(
        int id,
        ReaderUpsertRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            ReaderServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        if (id <= 0)
        {
            return OperationResult.Failure("Độc giả không tồn tại.");
        }

        try
        {
            ReaderInput input = Validate(request);
            Reader? reader =
                await readerRepository.GetByIdAsync(id, cancellationToken);
            if (reader is null)
            {
                return OperationResult.Failure("Độc giả không tồn tại.");
            }

            if (!string.Equals(
                    reader.ReaderCode,
                    input.ReaderCode,
                    StringComparison.Ordinal))
            {
                return OperationResult.Failure(
                    "Không thể thay đổi mã độc giả sau khi đã tạo.");
            }

            if (reader.RegisteredAt != input.RegisteredAt
                || reader.ExpirationDate != input.ExpirationDate)
            {
                return OperationResult.Failure(
                    "Ngày đăng ký và ngày hết hạn chỉ được thay đổi qua chức năng gia hạn thẻ.");
            }

            reader.FullName = input.FullName;
            reader.DateOfBirth = input.DateOfBirth;
            reader.Gender = input.Gender;
            reader.PhoneNumber = input.PhoneNumber;
            reader.Email = input.Email;
            reader.Address = input.Address;
            reader.ReaderType = input.ReaderType;
            reader.AvatarPath = input.AvatarPath;
            reader.Notes = input.Notes;
            await readerRepository.UpdateAsync(reader, cancellationToken);
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
                "Không thể cập nhật độc giả có mã {ReaderId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật độc giả. Vui lòng kiểm tra dữ liệu.");
        }
    }

    public Task<OperationResult> LockAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return ChangeLockStateAsync(id, lockReader: true, cancellationToken);
    }

    public Task<OperationResult> UnlockAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return ChangeLockStateAsync(id, lockReader: false, cancellationToken);
    }

    public async Task<OperationResult> RenewCardAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            ReaderServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Reader? reader = id <= 0
            ? null
            : await readerRepository.GetByIdAsync(id, cancellationToken);
        if (reader is null)
        {
            return OperationResult.Failure("Độc giả không tồn tại.");
        }

        if (reader.Status == ReaderStatus.Inactive)
        {
            return OperationResult.Failure(
                "Không thể gia hạn thẻ cho độc giả đã ngừng hoạt động.");
        }

        int? validityMonths = await GetCardValidityMonthsAsync(cancellationToken);
        if (!validityMonths.HasValue)
        {
            return OperationResult.Failure(
                "Cấu hình thời hạn thẻ độc giả không hợp lệ.");
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly renewalBaseDate =
            reader.ExpirationDate >= today ? reader.ExpirationDate : today;
        reader.ExpirationDate = renewalBaseDate.AddMonths(validityMonths.Value);
        if (reader.Status == ReaderStatus.Expired)
        {
            reader.Status = ReaderStatus.Active;
        }

        try
        {
            await readerRepository.UpdateAsync(reader, cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể gia hạn thẻ độc giả có mã {ReaderId}.",
                id);
            return OperationResult.Failure("Không thể gia hạn thẻ độc giả.");
        }
    }

    public async Task<DateOnly> GetSuggestedExpirationDateAsync(
        DateOnly registeredAt,
        CancellationToken cancellationToken = default)
    {
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        if (registeredAt > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new DomainValidationException(
                "Ngày đăng ký không được lớn hơn ngày hiện tại.");
        }

        int? validityMonths = await GetCardValidityMonthsAsync(cancellationToken);
        if (!validityMonths.HasValue)
        {
            throw new InvalidOperationException(
                "Cấu hình thời hạn thẻ độc giả không hợp lệ.");
        }

        return registeredAt.AddMonths(validityMonths.Value);
    }

    public async Task<IReadOnlyList<ReaderBorrowHistoryDto>>
        GetBorrowingHistoryAsync(
            int readerId,
            CancellationToken cancellationToken = default)
    {
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        if (readerId <= 0
            || await readerRepository.GetByIdAsync(
                readerId,
                cancellationToken) is null)
        {
            return [];
        }

        return await readerRepository.GetBorrowingHistoryAsync(
            readerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ReaderFineDto>> GetOutstandingFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        if (readerId <= 0
            || await readerRepository.GetByIdAsync(
                readerId,
                cancellationToken) is null)
        {
            return [];
        }

        return await readerRepository.GetOutstandingFinesAsync(
            readerId,
            cancellationToken);
    }

    public async Task<OperationResult> ValidateBorrowEligibilityAsync(
        int readerId,
        DateOnly? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        ReaderServiceAuthorization.DemandReadAccess(authenticationService);
        Reader? reader = readerId <= 0
            ? null
            : await readerRepository.GetByIdAsync(readerId, cancellationToken);
        if (reader is null)
        {
            return OperationResult.Failure("Độc giả không tồn tại.");
        }

        if (reader.Status == ReaderStatus.Locked)
        {
            return OperationResult.Failure(
                "Độc giả đang bị khóa.");
        }

        DateOnly date =
            evaluationDate ?? DateOnly.FromDateTime(DateTime.Today);
        if (reader.Status == ReaderStatus.Expired
            || reader.ExpirationDate < date)
        {
            return OperationResult.Failure("Thẻ độc giả đã hết hạn.");
        }

        return reader.Status == ReaderStatus.Active
            ? OperationResult.Success()
            : OperationResult.Failure("Độc giả không còn hoạt động.");
    }

    private async Task<OperationResult> ChangeLockStateAsync(
        int id,
        bool lockReader,
        CancellationToken cancellationToken)
    {
        OperationResult? accessFailure =
            ReaderServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        Reader? reader = id <= 0
            ? null
            : await readerRepository.GetByIdAsync(id, cancellationToken);
        if (reader is null)
        {
            return OperationResult.Failure("Độc giả không tồn tại.");
        }

        if (lockReader)
        {
            if (reader.Status == ReaderStatus.Inactive)
            {
                return OperationResult.Failure(
                    "Không thể khóa độc giả đã ngừng hoạt động.");
            }

            if (reader.Status == ReaderStatus.Locked)
            {
                return OperationResult.Success();
            }

            reader.Status = ReaderStatus.Locked;
        }
        else
        {
            if (reader.Status != ReaderStatus.Locked)
            {
                return OperationResult.Failure(
                    "Chỉ có thể mở khóa độc giả đang bị khóa.");
            }

            if (reader.ExpirationDate
                < DateOnly.FromDateTime(DateTime.Today))
            {
                return OperationResult.Failure(
                    "Thẻ độc giả đã hết hạn. Vui lòng gia hạn thẻ trước khi mở khóa.");
            }

            reader.Status = ReaderStatus.Active;
        }

        try
        {
            await readerRepository.UpdateAsync(reader, cancellationToken);
            return OperationResult.Success();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(
                exception,
                "Không thể đổi trạng thái khóa của độc giả {ReaderId}.",
                id);
            return OperationResult.Failure(
                "Không thể cập nhật trạng thái độc giả.");
        }
    }

    private static ReaderSearchRequest NormalizeSearchRequest(
        ReaderSearchRequest request)
    {
        int pageNumber = Math.Max(1, request.PageNumber);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);
        ReaderStatus? status =
            request.Status.HasValue && Enum.IsDefined(request.Status.Value)
                ? request.Status
                : null;
        ReaderType? readerType =
            request.ReaderType.HasValue
                && Enum.IsDefined(request.ReaderType.Value)
                    ? request.ReaderType
                    : null;
        ReaderSortField sortBy = Enum.IsDefined(request.SortBy)
            ? request.SortBy
            : ReaderSortField.FullName;
        return request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                150,
                "Từ khóa"),
            Status = status,
            ReaderType = readerType,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy
        };
    }

    private async Task<int?> GetCardValidityMonthsAsync(
        CancellationToken cancellationToken)
    {
        int? validityMonths =
            await readerRepository.GetReaderCardValidityMonthsAsync(
                cancellationToken);
        if (validityMonths is null or <= 0 or > 120)
        {
            logger.LogError(
                "Cấu hình ReaderCardValidityMonths không tồn tại hoặc không hợp lệ.");
            return null;
        }

        return validityMonths;
    }

    private static ReaderInput Validate(ReaderUpsertRequest request)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        ReaderValidator.Enums(request.Gender, request.ReaderType);
        ReaderValidator.CardDates(
            request.RegisteredAt,
            request.ExpirationDate,
            today);
        return new ReaderInput(
            ReaderValidator.ReaderCode(request.ReaderCode),
            ReaderValidator.FullName(request.FullName),
            ReaderValidator.DateOfBirth(
                request.DateOfBirth,
                today,
                request.RegisteredAt),
            request.Gender,
            DomainValidator.OptionalPhoneNumber(request.PhoneNumber),
            DomainValidator.OptionalEmail(request.Email),
            DomainValidator.OptionalMaximumLength(
                request.Address,
                500,
                "Địa chỉ"),
            request.ReaderType,
            request.RegisteredAt,
            request.ExpirationDate,
            DomainValidator.OptionalMaximumLength(
                request.AvatarPath,
                500,
                "Đường dẫn ảnh đại diện"),
            DomainValidator.OptionalMaximumLength(
                request.Notes,
                1000,
                "Ghi chú"));
    }

    private static ReaderListItemDto MapListItem(Reader reader)
    {
        return new ReaderListItemDto(
            reader.Id,
            reader.ReaderCode,
            reader.FullName,
            reader.DateOfBirth,
            reader.Gender,
            reader.PhoneNumber,
            reader.Email,
            reader.ReaderType,
            reader.ExpirationDate,
            GetEffectiveStatus(reader));
    }

    private static ReaderDetailDto MapDetail(Reader reader)
    {
        return new ReaderDetailDto(
            reader.Id,
            reader.ReaderCode,
            reader.FullName,
            reader.DateOfBirth,
            reader.Gender,
            reader.PhoneNumber,
            reader.Email,
            reader.Address,
            reader.ReaderType,
            reader.RegisteredAt,
            reader.ExpirationDate,
            reader.AvatarPath,
            GetEffectiveStatus(reader),
            reader.Notes,
            reader.CreatedAt,
            reader.UpdatedAt);
    }

    private static ReaderStatus GetEffectiveStatus(Reader reader)
    {
        return reader.Status == ReaderStatus.Active
            && reader.ExpirationDate < DateOnly.FromDateTime(DateTime.Today)
                ? ReaderStatus.Expired
                : reader.Status;
    }

    private sealed record ReaderInput(
        string ReaderCode,
        string FullName,
        DateOnly? DateOfBirth,
        Gender Gender,
        string? PhoneNumber,
        string? Email,
        string? Address,
        ReaderType ReaderType,
        DateOnly RegisteredAt,
        DateOnly ExpirationDate,
        string? AvatarPath,
        string? Notes);
}
