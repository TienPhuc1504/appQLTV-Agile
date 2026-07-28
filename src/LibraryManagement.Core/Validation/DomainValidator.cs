using System.Net.Mail;
using System.Text.RegularExpressions;

namespace LibraryManagement.Core.Validation;

public static partial class DomainValidator
{
    public static string Required(string? value, string fieldDisplayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"Vui lòng nhập {fieldDisplayName}.");
        }

        return value.Trim();
    }

    public static string? OptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();

        if (!MailAddress.TryCreate(normalizedValue, out MailAddress? address)
            || !string.Equals(address.Address, normalizedValue, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainValidationException("Email không đúng định dạng.");
        }

        return normalizedValue;
    }

    public static string? OptionalPhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalizedValue = value.Trim();
        if (!PhoneNumberRegex().IsMatch(normalizedValue))
        {
            throw new DomainValidationException("Số điện thoại không đúng định dạng.");
        }

        return normalizedValue;
    }

    public static decimal NonNegative(decimal value, string fieldDisplayName)
    {
        if (value < 0)
        {
            throw new DomainValidationException($"{fieldDisplayName} không được nhỏ hơn 0.");
        }

        return value;
    }

    public static void EnsureDateAfter(
        DateOnly laterDate,
        DateOnly earlierDate,
        string errorMessage)
    {
        if (laterDate <= earlierDate)
        {
            throw new DomainValidationException(errorMessage);
        }
    }

    [GeneratedRegex(@"^\+?[0-9][0-9 .-]{7,19}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneNumberRegex();
}
