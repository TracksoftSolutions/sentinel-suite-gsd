# Phase 1: Domain.Shared: GuardClauses - Pattern Map

**Mapped:** 2026-07-15
**Files analyzed:** 10 (7 source + 1 test-project scaffold + `global.json` + `.slnx` edit)
**Analogs found:** 0 / 10 (in-repo) — 10 / 10 (RESEARCH.md-sourced, from `ardalis/GuardClauses` upstream)

## Codebase Search Performed

Confirmed via direct filesystem search (`find ... -iname "*.cs"`) that **zero `.cs` source files exist anywhere in the solution**. The only files present are two empty `.csproj` scaffolds (`SentinelSuite.Framework.Domain.Shared.csproj`, `SentinelSuite.Framework.Domain.csproj`) and `SentinelSuite.slnx`. There is no test project in the solution yet. This phase is the first code written in the repository — there is no in-repo analog for any file below, by construction, not by search failure.

Per the orchestrator's explicit note for this phase: RESEARCH.md's Ardalis.GuardClauses-sourced code examples (confirmed against the actual `ardalis/GuardClauses` GitHub source this research session) are used as the pattern source of truth instead of a codebase analog. All excerpts below are traceable to `.planning/phases/01-domain-shared-guardclauses/01-RESEARCH.md`.

## File Classification

| New File | Role | Data Flow | Closest Analog | Match Quality |
|----------|------|-----------|-----------------|----------------|
| `SentinelSuite.Framework.Domain.Shared/Guards/IGuardClause.cs` | interface (extensibility marker) | N/A (no runtime data flow — pure type anchor) | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` | utility (static entry point) | request-response (call → return or throw) | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNullExtensions.cs` | utility (validation extension methods) | request-response | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstRangeExtensions.cs` | utility | request-response | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNumericExtensions.cs` | utility | request-response | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstInputExtensions.cs` | utility | request-response | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs` | utility | request-response | none in-repo | RESEARCH.md upstream pattern |
| `SentinelSuite.Framework.Domain.Shared.Tests/*.csproj` + `Guards/*Tests.cs` | test | request-response (arrange/act/assert) | none in-repo | RESEARCH.md upstream pattern (xUnit v3/MTP) |
| `SentinelSuite/global.json` | config | N/A | none in-repo | RESEARCH.md-provided (net-new scaffolding artifact) |
| `SentinelSuite.slnx` (modified) | config | N/A | existing file, additive edit only | exact (edit existing file, add one `<Project Path=.../>` line) |

## Pattern Assignments

### `Guards/IGuardClause.cs` (interface, extensibility marker)

**Analog:** none in-repo. **Source of truth:** `ardalis/GuardClauses` `Guard.cs`, as summarized in `01-RESEARCH.md` "Standard Stack" and "Architecture Patterns" sections.

**Pattern — zero-member marker interface:**
```csharp
namespace SentinelSuite.Framework.Domain.Shared.Guards;

/// <summary>
/// Marker interface used purely as an extensibility anchor for guard-clause
/// extension methods (Guard.Against.X()). This is intentionally a zero-member
/// "type class" style dispatch hook, NOT a domain-capability marker interface
/// of the kind docs/architecture-guidance.md warns against (see 01-RESEARCH.md
/// Pitfall 6) — there is no registry, no runtime capability query, and no
/// domain concept behind it. Any assembly/module may add its own
/// Guard.Against.X(...) method by writing an extension method on this
/// interface, with zero changes to Domain.Shared.
/// </summary>
public interface IGuardClause
{
}
```
Note: include the Pitfall-6 rationale comment verbatim (or equivalent) per D-06/architecture-guidance concerns — RESEARCH.md flags this explicitly so a future audit doesn't flag the empty interface as a smell.

---

### `Guards/Guard.cs` (static entry point)

**Analog:** none in-repo. **Source of truth:** `01-RESEARCH.md` "Architecture Patterns" diagram + `ardalis/GuardClauses` `Guard.cs`.

