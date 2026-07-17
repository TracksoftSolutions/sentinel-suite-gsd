# Phase 3: Domain.Shared: SmartEnum<T> - Pattern Map

**Mapped:** 2026-07-16
**Files analyzed:** 8 (4 production, ~8 test files collapsed to representative pattern)
**Analogs found:** 8 / 8 (all role-match; Phase 2 `Results/` not yet landed, so Phase 1 `Guards/` is the sole in-repo analog family)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|--------------------|------|-----------|-----------------|----------------|
| `SentinelSuite.Framework.Domain.Shared/SmartEnum/ISmartEnum.cs` | model (marker interface) | N/A | `Guards/IGuardClause.cs` | exact (both zero-member extensibility/marker interfaces) |
| `SentinelSuite.Framework.Domain.Shared/SmartEnum/SmartEnum{TEnum}.cs` | model (thin wrapper) | CRUD (lookup) | `Guards/Guard.cs` (sealed static-entry-point shape) + RESEARCH.md Pattern 1 (Ardalis reference, verbatim) | role-match (shape precedent from Guard.cs; exact implementation from RESEARCH.md) |
| `SentinelSuite.Framework.Domain.Shared/SmartEnum/SmartEnum{TEnum,TValue}.cs` | model (core generic base class) | CRUD (lookup) + transform (reflection scan) | RESEARCH.md Pattern 2/3/4 (Ardalis reference, verbatim) | exact (no in-repo generic-base analog exists; reference source is authoritative) |
| `SentinelSuite.Framework.Domain.Shared/SmartEnum/SmartEnumNotFoundException.cs` | model (exception type) | N/A | RESEARCH.md "Code Examples" section (Ardalis reference, adapted to `sealed`) | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/SmartEnumEqualityTests.cs` | test | CRUD (unit) | `Tests/Guards/GuardAgainstNullTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/SmartEnumFromValueFromNameTests.cs` | test | CRUD (unit) | `Tests/Guards/GuardAgainstNullTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/SmartEnumNotFoundTests.cs` | test | CRUD (unit) | `Tests/Guards/GuardAgainstStringTests.cs` (exception-throwing assertion pattern) | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/SmartEnum{Comparable,List,Operator,ToString,GenericValue}Tests.cs` | test | CRUD (unit) | `Tests/Guards/GuardAgainstNullTests.cs` | exact |

## Pattern Assignments

### `SentinelSuite.Framework.Domain.Shared/SmartEnum/ISmartEnum.cs` (model, marker interface)

**Analog:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/IGuardClause.cs` (full file, 23 lines)

**Pattern to copy:** zero-member marker interface used purely as an extensibility anchor, with an XML-doc `<remarks>` block explicitly distinguishing it from the architecture-guidance "marker interface smell" (which applies to tenant-defined capability markers, not framework-internal type-detection anchors).

```csharp
namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Marker interface used purely as an extensibility anchor ...
/// </summary>
public interface IGuardClause
{
}
```

Apply identically for `ISmartEnum`: namespace `SentinelSuite.Framework.Domain.Shared.SmartEnum`, XML-doc explaining it lets non-generic code detect "is this a SmartEnum" without knowing `TEnum`/`TValue` (per RESEARCH.md's Recommended Project Structure comment on `ISmartEnum.cs`).

---

### `SentinelSuite.Framework.Domain.Shared/SmartEnum/SmartEnum{TEnum}.cs` and `SmartEnum{TEnum,TValue}.cs` (model, core generic base classes)

**Analog (shape/sealed-static-entry-point precedent):** `Guards/Guard.cs` (full file, 65 lines) — establishes: single static-entry-point sealed class, private constructor, XML-doc `<remarks>` documenting a *naming/organizational convention this phase should also document inline* (Guard.cs documents the `GuardAgainst{Concept}Extensions` naming convention; SmartEnum should similarly document its sealed-by-default convention per D-13).

**Analog (exact implementation — no in-repo precedent, RESEARCH.md is authoritative):** RESEARCH.md Pattern 1 (`SmartEnum<TEnum>` thin wrapper, lines 195-212), Pattern 2 (`SmartEnum<TEnum,TValue>` four-`Lazy<T>`-field caching + `GetAllOptions()` reflection scan, lines 214-273), Pattern 3 (`FromName`/`FromValue`/`TryFromName`/`TryFromValue` signatures, lines 275-308), Pattern 4 (`IComparable`/equality/operators/`ToString()`, lines 310-337) — all sourced verbatim from `github.com/ardalis/SmartEnum` (MIT license).

**Core pattern — int-backed wrapper (RESEARCH.md Pattern 1):**
```csharp
namespace SentinelSuite.Framework.Domain.Shared.SmartEnum;

