using System.Linq;

using SentinelSuite.Framework.Domain.Shared.Guards;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail with one
/// or more structured <see cref="Results.Error"/> entries (D-01, D-04).
/// </summary>
/// <remarks>
/// <para>
/// Sealed class with a private constructor and static-factory-only
/// construction (D-16), mirroring Phase 1's <c>Guard</c> precedent —
/// callers never construct a <see cref="Result"/> directly, only through
/// the named static factories below.
/// </para>
/// <para>
/// <b>Naming-collision note:</b> D-04 requires an instance property literally
/// named <see cref="Error"/> (the first-error convenience accessor,
/// <c>result.Error.Message</c>), and D-09 lists a named static factory for
/// the generic <see cref="ResultStatus.Error"/> status. A property and a
/// static method cannot share the same identifier on the same C# type — the
/// compiler rejects it with CS0102. This is resolved by naming the
/// generic-failure static factory <see cref="Failure(Results.Error[])"/>
/// instead of <c>Error(...)</c>, while keeping the <see cref="Error"/>
/// instance property's name exactly as D-04 specifies. <c>Failure</c> is not
/// an invented name: it is CSharpFunctionalExtensions' own actual name for
/// this exact "generic, non-further-categorized failure" factory, so this is
/// a grounded substitution, not an arbitrary one. Do not "fix" this back to
/// a static <c>Error(...)</c> method — it will not compile alongside the
/// <see cref="Error"/> property.
/// </para>
/// </remarks>
public sealed class Result
{
    private static readonly IReadOnlyList<Results.Error> NoErrors = Array.Empty<Results.Error>();

    private Result(ResultStatus status, IReadOnlyList<Results.Error> errors, Exception? exception = null)
    {
        Status = status;
        Errors = errors;
        Exception = exception;
    }

    /// <summary>The outcome status of this <see cref="Result"/>.</summary>
    public ResultStatus Status { get; }

    /// <summary>
    /// The complete, ordered list of structured errors. Always non-null;
    /// empty for a successful <see cref="Result"/>.
    /// </summary>
    public IReadOnlyList<Results.Error> Errors { get; }

    /// <summary>
    /// Convenience accessor for the first entry in <see cref="Errors"/>, or
    /// <see langword="null"/> when the <see cref="Result"/> succeeded
    /// (D-04). Most call sites only care about one error and want
    /// <c>result.Error.Message</c> without indexing; <see cref="Errors"/>
    /// stays available for the multi-error aggregation case.
    /// </summary>
    public Results.Error? Error => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>
    /// The original exception caught and converted to a <see cref="Result"/>
    /// at a boundary. Only ever populated by the <c>CriticalError</c>
    /// factory; every factory in this class leaves it <see langword="null"/>
    /// (D-11).
    /// </summary>
    /// <remarks>
    /// This deliberately diverges from both Ardalis.Result and
    /// CSharpFunctionalExtensions, neither of which attaches an
    /// <see cref="System.Exception"/> to a Result-shaped type — this is a
    /// locked, discussed departure (D-11), not an oversight. Because
    /// <see cref="Exception"/> is a distinct top-level property, never
    /// folded into <see cref="Errors"/>/<see cref="Error"/>, it can be
    /// explicitly stripped at a future serialization boundary.
    /// <see cref="Exception"/> must never be included in any external-facing
    /// serialization of a <see cref="Result"/>; whichever future phase adds
    /// an API/Application layer mapping a <see cref="Result"/> to an HTTP
    /// response is responsible for stripping it before crossing that trust
    /// boundary (T-2-01).
    /// </remarks>
    public Exception? Exception { get; }

    /// <summary>
    /// <see langword="true"/> when <see cref="Status"/> is
    /// <see cref="ResultStatus.Ok"/>.
    /// </summary>
    public bool IsSuccess => Status == ResultStatus.Ok;

    /// <summary>The negation of <see cref="IsSuccess"/>.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Creates a successful <see cref="Result"/>.</summary>
    public static Result Success() => new(ResultStatus.Ok, NoErrors);

