# Phase 4: Domain.Shared: DomainException - Context

**Gathered:** 2026-07-16
**Status:** Ready for planning

<domain>
## Phase Boundary

A dedicated domain-level exception taxonomy exists in `SentinelSuite.Framework.Domain.Shared`, distinct from framework/infrastructure (BCL) exceptions, and becomes the throw type for the *domain-fact* subset of guard-clause failures — closing the loop Phase 1 (D-04) deliberately left open. The phase also folds in two adjacent, discussed corrections: aligning three numeric guards to the BCL-canonical `ArgumentOutOfRangeException`, and rebasing Phase 3's `SmartEnumNotFoundException` onto the new taxonomy.

**Requirement:** PRIM-04.
**Depends on:** Phase 1 (guards to retrofit). Also takes a *practical* dependency on Phase 2's `Error` type (D-11) and Phase 3's `SmartEnumNotFoundException` (D-27) — both in the same `Domain.Shared` assembly, both executing before Phase 4 in numeric order. **Flag to planner:** ROADMAP lists Phase 4 as depending on Phase 1 only; the Phase 2 and Phase 3 code dependencies are real and must be reflected in plan sequencing.

**Governing principle for the whole phase (D-25), in the user's words:**
> *Exceptions we can anticipate are just that, not broken code — which is what needs to be allowed to fail.*

Anticipated domain facts get the kernel taxonomy, a namespaced `Code`, and a place in the error catalog. Defects (programmer/caller bugs) get BCL exceptions and are **deliberately excluded** from the code vocabulary, so they fail loudly instead of aggregating in a dashboard next to real business outcomes. The BCL-code-vocabulary gap is intentional, not an oversight.

</domain>

<decisions>
## Implementation Decisions