public abstract class SmartEnum<TEnum> : SmartEnum<TEnum, int>
    where TEnum : SmartEnum<TEnum, int>
{
    protected SmartEnum(string name, int value) : base(name, value)
    {
    }
}
```

**Core pattern — generic base, caching (RESEARCH.md Pattern 2):**
```csharp
public abstract class SmartEnum<TEnum, TValue>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue> // D-08 as amended
{
    static readonly Lazy<List<TEnum>> _enumOptions =
        new(GetAllOptions, LazyThreadSafetyMode.ExecutionAndPublication);
    static readonly Lazy<Dictionary<string, TEnum>> _fromName =
        new(() => _enumOptions.Value.ToDictionary(item => item.Name));
    static readonly Lazy<Dictionary<string, TEnum>> _fromNameIgnoreCase =
        new(() => _enumOptions.Value.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase));
    static readonly Lazy<Dictionary<TValue, TEnum>> _fromValue =
        new(() => { /* first-wins on duplicate Value, see RESEARCH.md Pattern 2 note */ });

    private static List<TEnum> GetAllOptions()
    {
        var baseType = typeof(TEnum);
        return Assembly.GetAssembly(baseType)!
            .GetTypes()
            .Where(t => baseType.IsAssignableFrom(t))
            .SelectMany(t => t.GetFieldsOfType<TEnum>())
            .OrderBy(t => t.Name)
            .ToList();
    }

    public static IReadOnlyCollection<TEnum> List => _enumOptions.Value;
}
```

**Lookup surface (RESEARCH.md Pattern 3):**
```csharp
public static TEnum FromName(string name, bool ignoreCase = false)
{
    Guard.Against.NullOrWhiteSpace(name); // reuse Phase 1's Guard — direct cross-file dependency
    var dictionary = ignoreCase ? _fromNameIgnoreCase.Value : _fromName.Value;
    if (!dictionary.TryGetValue(name, out var result))
        throw new SmartEnumNotFoundException($"No {typeof(TEnum).Name} with Name \"{name}\" found.");
    return result;
}

public static bool TryFromName(string name, bool ignoreCase, out TEnum? result) { /* ... */ }
public static TEnum FromValue(TValue value) { /* ... */ }
public static bool TryFromValue(TValue value, out TEnum? result) { /* ... */ }
```

**Comparison/equality/ToString (RESEARCH.md Pattern 4):**
```csharp
public override string ToString() => Name;
public override int GetHashCode() => Value.GetHashCode();
public override bool Equals(object? obj) => obj is SmartEnum<TEnum, TValue> other && Equals(other);
public virtual bool Equals(SmartEnum<TEnum, TValue>? other) { /* ReferenceEquals then Value.Equals */ }
public static bool operator ==(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right) { /* ... */ }
public static bool operator !=(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right) { /* ... */ }
public virtual int CompareTo(SmartEnum<TEnum, TValue>? other) => Value.CompareTo(other!.Value);
// Plus <, <=, >, >= per D-10 (amended) — one-line CompareTo-based additions
```

**Cross-file reuse note:** `FromName`/`TryFromName` should call `Guard.Against.NullOrWhiteSpace(name)` — a direct dependency on Phase 1's completed `Guard` class (already in the same assembly, `Guards/GuardAgainstNullExtensions.cs`). This is the one place SmartEnum's implementation should literally reuse Phase 1 code rather than just mirror its style.

---

### `SentinelSuite.Framework.Domain.Shared/SmartEnum/SmartEnumNotFoundException.cs` (model, exception type)

**Analog:** RESEARCH.md "Code Examples" section (`SmartEnumNotFoundException`, RESEARCH.md lines 386-413), adapted per this project's sealed-by-default convention (Phase 1 `Guard`, Phase 2 `Result`/`Result<T>` precedent — both sealed).

```csharp
namespace SentinelSuite.Framework.Domain.Shared.SmartEnum;

