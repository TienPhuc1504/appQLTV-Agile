using FluentAssertions;
using LibraryManagement.Core.DTOs;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class BorrowValidatorTests
{
    [Fact]
    public void Validate_WithValidRequest_ShouldNormalizeNotes()
    {
        ValidatedBorrowRequest result = BorrowValidator.Validate(
            new BorrowCreateRequest(1, [2, 4], "  Ghi chú  "));

        result.ReaderId.Should().Be(1);
        result.BookCopyIds.Should().Equal(2, 4);
        result.Notes.Should().Be("Ghi chú");
    }

    [Fact]
    public void Validate_WithoutReader_ShouldThrow()
    {
        Action action = () => BorrowValidator.Validate(
            new BorrowCreateRequest(0, [2]));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*chọn độc giả*");
    }

    [Fact]
    public void Validate_WithoutCopies_ShouldThrow()
    {
        Action action = () => BorrowValidator.Validate(
            new BorrowCreateRequest(1, []));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*ít nhất một bản sách*");
    }

    [Fact]
    public void Validate_WithDuplicateCopy_ShouldThrow()
    {
        Action action = () => BorrowValidator.Validate(
            new BorrowCreateRequest(1, [2, 2]));

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("*trùng*");
    }
}
