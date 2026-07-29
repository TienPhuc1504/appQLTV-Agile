using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Enums;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class ReturnValidatorTests
{
    private static readonly DateOnly Today = new(2026, 7, 29);

    [Fact]
    public void Validate_WithValidRequest_ShouldNormalizeNotes()
    {
        ValidatedReturnRequest result = ReturnValidator.Validate(
            new ReturnMultipleBooksRequest(
                [new ReturnBookRequest(4, PhysicalCondition.Good, "  Tốt  ")],
                Today),
            Today);

        result.Items.Should().ContainSingle();
        result.Items.Single().Notes.Should().Be("Tốt");
        result.ReturnDate.Should().Be(Today);
    }

    [Fact]
    public void Validate_WithoutItems_ShouldThrow()
    {
        Action action = () => ReturnValidator.Validate(
            new ReturnMultipleBooksRequest([], Today),
            Today);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*ít nhất một bản sách*");
    }

    [Fact]
    public void Validate_WithDuplicateDetail_ShouldThrow()
    {
        Action action = () => ReturnValidator.Validate(
            new ReturnMultipleBooksRequest(
                [
                    new ReturnBookRequest(4, PhysicalCondition.Good),
                    new ReturnBookRequest(4, PhysicalCondition.Damaged)
                ],
                Today),
            Today);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*trùng*");
    }

    [Fact]
    public void Validate_WithFutureReturnDate_ShouldThrow()
    {
        Action action = () => ReturnValidator.Validate(
            new ReturnMultipleBooksRequest(
                [new ReturnBookRequest(4, PhysicalCondition.Good)],
                Today.AddDays(1)),
            Today);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*ngày hiện tại*");
    }
}
