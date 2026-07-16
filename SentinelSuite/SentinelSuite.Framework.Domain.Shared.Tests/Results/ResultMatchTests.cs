using SentinelSuite.Framework.Domain.Shared.Results;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;

public class ResultMatchTests
{
    [Fact]
    public void Match_Generic_Sync_WhenSourceSucceeds_ReturnsOnSuccessOutputAndNeverInvokesOnFailure()
    {
        var value = 42;
        var result = Result<int>.Success(value);
        var onFailureInvoked = false;

        var matched = result.Match(
            v =>
            {
                Assert.Equal(value, v);
                return "success";
            },
            errors =>
            {
                onFailureInvoked = true;
                return "failure";
            });

        Assert.Equal("success", matched);
        Assert.False(onFailureInvoked);
    }

    [Fact]
    public void Match_Generic_Sync_WhenSourceFails_ReturnsOnFailureOutputWithCompleteErrorsListAndNeverInvokesOnSuccess()
    {
        var error1 = new Error("Match.Code1", "match failure 1");
        var error2 = new Error("Match.Code2", "match failure 2");
        var result = Result<int>.Invalid(error1, error2);
        var onSuccessInvoked = false;

        var matched = result.Match(
            v =>
            {
                onSuccessInvoked = true;
                return "success";
            },
            errors =>
            {
                Assert.Equal(new[] { error1, error2 }, errors);
                return "failure";
            });

        Assert.Equal("failure", matched);
        Assert.False(onSuccessInvoked);
    }

    [Fact]
    public void Match_NonGeneric_Sync_WhenSourceSucceeds_ReturnsOnSuccessOutputAndNeverInvokesOnFailure()
    {
        var result = Result.Success();
        var onFailureInvoked = false;

        var matched = result.Match(
            () => "success",
            errors =>
            {
                onFailureInvoked = true;
                return "failure";
            });

        Assert.Equal("success", matched);
        Assert.False(onFailureInvoked);
    }

    [Fact]
    public void Match_NonGeneric_Sync_WhenSourceFails_ReturnsOnFailureOutputWithCompleteErrorsListAndNeverInvokesOnSuccess()
    {
        var error = new Error("Match.Code", "match failure");
        var result = Result.Invalid(error);
        var onSuccessInvoked = false;

        var matched = result.Match(
            () =>
            {
                onSuccessInvoked = true;
                return "success";
            },
            errors =>
            {
                Assert.Equal(new[] { error }, errors);
                return "failure";
            });

        Assert.Equal("failure", matched);
        Assert.False(onSuccessInvoked);
    }

    [Fact]
    public async Task Match_Generic_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndReturnsOnSuccessOutput()
    {
        var value = 42;
        var resultTask = Task.FromResult(Result<int>.Success(value));

        var matched = await resultTask.Match(
            v => "success",
            errors => "failure");

        Assert.Equal("success", matched);
    }

    [Fact]
    public async Task Match_Generic_LeftAsync_WhenSourceFails_AwaitsSourceAndReturnsOnFailureOutputWithCompleteErrorsList()
    {
        var error1 = new Error("Match.Code1", "match failure 1");
        var error2 = new Error("Match.Code2", "match failure 2");
        var resultTask = Task.FromResult(Result<int>.Invalid(error1, error2));

        var matched = await resultTask.Match(
            v => "success",
            errors =>
            {
                Assert.Equal(new[] { error1, error2 }, errors);
                return "failure";
            });

        Assert.Equal("failure", matched);
    }

    [Fact]
    public async Task Match_Generic_RightAsync_WhenSourceSucceeds_InvokesOnlyAsyncOnSuccessHandler()
    {
        var value = 42;
        var result = Result<int>.Success(value);
        var onSuccessInvoked = false;
        var onFailureInvoked = false;

        var matched = await result.Match(
            async v =>
            {
                onSuccessInvoked = true;
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                onFailureInvoked = true;
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("success", matched);
        Assert.True(onSuccessInvoked);
        Assert.False(onFailureInvoked);
    }

    [Fact]
    public async Task Match_Generic_RightAsync_WhenSourceFails_InvokesOnlyAsyncOnFailureHandler()
    {
        var error = new Error("Match.Code", "match failure");
        var result = Result<int>.Invalid(error);
        var onSuccessInvoked = false;
        var onFailureInvoked = false;

        var matched = await result.Match(
            async v =>
            {
                onSuccessInvoked = true;
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                onFailureInvoked = true;
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("failure", matched);
        Assert.False(onSuccessInvoked);
        Assert.True(onFailureInvoked);
    }

    [Fact]
    public async Task Match_Generic_BothAsync_WhenSourceSucceeds_AwaitsSourceAndReturnsOnSuccessOutput()
    {
        var value = 42;
        var resultTask = Task.FromResult(Result<int>.Success(value));

        var matched = await resultTask.Match(
            async v =>
            {
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("success", matched);
    }

    [Fact]
    public async Task Match_NonGeneric_LeftAsync_WhenSourceSucceeds_AwaitsSourceAndReturnsOnSuccessOutput()
    {
        var resultTask = Task.FromResult(Result.Success());

        var matched = await resultTask.Match(
            () => "success",
            errors => "failure");

        Assert.Equal("success", matched);
    }

    [Fact]
    public async Task Match_NonGeneric_LeftAsync_WhenSourceFails_AwaitsSourceAndReturnsOnFailureOutputWithCompleteErrorsList()
    {
        var error = new Error("Match.Code", "match failure");
        var resultTask = Task.FromResult(Result.Invalid(error));

        var matched = await resultTask.Match(
            () => "success",
            errors =>
            {
                Assert.Equal(new[] { error }, errors);
                return "failure";
            });

        Assert.Equal("failure", matched);
    }

    [Fact]
    public async Task Match_NonGeneric_RightAsync_WhenSourceSucceeds_InvokesOnlyAsyncOnSuccessHandler()
    {
        var result = Result.Success();
        var onSuccessInvoked = false;
        var onFailureInvoked = false;

        var matched = await result.Match(
            async () =>
            {
                onSuccessInvoked = true;
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                onFailureInvoked = true;
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("success", matched);
        Assert.True(onSuccessInvoked);
        Assert.False(onFailureInvoked);
    }

    [Fact]
    public async Task Match_NonGeneric_RightAsync_WhenSourceFails_InvokesOnlyAsyncOnFailureHandler()
    {
        var error = new Error("Match.Code", "match failure");
        var result = Result.Invalid(error);
        var onSuccessInvoked = false;
        var onFailureInvoked = false;

        var matched = await result.Match(
            async () =>
            {
                onSuccessInvoked = true;
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                onFailureInvoked = true;
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("failure", matched);
        Assert.False(onSuccessInvoked);
        Assert.True(onFailureInvoked);
    }

    [Fact]
    public async Task Match_NonGeneric_BothAsync_WhenSourceSucceeds_AwaitsSourceAndReturnsOnSuccessOutput()
    {
        var resultTask = Task.FromResult(Result.Success());

        var matched = await resultTask.Match(
            async () =>
            {
                await Task.Yield();
                return "success";
            },
            async errors =>
            {
                await Task.Yield();
                return "failure";
            });

        Assert.Equal("success", matched);
    }
}
