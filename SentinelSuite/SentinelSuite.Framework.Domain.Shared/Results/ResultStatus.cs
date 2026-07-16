namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// The fixed set of outcome statuses a <see cref="Result"/> or
/// <c>Result{T}</c> can carry. Mirrors Ardalis.Result's actual status set
/// (D-08) — deliberately excludes persistence/HTTP-flavored members such as
/// <c>Created</c> or <c>NoContent</c>, which have no meaning in a
/// persistence-agnostic Domain.Shared kernel.
/// </summary>
public enum ResultStatus
{
    /// <summary>The operation succeeded.</summary>
    Ok,

    /// <summary>A generic, not-further-categorized business failure.</summary>
    Error,

    /// <summary>The operation failed one or more validation rules.</summary>
    Invalid,

    /// <summary>The requested resource could not be found.</summary>
    NotFound,

    /// <summary>The operation conflicts with the current state of a resource.</summary>
    Conflict,

    /// <summary>The caller is authenticated but not permitted to perform the operation.</summary>
    Forbidden,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The operation cannot be performed because a dependency is unavailable.</summary>
    Unavailable,

    /// <summary>
    /// An unexpected exception was caught and converted to a <see cref="Result"/>
    /// at a boundary (D-11). Distinct from every other status: only this one
    /// ever carries a non-null <see cref="Result.Exception"/>.
    /// </summary>
    CriticalError,
}
