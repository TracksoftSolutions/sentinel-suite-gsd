---
phase: 01-domain-shared-guardclauses
plan: 6
subsystem: domain-shared
tags: [guard-clauses, dotnet, csharp, xunit, tdd]

# Dependency graph
requires:
  - phase: 01-domain-shared-guardclauses (Plan 01-02)
    provides: IGuardClause marker interface + Guard.Against singleton entry point
  - phase: 01-domain-shared-guardclauses (Plan 01-03)
    provides: Guard.Against.Null<T> (reused internally by the String guard family)
provides:
  - GuardAgainstInputExtensions static class with InvalidInput<T>(T, Func<T,bool>) predicate escape hatch
  - GuardAgainstStringExtensions static class with StringTooShort, StringTooLong, InvalidFormat
affects: [entity-base-class, entityassociation-base-class, aggregate-root]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Unconstrained-T predicate escape hatch (InvalidInput<T>) for validation that doesn't warrant its own named guard"
    - "String-shape guards (length/regex) delegating to Guard.Against.Null first, same shape as NullOrWhiteSpace"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstInputExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstInputTests.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstStringTests.cs
  modified: []

key-decisions:
  - "Followed 01-PATTERNS.md's GuardAgainstInputExtensions.cs and GuardAgainstStringExtensions.cs shapes verbatim"
  - "NotFound-style guard permanently excluded per D-04 (BCL-exception-only constraint); documented inline with Phase 4 (DomainException) forward-reference"
  - "Combined both tasks' RED test files into one test(...) commit and both implementations into one feat(...) commit, rather than four separate per-task TDD commits, since both guard families are small and were authored together in this final plan of the phase — see Deviations section"

patterns-established:
  - "Predicate escape hatch: unconstrained generic T + Func<T,bool>, message references only parameterName, never the raw input"
  - "String-shape guard family: delegate to Guard.Against.Null first, then apply length/regex check, throw ArgumentException with a fixed message"

requirements-completed: [PRIM-01]

coverage:
  - id: D1
    description: "InvalidInput<T> returns the value unchanged when the predicate passes, throws ArgumentException when it fails, and never leaks the rejected value in the exception message"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstInputTests.cs#InvalidInput_WhenPredicateReturnsTrue_ReturnsSameValueUnchanged, #InvalidInput_WhenPredicateReturnsFalse_ThrowsArgumentException, #InvalidInput_WhenPredicateReturnsFalse_DoesNotLeakRejectedValueInMessage"
        status: pass
    human_judgment: false
  - id: D2
    description: "StringTooShort/StringTooLong/InvalidFormat implemented with correct pass/throw behavior and the NotFound-style guard permanently excluded and documented"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstStringTests.cs#StringTooShort_WhenLengthMeetsMinimum_ReturnsSameStringUnchanged, #StringTooShort_WhenLengthBelowMinimum_ThrowsArgumentExceptionWithCapturedParameterName, #StringTooLong_WhenLengthWithinMaximum_ReturnsSameStringUnchanged, #StringTooLong_WhenLengthExceedsMaximum_ThrowsArgumentExceptionWithCapturedParameterName, #InvalidFormat_WhenInputMatchesPattern_ReturnsSameStringUnchanged, #InvalidFormat_WhenInputDoesNotMatchPattern_ThrowsArgumentExceptionWithCapturedParameterName"
        status: pass
      - kind: static-check
        ref: "grep -c 'NotFoundException' GuardAgainstStringExtensions.cs == 0"
        status: pass
    human_judgment: false

# Metrics
duration: 6min
completed: 2026-07-16
status: complete
---

# Phase 1 Plan 6: InvalidInput and String Guard Family Summary

**Predicate escape hatch (InvalidInput<T>) plus string-shape guards (StringTooShort, StringTooLong, InvalidFormat), completing D-08/D-09's guard surface and the last plan of Phase 1**

## Performance

