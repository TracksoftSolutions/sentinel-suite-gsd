using System.Collections.Generic;
using System.Threading.Tasks;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// <c>Match</c> combinator extension methods for <see cref="Result"/> and
/// <see cref="Result{T}"/> — collapses a <see cref="Result"/>/
/// <see cref="Result{T}"/> into a single terminal value via exactly one of
/// two handlers (D-12, D-13).
/// </summary>
/// <remarks>
/// <para>
/// <b>Exactly one handler:</b> <c>Match</c> always invokes exactly one of
/// <c>onSuccess</c>/<c>onFailure</c> — never both, never neither.
/// </para>
/// <para>
/// <b>Complete Errors list:</b> the <c>onFailure</c> handler always receives
/// the complete <see cref="IReadOnlyList{T}"/> of <see cref="Results.Error"/>
/// (<see cref="Result.Errors"/>/<see cref="Result{T}.Errors"/>), not merely
/// the first <see cref="Results.Error"/>, so callers building an aggregate
/// message/response see every failure at once (D-01, D-04).
/// </para>
/// <para>
/// <b>Async overload narrowing (RESEARCH.md Open Question 1):</b> unlike
/// <c>Bind</c>/<c>Ensure</c> (which return a <see cref="Result"/>-shaped
/// type and can mix a sync predicate/continuation independently of the
/// source's sync/async-ness), <c>Match</c> collapses to a plain
/// <c>TOut</c> via two handlers. Allowing each handler to independently be
/// sync or async would explode the overload count (4 source shapes x 4
/// handler-sync/async combinations). This is deliberately narrowed here:
/// for the Right-async and Both-async shapes, <c>onSuccess</c> and
/// <c>onFailure</c> are always both synchronous or both
/// <c>Task&lt;TOut&gt;</c>-returning together — never mixed on the same
/// overload. This is a scoped, documented decision; do not "complete" a
/// mixed-sync/async <c>Match</c> overload later by analogy to <c>Bind</c>'s
/// simpler single-continuation shape.
/// </para>
/// <para>
/// Every async method uses <c>ConfigureAwait(false)</c> throughout, matching
/// RESEARCH.md Pattern 2's citation from CSharpFunctionalExtensions.
/// </para>
/// </remarks>
public static class ResultMatchExtensions
{
    /// <summary>
    /// Sync generic variant: invokes <paramref name="onSuccess"/> with
    /// <see cref="Result{T}.Value"/> when <paramref name="result"/> succeeded;
    /// otherwise invokes <paramref name="onFailure"/> with the complete
    /// <see cref="Result{T}.Errors"/> list.
    /// </summary>
    public static TOut Match<T, TOut>(this Result<T> result, Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Errors);

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync handler-dispatch behavior.
    /// </summary>
    public static async Task<TOut> Match<T, TOut>(this Task<Result<T>> resultTask, Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// Right-async generic variant: awaits exactly one of
    /// <paramref name="onSuccess"/>/<paramref name="onFailure"/> depending on
    /// <paramref name="result"/>'s outcome — never both.
    /// </summary>
    public static async Task<TOut> Match<T, TOut>(this Result<T> result, Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) =>
        result.IsSuccess
            ? await onSuccess(result.Value).ConfigureAwait(false)
            : await onFailure(result.Errors).ConfigureAwait(false);

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// composes with the Right-async overload's async handler-dispatch
    /// behavior.
    /// </summary>
    public static async Task<TOut> Match<T, TOut>(this Task<Result<T>> resultTask, Func<T, Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Match(onSuccess, onFailure).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync non-generic variant: invokes the value-less
    /// <paramref name="onSuccess"/> when <paramref name="result"/> succeeded;
    /// otherwise invokes <paramref name="onFailure"/> with the complete
    /// <see cref="Result.Errors"/> list.
    /// </summary>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure) =>
        result.IsSuccess ? onSuccess() : onFailure(result.Errors);

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync handler-dispatch behavior.
    /// </summary>
    public static async Task<TOut> Match<TOut>(this Task<Result> resultTask, Func<TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    /// <summary>
    /// Right-async non-generic variant: awaits exactly one of the value-less
    /// <paramref name="onSuccess"/>/<paramref name="onFailure"/> depending on
    /// <paramref name="result"/>'s outcome — never both.
    /// </summary>
    public static async Task<TOut> Match<TOut>(this Result result, Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure) =>
        result.IsSuccess
            ? await onSuccess().ConfigureAwait(false)
            : await onFailure(result.Errors).ConfigureAwait(false);

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then composes with the Right-async overload's async handler-dispatch
    /// behavior.
    /// </summary>
    public static async Task<TOut> Match<TOut>(this Task<Result> resultTask, Func<Task<TOut>> onSuccess, Func<IReadOnlyList<Error>, Task<TOut>> onFailure)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Match(onSuccess, onFailure).ConfigureAwait(false);
    }
}
