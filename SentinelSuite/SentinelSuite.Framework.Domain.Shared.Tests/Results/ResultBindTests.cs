using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultBindTests
{
    [Fact]
    public void Bind_Generic_Sync_WhenSourceSucceeds_InvokesFuncOnceAndReturnsFuncsOwnResultDirectly()
    {
        var invocationCount = 0;
        var result = Result<int>.Success(21);
        var expected = Result<int>.Success(42);

        Result<int> Func(int value)
        {
            invocationCount++;
            return expected;
        }

        var bound = result.Bind(Func);

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public void Bind_Generic_Sync_WhenSourceFails_NeverInvokesFuncAndPropagatesErrors()
    {
        var error = new Error("Bind.Code", "bind failure");
        var result = Result<int>.Failure(error);

        var bound = result.Bind((Func<int, Result<int>>)(_ => throw new InvalidOperationException("func should not be invoked")));

        Assert.True(bound.IsFailure);
        Assert.Equal(result.Errors, bound.Errors);
    }

    [Fact]
    public async Task Bind_Generic_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndAppliesSyncBehavior()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result<int>.Success(21));
        var expected = Result<int>.Success(42);

        var bound = await resultTask.Bind(value =>
        {
            invocationCount++;
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_Generic_LeftAsync_WhenSourceFails_NeverInvokesFuncAndPropagatesErrors()
    {
        var error = new Error("Bind.Code", "bind failure");
        var resultTask = Task.FromResult(Result<int>.Failure(error));

        var bound = await resultTask.Bind((Func<int, Result<int>>)(_ => throw new InvalidOperationException("func should not be invoked")));

        Assert.True(bound.IsFailure);
        Assert.Equal(new[] { error }, bound.Errors);
    }

    [Fact]
    public async Task Bind_Generic_RightAsync_WhenSourceSucceeds_AwaitsFunc()
    {
        var invocationCount = 0;
        var result = Result<int>.Success(21);
        var expected = Result<int>.Success(42);

        var bound = await result.Bind(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_Generic_RightAsync_WhenSourceFails_NeverInvokesOrAwaitsFunc()
    {
        var error = new Error("Bind.Code", "bind failure");
        var result = Result<int>.Failure(error);
        var invocationCount = 0;

        var bound = await result.Bind(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return Result<int>.Success(value);
        });

        Assert.Equal(0, invocationCount);
        Assert.True(bound.IsFailure);
        Assert.Equal(new[] { error }, bound.Errors);
    }

    [Fact]
    public async Task Bind_Generic_BothAsync_WhenSourceSucceeds_ComposesBothAsyncBehaviors()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result<int>.Success(21));
        var expected = Result<int>.Success(42);

        var bound = await resultTask.Bind(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_Generic_BothAsync_WhenSourceFails_NeverInvokesOrAwaitsFunc()
    {
        var error = new Error("Bind.Code", "bind failure");
        var resultTask = Task.FromResult(Result<int>.Failure(error));
        var invocationCount = 0;

        var bound = await resultTask.Bind(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return Result<int>.Success(value);
        });

        Assert.Equal(0, invocationCount);
        Assert.True(bound.IsFailure);
        Assert.Equal(new[] { error }, bound.Errors);
    }

    [Fact]
    public void Bind_Generic_Sync_WhenChainingTwoBindsAndFirstFails_ShortCircuitsAtFirstFailureAndNeverInvokesSecondFunc()
    {
        var error = new Error("Bind.Code", "first bind failure");
        var secondInvocationCount = 0;

        var result = Result<int>.Success(1)
            .Bind(_ => Result<int>.Failure(error))
            .Bind(value =>
            {
                secondInvocationCount++;
                return Result<int>.Success(value);
            });

        Assert.Equal(0, secondInvocationCount);
        Assert.True(result.IsFailure);
        Assert.Equal(new[] { error }, result.Errors);
    }

    [Fact]
    public void Bind_NonGeneric_Sync_WhenSourceSucceeds_InvokesFuncOnceAndReturnsFuncsOwnResultDirectly()
    {
        var invocationCount = 0;
        var result = Result.Success();
        var expected = Result.Success();

        Result Func()
        {
            invocationCount++;
            return expected;
        }

        var bound = result.Bind(Func);

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public void Bind_NonGeneric_Sync_WhenSourceFails_NeverInvokesFuncAndReturnsExactSameFailedInstance()
    {
        var exception = new InvalidOperationException("original boom");
        var result = Result.CriticalError(exception);

        var bound = result.Bind((Func<Result>)(() => throw new InvalidOperationException("func should not be invoked")));

        Assert.Same(result, bound);
        Assert.Equal(ResultStatus.CriticalError, bound.Status);
        Assert.Same(exception, bound.Exception);
    }

    [Fact]
    public async Task Bind_NonGeneric_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndAppliesSyncBehavior()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());
        var expected = Result.Success();

        var bound = await resultTask.Bind(() =>
        {
            invocationCount++;
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_NonGeneric_LeftAsync_WhenSourceFails_NeverInvokesFuncAndReturnsExactSameFailedInstance()
    {
        var result = Result.Failure(new Error("Bind.Code", "bind failure"));
        var resultTask = Task.FromResult(result);

        var bound = await resultTask.Bind((Func<Result>)(() => throw new InvalidOperationException("func should not be invoked")));

        Assert.Same(result, bound);
    }

    [Fact]
    public async Task Bind_NonGeneric_RightAsync_WhenSourceSucceeds_AwaitsFunc()
    {
        var invocationCount = 0;
        var result = Result.Success();
        var expected = Result.Success();

        var bound = await result.Bind(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_NonGeneric_RightAsync_WhenSourceFails_NeverInvokesOrAwaitsFuncAndReturnsExactSameFailedInstance()
    {
        var result = Result.Failure(new Error("Bind.Code", "bind failure"));
        var invocationCount = 0;

        var bound = await result.Bind(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return Result.Success();
        });

        Assert.Equal(0, invocationCount);
        Assert.Same(result, bound);
    }

    [Fact]
    public async Task Bind_NonGeneric_BothAsync_WhenSourceSucceeds_ComposesBothAsyncBehaviors()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());
        var expected = Result.Success();

        var bound = await resultTask.Bind(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return expected;
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(expected, bound);
    }

    [Fact]
    public async Task Bind_NonGeneric_BothAsync_WhenSourceFails_NeverInvokesOrAwaitsFuncAndReturnsExactSameFailedInstance()
    {
        var result = Result.Failure(new Error("Bind.Code", "bind failure"));
        var resultTask = Task.FromResult(result);
        var invocationCount = 0;

        var bound = await resultTask.Bind(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return Result.Success();
        });

        Assert.Equal(0, invocationCount);
        Assert.Same(result, bound);
    }

    [Fact]
    public async Task Bind_Generic_RightAsync_WhenFuncThrows_ExceptionPropagatesAsFaultedTask()
    {
        var result = Result<int>.Success(21);

        async Task<Result<int>> ThrowingFunc(int value)
        {
            await Task.Yield();
            throw new InvalidOperationException("func boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => result.Bind(ThrowingFunc));
    }

    [Fact]
    public async Task Bind_NonGeneric_BothAsync_WhenFuncThrows_ExceptionPropagatesAsFaultedTask()
    {
        var resultTask = Task.FromResult(Result.Success());

        async Task<Result> ThrowingFunc()
        {
            await Task.Yield();
            throw new InvalidOperationException("func boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => resultTask.Bind(ThrowingFunc));
    }
}
