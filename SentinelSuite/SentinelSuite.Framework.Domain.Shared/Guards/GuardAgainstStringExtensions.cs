using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// String-shape guard clauses (length and format) commonly needed for
/// names/labels/codes — rounds out D-09's guard surface beyond the null/
/// whitespace family in <see cref="GuardAgainstNullExtensions"/>.
/// </summary>
/// <remarks>
/// A NotFound-style guard (upstream's <c>NotFound</c>) is deliberately
/// excluded from this file: Ardalis's equivalent throws a custom, non-BCL
/// exception type, which conflicts with this phase's D-04 BCL-exception-only
/// constraint. That guard is a candidate for Phase 4 (<c>DomainException</c>)
/// or a later application-layer phase, not a placeholder to implement now.
/// </remarks>
// NotFound-style guard deliberately excluded — see class-level remarks above
// for the D-04 BCL-exception-only rationale and Phase 4 forward-reference.
public static class GuardAgainstStringExtensions
{
    /// <summary>
    /// Guards against a string shorter than <paramref name="minLength"/>.
    /// Delegates to <see cref="GuardAgainstNullExtensions.Null{T}(IGuardClause, T, string?)"/>
    /// first so a null input throws <see cref="ArgumentNullException"/> rather
    /// than <see cref="ArgumentException"/>.
    /// </summary>
    public static string StringTooShort(
        this IGuardClause guardClause,
        string? input,
        int minLength,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);

        if (input.Length < minLength)
        {
            var safeParamName = Guard.SafeParamName(parameterName);
            throw new ArgumentException($"Input {safeParamName} is too short.", safeParamName);
        }

        return input;
    }

    /// <summary>
    /// Guards against a string longer than <paramref name="maxLength"/>.
    /// Delegates to <see cref="GuardAgainstNullExtensions.Null{T}(IGuardClause, T, string?)"/>
    /// first so a null input throws <see cref="ArgumentNullException"/> rather
    /// than <see cref="ArgumentException"/>.
    /// </summary>
    public static string StringTooLong(
        this IGuardClause guardClause,
        string? input,
        int maxLength,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);

        if (input.Length > maxLength)
        {
            var safeParamName = Guard.SafeParamName(parameterName);
            throw new ArgumentException($"Input {safeParamName} is too long.", safeParamName);
        }

        return input;
    }

    /// <summary>
    /// Guards against a string that does not match <paramref name="regexPattern"/>.
    /// Delegates to <see cref="GuardAgainstNullExtensions.Null{T}(IGuardClause, T, string?)"/>
    /// first so a null input throws <see cref="ArgumentNullException"/> rather
    /// than <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// None of the three methods in this file interpolate the rejected string
    /// value into their exception messages — only <paramref name="parameterName"/>
    /// — per the Information-Disclosure mitigation in this phase's threat
    /// model (T-1-02).
    /// </remarks>
    public static string InvalidFormat(
        this IGuardClause guardClause,
        string? input,
        string regexPattern,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);
        Guard.Against.NullOrWhiteSpace(regexPattern, nameof(regexPattern));

        if (!Regex.IsMatch(input, regexPattern))
        {
            var safeParamName = Guard.SafeParamName(parameterName);
            throw new ArgumentException($"Input {safeParamName} was not in the required format.", safeParamName);
        }

        return input;
    }
}
