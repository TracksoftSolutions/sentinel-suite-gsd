namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Marker interface used purely as an extensibility anchor for guard-clause
/// extension methods (<c>Guard.Against.X(...)</c>). This is intentionally a
/// zero-member "type class" style dispatch hook — NOT the domain-capability
/// marker-interface pattern that <c>docs/architecture-guidance.md</c> warns
/// against ("A marker interface with no members is a smell").
///
/// That rule targets capability markers on concrete/tenant-defined domain
/// types (e.g. "this Activity IS mergeable"), where a runtime registry is the
/// authoritative source of truth and an interface risks becoming a second,
/// driftable source of truth. <see cref="IGuardClause"/> is a different
/// category entirely: it has no registry, no runtime capability query, and
/// no domain concept behind it at all. It exists solely so that any
/// assembly — this one or any of the platform's future modules — can attach
/// its own <c>Guard.Against.X(...)</c> extension method to this interface,
/// with zero changes to <c>Domain.Shared</c>.
/// </summary>
public interface IGuardClause
{
}
