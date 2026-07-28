using LibraryManagement.Core.Interfaces;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int MinimumWorkFactor = 4;
    private const int MaximumWorkFactor = 31;
    private readonly int _workFactor;

    public BcryptPasswordHasher(int workFactor)
    {
        if (workFactor is < MinimumWorkFactor or > MaximumWorkFactor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workFactor),
                $"BCrypt work factor phải nằm trong khoảng {MinimumWorkFactor} đến {MaximumWorkFactor}.");
        }

        _workFactor = workFactor;
    }

    public string Hash(string password)
    {
        string validatedPassword = CredentialValidator.ValidateNewPassword(password);
        return BCrypt.Net.BCrypt.HashPassword(validatedPassword, _workFactor);
    }

    public bool Verify(string password, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
