using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultOnSuccessOnFailureTests
{
    [Fact]
    public void OnSuccess_NonGeneric_Sync_WhenSuccess_InvokesActionOnceAndReturnsSameInstance()
    {
        var invocationCount = 0;
        var result = Result.Success();

        var returned = result.OnSuccess(() => invocationCount++);

        Assert.Equal(1, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_NonGeneric_Sync_WhenFailure_NeverInvokesActionAndReturnsSameInstance()
    {
        var invocationCount = 0;
        var error = new Error("OnSuccess.Code", "on success failure");
        var result = Result.Failure(error);

        var returned = result.OnSuccess(() => invocationCount++);

        Assert.Equal(0, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_Generic_Sync_WhenSuccess_InvokesActionWithValueAndReturnsSameInstance()
    {
        var invocationCount = 0;
        var value = 42;
        var result = Result<int>.Success(value);

        var returned = result.OnSuccess(v =>
        {
            invocationCount++;
            Assert.Equal(value, v);
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_Generic_Sync_WhenFailure_NeverInvokesActionAndReturnsSameInstance()
    {
        var invocationCount = 0;
        var error = new Error("OnSuccess.Code", "on success failure");
        var result = Result<int>.Failure(error);

        var returned = result.OnSuccess(_ => invocationCount++);

        Assert.Equal(0, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public async Task OnSuccess_NonGeneric_LeftAsync_WhenSuccess_InvokesActionOnce()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());

        var returned = await resultTask.OnSuccess(() => invocationCount++);

        Assert.Equal(1, invocationCount);
        Assert.True(returned.IsSuccess);
    }

    [Fact]
    public async Task OnSuccess_NonGeneric_RightAsync_WhenSuccess_InvokesAsyncActionOnceAndReturnsOriginalResult()
    {
        var invocationCount = 0;
        var result = Result.Success();

        var returned = await result.OnSuccess(async () =>
        {
            invocationCount++;
            await Task.Yield();
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public async Task OnSuccess_NonGeneric_BothAsync_WhenSuccess_InvokesAsyncActionOnce()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());

        var returned = await resultTask.OnSuccess(async () =>
        {
            invocationCount++;
            await Task.Yield();
        });

        Assert.Equal(1, invocationCount);
        Assert.True(returned.IsSuccess);
    }

    [Fact]
    public async Task OnSuccess_Generic_LeftAsync_WhenSuccess_InvokesActionWithValue()
    {
        var invocationCount = 0;
        var value = 42;
        var resultTask = Task.FromResult(Result<int>.Success(value));

        var returned = await resultTask.OnSuccess(v =>
        {
            invocationCount++;
            Assert.Equal(value, v);
        });

        Assert.Equal(1, invocationCount);
        Assert.Equal(value, returned.Value);
    }

    [Fact]
    public async Task OnSuccess_Generic_RightAsync_WhenSuccess_InvokesAsyncActionWithValue()
    {
        var invocationCount = 0;
        var value = 42;
        var result = Result<int>.Success(value);

        var returned = await result.OnSuccess(async v =>
        {
            invocationCount++;
            Assert.Equal(value, v);
            await Task.Yield();
        });

        Assert.Equal(1, invocationCount);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_NonGeneric_Sync_WhenActionIsNull_ThrowsArgumentNullException()
    {
        var result = Result.Success();

        Assert.Throws<ArgumentNullException>(() => result.OnSuccess((Action)null!));
    }
}
