using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;

public class GuardAgainstInputTests
{
    [Fact]
    public void InvalidInput_WhenPredicateReturnsTrue_ReturnsSameValueUnchanged()
    {
        var input = 4;

        var result = Guard.Against.InvalidInput(input, x => x % 2 == 0);

        Assert.Equal(input, result);
    }

    [Fact]
    public void InvalidInput_WhenPredicateReturnsFalse_ThrowsArgumentException()
    {
        var input = 3;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.InvalidInput(input, x => x % 2 == 0));

        Assert.Equal(nameof(input), ex.ParamName);
    }

    [Fact]
    public void InvalidInput_WhenPredicateReturnsFalse_DoesNotLeakRejectedValueInMessage()
    {
        var input = 3;

        var ex = Assert.Throws<ArgumentException>(() => Guard.Against.InvalidInput(input, x => x % 2 == 0));

        Assert.DoesNotContain(input.ToString(), ex.Message);
    }
}