### Hierarchy & extensibility
- **D-01:** `DomainException` is an **open concrete base** — non-abstract (throwable directly) and non-sealed (derivable by the 26 future modules). This is an explicit, documented carve-out from the sealed-by-default convention Phase 3 (D-13) pre-established for Phase 9; the named justification is that the type's *purpose* is to be derived from, with `SmartEnumNotFoundException` (D-27) as the current concrete second implementation.
- **D-02:** This phase ships a **taxonomy**, not just the base. *(User overrode Claude's "base only" recommendation; rationale accepted — mirroring `ResultStatus` gives the throw side and the return side one shared vocabulary, so a failure keeps the same identity whether returned or thrown.)*
- **D-03:** The taxonomy is the **domain-meaningful subset** of `ResultStatus`: `DomainValidationException`, `DomainNotFoundException`, `DomainConflictException`, and `DomainRuleViolationException`. Deliberately **excluded**: `Forbidden`/`Unauthorized` (application-layer authorization — a `Domain.Shared` type asserting them contradicts PRIM-04's "distinct from framework/infrastructure"), `Unavailable` (infrastructure by definition), and `CriticalError` (its Phase 2 job is *wrapping* an exception at a boundary — circular as an exception itself).
- **D-04:** `DomainException` derives from **`System.Exception`** directly. Not `InvalidOperationException` (would collide with Phase 2 D-06's use of it for failed `.Value` access — the exact conflation PRIM-04 exists to prevent). Not `ApplicationException` (discouraged by Microsoft's own design guidelines).
- **D-05:** The four subtypes are **also open (non-sealed)** — a module's `ShiftOverlapException : DomainRuleViolationException` is then catchable both as itself and as a rule violation. Sealing them would flatten the taxonomy back to the root.
- **D-06:** Subtypes are **`Domain`-prefixed** (`DomainValidationException`, not `ValidationException`) to avoid collision under implicit usings with `System.ComponentModel.DataAnnotations.ValidationException` and the ecosystem-ubiquitous `NotFoundException`/`ConflictException`.
- **D-07:** The **validation-vs-rule test** is "**could I decide this without loading anything else?**" — *Validation* = the value is malformed on its own terms (judgeable from the value alone). *RuleViolation* = the value is well-formed but a domain rule forbids the operation in context (requires state to judge). Consequence: guards only ever see the value, so any migrating guard lands on `DomainValidationException`, never rule-violation.
- **D-08:** Future modules use **free exception names** for the actual failure (`ShiftOverlapException`, `BadgeRevokedException`) in their own namespaces. The documented convention constrains **only which base to derive from**: the most specific fitting taxonomy leaf, falling back to `DomainException`. (Mirrors Phase 1 D-06's inline-documented `{Module}GuardExtensions` approach — zero `Domain.Shared` edits required to extend.)
- **D-09:** **No `IDomainException` marker interface** — `catch (DomainException)` is the single mechanism. C# cannot `catch (IDomainException)`, so a marker would add a weaker "domain exception" path that silently loses catch-by-type.
- **D-10:** **Placement — central taxonomy, leaves local.** `DomainException` + the four subtypes live in `SentinelSuite.Framework.Domain.Shared.Exceptions` (cross-cutting, belong to no single concept). Concept-specific exceptions stay with their concept (`SmartEnumNotFoundException` in `...SmartEnum`) and derive from the central taxonomy. Follows the `Domain.Shared.{Concept}` convention while keeping each concept folder self-contained.

### Exception payload shape
- **D-11:** `DomainException` **reuses Phase 2's `Error(Code + Message)` record** as its payload — one error vocabulary across throw and return. *(Adds the Phase 2 practical dependency noted in Domain above.)*
- **D-12:** Payload is an **`IReadOnlyList<Error> Errors` plus an `.Error` first-item convenience accessor**, mirroring Phase 2 D-01/D-04 exactly. Preserves the round-trip: an aggregated `Result.Combine()` failure carrying 3 errors becomes an exception without dropping 2.
- **D-13:** Ship a **`ThrowIfFailure()` extension** on `Result`/`Result<T>` (in `...Exceptions`) that throws the taxonomy exception matching the `ResultStatus` (`Invalid→DomainValidationException`, `NotFound→DomainNotFoundException`, `Conflict→DomainConflictException`; `Error`/other → base `DomainException`), carrying the errors across intact. **Extension method — no edit to `Result`.** The reverse (`ToResult()`) is **deferred** (see Deferred Ideas) — no application/API boundary exists this milestone to convert at.
- **D-14:** `DomainException.Message` is **composed from `Errors`** — the single error's `Message` when there's one, a joined summary when several. `Errors` stays the single source of truth; nothing is invisible to a log that prints only `ex.Message`.
- **D-15:** **At least one `Error` is required** — no parameterless/message-only ctor. Ctors take `Error` or `IEnumerable<Error>`, plus a `(string code, string message)` convenience ctor that builds the `Error`, each with optional `innerException` overloads. Makes "a `DomainException` always carries ≥1 structured error" a compile-time invariant (parallel to Phase 2 D-03's non-empty-`Message` guarantee).
- **D-16:** **Document the error-code convention inline and reserve kernel prefixes.** Dot-namespaced codes (per Phase 2 D-02); the kernel reserves first segments `Guard.*`, `SmartEnum.*`, `Domain.*`; modules namespace under their own segment (`AccessControl.ShiftOverlap`). Documented the way Phase 1 D-06 documented its guard convention.
- **D-17:** **Information disclosure — documented contract, structural where the kernel controls the path.** Document on `DomainException` that `Error.Message` must never embed rejected values or sensitive state (codes + parameter names only), carrying Phase 1's T-1-02 discipline forward. The kernel's own throw sites (the guard retrofit) keep routing through `Guard.SafeParamName`, so kernel-controlled paths stay structurally safe; arbitrary caller-supplied strings are a documented contract, not runtime-sanitized (no reliable way to detect "sensitive" without mangling legitimate text).
- **D-18:** Each subtype **supplies an overridable default `Code`** (`DomainValidationException → Domain.Validation`, `→ Domain.NotFound`, `→ Domain.Conflict`, `→ Domain.RuleViolation`); throw sites may pass a more specific code. Ties D-03's taxonomy to D-16's reserved prefixes automatically.
- **D-19:** **No legacy binary-serialization ctor** (`SerializationInfo`/`ISerializable`). Obsolete as of .NET 8 (`SYSLIB0051`), `BinaryFormatter` removed in .NET 9+, nothing crosses an AppDomain, and it's a documented RCE surface FedRAMP review would question. The `Error(Code, Message)` payload is already cleanly JSON-serializable for any real boundary.

### Guard retrofit scope
- **D-20 (amends Phase 1 D-04):** **Principled split on the anticipate-vs-defect line (D-25), not .NET convention.**
  - **Migrating → `DomainValidationException`** (4 of 16 throw sites): `StringTooShort`, `StringTooLong`, `InvalidFormat` (all in `GuardAgainstStringExtensions.cs`), and `InvalidInput` (`GuardAgainstInputExtensions.cs`).
  - **Staying BCL, permanently and by design** (argument-contract = caller defect): `Null`/`NullOrEmpty`/`NullOrWhiteSpace` (`ArgumentNullException`/`ArgumentException`), `OutOfRange` (`ArgumentOutOfRangeException`), `EnumOutOfRange` (`InvalidEnumArgumentException`), `Negative`/`NegativeOrZero`/`Zero`/`Default`.
  - This **supersedes Phase 1 D-04's framing** that *all* guard BCL exceptions are "deliberately provisional" — the argument-contract BCL exceptions are now **final and correct**. No later phase should try to "finish" the migration.
- **D-21:** Migrating guards keep routing the captured name through `Guard.SafeParamName` (preserving D-17) and pass it to a **nullable `ParameterName` property on `DomainValidationException`**, which the composed `Message` also references. Preserves the structured, queryable parameter name `ArgumentException` gave up; lives on the subtype because it's context a validation failure has that other domain failures don't.
- **D-22:** **Document the validation-vs-argument test (D-07) inline** on `Guard`/`IGuardClause`, next to Phase 1 D-06's `{Module}GuardExtensions` naming rule — where an author writing a new guard is already looking.
- **D-23:** `InvalidInput` (the generic predicate escape hatch) **migrates**, documented as "the escape hatch for **domain shape** rules that don't warrant a named guard." Gives it one clear meaning; callers needing an argument-contract check use the BCL-throwing guard family or plain `ArgumentException`.
- **D-24 (scope addition beyond PRIM-04 — discussed, not creep):** Correct `Negative`/`NegativeOrZero`/`Zero` from `ArgumentException` to **`ArgumentOutOfRangeException`**, matching the BCL's own `ArgumentOutOfRangeException.ThrowIfNegative`/`ThrowIfZero`/`ThrowIfNegativeOrZero` (.NET 8) and the adjacent `OutOfRange` guard (resolving a current self-inconsistency in the kernel). `Default<T>` stays on `ArgumentException` (no BCL canonical for "unset struct"). Free now — the `Domain` project is empty, so nothing depends on the current behavior. **PLANNER TRAP:** `ArgumentOutOfRangeException(paramName, message)` takes its arguments in the **reverse order** of `ArgumentException(message, paramName)` — a mechanical type-name swap transposes message and parameter name silently.
- **D-25 (governing principle — see Domain):** Two-tier policy. Anticipated → kernel taxonomy + `Code` + catalog; defects → BCL, deliberately outside the vocabulary, allowed to fail loudly. **Document the full 7-category map** (below) as the kernel's exception policy. Retroactively validates Phase 2 D-06 (`InvalidOperationException` on failed `.Value` = defect, category 6, BCL correct, no reconciliation needed).

  **The 7-category map:**
  | # | Category | Example | Type |
  |---|---|---|---|
  | 1 | Argument-contract violation | `Guard.Against.Null(name)` | BCL (`ArgumentNullException`/`ArgumentOutOfRangeException`/`InvalidEnumArgumentException`) — defect |
  | 2 | Domain shape validation | badge number wrong format | `DomainValidationException` |
  | 3 | Domain rule violation | overlapping shift | `DomainRuleViolationException` |
  | 4 | Domain not-found | no such badge / SmartEnum miss | `DomainNotFoundException` |
  | 5 | Domain conflict | already checked in | `DomainConflictException` |
  | 6 | Kernel misuse | `.Value` on a failed `Result` | BCL (`InvalidOperationException`) — defect |
  | 7 | (module-specific) | derives from the fitting leaf (D-08) | kernel taxonomy |

### SmartEnum reconciliation (Phase 3 D-05 follow-up)
- **D-26:** A `SmartEnum.FromValue`/`FromName` miss is an **anticipated not-found** (category 4). `SmartEnumNotFoundException` **derives from `DomainNotFoundException`**, keeps its rich message (attempted value/name + target SmartEnum type), gains a `Code` (`SmartEnum.NotFound`, per D-16's reserved prefix), and becomes catchable as `DomainException`. The `Try*`/default-overload paths (Phase 3 D-04/D-14) remain the miss-is-normal path; the throwing overload is for "this genuinely should resolve, and its absence is a domain fact."
- **D-27:** **Keep numeric execution order.** Phase 3 ships `SmartEnumNotFoundException` on a plain base (e.g. `System.Exception`); Phase 4 **rebases it** to `DomainNotFoundException` as an explicit retrofit step, exactly the follow-up Phase 3 D-05 flagged. **Consequence:** Phase 4's retrofit scope covers guards **and** this SmartEnum rebase, so Phase 4 edits Phase 3's code. **Hand-off to Phase 3 planning:** ship the type from a plain base so the rebase is a one-line base-class swap (this does not reopen Phase 3's locked context — it's a note for its planner).

### Test strategy
- **D-28:** **Update Phase 1's tests in place + add full new coverage.** Edit the 7 affected Phase 1 tests to assert the new types (4 migrating guards → `DomainValidationException` with `Code` + `ParameterName`; 3 numeric guards → `ArgumentOutOfRangeException`), leaving non-migrating guard tests untouched. Add dedicated tests: catch-by-type (SC#1), message round-trip + `Code` + `Errors` list (SC#3), subtype default codes (D-18), `ThrowIfFailure()` mapping (D-13), and the SmartEnum rebase (D-27). *(Editing signed-off Phase 1 tests is intentional and in-scope for this retrofit.)*

### Guard error-code granularity
- **D-29:** Migrating guards carry **per-guard specific codes** under the reserved `Guard.*` prefix: `Guard.StringTooShort`, `Guard.StringTooLong`, `Guard.InvalidFormat`, `Guard.InvalidInput`. The subtype default (`Domain.Validation`, D-18) is the fallback for direct construction. Sets the precedent: codes are as specific as the throw site's knowledge allows.

### Equality / structural semantics
- **D-30:** **Reference equality only** — no `Equals`/`GetHashCode` override (exceptions are thrown and caught, never compared or keyed; same reasoning as Phase 2 D-16). `Errors` is exposed as a **read-only, order-preserving snapshot, defensively copied at construction** so a caller holding the original list can't mutate a thrown exception's errors, and aggregated failures read in insertion order.

### Documentation & threat model
- **D-31:** **Full XML docs + a Phase 4 threat-model note.** Every public type/ctor/property gets XML docs matching Phase 1–3's heavy-doc precedent, including the anticipate-vs-defect principle (D-25) on `DomainException` and the shape-vs-rule test (D-07) on the subtypes, so the reasoning lives in the code. Plus a Phase 4 threat-model note carrying D-17's information-disclosure concern forward explicitly (exception messages as a NIST 800-53 auditable disclosure surface), the way Phase 1 captured T-1-02.

### Claude's Discretion
- Exact subtype constructor overload set and internal file organization within `...Exceptions` (how many files, how the base and four subtypes are split) — follow Phase 1/2/3's per-concern file-split precedent.
- Exact wording of composed `Message` for the multi-error case (D-14) and of each XML-doc block (D-31).
- Exact `ThrowIfFailure()` overload shape for `Result` vs. `Result<T>` and the `ResultStatus`→exception mapping table's default arm (D-13) — the mapping is specified; the plumbing is planning's.
- Precise string values of default subtype codes (D-18) beyond the `Domain.{Case}` shape already specified, if the planner sees a reason to refine.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level constraints and requirements
- `.planning/PROJECT.md` — Constraints: dependency minimalism, no-ABP/no-Ardalis-package rule, .NET 10/C# nullable+implicit-usings baseline; Compliance note (DOE/FISMA/NIST 800-53/FedRAMP) that makes D-17/D-31's disclosure concern an auditable control, not a style choice.
- `.planning/REQUIREMENTS.md` §"Domain.Shared Primitives" — PRIM-04 definition (this phase's requirement): "a dedicated domain-level exception type, distinct from framework/infrastructure exceptions."
- `.planning/ROADMAP.md` §"Phase 4: Domain.Shared: DomainException" — the three success criteria this phase must satisfy. **Note the dependency correction:** ROADMAP lists Phase 4 → Phase 1 only, but D-11 (Phase 2 `Error`) and D-27 (Phase 3 `SmartEnumNotFoundException`) add real same-assembly code dependencies the planner must sequence around.

### Architecture and pattern guidance
- `docs/architecture-guidance.md` — the inherit/interface/compose three-way rule; informs D-09 (single-inheritance constraint reasoning) and the taxonomy shape generally.
- `.claude/CLAUDE.md` §"Hand-Roll vs. Depend" → "1. Entity/AggregateRoot" and the general zero-dependency-Domain-project constraint; also §"Explicitly excluded" (no `Ardalis.*`/`Volo.Abp.*`/`MediatR`). This phase writes zero-dependency BCL-and-hand-rolled code only.

### Prior-phase precedent (Phases 1–3) — load-bearing for this phase
- `.planning/phases/01-domain-shared-guardclauses/01-CONTEXT.md` — D-04 (BCL-exceptions-provisional, the loop this phase closes and **amends** per D-20); D-05 (`IGuardClause` extension anchor, the mechanism `ThrowIfFailure()` mirrors); D-06 (inline-documented convention precedent for D-16/D-22); T-1-02 information-disclosure threat model (`SafeParamName`), carried forward by D-17/D-21/D-31.
- `.planning/phases/02-domain-shared-result-result-t/02-CONTEXT.md` — D-01/D-04 (`Error` list + `.Error` accessor, reused by D-11/D-12); D-02 (freeform dot-namespaced `Code`, extended by D-16); D-05 (Result-vs-exception boundary — `DomainException` lives on the throw side); D-06 (`InvalidOperationException` on failed `.Value`, retroactively validated as category 6 by D-25); D-08 (`ResultStatus` set, the mirror source for D-03); D-15 (`Combine`, the aggregation D-12 preserves); D-16 (sealed-class, no value-equality — precedent for D-30).
- `.planning/phases/03-domain-shared-smartenum-t/03-CONTEXT.md` — D-04/D-14 (three lookup modes: `Try*`, default-overload, throwing); D-05/D-06 (`SmartEnumNotFoundException` introduced ahead of Phase 4 and **explicitly flagged for reconciliation here** — resolved by D-26/D-27); D-13 (sealed-by-default convention this phase's taxonomy is a documented carve-out from, D-01/D-05).

### Existing code to retrofit (read before planning the retrofit)
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` — `Guard.Against` entry point + `SafeParamName` (D-17/D-21); the remarks block is where D-16/D-22 conventions get documented.
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs` — `StringTooShort`/`StringTooLong`/`InvalidFormat` (3 migrating throw sites, D-20).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstInputExtensions.cs` — `InvalidInput` (migrating throw site, D-23).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNumericExtensions.cs` — `Negative`/`NegativeOrZero`/`Zero`/`Default` (3 BCL-type-correction sites + 1 unchanged, D-24; watch the reverse-argument-order trap).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/` — the xUnit v3 (MTP-native) test project; `GuardAgainstStringTests.cs`, `GuardAgainstInputTests.cs`, and `GuardAgainstNumericTests.cs` hold the 7 assertions D-28 updates in place.

No external specs beyond the above — requirements and decisions fully captured here.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Domain.Shared/Guards/` — Phase 1's completed, tested guard family; the retrofit target. Establishes the `GuardAgainst{Concept}Extensions.cs` per-concern file split the `Exceptions` folder should mirror where it makes sense.
- `Domain.Shared/Guards/Guard.cs` `SafeParamName` — an existing internal helper the migrating guards already call; reused as-is by D-21 (no change to its logic, only to what consumes its output).
- Phase 2 `Error` record and Phase 3 `SmartEnumNotFoundException` — **not yet on disk** (both phases have context but no executed code). Phase 4's plan must account for their arrival first in numeric order, or coordinate if execution order shifts.

### Established Patterns
- Namespace convention `SentinelSuite.Framework.Domain.Shared.{Concept}` (Phase 1 `...Guards`, Phase 2 `...Results`, Phase 3 `...SmartEnum`); this phase adds `...Exceptions` (D-10).
- Sealed-by-default for primitive types (Phase 1 `Guard`, Phase 2 `Result`, Phase 3 SmartEnum-derived) — this phase's exception taxonomy is the **first documented deliberate exception** to that rule (D-01/D-05).
- BCL-exceptions-for-defects: no longer "provisional until Phase 4" but a **settled two-tier policy** after D-20/D-25.
- Heavy XML docs + per-phase threat-model note (Phase 1 T-1-02) — continued by D-31.

### Integration Points
- `SentinelSuite.Framework.Domain` (empty scaffold) will be the first real consumer of the taxonomy once `Entity`/business methods land (Phases 9+) — the throw side of Phase 2's Result/exception boundary. Not this phase's concern beyond getting the base-type shape right.
- `ThrowIfFailure()` (D-13) attaches to `Result`/`Result<T>` without editing them — the same non-invasive extension pattern as Phase 1's `IGuardClause`.

</code_context>

<specifics>
## Specific Ideas

- The phase's spine is the user's own principle (D-25): *"exceptions we can anticipate are just that, not broken code, which is what needs to be allowed to fail."* Every classification call in this phase resolves against it — the guard split (D-20), the SmartEnum reconciliation (D-26), and the retroactive read of Phase 2's `InvalidOperationException` (category 6). The 7-category map (D-25) should read as *the* kernel exception policy, not an appendix.
- The user consistently chose the "one shared vocabulary across throw and return" thesis over layering purity: mirror `ResultStatus` (D-02/D-03), reuse `Error` (D-11), mirror the list+`.Error` shape (D-12), ship `ThrowIfFailure()` (D-13). Downstream planning should treat throw-side and return-side symmetry as an intentional design goal, not incidental.
- Two deliberate scope additions beyond PRIM-04's literal wording, both discussed and both cheap-now: the numeric-guard BCL correction (D-24) and the SmartEnum rebase (D-27). Flag both to the planner as intentional, not creep.

</specifics>

<deferred>
## Deferred Ideas

- **`ToResult()`** — converting a caught `DomainException` back into a failed `Result` (the reverse of D-13's `ThrowIfFailure()`). Deferred: no application/API boundary exists this milestone to convert at, and the catch-and-convert direction is what Phase 2's `CriticalError` status already covers. Revisit when the first real boundary (UseCases/Web) lands.

None else — discussion stayed within phase scope; the two scope *additions* (D-24, D-27) were resolved as in-scope decisions, not deferred.

</deferred>

---

*Phase: 4-Domain.Shared: DomainException*
*Context gathered: 2026-07-16*
