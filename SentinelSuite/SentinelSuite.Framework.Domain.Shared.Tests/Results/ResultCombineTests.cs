using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultCombineTests
{
    [Fact]
    public void Combine_WhenCalledWithZeroArguments_ReturnsSuccess()
    {
        var combined = Result.Combine();

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void Combine_WhenAllInputsSucceed_ReturnsSuccess()
    {
        var combined = Result.Combine(Result.Success(), Result.Success(), Result.Success());

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void Combine_WhenOneInputFails_ReturnsFailureWithThatErrorOnly()
    {
        var errorA = new Error("Combine.A", "error a");

        var combined = Result.Combine(Result.Success(), Result.Failure(errorA));

        Assert.True(combined.IsFailure);
        Assert.Equal(ResultStatus.Error, combined.Status);
        Assert.Equal(new[] { errorA }, combined.Errors);
    }

    [Fact]
    public void Combine_WhenMultipleInputsFail_ReturnsFlattenedOrderedUnionOfAllErrors()
    {
        var errorA = new Error("Combine.A", "error a");
        var errorB = new Error("Combine.B", "error b");
        var errorC = new Error("Combine.C", "error c");

        var combined = Result.Combine(Result.Failure(errorA), Result.Invalid(errorB, errorC));

        Assert.Equal(new[] { errorA, errorB, errorC }, combined.Errors);
    }

    [Fact]
    public void Combine_WhenFailuresSpanDifferentStatuses_AggregatesAllFailedErrorsAndIgnoresSuccesses()
    {
        var errorA = new Error("Combine.A", "error a");
        var errorB = new Error("Combine.B", "error b");

        var combined = Result.Combine(
            Result.Success(),
            Result.NotFound(errorA),
            Result.Success(),
            Result.Conflict(errorB));

        Assert.True(combined.IsFailure);
        Assert.Equal(new[] { errorA, errorB }, combined.Errors);
    }

    [Fact]
    public void Combine_WhenResultsArrayIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Combine(null!));
    }

    [Fact]
    public void CombineOfT_WhenCalledWithZeroArguments_ReturnsSuccess()
    {
        var combined = Result.Combine<int>();

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void CombineOfT_WhenAllInputsSucceed_ReturnsSuccess()
    {
        var combined = Result.Combine(Result<int>.Success(1), Result<int>.Success(2));

        Assert.True(combined.IsSuccess);
    }

    [Fact]
    public void CombineOfT_WhenOneInputFails_ReturnsFailureWithThatErrorOnly()
    {
        var errorA = new Error("Combine.A", "error a");

        var combined = Result.Combine(Result<int>.Success(1), Result<int>.Failure(errorA));

        Assert.True(combined.IsFailure);
        Assert.Equal(ResultStatus.Error, combined.Status);
        Assert.Equal(new[] { errorA }, combined.Errors);
    }

    [Fact]
    public void CombineOfT_WhenMultipleInputsFail_ReturnsFlattenedOrderedUnionOfAllErrors()
    {
        var errorA = new Error("Combine.A", "error a");
        var errorB = new Error("Combine.B", "error b");

        var combined = Result.Combine(Result<int>.Invalid(errorA), Result<int>.NotFound(errorB));

        Assert.Equal(new[] { errorA, errorB }, combined.Errors);
    }

    [Fact]
    public void CombineOfT_WhenAllInputsSucceed_ReturnsNonGenericResultTypeNotResultOfT()
    {
        var combined = Result.Combine(Result<string>.Success("ok"));

        Assert.IsType<Result>(combined);
    }

    [Fact]
    public void CombineOfT_WhenResultsArrayIsNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Combine<int>(null!));
    }
}