```csharp
namespace SentinelSuite.Framework.Domain.Shared.Guards;

public sealed class Guard : IGuardClause
{
    private Guard() { }

    /// <summary>
    /// Single entry point for all guard clauses: Guard.Against.Null(x), etc.
    /// Every actual guard is an extension method on IGuardClause — this class
    /// itself has no guard methods, only the singleton entry point.
    /// </summary>
    public static IGuardClause Against { get; } = new Guard();
}
```

---

### `Guards/GuardAgainstNullExtensions.cs` (Null, NullOrEmpty, NullOrWhiteSpace)

**Analog:** none in-repo. **Source of truth:** `01-RESEARCH.md` "Code Examples" → "Full guard method with two overloads (Null)" and "Pattern 2" (lines quoted in RESEARCH.md verbatim from `ardalis/GuardClauses GuardAgainstNullExtensions.cs`).

**Load-bearing shape — two overloads required for `Null<T>`** (RESEARCH.md Pattern 1 / Pitfall 2):
```csharp
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

public static class GuardAgainstNullExtensions
{
    public static T Null<T>(
        this IGuardClause guardClause,
        [NotNull] T? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : class
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return input;
    }

    public static T Null<T>(
        this IGuardClause guardClause,
        [NotNull] T? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct
    {
        if (input is null)
        {
            throw new ArgumentNullException(parameterName);
        }
        return input.Value;
    }

    public static string NullOrWhiteSpace(
        this IGuardClause guardClause,
        [NotNull] string? input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        Guard.Against.Null(input, parameterName);
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Required input was empty or whitespace.", parameterName);
        }
        return input;
    }

    // NullOrEmpty follows the same shape as NullOrWhiteSpace, using
    // string.IsNullOrEmpty and (for collections) a Count/Any check —
    // round out per D-09 mirroring Ardalis's actual overload set
    // (string + IEnumerable<T> variants).
}
```

**Critical correctness notes (do not deviate):**
- `[CallerArgumentExpression]` always targets the `input` parameter — never the `this IGuardClause guardClause` receiver (RESEARCH.md Pitfall 1).
- Do not add `[return: NotNull]` — RESEARCH.md Pitfall 2 confirms upstream does not use it and it triggers CS8825 on the reference-type overload; `[NotNull]` on the parameter + non-nullable return type `T` is sufficient and is the actual mechanism.

---

### `Guards/GuardAgainstRangeExtensions.cs` (OutOfRange, EnumOutOfRange)

**Analog:** none in-repo. **Source of truth:** `01-RESEARCH.md` "Code Examples" → "Range and enum guards" (quoted from `ardalis/GuardClauses GuardAgainstOutOfRangeExtensions.cs`).

```csharp
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SentinelSuite.Framework.Domain.Shared.Guards;

public static class GuardAgainstRangeExtensions
{
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
            throw new ArgumentException($"{nameof(rangeFrom)} should be less than or equal to {nameof(rangeTo)}");
        }
        if (input.CompareTo(rangeFrom) < 0 || input.CompareTo(rangeTo) > 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Input {parameterName} was out of range");
        }
        return input;
    }

    // System.ComponentModel.InvalidEnumArgumentException is a BCL type —
    // no extra package needed on net10.0.
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
```

**Explicitly exclude** `OutOfSQLDateRange` (RESEARCH.md Pitfall 3 — persistence-specific, violates Clean Architecture dependency direction for a Domain.Shared kernel).

---

### `Guards/GuardAgainstNumericExtensions.cs` (Negative, NegativeOrZero, Zero, Default<T>)

**Analog:** none in-repo. **Source of truth:** RESEARCH.md D-08 confirmed-floor list + "Sources" section naming `GuardAgainstZeroExtensions.cs` / `GuardAgainstNegativeExtensions.cs` as the upstream files this class consolidates.

