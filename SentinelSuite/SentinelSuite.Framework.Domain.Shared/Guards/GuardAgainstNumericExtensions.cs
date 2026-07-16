using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Guard clauses for numeric-sign and uninitialized-struct-default invariants —
/// gives downstream types (e.g. a future Entity identity <see cref="Guid"/>,
/// a quantity/count field) a consistent way to guard struct-shaped identity
/// and quantity arguments before they reach a constructor.
/// </summary>
public static class GuardAgainstNumericExtensions
{
    /// <summary>
    /// Guards against a negative input, returning it unchanged when valid.
    /// Zero is not considered negative and passes.
    /// </summary>
    /// <remarks>
    /// The exception message interpolates only <paramref name="parameterName"/>,
    /// never the actual input value, per the Information-Disclosure mitigation
    /// in this phase's threat model (T-1-02).
    /// </remarks>
    public static T Negative<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) < 0)
        {
            throw new ArgumentException(
                $"Required input {parameterName} cannot be negative.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Guards against a negative or zero input, returning it unchanged when valid.
    /// </summary>
    /// <remarks>
    /// The exception message interpolates only <paramref name="parameterName"/>,
    /// never the actual input value, per the Information-Disclosure mitigation
    /// in this phase's threat model (T-1-02).
    /// </remarks>
    public static T NegativeOrZero<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) <= 0)
        {
            throw new ArgumentException(
                $"Required input {parameterName} cannot be negative or zero.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Guards against a zero input, returning it unchanged when valid.
    /// </summary>
    /// <remarks>
    /// The exception message interpolates only <paramref name="parameterName"/>,
    /// never the actual input value, per the Information-Disclosure mitigation
    /// in this phase's threat model (T-1-02).
    /// </remarks>
    public static T Zero<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) == 0)
        {
            throw new ArgumentException(
                $"Required input {parameterName} cannot be zero.", parameterName);
        }

        return input;
    }

    /// <summary>
    /// Guards against an uninitialized/default-value struct (e.g. an unset
    /// <see cref="Guid"/> or <see cref="DateTime"/>), returning the input
    /// unchanged when valid. Deliberately generic over any
    /// <see cref="IEquatable{T}"/> struct rather than hand-written per
    /// concrete type.
    /// </summary>
    /// <remarks>
    /// The exception message interpolates only <paramref name="parameterName"/>,
    /// never the actual input value, per the Information-Disclosure mitigation
    /// in this phase's threat model (T-1-02).
    /// </remarks>
    public static T Default<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IEquatable<T>
    {
        if (input.Equals(default(T)))
        {
            throw new ArgumentException(
                $"Required input {parameterName} cannot be the default value.", parameterName);
        }

        return input;
    }
}
