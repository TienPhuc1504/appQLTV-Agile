using FluentAssertions;
using LibraryManagement.Core.Validation;

namespace LibraryManagement.Tests.Rules;

public sealed class BookValidatorTests
{
    [Theory]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("0-306-40615-2", "0306406152")]
    public void Isbn_WithValidValue_ShouldNormalize(
        string value,
        string expected)
    {
        BookValidator.Isbn(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("9780306406158")]
    [InlineData("0306406153")]
    [InlineData("123")]
    public void Isbn_WithInvalidValue_ShouldThrow(string value)
    {
        Action action = () => BookValidator.Isbn(value);

        action.Should()
            .Throw<DomainValidationException>()
            .WithMessage("ISBN không hợp lệ.");
    }

    [Fact]
    public void PublicationYear_InFuture_ShouldThrow()
    {
        Action action = () => BookValidator.PublicationYear(
            DateTime.Today.Year + 1,
            DateTime.Today.Year);

        action.Should().Throw<DomainValidationException>();
    }
}
