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

public sealed class FineService(
    IFineRepository fineRepository,
    IAuthenticationService authenticationService,
    ILogger<FineService> logger)
    : IFineService
{
    public Task<PagedResult<FineListItemDto>> GetAllAsync(
        FineSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        FineServiceAuthorization.DemandReadAccess(authenticationService);
        var normalizedRequest = request with
        {
            Keyword = DomainValidator.OptionalMaximumLength(
                request.Keyword,
                150,
                "Từ khóa"),
            ReaderId = request.ReaderId > 0 ? request.ReaderId : null,
            FineType = request.FineType.HasValue
                && Enum.IsDefined(request.FineType.Value)
                    ? request.FineType
                    : null,
            Status = request.Status.HasValue
                && Enum.IsDefined(request.Status.Value)
                    ? request.Status
                    : null,
            PageNumber = Math.Max(1, request.PageNumber),
            PageSize = Math.Clamp(request.PageSize, 1, 100)
        };
        return fineRepository.GetAllAsync(
            normalizedRequest,
            cancellationToken);
    }

    public Task<FineDetailDto?> GetByIdAsync(
        int fineId,
        CancellationToken cancellationToken = default)
    {
        FineServiceAuthorization.DemandReadAccess(authenticationService);
        return fineId <= 0
            ? Task.FromResult<FineDetailDto?>(null)
            : fineRepository.GetByIdAsync(fineId, cancellationToken);
    }

    public Task<IReadOnlyList<FineListItemDto>> GetReaderFinesAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        FineServiceAuthorization.DemandReadAccess(authenticationService);
        return readerId <= 0
            ? Task.FromResult<IReadOnlyList<FineListItemDto>>([])
            : fineRepository.GetReaderFinesAsync(
                readerId,
                cancellationToken);
    }

    public async Task<OperationResult> CreateFineAsync(
        FineCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure = GetWriteAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            ValidatedFineCreateRequest input =
                FineValidator.ValidateCreate(request);
            FineCreationSnapshot snapshot =
                await fineRepository.GetCreationSnapshotAsync(
                    input.ReaderId,
                    input.BorrowSlipDetailId,
                    cancellationToken);
            if (!snapshot.ReaderExists)
            {
                return OperationResult.Failure("Độc giả không tồn tại.");
            }

            if (!snapshot.BorrowSlipDetailExists)
            {
                return OperationResult.Failure(
                    "Chi tiết mượn sách không tồn tại.");
            }

            if (!snapshot.DetailBelongsToReader)
            {
                return OperationResult.Failure(
                    "Chi tiết mượn sách không thuộc độc giả đã chọn.");
            }

            CurrentUser currentUser =
                authenticationService.GetCurrentUser()!;
            var fine = new Fine
            {
                FineCode = CreateFineCode(),
                ReaderId = input.ReaderId,
                BorrowSlipDetailId = input.BorrowSlipDetailId,
                FineType = input.FineType,
                Amount = input.Amount,
                PaidAmount = 0m,
                Status = FineStatus.Unpaid,
                Reason = input.Reason,
                CreatedByEmployeeId = currentUser.EmployeeId
            };
            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "FineCreated",
                EntityName = nameof(Fine),
                Description =
                    $"Tạo khoản phạt {fine.FineCode}, số tiền "
                    + $"{fine.Amount:N0}."
            };
            await using IFineTransaction transaction =
                await fineRepository.BeginTransactionAsync(cancellationToken);
            await transaction.PersistFineAsync(
                fine,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HandlePersistenceException(
                exception,
                "tạo khoản phạt");
        }
    }

    public async Task<OperationResult> PayFineAsync(
        FinePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure = GetWriteAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            ValidatedFinePaymentRequest input =
                FineValidator.ValidatePayment(request);
            CurrentUser currentUser =
                authenticationService.GetCurrentUser()!;
            await using IFineTransaction transaction =
                await fineRepository.BeginTransactionAsync(cancellationToken);
            FineTransactionSnapshot? snapshot =
                await transaction.GetSnapshotAsync(
                    input.FineId,
                    cancellationToken);
            if (snapshot is null)
            {
                return OperationResult.Failure(
                    "Khoản phạt không tồn tại.");
            }

            if (snapshot.Status is FineStatus.Paid or FineStatus.Waived)
            {
                return OperationResult.Failure(
                    "Khoản phạt đã được thanh toán hoặc miễn.");
            }

            decimal outstandingAmount =
                snapshot.Amount - snapshot.PaidAmount;
            if (input.Amount > outstandingAmount)
            {
                return OperationResult.Failure(
                    "Số tiền thanh toán không được lớn hơn số tiền còn lại.");
            }

            decimal newPaidAmount =
                snapshot.PaidAmount + input.Amount;
            var payment = new FinePayment
            {
                FineId = input.FineId,
                EmployeeId = currentUser.EmployeeId,
                Amount = input.Amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = input.PaymentMethod,
                Notes = input.Notes
            };
            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "FinePaid",
                EntityName = nameof(Fine),
                Description =
                    $"Thanh toán {input.Amount:N0} cho khoản phạt "
                    + $"{snapshot.FineCode}."
            };
            await transaction.PersistPaymentAsync(
                payment,
                newPaidAmount,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HandlePersistenceException(
                exception,
                "thanh toán tiền phạt");
        }
    }

    public async Task<OperationResult> WaiveFineAsync(
        FineWaiveRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure = GetWriteAccessFailure();
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        try
        {
            ValidatedFineWaiveRequest input =
                FineValidator.ValidateWaiver(request);
            CurrentUser currentUser =
                authenticationService.GetCurrentUser()!;
            await using IFineTransaction transaction =
                await fineRepository.BeginTransactionAsync(cancellationToken);
            FineTransactionSnapshot? snapshot =
                await transaction.GetSnapshotAsync(
                    input.FineId,
                    cancellationToken);
            if (snapshot is null)
            {
                return OperationResult.Failure(
                    "Khoản phạt không tồn tại.");
            }

            if (snapshot.Status is FineStatus.Paid or FineStatus.Waived)
            {
                return OperationResult.Failure(
                    "Khoản phạt đã được thanh toán hoặc miễn.");
            }

            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "FineWaived",
                EntityName = nameof(Fine),
                Description =
                    $"Miễn khoản phạt {snapshot.FineCode}. "
                    + $"Lý do: {input.Reason}"
            };
            await transaction.PersistWaiverAsync(
                input.FineId,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HandlePersistenceException(exception, "miễn tiền phạt");
        }
    }

    public Task<decimal> GetOutstandingAmountAsync(
        int readerId,
        CancellationToken cancellationToken = default)
    {
        FineServiceAuthorization.DemandReadAccess(authenticationService);
        return readerId <= 0
            ? Task.FromResult(0m)
            : fineRepository.GetOutstandingAmountAsync(
                readerId,
                cancellationToken);
    }

    private OperationResult? GetWriteAccessFailure()
    {
        OperationResult? permissionFailure =
            FineServiceAuthorization.GetWriteFailure(authenticationService);
        if (permissionFailure is not null)
        {
            return permissionFailure;
        }

        return authenticationService.GetCurrentUser() is null
            ? OperationResult.Failure(
                "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.")
            : null;
    }

    private OperationResult HandlePersistenceException(
        Exception exception,
        string operation)
    {
        switch (exception)
        {
            case FineConflictException conflictException:
                return OperationResult.Failure(conflictException.Message);
            case SqliteException sqliteException
                when (sqliteException.SqliteErrorCode is 5 or 6):
                logger.LogWarning(
                    sqliteException,
                    "Database đang bận khi {Operation}.",
                    operation);
                return OperationResult.Failure(
                    "Dữ liệu đang được xử lý bởi thao tác khác. Vui lòng thử lại.");
            case DbUpdateException:
                logger.LogError(
                    exception,
                    "Không thể {Operation}.",
                    operation);
                return OperationResult.Failure(
                    $"Không thể {operation}. Dữ liệu có thể vừa thay đổi.");
            default:
                logger.LogError(
                    exception,
                    "Lỗi không xác định khi {Operation}.",
                    operation);
                return OperationResult.Failure(
                    $"Không thể {operation}. Vui lòng thử lại.");
        }
    }

    private static string CreateFineCode()
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        return $"PP{today:yyyyMMdd}-{Guid.NewGuid():N}"[..27]
            .ToUpperInvariant();
    }
}
