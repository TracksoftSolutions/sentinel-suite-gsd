using SentinelSuite.Framework.Domain.Shared.Guards;

namespace SentinelSuite.Framework.Domain.Shared.Results;

/// <summary>
/// A single structured failure entry carried by a failed <see cref="Result"/>
/// or <c>Result{T}</c> (D-01 through D-03).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Code"/> is a freeform, dot-namespaced convention string (e.g.
/// <c>"Validation.Required"</c>, <c>"Guard.OutOfRange"</c>) rather than a
/// fixed enum, so any of the 26 future modules can mint its own codes
/// without editing <c>Domain.Shared</c> (D-02). <see cref="Message"/> is
/// required and non-empty (D-03), guaranteeing anything catching a failure
/// (logs, UI, a future API response) always has a displayable string.
/// </para>
/// <para>
/// This is deliberately a <c>sealed record</c>, not a plain class, so two
/// <see cref="Error"/> instances with identical <see cref="Code"/> and
/// <see cref="Message"/> compare equal by value — downstream test code and
/// future error-aggregation logic depend on this structural equality.
/// </para>
/// </remarks>
public sealed record Error
{
    /// <summary>
    /// Initializes a new <see cref="Error"/> with a required, non-empty
    /// <paramref name="code"/> and <paramref name="message"/>.
    /// </summary>
    public Error(string code, string message)
    {
        Code = Guard.Against.NullOrWhiteSpace(code);
        Message = Guard.Against.NullOrWhiteSpace(message);
    }

    /// <summary>The freeform, dot-namespaced error code (D-02).</summary>
    public string Code { get; }

    /// <summary>The required, non-empty human-displayable message (D-03).</summary>
    public string Message { get; }
}
