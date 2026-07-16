---
phase: 02-domain-shared-result-result-t
plan: 5
subsystem: domain-shared
tags: [result, combinators, railway, side-effect, dotnet, tdd]

# Dependency graph
requires:
  - phase: 02-domain-shared-result-result-t
    plan: 1
    provides: "Result sealed class, Error sealed record, ResultStatus enum, Failure(...) naming precedent"
  - phase: 02-domain-shared-result-result-t
    plan: 2
    provides: "Result<T> sealed class with fail-fast Value getter and matching factory set"
provides:
  - "OnSuccess: Result/Result<T> side-effect hook firing on success, 4 sync/async shapes each, never transforming the source (D-12, D-13)"
  - "OnFailure: Result/Result<T> side-effect hook firing on failure, 4 sync/async shapes each, never referencing Value (D-06, D-12, D-13)"
affects: ["02-06 (Combine) — sibling Wave 3 combinator plan sharing the same async-overload convention"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "OnSuccess/OnFailure follow the same Left/Right/Both async-overload split as Map/Bind/Ensure/Match (RESEARCH.md Pattern 2): sync, Task<Result> source with sync continuation, sync source with Task-returning continuation, both async"
    - "Value/error-awareness axis narrowed per-verb rather than per-shape: OnSuccess<T> receives Value (safe once IsSuccess is confirmed); OnSuccess (non-generic) and OnFailure (both types) take a no-argument Action/Func<Task> — resolves RESEARCH.md Open Question 1 explicitly rather than doubling the overload surface for an errors-aware OnFailure variant"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOnSuccessOnFailureTests.cs

key-decisions:
  - "OnSuccess on Result<T> receives the value (Action<T>/Func<T, Task>) since Value is always safe to read once IsSuccess is confirmed true; OnFailure on both types and OnSuccess on non-generic Result take a no-argument Action/Func<Task> — locked by this plan's Objective section, resolving RESEARCH.md Open Question 1 rather than leaving it ambiguous"
  - "No errors-aware OnFailure(Action<IReadOnlyList<Error>>) overload was added — callers needing .Errors/.Error inside the failure hook already have the source Result/Result<T> in scope via the chained variable, so a parameterized overload would duplicate information already available. Combinator surface stays at 8 methods per verb (16 total)"
  - "Neither combinator ever constructs a new Result/Result<T> — every overload returns the exact same instance it received (verified via reference-equality assertions), distinguishing OnSuccess/OnFailure from Map/Bind/Ensure/Match which do transform"

requirements-completed: [PRIM-02]

coverage:
  - id: D1
    description: "OnSuccess invokes its action exactly once when the source Result/Result<T> is successful, and never when failed, across all 4 sync/async shapes on both types"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOnSuccessOnFailureTests.cs (OnSuccess_* cases, sync/LeftAsync/RightAsync/BothAsync, generic and non-generic)"
        status: pass
    human_judgment: false
  - id: D2
    description: "OnFailure invokes its action exactly once when the source Result/Result<T> is failed, and never when successful, across all 4 sync/async shapes on both types"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "ResultOnSuccessOnFailureTests.cs (OnFailure_* cases, sync/LeftAsync/RightAsync/BothAsync, generic and non-generic)"
        status: pass
    human_judgment: false
  - id: D3
    description: "Both combinators return the original Result/Result<T> instance unchanged (reference equality) regardless of whether the action fired"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "ResultOnSuccessOnFailureTests.cs (Assert.Same(result, returned) assertions across every case)"
        status: pass
    human_judgment: false
  - id: D4
    description: "No OnFailure overload on Result<T> ever reads Value, consistent with D-06's fail-fast guarantee"
    requirement: "PRIM-02"
    verification:
      - kind: other
        ref: "Manual grep across every OnFailure method body in ResultOnSuccessOnFailureExtensions.cs confirms zero .Value references"
        status: pass
    human_judgment: false
  - id: D5
    description: "Passing a null action/func to any OnSuccess or OnFailure overload throws ArgumentNullException before any Result state is inspected"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "ResultOnSuccessOnFailureTests.cs (OnSuccess_NonGeneric_Sync_WhenActionIsNull_ThrowsArgumentNullException, OnFailure_NonGeneric_Sync_WhenActionIsNull_ThrowsArgumentNullException)"
        status: pass
    human_judgment: false
  - id: D6
    description: "Domain.Shared remains free of third-party package references after this plan"
    requirement: "PRIM-02"
    verification:
      - kind: other
        ref: "grep -c 'PackageReference' SentinelSuite.Framework.Domain.Shared.csproj => 0; dotnet build => 0 Warning(s), 0 Error(s)"
        status: pass
    human_judgment: false

duration: 16min
completed: 2026-07-16
status: complete
---

# Phase 2 Plan 5: OnSuccess and OnFailure Combinators Summary

**Railway-style OnSuccess/OnFailure side-effect hooks (D-12) for Result/Result<T> — 16 methods total (8 per verb x 4 sync/async shapes), each returning the original instance unchanged, with the value/error-awareness axis deliberately narrowed to one shape per verb (RESEARCH.md Open Question 1).**

## Performance

- **Duration:** ~16 min
- **Tasks:** 2 completed
- **Files modified:** 2 (1 source, 1 test)

## Accomplishments

- `ResultOnSuccessOnFailureExtensions.cs` declares exactly 16 public static combinator overloads: 8 `OnSuccess` (4 on `Result` with `Action`/`Func<Task>`, 4 on `Result<T>` with `Action<T>`/`Func<T, Task>`) and 8 `OnFailure` (4 on `Result`, 4 on `Result<T>`, all sharing the identical no-argument `Action`/`Func<Task>` signature).
- Every overload validates its delegate via `Guard.Against.Null` before inspecting `Result`/`Result<T>` state, and returns the exact same instance it received — verified with `Assert.Same` reference-equality assertions across every test case, never constructing a new `Result`.
- No `OnFailure` overload on `Result<T>` ever references `Value` anywhere in its body — confirmed both by manual code inspection and by the fact `Value` throws on a failed instance (D-06), so any accidental read would fail the tests immediately.
- The value/error-awareness narrowing decision (RESEARCH.md Open Question 1) is documented both in this plan's Objective section and in the extension class's own XML remarks, matching the precedent set by 02-04's `Match` narrowing.
- Full `Results/` test suite (plans 02-01 through 02-05) — 143 tests total — passes green together; `Domain.Shared.csproj` confirmed still zero `PackageReference` elements.

## Task Commits

Each task was executed as a TDD RED/GREEN pair, committed atomically:

1. **Task 1: OnSuccess combinator** — RED `36d1573` (test), GREEN `5b05c68` (feat)
2. **Task 2: OnFailure combinator** — RED `0688527` (test), GREEN `eefc030` (feat)

## Files Created/Modified

- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs` - 16 extension methods (`OnSuccess` x8, `OnFailure` x8), XML remarks documenting the D-12/D-13 sync/async matrix and the Open Question 1 narrowing resolution
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOnSuccessOnFailureTests.cs` - 20 tests covering both combinators across all 4 shapes, both types, reference-equality assertions, and null-action `ArgumentNullException` cases

## Decisions Made

- Followed this plan's explicit Objective-section decision verbatim: `OnSuccess` on `Result<T>` receives the value, `OnFailure` on both types (and `OnSuccess` on non-generic `Result`) take a no-argument delegate — a locked, documented choice resolving RESEARCH.md Open Question 1, not an oversight or a gap to "complete" later with an errors-aware overload.
- Neither combinator transforms the value or the `Result`/`Result<T>` — every overload returns the original instance, distinguishing this plan's pair from `Map`/`Bind` (02-03) and `Ensure`/`Match` (02-04), which do construct new instances.
- Extended the existing class's XML remarks (rather than duplicating documentation) with a confirming note in Task 2 that `OnFailure`'s no-argument shape is implemented exactly as Task 1 anticipated.

## Deviations from Plan

### Auto-fixed Issues

None — plan executed as written for both tasks' production code and test coverage.

### Verification Script Note (not a code defect)

**1. [Informational] Plan's automated overload-count grep also matches the class declaration line**

- **Found during:** Task 1 and Task 2 verification
- **Issue:** The plan's `<verify>` automated command (`grep -c 'public static.*OnSuccess'` / `grep -c 'public static.*OnFailure'`) also matches the line `public static class ResultOnSuccessOnFailureExtensions`, because the class name itself contains both substrings "OnSuccess" and "OnFailure". This makes the raw grep count 9 instead of the actual 8 real method overloads per verb.
- **Resolution:** Manually verified the true overload count is exactly 8 per verb (16 total) by listing every matching line and excluding the class declaration — confirmed via `grep -n` inspection. No production code change was needed; this is a pre-existing quirk in the plan's own verification one-liner, not a defect in `ResultOnSuccessOnFailureExtensions.cs`.
- **Files modified:** None (verification-only finding).
- **Impact:** None on scope or correctness — all acceptance criteria (exactly 8 `OnSuccess` overloads, exactly 8 `OnFailure` overloads, zero `Value` references in `OnFailure` bodies) are independently confirmed true.

---

**Total deviations:** 0 auto-fixed; 1 informational verification-script note (no code impact).

## Issues Encountered

MTP's `--filter-class` required the fully-qualified type name (`SentinelSuite.Framework.Domain.Shared.Tests.Results.ResultOnSuccessOnFailureTests`) — a bare short class name returned "Zero tests ran" with no error, consistent with the prior-wave note's `--filter-namespace`/`--filter-class` guidance. No other issues.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

`OnSuccess` and `OnFailure` are complete, tested (143 total tests green in `Results/`), and compiling with zero third-party package references. Sibling Wave 3 plan 02-06 (Combine) now has the confirmed Left/Right/Both async-overload convention plus this plan's per-verb (rather than per-shape) narrowing precedent for any future combinator whose handler signature varies by success/failure rather than by sync/async-ness.

No blockers.

---
*Phase: 02-domain-shared-result-result-t*
*Completed: 2026-07-16*

## Self-Check: PASSED

Both created files (`ResultOnSuccessOnFailureExtensions.cs`, `ResultOnSuccessOnFailureTests.cs`) verified present on disk; all 4 task commit hashes (`36d1573`, `5b05c68`, `0688527`, `eefc030`) verified present in git log.
