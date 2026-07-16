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
    public static Result Failure(params Results.Error[] errors) => new(ResultStatus.Error, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Invalid"/>.</summary>
    public static Result Invalid(params Results.Error[] errors) => new(ResultStatus.Invalid, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.NotFound"/>.</summary>
    public static Result NotFound(params Results.Error[] errors) => new(ResultStatus.NotFound, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Conflict"/>.</summary>
    public static Result Conflict(params Results.Error[] errors) => new(ResultStatus.Conflict, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Forbidden"/>.</summary>
    public static Result Forbidden(params Results.Error[] errors) => new(ResultStatus.Forbidden, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Unauthorized"/>.</summary>
    public static Result Unauthorized(params Results.Error[] errors) => new(ResultStatus.Unauthorized, errors);

    /// <summary>Creates a failed <see cref="Result"/> with <see cref="ResultStatus.Unavailable"/>.</summary>
    public static Result Unavailable(params Results.Error[] errors) => new(ResultStatus.Unavailable, errors);
}
