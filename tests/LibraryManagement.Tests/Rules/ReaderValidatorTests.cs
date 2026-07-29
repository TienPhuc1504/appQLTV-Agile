using FluentAssertions;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class ReaderValidatorTests
{
    [Fact]
    public void ReaderCode_WithValidValue_ShouldNormalize()
    {
        ReaderValidator.ReaderCode(" dg-001 ")
            .Should()
            .Be("DG-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("DG 001")]
    [InlineData("DG@001")]
    public void ReaderCode_WithInvalidValue_ShouldThrow(string value)
    {
        Action action = () => ReaderValidator.ReaderCode(value);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void CardDates_WhenExpirationIsNotAfterRegistration_ShouldThrow()
    {
        var date = new DateOnly(2026, 7, 29);

        Action action = () => ReaderValidator.CardDates(date, date, date);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Ngày hết hạn phải lớn hơn ngày đăng ký.");
    }

    [Fact]
    public void DateOfBirth_InFuture_ShouldThrow()
    {
        var today = new DateOnly(2026, 7, 29);

        Action action = () =>
            ReaderValidator.DateOfBirth(today.AddDays(1), today);

        action.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void DateOfBirth_AfterRegistrationDate_ShouldThrow()
    {
        var today = new DateOnly(2026, 7, 29);
        DateOnly registeredAt = today.AddYears(-2);

        Action action = () => ReaderValidator.DateOfBirth(
            registeredAt.AddDays(1),
            today,
            registeredAt);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Ngày sinh không được lớn hơn ngày đăng ký.");
    }

    [Fact]
    public void Enums_WithUnknownReaderType_ShouldThrow()
    {
        Action action = () =>
            ReaderValidator.Enums(Gender.Other, (ReaderType)999);

        action.Should().Throw<DomainValidationException>();
    }
}
