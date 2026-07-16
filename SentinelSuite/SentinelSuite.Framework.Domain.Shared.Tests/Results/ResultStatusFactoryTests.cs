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

    [Fact]
    public void ResultOfT_Success_WhenCalledWithValue_ProducesSuccessfulResultWithOkStatus()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public void ResultOfT_Failure_WhenCalledWithSingleError_ProducesFailedResultWithErrorStatus()
    {
        var error = new Error("Error.Code", "message");

        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public void ResultOfT_Invalid_WhenCalledWithSingleError_ProducesFailedResultWithInvalidStatus()
    {
        var error = new Error("Validation.Required", "message");

        var result = Result<int>.Invalid(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public void ResultOfT_NotFound_WhenCalledWithSingleError_ProducesFailedResultWithNotFoundStatus()
    {
        var error = new Error("NotFound.Code", "message");

        var result = Result<int>.NotFound(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public void ResultOfT_Conflict_WhenCalledWithSingleError_ProducesFailedResultWithConflictStatus()
    {
        var error = new Error("Conflict.Code", "message");

        var result = Result<int>.Conflict(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Fact]
    public void ResultOfT_Forbidden_WhenCalledWithSingleError_ProducesFailedResultWithForbiddenStatus()
    {
        var error = new Error("Forbidden.Code", "message");

        var result = Result<int>.Forbidden(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public void ResultOfT_Unauthorized_WhenCalledWithSingleError_ProducesFailedResultWithUnauthorizedStatus()
    {
        var error = new Error("Unauthorized.Code", "message");

        var result = Result<int>.Unauthorized(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unauthorized, result.Status);
    }

    [Fact]
    public void ResultOfT_Unavailable_WhenCalledWithSingleError_ProducesFailedResultWithUnavailableStatus()
    {
        var error = new Error("Unavailable.Code", "message");

        var result = Result<int>.Unavailable(error);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Unavailable, result.Status);
    }

    private sealed class EmptyMessageException : Exception
    {
        public override string Message => string.Empty;
    }

    [Fact]
    public void ResultOfT_CriticalError_WhenCalledWithException_ProducesFailedResultWithCriticalErrorStatus()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int>.CriticalError(exception);

        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void ResultOfT_CriticalError_WhenCalledWithException_ExceptionIsReferenceEqualToOriginal()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int>.CriticalError(exception);

        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void ResultOfT_CriticalError_WhenExceptionMessageIsEmpty_ErrorMessageFallsBackToFixedNonEmptyString()
    {
        var exception = new EmptyMessageException();

        var result = Result<int>.CriticalError(exception);

        Assert.False(string.IsNullOrEmpty(result.Error!.Message));
    }

    [Fact]
    public void ResultOfT_CriticalError_WhenCalledWithException_ErrorsContainsExactlyOneEntry()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result<int>.CriticalError(exception);

        Assert.Single(result.Errors);
        Assert.Same(result.Error, result.Errors[0]);
    }

    [Fact]
    public void ResultOfT_CriticalError_WhenExceptionIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result<int>.CriticalError(null!));
    }
}
