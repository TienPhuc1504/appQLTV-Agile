using System.Text;
using System.Text.RegularExpressions;

namespace LibraryManagement.Core.Validation;

public static partial class CredentialValidator
{
    public const int MaximumUsernameLength = 50;
    public const int MinimumPasswordLength = 8;
    public const int MaximumBcryptPasswordBytes = 72;

    public static string NormalizeUsername(string? username)
    {
        string normalizedUsername = DomainValidator.Required(
            username,
            "tên đăng nhập");

        if (normalizedUsername.Length > MaximumUsernameLength)
        {
            throw new DomainValidationException(
                $"Tên đăng nhập không được vượt quá {MaximumUsernameLength} ký tự.");
        }

        if (!UsernameRegex().IsMatch(normalizedUsername))
        {
            throw new DomainValidationException(
                "Tên đăng nhập chỉ được chứa chữ cái, chữ số, dấu chấm, gạch dưới hoặc gạch ngang.");
        }

        return normalizedUsername;
    }

    public static string ValidateLoginPassword(string? password)
    {
        string validatedPassword = RequiredPassword(password, "mật khẩu");
        EnsureBcryptByteLimit(validatedPassword);
        return validatedPassword;
    }

    public static string ValidateNewPassword(string? password)
    {
        string validatedPassword = RequiredPassword(password, "mật khẩu mới");

        if (validatedPassword.Length < MinimumPasswordLength)
        {
            throw new DomainValidationException(
                $"Mật khẩu phải có ít nhất {MinimumPasswordLength} ký tự.");
        }

        EnsureBcryptByteLimit(validatedPassword);

        if (validatedPassword.Any(char.IsWhiteSpace))
        {
            throw new DomainValidationException(
                "Mật khẩu không được chứa khoảng trắng.");
        }

        if (!validatedPassword.Any(char.IsUpper)
            || !validatedPassword.Any(char.IsLower)
            || !validatedPassword.Any(char.IsDigit)
            || !validatedPassword.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new DomainValidationException(
                "Mật khẩu phải có chữ hoa, chữ thường, chữ số và ký tự đặc biệt.");
        }

        return validatedPassword;
    }

    private static string RequiredPassword(
        string? password,
        string fieldDisplayName)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new DomainValidationException(
                $"Vui lòng nhập {fieldDisplayName}.");
        }

        return password;
    }

    private static void EnsureBcryptByteLimit(string password)
    {
        if (Encoding.UTF8.GetByteCount(password) > MaximumBcryptPasswordBytes)
        {
            throw new DomainValidationException(
                $"Mật khẩu không được vượt quá {MaximumBcryptPasswordBytes} byte UTF-8.");
        }
    }

    [GeneratedRegex(
        @"^[\p{L}\p{N}._-]+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex UsernameRegex();
}
