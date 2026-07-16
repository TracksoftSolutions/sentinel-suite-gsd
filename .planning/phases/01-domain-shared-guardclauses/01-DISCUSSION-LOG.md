# Phase 1: Domain.Shared: GuardClauses - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-15
**Phase:** 1-Domain.Shared: GuardClauses
**Areas discussed:** API shape, Extensibility mechanism, Method scope for this phase, Naming & namespace, Nullable-analysis attributes

---

## API shape

| Option | Description | Selected |
|--------|-------------|----------|
| Fluent: `Guard.Against.Null(x, nameof(x))` | Matches Ardalis.GuardClauses exactly — static `Guard` class, `Against` entry point, guards as extension methods on `IGuardClause`. | ✓ |
| Plain static: `GuardClauses.NotNull(x, nameof(x))` | Simpler, one static class, no marker-interface indirection. | |

**User's choice:** Fluent `Guard.Against.Null(x, nameof(x))`

| Option | Description | Selected |
|--------|-------------|----------|
| CallerArgumentExpression (auto-capture) | Callers write `Guard.Against.Null(x)` — no `nameof` boilerplate. | ✓ |
| Explicit `nameof(x)` required | Caller always passes `nameof(x)` explicitly. | |

**User's choice:** CallerArgumentExpression (auto-capture)

| Option | Description | Selected |
|--------|-------------|----------|
| Return the validated value | Enables `_field = Guard.Against.Null(value)` inline assignment. | ✓ |
| Return void | Guard call is a separate statement. | |

**User's choice:** Return the validated value

| Option | Description | Selected |
|--------|-------------|----------|
| Standard BCL exceptions (`ArgumentNullException`, etc.) | Matches Ardalis.GuardClauses; Phase 4 explicitly retrofits to `DomainException` later. | ✓ (via "You decide") |
| You decide | Let Claude confirm the only sensible option. | ✓ |

**User's choice:** "You decide" — Claude confirmed standard BCL exceptions, consistent with ROADMAP.md's Phase 4 retrofit language.
**Notes:** Not inventing an interim domain exception type; Phase 4 owns the migration to `DomainException`.

---

## Extensibility mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| `IGuardClause` marker + extension methods | Every guard is an extension method on an empty `IGuardClause` marker interface — the actual Ardalis.GuardClauses mechanism. Future modules add guards with zero changes to Domain.Shared. | ✓ |
| Closed static class, no extension hook | All guard methods defined directly in one static class. | |

**User's choice:** `IGuardClause` marker + extension methods

| Option | Description | Selected |
|--------|-------------|----------|
| Anywhere, per-module convention (no rule yet) | Decide later, when first needed. | |
| Document a convention now | Decide the convention in this phase's context. | ✓ |

**User's choice:** Document a convention now

| Option | Description | Selected |
|--------|-------------|----------|
| `{Module}GuardExtensions` in the module's own namespace | e.g. `TenancyGuardExtensions`, mirroring Ardalis.GuardClauses' own community-extension naming. | |
| You decide | Let Claude pick a sensible, documented convention during planning. | ✓ |

**User's choice:** "You decide"
**Notes:** Convention will be documented in code comments/docs during planning, not locked to a specific name here.

---

## Method scope for this phase

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal set matching success criteria only | Null, NullOrEmpty/NullOrWhiteSpace, OutOfRange, EnumOutOfRange only. | |
| Broader upfront set | Also include Negative/NegativeOrZero, Default, InvalidInput, Zero, etc. | ✓ |

**User's choice:** Broader upfront set

| Option | Description | Selected |
|--------|-------------|----------|
| Negative / NegativeOrZero / Zero | Numeric sign guards. | ✓ |
| Default (`Guard.Against.Default<T>`) | Guards against an uninitialized struct/value. | ✓ |
| InvalidInput (predicate-based) | General-purpose `Guard.Against.InvalidInput(value, name, predicate)`. | ✓ |
| You decide the full list | Claude selects a broader set mirroring Ardalis.GuardClauses' most-used methods. | ✓ |

**User's choice:** All four selected (multi-select) — the three named guard families are confirmed as a floor, and Claude should round out the remaining list further during planning.

---

## Naming & namespace

| Option | Description | Selected |
|--------|-------------|----------|
| `SentinelSuite.Framework.Domain.Shared.Guards` | Dedicated sub-namespace, matching the likely convention for sibling primitives (Result, SmartEnum). | ✓ |
| `SentinelSuite.Framework.Domain.Shared` (root, no sub-namespace) | One less using statement, flatter root namespace over time. | |
| You decide | Let Claude pick, consistent with sibling phases. | |

**User's choice:** `SentinelSuite.Framework.Domain.Shared.Guards`

| Option | Description | Selected |
|--------|-------------|----------|
| `Guard` (with `Guard.Against.X` call sites) | Matches Ardalis.GuardClauses' actual type name. | ✓ |
| `GuardClauses` (with `GuardClauses.Against.X` call sites) | Matches the phase/requirement name (PRIM-01) literally. | |

**User's choice:** `Guard` (with `Guard.Against.X` call sites)

---

## Nullable-analysis attributes

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — add `[NotNull]`/`[return: NotNull]` attributes | Matches Ardalis.GuardClauses' actual implementation; eliminates false nullable warnings downstream. | ✓ |
| No — skip nullable-analysis attributes | Simpler, attribute-free implementation. | |
| You decide | Let Claude apply if low-effort. | |

**User's choice:** Yes — add `[NotNull]`/`[return: NotNull]` attributes

---

## Claude's Discretion

- Exact naming convention for future per-module guard-extension classes (e.g. `{Module}GuardExtensions`).
- The full rounded-out guard method list beyond the confirmed floor (Negative/NegativeOrZero/Zero, Default, InvalidInput).
- `IGuardClause` extension-method implementation details (static class organization, file layout).
- Whether the standard-BCL-exception choice for guard failures needed further discussion — confirmed as the only sensible option given Phase 4's explicit retrofit plan.

## Deferred Ideas

None — discussion stayed within phase scope.
