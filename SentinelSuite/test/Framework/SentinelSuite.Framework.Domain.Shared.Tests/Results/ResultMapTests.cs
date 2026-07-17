using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultMapTests
{
    [Fact]
    public void Map_Generic_Sync_WhenSourceSucceeds_InvokesMapperOnceAndReturnsSuccessfulResultWithMappedValue()
    {
        var invocationCount = 0;
        var result = Result<int>.Success(21);

        var mapped = result.Map(value =>
        {
            invocationCount++;
            return value * 2;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Map_Generic_Sync_WhenSourceFails_NeverInvokesMapperAndPropagatesErrors()
    {
        var error = new Error("Map.Code", "map failure");
        var result = Result<int>.Failure(error);

        var mapped = result.Map((Func<int, int>)(_ => throw new InvalidOperationException("mapper should not be invoked")));

        Assert.True(mapped.IsFailure);
        Assert.Equal(result.Errors, mapped.Errors);
    }

    [Fact]
    public async Task Map_Generic_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndAppliesSyncBehavior()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result<int>.Success(21));

        var mapped = await resultTask.Map(value =>
        {
            invocationCount++;
            return value * 2;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_Generic_LeftAsync_WhenSourceFails_NeverInvokesMapperAndPropagatesErrors()
    {
        var error = new Error("Map.Code", "map failure");
        var resultTask = Task.FromResult(Result<int>.Failure(error));

        var mapped = await resultTask.Map((Func<int, int>)(_ => throw new InvalidOperationException("mapper should not be invoked")));

        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public async Task Map_Generic_RightAsync_WhenSourceSucceeds_AwaitsMapper()
    {
        var invocationCount = 0;
        var result = Result<int>.Success(21);

        var mapped = await result.Map(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return value * 2;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_Generic_RightAsync_WhenSourceFails_NeverInvokesOrAwaitsMapper()
    {
        var error = new Error("Map.Code", "map failure");
        var result = Result<int>.Failure(error);
        var invocationCount = 0;

        var mapped = await result.Map<int, int>(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return value;
        });

        Assert.Equal(0, invocationCount);
        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public async Task Map_Generic_BothAsync_WhenSourceSucceeds_ComposesBothAsyncBehaviors()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result<int>.Success(21));

        var mapped = await resultTask.Map(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return value * 2;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_Generic_BothAsync_WhenSourceFails_NeverInvokesOrAwaitsMapper()
    {
        var error = new Error("Map.Code", "map failure");
        var resultTask = Task.FromResult(Result<int>.Failure(error));
        var invocationCount = 0;

        var mapped = await resultTask.Map<int, int>(async value =>
        {
            invocationCount++;
            await Task.Yield();
            return value;
        });

        Assert.Equal(0, invocationCount);
        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public void Map_NonGeneric_Sync_WhenSourceSucceeds_InvokesValueFactoryOnceAndReturnsSuccessfulResult()
    {
        var invocationCount = 0;
        var result = Result.Success();

        var mapped = result.Map(() =>
        {
            invocationCount++;
            return 42;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public void Map_NonGeneric_Sync_WhenSourceFails_NeverInvokesValueFactoryAndPropagatesErrors()
    {
        var error = new Error("Map.Code", "map failure");
        var result = Result.Failure(error);

        var mapped = result.Map((Func<int>)(() => throw new InvalidOperationException("valueFactory should not be invoked")));

        Assert.True(mapped.IsFailure);
        Assert.Equal(result.Errors, mapped.Errors);
    }

    [Fact]
    public async Task Map_NonGeneric_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndAppliesSyncBehavior()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());

        var mapped = await resultTask.Map(() =>
        {
            invocationCount++;
            return 42;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_NonGeneric_LeftAsync_WhenSourceFails_NeverInvokesValueFactoryAndPropagatesErrors()
    {
        var error = new Error("Map.Code", "map failure");
        var resultTask = Task.FromResult(Result.Failure(error));

        var mapped = await resultTask.Map((Func<int>)(() => throw new InvalidOperationException("valueFactory should not be invoked")));

        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public async Task Map_NonGeneric_RightAsync_WhenSourceSucceeds_AwaitsValueFactory()
    {
        var invocationCount = 0;
        var result = Result.Success();

        var mapped = await result.Map(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_NonGeneric_RightAsync_WhenSourceFails_NeverInvokesOrAwaitsValueFactory()
    {
        var error = new Error("Map.Code", "map failure");
        var result = Result.Failure(error);
        var invocationCount = 0;

        var mapped = await result.Map<int>(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(0, invocationCount);
        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public async Task Map_NonGeneric_BothAsync_WhenSourceSucceeds_ComposesBothAsyncBehaviors()
    {
        var invocationCount = 0;
        var resultTask = Task.FromResult(Result.Success());

        var mapped = await resultTask.Map(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(1, invocationCount);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(42, mapped.Value);
    }

    [Fact]
    public async Task Map_NonGeneric_BothAsync_WhenSourceFails_NeverInvokesOrAwaitsValueFactory()
    {
        var error = new Error("Map.Code", "map failure");
        var resultTask = Task.FromResult(Result.Failure(error));
        var invocationCount = 0;

        var mapped = await resultTask.Map<int>(async () =>
        {
            invocationCount++;
            await Task.Yield();
            return 42;
        });

        Assert.Equal(0, invocationCount);
        Assert.True(mapped.IsFailure);
        Assert.Equal(new[] { error }, mapped.Errors);
    }

    [Fact]
    public async Task Map_Generic_RightAsync_WhenMapperThrows_ExceptionPropagatesAsFaultedTask()
    {
        var result = Result<int>.Success(21);

        async Task<int> ThrowingMapper(int value)
        {
            await Task.Yield();
            throw new InvalidOperationException("mapper boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => result.Map(ThrowingMapper));
    }

    [Fact]
    public async Task Map_NonGeneric_BothAsync_WhenValueFactoryThrows_ExceptionPropagatesAsFaultedTask()
    {
        var resultTask = Task.FromResult(Result.Success());

        async Task<int> ThrowingValueFactory()
        {
            await Task.Yield();
            throw new InvalidOperationException("valueFactory boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => resultTask.Map(ThrowingValueFactory));
    }
}
