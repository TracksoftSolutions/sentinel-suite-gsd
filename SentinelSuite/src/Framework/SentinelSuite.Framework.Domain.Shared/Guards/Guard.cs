namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Single static entry point for all guard clauses in the kernel:
/// <c>Guard.Against.Null(x)</c>, <c>Guard.Against.NullOrEmpty(x)</c>, etc.
/// This class itself declares no guard methods — every actual guard is an
/// extension method on <see cref="IGuardClause"/>, dispatched through the
/// <see cref="Against"/> singleton. Call sites never construct
/// <see cref="Guard"/> directly.
/// </summary>
/// <remarks>
/// <para>
/// Naming convention for guard-extension classes (D-06): framework-level
/// guards living in this assembly (<c>Domain.Shared</c>) are grouped one
/// static class per concern, named <c>GuardAgainst{Concept}Extensions</c>
/// (for example <c>GuardAgainstNullExtensions</c>, <c>GuardAgainstRangeExtensions</c>).
/// </para>
/// <para>
/// Any downstream module that needs its own domain-specific guard (for
/// example a hypothetical multi-tenancy module adding
/// <c>Guard.Against.InvalidTenantId(...)</c>) should name its extension
/// class using a <c>{Module}GuardExtensions</c> pattern (for example
/// <c>MultiTenancyGuardExtensions</c>) and implement it as an extension
/// method on <see cref="IGuardClause"/>. This requires zero edits to
/// <c>Domain.Shared</c> — the whole point of the <see cref="IGuardClause"/>
/// extensibility anchor.
/// </para>
/// </remarks>
public sealed class Guard : IGuardClause
{
    private Guard()
    {
    }

    /// <summary>
    /// The single dispatch point every guard-clause extension method attaches
    /// to. Lazily constructed once and reused for every call site.
    /// </summary>
    public static IGuardClause Against { get; } = new Guard();

    /// <summary>
    /// Returns <paramref name="candidate"/> unchanged if it is a syntactically
    /// simple C# identifier (letters, digits, and underscores; not starting
    /// with a digit) — otherwise returns <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Runtime.CompilerServices.CallerArgumentExpressionAttribute"/>
    /// captures the raw *source text* of the argument expression, not a
    /// guaranteed identifier. When a call site passes a literal or inline
    /// expression instead of a named local/parameter, that source text — which
    /// may itself be the sensitive rejected value — would otherwise be echoed
    /// back inside the thrown exception's message/<c>ParamName</c>. Every
    /// guard in this kernel routes its captured parameter name through this
    /// method before using it in an exception, so this single check defends
    /// the whole family (Information-Disclosure mitigation, this phase's
    /// threat model T-1-02) without depending on 26 modules' worth of call
    /// sites to police it by convention.
    /// </remarks>
    internal static string? SafeParamName(string? candidate) =>
        candidate is { Length: > 0 }
        && (char.IsLetter(candidate[0]) || candidate[0] == '_')
        && candidate.All(static c => char.IsLetterOrDigit(c) || c == '_')
            ? candidate
            : null;
}
