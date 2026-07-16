# Phase 3: Domain.Shared: SmartEnum<T> - Research

**Researched:** 2026-07-16
**Domain:** C# type-safe enumeration base class design (Ardalis.SmartEnum-equivalent, hand-rolled, reflection-based instance discovery)
**Confidence:** HIGH for Ardalis.SmartEnum mechanics (actual source fetched verbatim from `github.com/ardalis/SmartEnum` via `gh api` this session — direct primary-source read, not a summary), MEDIUM for general C# CRTP/BeforeFieldInit guidance (WebSearch cross-checked against named sources), LOW-tooling-tag/HIGH-epistemic for anything sourced via `gh api`/WebFetch per this project's provenance rules (see Assumptions Log A1)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Value type support**
- **D-01:** Build **both forms**: an int-backed `SmartEnum<TEnum>` and a generic-value `SmartEnum<TEnum, TValue>` (e.g. string-backed). Matches Ardalis.SmartEnum's actual shape and continues the "build broader now" pattern set in Phase 1 (D-07) and Phase 2 (D-07/D-12/D-13) — lets future modules pick string-backed status/type codes without needing another phase.

**Instance discovery mechanism**
- **D-02:** Instance discovery is **reflection-based auto-discovery** — reflects over public static readonly fields on the derived type to find all defined instances. Matches Ardalis.SmartEnum exactly; zero boilerplate at each derived enum's call site (just declare the static instances, lookup "just works").
- **D-03:** The reflection scan is **lazy, cached on first lookup** per derived type (not run eagerly in a static constructor). Matches Ardalis.SmartEnum's actual behavior — avoids static-constructor ordering/exception-semantics risk, cost paid once per type on first `FromValue`/`FromName`/`List` access.

**Lookup failure API & exception type**
- **D-04:** Ship **both** throwing (`FromValue`/`FromName`) and non-throwing Try-variants (`TryFromValue`/`TryFromName`, bool + out value). Build broader now — mirrors Result's philosophy (Phase 2) of separating expected-miss handling from exceptional-miss handling.
- **D-05:** `FromValue`/`FromName` throw a **dedicated `SmartEnumNotFoundException`** on a miss — a deliberate deviation from Phase 1/2's "BCL exception now, DomainException retrofit in Phase 4" precedent (D-04 in Phase 1, D-06 in Phase 2). User explicitly chose to introduce the dedicated type now rather than wait for Phase 4. **Flag to planner:** Phase 4 (DomainException) may need to reconcile/rebase this exception type once `DomainException` lands — note this as a known follow-up, not a conflict to resolve now.
- **D-06:** `SmartEnumNotFoundException` is a **single type** used by both `FromValue`-miss and `FromName`-miss (not two distinct exception types), carrying the attempted lookup value/name and the target SmartEnum type for a precise error message.

**Comparison & enumeration surface**
- **D-07:** SmartEnum implements **`IComparable<SmartEnum<TEnum>>`**, sorting by underlying `Value`. Matches Ardalis.SmartEnum exactly and continues the "build broader now" pattern — lets any derived enum be sorted/ordered out of the box (e.g. status pipelines, severity levels).
- **D-08:** The generic-value form's `TValue` is **constrained to `IComparable<TValue>`** — so both the int-backed and generic-value-backed forms (D-01) are sortable by their underlying value, giving one consistent contract across both forms rather than only the int-backed form being comparable.
- **D-09:** A **static `List` property** (or `GetAll()`-equivalent) is exposed on every derived SmartEnum type, enumerating all its defined instances. Matches Ardalis.SmartEnum; near-zero-cost addition since the reflection scan (D-02) already collects this data.

**Equality & representation**
- **D-10:** SmartEnum overloads **`==` and `!=`** operators, in addition to `Equals`/`GetHashCode`. Matches Ardalis.SmartEnum exactly — lets call sites write `status == MyEnum.Active` naturally.
- **D-11:** SmartEnum overrides **`ToString()`** to return the instance's `Name`. Matches Ardalis.SmartEnum; makes logging, string interpolation, and debugger display show the enum's label instead of the fully-qualified type name.

**Type shape**
- **D-12:** The self-referencing generic constraint (`TEnum : SmartEnum<TEnum>`) is **compiler-enforced**, matching Ardalis.SmartEnum and preventing mismatched generic usage at derived-type declaration.
- **D-13:** Derived SmartEnum types are **expected/documented to be sealed** — this phase establishes the sealed-by-default convention ahead of `Entity`'s formal rule in Phase 9 (per ROADMAP.md Phase 9 success criteria #3). Document this precedent inline (matching Phase 1's D-06 naming-convention-documented-in-code approach) so Phase 9 has an established pattern to point back to, not a new decision to make from scratch.

