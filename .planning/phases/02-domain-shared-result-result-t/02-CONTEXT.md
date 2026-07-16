# Phase 2: Domain.Shared: Result / Result<T> - Context

**Gathered:** 2026-07-16
**Status:** Ready for planning

<domain>
## Phase Boundary

A hand-rolled operation-result pattern exists in `SentinelSuite.Framework.Domain.Shared` for expected failure paths, giving the kernel a dependency-free alternative to throwing exceptions for anticipated business failures. This is an Ardalis.Result-equivalent — the *pattern* is borrowed, not the package (per `PROJECT.md` constraint).

**Requirement:** PRIM-02.
**Depends on:** Nothing (parallel with Phase 1).

</domain>

<decisions>
## Implementation Decisions

### Error representation
- **D-01:** A failed `Result`/`Result<T>` carries a **list of structured errors**, not a single string or single code+message pair. Each error is a `Code` (string) + `Message` (string) pair. This supports aggregating multiple invariant/validation failures into one `Result` (e.g., an Entity failing 3 validations at once) — the closest match to Ardalis.Result's actual shape and the most future-proof for downstream modules.
- **D-02:** `Code` is a **plain string**, not a fixed enum. Freeform, dot-namespaced convention expected (e.g., `"Validation.Required"`, `"Guard.OutOfRange"`) so any of the 26 future modules can mint its own codes without editing `Domain.Shared` — same extensibility philosophy as Phase 1's `IGuardClause` anchor.
- **D-03:** `Message` is **required** (non-empty) on every error — guarantees anything catching a failure (logs, UI, future API responses) always has a displayable string without needing a code→message lookup table.
- **D-04:** `Result`/`Result<T>` expose **both** `.Errors` (full list) and `.Error` (convenience accessor for the first error) — most call sites only care about one error and want `result.Error.Message` without indexing; `.Errors` stays available for the multi-error aggregation case. Matches how Ardalis.Result actually shipped this.

