using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Validation;

public static class ReturnValidator
{
    public static ValidatedReturnRequest Validate(
        ReturnMultipleBooksRequest request,
        DateOnly currentDate)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Items);

        ReturnBookRequest[] items = request.Items.ToArray();
        if (items.Length == 0)
        {
            throw new DomainValidationException(
                "Vui lòng chọn ít nhất một bản sách cần trả.");
        }

        if (request.ReturnDate == default)
        {
            throw new DomainValidationException(
                "Ngày trả sách không hợp lệ.");
        }

        if (request.ReturnDate > currentDate)
        {
            throw new DomainValidationException(
                "Ngày trả sách không được lớn hơn ngày hiện tại.");
        }

        if (items.Any(item => item.BorrowSlipDetailId <= 0))
        {
            throw new DomainValidationException(
                "Danh sách sách trả không hợp lệ.");
        }

        if (items.Select(item => item.BorrowSlipDetailId).Distinct().Count()
            != items.Length)
        {
            throw new DomainValidationException(
                "Không được trả trùng một bản sách trong cùng giao dịch.");
        }

        ValidatedReturnItem[] validatedItems = items
            .Select(ValidateItem)
            .ToArray();
        return new ValidatedReturnRequest(validatedItems, request.ReturnDate);
    }

    private static ValidatedReturnItem ValidateItem(ReturnBookRequest item)
    {
        if (!Enum.IsDefined(item.ReturnedCondition))
        {
            throw new DomainValidationException(
                "Tình trạng sách khi trả không hợp lệ.");
        }

        string? notes = DomainValidator.OptionalMaximumLength(
            item.Notes,
            1000,
            "Ghi chú");
        return new ValidatedReturnItem(
            item.BorrowSlipDetailId,
            item.ReturnedCondition,
            notes);
    }
}

public sealed record ValidatedReturnRequest(
    IReadOnlyCollection<ValidatedReturnItem> Items,
    DateOnly ReturnDate);

public sealed record ValidatedReturnItem(
    int BorrowSlipDetailId,
    PhysicalCondition ReturnedCondition,
    string? Notes);
