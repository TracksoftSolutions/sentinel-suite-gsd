using System.Linq;
using System.Threading.Tasks;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// <c>Map</c> combinator extension methods for <see cref="Result"/> and
/// <see cref="Result{T}"/> (D-12, D-13).
/// </summary>
/// <remarks>
/// <para>
/// <b>File-per-combinator convention:</b> mirrors RESEARCH.md's "Pattern 2:
/// Left/Right/Both async-overload split for every combinator" and this
/// repo's Phase 1 <c>Guards/</c> file-per-concern precedent. Every combinator
/// gets its own static extension-method file; this file contains only
/// <c>Map</c> — <c>Bind</c>/<c>Then</c>, <c>Ensure</c>, <c>Match</c>,
/// <c>OnSuccess</c>/<c>OnFailure</c>, and <c>Combine</c> each live in their
/// own dedicated files from separate plans (02-04, 02-05, 02-06).
/// </para>
/// <para>
/// Each variant below is implemented across all 4 sync/async shapes: sync
/// (sync source, sync continuation), Left-async (<c>Task</c>-typed source,
/// sync continuation), Right-async (sync source, <c>Task</c>-returning
/// continuation), and Both-async (both operands async). Every async overload
/// awaits its <c>Task</c> operand exactly once via <c>ConfigureAwait(false)</c>,
/// and never invokes the mapper/valueFactory delegate once the source has
/// already failed (T-2-07, T-2-09).
/// </para>
/// <para>
/// <b>Short-circuit fidelity note (deliberate, documented simplification):</b>
/// when the generic <c>Result&lt;TIn&gt; -&gt; Result&lt;TOut&gt;</c> variant
/// short-circuits a failed source into a new <c>Result&lt;TOut&gt;</c>, only
/// the source's <see cref="Result{T}.Errors"/> list is propagated via the
/// generic <c>Failure(...)</c> factory — the source's original
/// <see cref="Result{T}.Status"/> (e.g. <c>NotFound</c>, <c>Conflict</c>,
/// <c>CriticalError</c>) and, for a <c>CriticalError</c> source, its
/// <see cref="Result{T}.Exception"/>, are NOT preserved through the type
/// change; they collapse to <see cref="ResultStatus.Error"/> with a null
/// <c>Exception</c>. This matches RESEARCH.md's own cited Pattern 2 code
/// example verbatim (adapted here to <c>Failure</c> per plan 02-01's naming
/// resolution) and is the correct, minimal-surface choice for this plan —
/// <c>Result&lt;TIn&gt;</c> and <c>Result&lt;TOut&gt;</c> are different
/// closed generic types, so preserving status/exception through the type
/// change would require dispatching through every named factory by a
/// <c>Status</c> switch, which is out of this plan's scope. This is a
/// deliberate simplification: do not "fix" it without a new decision.
/// </para>
/// </remarks>
public static class ResultMapExtensions
{
    /// <summary>
    /// Sync generic variant: transforms a successful <see cref="Result{T}"/>'s
    /// value via <paramref name="mapper"/>; short-circuits without invoking
    /// <paramref name="mapper"/> on a failed <paramref name="result"/>,
    /// propagating its <see cref="Result{T}.Errors"/> into the returned
    /// <see cref="Result{T}"/> (see this class's short-circuit fidelity note).
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> mapper) =>
        result.IsFailure
            ? Result<TOut>.Failure(result.Errors.ToArray())
            : Result<TOut>.Success(mapper(result.Value));

    /// <summary>
    /// Left-async generic variant: awaits <paramref name="resultTask"/>, then
    /// applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, TOut> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(mapper);
    }

    /// <summary>
    /// Right-async generic variant: awaits <paramref name="mapper"/> only
    /// when <paramref name="result"/> succeeded; never invokes or awaits it
    /// when <paramref name="result"/> failed.
    /// </summary>
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<TOut>> mapper)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors.ToArray());
        }

        return Result<TOut>.Success(await mapper(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Both-async generic variant: awaits <paramref name="resultTask"/>, then
    /// composes with the Right-async overload's async continuation behavior.
    /// </summary>
    public static async Task<Result<TOut>> Map<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<TOut>> mapper)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Map(mapper).ConfigureAwait(false);
    }

    /// <summary>
    /// Sync non-generic variant: produces a successful <see cref="Result{T}"/>
    /// by invoking <paramref name="valueFactory"/> on a successful (valueless)
    /// <see cref="Result"/> source; short-circuits without invoking
    /// <paramref name="valueFactory"/> on failure.
    /// </summary>
    public static Result<TOut> Map<TOut>(this Result result, Func<TOut> valueFactory) =>
        result.IsFailure
            ? Result<TOut>.Failure(result.Errors.ToArray())
            : Result<TOut>.Success(valueFactory());

    /// <summary>
    /// Left-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then applies the identical sync success/short-circuit behavior.
    /// </summary>
    public static async Task<Result<TOut>> Map<TOut>(this Task<Result> resultTask, Func<TOut> valueFactory)
    {
        var result = await resultTask.ConfigureAwait(false);
        return result.Map(valueFactory);
    }

    /// <summary>
    /// Right-async non-generic variant: awaits <paramref name="valueFactory"/>
    /// only when <paramref name="result"/> succeeded; never invokes or awaits
    /// it when <paramref name="result"/> failed.
    /// </summary>
    public static async Task<Result<TOut>> Map<TOut>(this Result result, Func<Task<TOut>> valueFactory)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors.ToArray());
        }

        return Result<TOut>.Success(await valueFactory().ConfigureAwait(false));
    }

    /// <summary>
    /// Both-async non-generic variant: awaits <paramref name="resultTask"/>,
    /// then composes with the Right-async overload's async continuation
    /// behavior.
    /// </summary>
    public static async Task<Result<TOut>> Map<TOut>(this Task<Result> resultTask, Func<Task<TOut>> valueFactory)
    {
        var result = await resultTask.ConfigureAwait(false);
        return await result.Map(valueFactory).ConfigureAwait(false);
    }
}
