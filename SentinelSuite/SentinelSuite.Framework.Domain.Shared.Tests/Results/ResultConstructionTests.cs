using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultConstructionTests
{
    [Fact]
    public void Success_WhenCalled_ProducesSuccessfulResultWithEmptyErrorsAndNullError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Errors);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WhenCalled_ErrorsIsNonNullEmptyReadOnlyList()
    {
        var result = Result.Success();

        Assert.NotNull(result.Errors);
        Assert.IsAssignableFrom<IReadOnlyList<Error>>(result.Errors);
        Assert.Empty(result.Errors);
    }
}
