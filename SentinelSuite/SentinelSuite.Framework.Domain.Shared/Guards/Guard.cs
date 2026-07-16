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
}
