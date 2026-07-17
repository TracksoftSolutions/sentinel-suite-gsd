using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// General-purpose predicate escape hatch for validation that doesn't warrant
/// its own named guard (D-08 confirmed floor).
/// </summary>
public static class GuardAgainstInputExtensions
{
    /// <summary>
    /// Guards against an input that does not satisfy <paramref name="predicate"/>,
    /// returning it unchanged when valid.
    /// </summary>
    /// <remarks>
    /// The thrown message is a fixed string referencing only
    /// <paramref name="parameterName"/> — never the rejected value itself —
    /// since <typeparamref name="T"/> is unconstrained and could carry
    /// sensitive call-site data (Information-Disclosure mitigation, this
    /// phase's threat model T-1-02).
    /// </remarks>
    public static T InvalidInput<T>(
        this IGuardClause guardClause,
        T input,
        Func<T, bool> predicate,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(predicate, nameof(predicate));

        if (!predicate(input))
        {
            var safeParamName = Guard.SafeParamName(parameterName);
            throw new ArgumentException(
                $"Required input {safeParamName} did not satisfy the required condition.",
                safeParamName);
        }

        return input;
    }
}
