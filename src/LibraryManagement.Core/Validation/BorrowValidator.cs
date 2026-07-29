using LibraryManagement.Core.DTOs;

namespace LibraryManagement.Core.Validation;

public static class BorrowValidator
{
    public static ValidatedBorrowRequest Validate(BorrowCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ReaderId <= 0)
        {
            throw new DomainValidationException(
                "Vui lòng chọn độc giả cần mượn sách.");
        }

        ArgumentNullException.ThrowIfNull(request.BookCopyIds);
        int[] bookCopyIds = request.BookCopyIds.ToArray();
        if (bookCopyIds.Length == 0)
        {
            throw new DomainValidationException(
                "Vui lòng thêm ít nhất một bản sách.");
        }

        if (bookCopyIds.Any(id => id <= 0))
        {
            throw new DomainValidationException(
                "Danh sách bản sách không hợp lệ.");
        }

        if (bookCopyIds.Distinct().Count() != bookCopyIds.Length)
        {
            throw new DomainValidationException(
                "Không được thêm trùng một bản sách.");
        }

        string? notes = DomainValidator.OptionalMaximumLength(
            request.Notes,
            1000,
            "Ghi chú");
        return new ValidatedBorrowRequest(
            request.ReaderId,
            bookCopyIds,
            notes);
    }
}

public sealed record ValidatedBorrowRequest(
    int ReaderId,
    IReadOnlyCollection<int> BookCopyIds,
    string? Notes);
