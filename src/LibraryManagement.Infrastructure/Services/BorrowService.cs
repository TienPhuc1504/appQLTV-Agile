using System.Globalization;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Models;
using LibraryManagement.Core.Validation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LibraryManagement.Infrastructure.Services;

public sealed class BorrowService(
    IBorrowRepository borrowRepository,
    IAuthenticationService authenticationService,
    ILogger<BorrowService> logger)
    : IBorrowService
{
    private static readonly string[] PolicySettingKeys =
    [
        SystemSettingKeys.MaximumBorrowedBooks,
        SystemSettingKeys.DefaultBorrowDays,
        SystemSettingKeys.MaximumOutstandingFineAmount
    ];

    public async Task<BorrowPolicyDto> GetBorrowPolicyAsync(
        CancellationToken cancellationToken = default)
    {
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        IReadOnlyDictionary<string, string> settings =
            await borrowRepository.GetSettingsAsync(
                PolicySettingKeys,
                cancellationToken);
        return ParsePolicy(settings);
    }

    public async Task<OperationResult> ValidateBorrowRequestAsync(
        BorrowCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        try
        {
            ValidatedBorrowRequest input = BorrowValidator.Validate(request);
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            BorrowValidationSnapshot snapshot =
                await borrowRepository.GetValidationSnapshotAsync(
                    input.ReaderId,
                    input.BookCopyIds,
                    today,
                    cancellationToken);
            return EvaluateBorrowRequest(snapshot, input.BookCopyIds, today);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    public async Task<OperationResult> ValidateReaderEligibilityAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        if (readerId <= 0)
        {
            return OperationResult.Failure(
                "Vui lòng chọn độc giả cần mượn sách.");
        }

        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            BorrowValidationSnapshot snapshot =
                await borrowRepository.GetValidationSnapshotAsync(
                    readerId,
                    [],
                    today,
                    cancellationToken);
            BorrowPolicyDto policy = ParsePolicy(snapshot.Settings);
            return EvaluateReaderEligibility(snapshot, policy, today);
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
    }

    public async Task<OperationResult> CreateBorrowSlipAsync(
        BorrowCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            BorrowServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        CurrentUser? currentUser = authenticationService.GetCurrentUser();
        if (currentUser is null)
        {
            return OperationResult.Failure(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        try
        {
            ValidatedBorrowRequest input = BorrowValidator.Validate(request);
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            await using IBorrowTransaction transaction =
                await borrowRepository.BeginTransactionAsync(cancellationToken);
            BorrowValidationSnapshot snapshot =
                await transaction.GetValidationSnapshotAsync(
                    input.ReaderId,
                    input.BookCopyIds,
                    today,
                    cancellationToken);
            OperationResult validationResult =
                EvaluateBorrowRequest(snapshot, input.BookCopyIds, today);
            if (!validationResult.Succeeded)
            {
                return validationResult;
            }

            BorrowPolicyDto policy = ParsePolicy(snapshot.Settings);
            DateOnly expectedReturnDate =
                today.AddDays(policy.DefaultBorrowDays);
            string borrowCode = CreateBorrowCode(today);
            var borrowSlip = new BorrowSlip
            {
                BorrowCode = borrowCode,
                ReaderId = input.ReaderId,
                EmployeeId = currentUser.EmployeeId,
                BorrowDate = today,
                ExpectedReturnDate = expectedReturnDate,
                Status = BorrowSlipStatus.Active,
                Notes = input.Notes,
                Details = input.BookCopyIds
                    .Select(bookCopyId => new BorrowSlipDetail
                    {
                        BookCopyId = bookCopyId,
                        ExpectedReturnDate = expectedReturnDate,
                        RenewalCount = 0,
                        Status = BorrowSlipDetailStatus.Borrowing
                    })
                    .ToArray()
            };
            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "BorrowCreated",
                EntityName = nameof(BorrowSlip),
                Description =
                    $"Tạo phiếu mượn {borrowCode} cho độc giả "
                    + $"{snapshot.Reader!.ReaderCode} với "
                    + $"{input.BookCopyIds.Count} bản sách."
            };
            await transaction.PersistAsync(
                borrowSlip,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Đã tạo phiếu mượn {BorrowCode} cho độc giả {ReaderCode}.",
                borrowCode,
                snapshot.Reader.ReaderCode);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (BorrowConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (SqliteException exception)
            when (exception.SqliteErrorCode is 5 or 6)
        {
            logger.LogWarning(
                exception,
                "Database đang bận khi tạo phiếu mượn.");
            return OperationResult.Failure(
                "Dữ liệu đang được xử lý bởi thao tác khác. Vui lòng thử lại.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể lưu phiếu mượn.");
            return OperationResult.Failure(
                "Không thể tạo phiếu mượn. Dữ liệu có thể vừa thay đổi.");
        }
    }

    public Task<BorrowSlipDto?> GetBorrowSlipAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default)
    {
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        return borrowSlipId <= 0
            ? Task.FromResult<BorrowSlipDto?>(null)
            : borrowRepository.GetBorrowSlipAsync(
                borrowSlipId,
                cancellationToken);
    }

    public Task<PagedResult<BorrowSlipListItemDto>> GetActiveBorrowSlipsAsync(
        BorrowSlipSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        var normalizedRequest = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                150,
                "Từ khóa"),
            ReaderId = request.ReaderId > 0 ? request.ReaderId : null,
            PageNumber = Math.Max(1, request.PageNumber),
            PageSize = Math.Clamp(request.PageSize, 1, 100)
        };
        return borrowRepository.GetActiveBorrowSlipsAsync(
            normalizedRequest,
            cancellationToken);
    }

    public Task<IReadOnlyList<BorrowSlipDetailDto>>
        GetReaderActiveBorrowsAsync(
            int readerId,
            CancellationToken cancellationToken = default)
    {
        BorrowServiceAuthorization.DemandReadAccess(authenticationService);
        return readerId <= 0
            ? Task.FromResult<IReadOnlyList<BorrowSlipDetailDto>>([])
            : borrowRepository.GetReaderActiveBorrowsAsync(
                readerId,
                cancellationToken);
    }

    public async Task<OperationResult> RenewBorrowedBookAsync(
        int borrowSlipDetailId,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            BorrowServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        CurrentUser? currentUser = authenticationService.GetCurrentUser();
        if (currentUser is null)
        {
            return OperationResult.Failure(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
        }

        if (borrowSlipDetailId <= 0)
        {
            return OperationResult.Failure(
                "Chi tiết mượn sách không hợp lệ.");
        }

        try
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            await using IBorrowTransaction transaction =
                await borrowRepository.BeginTransactionAsync(cancellationToken);
            RenewalSnapshot? snapshot =
                await transaction.GetRenewalSnapshotAsync(
                    borrowSlipDetailId,
                    cancellationToken);
            if (snapshot is null)
            {
                return OperationResult.Failure(
                    "Không tìm thấy sách đang mượn.");
            }

            OperationResult validation = ValidateRenewal(snapshot, today);
            if (!validation.Succeeded)
            {
                return validation;
            }

            int renewalDays = ParseIntSetting(
                snapshot.Settings,
                SystemSettingKeys.RenewalDays,
                minimum: 1,
                maximum: 365);
            DateOnly newExpectedReturnDate =
                snapshot.ExpectedReturnDate.AddDays(renewalDays);
            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "BorrowRenewed",
                EntityName = nameof(BorrowSlipDetail),
                Description =
                    $"Gia hạn bản sách {snapshot.CopyCode} đến "
                    + $"{newExpectedReturnDate:dd/MM/yyyy}."
            };
            await transaction.PersistRenewalAsync(
                borrowSlipDetailId,
                newExpectedReturnDate,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Đã gia hạn chi tiết mượn {BorrowSlipDetailId} đến {ExpectedReturnDate}.",
                borrowSlipDetailId,
                newExpectedReturnDate);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (BorrowConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (SqliteException exception)
            when (exception.SqliteErrorCode is 5 or 6)
        {
            logger.LogWarning(
                exception,
                "Database đang bận khi gia hạn sách.");
            return OperationResult.Failure(
                "Dữ liệu đang được xử lý bởi thao tác khác. Vui lòng thử lại.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể lưu giao dịch gia hạn.");
            return OperationResult.Failure(
                "Không thể gia hạn sách. Dữ liệu có thể vừa thay đổi.");
        }
    }

    private static OperationResult ValidateRenewal(
        RenewalSnapshot snapshot,
        DateOnly evaluationDate)
    {
        if (snapshot.DetailStatus != BorrowSlipDetailStatus.Borrowing
            || snapshot.ActualReturnDate is not null)
        {
            return OperationResult.Failure(
                "Sách đã được trả hoặc không còn được phép gia hạn.");
        }

        if (snapshot.ExpectedReturnDate < evaluationDate)
        {
            return OperationResult.Failure(
                "Không được gia hạn sách đã quá hạn.");
        }

        if (snapshot.ReaderStatus == ReaderStatus.Expired
            || snapshot.ReaderExpirationDate < evaluationDate)
        {
            return OperationResult.Failure(
                "Thẻ độc giả đã hết hạn.");
        }

        if (snapshot.ReaderStatus != ReaderStatus.Active)
        {
            return OperationResult.Failure(
                "Độc giả không còn hoạt động.");
        }

        if (snapshot.BookCopyStatus != BookCopyStatus.Borrowed)
        {
            return OperationResult.Failure(
                "Trạng thái bản sách không hợp lệ để gia hạn.");
        }

        if (snapshot.BorrowSlipStatus is
            BorrowSlipStatus.Completed or BorrowSlipStatus.Cancelled)
        {
            return OperationResult.Failure(
                "Phiếu mượn không còn hiệu lực.");
        }

        int maximumRenewalCount = ParseIntSetting(
            snapshot.Settings,
            SystemSettingKeys.MaximumRenewalCount,
            minimum: 0,
            maximum: 100);
        if (snapshot.RenewalCount >= maximumRenewalCount)
        {
            return OperationResult.Failure(
                $"Sách chỉ được gia hạn tối đa {maximumRenewalCount} lần.");
        }

        return OperationResult.Success();
    }

    private static OperationResult EvaluateBorrowRequest(
        BorrowValidationSnapshot snapshot,
        IReadOnlyCollection<int> requestedBookCopyIds,
        DateOnly evaluationDate)
    {
        BorrowPolicyDto policy = ParsePolicy(snapshot.Settings);
        OperationResult readerEligibility =
            EvaluateReaderEligibility(snapshot, policy, evaluationDate);
        if (!readerEligibility.Succeeded)
        {
            return readerEligibility;
        }

        if (snapshot.ActiveBorrowedCopyCount + requestedBookCopyIds.Count
            > policy.MaximumBorrowedBooks)
        {
            return OperationResult.Failure(
                $"Độc giả chỉ được mượn tối đa "
                + $"{policy.MaximumBorrowedBooks} bản sách.");
        }

        if (snapshot.BookCopies.Count != requestedBookCopyIds.Count)
        {
            return OperationResult.Failure(
                "Một hoặc nhiều bản sách không tồn tại.");
        }

        BorrowCopySnapshot? unavailableCopy = snapshot.BookCopies
            .FirstOrDefault(copy =>
                !copy.IsBookActive
                || copy.Status != BookCopyStatus.Available);
        if (unavailableCopy is not null)
        {
            return OperationResult.Failure(
                $"Bản sách {unavailableCopy.CopyCode} hiện không có sẵn.");
        }

        return OperationResult.Success();
    }

    private static OperationResult EvaluateReaderEligibility(
        BorrowValidationSnapshot snapshot,
        BorrowPolicyDto policy,
        DateOnly evaluationDate)
    {
        BorrowReaderSnapshot? reader = snapshot.Reader;
        if (reader is null)
        {
            return OperationResult.Failure("Độc giả không tồn tại.");
        }

        if (reader.Status == ReaderStatus.Locked)
        {
            return OperationResult.Failure("Độc giả đang bị khóa.");
        }

        if (reader.Status == ReaderStatus.Expired
            || reader.ExpirationDate < evaluationDate)
        {
            return OperationResult.Failure("Thẻ độc giả đã hết hạn.");
        }

        if (reader.Status != ReaderStatus.Active)
        {
            return OperationResult.Failure(
                "Độc giả không còn hoạt động.");
        }

        if (snapshot.HasOverdueBorrow)
        {
            return OperationResult.Failure(
                "Độc giả đang có sách quá hạn chưa trả.");
        }

        if (snapshot.ActiveBorrowedCopyCount
            >= policy.MaximumBorrowedBooks)
        {
            return OperationResult.Failure(
                $"Độc giả đã mượn tối đa {policy.MaximumBorrowedBooks} bản sách cho phép.");
        }

        if (snapshot.OutstandingFineAmount
            > policy.MaximumOutstandingFineAmount)
        {
            return OperationResult.Failure(
                "Tiền phạt chưa thanh toán của độc giả vượt mức cho phép.");
        }

        return OperationResult.Success();
    }

    private static BorrowPolicyDto ParsePolicy(
        IReadOnlyDictionary<string, string> settings)
    {
        int maximumBorrowedBooks = ParseIntSetting(
            settings,
            SystemSettingKeys.MaximumBorrowedBooks,
            minimum: 1,
            maximum: 100);
        int defaultBorrowDays = ParseIntSetting(
            settings,
            SystemSettingKeys.DefaultBorrowDays,
            minimum: 1,
            maximum: 365);
        decimal maximumOutstandingFineAmount = ParseDecimalSetting(
            settings,
            SystemSettingKeys.MaximumOutstandingFineAmount);
        return new BorrowPolicyDto(
            maximumBorrowedBooks,
            defaultBorrowDays,
            maximumOutstandingFineAmount);
    }

    private static int ParseIntSetting(
        IReadOnlyDictionary<string, string> settings,
        string key,
        int minimum,
        int maximum)
    {
        if (!settings.TryGetValue(key, out string? value)
            || !int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int result)
            || result < minimum
            || result > maximum)
        {
            throw new DomainValidationException(
                $"Cấu hình {key} không hợp lệ.");
        }

        return result;
    }

    private static decimal ParseDecimalSetting(
        IReadOnlyDictionary<string, string> settings,
        string key)
    {
        if (!settings.TryGetValue(key, out string? value)
            || !decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal result)
            || result < 0)
        {
            throw new DomainValidationException(
                $"Cấu hình {key} không hợp lệ.");
        }

        return result;
    }

    private static string CreateBorrowCode(DateOnly borrowDate)
    {
        return $"PM{borrowDate:yyyyMMdd}-{Guid.NewGuid():N}"[..27]
            .ToUpperInvariant();
    }
}
