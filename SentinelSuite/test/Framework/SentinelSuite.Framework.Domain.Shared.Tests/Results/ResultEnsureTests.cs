using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultEnsureTests
{
    [Fact]
    public void Ensure_Generic_Sync_WhenSourceSucceedsAndPredicateTrue_ReturnsUnchangedSuccess()
    {
        var invocationCount = 0;
        var value = 42;
        var result = Result<int>.Success(value);
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(v =>
        {
            invocationCount++;
            Assert.Equal(value, v);
            return true;
        }, error);

        Assert.Equal(1, invocationCount);
        Assert.True(ensured.IsSuccess);
        Assert.Equal(value, ensured.Value);
    }

    [Fact]
    public void Ensure_Generic_Sync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var result = Result<int>.Success(42);
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(_ => false, error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
        Assert.Throws<InvalidOperationException>(() => { var _ = ensured.Value; });
    }

    [Fact]
    public void Ensure_Generic_Sync_WhenSourceFails_ShortCircuitsWithoutInvokingPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var result = Result<int>.Invalid(sourceError);
        var predicateInvoked = false;
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(_ =>
        {
            predicateInvoked = true;
            return true;
        }, error);

        Assert.False(predicateInvoked);
        Assert.Equal(result.Status, ensured.Status);
        Assert.Equal(result.Errors, ensured.Errors);
    }

    [Fact]
    public void Ensure_NonGeneric_Sync_WhenSourceSucceedsAndPredicateTrue_ReturnsUnchangedSuccess()
    {
        var result = Result.Success();
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(() => true, error);

        Assert.True(ensured.IsSuccess);
    }

    [Fact]
    public void Ensure_NonGeneric_Sync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var result = Result.Success();
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(() => false, error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public void Ensure_NonGeneric_Sync_WhenSourceFails_ShortCircuitsWithoutInvokingPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var result = Result.Invalid(sourceError);
        var predicateInvoked = false;
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = result.Ensure(() =>
        {
            predicateInvoked = true;
            return true;
        }, error);

        Assert.False(predicateInvoked);
        Assert.Equal(result.Status, ensured.Status);
        Assert.Equal(result.Errors, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_Generic_LeftAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure(_ => false, error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_Generic_LeftAsync_WhenSourceFails_ShortCircuitsWithoutInvokingPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var resultTask = Task.FromResult(Result<int>.Invalid(sourceError));
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure((Func<int, bool>)(_ => throw new InvalidOperationException("predicate should not be invoked")), error);

        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { sourceError }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_Generic_RightAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var result = Result<int>.Success(42);
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await result.Ensure(_ => Task.FromResult(false), error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_Generic_RightAsync_WhenSourceFails_ShortCircuitsWithoutInvokingAsyncPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var result = Result<int>.Invalid(sourceError);
        var predicateInvoked = false;
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await result.Ensure(async _ =>
        {
            predicateInvoked = true;
            await Task.Yield();
            return true;
        }, error);

        Assert.False(predicateInvoked);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { sourceError }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_Generic_BothAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure(_ => Task.FromResult(false), error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_NonGeneric_LeftAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var resultTask = Task.FromResult(Result.Success());
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure(() => false, error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_NonGeneric_LeftAsync_WhenSourceFails_ShortCircuitsWithoutInvokingPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var resultTask = Task.FromResult(Result.Invalid(sourceError));
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure((Func<bool>)(() => throw new InvalidOperationException("predicate should not be invoked")), error);

        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { sourceError }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_NonGeneric_RightAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var result = Result.Success();
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await result.Ensure(() => Task.FromResult(false), error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_NonGeneric_RightAsync_WhenSourceFails_ShortCircuitsWithoutInvokingAsyncPredicate()
    {
        var sourceError = new Error("Source.Code", "source failure");
        var result = Result.Invalid(sourceError);
        var predicateInvoked = false;
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await result.Ensure(async () =>
        {
            predicateInvoked = true;
            await Task.Yield();
            return true;
        }, error);

        Assert.False(predicateInvoked);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { sourceError }, ensured.Errors);
    }

    [Fact]
    public async Task Ensure_NonGeneric_BothAsync_WhenSourceSucceedsAndPredicateFalse_ReturnsInvalidWithSuppliedError()
    {
        var resultTask = Task.FromResult(Result.Success());
        var error = new Error("Ensure.Code", "ensure failure");

        var ensured = await resultTask.Ensure(() => Task.FromResult(false), error);

        Assert.True(ensured.IsFailure);
        Assert.Equal(ResultStatus.Invalid, ensured.Status);
        Assert.Equal(new[] { error }, ensured.Errors);
    }
}