**Pattern to follow** (same shape as `OutOfRange` above — `IComparable<T>`-constrained generic, `CallerArgumentExpression` on `input`, throws `ArgumentException`):
```csharp
namespace SentinelSuite.Framework.Domain.Shared.Guards;

public static class GuardAgainstNumericExtensions
{
    public static T Negative<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IComparable<T>
    {
        if (input.CompareTo(default) < 0)
        {
            throw new ArgumentException($"Required input {parameterName} cannot be negative.", parameterName);
        }
        return input;
    }

    // NegativeOrZero, Zero follow the same comparison-against-default(T) shape.

    public static T Default<T>(
        this IGuardClause guardClause,
        T input,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
        where T : struct, IEquatable<T>
    {
        if (input.Equals(default(T)))
        {
            throw new ArgumentException($"Required input {parameterName} cannot be the default value.", parameterName);
        }
        return input;
    }
}
```

---

### `Guards/GuardAgainstInputExtensions.cs` (InvalidInput — predicate escape hatch)

**Analog:** none in-repo. **Source of truth:** RESEARCH.md D-08's confirmed floor + "Sources" reference to `GuardAgainstExpressionExtensions.cs` upstream.

```csharp
namespace SentinelSuite.Framework.Domain.Shared.Guards;

public static class GuardAgainstInputExtensions
{
    public static T InvalidInput<T>(
        this IGuardClause guardClause,
        T input,
        Func<T, bool> predicate,
        [CallerArgumentExpression(nameof(input))] string? parameterName = null)
    {
        if (!predicate(input))
        {
            throw new ArgumentException($"Required input {parameterName} did not satisfy the required condition.", parameterName);
        }
        return input;
    }
}
```
Note: exception message must never interpolate the raw `input` value — only `parameterName` — per RESEARCH.md's "Security Domain" Information-Disclosure mitigation.

---

### `Guards/GuardAgainstStringExtensions.cs` (rounded-out — StringTooShort/TooLong, InvalidFormat)

**Analog:** none in-repo. **Source of truth:** RESEARCH.md "Sources" cites `GuardAgainstStringLengthExtensions.cs` and `GuardAgainstInvalidFormatExtensions.cs` upstream as the files to mirror for D-09's rounded-out list.

Follow the same shape as `NullOrWhiteSpace` above (guard against null first via `Guard.Against.Null`, then apply the length/regex check, throw `ArgumentException`). **Exclude `NotFound`** — throws a non-BCL `NotFoundException`, conflicts with D-04's BCL-only constraint this phase (RESEARCH.md Pitfall 3); flag as deferred to Phase 4 (`DomainException`) or a later application-layer phase, do not implement now.

---

### `SentinelSuite.Framework.Domain.Shared.Tests/*` (test project + test files)

**Analog:** none in-repo (no test project exists in the solution yet). **Source of truth:** `01-RESEARCH.md` "Code Examples" → hand-authored `.csproj` and "xUnit v3 pass/throw test pattern".

**`.csproj` pattern** (guaranteed-correct hand-authored fallback per RESEARCH.md Pitfall 5 — do not trust default `dotnet new xunit`, which emits VSTest, not MTP):
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
        <OutputType>Exe</OutputType>
        <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
        <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
    </PropertyGroup>
    <ItemGroup>
        <PackageReference Include="xunit.v3" Version="3.2.2" />
        <PackageReference Include="coverlet.mtp" Version="10.0.1" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include="..\SentinelSuite.Framework.Domain.Shared\SentinelSuite.Framework.Domain.Shared.csproj" />
    </ItemGroup>
</Project>
```
**Note:** use `coverlet.mtp`, NOT `coverlet.collector` (which CLAUDE.md's Testing Stack table recommends but RESEARCH.md Pitfall 4 confirms is incompatible with MTP-hosted projects — this supersedes CLAUDE.md for this one line item only).

**Test pattern (arrange/act/assert, pass + throw paths per guard method):**
```csharp
using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

