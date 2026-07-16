using System.Threading.Tasks;

using SentinelSuite.Framework.Domain.Shared.Guards;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// <c>Ensure</c> combinator extension methods for <see cref="Result"/> and
/// <see cref="Result{T}"/> — a validation gate over an already-successful
/// chain (D-12, D-13).
/// </summary>
/// <remarks>
/// <para>
/// <b>Failure status:</b> when <paramref name="predicate"/> evaluates
/// <see langword="false"/> against a successful source, <c>Ensure</c>
/// converts it to <see cref="ResultStatus.Invalid"/> via
/// <see cref="Result.Invalid(Results.Error[])"/>/<see cref="Result{T}.Invalid(Results.Error[])"/>,
/// carrying exactly the supplied <see cref="Results.Error"/>. This is a
/// deliberate, documented choice (not locked by CONTEXT.md) consistent with
/// D-08's status vocabulary and D-12's validation-chain framing — a rejected
/// predicate is "an otherwise-successful value failed validation," which is
/// exactly what <see cref="ResultStatus.Invalid"/> represents. Do not use
/// <see cref="ResultStatus.Error"/>/<c>Failure</c> for <c>Ensure</c>'s failure
/// branch.
/// </para>
/// <para>
/// <b>Short-circuit rule:</b> a failed source <see cref="Result"/>/
/// <see cref="Result{T}"/> is always returned unchanged without ever
/// invoking <paramref name="predicate"/> — mirroring
/// <c>GuardAgainstNullExtensions</c>'s delegates-to-a-more-fundamental-check-first
/// pattern from Phase 1.
/// </para>
/// <para>
/// Every async overload uses <c>ConfigureAwait(false)</c> throughout,
/// matching RESEARCH.md Pattern 2's citation from
/// CSharpFunctionalExtensions.
/// </para>
/// </remarks>
public static class ResultEnsureExtensions
{
    /// <summary>
    /// Sync generic variant: short-circuits an already-failed
    /// <paramref name="result"/> without invoking <paramref name="predicate"/>;
    /// otherwise evaluates <paramref name="predicate"/> against
    /// <see cref="Result{T}.Value"/>, returning <paramref name="result"/>
    /// unchanged when it is <see langword="true"/>, or
    /// <see cref="Result{T}.Invalid(Results.Error[])"/> carrying
    /// <paramref name="error"/> when it is <see langword="false"/>.
    /// </summary>
    public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
    {
        Guard.Against.Null(error);

        if (result.IsFailure)
        {
            return result;
        }

        return predicate(result.Value) ? result : Result<T>.Invalid(error);
    }

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, Error error)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, error);
    }

    /// <summary>
    /// Right-async generic variant: short-circuits an already-failed
    /// <paramref name="result"/> without invoking or awaiting
    /// <paramref name="predicate"/>; otherwise awaits
    /// <paramref name="predicate"/> against <see cref="Result{T}.Value"/>.
    /// </summary>
    public static async Task<Result<T>> Ensure<T>(this Result<T> result, Func<T, Task<bool>> predicate, Error error)
    {
        Guard.Against.Null(error);

        if (result.IsFailure)
        {
            return result;
        }

        var isValid = await predicate(result.Value).ConfigureAwait(false);
        return isValid ? result : Result<T>.Invalid(error);
    }

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// composes with the Right-async overload's async predicate behavior.
    /// </summary>
    public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, Task<bool>> predicate, Error error)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Ensure(predicate, error).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync non-generic variant: short-circuits an already-failed
    /// <paramref name="result"/> without invoking <paramref name="predicate"/>;
    /// otherwise evaluates the value-less <paramref name="predicate"/>,
    /// returning <paramref name="result"/> unchanged when it is
    /// <see langword="true"/>, or <see cref="Result.Invalid(Results.Error[])"/>
    /// carrying <paramref name="error"/> when it is <see langword="false"/>.
    /// </summary>
    public static Result Ensure(this Result result, Func<bool> predicate, Error error)
    {
        Guard.Against.Null(error);

        if (result.IsFailure)
        {
            return result;
        }

        return predicate() ? result : Result.Invalid(error);
    }

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result> Ensure(this Task<Result> resultTask, Func<bool> predicate, Error error)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Ensure(predicate, error);
    }

    /// <summary>
    /// Right-async non-generic variant: short-circuits an already-failed
    /// <paramref name="result"/> without invoking or awaiting
    /// <paramref name="predicate"/>; otherwise awaits the value-less
    /// <paramref name="predicate"/>.
    /// </summary>
    public static async Task<Result> Ensure(this Result result, Func<Task<bool>> predicate, Error error)
    {
        Guard.Against.Null(error);

        if (result.IsFailure)
        {
            return result;
        }

        var isValid = await predicate().ConfigureAwait(false);
        return isValid ? result : Result.Invalid(error);
    }

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then composes with the Right-async overload's async predicate
    /// behavior.
    /// </summary>
    public static async Task<Result> Ensure(this Task<Result> resultTask, Func<Task<bool>> predicate, Error error)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Ensure(predicate, error).ConfigureAwait(false);
    }
}
