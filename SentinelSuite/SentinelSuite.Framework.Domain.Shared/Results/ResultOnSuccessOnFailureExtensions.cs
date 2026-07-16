using System.Threading.Tasks;
using SentinelSuite.Framework.Domain.Shared.Guards;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// <c>OnSuccess</c>/<c>OnFailure</c> side-effect combinator extension methods
/// for <see cref="Result"/> and <see cref="Result{T}"/> — attach a
/// caller-supplied action to a chain without altering it (D-12).
/// </summary>
/// <remarks>
/// <para>
/// <b>Sync/async axis (D-13):</b> every combinator ships the full 4-shape
/// overload matrix — sync source with sync action; async source
/// (<c>Task&lt;Result&gt;</c>/<c>Task&lt;Result{T}&gt;</c>) with sync action
/// ("Left-async"); sync source with an async (<c>Func&lt;Task&gt;</c>) action
/// ("Right-async"); and async source with an async action ("Both-async").
/// </para>
/// <para>
/// <b>Value/error-awareness axis (RESEARCH.md Open Question 1 — resolved,
/// not left ambiguous):</b> this axis is deliberately narrowed to one
/// natural shape per verb. <c>OnSuccess</c> on <see cref="Result{T}"/>
/// receives the value (<c>Action&lt;T&gt;</c>/<c>Func&lt;T, Task&gt;</c>),
/// because <see cref="Result{T}.Value"/> is always safe to read once
/// <see cref="Result{T}.IsSuccess"/> is confirmed true. <c>OnSuccess</c> on
/// the non-generic <see cref="Result"/> and <c>OnFailure</c> on both types
/// take a no-argument <see cref="Action"/>/<c>Func&lt;Task&gt;</c>:
/// <see cref="Result{T}.Value"/> throws on a failed instance (D-06), so an
/// <c>OnFailure</c> overload that tried to pass <c>T</c> to its action would
/// be unsound by construction, and the non-generic <see cref="Result"/>
/// never has a value to pass in the first place. No errors-aware <c>OnFailure(Action&lt;IReadOnlyList&lt;Error&gt;&gt;)</c>
/// overload is added — callers who need <c>.Errors</c>/<c>.Error</c> inside
/// the failure hook already have the source <see cref="Result"/>/
/// <see cref="Result{T}"/> in scope via the variable they are chaining from
/// (for example <c>result.OnFailure(() =&gt; log.Error(result.Error!.Message))</c>),
/// so a parameterized overload would only duplicate information already
/// available rather than add capability. This keeps the combinator surface
/// at 8 methods per verb (16 total) instead of doubling again for an
/// errors-aware variant.
/// </para>
/// <para>
/// <b>Never transforms:</b> unlike <c>Map</c>/<c>Bind</c> (02-03) or
/// <c>Ensure</c>/<c>Match</c> (02-04), neither <c>OnSuccess</c> nor
/// <c>OnFailure</c> ever constructs a new <see cref="Result"/>/
/// <see cref="Result{T}"/> — every overload returns the exact same instance
/// it received, whether or not the action fired.
/// </para>
/// <para>
/// Every overload validates its action/func delegate via
/// <see cref="Guard.Against"/>'s <c>Null</c> guard before inspecting
/// <see cref="Result"/>/<see cref="Result{T}"/> state (T-2-08). Every async
/// overload uses <c>ConfigureAwait(false)</c> throughout, matching
/// RESEARCH.md Pattern 2.
/// </para>
/// </remarks>
public static class ResultOnSuccessOnFailureExtensions
{
    /// <summary>
    /// Sync non-generic variant: invokes <paramref name="action"/> exactly
    /// once when <paramref name="result"/> is successful, never when it is
    /// failed, and returns <paramref name="result"/> unchanged either way.
    /// </summary>
    public static Result OnSuccess(this Result result, Action action)
    {
        Guard.Against.Null(action);

        if (result.IsSuccess)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync invocation behavior.
    /// </summary>
    public static async Task<Result> OnSuccess(this Task<Result> resultTask, Action action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.OnSuccess(action);
    }

    /// <summary>
    /// Right-async non-generic variant: awaits <paramref name="action"/>
    /// exactly once when <paramref name="result"/> is successful, never when
    /// it is failed, and returns <paramref name="result"/> unchanged either
    /// way.
    /// </summary>
    public static async Task<Result> OnSuccess(this Result result, Func<Task> action)
    {
        Guard.Against.Null(action);

        if (result.IsSuccess)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then awaits the delegation to the Right-async overload's async
    /// invocation behavior.
    /// </summary>
    public static async Task<Result> OnSuccess(this Task<Result> resultTask, Func<Task> action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.OnSuccess(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync generic variant: invokes <paramref name="action"/> with
    /// <see cref="Result{T}.Value"/> exactly once when <paramref name="result"/>
    /// is successful (safe here specifically because <see cref="Result{T}.IsSuccess"/>
    /// gates it), never when it is failed, and returns <paramref name="result"/>
    /// unchanged either way.
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        Guard.Against.Null(action);

        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync invocation behavior.
    /// </summary>
    public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Action<T> action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.OnSuccess(action);
    }

    /// <summary>
    /// Right-async generic variant: awaits <paramref name="action"/> with
    /// <see cref="Result{T}.Value"/> exactly once when <paramref name="result"/>
    /// is successful, never when it is failed, and returns
    /// <paramref name="result"/> unchanged either way.
    /// </summary>
    public static async Task<Result<T>> OnSuccess<T>(this Result<T> result, Func<T, Task> action)
    {
        Guard.Against.Null(action);

        if (result.IsSuccess)
        {
            await action(result.Value).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// awaits the delegation to the Right-async overload's async invocation
    /// behavior.
    /// </summary>
    public static async Task<Result<T>> OnSuccess<T>(this Task<Result<T>> resultTask, Func<T, Task> action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.OnSuccess(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync non-generic variant: invokes <paramref name="action"/> exactly
    /// once when <paramref name="result"/> is failed, never when it is
    /// successful, and returns <paramref name="result"/> unchanged either
    /// way. This is <c>OnSuccess</c>'s no-argument shape confirmed exactly as
    /// documented in this class's remarks — <c>OnFailure</c> never receives a
    /// value.
    /// </summary>
    public static Result OnFailure(this Result result, Action action)
    {
        Guard.Against.Null(action);

        if (result.IsFailure)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync invocation behavior.
    /// </summary>
    public static async Task<Result> OnFailure(this Task<Result> resultTask, Action action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.OnFailure(action);
    }

    /// <summary>
    /// Right-async non-generic variant: awaits <paramref name="action"/>
    /// exactly once when <paramref name="result"/> is failed, never when it
    /// is successful, and returns <paramref name="result"/> unchanged either
    /// way.
    /// </summary>
    public static async Task<Result> OnFailure(this Result result, Func<Task> action)
    {
        Guard.Against.Null(action);

        if (result.IsFailure)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then awaits the delegation to the Right-async overload's async
    /// invocation behavior.
    /// </summary>
    public static async Task<Result> OnFailure(this Task<Result> resultTask, Func<Task> action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.OnFailure(action).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync generic variant: invokes the no-argument <paramref name="action"/>
    /// exactly once when <paramref name="result"/> is failed, never when it
    /// is successful, and returns <paramref name="result"/> unchanged either
    /// way. Never references <see cref="Result{T}.Value"/>, which throws on a
    /// failed instance (D-06).
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action action)
    {
        Guard.Against.Null(action);

        if (result.IsFailure)
        {
            action();
        }

        return result;
    }

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync invocation behavior.
    /// </summary>
    public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Action action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return result.OnFailure(action);
    }

    /// <summary>
    /// Right-async generic variant: awaits the no-argument
    /// <paramref name="action"/> exactly once when <paramref name="result"/>
    /// is failed, never when it is successful, and returns
    /// <paramref name="result"/> unchanged either way.
    /// </summary>
    public static async Task<Result<T>> OnFailure<T>(this Result<T> result, Func<Task> action)
    {
        Guard.Against.Null(action);

        if (result.IsFailure)
        {
            await action().ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// awaits the delegation to the Right-async overload's async invocation
    /// behavior.
    /// </summary>
    public static async Task<Result<T>> OnFailure<T>(this Task<Result<T>> resultTask, Func<Task> action)
    {
        _ = Guard.Against.Null(resultTask);

        var result = await resultTask.ConfigureAwait(false);
        return await result.OnFailure(action).ConfigureAwait(false);
    }
}