### Result vs. exception boundary
- **D-05:** **`Result` is for expected business failures; throwing remains the path for invariant/programmer errors.** Phase 1's guard clauses and Phase 4's future `DomainException` stay the throw path for "this should never happen" / precondition violations. `Result` is reserved for outcomes a caller should anticipate and handle (validation failed, not found, business rule blocked) — matches REQUIREMENTS.md's framing of Result as being for "expected failure paths." Guard clauses are NOT retrofitted to return `Result` instead of throwing.
- **D-06:** Accessing `.Value` on a **failed** `Result<T>` throws `System.InvalidOperationException` (standard BCL exception, no new exception type — mirrors Ardalis.Result's actual behavior and Phase 1's precedent of using BCL exceptions this early in the kernel, before Phase 4's `DomainException` exists).

### Status richness
- **D-07:** Build the **full richer `ResultStatus` enum now**, not a bare success/failure boolean — even though there's no Application/API layer yet this milestone. User explicitly chose to front-load this surface rather than defer it (see Claude's Discretion note on user's stated preference below).
- **D-08:** `ResultStatus` includes the **full Ardalis.Result-style set**: `Ok`, `Error`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`, `CriticalError`.
- **D-09:** Status is set via **named static factory methods per status** — `Result.Success()`, `Result.NotFound(...)`, `Result.Conflict(...)`, `Result.Invalid(errors)`, `Result.Forbidden(...)`, `Result.Unauthorized(...)`, `Result.Unavailable(...)`, `Result.CriticalError(exception)`, `Result.Error(...)` — not a single generic `Fail(status, errors)` factory. Call sites read as intent.
- **D-10:** The **identical factory set applies to both `Result` and `Result<T>`** — `Result<T>.NotFound(...)`, `Result<T>.Conflict(...)`, etc. mirror `Result`'s factories exactly (just without a `Value` on failure). One consistent API surface regardless of return type.
- **D-11 (CriticalError diagnostics):** `Result.CriticalError(exception)` **carries the original caught `Exception`** alongside its Code+Message error — distinct from a normal business-rule `Error`, which never wraps an exception. This is the whole point of `CriticalError` existing as a distinct status: it represents something unexpected being caught and converted to a `Result` at a boundary, and the exception/stack trace must survive for logging.

### Combinators/chaining
- **D-12:** This phase ships **railway-style chaining combinators**, beyond the literal construction/inspection minimum in REQUIREMENTS.md's success criteria — following Phase 1's D-07 "build broader than minimum" precedent. Confirmed set:
  - **Map** — transform the success value into a new value/type, short-circuiting on failure.
  - **Bind/Then** — chain to another operation that itself returns a `Result`, flattening instead of nesting (the actual "railway" mechanism).
  - **OnSuccess/OnFailure** — fire a side-effect action without changing the `Result` (logging/notification hooks).
  - **Match** — collapse a `Result` into a single value via success-handler + failure-handler functions.
  - **Ensure** — turn a success into a failure mid-chain if a predicate isn't met (`result.Ensure(x => x.IsValid, error)`); pairs naturally with Map/Bind for validation-in-a-chain.
- **D-13:** Every combinator above ships with **both sync and async (`Task<Result<T>>`) overloads** this phase. User explicitly chose to front-load async support even though nothing in this milestone is async yet (no persistence/I/O exists — Domain-layer only per PROJECT.md) — same "build broader now" choice as D-07/D-12.
- **D-14:** `Result<T>` supports an **implicit conversion from a bare value** (`T → Result<T>`) — `public Result<T> DoThing() { ... return value; }` compiles directly without `Result<T>.Success(value)`. Matches Ardalis.Result's actual ergonomic; keeps success-path returns terse across 26 future modules.
- **D-15 (Combine):** `Result.Combine(r1, r2, r3, ...)` exists — succeeds only if all inputs succeed; on any failure, aggregates **all** errors across every failed input into one failed `Result`. Distinct from Bind's short-circuit-on-first-failure chaining; this is the "validate everything, report every problem at once" batch case. Natural pairing with the list-of-structured-errors shape (D-01).

### Type shape
- **D-16:** `Result` and `Result<T>` are **sealed classes**, not records or readonly structs. Matches Ardalis.Result's actual implementation and Phase 1's `Guard` precedent (sealed class). Reference-type semantics are appropriate — `Result` isn't used as a dictionary key or compared for value equality, and constructor-based factories (Success/Error/NotFound/etc.) read naturally as a class hierarchy.

### Naming & namespace
- **D-17 (Claude's discretion, following established precedent):** Namespace should follow Phase 1's `Domain.Shared.Guards` precedent — expected to be `SentinelSuite.Framework.Domain.Shared.Results`, keeping `Domain.Shared`'s root namespace uncluttered. Not explicitly discussed this session but directly follows D-10 from Phase 1's CONTEXT.md; confirm during planning rather than re-litigating.

### Claude's Discretion
- Exact namespace confirmation (D-17) — apply Phase 1's established `Domain.Shared.{Concept}` sub-namespace convention.
- Exact type name for the error record (e.g., `Error` vs `ResultError`) — avoid collision with any BCL/common type names; not discussed, left to planning.
- Internal implementation details of `Combine` (D-15) when mixing `Result` and `Result<T>` inputs — not discussed, left to planning to design a sensible overload set.
- File/class organization within the `Results` sub-namespace (how many files, how combinators are grouped) — not discussed, left to planning, matching Phase 1's per-concern file split precedent (`GuardAgainstNullExtensions.cs`, etc.).

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level constraints and requirements
- `.planning/PROJECT.md` — Constraints section: dependency minimalism, no-ABP/no-Ardalis-package rule, .NET 10/C# nullable+implicit-usings baseline.
- `.planning/REQUIREMENTS.md` §"Domain.Shared Primitives" — PRIM-02 definition (this phase's requirement).
- `.planning/ROADMAP.md` §"Phase 2: Domain.Shared: Result / Result<T>" — success criteria this phase must satisfy; §"Phase 4: Domain.Shared: DomainException" — informs the Result/exception boundary (D-05), since guard-clause failures migrate to `DomainException` in Phase 4, not `Result`.

### Architecture and pattern guidance
- `docs/architecture-guidance.md` — the inherit/interface/compose three-way rule this kernel follows generally.
- `.claude/CLAUDE.md` §"Hand-Roll vs. Depend" → "7. Result pattern — HAND-ROLL (trivial, ~80-120 lines)" — sizing/scope estimate for this phase. Note: this session's decisions (full status enum, five combinators with sync+async overloads, Combine) intentionally exceed that trivial-scope estimate per the user's explicit "build broader now" choices (D-07, D-12, D-13) — flag this to the planner as a deliberate scope expansion, not scope creep, since it's still entirely within "how to implement Result," not a new capability.
- `.claude/CLAUDE.md` §"Explicitly excluded" — confirms `Ardalis.Result` itself must never be taken as a NuGet dependency; only the pattern is borrowed.

### Prior-phase precedent (Phase 1)
- `.planning/phases/01-domain-shared-guardclauses/01-CONTEXT.md` — establishes the `Domain.Shared.{Concept}` sub-namespace convention (D-10 in that file), the sealed-class-with-static-entry-point shape precedent, and D-04's explicit note that guard failures throw BCL exceptions this phase with `DomainException` retrofit deferred to Phase 4 (directly informs this phase's D-05/D-06).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` — precedent for sealed-class + static singleton entry-point shape.
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/IGuardClause.cs` — precedent for the extension-method extensibility anchor pattern (not directly reused by Result, but the file-organization/naming precedent applies).

No external specs beyond the above — requirements and decisions fully captured above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `SentinelSuite.Framework.Domain.Shared/Guards/` — Phase 1's completed guard-clause implementation. Not directly reused by `Result`, but establishes the file-per-concern organization (`GuardAgainst{Concept}Extensions.cs`) that Result's implementation should mirror where applicable (e.g., separate files per combinator group).
- `SentinelSuite.Framework.Domain.Shared.Tests/Guards/` — existing xUnit v3 (MTP-native) test project and structure; Result's tests should follow the same project/test-file conventions.

### Established Patterns
- Namespace convention: `SentinelSuite.Framework.Domain.Shared.{Concept}` sub-namespaces (Phase 1 set `...Guards`; this phase is expected to set `...Results`).
- Sealed class + static entry point (Phase 1's `Guard` class) — establishes precedent that Domain.Shared primitive types default to `sealed class`, informing D-16.
- BCL exceptions (not custom domain exceptions) are the throw mechanism until Phase 4's `DomainException` lands — directly informs D-06's choice of `InvalidOperationException`.

### Integration Points
- `SentinelSuite.Framework.Domain` project (not yet started) will be the first real consumer of `Result`/`Result<T>` once `Entity` and friends land in later phases — not this phase's concern, but the API shape decided here is what all future validation/business-rule code will use.

</code_context>

<specifics>
## Specific Ideas

- User consistently chose the "build broader now" option over the minimal/deferred option at every fork this session (full status enum vs. bare boolean, five combinators + async overloads vs. sync-only construction/inspection, adding Combine). This mirrors the pattern already set in Phase 1 (D-07/D-09: "build a broader-than-minimum guard surface now"). Downstream planning/research should expect this phase's actual surface area to land meaningfully larger than REQUIREMENTS.md's literal 4-point success criteria — that's an intentional, discussed choice, not scope drift.
- Call sites should be able to write `return value;` for success (D-14) and `result.Ensure(...).Map(...).Bind(...)` style chains for validation/transformation pipelines, mirroring Ardalis.Result's actual fluent ergonomics throughout.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. All seven discussed areas (Error representation, Result vs. exception boundary, Status richness, Combinators/chaining, Type shape, Combine, CriticalError diagnostics) resolved as in-scope decisions above, not deferred.

</deferred>

---

*Phase: 2-Domain.Shared: Result / Result<T>*
*Context gathered: 2026-07-16*
