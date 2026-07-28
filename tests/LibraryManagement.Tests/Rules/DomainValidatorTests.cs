using FluentAssertions;
using LibraryManagement.Core.Entities;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class DomainValidatorTests
{
    [Fact]
    public void Required_WhenValueIsBlank_ShouldReturnVietnameseValidationError()
    {
        Action action = () => DomainValidator.Required("   ", "tên độc giả");

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Vui lòng nhập tên độc giả.");
    }

    [Fact]
    public void OptionalEmail_WhenEmailIsInvalid_ShouldThrow()
    {
        Action action = () => DomainValidator.OptionalEmail("email-khong-hop-le");

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Email không đúng định dạng.");
    }

    [Theory]
    [InlineData("0901234567")]
    [InlineData("+84 901 234 567")]
    public void OptionalPhoneNumber_WhenPhoneNumberIsValid_ShouldReturnNormalizedValue(
        string phoneNumber)
    {
        string? result = DomainValidator.OptionalPhoneNumber($" {phoneNumber} ");

        result.Should().Be(phoneNumber);
    }

    [Fact]
    public void EnsureDateAfter_WhenExpirationIsNotAfterRegistration_ShouldThrow()
    {
        DateOnly registeredAt = new(2026, 7, 28);

        Action action = () => DomainValidator.EnsureDateAfter(
            registeredAt,
            registeredAt,
            "Ngày hết hạn phải lớn hơn ngày đăng ký.");

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("Ngày hết hạn phải lớn hơn ngày đăng ký.");
    }

    [Fact]
    public void ReaderCard_WhenReaderIsActiveAndNotExpired_ShouldBeValid()
    {
        var reader = new Reader
        {
            Status = ReaderStatus.Active,
            ExpirationDate = new DateOnly(2027, 1, 1)
        };

        bool result = reader.IsCardValid(new DateOnly(2026, 7, 28));

        result.Should().BeTrue();
    }

    [Fact]
    public void FineOutstandingAmount_ShouldNeverBeNegative()
    {
        var fine = new Fine
        {
            Amount = 10000m,
            PaidAmount = 12000m
        };

        fine.OutstandingAmount.Should().Be(0m);
    }
}
