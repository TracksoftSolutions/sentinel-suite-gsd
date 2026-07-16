using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultCriticalErrorTests
{
    private sealed class EmptyMessageException : Exception
    {
        public override string Message => string.Empty;
    }

    [Fact]
    public void CriticalError_WhenCalledWithException_ProducesFailedResultWithCriticalErrorStatus()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.CriticalError(exception);

        Assert.Equal(ResultStatus.CriticalError, result.Status);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CriticalError_WhenCalledWithException_ExceptionIsReferenceEqualToOriginal()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.CriticalError(exception);

        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void CriticalError_WhenExceptionMessageIsNonEmpty_ErrorMessageEqualsExceptionMessage()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.CriticalError(exception);

        Assert.Equal(exception.Message, result.Error!.Message);
    }

    [Fact]
    public void CriticalError_WhenExceptionMessageIsEmpty_ErrorMessageFallsBackToFixedNonEmptyString()
    {
        var exception = new EmptyMessageException();

        var result = Result.CriticalError(exception);

        Assert.False(string.IsNullOrEmpty(result.Error!.Message));
    }

    [Fact]
    public void CriticalError_WhenCalledWithException_ErrorsContainsExactlyOneEntry()
    {
        var exception = new InvalidOperationException("boom");

        var result = Result.CriticalError(exception);

        Assert.Single(result.Errors);
        Assert.Same(result.Error, result.Errors[0]);
    }

    [Fact]
    public void CriticalError_WhenExceptionIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.CriticalError(null!));
    }

    [Fact]
    public void Success_WhenCalled_ExceptionIsNull()
    {
        var result = Result.Success();

        Assert.Null(result.Exception);
    }

    [Fact]
    public void Invalid_WhenCalledWithError_ExceptionIsNull()
    {
        var result = Result.Invalid(new Error("Validation.Required", "message"));

        Assert.Null(result.Exception);
    }
}
