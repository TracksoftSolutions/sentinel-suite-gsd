---
phase: 01-domain-shared-guardclauses
plan: 5
subsystem: domain-shared
tags: [guard-clauses, dotnet, csharp, xunit, tdd]

# Dependency graph
requires:
  - phase: 01-domain-shared-guardclauses (Plan 01-02)
    provides: IGuardClause marker interface + Guard.Against singleton entry point
provides:
  - GuardAgainstNumericExtensions static class with Negative<T>, NegativeOrZero<T>, Zero<T>, Default<T>
  - Generic numeric-sign guards for any IComparable<T> struct
  - Generic uninitialized-struct-default guard for any IEquatable<T> struct (Guid, DateTime, etc.)
affects: [entity-base-class, entityassociation-base-class, aggregate-root]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IComparable<T>-constrained generic numeric guard comparing input against default(T)"
    - "IEquatable<T>-constrained generic default-value guard, type-agnostic across struct types"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNumericExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNumericTests.cs
  modified: []

key-decisions:
  - "Followed 01-PATTERNS.md's GuardAgainstNumericExtensions.cs shape verbatim: IComparable<T>-constrained generics for Negative/NegativeOrZero/Zero, IEquatable<T>-constrained generic for Default<T>"
  - "Zero-boundary semantics per D-08: Negative passes 0 (0 is not negative), NegativeOrZero rejects 0, Zero rejects only 0"

patterns-established:
  - "Numeric-sign guard family: compare against default(T) via CompareTo, never a hardcoded numeric type"
  - "Default<T> guard: compare against default(T) via Equals, proven type-agnostic across Guid and DateTime fixtures"

requirements-completed: [PRIM-01]

coverage:
  - id: D1
    description: "Negative<T>/NegativeOrZero<T>/Zero<T> guards implemented with correct zero-boundary handling per guard"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNumericTests.cs#Negative_WhenInputPositive_ReturnsSameValueUnchanged, #Negative_WhenInputNegative_ThrowsArgumentExceptionWithCapturedParameterName, #Negative_WhenInputZero_ReturnsSameValueUnchanged, #NegativeOrZero_WhenInputPositive_ReturnsSameValueUnchanged, #NegativeOrZero_WhenInputZero_ThrowsArgumentExceptionWithCapturedParameterName, #NegativeOrZero_WhenInputNegative_ThrowsArgumentExceptionWithCapturedParameterName, #Zero_WhenInputNonZero_ReturnsSameValueUnchanged, #Zero_WhenInputZero_ThrowsArgumentExceptionWithCapturedParameterName"
        status: pass
    human_judgment: false
  - id: D2
    description: "Default<T> guard against an uninitialized/default-value struct, proven type-agnostic across Guid and DateTime"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNumericTests.cs#Default_WhenGuidNonEmpty_ReturnsSameValueUnchanged, #Default_WhenGuidEmpty_ThrowsArgumentExceptionWithCapturedParameterName, #Default_WhenDateTimeNonDefault_ReturnsSameValueUnchanged, #Default_WhenDateTimeIsDefault_ThrowsArgumentExceptionWithCapturedParameterName"
        status: pass
    human_judgment: false

# Metrics
duration: 9min
completed: 2026-07-16
status: complete
---

# Phase 1 Plan 5: Numeric-Sign and Default-Value Guards Summary

**Generic Negative/NegativeOrZero/Zero numeric-sign guards plus a type-agnostic Default<T> guard against uninitialized struct values (Guid, DateTime), completing D-08's confirmed guard floor**

## Performance

- **Duration:** 9 min
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- `GuardAgainstNumericExtensions.cs` with `Negative<T>`, `NegativeOrZero<T>`, `Zero<T>` — all `IComparable<T>`-constrained generics comparing against `default(T)`, with correct per-method zero-boundary handling (Negative passes zero, NegativeOrZero and Zero both reject zero)
- `Default<T>` guard, `IEquatable<T>`-constrained, proven type-agnostic against both a `Guid` fixture and a `DateTime` fixture
- All four guards capture `parameterName` via `CallerArgumentExpression` and return the validated input unchanged on success (D-02/D-03)
- Exception messages interpolate only `parameterName`, never the raw numeric/struct value (T-1-02 mitigation)
- Full TDD RED → GREEN cycle for both tasks; test suite grew from 22 to 34 passing tests

## Task Commits

Each task was committed atomically (TDD RED/GREEN pairs):

1. **Task 1: Negative, NegativeOrZero, Zero**
   - `cb50583` (test) — 8 failing behavior-block tests, compile error confirms RED
   - `f41fb00` (feat) — implementation, 30/30 tests pass
2. **Task 2: Default\<T\>**
   - `5169930` (test) — 4 failing behavior-block tests, compile error confirms RED
   - `a3aff18` (feat) — implementation, 34/34 tests pass

_TDD tasks: test → feat commit pairs, as expected._

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNumericExtensions.cs` - `Negative<T>`, `NegativeOrZero<T>`, `Zero<T>`, `Default<T>` guard-clause extension methods
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNumericTests.cs` - 12 xUnit v3 `[Fact]` tests covering all pass/throw/boundary paths

## Decisions Made
- Followed 01-PATTERNS.md's `GuardAgainstNumericExtensions.cs` shape verbatim — no deviation from the researched pattern.
- Zero-boundary semantics implemented exactly per D-08: `Negative` treats zero as valid (returns it), `NegativeOrZero` and `Zero` both reject zero.

## Deviations from Plan

None - plan executed exactly as written.

## TDD Gate Compliance

Verified in git log: `test(01-05)` commits precede their corresponding `feat(01-05)` commits for both Task 1 (`cb50583` → `f41fb00`) and Task 2 (`5169930` → `a3aff18`). RED phase confirmed via compile failure (guard method did not exist); GREEN phase confirmed via full test-suite pass (30/30 then 34/34). No REFACTOR commits were needed — implementation matched the researched pattern with no cleanup required.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- D-08's confirmed numeric-sign and default-value guard floor is fully implemented and green (success criteria met).
- `Default<T>` proven type-agnostic across two distinct struct types (Guid, DateTime), ready to guard future Entity identity fields.
- Phase 01 now has 5 of 6 plans complete; only 01-06 (final guard family / rounded-out floor) remains before phase transition.

---
*Phase: 01-domain-shared-guardclauses*
*Completed: 2026-07-16*

## Self-Check: PASSED

All created files verified present on disk; all referenced commits (cb50583, f41fb00, 5169930, a3aff18, 4c92274) verified present in git log.
