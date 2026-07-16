using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Guard clauses for null, empty, and whitespace-only input — this kernel's
/// most load-bearing guard family, since every future <c>Entity</c>/
/// <c>EntityAssociation</c> constructor will call through here first.
/// </summary>
/// <remarks>
/// <c>Null&lt;T&gt;</c> requires two overloads (one <c>where T : class</c>,
/// one <c>where T : struct</c>) because a single generic method cannot
/// correctly express null-check semantics for both reference types and
/// <see cref="Nullable{T}"/> value types. Neither overload declares
/// <c>[return: NotNull]</c> — only <see cref="NotNullAttribute"/> on the
/// <c>input</c> parameter, combined with the non-nullable declared return
/// type <c>T</c>, is required; adding <c>[return: NotNull]</c> on the
/// reference-type overload triggers CS8825 since its return type is already
/// non-nullable.
/// </remarks>
public static class GuardAgainstNullExtensions
{
    /// <summary>
    /// Guards against a null reference-type input, returning it unchanged
    /// when valid.
    /// </summary>
    public static T Null<T>(
        this IGuardClause guardClause,
        [NotNull] T? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : class
    {
        if (input is null)
        {
            throw new ArgumentNullException(Guard.SafeParamName(parameterName));
        }

        return input;
    }

    /// <summary>
    /// Guards against a null nullable-value-type input, returning the
    /// unwrapped value when valid.
    /// </summary>
    public static T Null<T>(
        this IGuardClause guardClause,
        [NotNull] T? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct
    {
        if (input is null)
        {
            throw new ArgumentNullException(Guard.SafeParamName(parameterName));
        }

        return input.Value;
    }

    /// <summary>
    /// Guards against a null, empty, or whitespace-only string. Delegates to
    /// <see cref="Null{T}(IGuardClause, T, string?)"/> first so a null input
    /// throws <see cref="ArgumentNullException"/> rather than
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// The thrown message is a fixed string literal — never an interpolation
    /// of the rejected value — per the Information-Disclosure mitigation in
    /// this phase's threat model (T-1-02).
    /// </remarks>
    public static string NullOrWhiteSpace(
        this IGuardClause guardClause,
        [NotNull] string? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Required input was empty or whitespace.", Guard.SafeParamName(parameterName));
        }

        return input;
    }

    /// <summary>
    /// Guards against a null or empty (zero-length) string. Delegates to
    /// <see cref="Null{T}(IGuardClause, T, string?)"/> first so a null input
    /// throws <see cref="ArgumentNullException"/> rather than
    /// <see cref="ArgumentException"/>.
    /// </summary>
    public static string NullOrEmpty(
        this IGuardClause guardClause,
        [NotNull] string? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);

        if (input.Length == 0)
        {
            throw new ArgumentException("Required input was empty.", Guard.SafeParamName(parameterName));
        }

        return input;
    }

    /// <summary>
    /// Guards against a null or empty sequence. Delegates to
    /// <see cref="Null{T}(IGuardClause, T, string?)"/> first so a null input
    /// throws <see cref="ArgumentNullException"/> rather than
    /// <see cref="ArgumentException"/>.
    /// </summary>
    /// <remarks>
    /// Emptiness is checked via <see cref="ICollection{T}.Count"/> rather than
    /// <see cref="Enumerable.Any{T}(IEnumerable{T})"/>, and a non-collection
    /// <paramref name="input"/> is materialized to a <see cref="List{T}"/>
    /// before returning. Checking via <c>.Any()</c> alone would enumerate
    /// (and, for a forward-only/single-use sequence, partially consume) the
    /// caller's sequence before handing it back, leaving a reference the
    /// caller could no longer safely re-enumerate.
    /// </remarks>
    public static IEnumerable<T> NullOrEmpty<T>(
        this IGuardClause guardClause,
        [NotNull] IEnumerable<T>? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);

        var materialized = input as ICollection<T> ?? input.ToList();

        if (materialized.Count == 0)
        {
            throw new ArgumentException("Required input was empty.", Guard.SafeParamName(parameterName));
        }

        return materialized;
    }
}
