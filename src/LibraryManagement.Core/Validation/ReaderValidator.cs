using System.Text.RegularExpressions;
using LibraryManagement.Core.Enums;

namespace LibraryManagement.Core.Validation;

public static partial class ReaderValidator
{
    public static string ReaderCode(string? value)
    {
        string code = DomainValidator.MaximumLength(
            DomainValidator.Required(value, "mã độc giả"),
            20,
            "Mã độc giả");
        if (!ReaderCodeRegex().IsMatch(code))
        {
            throw new DomainValidationException(
                "Mã độc giả chỉ được chứa chữ cái, chữ số, dấu chấm, gạch ngang và gạch dưới.");
        }

        return code.ToUpperInvariant();
    }

    public static string FullName(string? value)
    {
        return DomainValidator.MaximumLength(
            DomainValidator.Required(value, "tên độc giả"),
            150,
            "Tên độc giả");
    }

    public static DateOnly? DateOfBirth(
        DateOnly? value,
        DateOnly today,
        DateOnly? registeredAt = null)
    {
        DateOnly? dateOfBirth =
            DomainValidator.NotInFuture(value, "Ngày sinh", today);
        if (dateOfBirth.HasValue
            && registeredAt.HasValue
            && dateOfBirth.Value > registeredAt.Value)
        {
            throw new DomainValidationException(
                "Ngày sinh không được lớn hơn ngày đăng ký.");
        }

        return dateOfBirth;
    }

    public static void CardDates(
        DateOnly registeredAt,
        DateOnly expirationDate,
        DateOnly today)
    {
        if (registeredAt > today)
        {
            throw new DomainValidationException(
                "Ngày đăng ký không được lớn hơn ngày hiện tại.");
        }

        DomainValidator.EnsureDateAfter(
            expirationDate,
            registeredAt,
            "Ngày hết hạn phải lớn hơn ngày đăng ký.");
    }

    public static void Enums(Gender gender, ReaderType readerType)
    {
        if (!Enum.IsDefined(gender))
        {
            throw new DomainValidationException("Giới tính không hợp lệ.");
        }

        if (!Enum.IsDefined(readerType))
        {
            throw new DomainValidationException("Loại độc giả không hợp lệ.");
        }
    }

    [GeneratedRegex(
        @"^[\p{L}\p{N}._-]+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ReaderCodeRegex();
}
