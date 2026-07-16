# Phase 1: Domain.Shared: GuardClauses - Research

**Researched:** 2026-07-15
**Domain:** C# guard-clause validation library design (Ardalis.GuardClauses-equivalent, hand-rolled)
**Confidence:** HIGH (Ardalis.GuardClauses source mechanics), MEDIUM (xUnit v3/MTP tooling — no context7 available this session)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Fluent API matching Ardalis.GuardClauses exactly: a static `Guard` class exposes an `Against` entry point; individual guards (`Null`, `NullOrEmpty`, `OutOfRange`, etc.) are extension methods invoked as `Guard.Against.Null(x)`.
- **D-02:** Parameter names are auto-captured via `CallerArgumentExpression` (C# 10+, available on this stack's C# 14/.NET 10) — call sites do NOT need to pass `nameof(x)` explicitly.
- **D-03:** Every `Guard.Against.X(...)` method returns the validated value, enabling inline assignment (e.g., `_name = Guard.Against.NullOrEmpty(name)`), matching Ardalis.GuardClauses.
- **D-04:** Guard failures throw standard BCL exceptions this phase (`ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`) — matches Ardalis.GuardClauses' own convention. Deliberately provisional: Phase 4 (`DomainException`) retrofits at least one guard-failure path. Do not invent an interim domain exception type in this phase.
- **D-05:** `Guard.Against` is typed as an empty `IGuardClause` marker interface. Every guard method is implemented as an extension method on `IGuardClause`, not as a member of a closed class.
- **D-06:** A naming convention for future per-module guard-extension classes should be documented now (Claude's discretion — see below).
- **D-07:** Build a broader-than-minimum guard surface now, not just the exact methods implied by the roadmap's stated success criteria.
- **D-08:** Confirmed additions beyond the success-criteria minimum (`Null`, `NullOrEmpty`, `NullOrWhiteSpace`, `OutOfRange`, `EnumOutOfRange`): `Negative` / `NegativeOrZero` / `Zero` (numeric sign guards), `Default<T>` (guards against an uninitialized/default-value struct), `InvalidInput` (general-purpose predicate-based guard: `Guard.Against.InvalidInput(value, name, predicate)`).
- **D-09:** Claude should round out the rest of the method list with other commonly-used Ardalis.GuardClauses-equivalent guards during planning — the confirmed list above is a floor, not a ceiling.
- **D-10:** Namespace: `SentinelSuite.Framework.Domain.Shared.Guards` — a dedicated sub-namespace within `Domain.Shared`.
- **D-11:** The entry-point type is literally named `Guard` (call sites: `Guard.Against.X`), matching Ardalis.GuardClauses' actual public API exactly. "GuardClauses" remains the informal name for the phase/requirement (PRIM-01), not the literal type name.
- **D-12:** Guard methods carry C# nullable-analysis attributes (`[NotNull]` on parameters, `[return: NotNull]` on the return value) so the compiler treats a value as non-null immediately after a guard call. Matches Ardalis.GuardClauses' actual implementation (see Pitfall 2 below for an important correction to this claim).

### Claude's Discretion
- Exact naming convention for future per-module guard-extension classes (D-06) — e.g. `{Module}GuardExtensions`, one static class of `IGuardClause` extension methods per module's own namespace. Document whatever convention is chosen in code comments/docs so downstream phases have a precedent to follow.
- The full rounded-out guard method list beyond the confirmed floor in D-08 (D-09) — mirror Ardalis.GuardClauses' actual most-used methods; don't reinvent method names/shapes gratuitously.
- Exact `IGuardClause` extension-method implementation details (static class organization, file layout within the `Guards` sub-namespace) — not discussed, left to planning.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope. The extensibility mechanism and floor method list were both resolved as in-scope decisions, not deferred.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRIM-01 | `GuardClauses` — hand-rolled argument/invariant validation helpers (Ardalis.GuardClauses-equivalent), zero NuGet dependency | Full Ardalis.GuardClauses source mechanics documented below (API shape, `IGuardClause` extensibility, `CallerArgumentExpression` usage, nullable-attribute application, exact method/exception list) give the planner everything needed to spec a complete, correct hand-rolled implementation. Package Legitimacy Audit confirms no production NuGet packages are being introduced (Domain.Shared stays zero-dependency); test-project-only packages are audited separately. |
</phase_requirements>

## Summary

Ardalis.GuardClauses (current: v5.0.0, MIT license, verified on NuGet) is a small (~15 source files), well-established library whose entire public surface is reproducible in a single afternoon. Its actual mechanics — confirmed directly against the `ardalis/GuardClauses` GitHub source this session — are simpler than they might appear from the outside: `IGuardClause` is a literal zero-member marker interface; `Guard` is a sealed-by-convention class with a private constructor and one static property (`Against`) that returns a singleton `Guard` instance typed as `IGuardClause`; every actual guard (`Null`, `NullOrEmpty`, `OutOfRange`, etc.) is a `static` extension method on `IGuardClause`, one static class per concern, following the naming convention `GuardAgainst{Concept}Extensions`. Parameter-name capture uses `[CallerArgumentExpression("input")]` on the *validated value* parameter (never on the extension method's `this` receiver) — this distinction matters because `CallerArgumentExpression` has a well-documented gotcha when applied to a chained extension-method receiver, which does **not** apply here (see Pitfall 1).

Two implementation details in this phase's locked decisions need refinement during planning, both surfaced by reading Ardalis's actual source rather than relying on general C# knowledge: (1) `Null<T>` requires **two overloads** — one unconstrained-`T` (reference types) and one `where T : struct` (for `Nullable<T>`) — a single generic method cannot correctly express both null-check semantics; and (2) Ardalis's actual source does **not** use `[return: NotNull]` anywhere — it relies solely on `[NotNull]` applied to the input parameter plus a non-nullable declared return type (`T`, not `T?`). D-12's mention of `[return: NotNull]` is not wrong to include (it's valid, harmless, and gives IDE tooling one more nullable-analysis breadcrumb), but the planner should know it is not what makes Ardalis's actual pattern work, and is not required for correctness — see Pitfall 2 for the full mechanics.

Separately, and unrelated to the guard-clause design itself, this session surfaced a testing-infrastructure fact that updates guidance already recorded in this project's CLAUDE.md: `coverlet.collector` (recommended there) is **incompatible with Microsoft.Testing.Platform (MTP)** because it depends on the VSTest execution pipeline that MTP replaces. Since this phase is also the first to stand up a test project, the planner needs the corrected coverage-tooling recommendation (`coverlet.mtp` or `Microsoft.Testing.Extensions.CodeCoverage`) rather than the one in CLAUDE.md's Testing Stack table.

**Primary recommendation:** Reproduce Ardalis.GuardClauses' actual file-per-concern, extension-method-on-`IGuardClause` structure verbatim (renaming only the namespace per D-10), including the two-overload `Null<T>` pattern; skip `NotFound` and `OutOfSQLDateRange` from the rounded-out method list (both are out of place in a persistence-agnostic Domain.Shared kernel and/or require a non-BCL exception type this phase forbids); stand up the test project on `xunit.v3` + native MTP (not the SDK's built-in `dotnet new xunit` template, which is VSTest-based) with `coverlet.mtp` for coverage.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Argument/invariant validation helpers (`Guard.Against.*`) | Domain.Shared (shared kernel library, referenced by every other tier) | — | Guard clauses are a pure, side-effect-free static utility with zero dependencies on persistence, HTTP, or tenancy — the textbook definition of a shared-kernel primitive. Every later tier (Domain entities, UseCases validation, Infrastructure mapping code) calls into this same static surface; it does not itself belong to any one of them. |
| Test coverage of guard behavior (pass/throw paths) | Test project (`*.Tests`, MTP-hosted) | — | Standard xUnit v3 test project, referencing `Domain.Shared` only — no other project dependencies needed since Guard has no collaborators. |

## Standard Stack

### Core
This phase introduces **zero production NuGet packages** by design (dependency-minimalism constraint, `PROJECT.md`). "Standard stack" here means the hand-rolled pattern being reproduced, not an installable package.

| Pattern reproduced | Source of truth | Purpose | Why this shape |
|---------------------|------------------|---------|-----------------|
| `IGuardClause` marker interface + `Guard.Against` static entry point | `Ardalis.GuardClauses` v5.0.0 source (`Guard.cs`), MIT license [CITED: github.com/ardalis/GuardClauses/blob/main/src/GuardClauses/Guard.cs] | Extensibility anchor: any assembly can add `Guard.Against.X(...)` via an extension method on `IGuardClause` with zero changes to Domain.Shared | This is the actual mechanism (not merely "a" possible mechanism) that makes the real Ardalis library extensible — confirmed by direct source read, not inference |
| One static extension class per guard concern (`GuardAgainst{Concept}Extensions`) | Same source tree — 10 files follow this exact naming pattern (`GuardAgainstNullExtensions.cs`, `GuardAgainstZeroExtensions.cs`, `GuardAgainstOutOfRangeExtensions.cs`, etc.) [CITED: github.com/ardalis/GuardClauses/tree/main/src/GuardClauses] | File-per-concern organization, not one giant static class | Matches D-06's need for a documented convention future per-module guard extensions can follow |

### Supporting Libraries — Test project only

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `xunit.v3` | 3.2.2 (current stable; 4.0.0-pre previews exist, do not use) [VERIFIED: nuget.org registry — `api.nuget.org/v3-flatcontainer/xunit.v3/index.json`] | Test framework, MTP-native | This phase's test project |
| `xunit.v3.templates` (dotnet tool template package, not a project reference) | 3.2.2 current stable [CITED: xunit.net official docs — install command `dotnet new install xunit.v3.templates`; existence confirmed via NuGet registry] | Scaffolds a from-scratch xUnit v3 + MTP project | One-time local dev-machine install; **the SDK's built-in `dotnet new xunit` template does NOT produce this** — see Pitfall 5 |
| `coverlet.mtp` | 10.0.1 current [VERIFIED: nuget.org registry — package exists, replaces `coverlet.collector` for MTP-hosted projects] | Code coverage collector, MTP-native | Recommended over `coverlet.collector` (see Pitfall 4) |
| `Microsoft.Testing.Extensions.CodeCoverage` | 18.9.0 current [VERIFIED: nuget.org registry] | Alternative Microsoft-native coverage collector for MTP | Equally valid alternative to `coverlet.mtp`; slightly more setup (explicit `--coverage` CLI flag), fully Microsoft-supported |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled `Guard`/`IGuardClause` | Actual `Ardalis.GuardClauses` NuGet package | Explicitly excluded by `PROJECT.md`/CLAUDE.md constraint — every dependency lengthens FedRAMP authorization; the pattern is genuinely ~150-250 lines to reproduce |
| `coverlet.mtp` | `Microsoft.Testing.Extensions.CodeCoverage` | Either works with MTP; `coverlet.mtp` stays closer to CLAUDE.md's original coverlet-branded recommendation, `Microsoft.Testing.Extensions.CodeCoverage` is fully first-party Microsoft |
| SDK built-in `dotnet new xunit` template | `xunit.v3.templates` (community/xunit.net template package) | Built-in template still emits a VSTest-based (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`) v2-flavored project as of this SDK version — not what this phase needs |

**Installation (test project only):**
```bash
dotnet new install xunit.v3.templates   # one-time, local dev machine
# then from the SentinelSuite/ solution directory:
dotnet new xunit3 -n SentinelSuite.Framework.Domain.Shared.Tests -o SentinelSuite.Framework.Domain.Shared.Tests
dotnet add SentinelSuite.Framework.Domain.Shared.Tests package coverlet.mtp
dotnet add SentinelSuite.Framework.Domain.Shared.Tests reference SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj
```
*(Exact template short-name (`xunit3` vs `xunit.v3`) should be confirmed against the installed `xunit.v3.templates` package's `dotnet new list xunit` output at execution time — not independently confirmed this session since the template package was not installed in the research sandbox; installing it is a Wave 0 task, not a research gap that blocks planning.)*

**Version verification performed:** `xunit.v3`, `xunit.v3.templates`, `coverlet.mtp`, `Microsoft.Testing.Extensions.CodeCoverage`, and `Ardalis.GuardClauses` version lists were all fetched directly from the NuGet v3 flatcontainer API (`api.nuget.org/v3-flatcontainer/{id}/index.json`) this session — not taken from training data.

## Package Legitimacy Audit

> This phase deliberately installs **zero production NuGet packages** in `Domain.Shared` (the entire point of PRIM-01). The only packages installed at all are **test-project-only** and were verified directly against the NuGet registry (the project's `package-legitimacy check` tool only supports npm/PyPI/crates ecosystems — NuGet was audited manually below).

| Package | Registry | Age (first release) | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|----------------------|-----------|--------------|---------|-------------|
| `xunit.v3` | NuGet | 1.0.0 released well before current 3.2.2; long-established xUnit project | Very high (xUnit is one of the most-used .NET test frameworks) | github.com/xunit/xunit | OK | Approved (test project only) |
| `xunit.v3.templates` | NuGet | Same xUnit project, template-package variant | High (official xunit.net-documented install path) | github.com/xunit/xunit | OK | Approved (dev-machine tool install only, not a project dependency) |
| `coverlet.mtp` | NuGet | Part of long-established `coverlet-coverage/coverlet` project (10.x generation) | High (coverlet is the de facto standard .NET coverage tool) | github.com/coverlet-coverage/coverlet | OK | Approved (test project only) |
| `Microsoft.Testing.Extensions.CodeCoverage` | NuGet | First-party Microsoft package, versioned alongside VS/MTP tooling (18.x) | High (Microsoft-published) | N/A (Microsoft-internal, not a public single-repo OSS project in the same sense) | OK | Alternative to `coverlet.mtp` — not both |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*All four packages above were discovered via official documentation (xunit.net docs, coverlet's own MTP integration doc) rather than pure WebSearch/training-data guessing, and were independently confirmed to exist and hold the stated version on the NuGet registry via direct API call this session — this satisfies the `[VERIFIED]` bar for provenance, since discovery traces to an authoritative source AND registry existence was independently confirmed.*

## Architecture Patterns

### System Architecture Diagram

```
Any caller (Domain entity ctor, UseCases validator, future modules)
        │
        │  Guard.Against.Null(value)
        ▼
┌───────────────────────────────────────────────┐
│  Guard (static entry point)                    │
│    public static IGuardClause Against { get; } │  ← singleton, private ctor
└───────────────────┬─────────────────────────────┘
                     │  returns IGuardClause (marker, no members)
                     ▼
┌───────────────────────────────────────────────────────────┐
│  Extension methods on IGuardClause                          │
│  (one static class per concern, in Guards sub-namespace)     │
│                                                                │
│   GuardAgainstNullExtensions       .Null / .NullOrEmpty /     │
│                                     .NullOrWhiteSpace          │
│   GuardAgainstRangeExtensions      .OutOfRange / .EnumOutOfRange│
│   GuardAgainstNumericExtensions    .Negative / .NegativeOrZero │
│                                     / .Zero / .Default<T>       │
│   GuardAgainstInputExtensions      .InvalidInput (predicate)   │
│   GuardAgainstStringExtensions     .StringTooShort/TooLong/    │
│                                     .InvalidFormat (rounded out)│
└───────────────────┬────────────────────────────────────────┘
                     │  validation fails → throw BCL exception
                     │  (ArgumentNullException / ArgumentException /
                     │   ArgumentOutOfRangeException / InvalidEnumArgumentException)
                     │  validation passes → return input unchanged
                     ▼
              back to caller (value now compiler-known non-null
              at the call site, via [NotNull] parameter attribute)

Future module (e.g., Multi-Tenancy phase, Phase 8+)
        │  adds its own extension method on IGuardClause
        │  in its OWN namespace — zero edits to Domain.Shared
        ▼
   Guard.Against.InvalidTenantId(tenantId)   ← same entry point, new capability
```

### Recommended Project Structure
```
SentinelSuite.Framework.Domain.Shared/
├── Guards/
│   ├── IGuardClause.cs                       # marker interface, zero members
│   ├── Guard.cs                              # static Guard.Against entry point
│   ├── GuardAgainstNullExtensions.cs         # Null, NullOrEmpty, NullOrWhiteSpace
│   ├── GuardAgainstRangeExtensions.cs        # OutOfRange, EnumOutOfRange
│   ├── GuardAgainstNumericExtensions.cs      # Negative, NegativeOrZero, Zero, Default<T>
│   ├── GuardAgainstInputExtensions.cs        # InvalidInput (predicate escape hatch)
│   └── GuardAgainstStringExtensions.cs       # StringTooShort/TooLong, InvalidFormat (rounded out)
└── SentinelSuite.Framework.Domain.Shared.csproj

SentinelSuite.Framework.Domain.Shared.Tests/
├── Guards/
│   ├── GuardAgainstNullTests.cs
│   ├── GuardAgainstRangeTests.cs
│   ├── GuardAgainstNumericTests.cs
│   └── GuardAgainstInputTests.cs
└── SentinelSuite.Framework.Domain.Shared.Tests.csproj
```

### Pattern 1: The two-overload `Null<T>` shape (load-bearing — see Pitfall 2)
**What:** `Null<T>` must be implemented as *two* generic overloads: one unconstrained `T` for reference types, one `where T : struct` for `Nullable<T>` value types.
**When to use:** Any guard whose whole purpose is a null-check on a generic value (`Null`, and by extension anywhere `T?` appears on an unconstrained type parameter).
**Example:**
```csharp
// Source: pattern confirmed against ardalis/GuardClauses GuardAgainstNullExtensions.cs [CITED]
namespace SentinelSuite.Framework.Domain.Shared.Guards;

public static class GuardAgainstNullExtensions
{
    // Reference-type overload
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

    // Value-type overload (Nullable<T>)
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
}
```
*(Ardalis's actual source leaves the reference-type overload unconstrained — i.e. no `where T : class` — because unconstrained `T?` is nullable-annotated as "reference-or-null" by the compiler for unconstrained type parameters. Adding `where T : class` explicitly, as shown here, is a defensible simplification that makes overload resolution unambiguous and is arguably clearer; either shape compiles and behaves correctly. Flag this specific choice for the planner/discuss-phase, not a blocking issue.)*

### Pattern 2: `CallerArgumentExpression` applied to an ordinary argument, never the `this` receiver
**What:** `[CallerArgumentExpression(nameof(input))]` decorates the *second* parameter (`input`, the value being validated) — never the extension method's `this IGuardClause guardClause` parameter.
**When to use:** Every guard method in this phase.
**Example:**
```csharp
// Source: pattern confirmed against ardalis/GuardClauses GuardAgainstNullExtensions.cs [CITED]
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
```

### Anti-Patterns to Avoid
- **Applying `CallerArgumentExpression` to the extension method's `this` parameter to try to capture a chained receiver expression** — this is the one documented failure mode of `CallerArgumentExpression` + extension methods (see Pitfall 1). This phase's design never does this (CAE always targets `input`, an ordinary parameter), so it is not at risk, but it's worth planners understanding *why* it's safe here specifically.
- **A single unconstrained `Null<T>` overload trying to handle both reference and `Nullable<T>` cases** — produces either a compile error or silently-wrong null semantics for value types. Always split into two overloads (Pattern 1).
- **Adding a persistence-flavored guard (`OutOfSQLDateRange`) to a persistence-agnostic Domain.Shared kernel** — Ardalis's own library includes this guard because it's a general-purpose package; this project's Domain.Shared must not know SQL Server exists (Clean Architecture layering — Infrastructure is the only layer allowed to know about a specific database engine). Do not include it.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Whole guard-clause library as a single closed static class | A `GuardClauses` class with 15 hardcoded methods, no extension points | The `IGuardClause` marker-interface + extension-method pattern (D-05) | Closes off exactly the extensibility every future module (26 of them) needs; the marker-interface indirection is the entire value proposition, not incidental complexity |
| Parameter-name plumbing | Manual `nameof(x)` passed at every call site | `[CallerArgumentExpression]` (D-02) | Ardalis's own reason for the attribute's existence; hand-passing `nameof` at 100s of future call sites is exactly the kind of boilerplate this phase exists to eliminate |

**Key insight:** This phase is unusual among "don't hand-roll" guidance in that hand-rolling is precisely the point (per `PROJECT.md`'s dependency-minimalism constraint) — the risk is not "why hand-roll this," it's "hand-roll it *correctly*," which is why this research leans heavily on quoting Ardalis's actual source rather than reinventing the shape from first principles.

## Common Pitfalls

### Pitfall 1: `CallerArgumentExpression` + extension methods — narrow gotcha, does not apply here
**What goes wrong:** When `[CallerArgumentExpression("this")]` (or `nameof`) is applied to an extension method's implicit receiver (`this` parameter) to try to capture a full chained expression like `foo?.Bar.ThrowIfNull()`, the compiler only captures the last token in the chain (`.Bar`), not the full `foo?.Bar` [CITED: neosmart.net blog, cross-referenced against the general CAE mechanics documented at learn.microsoft.com/dotnet/csharp/language-reference/attributes/caller-information — third-party blog, not an official Microsoft source, treat the specific "only last token" claim as MEDIUM confidence, not verified against Roslyn source this session].
**Why it happens:** Extension-method call syntax is compiler sugar; the receiver expression gets desugared into a static-method-call argument, and the desugaring only preserves the final member-access segment when the attribute targets the receiver position specifically.
**How to avoid:** This phase's design is not exposed to this gotcha at all — `CallerArgumentExpression` is always applied to the *second, ordinary parameter* (`input`), never the `this IGuardClause guardClause` receiver. Capturing an ordinary explicit argument's source text works identically whether or not the enclosing method is an extension method; only receiver-position capture is affected. No design change needed — documenting this so a future contributor doesn't "fix" a non-bug.
**Warning signs:** If a future guard extension method ever tries to add `[CallerArgumentExpression]` to the `this` parameter (e.g., to auto-capture "the guard clause instance" — which would never make sense semantically anyway), that is the shape that breaks.

### Pitfall 2: `[return: NotNull]` is not what makes Ardalis's actual pattern work
**What goes wrong:** D-12 states guard methods should carry `[return: NotNull]` "matching Ardalis.GuardClauses' actual implementation." Direct inspection of the current source (`GuardAgainstNullExtensions.cs`, quoted verbatim this session) shows **no `[return: NotNull]` attribute anywhere** in the library. The mechanism Ardalis actually relies on is: `[NotNull]` on the *input parameter* (a postcondition attribute meaning "if this method returns without throwing, this parameter was not null") combined with a **non-nullable declared return type** (`T`, never `T?`) — the compiler's ordinary nullable-flow analysis handles the return value's non-null status from the declared type alone.
**Why it happens:** `[return: NotNull]` is designed for methods whose *declared* return type is nullable-oblivious or nullable-annotated but which are known to never actually return null (e.g., `string? GetOrThrow()` that always throws instead of returning null) — that's not this shape, since the guard methods' return type is already plain `T`.
**How to avoid:** Implement guard methods with `[NotNull]` on the input parameter and a non-nullable return type; adding `[return: NotNull]` on top is harmless and technically redundant, not incorrect — planner should not treat its absence as a defect, and should not block on reproducing it if it turns out awkward with the two-overload generic shape in Pattern 1.
**Warning signs:** Compiler warning CS8825 ("cannot apply \[return: NotNull\] to a member with a non-nullable return type") if `[return: NotNull]` is added on the reference-type `Null<T>` overload where the return type is already `T` (non-nullable) — this warning is the direct signal this attribute is superfluous in that position.

### Pitfall 3: Two of Ardalis's methods don't fit this project's constraints — exclude them from the rounded-out list
**What goes wrong:** `NotFound` throws a custom `NotFoundException` type (not a BCL exception) — directly conflicts with D-04's "BCL exceptions only this phase" constraint. `OutOfSQLDateRange` bakes a SQL-Server-specific assumption into a persistence-agnostic Domain.Shared kernel — conflicts with Clean Architecture's dependency-direction rule (only Infrastructure should know a concrete database engine exists).
**Why it happens:** Ardalis.GuardClauses is a general-purpose package built for many kinds of consumers, some of which are fine coupling guard clauses to persistence concerns; this project's Domain.Shared is deliberately narrower.
**How to avoid:** Exclude both from this phase's rounded-out method list (D-09). `NotFound`'s concept (validate a lookup-by-key result) is legitimately useful later, once `DomainException` (Phase 4) or a repository abstraction exists to give it a proper non-BCL exception target — flag as an Open Question / later-phase candidate, not this phase's scope.
**Warning signs:** A test or call site that needs to guard a database-shaped concept (a SQL date range, a "not found in the store" case) signals guard-clause scope creep back toward persistence — resist it in this phase.

### Pitfall 4: `coverlet.collector` (as recommended in this project's CLAUDE.md) does not work with Microsoft.Testing.Platform
**What goes wrong:** `coverlet.collector` and `coverlet.msbuild` both depend on the VSTest execution pipeline; MTP uses an entirely different test-execution architecture, so neither works when a project sets `UseMicrosoftTestingPlatformRunner=true` [CITED: xunit.net official docs, "Code Coverage with MTP" page, and coverlet's own `Coverlet.MTP.Integration.md` doc].
**Why it happens:** MTP is a from-scratch test-execution host (not an evolution of VSTest); tooling built against the old adapter model needs a native MTP port to keep working.
**How to avoid:** Use `coverlet.mtp` (native MTP port of coverlet, confirmed to exist on NuGet, v10.0.1 current) or `Microsoft.Testing.Extensions.CodeCoverage` (first-party Microsoft MTP extension) instead. This is a correction to CLAUDE.md's Testing Stack table, which still lists `coverlet.collector` — the planner should treat this research finding as superseding that specific line item (CLAUDE.md's broader guidance — xUnit v3, MTP-native from day one, no FluentAssertions 8+ — remains correct and unaffected).
**Warning signs:** `coverlet.collector` silently produces no coverage output (or errors about missing VSTest data collector) when the test project is MTP-configured.

### Pitfall 5: The SDK's built-in `dotnet new xunit` template is not xUnit v3 / MTP
**What goes wrong:** Running `dotnet new xunit` on a machine with only the base .NET 10 SDK installed (confirmed this session: `dotnet --list-sdks` shows `10.0.201`, no extra template packages installed) produces a VSTest-based project (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`), not an MTP-native `xunit.v3` project.
**Why it happens:** The SDK-bundled `xunit` template hasn't been updated to default to v3/MTP; the MTP-native template ships as a separate, community-maintained template package (`xunit.v3.templates`).
**How to avoid:** Explicitly `dotnet new install xunit.v3.templates` before scaffolding this phase's test project, or hand-author the `.csproj` (see Code Examples below) rather than trust the default `dotnet new xunit` output.
**Warning signs:** Generated test `.csproj` references `Microsoft.NET.Test.Sdk` and/or `xunit.runner.visualstudio` instead of a bare `xunit.v3` package reference with `UseMicrosoftTestingPlatformRunner`.

### Pitfall 6: `IGuardClause`'s empty-marker-interface shape superficially resembles a pattern `docs/architecture-guidance.md` warns against — it is not the same category
**What goes wrong:** `docs/architecture-guidance.md` states: "A marker interface with no members is a smell... declare it in the registration and let code query the registry, or give the interface real members." `IGuardClause` is literally an empty marker interface with no members. A reviewer applying that rule mechanically could flag D-05 as a violation.
**Why it happens:** The architecture-guidance rule is specifically about **domain capability markers** on concrete/tenant-defined types (e.g., "this Activity IS mergeable") where a registry is the authoritative source of truth and an interface risks becoming a second, driftable source of truth. `IGuardClause` is a different category entirely — it is a **dispatch/extensibility anchor** (a "type class" style hook purely for grouping extension methods under a common static-typed entry point), with no registry, no runtime capability query, and no domain concept behind it at all.
**How to avoid:** Document this distinction explicitly in code comments on `IGuardClause` (or in this phase's PLAN.md rationale) so a future `/gsd-plan-review-convergence` or architecture audit doesn't flag it. No design change needed.
**Warning signs:** A future code review citing `architecture-guidance.md`'s marker-interface rule against `IGuardClause` specifically — the rebuttal is the distinction above, not an exception to the rule.

## Code Examples

### Full guard method with two overloads (Null)
```csharp
// Source: pattern confirmed against ardalis/GuardClauses GuardAgainstNullExtensions.cs [CITED: github.com/ardalis/GuardClauses]
namespace SentinelSuite.Framework.Domain.Shared.Guards;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
}
```

### Range and enum guards
```csharp
// Source: pattern confirmed against ardalis/GuardClauses GuardAgainstOutOfRangeExtensions.cs [CITED]
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

// System.ComponentModel.InvalidEnumArgumentException is a BCL type — no extra package needed
// on net10.0 [CITED: well-established BCL type, present since .NET Framework 1.1]
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
```

### xUnit v3 test project (.csproj), hand-authored to guarantee MTP + no VSTest packages
```xml
<!-- Source: pattern confirmed against xunit.net official MTP docs [CITED: xunit.net/docs/getting-started/v3/microsoft-testing-platform] -->
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

### `global.json` — required at the solution root for `dotnet test` to route through MTP on SDK 10+
```json
// Source: [CITED: xunit.net/docs/getting-started/v3/microsoft-testing-platform]
// Place at: SentinelSuite/global.json (next to SentinelSuite.slnx) — does not currently exist, confirmed via file search this session
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

### xUnit v3 pass/throw test pattern
```csharp
// Standard xUnit v3 [Fact]/[Theory] pattern — no framework-version-specific gotchas found
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

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| VSTest-based test execution (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`, `coverlet.collector`) | Microsoft.Testing.Platform (MTP) native execution (`xunit.v3` + `coverlet.mtp`/`Microsoft.Testing.Extensions.CodeCoverage`) | MTP adopted across "all major .NET test frameworks" per Microsoft's own devblog as of this research window; xUnit v3 built on MTP directly | Test project scaffolding, coverage tooling, and `dotnet test`/`dotnet run` behavior all differ from pre-MTP guidance still found in many tutorials (and, notably, in this project's own CLAUDE.md coverage-tool line) |
| `nameof(x)` passed explicitly at every guard call site | `[CallerArgumentExpression]` auto-capture (C# 10+) | C# 10 / .NET 6 | Directly enables D-02's call-site ergonomics; already available on this project's C# 14/.NET 10 stack |

**Deprecated/outdated:**
- `coverlet.collector` for any project opted into MTP (`UseMicrosoftTestingPlatformRunner=true`) — architecturally incompatible, not merely "legacy but working."
- SDK built-in `dotnet new xunit` template as a source of truth for "current xUnit setup" — it still emits the pre-MTP shape.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | The exact `dotnet new` template short name for the MTP-native xUnit v3 project template (guessed as `xunit3`) was not independently confirmed by installing `xunit.v3.templates` in the research sandbox | Standard Stack, Installation | Low — planner/executor can simply run `dotnet new list xunit` after installing the template package to get the exact short name; does not block planning, only affects one scaffolding command |
| A2 | The "only captures the last token" specifics of the `CallerArgumentExpression` + extension-method-receiver gotcha come from a third-party blog (neosmart.net), not an official Microsoft source, though cross-referenced against the general CAE mechanism documented at learn.microsoft.com | Common Pitfalls, Pitfall 1 | Low — this gotcha does not affect this phase's actual design (CAE is never applied to the `this` receiver here), so even if the precise failure mode differs slightly from the blog's description, the mitigation ("never apply CAE to the receiver") is unaffected |
| A3 | `Ardalis.GuardClauses`' reference-type `Null<T>` overload is unconstrained (no `where T : class`) per the fetched source excerpt, while this research's Code Example adds `where T : class` for clarity — both compile and behave correctly, but they are not textually identical to upstream | Architecture Patterns, Pattern 1 | Low — either shape is valid; flagged so the planner/discuss-phase can pick one deliberately rather than by accident |

**If this table is empty:** N/A — see entries above.

## Open Questions

1. **Should `NotFound` land in a later phase once `DomainException` exists?**
   - What we know: Ardalis's `NotFound` guard is a commonly-used method that throws a custom `NotFoundException`, which conflicts with this phase's BCL-only exception constraint (D-04).
   - What's unclear: Whether a future phase (Phase 4 `DomainException`, or a later repository/persistence-adjacent phase) is the right home for a `NotFound`-style guard, or whether it belongs in a UseCases/Application layer instead (arguably a lookup-by-key failure is more of an application-level "not found" than a constructor-argument guard).
   - Recommendation: Exclude from this phase entirely; leave a one-line note in the `Guards` namespace (or this phase's PLAN.md) flagging it as a deliberately-deferred method, so it doesn't get silently forgotten or reinvented ad hoc later.

2. **Exact `xunit.v3.templates` template short name and generated project shape**
   - What we know: The package exists on NuGet (v3.2.2), and xunit.net's official docs describe the MTP setup properties (`UseMicrosoftTestingPlatformRunner`, `TestingPlatformDotnetTestSupport`).
   - What's unclear: The precise `dotnet new <short-name>` invocation, since the template package wasn't installed in the research sandbox this session.
   - Recommendation: Treat as a Wave 0 task — install the template package and run `dotnet new list xunit` (or hand-author the `.csproj` per the Code Examples section, which is guaranteed correct regardless of template naming).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| .NET 10 SDK | Compiling Domain.Shared and the test project | ✓ | 10.0.201 (confirmed via `dotnet --list-sdks`) | — |
| `xunit.v3.templates` (dotnet new template package) | Scaffolding the MTP-native test project via template | ✗ (not installed in this environment as of research time) | — | Hand-author the `.csproj` directly per the Code Examples section (guaranteed-correct fallback, no functional loss) |
| `SentinelSuite.slnx` solution file | Adding the new test project to the solution | ✓ (exists, currently references only the two existing empty scaffolds) | — | — |
| `global.json` (solution root) | `dotnet test` routing through MTP on SDK 10+ | ✗ (does not exist yet — confirmed via file search) | — | None needed as a fallback — this file must simply be created as part of this phase's Wave 0 |

**Missing dependencies with no fallback:**
- `global.json` must be created (not really a "dependency," but a required scaffolding artifact — listed here since its absence would silently misroute `dotnet test`).

**Missing dependencies with fallback:**
- `xunit.v3.templates` — fallback is hand-authoring the `.csproj`, which this research already provides verbatim.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit v3 (`xunit.v3` 3.2.2) on Microsoft.Testing.Platform |
| Config file | none yet — `SentinelSuite/global.json` must be created this phase (Wave 0) |
| Quick run command | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests` |
| Full suite command | `dotnet test` (once `global.json` routes it through MTP) |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|--------------------|---------------|
| PRIM-01 | `Guard` class compiles in `Domain.Shared` with zero third-party `PackageReference` entries | build/static check | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` + manual/grep check of `.csproj` for absence of `<PackageReference>` | ❌ Wave 0 (project has no source yet) |
| PRIM-01 | Null-argument guard: pass path (valid input returns value) and throw path (`ArgumentNullException`, correct captured parameter name) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter GuardAgainstNullTests` | ❌ Wave 0 |
| PRIM-01 | Empty/range/enum-membership guard: pass and throw paths (`ArgumentException`/`ArgumentOutOfRangeException`/`InvalidEnumArgumentException`) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter "GuardAgainstRangeTests|GuardAgainstNullTests"` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests`
- **Per wave merge:** `dotnet test` (full suite, once `global.json` is in place)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `SentinelSuite/global.json` — routes `dotnet test` through MTP on SDK 10+; does not exist
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj` — new MTP-native test project; does not exist
- [ ] `SentinelSuite.slnx` — needs the new test project added as a third `<Project Path=.../>` entry
- [ ] Framework install: `dotnet new install xunit.v3.templates` (optional — Code Examples section provides a hand-authored `.csproj` fallback that does not require this)

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | No | This phase has no authentication surface |
| V3 Session Management | No | No session concept in a static validation utility |
| V4 Access Control | No | No access-control surface |
| V5 Input Validation | Yes | This phase *is* input-validation infrastructure — guard clauses are the mechanism, not a consumer of one. The standard control (throwing on invalid input at the boundary, fail-fast, no silent coercion) is the entire design goal of `Guard.Against.*` |
| V6 Cryptography | No | No cryptographic material handled |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| Invalid/malformed constructor arguments silently accepted, corrupting downstream invariants (e.g., a negative quantity, an out-of-range enum cast from external input) | Tampering | Fail-fast guard clauses at every constructor/method boundary — exactly what this phase builds. No additional mitigation needed beyond consistent adoption in later phases. |
| Exception messages leaking sensitive parameter values (e.g., a guard's `message` parameter echoing a secret back in an `ArgumentException.Message`) | Information Disclosure | Guard exceptions in this design only ever echo the *parameter name* (via `CallerArgumentExpression`) and a static message string — never the actual invalid value — matching Ardalis's own convention. Confirm during implementation that no guard method interpolates the raw `input` value into an exception message. |

## Sources

### Primary (HIGH confidence — direct source-code fetch from the official repository)
- `github.com/ardalis/GuardClauses` — `Guard.cs`, `GuardAgainstNullExtensions.cs`, `GuardAgainstOutOfRangeExtensions.cs`, `GuardAgainstZeroExtensions.cs`, `GuardAgainstNegativeExtensions.cs`, `GuardAgainstExpressionExtensions.cs`, `GuardAgainstEmptyOrWhiteSpaceExtensions.cs`, `GuardAgainstStringLengthExtensions.cs`, `GuardAgainstNotFoundExtensions.cs`, `GuardAgainstInvalidFormatExtensions.cs`, `ValidatedNotNullAttribute.cs` — fetched and quoted directly this session
- `api.nuget.org/v3-flatcontainer/{package}/index.json` — direct registry queries for `Ardalis.GuardClauses`, `xunit.v3`, `xunit.v3.templates`, `coverlet.collector`, `coverlet.mtp`, `Microsoft.Testing.Extensions.CodeCoverage`
- Direct `Read` of `SentinelSuite.Framework.Domain.Shared.csproj` and `SentinelSuite.slnx` (this repo)
- `dotnet --list-sdks`, `dotnet new xunit --help` run directly against the local environment

### Secondary (MEDIUM confidence — official documentation, fetched via WebFetch)
- xunit.net official docs: `xunit.net/docs/getting-started/v3/microsoft-testing-platform`, `xunit.net/docs/getting-started/v3/code-coverage-with-mtp`
- Microsoft Learn: `learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/nullable-analysis`, `learn.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information`, `learn.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.notnullattribute`
- `coverlet-coverage/coverlet` repository's `Documentation/Coverlet.MTP.Integration.md`

### Tertiary (LOW confidence — WebSearch synthesis and third-party blog content, flagged for validation)
- `neosmart.net/blog/callerargumentexpression-and-extension-methods-dont-mix/` — third-party blog, not official Microsoft documentation; specific claim isolated and cross-referenced in Pitfall 1 / Assumption A2
- General WebSearch result summaries used to identify candidate URLs before direct fetch (not relied on for factual claims beyond pointing to primary sources)

## Metadata

**Confidence breakdown:**
- Standard stack (guard-clause pattern mechanics): HIGH — confirmed via direct source-code fetch from the official `ardalis/GuardClauses` repository, cross-checked against multiple files
- Standard stack (test tooling versions): MEDIUM-HIGH — versions confirmed via direct NuGet registry API calls; setup mechanics confirmed via official xunit.net docs, but no context7/MCP doc-lookup tool was available this session, so template-invocation specifics (Open Question 2) remain unconfirmed
- Architecture: HIGH — the `IGuardClause`/`Guard`/extension-method shape is quoted verbatim from source, not inferred
- Pitfalls: HIGH for Pitfalls 2-6 (each grounded in a direct source fetch or official doc); MEDIUM for Pitfall 1 (grounded in a third-party blog, flagged as such)

**Research date:** 2026-07-15
**Valid until:** 30 days for the Ardalis.GuardClauses pattern mechanics (stable, slow-moving library); 14 days for the xUnit v3/MTP tooling recommendations (actively evolving ecosystem as of this research window — re-verify package versions before executing if this phase is delayed)
