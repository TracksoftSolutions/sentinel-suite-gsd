namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// Represents the outcome of an operation that can succeed with a value of
/// type <typeparamref name="T"/>, or fail with one or more structured
/// <see cref="Results.Error"/> entries (D-01, D-04, D-10).
/// </summary>
/// <remarks>
/// <para>
/// Sealed class with a private constructor and static-factory-only
/// construction (D-16), mirroring plan 02-01's <see cref="Result"/> exactly.
/// Callers never construct a <see cref="Result{T}"/> directly, only through
/// the named static factories below.
/// </para>
/// <para>
/// <b>Naming-collision note (carried forward from plan 02-01):</b> just like
/// the non-generic <see cref="Result"/>, this class names its generic-failure
/// static factory <see cref="Failure(Results.Error[])"/> instead of
/// <c>Error(...)</c>, to avoid the CS0102 collision between a static method
/// and the <see cref="Error"/> instance property. D-10 requires
/// <see cref="Result{T}"/> to expose the identical named factory set as
/// <see cref="Result"/>, so this naming choice is mirrored exactly rather
/// than re-derived.
/// </para>
/// <para>
/// <b>Value getter fail-fast note (D-06):</b> <see cref="Value"/>'s getter
/// deliberately diverges from Ardalis.Result's actual shape, which is an
/// unguarded auto-property (<c>public T Value { get; init; }</c>) that
/// silently returns <c>default(T)</c> on a failed instance. This class's
/// <see cref="Value"/> getter explicitly checks <see cref="IsFailure"/> and
/// throws <see cref="InvalidOperationException"/> before ever returning the
/// backing field, so a failed <see cref="Result{T}"/> can never silently
/// present an uninitialized value as if it were a legitimately-produced
/// result (RESEARCH.md Pitfall 3).
/// </para>
/// </remarks>
public sealed class Result<T>
{
    private static readonly IReadOnlyList<Results.Error> NoErrors = Array.Empty<Results.Error>();

    private readonly T? _value;

    private Result(ResultStatus status, T? value, IReadOnlyList<Results.Error> errors, Exception? exception = null)
    {
        Status = status;
        _value = value;
        Errors = errors;
        Exception = exception;
    }

    /// <summary>The outcome status of this <see cref="Result{T}"/>.</summary>
    public ResultStatus Status { get; }

    /// <summary>
    /// The complete, ordered list of structured errors. Always non-null;
    /// empty for a successful <see cref="Result{T}"/>.
    /// </summary>
    public IReadOnlyList<Results.Error> Errors { get; }

    /// <summary>
    /// Convenience accessor for the first entry in <see cref="Errors"/>, or
    /// <see langword="null"/> when the <see cref="Result{T}"/> succeeded
    /// (D-04). Most call sites only care about one error and want
    /// <c>result.Error.Message</c> without indexing; <see cref="Errors"/>
    /// stays available for the multi-error aggregation case.
    /// </summary>
    public Results.Error? Error => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>
    /// The original exception caught and converted to a
    /// <see cref="Result{T}"/> at a boundary. Only ever populated by the
    /// <c>CriticalError</c> factory; every other factory in this class
    /// leaves it <see langword="null"/> (D-11).
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// <see langword="true"/> when <see cref="Status"/> is
    /// <see cref="ResultStatus.Ok"/>.
    /// </summary>
    public bool IsSuccess => Status == ResultStatus.Ok;

    /// <summary>The negation of <see cref="IsSuccess"/>.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The successfully-produced value. Throws
    /// <see cref="InvalidOperationException"/> when accessed on a failed
    /// <see cref="Result{T}"/> instead of silently returning
    /// <c>default(T)</c> — a deliberate divergence from Ardalis.Result's
    /// unguarded auto-property shape (D-06, RESEARCH.md Pitfall 3).
    /// </summary>
    public T Value =>
        IsFailure
            ? throw new InvalidOperationException(
                $"Cannot access {nameof(Value)} on a failed Result (Status: {Status}).")
            : _value!;

    /// <summary>Creates a successful <see cref="Result{T}"/> carrying <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(ResultStatus.Ok, value, NoErrors);

    /// <summary>
    /// Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Error"/> —
    /// a generic, not-further-categorized business failure. Named
    /// <c>Failure</c> rather than <c>Error</c> to avoid the CS0102 collision
    /// with the <see cref="Error"/> instance property, mirroring plan
    /// 02-01's <see cref="Result.Failure(Results.Error[])"/> naming exactly (D-10).
    /// </summary>
    public static Result<T> Failure(params Results.Error[] errors) => new(ResultStatus.Error, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Invalid"/>.</summary>
    public static Result<T> Invalid(params Results.Error[] errors) => new(ResultStatus.Invalid, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.NotFound"/>.</summary>
    public static Result<T> NotFound(params Results.Error[] errors) => new(ResultStatus.NotFound, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Conflict"/>.</summary>
    public static Result<T> Conflict(params Results.Error[] errors) => new(ResultStatus.Conflict, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Forbidden"/>.</summary>
    public static Result<T> Forbidden(params Results.Error[] errors) => new(ResultStatus.Forbidden, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Unauthorized"/>.</summary>
    public static Result<T> Unauthorized(params Results.Error[] errors) => new(ResultStatus.Unauthorized, default, errors);

    /// <summary>Creates a failed <see cref="Result{T}"/> with <see cref="ResultStatus.Unavailable"/>.</summary>
    public static Result<T> Unavailable(params Results.Error[] errors) => new(ResultStatus.Unavailable, default, errors);
}
