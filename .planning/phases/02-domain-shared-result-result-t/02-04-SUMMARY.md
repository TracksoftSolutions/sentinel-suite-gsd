---
phase: 02-domain-shared-result-result-t
plan: 4
subsystem: domain-shared
tags: [result, combinators, railway, validation, dotnet, tdd]

# Dependency graph
requires:
  - phase: 02-domain-shared-result-result-t
    plan: 1
    provides: "Result sealed class, Error sealed record, ResultStatus enum, Failure(...) naming precedent"
  - phase: 02-domain-shared-result-result-t
    plan: 2
    provides: "Result<T> sealed class with fail-fast Value getter and matching factory set"
  - phase: 02-domain-shared-result-result-t
    plan: 3
    provides: "Left/Right/Both async-overload split convention confirmed for Map/Bind, mirrored here for Ensure/Match"
provides:
  - "Ensure: Result/Result<T> validation-gate combinator, 4 sync/async shapes each, converting predicate-false to ResultStatus.Invalid (D-12, D-13)"
  - "Match: Result/Result<T> terminal-value-collapse combinator, 4 sync/async shapes each, with onFailure always receiving the complete Errors list (D-12, D-13)"
affects: ["02-05 (OnSuccess/OnFailure), 02-06 (Combine) — sibling Wave 3 combinator plans sharing the same async-overload convention"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Ensure/Match follow the same Left/Right/Both async-overload split as Map/Bind (RESEARCH.md Pattern 2): sync, Task<Result> source with sync continuation, sync source with Task-returning continuation, both async"
    - "Match narrows its async overload set relative to Bind/Ensure: for Right-async/Both-async shapes, onSuccess and onFailure are always both sync or both Task<TOut>-returning together, never mixed, avoiding a 4x4 handler-combination explosion (RESEARCH.md Open Question 1, resolved and documented in ResultMatchExtensions.cs class remarks)"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultEnsureTests.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMatchTests.cs

key-decisions:
  - "Ensure's predicate-false branch always produces ResultStatus.Invalid via Result.Invalid(error)/Result<T>.Invalid(error), never Error/Failure status — locked by this plan's own Objective section, consistent with D-08's status vocabulary"
  - "Ensure implemented symmetrically on both Result (Func<bool> predicate, since Result carries no value to test) and Result<T> (Func<T, bool> predicate), completing the combinator's factory-parity symmetry (D-10)"
  - "Match's Right-async/Both-async shapes require onSuccess and onFailure to be both-sync or both-Task<TOut>-returning together — never mixed — per this plan's explicit resolution of RESEARCH.md Open Question 1, documented in ResultMatchExtensions.cs's class-level XML remarks so a future contributor doesn't 'complete' a mixed-sync/async overload by analogy to Bind"
  - "Match's onFailure handler is always typed Func<IReadOnlyList<Error>, TOut> (or its async counterpart) — receives the complete Errors list, never just the first Error, per D-01/D-04"

patterns-established:
  - "Test files fixed an obsolete-API warning (Assert.Throws<T>(Func<Task>) is obsolete for testing sync code returning a value) by wrapping the assertion target in a statement-bodied Action lambda (`() => { var _ = ensured.Value; }`) instead of an expression-bodied Func<T> lambda, disambiguating the overload cleanly"

requirements-completed: [PRIM-02]

coverage:
  - id: D1
    description: "Ensure short-circuits (returns the original failed Result/Result<T> unchanged without invoking the predicate) when the source is already a failure, for all 4 sync/async shapes on both Result and Result<T>"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultEnsureTests.cs (*_WhenSourceFails_ShortCircuitsWithoutInvokingPredicate cases, both generic and non-generic, all 4 shapes)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Ensure converts a successful Result/Result<T> into ResultStatus.Invalid carrying the supplied Error when the predicate evaluates false, and leaves it unchanged when true, for all 4 shapes on both types"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "ResultEnsureTests.cs (*_WhenSourceSucceedsAndPredicateTrue_* and *_WhenSourceSucceedsAndPredicateFalse_* cases across sync/LeftAsync/RightAsync/BothAsync, generic and non-generic)"
        status: pass
    human_judgment: false
  - id: D3
    description: "Match collapses a successful Result/Result<T> into the onSuccess handler's output (passed the unwrapped Value for Result<T>), and a failed Result/Result<T> into the onFailure handler's output, receiving the full Errors list"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMatchTests.cs (Match_Generic_Sync_*, Match_NonGeneric_Sync_* and their async counterparts)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Every async overload (Left-async, Right-async, Both-async) of Ensure and Match on both Result and Result<T> produces behavior identical to its synchronous counterpart when awaited"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "ResultEnsureTests.cs and ResultMatchTests.cs (*_LeftAsync_*, *_RightAsync_*, *_BothAsync_* cases, including was-called-flag assertions confirming exactly one handler invoked per Match call)"
        status: pass
    human_judgment: false
  - id: D5
    description: "Domain.Shared remains free of third-party package references after this plan"
    requirement: "PRIM-02"
    verification:
      - kind: other
        ref: "grep -c 'PackageReference' SentinelSuite.Framework.Domain.Shared.csproj => 0; dotnet build => 0 Warning(s), 0 Error(s)"
        status: pass
    human_judgment: false

duration: 22min
completed: 2026-07-16
status: complete
---

# Phase 2 Plan 4: Ensure and Match Combinators Summary

**Railway-style Ensure (predicate gate converting success to ResultStatus.Invalid on failure) and Match (terminal collapse to a single value via exactly one of two handlers, with the full Errors list on failure) for Result/Result<T>, each across all 4 sync/async shapes (16 methods per file, 32 total), with Match's async overload set deliberately narrowed per RESEARCH.md Open Question 1.**

## Performance

- **Duration:** ~22 min
- **Tasks:** 2 completed
- **Files modified:** 4 (2 source, 2 test)

## Accomplishments

- `ResultEnsureExtensions.cs` declares exactly 8 `Ensure` overloads: 4 on `Result` (`Func<bool>`/`Func<Task<bool>>` predicates, sync/Left-async/Right-async/Both-async) and 4 on `Result<T>` (`Func<T, bool>`/`Func<T, Task<bool>>` predicates, same 4 shapes) — every overload short-circuits a failed source without invoking the predicate, and converts predicate-false into `ResultStatus.Invalid` carrying exactly the supplied `Error`.
- `ResultMatchExtensions.cs` declares exactly 8 `Match` overloads mirroring the same 2-variant/4-shape structure — every overload invokes exactly one of `onSuccess`/`onFailure`, never both, never neither, and `onFailure` always receives the complete `IReadOnlyList<Error>` (not just the first `Error`).
- Match's Right-async/Both-async shapes deliberately require both handlers to share sync/async-ness (never mixed), resolving RESEARCH.md Open Question 1 and documented inline in `ResultMatchExtensions.cs`'s class-level XML remarks.
- Full `Results/` test suite (plans 02-01, 02-02, 02-03, and this plan) — 123 tests total — passes green together; `Domain.Shared.csproj` confirmed still zero `PackageReference` elements.

## Task Commits

Each task was executed as a TDD RED/GREEN pair, committed atomically:

1. **Task 1: Ensure combinator** — RED `4f9c4bf` (test), GREEN `b3f1a9f` (feat)
2. **Task 2: Match combinator** — RED `7a0a996` (test), GREEN `e104d5b` (feat)

## Files Created/Modified

- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs` - 8 `Ensure` extension methods (2 variants x 4 shapes), XML remarks documenting the `ResultStatus.Invalid` failure-status decision and short-circuit rule
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs` - 8 `Match` extension methods (2 variants x 4 shapes), XML remarks documenting exactly-one-handler dispatch, the complete-Errors-list guarantee, and the async-overload narrowing decision
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultEnsureTests.cs` - 16 tests covering both `Ensure` variants across all 4 shapes, short-circuit-without-invoking-predicate cases, and predicate-true/predicate-false branches
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMatchTests.cs` - 14 tests covering both `Match` variants across all 4 shapes, complete-Errors-list assertions, and exactly-one-handler-invoked assertions via was-called flags

## Decisions Made

- Followed this plan's explicit Objective-section decision verbatim: `Ensure`'s predicate-false branch always produces `ResultStatus.Invalid`, never `Error`/`Failure` — a locked, documented choice, not a gap to "fix."
- Implemented `Ensure` symmetrically on non-generic `Result` (value-less `Func<bool>` predicate) and `Result<T>` (value-taking `Func<T, bool>` predicate), completing factory-parity symmetry per D-10.
- `Match`'s Right-async/Both-async shapes narrow to both-handlers-sync-or-both-async-together, resolving RESEARCH.md Open Question 1 exactly as the plan's Objective section specified — no independent 4x4 handler matrix was built.
- Test files used a statement-bodied `Action` lambda (`() => { var _ = ensured.Value; }`) rather than an expression-bodied `Func<T>` lambda when asserting `Assert.Throws<InvalidOperationException>` against `.Value`, avoiding an obsolete-API overload-resolution ambiguity (`Assert.Throws<T>(Func<Task>)` is obsolete for testing sync code) — same category of overload-disambiguation issue plan 02-03 encountered with throw-only lambdas, different mechanism.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] `Assert.Throws<T>(Func<Task>)` obsolete-API compile error on a sync value-access assertion**
- **Found during:** Task 1, initial RED test write
- **Issue:** `Assert.Throws<InvalidOperationException>(() => ensured.Value)` triggered CS0619 (`Assert.Throws<T>(Func<Task>)` is obsolete — must use `Assert.ThrowsAsync<T>` for async code) because the compiler resolved the expression-bodied `Func<int>` lambda against an unintended overload.
- **Fix:** Rewrote the lambda as a statement-bodied `Action` (`() => { var _ = ensured.Value; }`), which unambiguously resolves to `Assert.Throws<T>(Action)`.
- **Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultEnsureTests.cs`
- **Verification:** `dotnet build` succeeds with 0 errors/warnings; all 16 `ResultEnsureTests` pass.
- **Committed in:** `4f9c4bf` (Task 1 RED commit already included the fix — no separate fix commit needed).

---

**Total deviations:** 1 auto-fixed (Rule 3), test-file-only compile-time disambiguation fix.
**Impact on plan:** No scope creep — no production code or behavior affected.

## Issues Encountered

None beyond the obsolete-API disambiguation documented above. MTP's `--filter-class`/`--filter-namespace` flags (not `--filter`) were used correctly throughout this plan's verification, per the prior-wave note.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

`Ensure` and `Match` are complete, tested (123 total tests green in `Results/`), and compiling with zero third-party package references. Sibling Wave 3 plans (02-05 OnSuccess/OnFailure, 02-06 Combine) now have the confirmed Left/Right/Both async-overload convention (from 02-03 and reaffirmed here) plus this plan's async-overload-narrowing precedent for any future combinator that collapses to a plain value via multiple handlers.

No blockers.

---
*Phase: 02-domain-shared-result-result-t*
*Completed: 2026-07-16*

## Self-Check: PASSED

All 4 created files (`ResultEnsureExtensions.cs`, `ResultMatchExtensions.cs`, `ResultEnsureTests.cs`, `ResultMatchTests.cs`) verified present on disk; all 4 task commit hashes (`4f9c4bf`, `b3f1a9f`, `7a0a996`, `e104d5b`) verified present in git log.
