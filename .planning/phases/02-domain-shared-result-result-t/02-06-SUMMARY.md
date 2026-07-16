---
phase: 02-domain-shared-result-result-t
plan: 6
subsystem: domain
tags: [result, domain-shared, dotnet, tdd, combine]

# Dependency graph
requires:
  - phase: 02-domain-shared-result-result-t (plan 01)
    provides: sealed Result class with Success/Failure/Invalid/NotFound/Conflict/Forbidden/Unauthorized/Unavailable/CriticalError static factories
  - phase: 02-domain-shared-result-result-t (plan 02)
    provides: sealed Result<T> class mirroring Result's factory set with fail-fast Value getter
provides:
  - "Result.Combine(params Result[] results) — all-or-nothing batch aggregator, full error union on any failure"
  - "Result.Combine<T>(params Result<T>[] results) — generic overload mirroring the same semantics, always returning non-generic Result"
affects: [phase-03-domain-shared-smartenum-t]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Batch aggregator (Combine) placed as additional static methods on the sealed Result class rather than an extension-method file, since params-array-of-the-extended-type has no single instance receiver to extend"
    - "Two separate Combine overloads (Result[] and Result<T>[]) instead of an inheritance trick, preserving D-16's sealed-class requirement on both Result and Result<T>"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs
  modified:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs

key-decisions:
  - "Combine/Combine<T> reuse the existing Failure(params Error[]) factory to construct the aggregate failed Result, never a method literally named Error, per plan 02-01's CS0102 naming resolution"
  - "Combine<T> always returns the non-generic Result type, never Result<T>, since there is no single value to select from N independent successful Result<T> inputs"
  - "Neither overload performs per-element null checks on the results array — only the array reference itself is guarded via Guard.Against.Null, mirroring CSharpFunctionalExtensions' actual Combine.cs shape; a null element is a programmer error surfaced naturally as a NullReferenceException (D-05)"

patterns-established:
  - "All-or-nothing batch aggregation over independent Result/Result<T> inputs, distinct from Bind's short-circuit-on-first-failure chaining"

requirements-completed: [PRIM-02]

coverage:
  - id: D1
    description: "Result.Combine(params Result[] results) succeeds only when every input succeeds (including empty batch), and on any failure returns the flattened ordered union of every failed input's Errors"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenCalledWithZeroArguments_ReturnsSuccess"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenAllInputsSucceed_ReturnsSuccess"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenOneInputFails_ReturnsFailureWithThatErrorOnly"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenMultipleInputsFail_ReturnsFlattenedOrderedUnionOfAllErrors"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenFailuresSpanDifferentStatuses_AggregatesAllFailedErrorsAndIgnoresSuccesses"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#Combine_WhenResultsArrayIsNull_ThrowsArgumentNullException"
        status: pass
    human_judgment: false
  - id: D2
    description: "Result.Combine<T>(params Result<T>[] results) mirrors the non-generic Combine's all-or-nothing/full-error-union behavior, always returning the non-generic Result type"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenCalledWithZeroArguments_ReturnsSuccess"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenAllInputsSucceed_ReturnsSuccess"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenOneInputFails_ReturnsFailureWithThatErrorOnly"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenMultipleInputsFail_ReturnsFlattenedOrderedUnionOfAllErrors"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenAllInputsSucceed_ReturnsNonGenericResultTypeNotResultOfT"
        status: pass
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs#CombineOfT_WhenResultsArrayIsNull_ThrowsArgumentNullException"
        status: pass
    human_judgment: false

duration: 14min
completed: 2026-07-16
status: complete
---

# Phase 02 Plan 6: Result.Combine Batch Aggregator Summary

**Result.Combine(params Result[]) and Result.Combine<T>(params Result<T>[]) — all-or-nothing batch aggregators with full error-union-on-any-failure, added as two static methods on the existing sealed Result class**

## Performance

- **Duration:** 14 min
- **Started:** 2026-07-16T21:15:00Z
- **Completed:** 2026-07-16T21:29:00Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- `Result.Combine(params Result[] results)` — non-generic all-or-nothing aggregator; returns `Success()` when zero inputs fail (including an empty argument list), otherwise `Failure(...)` with the flattened, ordered union of every failed input's `Errors`
- `Result.Combine<T>(params Result<T>[] results)` — generic overload mirroring the identical semantics for `Result<T>` inputs, always returning the non-generic `Result` type
- Both overloads guard against a null `results` array via `Guard.Against.Null`, matching the `CriticalError(null!)` null-guard precedent from plan 02-01
- Neither overload introduces an inheritance relationship between `Result` and `Result<T>`, preserving D-16's sealed-class requirement on both types

## Task Commits

Each task was committed as an RED/GREEN TDD pair (both tasks' behavior was implemented together since Task 2 extends the same test file and the same class edit):

1. **Task 1 + Task 2 (RED): failing tests for both Combine overloads** - `e526430` (test)
2. **Task 1 + Task 2 (GREEN): Result.Combine and Result.Combine<T> implementation** - `ee4dfe9` (feat)

**Plan metadata:** (this commit) `docs(02-06): complete Result.Combine batch aggregator plan`

_Note: Both tasks were combined into a single RED commit and single GREEN commit since they modify the same file/test-file pair and Task 2's action explicitly instructs adding the generic overload "immediately after" Task 1's method in the same edit pass._

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs` - 12 xUnit v3 facts covering both Combine overloads' empty-batch, all-success, single-failure, multi-failure ordered-union, mixed-status, null-array-guard, and generic-return-type cases
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs` - Added `Combine(params Result[] results)` and `Combine<T>(params Result<T>[] results)` static methods with XML doc remarks explaining the all-or-nothing/full-error-union semantics and the placement/overload-strategy rationale

## Decisions Made
- Combined Task 1 and Task 2 into a single RED and single GREEN commit pair rather than four separate task commits, since both tasks touch the exact same two files and Task 2's action explicitly builds on Task 1's method in the same edit pass (mirroring the precedent set in Phase 01 Plan 06)
- Reused `Failure(params Error[] errors)` for the aggregate failure construction in both overloads, never a method literally named `Error`, consistent with plan 02-01's CS0102 naming resolution
- Followed RESEARCH.md Pitfall 4's separate-overloads strategy exactly — no `Result : Result<Result>` inheritance trick, since both types must remain sealed per D-16

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Phase 02 (Result/Result<T>) is now complete across all 6 plans (02-01 through 02-06): construction/factories, Result<T> value access, Map/Bind, Ensure/Match, OnSuccess/OnFailure, and Combine.
- All 155 tests in the `Results/` test folder pass; `Domain.Shared.csproj` has zero `PackageReference` elements.
- Phase 02 moves to verification next; Phase 03 (SmartEnum<T>) is queued after that.

---
*Phase: 02-domain-shared-result-result-t*
*Completed: 2026-07-16*

## Self-Check: PASSED

- FOUND: SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs
- FOUND: SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs
- FOUND: commit e526430 (test)
- FOUND: commit ee4dfe9 (feat)
