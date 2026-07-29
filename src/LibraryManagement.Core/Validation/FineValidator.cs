using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Validation;

public static class FineValidator
{
    public static ValidatedFineCreateRequest ValidateCreate(
        FineCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ReaderId <= 0 || request.BorrowSlipDetailId <= 0)
        {
            throw new DomainValidationException(
                "Độc giả hoặc chi tiết mượn sách không hợp lệ.");
        }

        if (!Enum.IsDefined(request.FineType))
        {
            throw new DomainValidationException(
                "Loại tiền phạt không hợp lệ.");
        }

        if (request.Amount <= 0)
        {
            throw new DomainValidationException(
                "Số tiền phạt phải lớn hơn 0.");
        }

        string reason = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Reason, "lý do phạt"),
            1000,
            "Lý do phạt");
        decimal normalizedAmount = decimal.Round(
            request.Amount,
            2,
            MidpointRounding.AwayFromZero);
        if (normalizedAmount <= 0)
        {
            throw new DomainValidationException(
                "Số tiền phạt phải lớn hơn 0.");
        }

        return new ValidatedFineCreateRequest(
            request.ReaderId,
            request.BorrowSlipDetailId,
            request.FineType,
            normalizedAmount,
            reason);
    }

    public static ValidatedFinePaymentRequest ValidatePayment(
        FinePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.FineId <= 0)
        {
            throw new DomainValidationException(
                "Khoản phạt không hợp lệ.");
        }

        if (request.Amount <= 0)
        {
            throw new DomainValidationException(
                "Số tiền thanh toán phải lớn hơn 0.");
        }

        if (!Enum.IsDefined(request.PaymentMethod))
        {
            throw new DomainValidationException(
                "Phương thức thanh toán không hợp lệ.");
        }

        decimal normalizedAmount = decimal.Round(
            request.Amount,
            2,
            MidpointRounding.AwayFromZero);
        if (normalizedAmount <= 0)
        {
            throw new DomainValidationException(
                "Số tiền thanh toán phải lớn hơn 0.");
        }

        return new ValidatedFinePaymentRequest(
            request.FineId,
            normalizedAmount,
            request.PaymentMethod,
            DomainValidator.OptionalMaximumLength(
                request.Notes,
                1000,
                "Ghi chú"));
    }

    public static ValidatedFineWaiveRequest ValidateWaiver(
        FineWaiveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.FineId <= 0)
        {
            throw new DomainValidationException(
                "Khoản phạt không hợp lệ.");
        }

        string reason = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Reason, "lý do miễn phạt"),
            500,
            "Lý do miễn phạt");
        return new ValidatedFineWaiveRequest(request.FineId, reason);
    }
}

public sealed record ValidatedFineCreateRequest(
    int ReaderId,
    int BorrowSlipDetailId,
    FineType FineType,
    decimal Amount,
    string Reason);

public sealed record ValidatedFinePaymentRequest(
    int FineId,
    decimal Amount,
    PaymentMethod PaymentMethod,
    string? Notes);

public sealed record ValidatedFineWaiveRequest(
    int FineId,
    string Reason);
