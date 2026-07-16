using System.Linq;
using System.Threading.Tasks;

using SentinelSuite.Framework.Domain.Shared.Guards;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// <c>Bind</c> combinator extension methods for <see cref="Result"/> and
/// <see cref="Result{T}"/> — the railway-chaining mechanism (D-12, D-13).
/// </summary>
/// <remarks>
/// <para>
/// <b>Bind is the sole name for this combinator:</b> the phase context's
/// "Bind/Then" is one combinator with two informal names for the same
/// railway mechanism, and RESEARCH.md's Recommended Project Structure names
/// only <c>ResultBindExtensions.cs</c> (no separate
/// <c>ResultThenExtensions.cs</c>) — so no separate <c>Then</c> method is
/// added here or anywhere else. Do not "complete" a <c>Then</c> alias later;
/// this is a locked naming decision, not an oversight.
/// </para>
/// <para>
/// <b>Short-circuit fidelity note:</b> the generic
/// <c>Result&lt;TIn&gt; -&gt; Result&lt;TOut&gt;</c> variant collapses a
/// failed source's <see cref="Result{T}.Status"/>/<see cref="Result{T}.Exception"/>
/// to the generic <see cref="ResultStatus.Error"/>/null-<c>Exception</c> shape
/// on the type change, propagating only <see cref="Result{T}.Errors"/> via
/// the generic <c>Failure(...)</c> factory — the same deliberate,
/// documented simplification as <see cref="ResultMapExtensions"/>. The
/// non-generic <c>Result -&gt; Result</c> variant has no such gap: on
/// failure it returns the original <see cref="Result"/> instance unchanged
/// (reference-equal), so <see cref="Result.Status"/>/<see cref="Result.Exception"/>
/// are always fully preserved there.
/// </para>
/// <para>
/// <b>Flatten behavior (the railway mechanism):</b> on success, <c>func</c>'s
/// own <see cref="Result{T}"/>/<see cref="Result"/> is returned directly,
/// never re-wrapped — the call site never sees a
/// <c>Result&lt;Result&lt;TOut&gt;&gt;</c>. This is what distinguishes
/// <c>Bind</c> (railway chaining to another <c>Result</c>-returning
/// operation) from <see cref="ResultMapExtensions"/>'s <c>Map</c> (which
/// transforms a bare value and always wraps the mapper's return value in a
/// new success).
/// </para>
/// </remarks>
public static class ResultBindExtensions
{
    /// <summary>
    /// Sync generic variant: chains to <paramref name="func"/>'s own
    /// <see cref="Result{T}"/> on success (flattened, not re-wrapped);
    /// short-circuits without invoking <paramref name="func"/> on a failed
    /// <paramref name="result"/>, propagating its
    /// <see cref="Result{T}.Errors"/> into the returned <see cref="Result{T}"/>.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func)
    {
        Guard.Against.Null(func);

        return result.IsFailure
            ? Result<TOut>.Failure(result.Errors.ToArray())
            : func(result.Value);
    }

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> func)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.Bind(func);
    }

    /// <summary>
    /// Right-async generic variant: awaits <paramref name="func"/> only
    /// when <paramref name="result"/> succeeded; never invokes or awaits it
    /// when <paramref name="result"/> failed.
    /// </summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func)
    {
        Guard.Against.Null(func);

        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors.ToArray());
        }

        return await func(result.Value).ConfigureAwait(false);
    }

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// composes with the Right-async overload's async continuation behavior.
    /// </summary>
    public static async Task<Result<TOut>> Bind<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> func)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.Bind(func).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync non-generic variant: chains to <paramref name="func"/>'s own
    /// <see cref="Result"/> on success (flattened, not re-wrapped);
    /// short-circuits on a failed <paramref name="result"/> by returning the
    /// exact same instance unchanged — no reconstruction needed since both
    /// sides are the same type, which preserves the source's original
    /// <see cref="Result.Status"/> and <see cref="Result.Exception"/> exactly,
    /// unlike the generic variant.
    /// </summary>
    public static Result Bind(this Result result, Func<Result> func)
    {
        Guard.Against.Null(func);

        return result.IsFailure
            ? result
            : func();
    }

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result> Bind(this Task<Result> resultTask, Func<Result> func)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.Bind(func);
    }

    /// <summary>
    /// Right-async non-generic variant: awaits <paramref name="func"/> only
    /// when <paramref name="result"/> succeeded; never invokes or awaits it
    /// when <paramref name="result"/> failed, returning the original failed
    /// instance unchanged.
    /// </summary>
    public static async Task<Result> Bind(this Result result, Func<Task<Result>> func)
    {
        Guard.Against.Null(func);

        if (result.IsFailure)
        {
            return result;
        }

        return await func().ConfigureAwait(false);
    }

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then composes with the Right-async overload's async continuation
    /// behavior.
    /// </summary>
    public static async Task<Result> Bind(this Task<Result> resultTask, Func<Task<Result>> func)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.Bind(func).ConfigureAwait(false);
    }
}