public sealed class SmartEnumNotFoundException : Exception
{
    public SmartEnumNotFoundException() { }
    public SmartEnumNotFoundException(string message) : base(message) { }
    public SmartEnumNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
```

**Deviation from reference:** Ardalis's actual type is non-sealed `public class`. Follow this project's own sealed-by-default precedent (`sealed`) per D-06's "single, precise type" intent and Phase 1/2 convention — not the reference's exact modifier.

**Security note (from RESEARCH.md Security Domain):** the exception message interpolates the raw rejected lookup value/name directly (matches the reference). Acceptable at this Domain.Shared-only phase (no external-facing boundary yet) — but XML-doc remarks should flag that a future API/Application layer must not surface `.Message` directly to external callers without review (same discipline as Phase 1's Guard `SafeParamName` mitigation and Phase 2's `Result.CriticalError` flag).

---

### Test files (`SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/*.cs`)

**Analog:** `SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs` (full file read, 60+ lines) and `GuardAgainstStringTests.cs` (for exception-assertion shape).

**Imports pattern:**
```csharp
using SentinelSuite.Framework.Domain.Shared.Guards; // → .SmartEnum for the new tests
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards; // → .Tests.SmartEnum
```

**Core test pattern — plain xUnit v3 `[Fact]`, `Given_When_Then`-in-method-name style, no test-base-class/fixture machinery:**
```csharp
public class GuardAgainstNullTests
{
    [Fact]
    public void Null_WhenReferenceTypeInputProvided_ReturnsSameInstanceUnchanged()
    {
        var input = "value";
        var result = Guard.Against.Null(input);
        Assert.Same(input, result);
    }

    [Fact]
    public void Null_WhenReferenceTypeInputIsNull_ThrowsArgumentNullExceptionWithCapturedParameterName()
    {
        string? input = null;
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.Null(input));
        Assert.Equal(nameof(input), ex.ParamName);
    }
}
```

**Exception-assertion pattern to reuse for `SmartEnumNotFoundTests.cs`:** `Assert.Throws<SmartEnumNotFoundException>(() => MyFixtureEnum.FromValue(999))` — mirrors `Assert.Throws<ArgumentNullException>(...)` shape above exactly. For Try-variant tests, assert `false` return + `null` out-param rather than a thrown exception (mirrors D-04's throw/Try split).

**Test naming convention:** `{MethodUnderTest}_When{Condition}_{ExpectedOutcome}` — apply verbatim to all new `SmartEnum*Tests.cs` files (`FromValue_WhenValueExists_ReturnsMatchingInstance`, `TryFromName_WhenNameDoesNotExist_ReturnsFalseAndNullOut`, etc.).

**Fixture requirement (new — no direct Guards analog since Guards has no "derived type" concept):** each test file will need at least one private/internal sealed `SmartEnum`-derived fixture type declared either at the top of the test file or in a shared fixtures file, per RESEARCH.md's "Fixture-style derived enum" code example (RESEARCH.md lines 415-438) — both an int-backed (`OrderStatus : SmartEnum<OrderStatus>`) and a string-backed (`TenantIsolationTier : SmartEnum<TenantIsolationTier, string>`) fixture are needed to cover D-01's both-forms requirement, especially for `SmartEnumGenericValueTests.cs`.

---

## Shared Patterns

### Sealed-by-default kernel types
**Source:** `Guards/Guard.cs` (sealed, private constructor, static entry point)
**Apply to:** `SmartEnumNotFoundException` (sealed, diverging from Ardalis reference); document inline that derived `SmartEnum<TEnum>`/`SmartEnum<TEnum,TValue>` types are *expected* to be sealed (D-13) even though the base classes themselves must be `abstract` (not sealed) to be inherited at all — this is a "convention documented in code," not a compiler-enforced rule, same pattern as Guard.cs's naming-convention `<remarks>` block.

### Namespace convention
**Source:** `Guards/*.cs` (`SentinelSuite.Framework.Domain.Shared.Guards`)
**Apply to:** All new SmartEnum files → `SentinelSuite.Framework.Domain.Shared.SmartEnum` namespace (confirmed by RESEARCH.md's Recommended Project Structure; no deviation needed).

### Guard-clause reuse for input validation
**Source:** `Guards/GuardAgainstNullExtensions.cs` — `Guard.Against.NullOrWhiteSpace(input, parameterName)`
**Apply to:** `SmartEnum<TEnum,TValue>.FromName`/`TryFromName` — call `Guard.Against.NullOrWhiteSpace(name)` at the top of `FromName` (throwing path only; `TryFromName` should do a plain `string.IsNullOrEmpty` check per RESEARCH.md Pattern 3, not throw).

### xUnit v3 test file/method conventions
**Source:** `Tests/Guards/GuardAgainstNullTests.cs`, `GuardAgainstStringTests.cs`
**Apply to:** All 8 new `SmartEnum*Tests.cs` files — plain `[Fact]` methods (no `[Theory]` seen in the analog, though `[Theory]`/`[InlineData]` is idiomatic xUnit and may be warranted for e.g. `SmartEnumComparableTests` sorting multiple pairs), `Given_When_Then`-style method names, `Assert.Same`/`Assert.Equal`/`Assert.Throws` from plain xUnit `Assert` (no Shouldly in this repo yet).

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| `SmartEnum{TEnum,TValue}.cs` (generic base with reflection-based lazy discovery) | model | transform (reflection scan) | No existing file in the repo implements reflection-based type discovery or `Lazy<T>`-cached static state; Guards/Result are simple stateless static dispatchers. RESEARCH.md's verbatim-sourced Ardalis.SmartEnum reference (Patterns 1-4) is the only available concrete template and should be treated as authoritative in place of an in-repo analog. |

## Metadata

**Analog search scope:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/` and `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/` (only existing production code in the solution at this phase — Phase 2 `Results/` has not yet landed).
**Files scanned:** 7 production files (`Guards/`), 5 test files (`Tests/Guards/`).
**Pattern extraction date:** 2026-07-16