### Claude's Discretion
- Exact namespace — apply the established `Domain.Shared.{Concept}` sub-namespace convention (Phase 1 set `...Guards`, Phase 2 set `...Results`); this phase is expected to set `...SmartEnum` or `...Enumerations`. Confirm during planning.
- Internal reflection-caching implementation details (e.g., `ConcurrentDictionary<Type, ...>` vs `Lazy<T>` per type) — not discussed, left to planning. **Research finding: this question has a definitive answer — see Pattern 2 below. Ardalis.SmartEnum uses per-type static `Lazy<T>` fields, not a `ConcurrentDictionary<Type,...>`, and the reason is structural (C# generic static field semantics), not a style preference.**
- File/class organization within the SmartEnum sub-namespace (how many files, how the int-backed vs generic-value forms are split) — not discussed, left to planning, matching Phase 1/2's per-concern file split precedent.
- Exact wording/shape of `SmartEnumNotFoundException`'s message and properties — not discussed beyond D-06, left to planning.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope. All seven discussed areas (Value type support, Instance discovery mechanism, Lookup failure API & exception type, Comparison & enumeration surface, Equality operators, Sealed-by-default & generic constraint, ToString() override) resolved as in-scope decisions above, not deferred.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRIM-03 | `SmartEnum<T>` — hand-rolled type-safe enumeration base (Ardalis.SmartEnum-equivalent) | Full Ardalis.SmartEnum source mechanics (`SmartEnum<TEnum>`, `SmartEnum<TEnum,TValue>`, four-`Lazy<T>`-field caching, `GetAllOptions()` reflection scan, `FromValue`/`FromName`/`TryFromValue`/`TryFromName` signatures, `SmartEnumNotFoundException`, `IComparable`/operator/`ToString()` implementations) fetched verbatim via `gh api` from `github.com/ardalis/SmartEnum` (MIT-licensed, official repo) this session — gives the planner an exact, line-level reference shape to reproduce (not the package itself, per `PROJECT.md`). Package Legitimacy Audit confirms zero NuGet packages are introduced. |
</phase_requirements>

## Summary

Ardalis.SmartEnum's actual implementation — fetched verbatim this session directly from its official GitHub repository (`ardalis/SmartEnum`, MIT license, `src/SmartEnum/SmartEnum.cs` + supporting files) — maps almost exactly onto CONTEXT.md's thirteen locked decisions. `SmartEnum<TEnum>` is a thin wrapper (`SmartEnum<TEnum, int>`), and `SmartEnum<TEnum, TValue>` is the real generic implementation, constrained `where TEnum : SmartEnum<TEnum, TValue> where TValue : IEquatable<TValue>, IComparable<TValue>`. One concrete gap between the locked decisions and the reference shape is worth flagging up front: D-08 constrains `TValue` to `IComparable<TValue>` only, but Ardalis.SmartEnum's actual constraint is `IEquatable<TValue>, IComparable<TValue>` — the equality implementation (`_value.Equals(other._value)`) needs `IEquatable<TValue>` (or falls back to boxing `object.Equals`) to be correct and allocation-free. This is a genuine design question for the planner, not a nitpick — see Pitfall 1 and Open Question 1.

The single most load-bearing implementation detail for D-02/D-03 (lazy, cached, reflection-based discovery) is **how** Ardalis.SmartEnum caches: it does **not** use a `ConcurrentDictionary<Type, ...>` keyed cache. It uses four `static readonly Lazy<T>` fields declared directly on `SmartEnum<TEnum, TValue>` itself (`_enumOptions`, `_fromName`, `_fromNameIgnoreCase`, `_fromValue`), each with `LazyThreadSafetyMode.ExecutionAndPublication`. This works — and requires no `Type`-keyed dictionary at all — because C# generic static fields are per-closed-generic-type: `SmartEnum<StatusEnum, int>` and `SmartEnum<SeverityEnum, string>` are different closed generic types at the CLR level, each with its own independent copy of every static field. This is a structural fact about C# generics, not an implementation choice, and it directly answers one of CONTEXT.md's open "Claude's Discretion" questions (see Pattern 2).

The reflection scan itself (`GetAllOptions()`) is broader than "reflect over the derived type's own fields": it uses `Assembly.GetAssembly(typeof(TEnum)).GetTypes().Where(t => baseType.IsAssignableFrom(t)).SelectMany(t => t.GetFieldsOfType<TEnum>())` — i.e., it scans **every type in the assembly** where `TEnum` is defined, filters to types assignable to the base, and pulls public static fields whose declared type is assignable to `TEnum` from each. This is deliberately more permissive than "only fields declared directly on the sealed derived class" — it would also pick up static fields on a partial class split across files, or (in principle) fields declared on an intermediate abstract subclass. Reproduce this exact scan shape, not a narrower "only look at `typeof(TEnum).GetFields()`" version, since the assembly-wide-then-filter approach is what makes it work correctly for the general case.

**Primary recommendation:** Implement `SmartEnum<TEnum>`/`SmartEnum<TEnum, TValue>` following Ardalis.SmartEnum's actual class shape, generic constraints, and four-`Lazy<T>`-field caching pattern verbatim (Pattern 1/2 below), adapted only where CONTEXT.md's locked decisions differ (single exception type — matches; `IComparable<TValue>`-only constraint — flag as Open Question 1 rather than silently diverging from the reference's `IEquatable<TValue>` requirement).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| `SmartEnum<TEnum>` / `SmartEnum<TEnum, TValue>` base classes, reflection-based discovery, lookup APIs | Domain.Shared (shared kernel library) | — | Pure, side-effect-free, in-memory type with zero I/O/persistence/tenancy dependency — same category as Phase 1's `Guard` and Phase 2's `Result`. Every later tier (Domain entities defining status/type enums, future capability/audit enums) depends on this static surface. |
| `SmartEnumNotFoundException` | Domain.Shared (shared kernel library) | — | Co-located with the type it's thrown by, matching Phase 1/2's exception-lives-with-its-throwing-code precedent. |
| Test coverage of discovery/equality/comparison/lookup behavior | Test project (`SentinelSuite.Framework.Domain.Shared.Tests`, MTP-hosted) | — | Existing xUnit v3 test project from Phase 1; new `SmartEnum/` test subfolder mirrors the `Guards/` precedent. |

## Standard Stack

### Core

This phase introduces **zero production NuGet packages** by design (dependency-minimalism constraint, `PROJECT.md`). "Standard stack" here is the hand-rolled pattern being reproduced, not an installable package.

| Pattern reproduced | Source of truth | Purpose | Why this shape |
|---------------------|------------------|---------|-----------------|
| `SmartEnum<TEnum>` / `SmartEnum<TEnum,TValue>` class hierarchy, reflection discovery, `Lazy<T>`-based caching, lookup APIs, `IComparable`/operator/`ToString()` implementation | `Ardalis.SmartEnum` (current: source fetched directly from `main` branch via `gh api` this session), MIT license [CITED: github.com/ardalis/SmartEnum/blob/main/src/SmartEnum/{SmartEnum.cs,ThrowHelper.cs,TypeExtensions.cs,ISmartEnum.cs,Exceptions/SmartEnumNotFoundException.cs}] | Gives every derived enum in the kernel zero-boilerplate discovery, sorting, and safe/unsafe lookup for free, matching D-01 through D-13 almost line-for-line | This is the actual mechanism Ardalis.SmartEnum ships (not inference) — confirmed by direct source read this session, not a summary or search-result synthesis |

### Supporting

No additional NuGet packages. This phase reuses the existing `SentinelSuite.Framework.Domain.Shared.Tests` project (xUnit v3 `xunit.v3.mtp-v2` 3.2.2 / MTP, `coverlet.mtp` 10.0.1) established in Phase 1 — no new test-project scaffolding needed, only new test files under a `SmartEnum/` subfolder.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled `SmartEnum<TEnum>`/`SmartEnum<TEnum,TValue>` | Actual `Ardalis.SmartEnum` NuGet package | Explicitly excluded by `PROJECT.md`/CLAUDE.md constraint — every dependency lengthens FedRAMP authorization; the pattern itself (~150-250 lines once discovery + both value-type forms + Try-variants + IComparable + operators are included) is small enough to hand-roll per CLAUDE.md's own sizing estimate |
| `Lazy<T>` per closed generic type (Ardalis's actual approach) | `ConcurrentDictionary<Type, CachedData>` keyed cache | The `ConcurrentDictionary<Type,...>` approach is unnecessary extra complexity — C# generic static fields already give free per-type isolation; a `Type`-keyed dictionary would be solving a problem that doesn't exist in this design (see Pattern 2, Pitfall 2) |
| `where TValue : IComparable<TValue>` only (D-08) | `where TValue : IEquatable<TValue>, IComparable<TValue>` (Ardalis's actual constraint) | D-08 as locked is narrower than the reference; flagged as Open Question 1 — narrowing to `IComparable`-only changes how `Equals`/`GetHashCode`/the `_fromValue` dictionary must be implemented (falls back to `EqualityComparer<TValue>.Default` or `CompareTo(...) == 0` instead of a direct `IEquatable<TValue>.Equals` call) |

**Installation:** None — zero packages to install in `Domain.Shared`. Existing test project already references `xunit.v3.mtp-v2` + `coverlet.mtp` from Phase 1; no `dotnet add package` commands needed this phase.

**Version verification:** N/A — no packages installed. `Ardalis.SmartEnum` source was fetched directly from its GitHub `main` branch via `gh api repos/ardalis/SmartEnum/contents/...` this session (not from NuGet, since the package itself is never taken as a dependency — only its pattern is studied). Repo confirmed active: `pushed_at: 2026-04-29`, 2,430 stargazers, MIT license, default branch `main`.

## Package Legitimacy Audit

> This phase installs **zero production NuGet packages** (the entire point of PRIM-03, matching PRIM-01/PRIM-02's precedent). No new test-project packages are needed either — Phase 1 already established `xunit.v3.mtp-v2` + `coverlet.mtp` in `SentinelSuite.Framework.Domain.Shared.Tests`, and this phase adds only new test files, not new package references.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| *(none — zero packages installed this phase)* | — | — | — | — | — | N/A |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*No packages were introduced this phase, so no legitimacy check was required. `Ardalis.SmartEnum` is studied as a reference implementation only (source fetched directly from its official GitHub repository via `gh api`, MIT-licensed, 2,430 stars, actively maintained as of April 2026) — it is never added as a `PackageReference` anywhere in this project.*

## Architecture Patterns

### System Architecture Diagram

```
Derived enum declaration site (e.g. a future Domain-layer
status/type enum — not this phase's concern to define concrete
instances, only the base class)

    public sealed class MyStatus : SmartEnum<MyStatus>
    {
        public static readonly MyStatus Active = new(1, "Active");
        public static readonly MyStatus Inactive = new(2, "Inactive");
        private MyStatus(int value, string name) : base(name, value) { }
    }
        │
        │  first call to MyStatus.FromValue(...) / .FromName(...) / .List
        ▼
┌───────────────────────────────────────────────────────────────────┐
│  SmartEnum<TEnum, TValue>  (abstract, SmartEnum sub-namespace)      │
│                                                                       │
│   4x static readonly Lazy<T> fields — ONE SET PER CLOSED GENERIC     │
│   TYPE (i.e., per derived TEnum), populated on first access:         │
│                                                                       │
│     _enumOptions   = Lazy<List<TEnum>>(GetAllOptions)                │
│     _fromName       = Lazy<Dictionary<string,TEnum>>                 │
│     _fromNameIgnoreCase = Lazy<Dictionary<string,TEnum>> (ordinal-ic) │
│     _fromValue      = Lazy<Dictionary<TValue,TEnum>>                 │
│                                                                       │
│   GetAllOptions() — the ONE reflection scan, run once:                │
│     Assembly.GetAssembly(typeof(TEnum))                              │
│         .GetTypes()                                                  │
│         .Where(t => typeof(TEnum).IsAssignableFrom(t))               │
│         .SelectMany(t => t.GetFieldsOfType<TEnum>())                 │
│           // BindingFlags.Public | Static | FlattenHierarchy         │
│           // filters fields whose FieldType is assignable to TEnum   │
│         .OrderBy(x => x.Name)                                        │
│                                                                       │
│   Public surface built from the cached Lazy values:                  │
│     List (static)              → _enumOptions.Value                 │
│     FromName / TryFromName     → _fromName / _fromNameIgnoreCase     │
│     FromValue / TryFromValue   → _fromValue                          │
│     IComparable<T>.CompareTo   → Value.CompareTo(other.Value)        │
│     == / != operators          → delegate to Equals()                │
│     ToString()                 → returns Name                        │
└──────────────────────────┬────────────────────────────────────────┘
                            │  miss on FromName/FromValue
                            ▼
              throw SmartEnumNotFoundException
              ("No {TEnum.Name} with Name/Value {x} found.")
```

### Recommended Project Structure
```
SentinelSuite.Framework.Domain.Shared/
├── Guards/                                    # Phase 1 — unchanged
├── Results/                                   # Phase 2 (if landed) — unchanged
├── SmartEnum/
│   ├── ISmartEnum.cs                          # empty marker interface (Ardalis's actual shape —
│   │                                           #   lets non-generic code detect "is this a SmartEnum" without
│   │                                           #   knowing TEnum/TValue; near-zero cost to include)
│   ├── SmartEnum{TEnum}.cs                    # int-backed convenience wrapper: SmartEnum<TEnum> : SmartEnum<TEnum,int>
│   ├── SmartEnum{TEnum,TValue}.cs             # core generic implementation — discovery, caching, lookups,
│   │                                           #   IComparable, operators, ToString, Equals/GetHashCode
│   └── SmartEnumNotFoundException.cs          # single exception type, D-05/D-06
└── SentinelSuite.Framework.Domain.Shared.csproj

SentinelSuite.Framework.Domain.Shared.Tests/
├── Guards/                                    # Phase 1 — unchanged
├── SmartEnum/
│   ├── SmartEnumEqualityTests.cs              # Success criterion 2: two instances, same underlying value, are equal
│   ├── SmartEnumFromValueFromNameTests.cs     # Success criterion 3: FromValue/FromName succeed for valid input
│   ├── SmartEnumNotFoundTests.cs              # Success criterion 4: FromValue/FromName throw SmartEnumNotFoundException
│   │                                           #   for invalid input; TryFromValue/TryFromName return false, not throw
│   ├── SmartEnumComparableTests.cs            # IComparable<T> sorts by Value (D-07/D-08)
│   ├── SmartEnumListTests.cs                  # static List/GetAll enumerates every defined instance (D-09)
│   ├── SmartEnumOperatorTests.cs              # == / != operator overloads (D-10)
│   ├── SmartEnumToStringTests.cs              # ToString() returns Name (D-11)
│   └── SmartEnumGenericValueTests.cs          # string-backed SmartEnum<TEnum,string> fixture — exercises D-01's
│                                               #   second form end-to-end (discovery + lookup + IComparable<string>)
└── SentinelSuite.Framework.Domain.Shared.Tests.csproj
```

### Pattern 1: `SmartEnum<TEnum>` as a thin wrapper over `SmartEnum<TEnum, TValue>` (D-01)
**What:** The int-backed form is not a separate implementation — it's `SmartEnum<TEnum, int>` under the hood, with a one-constructor wrapper class that just forwards to the generic base.
**When to use:** Every call site that only needs int-backed enums (the common case) declares `: SmartEnum<TEnum>`; call sites needing a different backing type (string, Guid, etc.) declare `: SmartEnum<TEnum, TValue>` directly.
**Example:**
```csharp
// Source: verbatim shape from github.com/ardalis/SmartEnum src/SmartEnum/SmartEnum.cs
// [CITED: github.com/ardalis/SmartEnum] — fetched via `gh api` this session, direct source read
namespace SentinelSuite.Framework.Domain.Shared.SmartEnum;

public abstract class SmartEnum<TEnum> : SmartEnum<TEnum, int>
    where TEnum : SmartEnum<TEnum, int>
{
    protected SmartEnum(string name, int value) : base(name, value)
    {
    }
}
```
This single wrapper class is the entirety of D-01's "int-backed form" — all the real logic lives in `SmartEnum<TEnum, TValue>` below, so there is no duplication between the two forms.

### Pattern 2: Per-closed-generic-type `Lazy<T>` caching — the actual answer to CONTEXT.md's open caching-strategy question (D-02/D-03)
**What:** Four `static readonly Lazy<T>` fields declared directly on `SmartEnum<TEnum, TValue>`, each with `LazyThreadSafetyMode.ExecutionAndPublication`. Because C# generic types get one independent copy of every static field per closed generic instantiation, `SmartEnum<StatusEnum, int>`'s `_enumOptions` field and `SmartEnum<SeverityEnum, int>`'s `_enumOptions` field are two entirely separate storage locations at the CLR level — there is no cross-type collision risk and no need for a `Type`-keyed lookup structure.
**When to use:** This is the caching mechanism for the whole type — not a per-lookup decision.
**Example:**
```csharp
// Source: verbatim shape from github.com/ardalis/SmartEnum src/SmartEnum/SmartEnum.cs
// [CITED: github.com/ardalis/SmartEnum]
public abstract class SmartEnum<TEnum, TValue>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IComparable<TValue> // see Open Question 1 re: IEquatable<TValue>
{
    static readonly Lazy<List<TEnum>> _enumOptions =
        new(GetAllOptions, LazyThreadSafetyMode.ExecutionAndPublication);

    static readonly Lazy<Dictionary<string, TEnum>> _fromName =
        new(() => _enumOptions.Value.ToDictionary(item => item.Name));

    static readonly Lazy<Dictionary<string, TEnum>> _fromNameIgnoreCase =
        new(() => _enumOptions.Value.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase));

    static readonly Lazy<Dictionary<TValue, TEnum>> _fromValue =
        new(() =>
        {
            var dictionary = new Dictionary<TValue, TEnum>();
            foreach (var item in _enumOptions.Value)
            {
                if (item.Value is not null && !dictionary.ContainsKey(item.Value))
                    dictionary.Add(item.Value, item);
            }
            return dictionary;
        });

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
```csharp
// TypeExtensions.cs — the reflection helper GetAllOptions() calls
// Source: verbatim from github.com/ardalis/SmartEnum src/SmartEnum/TypeExtensions.cs
// [CITED: github.com/ardalis/SmartEnum]
internal static class TypeExtensions
{
    public static List<TFieldType> GetFieldsOfType<TFieldType>(this Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(p => type.IsAssignableFrom(p.FieldType))
            .Select(pi => (TFieldType)pi.GetValue(null)!)
            .ToList();
}
```
**Note on `_fromValue`'s "multiple instances, same value" behavior:** the reference implementation silently keeps only the **first** instance found for a given `Value` (`!dictionary.ContainsKey(...)` guard) when duplicate values exist across static fields. This is directly relevant to this phase's Success Criterion 2 ("two `SmartEnum<T>`-derived fixture instances with the same underlying value are equal") — equality is a separate mechanism from the `_fromValue` dictionary (see Pattern 4), so two same-valued instances remain independently `Equals()`-true even though only one of them will ever be returned by `FromValue`. Confirm this distinction is preserved: **equality by value** (any two instances with equal `Value` are `==`) is different from **canonical lookup instance** (only one instance is returned by `FromValue` when duplicates exist).

### Pattern 3: `FromValue`/`FromName`/`TryFromValue`/`TryFromName` signatures (D-04, D-05, D-06)
**What:** Exact method shapes to reproduce.
**When to use:** Every derived SmartEnum's lookup call sites.
**Example:**
```csharp
// Source: verbatim shape from github.com/ardalis/SmartEnum src/SmartEnum/SmartEnum.cs
// [CITED: github.com/ardalis/SmartEnum]
public static TEnum FromName(string name, bool ignoreCase = false)
{
    Guard.Against.NullOrWhiteSpace(name); // reuse Phase 1's Guard, replaces Ardalis's inline null check
    var dictionary = ignoreCase ? _fromNameIgnoreCase.Value : _fromName.Value;
    if (!dictionary.TryGetValue(name, out var result))
        throw new SmartEnumNotFoundException($"No {typeof(TEnum).Name} with Name \"{name}\" found.");
    return result;
}

public static bool TryFromName(string name, bool ignoreCase, out TEnum? result)
{
    if (string.IsNullOrEmpty(name)) { result = null; return false; }
    var dictionary = ignoreCase ? _fromNameIgnoreCase.Value : _fromName.Value;
    return dictionary.TryGetValue(name, out result);
}

public static TEnum FromValue(TValue value)
{
    if (!_fromValue.Value.TryGetValue(value, out var result))
        throw new SmartEnumNotFoundException($"No {typeof(TEnum).Name} with Value {value} found.");
    return result;
}

public static bool TryFromValue(TValue value, out TEnum? result) =>
    _fromValue.Value.TryGetValue(value, out result);
```
**Deliberate divergence from the reference:** Ardalis.SmartEnum also ships `FromValue(TValue value, TEnum defaultValue)` (returns a caller-supplied default instead of throwing). CONTEXT.md's D-04 only locks throwing + `Try*` forms — this default-value overload is **not** a locked decision. Do not add it silently; if desired, surface as a planning discretion call, since it's a third lookup-failure mode beyond D-04's explicit two ("both throwing... and non-throwing Try-variants").

### Pattern 4: `IComparable`, equality, operators, `ToString()` (D-07, D-08, D-10, D-11)
**What:** Comparison delegates to `Value.CompareTo`; equality checks reference-equality first, then `Value.Equals`; `==`/`!=` delegate to `Equals`; `ToString()` returns `Name`.
**Example:**
```csharp
// Source: verbatim shape from github.com/ardalis/SmartEnum src/SmartEnum/SmartEnum.cs
// [CITED: github.com/ardalis/SmartEnum]
public override string ToString() => Name;

public override int GetHashCode() => Value.GetHashCode();

public override bool Equals(object? obj) => obj is SmartEnum<TEnum, TValue> other && Equals(other);

public virtual bool Equals(SmartEnum<TEnum, TValue>? other)
{
    if (ReferenceEquals(this, other)) return true;
    if (other is null) return false;
    return Value.Equals(other.Value); // requires TValue : IEquatable<TValue> — see Open Question 1
}

public static bool operator ==(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right) =>
    left is null ? right is null : left.Equals(right);

public static bool operator !=(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right) =>
    !(left == right);

public virtual int CompareTo(SmartEnum<TEnum, TValue>? other) => Value.CompareTo(other!.Value);
```
**Note:** The reference implementation also provides `<`, `<=`, `>`, `>=` operators (trivial once `CompareTo` exists) — CONTEXT.md's D-10 only locks `==`/`!=`. Flag as Open Question 2: these are essentially free once `IComparable` is implemented and consistent with D-07's intent ("lets any derived enum be sorted/ordered"), but were not explicitly discussed.

### Anti-Patterns to Avoid
- **A `ConcurrentDictionary<Type, CachedData>` keyed cache "to be safe about thread-safety across derived types."** Unnecessary — see Pattern 2. C# generic static fields already provide per-derived-type isolation; a `Type`-keyed dictionary adds a dictionary lookup on every access for no benefit and risks its own thread-safety bugs (double-checked locking around dictionary population) that `Lazy<T>` already solves correctly.
- **Reflecting only over `typeof(TEnum).GetFields()` (the derived type's own declared fields) instead of the assembly-wide scan.** Narrower than the reference shape (see Pattern 2's `GetAllOptions()`); works for the simple case but silently breaks if a derived type's static instances are ever declared on a partial class split across files or an intermediate subclass — reproduce the assembly-scan-then-filter shape, not a narrower one.
- **Eager reflection in a static constructor.** Explicitly rejected by D-03; also loses the benefit of `Lazy<T>`'s exception-caching semantics (a `Lazy<T>` that throws during first evaluation will rethrow the *same* cached exception on subsequent access by default, whereas a static constructor that throws marks the *type* as permanently unusable for the process lifetime — `TypeInitializationException` on every subsequent access, even to unrelated members).
- **Two exception types (one for name-miss, one for value-miss).** D-06 explicitly locks a single `SmartEnumNotFoundException` for both — matches the reference exactly; don't split it.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Thread-safe lazy computation of a cache that must never run its expensive setup twice under concurrent first access | A hand-rolled double-checked-locking pattern around a nullable field | `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication` (BCL type, zero new dependency) | This is exactly the problem `Lazy<T>` was built to solve; a hand-rolled DCL implementation is a well-known source of subtle memory-visibility bugs in the absence of proper memory barriers |
| Assembly-wide type/field reflection scan with correct static-field filtering | Ad hoc `Type.GetFields()` call without the `BindingFlags.Public | Static | FlattenHierarchy` combination, or without the `IsAssignableFrom` type filter | The exact `GetAllOptions()`/`GetFieldsOfType<T>()` shape in Pattern 2 (adapted from the reference) | Getting `BindingFlags` wrong is a classic reflection footgun — e.g., omitting `FlattenHierarchy` silently drops inherited static fields, omitting `Static` silently returns nothing useful; the reference shape is already correct and tested against real-world usage |

**Key insight:** Unlike a purely novel design, this pattern has one authoritative, widely-used, and directly-inspectable reference implementation (2,430 GitHub stars, MIT license). The risk in this phase is not "the algorithm is hard" — it's "silently diverging from the proven shape in a way that looks correct but has a subtle bug" (e.g., wrong `BindingFlags`, missing `Lazy<T>` thread-safety mode, or a `TValue` constraint mismatch — see Open Question 1). Reproduce the reference shape closely; deviate only where CONTEXT.md's locked decisions explicitly require it.

## Runtime State Inventory

> N/A — this is a greenfield addition to `Domain.Shared` (new types in a new sub-namespace). No rename/refactor/migration involved. Skipping per this section's own scope note.

## Common Pitfalls

### Pitfall 1: D-08's `IComparable<TValue>`-only constraint is narrower than the reference's `IEquatable<TValue>, IComparable<TValue>` — equality implementation needs a decision
**What goes wrong:** The reference implementation's `Equals(SmartEnum<TEnum,TValue> other)` calls `Value.Equals(other.Value)`, which resolves to `IEquatable<TValue>.Equals` when `TValue : IEquatable<TValue>`. If this project's `TValue` is constrained only to `IComparable<TValue>` (per D-08 as literally written), that same `Value.Equals(other.Value)` call would resolve to `object.Equals` (virtual dispatch, boxing for value types, and — critically — **not guaranteed to agree with `CompareTo == 0`** for all types, though it does for the primitives/strings this kernel will realistically use).
**Why it happens:** D-08 was discussed specifically in terms of sortability ("both forms are sortable by their underlying value") — equality wasn't the explicit focus of that decision, so the constraint list may be an incomplete transcription of intent rather than a deliberate narrowing.
**How to avoid:** Two valid options, both worth surfacing to the planner rather than silently picking one: (a) implement `Equals` via `Value.CompareTo(other.Value) == 0` instead of `Value.Equals(...)`, staying strictly within D-08's literal `IComparable<TValue>`-only constraint at the cost of a values that are unequal-by-Equals-but-equal-by-CompareTo edge case being treated as SmartEnum-equal (rare for the types this kernel will actually use — `int`, `string`, `Guid` — but a real semantic difference for a hypothetical `TValue` where `CompareTo` and `Equals` disagree); or (b) constrain `TValue : IEquatable<TValue>, IComparable<TValue>` matching the reference exactly (asks the planner/CONTEXT-owner to confirm this is an acceptable, minor broadening of D-08 rather than a violation of it, since it only adds a well-known, ubiquitous interface most sensible value types already implement). Recommendation: option (b) — it's what the proven reference does, it doesn't materially restrict which types can back a `SmartEnum<TEnum,TValue>` in practice (every BCL primitive and `Guid`/`string` already implements both), and it avoids introducing a semantic edge case. Flag explicitly during planning rather than resolving unilaterally, since D-08 is a locked decision this deviates from in letter (not spirit).
**Warning signs:** A unit test constructing two fixture instances with `IComparable`-equal-but-`Equals`-unequal values behaving inconsistently between `==` and `CompareTo(...) == 0` — unlikely to surface with this kernel's realistic value types (int/string), but worth a one-line XML-doc note either way on whichever choice is made.

### Pitfall 2: Reaching for `ConcurrentDictionary<Type, ...>` because "reflection caching" pattern-matches to that shape from other codebases
**What goes wrong:** A very common, generically-correct caching pattern elsewhere in .NET is "keyed by `Type`, stored in a `ConcurrentDictionary`" (e.g., for caching compiled expression trees, serializer metadata, etc.). Applying that same instinct here is unnecessary and adds complexity with no benefit.
**Why it happens:** `SmartEnum<TEnum, TValue>` is itself a generic type, so it's tempting to think "the cache needs to be `Type`-aware" — but the generic-ness is exactly what makes a `Type`-keyed structure redundant here (see Pattern 2).
**How to avoid:** Use `static readonly Lazy<T>` fields declared directly on the generic base class, exactly as the reference does. Confirm this explicitly in the plan since CONTEXT.md flagged it as an open "Claude's Discretion" question — this research resolves it, it isn't actually open.
**Warning signs:** A design proposing a `private static readonly ConcurrentDictionary<Type, List<object>> _cache` or similar — this is solving a problem (cross-instantiation collision) that doesn't exist for generic static fields.

### Pitfall 3: `Lazy<T>` exception-caching semantics differ from a static constructor's — matters for the "predictable, catchable exception" success criterion
**What goes wrong:** If `GetAllOptions()` throws during the very first `.Value` access of `_enumOptions` (e.g., because a derived type declares zero static instances, or a reflection call fails for an unexpected reason), the default `Lazy<T>` behavior **re-throws the same cached exception on every subsequent access** to `.Value` for the lifetime of that `Lazy<T>` instance (default `LazyThreadSafetyMode` caches the exception, not just the successful result) — this is a different (and generally more forgiving) failure mode than a static constructor throwing, which poisons the *entire type* with `TypeInitializationException` for every member access, not just the one that triggered it.
**Why it happens:** `Lazy<T>`'s default re-throw behavior is a lesser-known BCL detail; most examples of `Lazy<T>` only show the success path.
**How to avoid:** This is actually a point in favor of the `Lazy<T>` approach over eager static-constructor-based discovery (matches D-03's stated rationale — "avoids static-constructor ordering/exception-semantics risk"), but worth an explicit test: declare a `SmartEnum`-derived fixture with a construction-time invariant violation (e.g., a duplicate `Name`, if that's an invariant this phase decides to enforce) and confirm accessing `.List`/`.FromValue` on it throws a specific, catchable, reproducible exception rather than corrupting later, unrelated `SmartEnum` types.
**Warning signs:** A test suite that never exercises "what happens when a badly-formed SmartEnum type's static fields are first reflected over" — Success Criterion 4 asks for "fail predictably (a specific, catchable exception) for invalid *inputs*" (lookup misses), but a badly-formed *type* (reflection-time failure) is a related, distinct failure mode worth at least one explicit test or an Open Question note if descoped.

### Pitfall 4: The self-referencing generic constraint does not prevent all misuse — a known, documented C# limitation
**What goes wrong:** `where TEnum : SmartEnum<TEnum, TValue>` reads like it should force "a derived type can only ever be its own generic parameter," but C# has no `where T : this` constraint (an unresolved, long-standing language proposal — `dotnet/csharplang#2495`, `dotnet/roslyn#11773`). It's still possible to write `public sealed class Foo : SmartEnum<Bar, int>` where `Bar` is a *different*, independently-valid `SmartEnum<Bar, int>` — this compiles, but `Foo.List` (inherited, not overridden) returns `IReadOnlyCollection<Bar>`, not `Foo`, and `Foo`'s own static instances (if declared) are simply invisible to the reflection scan (which looks for fields of type `Bar`, not `Foo`).
**Why it happens:** This is a documented, known limitation of the CRTP-style pattern in C# specifically (not a bug in this phase's implementation) — confirmed via WebSearch cross-referencing Phil Haacked's 2012 writeup and the still-open language proposal issues.
**How to avoid:** D-12 ("compiler-enforced... preventing mismatched generic usage") is correct as far as C#'s type system goes — the constraint *does* reject a completely unrelated `TEnum` (e.g., `class Foo : SmartEnum<string, int>` fails to compile since `string` doesn't satisfy `SmartEnum<string,int>`). What it does *not* reject is the "self-reference to a sibling type" case above. Document this precisely in the base class's XML remarks (matching D-13's inline-documentation precedent) — "always inherit as `SmartEnum<TSelf>` where `TSelf` is the declaring type itself; the compiler cannot fully enforce this" — so the sealed-by-default convention (D-13) plus this documented caveat together are the actual enforcement mechanism, not the generic constraint alone.
**Warning signs:** A code review or test that assumes "the compiler would catch it if someone got this wrong" — it wouldn't, for the sibling-substitution case. A unit test is not practical for a compile-time-shape issue like this; documentation is the correct mitigation.

## Code Examples

### `SmartEnumNotFoundException` (D-05, D-06)
```csharp
// Source: verbatim shape from github.com/ardalis/SmartEnum
// src/SmartEnum/Exceptions/SmartEnumNotFoundException.cs [CITED: github.com/ardalis/SmartEnum]
// Adapted: this project's Phase 1/2 precedent doesn't use [Serializable]/ISerializable suppression
// (no BinaryFormatter-era serialization concern in this codebase) — confirm during planning whether
// to keep or drop the [Serializable] attribute; the three-constructor shape (parameterless, message,
// message+innerException) is the load-bearing part to reproduce, matching standard .NET exception
// design guidance regardless of the [Serializable] question.
namespace SentinelSuite.Framework.Domain.Shared.SmartEnum;

public sealed class SmartEnumNotFoundException : Exception
{
    public SmartEnumNotFoundException()
    {
    }

    public SmartEnumNotFoundException(string message) : base(message)
    {
    }

    public SmartEnumNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```
**Note:** the reference declares this as a plain (non-sealed) `public class`. This project's Phase 1 (`Guard`) and Phase 2 (`Result`/`Result<T>`) precedent is sealed-by-default for kernel types — apply the same convention here unless there's a reason a downstream module would need to derive a more specific not-found exception from this one (unlikely; D-06 explicitly wants a single, precise type). Recommend `sealed`.

### Fixture-style derived enum, both value-type forms (D-01) — illustrates the shape planning/task-writing should target
```csharp
// Int-backed form (SmartEnum<TEnum>)
public sealed class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Pending = new(1, nameof(Pending));
    public static readonly OrderStatus Shipped = new(2, nameof(Shipped));

    private OrderStatus(int value, string name) : base(name, value)
    {
    }
}

// String-backed form (SmartEnum<TEnum, TValue>) — exercises D-01's second form
public sealed class TenantIsolationTier : SmartEnum<TenantIsolationTier, string>
{
    public static readonly TenantIsolationTier Shared = new("shared", nameof(Shared));
    public static readonly TenantIsolationTier DedicatedDb = new("dedicated_db", nameof(DedicatedDb));

    private TenantIsolationTier(string value, string name) : base(name, value)
    {
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| Bare C# `enum` for a fixed set of named values | Type-safe enumeration classes (SmartEnum-style) when the "enum" needs behavior, richer data, or extensibility beyond a bare integer | Long-standing DDD-community convention (Ardalis.SmartEnum itself dates to 2017, `pushed_at: 2026-04-29` — actively maintained 8+ years) | Directly matches `docs/architecture-guidance.md`'s own confirmation (per CONTEXT.md's canonical refs) that a type-safe enum with behavior is the correct tool where a bare C# `enum` wouldn't suffice — this phase is adopting an already-mainstream, long-stable .NET pattern, not inventing one |
| `ConcurrentDictionary<Type,...>` as the default answer to "cache something per-type" | Per-closed-generic-type `static Lazy<T>` fields, when the "something" is itself scoped by a generic type parameter | Not a recent change — this has always been how C# generics work; worth calling out because it's a frequently-missed optimization/simplification in reflection-caching code that defaults to `Type`-keyed structures out of habit | Directly simplifies this phase's implementation relative to a naive first instinct (see Pitfall 2) |

**Deprecated/outdated:**
- None identified specific to this phase's scope — `Ardalis.SmartEnum`'s source was fetched from its current default branch (`main`) this session; no legacy-vs-current API split found for the specific mechanics studied.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Ardalis.SmartEnum` source was fetched via `gh api repos/ardalis/SmartEnum/contents/...` (base64-decoded raw file content) this session rather than a first-party doc-lookup tool (no Context7/MCP doc tool was used for this fetch). The `gsd-tools classify-confidence` seam tags `webfetch`-provenance content as LOW regardless of the fact that the actual content retrieved is the literal, verbatim source file text from the official GitHub repository (not a summary, not a search-result synthesis) — the LOW tag reflects tooling-provenance classification, not epistemic uncertainty about the code shown. This mirrors Phase 2 RESEARCH.md's identical A1 entry for the same class of fetch. | Standard Stack, Architecture Patterns, Code Examples | Low — the fetched content is the actual source file text from `github.com/ardalis/SmartEnum`, MIT-licensed, 2,430-star, actively-maintained (pushed April 2026) repository — as close to ground truth as this domain gets short of running the library itself |
| A2 | The C# CRTP self-referencing-generic-constraint limitation (Pitfall 4) and the `BeforeFieldInit`/`Lazy<T>` exception-caching mechanics (Pitfall 3) are grounded in WebSearch results cross-referencing named sources (Phil Haacked's 2012 blog post, `dotnet/csharplang` and `dotnet/roslyn` GitHub issue trackers, csharpindepth.com) rather than a single canonical spec document fetched directly this session | Common Pitfalls (Pitfall 3, Pitfall 4) | Low-Medium — these are well-established, multiply-corroborated facts about C# generics and the BCL `Lazy<T>` type (not contested or version-dependent), but the specific citations were not fetched verbatim the way the Ardalis.SmartEnum source was — treat the underlying claims as reliable, the specific phrasing/citation as MEDIUM confidence |
| A3 | D-08's `IComparable<TValue>`-only constraint vs. the reference's `IEquatable<TValue>, IComparable<TValue>` constraint (Pitfall 1) is this research's identification of a gap, not something CONTEXT.md's discussion explicitly resolved — recommendation (b) in Pitfall 1 (broaden to match the reference) is this research's synthesis, not a locked decision | Summary, Pitfall 1, Open Questions | Medium — if planning silently picks option (a) instead without documenting the tradeoff, a future consumer relying on `Equals`/`==` behavior for a `TValue` where `Equals` and `CompareTo==0` disagree could get a surprising result; low practical risk given this kernel's realistic value types (int, string, Guid) but worth an explicit planning decision either way |

## Open Questions

1. **Should `TValue` be constrained to `IComparable<TValue>` only (D-08 literal) or `IEquatable<TValue>, IComparable<TValue>` (matching the reference exactly)?**
   - What we know: The reference implementation requires both; its `Equals` implementation depends on `IEquatable<TValue>`. D-08 as written in CONTEXT.md only mentions `IComparable<TValue>`.
   - What's unclear: Whether D-08's omission of `IEquatable<TValue>` was deliberate (a narrower, more permissive constraint) or an incomplete transcription of "sortable by value" intent that didn't separately consider equality.
   - Recommendation: Broaden to `IEquatable<TValue>, IComparable<TValue>` (Pitfall 1, option b) — matches the proven reference shape, doesn't meaningfully restrict realistic value types for this kernel, and avoids a semantic edge case. Confirm explicitly during planning since this is a literal (if minor) deviation from a locked decision's exact wording.

2. **Should the full `<`, `<=`, `>`, `>=` operator set be added alongside the locked `==`/`!=` (D-10)?**
   - What we know: The reference implements all six comparison/equality operators; D-10 only locks `==`/`!=`. `IComparable<T>` (D-07) is already required, so `<`/`<=`/`>`/`>=` are each a one-line `CompareTo(...) < 0`-style addition once `CompareTo` exists.
   - What's unclear: Whether CONTEXT.md's silence on `<`/`<=`/`>`/`>=` means "deliberately excluded" or "not discussed, default to matching the reference since IComparable is already in scope."
   - Recommendation: Include them — near-zero marginal implementation cost given `IComparable<T>` (D-07) is already locked in scope, consistent with this phase's repeated "build broader now" pattern elsewhere (D-01, D-04, D-09), and directly useful for the status-pipeline/severity-level use cases D-07's own rationale cites. Confirm during planning; low-risk to include, low-risk to omit if the planner prefers a stricter reading of D-10's literal scope.

3. **Should `FromValue(TValue value, TEnum defaultValue)` (a third lookup-failure mode beyond D-04's throw/Try pair) be included?**
   - What we know: The reference ships it; CONTEXT.md's D-04 explicitly frames the lookup surface as a binary choice ("both throwing... and non-throwing Try-variants").
   - What's unclear: Whether a default-value overload is a useful third option or unnecessary surface-area beyond what was discussed.
   - Recommendation: Omit for this phase — D-04's explicit two-mode framing reads as intentionally scoped, unlike D-01/D-07/D-09/D-10 where "build broader now" was the recurring theme; a default-value overload is easily added in a later phase if a concrete consumer need arises. Flag as descoped-not-forgotten if it comes up during implementation.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| .NET 10 SDK | Compiling `Domain.Shared` and the existing test project | Yes | 10.0.201 (confirmed via `dotnet --list-sdks`) | — |
| `SentinelSuite.Framework.Domain.Shared.Tests` project (xUnit v3 `xunit.v3.mtp-v2` / MTP, `coverlet.mtp`) | Test coverage for this phase | Yes (already exists from Phase 1, confirmed via file listing) | — | — |
| `global.json` (routes `dotnet test` through MTP) | Running the full test suite | Yes (already exists at `SentinelSuite/global.json`, confirmed) | — | — |
| `gh` CLI | Used this session to fetch Ardalis.SmartEnum source for research — not a runtime/build dependency of the phase itself | Yes (used successfully this session) | — | N/A — research-time tool only, no bearing on the implementation |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none — this phase's environment needs are a strict subset of what Phase 1 already established.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit v3 (`xunit.v3.mtp-v2` 3.2.2) on Microsoft.Testing.Platform — established in Phase 1, unchanged |
| Config file | `SentinelSuite/global.json` (exists from Phase 1) |
| Quick run command | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnum` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|--------------------|---------------|
| PRIM-03 | `SmartEnum<T>` compiles with zero third-party `PackageReference` entries | build/static check | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` + manual `.csproj` inspection | ❌ Wave 0 |
| PRIM-03 (Success Criterion 2) | Two `SmartEnum<T>`-derived fixture instances with the same underlying value are equal | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumEqualityTests` | ❌ Wave 0 |
| PRIM-03 (Success Criterion 3) | `FromValue`/`FromName` succeed for valid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumFromValueFromNameTests` | ❌ Wave 0 |
| PRIM-03 (Success Criterion 4) | `FromValue`/`FromName` fail predictably (specific, catchable exception) for invalid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumNotFoundTests` | ❌ Wave 0 |
| PRIM-03 (D-04) | `TryFromValue`/`TryFromName` return `false` (not throw) for invalid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumNotFoundTests` | ❌ Wave 0 |
| PRIM-03 (D-07/D-08) | `IComparable<T>` sorts by underlying `Value`, for both int-backed and generic-value-backed forms | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumComparableTests` | ❌ Wave 0 |
| PRIM-03 (D-09) | Static `List`/`GetAll()` enumerates every defined instance | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumListTests` | ❌ Wave 0 |
| PRIM-03 (D-10) | `==`/`!=` operator overloads behave consistently with `Equals` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumOperatorTests` | ❌ Wave 0 |
| PRIM-03 (D-11) | `ToString()` returns `Name` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumToStringTests` | ❌ Wave 0 |
| PRIM-03 (D-01, generic-value form) | String-backed `SmartEnum<TEnum,string>` fixture exercises discovery + lookup + comparison end-to-end | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumGenericValueTests` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnum`
- **Per wave merge:** `dotnet test` (full suite, includes Phase 1's `Guards` tests and any landed Phase 2 `Results` tests unaffected)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `SentinelSuite.Framework.Domain.Shared/SmartEnum/` — new sub-namespace, does not exist
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/` — new test subfolder, does not exist
- [ ] Framework install: none — reuses Phase 1's existing test project unchanged

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | No | This phase has no authentication surface |
| V3 Session Management | No | No session concept in a pure in-memory type-safe enumeration base |
| V4 Access Control | No | No access-control surface |
| V5 Input Validation | Partial | `FromName`/`TryFromName` should guard against null/empty `name` input (reuse Phase 1's `Guard.Against.NullOrWhiteSpace`, consistent with the constructor's own name-guard) — this is invariant validation on the type's own lookup API, not a general external-input-validation surface |
| V6 Cryptography | No | No cryptographic material handled |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| `SmartEnumNotFoundException`'s message interpolates the raw attempted lookup value/name directly (`$"No {typeof(TEnum).Name} with Value {value} found."`) — if a derived `SmartEnum<TEnum,string>` is ever looked up with caller-supplied (e.g., HTTP request) input and the exception message is surfaced to an external caller in a future API layer, the raw rejected input is echoed back | Information Disclosure | Same discipline as Phase 1's Guard-clause Information-Disclosure mitigation (T-1-02) and Phase 2's `Error.Message` guidance: this Domain.Shared-only phase has no external-facing boundary yet, so the raw-value-in-message shape (matching the reference implementation exactly) is acceptable *here*, but document in `SmartEnumNotFoundException`'s XML remarks that a future API/Application layer must not pass this exception's `.Message` directly to an external response without review — flag for whichever future phase adds an HTTP surface, mirroring Phase 2's identical flag for `Result.CriticalError`'s `Exception` property |
| Reflection-based discovery (`GetAllOptions()`) scans the **entire assembly** for public static fields assignable to `TEnum` — a large or unusual assembly layout (e.g., many unrelated `SmartEnum`-derived types across many files) is a performance/availability consideration, not a security one at this phase's scope, but worth noting since assembly-wide reflection scans have historically been a source of unexpected first-request latency spikes in ASP.NET-hosted applications that eagerly touch many types at startup | Denial of Service (theoretical, low-relevance here) | Out of scope for this Domain.Shared-only phase (no hosting/startup-path concern yet — no host exists per `PROJECT.md`'s explicit Module System scoping-down decision), but the lazy-on-first-access design (D-03) already mitigates the worst case (a startup-time stall) by design; flag for whichever future phase adds a host/bootstrapper that this cost is paid once per `SmartEnum`-derived type on its first real use, not eagerly at process start |

## Sources

### Primary (HIGH confidence — direct source-code fetch from the official repository)
- `github.com/ardalis/SmartEnum` (`main` branch, fetched via `gh api repos/ardalis/SmartEnum/contents/...`, base64-decoded, this session) — `src/SmartEnum/SmartEnum.cs` (full `SmartEnum<TEnum>` and `SmartEnum<TEnum,TValue>` source), `src/SmartEnum/ThrowHelper.cs`, `src/SmartEnum/TypeExtensions.cs`, `src/SmartEnum/ISmartEnum.cs`, `src/SmartEnum/Exceptions/SmartEnumNotFoundException.cs` — repo metadata confirmed via `gh api repos/ardalis/SmartEnum` (MIT license, 2,430 stargazers, `pushed_at: 2026-04-29`, default branch `main`)
- `gh api repos/ardalis/SmartEnum/git/trees/main?recursive=true` — full repository file tree, used to locate the correct source paths before fetching (the initial `raw.githubusercontent.com`/`WebFetch` attempt against a guessed path 404'd; the correct path `src/SmartEnum/*.cs`, not `src/Ardalis.SmartEnum/*.cs`, was confirmed via this tree listing)
- `dotnet --list-sdks` run directly against the local environment (10.0.201 confirmed)
- Direct `Read` of `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs`, `GuardAgainstNullExtensions.cs`, `SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs`, both `.csproj` files, `SentinelSuite/global.json` (this repo, Phase 1 precedent)
- `.planning/phases/02-domain-shared-result-result-t/02-RESEARCH.md` (this repo, Phase 2 precedent — read for RESEARCH.md structural/citation-convention precedent, including the A1-pattern for webfetch-provenance disclosure)

### Secondary (MEDIUM confidence — WebSearch cross-referencing named sources)
- `csharpindepth.com/articles/BeforeFieldInit` (Jon Skeet) — `BeforeFieldInit` semantics
- `learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors` — static constructor initialization timing guarantees
- `haacked.com/archive/2012/09/30/primitive-obsession-custom-string-types-and-self-referencing-generic-constraints.aspx` (Phil Haacked) — CRTP-style self-referencing generic constraint limitations in C#
- `github.com/dotnet/csharplang/issues/2495`, `github.com/dotnet/roslyn/issues/11773` — unresolved language proposals confirming C# still lacks a `where T : this` constraint

### Tertiary (LOW confidence — WebSearch synthesis, used only to locate/corroborate primary sources)
- General WebSearch result summaries used to identify candidate sources before cross-referencing named authors/official trackers above — not relied on for factual claims beyond pointing to primary/secondary sources

## Metadata

**Confidence breakdown:**
- Standard stack (Ardalis.SmartEnum class shape, caching mechanism, lookup API signatures, comparison/equality/operator implementation): HIGH — confirmed via direct, verbatim source-code fetch from the official `ardalis/SmartEnum` repository this session (via `gh api`, not a summarizing WebFetch)
- Architecture (per-closed-generic-type `Lazy<T>` caching resolving the open "Claude's Discretion" caching-strategy question): HIGH — this is a structural fact about C# generics confirmed by the reference implementation's actual code, not a design opinion
- Pitfalls: HIGH for Pitfall 1 (D-08 constraint gap — direct comparison against the fetched reference source), Pitfall 2 (ConcurrentDictionary anti-pattern — same grounding), MEDIUM for Pitfall 3 (`Lazy<T>` exception-caching semantics — general BCL knowledge, not independently fetched from a spec this session) and Pitfall 4 (CRTP limitation — WebSearch-corroborated against named sources, not a single canonical spec)

**Research date:** 2026-07-16
**Valid until:** 30 days — `Ardalis.SmartEnum` is a stable, slow-moving, mature MIT-licensed project (8+ years, actively maintained); re-verify only if this phase is delayed past that window or the upstream project's source has since restructured its file layout
