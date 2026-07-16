# Phase 2: Domain.Shared: Result / Result<T> - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-16
**Phase:** 2-Domain.Shared: Result / Result<T>
**Areas discussed:** Error representation, Result vs. exception boundary, Status richness, Combinators/chaining, Type shape, Combine (batch validation), CriticalError diagnostics

---

## Error representation

| Option | Description | Selected |
|--------|-------------|----------|
| List of structured errors | Code+Message pairs, aggregates multiple failures | ✓ |
| Single Code+Message pair | One failure, one code, one message | |
| Plain string message only | Simplest shape, under-delivers REQUIREMENTS.md wording | |

**User's choice:** List of structured errors (recommended option).
**Notes:** Follow-up questions in this area:

| Option | Description | Selected |
|--------|-------------|----------|
| Plain string Code | Freeform, dot-namespaced convention, module-extensible | ✓ |
| Fixed enum Code | Type-safe but requires editing Domain.Shared per new code | |

**User's choice:** Plain string.

| Option | Description | Selected |
|--------|-------------|----------|
| Both .Errors (list) + .Error (first) | Convenience shortcut for common single-error case | ✓ |
| Only .Errors (list) | Single access path, no shortcut | |

**User's choice:** Both.

| Option | Description | Selected |
|--------|-------------|----------|
| Message required | Every error has a non-empty message | ✓ |
| Message optional, code-only allowed | Compact but risks blank messages surfacing | |

**User's choice:** Message required.

---

## Result vs. exception boundary

| Option | Description | Selected |
|--------|-------------|----------|
| Result = expected business failures; throw = invariant/programmer errors | Guard clauses/DomainException stay the throw path | ✓ |
| Result absorbs everything, including invariant violations | Bigger surface change, guard clauses get wrapped/bypassed | |

**User's choice:** Result = expected business failures; throw = invariant/programmer errors.
**Notes:** Follow-up on failed `Result<T>.Value` access:

| Option | Description | Selected |
|--------|-------------|----------|
| Throw InvalidOperationException | Standard BCL exception, mirrors Ardalis.Result | ✓ |
| Throw custom ResultFailureException | New exception type, risks competing with Phase 4's DomainException | |

**User's choice:** InvalidOperationException.

---

## Status richness

| Option | Description | Selected |
|--------|-------------|----------|
| Bare success/failure only | Minimal, no HTTP-flavored statuses yet | |
| Build the richer status enum now | Full Ardalis.Result-style ResultStatus | ✓ |

**User's choice:** Build the richer status enum now (against recommendation).
**Notes:** Follow-up on which statuses to include:

| Option | Description | Selected |
|--------|-------------|----------|
| Full Ardalis.Result set (Ok/Error/Invalid/NotFound/Conflict/Forbidden/Unauthorized/Unavailable/CriticalError) | Complete real-world set | ✓ |
| Smaller starter set (Ok/Error/NotFound/Invalid/Conflict) | Defers transport-layer-flavored statuses | |

**User's choice:** Full set.

| Option | Description | Selected |
|--------|-------------|----------|
| Named static factories per status | Result.NotFound(...), Result.Conflict(...), etc. | ✓ |
| Single Fail(status, errors) factory | Fewer methods, less self-documenting | |

**User's choice:** Named static factories per status.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, identical factory set on both Result and Result<T> | Consistent API surface | ✓ |
| Non-generic Result only | Narrower Result<T> API, inconsistency | |

**User's choice:** Identical factory set on both.

---

## Combinators/chaining

| Option | Description | Selected |
|--------|-------------|----------|
| No — construction/inspection only | Matches literal success criteria | |
| Yes — add Map/Bind/OnSuccess/OnFailure now | Round out surface per Phase 1's D-07 precedent | ✓ |

**User's choice:** Yes, add combinators now (against recommendation).
**Notes:** Which combinators (multiSelect):

| Option | Description | Selected |
|--------|-------------|----------|
| Map | Transform success value, short-circuit on failure | ✓ |
| Bind/Then | Chain to another Result-returning operation | ✓ |
| OnSuccess/OnFailure | Side-effect actions without changing Result | ✓ |
| Match | Collapse Result to a single value via handlers | ✓ |

**User's choice:** All four selected.

| Option | Description | Selected |
|--------|-------------|----------|
| No — sync only this phase | No async consumer exists yet | |
| Yes — add async overloads now | Avoid retrofitting later | ✓ |

**User's choice:** Add async overloads now (against recommendation).

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — implicit T → Result<T> | Terse success returns | ✓ |
| No — explicit Result<T>.Success(value) always | Greppable, no implicit-operator magic | |

**User's choice:** Implicit conversion.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — include Ensure | Validation-in-a-chain combinator | ✓ |
| No — skip Ensure this phase | Can add later | |

**User's choice:** Include Ensure.

---

## Type shape

| Option | Description | Selected |
|--------|-------------|----------|
| Sealed class | Matches Ardalis.Result and Phase 1's Guard precedent | ✓ |
| Readonly struct | Avoids allocation but mutable-list-in-struct footgun | |
| Record class | Free structural equality/with-expressions, rarely useful here | |

**User's choice:** Sealed class.

---

## Combine (batch validation)

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — include Combine | Aggregates errors from multiple independent Results | ✓ |
| No — skip Combine this phase | Not in literal success criteria | |

**User's choice:** Include Combine.

---

## CriticalError diagnostics

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — CriticalError carries the original Exception | Preserves stack trace for logging/diagnostics | ✓ |
| No — CriticalError is just another status, no exception payload | Simpler, uniform Error shape | |

**User's choice:** CriticalError carries the original Exception.

---

## Claude's Discretion

- Exact namespace confirmation — apply Phase 1's established `Domain.Shared.{Concept}` sub-namespace convention (expected: `SentinelSuite.Framework.Domain.Shared.Results`). Not explicitly re-asked this session since it directly follows an existing precedent.
- Exact type name for the error record (e.g., `Error` vs `ResultError`) — left to planning.
- Internal implementation details of `Combine` when mixing `Result` and `Result<T>` inputs — left to planning.
- File/class organization within the `Results` sub-namespace — left to planning, matching Phase 1's per-concern file split precedent.

## Deferred Ideas

None — discussion stayed within phase scope for all seven areas discussed.
