using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultStatusFactoryTests
{
    [Fact]
    public void Failure_WhenCalledWithSingleError_ProducesFailedResultWithErrorStatus()
    {
        var error = new Error("Error.Code", "message");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void Invalid_WhenCalledWithSingleError_ProducesFailedResultWithInvalidStatus()
    {
        var error = new Error("Validation.Required", "message");

        var result = Result.Invalid(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void NotFound_WhenCalledWithSingleError_ProducesFailedResultWithNotFoundStatus()
    {
        var error = new Error("NotFound.Code", "message");

        var result = Result.NotFound(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public void Conflict_WhenCalledWithSingleError_ProducesFailedResultWithConflictStatus()
    {
        var error = new Error("Conflict.Code", "message");

        var result = Result.Conflict(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void Forbidden_WhenCalledWithSingleError_ProducesFailedResultWithForbiddenStatus()
    {
        var error = new Error("Forbidden.Code", "message");

        var result = Result.Forbidden(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public void Unauthorized_WhenCalledWithSingleError_ProducesFailedResultWithUnauthorizedStatus()
    {
        var error = new Error("Unauthorized.Code", "message");

        var result = Result.Unauthorized(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public void Unavailable_WhenCalledWithSingleError_ProducesFailedResultWithUnavailableStatus()
    {
        var error = new Error("Unavailable.Code", "message");

        var result = Result.Unavailable(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unavailable, result.Status);
    }
}
