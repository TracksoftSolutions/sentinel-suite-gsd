using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;

public class GuardAgainstNumericTests
{
    [Fact]
    public void Negative_WhenInputPositive_ReturnsSameValueUnchanged()
    {
        var input = 5;

        var result = Guard.Against.Negative(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Negative_WhenInputNegative_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = -1;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.Negative(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void Negative_WhenInputZero_ReturnsSameValueUnchanged()
    {
        var input = 0;

        var result = Guard.Against.Negative(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void NegativeOrZero_WhenInputPositive_ReturnsSameValueUnchanged()
    {
        var input = 5;

        var result = Guard.Against.NegativeOrZero(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void NegativeOrZero_WhenInputZero_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = 0;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.NegativeOrZero(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void NegativeOrZero_WhenInputNegative_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = -1;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.NegativeOrZero(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void Zero_WhenInputNonZero_ReturnsSameValueUnchanged()
    {
        var input = 5;

        var result = Guard.Against.Zero(input);

        Assert.Equal(input, result);
    }

    [Fact]
    public void Zero_WhenInputZero_ThrowsArgumentExceptionWithCapturedParameterName()
    {
        var input = 0;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.Zero(input));

        Assert.Equal(nameof(input), ex.ParamName);
    }
}
