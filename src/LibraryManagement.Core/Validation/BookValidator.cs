using System.Text.RegularExpressions;

namespace LibraryManagement.Core.Validation;

public static partial class BookValidator
{
    public static string BookCode(string? value)
    {
        string normalized = DomainValidator.MaximumLength(
            DomainValidator.Required(value, "mã sách"),
            20,
            "Mã sách");
        if (!CodeRegex().IsMatch(normalized))
        {
            throw new DomainValidationException(
                "Mã sách chỉ được chứa chữ cái, chữ số, dấu chấm, gạch ngang và gạch dưới.");
        }

        return normalized.ToUpperInvariant();
    }

    public static string? Isbn(string? value)
    {
        string? normalized = DomainValidator.Optional(value);
        if (normalized is null)
        {
            return null;
        }

        string compact = normalized
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
        if (!IsValidIsbn10(compact) && !IsValidIsbn13(compact))
        {
            throw new DomainValidationException("ISBN không hợp lệ.");
        }

        return compact;
    }

    public static int PublicationYear(int value, int currentYear)
    {
        if (value < 1000 || value > currentYear)
        {
            throw new DomainValidationException(
                $"Năm xuất bản phải từ 1000 đến {currentYear}.");
        }

        return value;
    }

    public static int Positive(int value, string fieldDisplayName)
    {
        if (value <= 0)
        {
            throw new DomainValidationException(
                $"{fieldDisplayName} phải lớn hơn 0.");
        }

        return value;
    }

    private static bool IsValidIsbn10(string value)
    {
        if (value.Length != 10
            || !value[..9].All(char.IsDigit)
            || (!char.IsDigit(value[9]) && value[9] != 'X'))
        {
            return false;
        }

        int sum = 0;
        for (int index = 0; index < 10; index++)
        {
            int digit = value[index] == 'X' ? 10 : value[index] - '0';
            sum += digit * (10 - index);
        }

        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string value)
    {
        if (value.Length != 13 || !value.All(char.IsDigit))
        {
            return false;
        }

        int sum = 0;
        for (int index = 0; index < 12; index++)
        {
            int digit = value[index] - '0';
            sum += digit * (index % 2 == 0 ? 1 : 3);
        }

        int checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == value[12] - '0';
    }

    [GeneratedRegex(@"^[\p{L}\p{N}._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex CodeRegex();
}
