using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;

public class GuardAgainstStringTests
{
    [Fact]
    public void StringTooShort_WhenLengthMeetsMinimum_ReturnsSameStringUnchanged()
    {
        var input = "hello";

        var result = Guard.Against.StringTooShort(input, 3);

        Assert.Same(input, result);
    }

    [Fact]
    public void StringTooShort_WhenLengthBelowMinimum_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = "hi";

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.StringTooShort(input, 3));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void StringTooLong_WhenLengthWithinMaximum_ReturnsSameStringUnchanged()
    {
        var input = "hello";

        var result = Guard.Against.StringTooLong(input, 10);

        Assert.Same(input, result);
    }

    [Fact]
    public void StringTooLong_WhenLengthExceedsMaximum_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = "hello world";

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.StringTooLong(input, 5));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void InvalidFormat_WhenInputMatchesPattern_ReturnsSameStringUnchanged()
    {
        var input = "abc123";

        var result = Guard.Against.InvalidFormat(input, "^[a-z]+[0-9]+$");

        Assert.Same(input, result);
    }

    [Fact]
    public void InvalidFormat_WhenInputDoesNotMatchPattern_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = "123abc";

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.InvalidFormat(input, "^[a-z]+[0-9]+$"));

        Assert.Equal(nameof(input), ex.ParamName);
    }
}
