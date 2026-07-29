using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class FineValidatorTests
{
    [Fact]
    public void ValidatePayment_WithValidData_ShouldNormalizeNotes()
    {
        ValidatedFinePaymentRequest result =
            FineValidator.ValidatePayment(
                new FinePaymentRequest(
                    1,
                    25000m,
                    PaymentMethod.Cash,
                    "  Thu tại quầy  "));

        result.Amount.Should().Be(25000m);
        result.Notes.Should().Be("Thu tại quầy");
    }

    [Fact]
    public void ValidatePayment_WithZeroAmount_ShouldThrow()
    {
        Action action = () => FineValidator.ValidatePayment(
            new FinePaymentRequest(1, 0m, PaymentMethod.Cash));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*lớn hơn 0*");
    }

    [Fact]
    public void ValidatePayment_WithAmountRoundedToZero_ShouldThrow()
    {
        Action action = () => FineValidator.ValidatePayment(
            new FinePaymentRequest(
                1,
                0.001m,
                PaymentMethod.Cash));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*lớn hơn 0*");
    }

    [Fact]
    public void ValidateWaiver_WithoutReason_ShouldThrow()
    {
        Action action = () => FineValidator.ValidateWaiver(
            new FineWaiveRequest(1, " "));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*lý do miễn phạt*");
    }

    [Fact]
    public void ValidateCreate_WithInvalidReader_ShouldThrow()
    {
        Action action = () => FineValidator.ValidateCreate(
            new FineCreateRequest(
                0,
                1,
                FineType.Other,
                10000m,
                "Lý do"));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*không hợp lệ*");
    }
}