    /// <summary>
    /// Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Error"/> —
    /// a generic, not-further-categorized business failure. Named
    /// <c>Failure</c> rather than <c>Error</c> to avoid the CS0102 collision
    /// with the <see cref="Error"/> instance property; see this class's
    /// remarks for the full rationale.
    /// </summary>
    public static Result Failure(params Results.Error[] errors) => new(ResultStatus.Error, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Invalid"/>.</summary>
    public static Result Invalid(params Results.Error[] errors) => new(ResultStatus.Invalid, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.NotFound"/>.</summary>
    public static Result NotFound(params Results.Error[] errors) => new(ResultStatus.NotFound, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Conflict"/>.</summary>
    public static Result Conflict(params Results.Error[] errors) => new(ResultStatus.Conflict, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Forbidden"/>.</summary>
    public static Result Forbidden(params Results.Error[] errors) => new(ResultStatus.Forbidden, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Unauthorized"/>.</summary>
    public static Result Unauthorized(params Results.Error[] errors) => new(ResultStatus.Unauthorized, GuardErrors(errors));

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Unavailable"/>.</summary>
    public static Result Unavailable(params Results.Error[] errors) => new(ResultStatus.Unavailable, GuardErrors(errors));

    /// <summary>
    /// Guards a failure factory's <paramref name="errors"/> array against
    /// being null, empty, or containing a null element, so every failed
    /// <see cref="Result"/> is guaranteed to carry at least one non-null,
    /// displayable <see cref="Results.Error"/> (D-03).
    /// </summary>
    private static Results.Error[] GuardErrors(Results.Error[] errors)
    {
        Guard.Against.NullOrEmpty(errors);

        if (errors.Any(e => e is null))
        {
            throw new ArgumentException("Required input must not contain a null Error.", nameof(errors));
        }

        return errors;
    }

    /// <summary>
    /// Creates a failed <see cref="Result"/> with
    /// <see cref="ResultStatus.CriticalError"/>, carrying the original
    /// caught <paramref name="exception"/> (D-11).
    /// </summary>
    /// <param name="exception">
    /// The caught exception being converted to a <see cref="Result"/>. Must
    /// not be <see langword="null"/> — a null exception is itself a
    /// programmer error (D-05), not something to silently accept.
    /// </param>
    /// <param name="error">
    /// An optional explicit <see cref="Results.Error"/> to use instead of
    /// deriving one from <paramref name="exception"/>. When omitted, an
    /// <see cref="Results.Error"/> is derived using a fixed
    /// <c>"CriticalError"</c> code and <paramref name="exception"/>'s own
    /// <see cref="System.Exception.Message"/> when that is non-null and
    /// non-empty, or a fixed fallback literal otherwise — never passing a
    /// potentially-empty string straight into <see cref="Results.Error"/>'s
    /// constructor, which would otherwise throw and mask the real
    /// <see cref="CriticalError(Exception, Results.Error?)"/> call site with
    /// a confusing secondary exception (T-2-02).
    /// </param>
    public static Result CriticalError(Exception exception, Results.Error? error = null)
    {
        Guard.Against.Null(exception);

        var resolvedError = error ?? new Results.Error(
            "CriticalError",
            string.IsNullOrWhiteSpace(exception.Message) ? "An unexpected error occurred." : exception.Message);

        return new Result(ResultStatus.CriticalError, [resolvedError], exception);
    }

    /// <summary>
    /// All-or-nothing batch aggregator over N independent <see cref="Result"/>
    /// inputs (D-15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Combine(Result[])"/> succeeds — returning <see cref="Success"/> —
    /// only when every element of <paramref name="results"/> succeeds
    /// (including the zero-length/empty-batch case, which trivially has no
    /// failures). On any failure it returns one failed <see cref="Result"/>
    /// whose <see cref="Errors"/> is the flattened, ordered union of
    /// <b>every</b> failed input's <see cref="Errors"/> — not just the first
    /// failure encountered. This is deliberately distinct from
    /// <c>Bind</c>'s short-circuit-on-first-failure chaining: <c>Bind</c>
    /// composes one dependent chain, while <see cref="Combine(Result[])"/>
    /// validates a batch of independent inputs and reports every problem at
    /// once.
    /// </para>
    /// <para>
    /// This method is declared directly on the sealed <see cref="Result"/>
    /// class, rather than as a <c>this Result</c> extension method in a
    /// separate file, specifically so call sites read as
    /// <c>Result.Combine(...)</c> literally. A true C# extension method
    /// cannot provide that call syntax for a <c>params</c>-array-of-the-
    /// extended-type signature — extension methods require a single
    /// instance receiver to extend, not a collection of the type itself.
    /// </para>
    /// </remarks>
    public static Result Combine(params Result[] results)
    {
        Guard.Against.Null(results);

        var failed = results.Where(result => result.IsFailure).ToArray();

        if (failed.Length == 0)
        {
            return Success();
        }

        var errors = failed.SelectMany(result => result.Errors).ToArray();

        return Failure(errors);
    }

    /// <summary>
    /// All-or-nothing batch aggregator over N independent
    /// <see cref="Result{T}"/> inputs (D-15), mirroring
    /// <see cref="Combine(Result[])"/> exactly for the generic
    /// <see cref="Result{T}"/> shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Always returns the non-generic <see cref="Result"/> type, never
    /// <see cref="Result{T}"/> — even when every input succeeds. A
    /// <see cref="Combine{T}(Result{T}[])"/> call has no single meaningful
    /// combined value to expose: there is no way to select one
    /// <typeparamref name="T"/> out of N independent successful
    /// <see cref="Result{T}"/> inputs, so the return type deliberately
    /// matches <see cref="Combine(Result[])"/>'s return type rather than
    /// inventing a combined-value <see cref="Result{T}"/>.
    /// </para>
    /// <para>
    /// Implemented as a separate overload — not via
    /// CSharpFunctionalExtensions'/Ardalis.Result's self-referential
    /// <c>Result : Result&lt;Result&gt;</c> inheritance trick — so that no
    /// inheritance relationship is introduced between <see cref="Result"/>
    /// and <see cref="Result{T}"/>, preserving D-16's requirement that both
    /// types remain sealed (RESEARCH.md Pitfall 4).
    /// </para>
    /// </remarks>
    public static Result Combine<T>(params Result<T>[] results)
    {
        Guard.Against.Null(results);

        var failed = results.Where(result => result.IsFailure).ToArray();

        if (failed.Length == 0)
        {
            return Success();
        }

        var errors = failed.SelectMany(result => result.Errors).ToArray();

        return Failure(errors);
    }
}
