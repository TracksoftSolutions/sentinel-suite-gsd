# Phase 3: Domain.Shared: SmartEnum<T> - Context

**Gathered:** 2026-07-16
**Status:** Ready for planning

<domain>
## Phase Boundary

A hand-rolled type-safe enumeration base exists in `SentinelSuite.Framework.Domain.Shared`, giving the kernel an Ardalis.SmartEnum-equivalent without the NuGet dependency. This is an Ardalis.SmartEnum-equivalent — the *pattern* is borrowed, not the package (per `PROJECT.md` constraint).

**Requirement:** PRIM-03.
**Depends on:** Nothing (parallel with Phase 1).

</domain>

<decisions>
## Implementation Decisions

### Value type support
- **D-01:** Build **both forms**: an int-backed `SmartEnum<TEnum>` and a generic-value `SmartEnum<TEnum, TValue>` (e.g. string-backed). Matches Ardalis.SmartEnum's actual shape and continues the "build broader now" pattern set in Phase 1 (D-07) and Phase 2 (D-07/D-12/D-13) — lets future modules pick string-backed status/type codes without needing another phase.

### Instance discovery mechanism
- **D-02:** Instance discovery is **reflection-based auto-discovery** — reflects over public static readonly fields on the derived type to find all defined instances. Matches Ardalis.SmartEnum exactly; zero boilerplate at each derived enum's call site (just declare the static instances, lookup "just works").
- **D-03:** The reflection scan is **lazy, cached on first lookup** per derived type (not run eagerly in a static constructor). Matches Ardalis.SmartEnum's actual behavior — avoids static-constructor ordering/exception-semantics risk, cost paid once per type on first `FromValue`/`FromName`/`List` access.

### Lookup failure API & exception type
- **D-04:** Ship **both** throwing (`FromValue`/`FromName`) and non-throwing Try-variants (`TryFromValue`/`TryFromName`, bool + out value). Build broader now — mirrors Result's philosophy (Phase 2) of separating expected-miss handling from exceptional-miss handling.
- **D-05:** `FromValue`/`FromName` throw a **dedicated `SmartEnumNotFoundException`** on a miss — a deliberate deviation from Phase 1/2's "BCL exception now, DomainException retrofit in Phase 4" precedent (D-04 in Phase 1, D-06 in Phase 2). User explicitly chose to introduce the dedicated type now rather than wait for Phase 4. **Flag to planner:** Phase 4 (DomainException) may need to reconcile/rebase this exception type once `DomainException` lands — note this as a known follow-up, not a conflict to resolve now.
- **D-06:** `SmartEnumNotFoundException` is a **single type** used by both `FromValue`-miss and `FromName`-miss (not two distinct exception types), carrying the attempted lookup value/name and the target SmartEnum type for a precise error message.

### Comparison & enumeration surface
- **D-07:** SmartEnum implements **`IComparable<SmartEnum<TEnum>>`**, sorting by underlying `Value`. Matches Ardalis.SmartEnum exactly and continues the "build broader now" pattern — lets any derived enum be sorted/ordered out of the box (e.g. status pipelines, severity levels).
- **D-08:** The generic-value form's `TValue` is **constrained to `IComparable<TValue>`** — so both the int-backed and generic-value-backed forms (D-01) are sortable by their underlying value, giving one consistent contract across both forms rather than only the int-backed form being comparable.
- **D-09:** A **static `List` property** (or `GetAll()`-equivalent) is exposed on every derived SmartEnum type, enumerating all its defined instances. Matches Ardalis.SmartEnum; near-zero-cost addition since the reflection scan (D-02) already collects this data.

### Equality & representation
- **D-10:** SmartEnum overloads **`==` and `!=`** operators, in addition to `Equals`/`GetHashCode`. Matches Ardalis.SmartEnum exactly — lets call sites write `status == MyEnum.Active` naturally.
- **D-11:** SmartEnum overrides **`ToString()`** to return the instance's `Name`. Matches Ardalis.SmartEnum; makes logging, string interpolation, and debugger display show the enum's label instead of the fully-qualified type name.

### Type shape
- **D-12:** The self-referencing generic constraint (`TEnum : SmartEnum<TEnum>`) is **compiler-enforced**, matching Ardalis.SmartEnum and preventing mismatched generic usage at derived-type declaration.
- **D-13:** Derived SmartEnum types are **expected/documented to be sealed** — this phase establishes the sealed-by-default convention ahead of `Entity`'s formal rule in Phase 9 (per ROADMAP.md Phase 9 success criteria #3). Document this precedent inline (matching Phase 1's D-06 naming-convention-documented-in-code approach) so Phase 9 has an established pattern to point back to, not a new decision to make from scratch.

### Claude's Discretion
- Exact namespace — apply the established `Domain.Shared.{Concept}` sub-namespace convention (Phase 1 set `...Guards`, Phase 2 set `...Results`); this phase is expected to set `...SmartEnum` or `...Enumerations`. Confirm during planning.
- Internal reflection-caching implementation details (e.g., `ConcurrentDictionary<Type, ...>` vs `Lazy<T>` per type) — not discussed, left to planning.
- File/class organization within the SmartEnum sub-namespace (how many files, how the int-backed vs generic-value forms are split) — not discussed, left to planning, matching Phase 1/2's per-concern file split precedent.
- Exact wording/shape of `SmartEnumNotFoundException`'s message and properties — not discussed beyond D-06, left to planning.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level constraints and requirements
- `.planning/PROJECT.md` — Constraints section: dependency minimalism, no-ABP/no-Ardalis-package rule, .NET 10/C# nullable+implicit-usings baseline.
- `.planning/REQUIREMENTS.md` §"Domain.Shared Primitives" — PRIM-03 definition (this phase's requirement).
- `.planning/ROADMAP.md` §"Phase 3: Domain.Shared: SmartEnum<T>" — success criteria this phase must satisfy; §"Phase 4: Domain.Shared: DomainException" — relevant to D-05's flagged follow-up (SmartEnumNotFoundException introduced ahead of DomainException, unlike Phase 1/2's precedent); §"Phase 9: Entity Base Class (the Keystone)" success criterion #3 — the sealed-by-default discipline this phase pre-establishes per D-13.

