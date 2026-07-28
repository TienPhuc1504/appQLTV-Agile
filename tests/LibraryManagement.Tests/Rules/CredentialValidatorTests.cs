using FluentAssertions;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class CredentialValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData("user name")]
    [InlineData("user@name")]
    public void NormalizeUsername_WhenUsernameIsInvalid_ShouldThrow(
        string username)
    {
        Action action = () => CredentialValidator.NormalizeUsername(username);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*tên đăng nhập*");
    }

    [Theory]
    [InlineData("password")]
    [InlineData("Password1")]
    [InlineData("password@1")]
    [InlineData("PASSWORD@1")]
    public void ValidateNewPassword_WhenPasswordIsWeak_ShouldThrow(
        string password)
    {
        Action action = () => CredentialValidator.ValidateNewPassword(password);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void ValidateNewPassword_WhenPasswordIsStrong_ShouldReturnPassword()
    {
        const string password = "Library@123";

        string result = CredentialValidator.ValidateNewPassword(password);

        result.Should().Be(password);
    }

    [Fact]
    public void ValidateLoginPassword_ShouldNotTrimPassword()
    {
        const string password = " Admin@123 ";

        string result = CredentialValidator.ValidateLoginPassword(password);

        result.Should().Be(password);
    }

    [Fact]
    public void ValidateNewPassword_WhenPasswordContainsWhitespace_ShouldThrow()
    {
        Action action = () =>
            CredentialValidator.ValidateNewPassword("Library @123");

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*khoảng trắng*");
    }
}
