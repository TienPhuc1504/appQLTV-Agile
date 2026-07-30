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

public sealed class ReturnService(
    IReturnRepository returnRepository,
    IAuthenticationService authenticationService,
    ILogger<ReturnService> logger)
    : IReturnService
{
    public Task<IReadOnlyList<ReturnLookupDto>> SearchOutstandingAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        ReturnServiceAuthorization.DemandReadAccess(authenticationService);
        string normalizedKeyword =
            DomainValidator.OptionalMaximumLength(
                keyword,
                150,
                "Từ khóa tìm kiếm")
            ?? string.Empty;
        return returnRepository.SearchOutstandingAsync(
            normalizedKeyword,
            cancellationToken);
    }

    public Task<OperationResult> ReturnBookAsync(
        ReturnBookRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ReturnMultipleBooksAsync(
            new ReturnMultipleBooksRequest(
                [request],
                DateOnly.FromDateTime(DateTime.Today)),
            cancellationToken);
    }

    public async Task<OperationResult> ReturnMultipleBooksAsync(
        ReturnMultipleBooksRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OperationResult? accessFailure =
            ReturnServiceAuthorization.GetWriteFailure(authenticationService);
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
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            ValidatedReturnRequest input =
                ReturnValidator.Validate(request, today);
            int[] detailIds = input.Items
                .Select(item => item.BorrowSlipDetailId)
                .ToArray();
            await using IReturnTransaction transaction =
                await returnRepository.BeginTransactionAsync(cancellationToken);
            ReturnTransactionSnapshot snapshot =
                await transaction.GetSnapshotAsync(
                    detailIds,
                    cancellationToken);
            ReturnFinePolicy policy = ParseFinePolicy(snapshot.Settings);
            IReadOnlyCollection<ReturnPersistenceItem> persistenceItems =
                CreatePersistenceItems(
                    input,
                    snapshot,
                    policy,
                    currentUser.EmployeeId);
            var command = new ReturnPersistenceCommand(
                persistenceItems,
                input.ReturnDate);
            var activityLog = new ActivityLog
            {
                EmployeeId = currentUser.EmployeeId,
                Action = "BooksReturned",
                EntityName = nameof(ReturnRecord),
                Description =
                    $"Đã xử lý trả {persistenceItems.Count} bản sách."
            };
            await transaction.PersistAsync(
                command,
                activityLog,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation(
                "Nhân viên {EmployeeId} đã xử lý trả {BookCount} bản sách.",
                currentUser.EmployeeId,
                persistenceItems.Count);
            return OperationResult.Success();
        }
        catch (DomainValidationException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (ReturnConflictException exception)
        {
            return OperationResult.Failure(exception.Message);
        }
        catch (SqliteException exception)
            when (exception.SqliteErrorCode is 5 or 6)
        {
            logger.LogWarning(
                exception,
                "Database đang bận khi xử lý trả sách.");
            return OperationResult.Failure(
                "Dữ liệu đang được xử lý bởi thao tác khác. Vui lòng thử lại.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Không thể lưu giao dịch trả sách.");
            return OperationResult.Failure(
                "Không thể trả sách. Dữ liệu có thể vừa thay đổi.");
        }
    }

    public int CalculateOverdueDays(
        DateOnly expectedReturnDate,
        DateOnly actualReturnDate)
    {
        return Math.Max(
            0,
            actualReturnDate.DayNumber - expectedReturnDate.DayNumber);
    }

    public async Task<ReturnPreviewDto> CalculateFineAsync(
        int borrowSlipDetailId,
        PhysicalCondition returnedCondition,
        DateOnly returnDate,
        CancellationToken cancellationToken = default)
    {
        ReturnServiceAuthorization.DemandReadAccess(authenticationService);
        var request = new ReturnMultipleBooksRequest(
            [new ReturnBookRequest(borrowSlipDetailId, returnedCondition)],
            returnDate);
        ValidatedReturnRequest input = ReturnValidator.Validate(
            request,
            DateOnly.FromDateTime(DateTime.Today));
        ReturnTransactionSnapshot snapshot =
            await returnRepository.GetSnapshotAsync(
                [borrowSlipDetailId],
                cancellationToken);
        ReturnFinePolicy policy = ParseFinePolicy(snapshot.Settings);
        ReturnDetailSnapshot detail = GetReturnableDetail(
            snapshot,
            borrowSlipDetailId,
            returnDate);
        return CreatePreview(
            detail,
            input.Items.Single().ReturnedCondition,
            returnDate,
            policy);
    }

    public async Task<OperationResult> UpdateBorrowSlipStatusAsync(
        int borrowSlipId,
        CancellationToken cancellationToken = default)
    {
        OperationResult? accessFailure =
            ReturnServiceAuthorization.GetWriteFailure(authenticationService);
        if (accessFailure is not null)
        {
            return accessFailure;
        }

        if (borrowSlipId <= 0)
        {
            return OperationResult.Failure("Phiếu mượn không hợp lệ.");
        }

        bool updated = await returnRepository.UpdateBorrowSlipStatusAsync(
            borrowSlipId,
            DateOnly.FromDateTime(DateTime.Today),
            cancellationToken);
        return updated
            ? OperationResult.Success()
            : OperationResult.Failure("Phiếu mượn không tồn tại.");
    }

    private IReadOnlyCollection<ReturnPersistenceItem>
        CreatePersistenceItems(
            ValidatedReturnRequest input,
            ReturnTransactionSnapshot snapshot,
            ReturnFinePolicy policy,
            int employeeId)
    {
        if (snapshot.Details.Count != input.Items.Count)
        {
            throw new DomainValidationException(
                "Một hoặc nhiều sách mượn không tồn tại.");
        }

        var result = new List<ReturnPersistenceItem>(input.Items.Count);
        foreach (ValidatedReturnItem item in input.Items)
        {
            ReturnDetailSnapshot detail = GetReturnableDetail(
                snapshot,
                item.BorrowSlipDetailId,
                input.ReturnDate);
            ReturnPreviewDto preview = CreatePreview(
                detail,
                item.ReturnedCondition,
                input.ReturnDate,
                policy);
            IReadOnlyCollection<Fine> fines = CreateFines(
                detail,
                preview,
                employeeId,
                input.ReturnDate);
            result.Add(new ReturnPersistenceItem(
                item.BorrowSlipDetailId,
                item.ReturnedCondition,
                GetDetailStatus(item.ReturnedCondition),
                GetBookCopyStatus(item.ReturnedCondition),
                item.Notes,
                new ReturnRecord
                {
                    BorrowSlipDetailId = item.BorrowSlipDetailId,
                    EmployeeId = employeeId,
                    ReturnDate = input.ReturnDate,
                    ReturnedCondition = item.ReturnedCondition,
                    OverdueDays = preview.OverdueDays,
                    Notes = item.Notes
                },
                fines));
        }

        return result;
    }

    private ReturnPreviewDto CreatePreview(
        ReturnDetailSnapshot detail,
        PhysicalCondition returnedCondition,
        DateOnly returnDate,
        ReturnFinePolicy policy)
    {
        int overdueDays = CalculateOverdueDays(
            detail.ExpectedReturnDate,
            returnDate);
        decimal totalOverdueFine = RoundMoney(
            overdueDays * policy.OverdueFinePerDay);
        decimal overdueFine = CalculateAdditionalFine(
            totalOverdueFine,
            detail.ExistingOverdueFineAmount);
        decimal totalConditionFine = returnedCondition switch
        {
            PhysicalCondition.Damaged => RoundMoney(
                detail.BookPrice * policy.DamagedBookFineMultiplier),
            PhysicalCondition.Lost => RoundMoney(
                detail.BookPrice * policy.LostBookFineMultiplier),
            _ => 0m
        };
        decimal existingConditionFine = returnedCondition switch
        {
            PhysicalCondition.Damaged =>
                detail.ExistingDamagedFineAmount,
            PhysicalCondition.Lost => detail.ExistingLostFineAmount,
            _ => 0m
        };
        decimal conditionFine = CalculateAdditionalFine(
            totalConditionFine,
            existingConditionFine);
        return new ReturnPreviewDto(
            detail.BorrowSlipDetailId,
            overdueDays,
            returnedCondition,
            overdueFine,
            conditionFine);
    }

    private static ReturnDetailSnapshot GetReturnableDetail(
        ReturnTransactionSnapshot snapshot,
        int borrowSlipDetailId,
        DateOnly returnDate)
    {
        ReturnDetailSnapshot? detail = snapshot.Details.SingleOrDefault(
            item => item.BorrowSlipDetailId == borrowSlipDetailId);
        if (detail is null)
        {
            throw new DomainValidationException(
                "Sách mượn không tồn tại.");
        }

        if (detail.DetailStatus is
            not BorrowSlipDetailStatus.Borrowing
            and not BorrowSlipDetailStatus.Overdue
            || detail.HasReturnRecord)
        {
            throw new DomainValidationException(
                $"Bản sách {detail.CopyCode} đã được trả trước đó.");
        }

        if (detail.BorrowSlipStatus is
            not BorrowSlipStatus.Active
            and not BorrowSlipStatus.PartiallyReturned
            and not BorrowSlipStatus.Overdue)
        {
            throw new DomainValidationException(
                "Phiếu mượn không còn hiệu lực để trả sách.");
        }

        if (detail.BookCopyStatus != BookCopyStatus.Borrowed)
        {
            throw new DomainValidationException(
                $"Trạng thái bản sách {detail.CopyCode} không hợp lệ để trả.");
        }

        if (returnDate < detail.BorrowDate)
        {
            throw new DomainValidationException(
                "Ngày trả không được nhỏ hơn ngày mượn.");
        }

        return detail;
    }

    private static IReadOnlyCollection<Fine> CreateFines(
        ReturnDetailSnapshot detail,
        ReturnPreviewDto preview,
        int employeeId,
        DateOnly returnDate)
    {
        var fines = new List<Fine>(2);
        if (preview.OverdueFineAmount > 0)
        {
            fines.Add(CreateFine(
                detail,
                FineType.Overdue,
                preview.OverdueFineAmount,
                $"Quá hạn {preview.OverdueDays} ngày.",
                employeeId,
                returnDate));
        }

        if (preview.ConditionFineAmount > 0)
        {
            FineType fineType = preview.ReturnedCondition
                == PhysicalCondition.Lost
                ? FineType.Lost
                : FineType.Damaged;
            string reason = fineType == FineType.Lost
                ? $"Bản sách {detail.CopyCode} bị mất."
                : $"Bản sách {detail.CopyCode} bị hư hỏng.";
            fines.Add(CreateFine(
                detail,
                fineType,
                preview.ConditionFineAmount,
                reason,
                employeeId,
                returnDate));
        }

        return fines;
    }

    private static Fine CreateFine(
        ReturnDetailSnapshot detail,
        FineType fineType,
        decimal amount,
        string reason,
        int employeeId,
        DateOnly returnDate)
    {
        return new Fine
        {
            FineCode = CreateFineCode(returnDate),
            ReaderId = detail.ReaderId,
            BorrowSlipDetailId = detail.BorrowSlipDetailId,
            FineType = fineType,
            Amount = amount,
            PaidAmount = 0m,
            Status = FineStatus.Unpaid,
            Reason = reason,
            CreatedByEmployeeId = employeeId
        };
    }

    private static BorrowSlipDetailStatus GetDetailStatus(
        PhysicalCondition returnedCondition)
    {
        return returnedCondition switch
        {
            PhysicalCondition.Damaged => BorrowSlipDetailStatus.Damaged,
            PhysicalCondition.Lost => BorrowSlipDetailStatus.Lost,
            _ => BorrowSlipDetailStatus.Returned
        };
    }

    private static BookCopyStatus GetBookCopyStatus(
        PhysicalCondition returnedCondition)
    {
        return returnedCondition switch
        {
            PhysicalCondition.Damaged => BookCopyStatus.Damaged,
            PhysicalCondition.Lost => BookCopyStatus.Lost,
            _ => BookCopyStatus.Available
        };
    }

    private static ReturnFinePolicy ParseFinePolicy(
        IReadOnlyDictionary<string, string> settings)
    {
        return new ReturnFinePolicy(
            ParseNonNegativeDecimal(
                settings,
                SystemSettingKeys.OverdueFinePerDay),
            ParseNonNegativeDecimal(
                settings,
                SystemSettingKeys.LostBookFineMultiplier),
            ParseNonNegativeDecimal(
                settings,
                SystemSettingKeys.DamagedBookFineMultiplier));
    }

    private static decimal ParseNonNegativeDecimal(
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

    private static decimal RoundMoney(decimal amount)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateAdditionalFine(
        decimal totalRequiredFine,
        decimal existingFineAmount)
    {
        return Math.Max(
            0m,
            RoundMoney(totalRequiredFine - existingFineAmount));
    }

    private static string CreateFineCode(DateOnly date)
    {
        return $"PP{date:yyyyMMdd}-{Guid.NewGuid():N}"[..27]
            .ToUpperInvariant();
    }

    private sealed record ReturnFinePolicy(
        decimal OverdueFinePerDay,
        decimal LostBookFineMultiplier,
        decimal DamagedBookFineMultiplier);
}
