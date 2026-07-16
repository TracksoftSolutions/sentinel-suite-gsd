# Phase 1: Domain.Shared: GuardClauses - Context

**Gathered:** 2026-07-15
**Status:** Ready for planning

<domain>
## Phase Boundary

Hand-rolled guard-clause argument/invariant validation helpers exist in `SentinelSuite.Framework.Domain.Shared`, with zero third-party NuGet package references, giving every later phase a consistent, dependency-free way to validate arguments and invariants. This is an Ardalis.GuardClauses-equivalent — the *pattern* is borrowed, not the package (per `PROJECT.md` constraint).

**Requirement:** PRIM-01.
**Depends on:** Nothing (first phase).

</domain>

<decisions>
## Implementation Decisions

### API shape
- **D-01:** Fluent API matching Ardalis.GuardClauses exactly: a static `Guard` class exposes an `Against` entry point; individual guards (`Null`, `NullOrEmpty`, `OutOfRange`, etc.) are extension methods invoked as `Guard.Against.Null(x)`.
- **D-02:** Parameter names are auto-captured via `CallerArgumentExpression` (C# 10+, available on this stack's C# 14/.NET 10) — call sites do NOT need to pass `nameof(x)` explicitly.
- **D-03:** Every `Guard.Against.X(...)` method returns the validated value, enabling inline assignment (e.g., `_name = Guard.Against.NullOrEmpty(name)`), matching Ardalis.GuardClauses.
- **D-04:** Guard failures throw standard BCL exceptions this phase (`ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException`) — matches Ardalis.GuardClauses' own convention. This is deliberately provisional: Phase 4 (`DomainException`) explicitly retrofits at least one guard-failure path to throw `DomainException` instead, per `ROADMAP.md`'s Phase 4 success criteria. Do not invent an interim domain exception type in this phase.

### Extensibility mechanism
- **D-05:** `Guard.Against` is typed as an empty `IGuardClause` marker interface. Every guard method — including all the ones this phase builds — is implemented as an extension method on `IGuardClause`, not as a member of a closed class. This is the actual mechanism that makes Ardalis.GuardClauses extensible: any future module can add its own domain-specific guard (e.g., a hypothetical `Guard.Against.InvalidTenantId(...)` in a later phase/module) as an extension method with zero changes to `Domain.Shared`.
- **D-06:** A naming convention for future per-module guard-extension classes should be documented now so downstream phases don't reinvent it ad hoc (left to Claude's discretion below — see Claude's Discretion).

### Method scope for this phase
- **D-07:** Build a broader-than-minimum guard surface now, not just the exact methods implied by the roadmap's stated success criteria. The roadmap's literal success criteria only require null-argument, empty/range, and enum-membership coverage — but since 26 future modules will need a consistent guard vocabulary, it's cheap to add more now rather than growing the surface ad hoc later.
- **D-08:** Confirmed additions beyond the success-criteria minimum (`Null`, `NullOrEmpty`, `NullOrWhiteSpace`, `OutOfRange` (numeric range), `EnumOutOfRange`):
  - `Negative` / `NegativeOrZero` / `Zero` (numeric sign guards)
  - `Default<T>` (guards against an uninitialized/default-value struct, e.g. an unset `Guid` or `DateTime`)
  - `InvalidInput` (general-purpose predicate-based guard: `Guard.Against.InvalidInput(value, name, predicate)`, an escape hatch for validation that doesn't warrant its own named guard)
- **D-09:** Claude should round out the rest of the method list with other commonly-used Ardalis.GuardClauses-equivalent guards during planning (see Claude's Discretion) — the confirmed list above is a floor, not a ceiling.

### Naming & namespace
- **D-10:** Namespace: `SentinelSuite.Framework.Domain.Shared.Guards` — a dedicated sub-namespace within `Domain.Shared`, not the root namespace. This anticipates the same convention likely applying to sibling primitives (`Result`, `SmartEnum`) landing in Phases 2–3, keeping `Domain.Shared`'s root namespace uncluttered.
- **D-11:** The entry-point type is literally named `Guard` (call sites: `Guard.Against.X`), matching Ardalis.GuardClauses' actual public API exactly. "GuardClauses" remains the informal name for the phase/requirement (PRIM-01), not the literal type name.

### Nullable-analysis attributes
- **D-12:** Guard methods carry C# nullable-analysis attributes (`[NotNull]` on parameters, `[return: NotNull]` on the return value) so the compiler treats a value as non-null immediately after a guard call, eliminating false "possible null reference" warnings at every downstream call site. Matches Ardalis.GuardClauses' actual implementation. This compounds in value since every future `Entity`/`EntityAssociation` constructor across 222 features will guard its inputs this way.

### Claude's Discretion
- Exact naming convention for future per-module guard-extension classes (D-06) — e.g. `{Module}GuardExtensions`, one static class of `IGuardClause` extension methods per module's own namespace. Document whatever convention is chosen in code comments/docs so downstream phases have a precedent to follow.
- The full rounded-out guard method list beyond the confirmed floor in D-08 (D-09) — mirror Ardalis.GuardClauses' actual most-used methods; don't reinvent method names/shapes gratuitously.
- Exact `IGuardClause` extension-method implementation details (static class organization, file layout within the `Guards` sub-namespace) — not discussed, left to planning.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level constraints and requirements
- `.planning/PROJECT.md` — Constraints section: dependency minimalism, no-ABP/no-Ardalis-package rule, .NET 10/C# nullable+implicit-usings baseline.
- `.planning/REQUIREMENTS.md` §"Domain.Shared Primitives" — PRIM-01 definition (this phase's requirement).
- `.planning/ROADMAP.md` §"Phase 1: Domain.Shared: GuardClauses" — success criteria this phase must satisfy; §"Phase 4: Domain.Shared: DomainException" — the explicit retrofit that migrates at least one guard-failure path from a BCL exception to `DomainException` (informs D-04).

### Architecture and pattern guidance
- `docs/architecture-guidance.md` — the inherit/interface/compose three-way rule this kernel follows generally (not GuardClauses-specific, but the governing discipline for everything built in this milestone).
- `.claude/CLAUDE.md` §"Hand-Roll vs. Depend" → "9. GuardClauses — HAND-ROLL (trivial, ~100-150 lines for the useful subset)" — the research-backed sizing/scope estimate for this exact phase, and confirmation that Ardalis.GuardClauses' actual mechanics (the `IGuardClause` extension-method pattern) is the correct one to reproduce, not merely a plain static-class approximation.
- `.claude/CLAUDE.md` §"Explicitly excluded" — confirms `Ardalis.GuardClauses` itself must never be taken as a NuGet dependency; only the pattern is borrowed.

No external specs beyond the above — requirements and decisions fully captured above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- None. `SentinelSuite/SentinelSuite.Framework.Domain.Shared/` is an empty .NET 10.0 project scaffold (`.csproj` only, nullable + implicit usings enabled) — no `.cs` source files exist yet. This phase is the first code written in the project.

### Established Patterns
- None yet established in code. This phase sets the precedent (namespace convention, extension-method-based extensibility) that later Domain.Shared phases (Result, SmartEnum, DomainException) are expected to follow.

### Integration Points
- `SentinelSuite.Framework.Domain` project already exists as an empty scaffold and will reference `Domain.Shared` in a later phase (Phase 5, solution layout) — not this phase's concern, but the guard clauses built here are the first thing later Domain-layer code (`Entity`, `EntityAssociation`, etc.) will call.

</code_context>

<specifics>
## Specific Ideas

- Call sites should read like Ardalis.GuardClauses in every particular: `Guard.Against.Null(value)` (no `nameof`, thanks to `CallerArgumentExpression`), assignable inline, non-null-inferred by the compiler afterward.
- The `IGuardClause` marker-interface extensibility hook is a deliberate choice specifically because this kernel will grow to 26 modules — the user explicitly wants each future module able to add its own guard without editing `Domain.Shared`.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. (The extensibility *mechanism* and *floor method list* were both resolved as in-scope decisions above, not deferred.)

</deferred>

---

*Phase: 1-Domain.Shared: GuardClauses*
*Context gathered: 2026-07-15*