public class GuardAgainstNullTests
{
    [Fact]
    public void Null_WhenValueProvided_ReturnsValue()
    {
        var input = "value";
        var result = Guard.Against.Null(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Null_WhenNullProvided_ThrowsArgumentNullExceptionWithCapturedParameterName()
    {
        string? input = null;
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.Null(input));
        Assert.Equal(nameof(input), ex.ParamName);
    }
}
```
Follow this pass/throw pair convention for every guard method — one `[Fact]` for the valid-input pass-through path, one (or more, for multiple invalid-input variants) for the throw path asserting both exception type and captured `parameterName`.

---

### `SentinelSuite/global.json` (config, net-new)

**Analog:** none — file does not exist yet anywhere in the solution. **Source of truth:** `01-RESEARCH.md` "Code Examples" → `global.json`.

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```
Place at `SentinelSuite/global.json` (next to `SentinelSuite.slnx`). Required for `dotnet test` to route through MTP on SDK 10+ (RESEARCH.md Environment Availability — confirmed missing via file search this session).

---

### `SentinelSuite.slnx` (modified, additive)

**Analog:** the file itself — exact match, edit in place.

**Current content** (read directly this session):
```xml
<Solution>
  <Project Path="SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj" />
  <Project Path="SentinelSuite.Framework.Domain/SentinelSuite.Framework.Domain.csproj" />
</Solution>
```

**Edit pattern:** add one line, following the exact existing `<Project Path="..."/>` element shape:
```xml
<Project Path="SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj" />
```

---

## Shared Patterns

### `CallerArgumentExpression` parameter-name capture
**Source:** `01-RESEARCH.md` Pattern 2 / Pitfall 1 (grounded in `ardalis/GuardClauses` source)
**Apply to:** every guard method in every `GuardAgainst*Extensions.cs` file.
```csharp
[CallerArgumentExpression(nameof(input))] string? parameterName = null
```
Always decorate the ordinary `input` parameter; never the `this IGuardClause guardClause` receiver.

### Nullable-analysis attributes
**Source:** `01-RESEARCH.md` Pitfall 2 (D-12 clarification)
**Apply to:** every guard method whose input can be null.
Use `[NotNull]` on the input parameter + a non-nullable declared return type (`T`). Do **not** add `[return: NotNull]` — it is redundant/superfluous here and triggers CS8825 on the `Null<T>` reference-type overload.

### BCL-exception-only convention (this phase)
**Source:** D-04, `01-CONTEXT.md`
**Apply to:** every guard method's throw path.
Only `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`, `System.ComponentModel.InvalidEnumArgumentException`. Never interpolate the raw invalid value into the exception message — only the captured `parameterName` (Information-Disclosure mitigation, RESEARCH.md Security Domain).

### File-per-concern static-class organization
**Source:** `01-RESEARCH.md` "Standard Stack" (10 files following `GuardAgainst{Concept}Extensions.cs` naming in upstream)
**Apply to:** all `Guards/*.cs` files — this is also the documented convention (D-06) future per-module guard extensions should follow: one static class of `IGuardClause` extension methods per module's own namespace, named `GuardAgainst{Concept}Extensions` (or `{Module}GuardExtensions` for a whole-module grouping).

## No Analog Found

All 10 files/edits in this phase have no in-repo analog — this is the first code in the repository. Every pattern assignment above is sourced from RESEARCH.md's direct fetch of upstream `ardalis/GuardClauses` source, not from codebase search, per this phase's explicit greenfield framing.

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| all 8 `Guards/*.cs` source files | utility/interface | request-response | No `.cs` files exist anywhere in the solution; this phase is Wave 0 of the entire codebase |
| `*.Tests` project + test files | test | request-response | No test project exists in the solution yet |

## Metadata

**Analog search scope:** entire `SentinelSuite/` directory tree (`find -iname "*.cs" -o -iname "*.csproj"`, excluding `obj`/`bin`), plus `SentinelSuite.slnx`
**Files scanned:** 3 (2 `.csproj` scaffolds, 1 `.slnx`) — no `.cs` files found
**Pattern extraction date:** 2026-07-15
**Pattern source:** `.planning/phases/01-domain-shared-guardclauses/01-RESEARCH.md` (itself sourced from direct fetch of `github.com/ardalis/GuardClauses` — see RESEARCH.md "Sources" section for full citation list)
</content>
