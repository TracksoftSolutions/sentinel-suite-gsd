using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Guard clauses for comparable-range and enum-membership invariants — gives
/// downstream types (e.g. a future multi-tenancy isolation-tier enum, numeric
/// quantity ranges) a correct, generic way to guard both a comparable value
/// against an inclusive range and an enum value against its defined members.
/// </summary>
/// <remarks>
/// A SQL-Server-specific date-range guard (upstream's <c>OutOfSQLDateRange</c>)
/// is deliberately excluded from this file: it bakes a persistence-engine-
/// specific assumption into a persistence-agnostic Domain.Shared kernel,
/// violating Clean Architecture's dependency direction (only Infrastructure
/// may know a concrete database engine exists). This is a permanent
/// exclusion, not a placeholder for a future phase.
/// </remarks>
// A SQL-Server-specific date-range guard is deliberately excluded from this
// Domain.Shared kernel — see the class-level remarks above for the full
// Clean Architecture dependency-direction rationale (permanent, not deferred).
public static class GuardAgainstRangeExtensions
{
    /// <summary>
    /// Guards against an input outside the inclusive <paramref name="rangeFrom"/>-
    /// <paramref name="rangeTo"/> range, returning it unchanged when valid.
    /// </summary>
    /// <remarks>
    /// Throws <see cref="ArgumentException"/> (not
    /// <see cref="ArgumentOutOfRangeException"/>) when the range definition
    /// itself is invalid (<paramref name="rangeFrom"/> greater than
    /// <paramref name="rangeTo"/>) — this is distinct from an out-of-bounds
    /// input. Neither exception message interpolates the actual input value,
    /// only parameter names, per the Information-Disclosure mitigation in
    /// this phase's threat model (T-1-02).
    /// </remarks>
    public static T OutOfRange<T>(
        this IGuardClause guardClause,
        T input,
        T rangeFrom,
        T rangeTo,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : IComparable, IComparable<T>
    {
        if (rangeFrom.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentException(
                $"{nameof(rangeFrom)} should be less than or equal to {nameof(rangeTo)}.");
        }

        if (input.CompareTo(rangeFrom) < 0 || input.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Input {parameterName} was out of range.");
        }

        return input;
    }

    /// <summary>
    /// Guards against an enum value that is not defined on <typeparamref name="T"/>,
    /// returning it unchanged when valid.
    /// </summary>
    /// <remarks>
    /// <see cref="InvalidEnumArgumentException"/> is a BCL type
    /// (<c>System.ComponentModel</c>) — no extra package needed on net10.0.
    /// </remarks>
    public static T EnumOutOfRange<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), input))
        {
            throw new InvalidEnumArgumentException(parameterName, Convert.ToInt32(input), typeof(T));
        }

        return input;
    }
}
