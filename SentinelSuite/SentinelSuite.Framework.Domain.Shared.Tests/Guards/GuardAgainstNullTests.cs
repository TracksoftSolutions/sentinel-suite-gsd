using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;

public class GuardAgainstNullTests
{
    [Fact]
    public void Null_WhenReferenceTypeInputProvided_ReturnsSameInstanceUnchanged()
    {
        var input = "value";

        var result = Guard.Against.Null(input);

        Assert.Same(input, result);
    }

    [Fact]
    public void Null_WhenReferenceTypeInputIsNull_ThrowsArgumentNullExceptionWithCapturedParameterName()
    {
        string? input = null;

        var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.Null(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void Null_WhenNullableValueTypeInputProvided_ReturnsUnwrappedValue()
    {
        int? input = 5;

        var result = Guard.Against.Null(input);

        Assert.Equal(5, result);
    }

    [Fact]
    public void Null_WhenNullableValueTypeInputIsNull_ThrowsArgumentNullException()
    {
        int? input = null;

        var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.Null(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void NullOrWhiteSpace_WhenValidStringProvided_ReturnsSameStringUnchanged()
    {
        var input = "hello";

        var result = Guard.Against.NullOrWhiteSpace(input);

        Assert.Same(input, result);
    }

    [Fact]
    public void NullOrWhiteSpace_WhenWhitespaceOnlyInputProvided_ThrowsArgumentExceptionWithoutLeakingValue()
    {
        var input = "   ";

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.NullOrWhiteSpace(input));

        Assert.Equal(nameof(input), ex.ParamName);
        Assert.DoesNotContain(input, ex.Message);
    }

    [Fact]
    public void NullOrWhiteSpace_WhenInputIsNull_ThrowsArgumentNullException()
    {
        string? input = null;

        var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.NullOrWhiteSpace(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }
}