- **Duration:** 6 min
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- `GuardAgainstInputExtensions.cs` with `InvalidInput<T>` — unconstrained generic predicate escape hatch, returns input unchanged on pass, throws `ArgumentException` referencing only `parameterName` on fail
- `GuardAgainstStringExtensions.cs` with `StringTooShort`, `StringTooLong`, `InvalidFormat` — each delegates to `Guard.Against.Null` first, then a length/regex check, throwing `ArgumentException`
- NotFound-style guard permanently excluded from `GuardAgainstStringExtensions.cs`, documented inline (class-level remarks + a leading comment) with its Phase 4 (`DomainException`) forward-reference, per D-04's BCL-exception-only constraint
- All guard methods capture `parameterName` via `CallerArgumentExpression` and return the validated input unchanged on success (D-02/D-03)
- Exception messages interpolate only `parameterName`, never the raw rejected value (T-1-02 mitigation) — verified directly by a dedicated test asserting the message does not contain the rejected value's string form
- Full solution-wide `dotnet test` (run from `SentinelSuite/`) passes 43/43 with zero failures, confirming ROADMAP.md's Phase 1 success criteria are satisfied now that all 6 plans have landed

## Task Commits

Both tasks were implemented together as a single TDD RED/GREEN pair (see Deviations below):

1. **Task 1 (InvalidInput) + Task 2 (StringTooShort/StringTooLong/InvalidFormat)**
   - `2aceb1c` (test) — 9 failing behavior-block tests across both new test files; confirmed RED via compile failure (`CS1061`, guard methods did not exist)
   - `2d225db` (feat) — both extension classes implemented, 43/43 tests pass

_TDD tasks: test → feat commit pair, as expected._

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstInputExtensions.cs` - `InvalidInput<T>` guard-clause extension method
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs` - `StringTooShort`, `StringTooLong`, `InvalidFormat` guard-clause extension methods
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstInputTests.cs` - 3 xUnit v3 `[Fact]` tests covering pass/throw/no-leak paths
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstStringTests.cs` - 6 xUnit v3 `[Fact]` tests covering pass/throw paths for all three string guards

## Decisions Made
- Followed 01-PATTERNS.md's `GuardAgainstInputExtensions.cs` and `GuardAgainstStringExtensions.cs` shapes verbatim — no deviation from the researched pattern.
- NotFound-style guard excluded per D-04, documented inline in both class-level XML remarks and a leading `//` comment, with an explicit Phase 4 (`DomainException`) forward-reference, matching the precedent set by Plan 01-04's SQL-Server-guard exclusion.

## Deviations from Plan

**Process note (not a Rule 1-4 deviation):** Rather than producing four separate TDD commits (RED/GREEN per task), both tasks' test files were written together and both implementation files were written together, resulting in one combined `test(01-06)` commit and one combined `feat(01-06)` commit covering Task 1 and Task 2. This was a scheduling choice for this final, small plan — both guard families are tightly related (string guards reuse the same delegate-to-Null shape as the predicate guard's sibling files) and each task's acceptance criteria were independently verified before commit. No functional deviation from the plan's `<action>`/`<acceptance_criteria>` occurred; all per-task acceptance criteria were checked individually (see Self-Check and coverage IDs above, one per task).

Otherwise: None - plan executed exactly as written.

## TDD Gate Compliance

Verified in git log: `test(01-06)` commit `2aceb1c` precedes `feat(01-06)` commit `2d225db`. RED phase confirmed via compile failure (`CS1061`: guard methods did not exist on `IGuardClause`) rather than a merely-failing assertion — a stronger RED signal. GREEN phase confirmed via full test-suite pass (43/43, up from 34/34 at the start of this plan). No REFACTOR commit was needed — implementation matched the researched pattern with no cleanup required.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- D-08's `InvalidInput` floor and D-09's rounded-out string guards (`StringTooShort`, `StringTooLong`, `InvalidFormat`) are both implemented and green.
- NotFound-style guard permanently excluded and documented with its Phase 4 forward-reference.
- Full solution-wide `dotnet test` passes 43/43 with zero failures — all six Phase 1 plans (01-01 through 01-06) have now landed, satisfying ROADMAP.md's Phase 1 success criteria in full.
- Phase 1 (Domain.Shared GuardClauses) is complete; ready for phase transition to Phase 2.

---
*Phase: 01-domain-shared-guardclauses*
*Completed: 2026-07-16*

## Self-Check: PASSED

All 4 created files verified present on disk; both referenced commits (2aceb1c, 2d225db) verified present in git log.
