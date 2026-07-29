using System.Globalization;
using LibraryManagement.Core.Constants;
using LibraryManagement.Core.DTOs;

namespace LibraryManagement.Core.Validation;

public static class SystemSettingValidator
{
    public static SystemSettingUpdateRequest Validate(
        SystemSettingUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        string key = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Key, "khóa cài đặt"),
            100,
            "Khóa cài đặt");
        string value = DomainValidator.MaximumLength(
            DomainValidator.Required(request.Value, "giá trị cài đặt"),
            500,
            "Giá trị cài đặt");

        if (IntegerRules.TryGetValue(
                key,
                out SettingRange<int>? intRule)
            && intRule is not null)
        {
            if (!int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int intValue)
                || intValue < intRule.Minimum
                || intValue > intRule.Maximum)
            {
                throw new DomainValidationException(
                    $"{intRule.DisplayName} phải từ {intRule.Minimum} đến {intRule.Maximum}.");
            }

            value = intValue.ToString(CultureInfo.InvariantCulture);
        }
        else if (DecimalRules.TryGetValue(
                     key,
                     out SettingRange<decimal>? decimalRule)
                 && decimalRule is not null)
        {
            string normalizedDecimal = NormalizeDecimalInput(value);
            if (!decimal.TryParse(
                    normalizedDecimal,
                    NumberStyles.AllowLeadingSign
                        | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out decimal decimalValue)
                || decimalValue < decimalRule.Minimum
                || decimalValue > decimalRule.Maximum)
            {
                throw new DomainValidationException(
                    $"{decimalRule.DisplayName} phải từ {decimalRule.Minimum} đến {decimalRule.Maximum}.");
            }

            value = decimalValue.ToString(
                "0.############################",
                CultureInfo.InvariantCulture);
        }
        else
        {
            throw new DomainValidationException(
                "Cài đặt hệ thống không được hỗ trợ.");
        }

        return new SystemSettingUpdateRequest(key, value);
    }

    private static readonly IReadOnlyDictionary<string, SettingRange<int>>
        IntegerRules = new Dictionary<string, SettingRange<int>>
        {
            [SystemSettingKeys.MaximumBorrowedBooks] =
                new(1, 100, "Số sách mượn tối đa"),
            [SystemSettingKeys.DefaultBorrowDays] =
                new(1, 365, "Số ngày mượn"),
            [SystemSettingKeys.MaximumRenewalCount] =
                new(0, 100, "Số lần gia hạn"),
            [SystemSettingKeys.RenewalDays] =
                new(1, 365, "Số ngày gia hạn"),
            [SystemSettingKeys.ReaderCardValidityMonths] =
                new(1, 120, "Thời hạn thẻ độc giả")
        };

    private static readonly IReadOnlyDictionary<string, SettingRange<decimal>>
        DecimalRules = new Dictionary<string, SettingRange<decimal>>
        {
            [SystemSettingKeys.OverdueFinePerDay] =
                new(0m, 1_000_000_000m, "Mức phạt quá hạn"),
            [SystemSettingKeys.LostBookFineMultiplier] =
                new(0m, 100m, "Hệ số phạt mất sách"),
            [SystemSettingKeys.DamagedBookFineMultiplier] =
                new(0m, 100m, "Hệ số phạt hư hỏng"),
            [SystemSettingKeys.MaximumOutstandingFineAmount] =
                new(0m, 1_000_000_000_000m, "Mức tiền phạt chưa thu tối đa")
        };

    private static string NormalizeDecimalInput(string value)
    {
        bool hasDot = value.Contains('.');
        bool hasComma = value.Contains(',');
        if (hasDot && hasComma)
        {
            return string.Empty;
        }

        return hasComma
            ? value.Replace(',', '.')
            : value;
    }

    private sealed record SettingRange<T>(
        T Minimum,
        T Maximum,
        string DisplayName);
}
