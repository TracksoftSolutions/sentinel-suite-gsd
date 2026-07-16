using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;

public class GuardAgainstRangeTests
{
    [Fact]
    public void OutOfRange_WhenInputWithinRange_ReturnsSameValueUnchanged()
    {
        var input = 5;

        var result = Guard.Against.OutOfRange(input, 1, 10);

        Assert.Equal(input, result);
    }

    [Fact]
    public void OutOfRange_WhenInputEqualsLowerBoundary_ReturnsSameValueUnchanged()
    {
        var input = 1;

        var result = Guard.Against.OutOfRange(input, 1, 10);

        Assert.Equal(input, result);
    }

    [Fact]
    public void OutOfRange_WhenInputEqualsUpperBoundary_ReturnsSameValueUnchanged()
    {
        var input = 10;

        var result = Guard.Against.OutOfRange(input, 1, 10);

        Assert.Equal(input, result);
    }

    [Fact]
    public void OutOfRange_WhenInputBelowLowerBoundary_ThrowsArgumentOutOfRangeExceptionWithCapturedParameterName()
    {
        var input = 0;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Against.OutOfRange(input, 1, 10));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void OutOfRange_WhenInputAboveUpperBoundary_ThrowsArgumentOutOfRangeException()
    {
        var input = 11;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Against.OutOfRange(input, 1, 10));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void OutOfRange_WhenRangeFromGreaterThanRangeTo_ThrowsArgumentExceptionNotArgumentOutOfRangeException()
    {
        var input = 5;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.OutOfRange(input, 10, 1));

        Assert.IsNotType<ArgumentOutOfRangeException>(ex);
    }

    private enum TestFixtureEnum
    {
        First = 0,
        Second = 1,
        Third = 2,
    }

    [Fact]
    public void EnumOutOfRange_WhenDefinedMemberProvided_ReturnsSameValueUnchanged()
    {
        var input = TestFixtureEnum.Second;

        var result = Guard.Against.EnumOutOfRange(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void EnumOutOfRange_WhenUndefinedValueProvided_ThrowsInvalidEnumArgumentException()
    {
        var input = (TestFixtureEnum)99;

        Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(() => Guard.Against.EnumOutOfRange(input));
    }

    [Fact]
    public void EnumOutOfRange_WhenUndefinedValueProvided_ThrowsWithCapturedParameterName()
    {
        var input = (TestFixtureEnum)99;

        var ex = Assert.Throws<System.ComponentModel.InvalidEnumArgumentException>(() => Guard.Against.EnumOutOfRange(input));

        Assert.Contains(nameof(input), ex.Message);
    }
}
