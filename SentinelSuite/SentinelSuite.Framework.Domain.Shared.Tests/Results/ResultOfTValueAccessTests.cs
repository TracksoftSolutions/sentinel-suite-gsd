using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultOfTValueAccessTests
{
    [Fact]
    public void Success_WhenCalledWithValue_ProducesSuccessfulResultWithOkStatusAndEmptyErrors()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Errors);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Success_WhenCalledWithValue_ValueReturnsExactValueWithoutThrowing()
    {
        var result = Result<int>.Success(42);

        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_WhenCalledWithReferenceTypeValue_ValueIsReferenceEqualToOriginalInstance()
    {
        var instance = new object();

        var result = Result<object>.Success(instance);

        Assert.Same(instance, result.Value);
    }

    [Fact]
    public void Failure_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Failure(new Error("Error.Code", "message"));

        Assert.True(result.IsFailure);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Invalid_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Invalid(new Error("Validation.Required", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void NotFound_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.NotFound(new Error("NotFound.Code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Conflict_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Conflict(new Error("Conflict.Code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Forbidden_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Forbidden(new Error("Forbidden.Code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Unauthorized_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Unauthorized(new Error("Unauthorized.Code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Unavailable_WhenAccessingValue_ThrowsInvalidOperationException()
    {
        var result = Result<int>.Unavailable(new Error("Unavailable.Code", "message"));

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Invalid_WhenCalledWithMultipleErrors_ErrorsContainsExactlyThoseInstancesInOrder()
    {
        var error1 = new Error("Validation.Required", "first");
        var error2 = new Error("Validation.Required", "second");

        var result = Result<int>.Invalid(error1, error2);

        Assert.Equal(new[] { error1, error2 }, result.Errors);
    }
}