### Architecture and pattern guidance
- `docs/architecture-guidance.md` — the inherit/interface/compose three-way rule this kernel follows generally; also confirms (§"A 'type' distinguished only by name...") that a type-safe enum with behavior is the correct tool where a bare C# `enum` wouldn't suffice.
- `.claude/CLAUDE.md` §"Hand-Roll vs. Depend" → "9. SmartEnum — HAND-ROLL (small, ~100-150 lines)" — sizing/scope estimate for this phase. Note: this session's decisions (dual value-type forms, Try-variants, IComparable, static List, operator overloads) intentionally exceed that estimate per the user's continued "build broader now" choices (D-01, D-04, D-07, D-09, D-10) — flag this to the planner as a deliberate scope expansion, not scope creep.
- `.claude/CLAUDE.md` §"Explicitly excluded" — confirms `Ardalis.SmartEnum` itself must never be taken as a NuGet dependency; only the pattern is borrowed.

### Prior-phase precedent (Phases 1 & 2)
- `.planning/phases/01-domain-shared-guardclauses/01-CONTEXT.md` — establishes the `Domain.Shared.{Concept}` sub-namespace convention (D-10), the sealed-class-with-static-entry-point shape precedent, and the "build broader now" pattern (D-07) this phase continues.
- `.planning/phases/02-domain-shared-result-result-t/02-CONTEXT.md` — establishes the `...Results` sub-namespace precedent, sealed-class type shape (D-16), and the "BCL exception now, DomainException retrofit in Phase 4" precedent (D-06) that this phase's D-05 deliberately deviates from — the planner should be aware this is an explicit, discussed deviation, not an oversight.
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` — precedent for sealed-class + static entry-point shape.

No external specs beyond the above — requirements and decisions fully captured above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SentinelSuite.Framework.Domain.Shared/Guards/` — Phase 1's completed guard-clause implementation. Establishes the file-per-concern organization (`GuardAgainst{Concept}Extensions.cs`) precedent that SmartEnum's implementation should mirror where applicable.
- `SentinelSuite.Framework.Domain.Shared.Tests/Guards/` — existing xUnit v3 (MTP-native) test project and structure; SmartEnum's tests should follow the same project/test-file conventions.
- Phase 2 (`Result`/`Result<T>`) has not yet landed in code (context gathered, not yet planned/executed) — no `Results/` folder exists yet in `Domain.Shared`. SmartEnum can proceed independently since Phase 3 has no dependency on Phase 2.

### Established Patterns
- Namespace convention: `SentinelSuite.Framework.Domain.Shared.{Concept}` sub-namespaces (Phase 1 set `...Guards`, Phase 2 set `...Results`; this phase expected to set `...SmartEnum`).
- Sealed class + static entry point / sealed-by-default (Phase 1's `Guard`, Phase 2's `Result`/`Result<T>` — both sealed classes) — directly informs D-13's sealed-by-default convention for derived SmartEnum types.
- BCL exceptions were the default exception-handling precedent through Phase 1/2; D-05 is a deliberate, discussed departure from that precedent for this phase only.

### Integration Points
- `SentinelSuite.Framework.Domain` project (not yet started) will be the first real consumer of `SmartEnum<T>` once concrete kernel types needing type-safe enumerations land in later phases — not this phase's concern, but the API shape decided here is what all future enum-like domain concepts will use.

</code_context>

<specifics>
## Specific Ideas

- User consistently chose the "build broader now" option at nearly every fork this session (dual value-type forms, Try-variants alongside throwing lookups, IComparable + static List, operator overloads, ToString() override) — continuing the exact pattern from Phase 1 (D-07/D-09) and Phase 2 (D-07/D-09/D-12/D-13). Downstream planning should expect this phase's actual surface area to land meaningfully larger than REQUIREMENTS.md's literal 4-point success criteria — that's an intentional, discussed choice, not scope drift.
- The one deviation from pure precedent-following: D-05's dedicated `SmartEnumNotFoundException` breaks from Phase 1/2's "throw BCL exceptions until Phase 4" convention. This was a deliberate, explicit choice — not an oversight — and should be treated as a known, accepted departure when Phase 4 (DomainException) is eventually planned.
- Call sites should be able to write `MyStatus.FromValue(1)` / `MyStatus.FromName("Active")` (throwing) or `MyStatus.TryFromValue(1, out var status)` (non-throwing), sort a list of instances directly via `IComparable`, and get `MyStatus.List` to enumerate every defined value — mirroring Ardalis.SmartEnum's actual ergonomics throughout.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. All seven discussed areas (Value type support, Instance discovery mechanism, Lookup failure API & exception type, Comparison & enumeration surface, Equality operators, Sealed-by-default & generic constraint, ToString() override) resolved as in-scope decisions above, not deferred.

</deferred>

---

*Phase: 3-Domain.Shared: SmartEnum<T>*
*Context gathered: 2026-07-16*
