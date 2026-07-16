---
phase: 02-domain-shared-result-result-t
plan: 3
subsystem: domain-shared
tags: [result, combinators, railway, dotnet, tdd]

# Dependency graph
requires:
  - phase: 02-domain-shared-result-result-t
    plan: 1
    provides: "Result sealed class, Error sealed record, ResultStatus enum, Failure(...) naming precedent"
  - phase: 02-domain-shared-result-result-t
    plan: 2
    provides: "Result<T> sealed class with fail-fast Value getter and matching factory set"
provides:
  - "Map: Result<TIn>->Result<TOut> and Result->Result<TOut> value-transform combinators, 4 sync/async shapes each (D-12, D-13)"
  - "Bind: Result<TIn>->Result<TOut> and Result->Result railway-chaining combinators, 4 sync/async shapes each, flattening not nesting (D-12, D-13)"
affects: ["02-04", "02-05", "02-06 (Ensure/Match, OnSuccess/OnFailure, Combine combinator plans building on Map/Bind's Left/Right/Both async convention)"]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Left/Right/Both async-overload split for every combinator (RESEARCH.md Pattern 2): sync, Task<Result> source with sync continuation, sync source with Task-returning continuation, both async — 4 shapes x 2 variants x 2 combinators = 32 methods total across ResultMapExtensions.cs and ResultBindExtensions.cs"
    - "Every async overload awaits its Task operand exactly once via ConfigureAwait(false); Left/Both-async overloads delegate to the sync/Right-async overload after awaiting rather than duplicating the short-circuit logic"
    - "Bind is the sole name for the D-12 'Bind/Then' combinator — no Then alias exists anywhere in Domain.Shared"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultBindTests.cs

key-decisions:
  - "Map/Bind's generic Result<TIn>->Result<TOut> variants propagate only .Errors on short-circuit, per this plan's documented short-circuit fidelity note — Status/Exception collapse to ResultStatus.Error/null on the type change (matches RESEARCH.md Pattern 2's own cited code example verbatim)"
  - "Bind's non-generic Result->Result variant returns the exact original failed Result instance (reference-equal) on short-circuit, fully preserving Status/Exception since no type change occurs"
  - "Bind is the sole implemented name for the railway combinator; Then is never added, per D-12's explicit two-names-one-mechanism resolution and RESEARCH.md's Recommended Project Structure naming only ResultBindExtensions.cs"

patterns-established:
  - "Test files use an explicit delegate-type cast, e.g. (Func<int, int>)(_ => throw ...), whenever a throw-only lambda is passed to an overloaded Map/Bind call site — bare throw-expression lambdas are ambiguous between the sync and Task-returning overloads since the compiler cannot infer delegate type from a throw statement alone"

requirements-completed: [PRIM-02]

coverage:
  - id: D1
    description: "Result<TIn>.Map(mapper) invokes mapper and returns a successful Result<TOut> on success; short-circuits without invoking mapper on failure, propagating Errors — across all 4 shapes"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs (Map_Generic_Sync_*, Map_Generic_LeftAsync_*, Map_Generic_RightAsync_*, Map_Generic_BothAsync_*)"
        status: pass
    human_judgment: false
  - id: D2
    description: "Result.Map(valueFactory) converts a successful non-generic Result into a successful Result<TOut>; short-circuits without invoking valueFactory on failure — across all 4 shapes"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs (Map_NonGeneric_Sync_*, Map_NonGeneric_LeftAsync_*, Map_NonGeneric_RightAsync_*, Map_NonGeneric_BothAsync_*)"
        status: pass
    human_judgment: false
  - id: D3
    description: "Result<TIn>.Bind(func) chains to func's own Result<TOut>, flattening (never Result<Result<TOut>>); short-circuits without invoking func on failure, propagating Errors"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultBindTests.cs (Bind_Generic_Sync_*, Bind_Generic_LeftAsync_*, Bind_Generic_RightAsync_*, Bind_Generic_BothAsync_*, Bind_Generic_Sync_WhenChainingTwoBindsAndFirstFails_*)"
        status: pass
    human_judgment: false
  - id: D4
    description: "Result.Bind(func) chains to func's own Result on success; on failure returns the original failed Result instance unchanged (reference-equal), preserving Status and Exception"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultBindTests.cs (Bind_NonGeneric_Sync_WhenSourceFails_NeverInvokesFuncAndReturnsExactSameFailedInstance and its 3 async counterparts)"
        status: pass
    human_judgment: false
  - id: D5
    description: "Every async shape awaits its Task operand exactly once via ConfigureAwait(false) and never invokes the mapper/continuation when the source has already failed"
    requirement: "PRIM-02"
    verification:
      - kind: unit
        ref: "SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs and ResultBindTests.cs (all *_RightAsync_WhenSourceFails_NeverInvokesOrAwaits* and *_BothAsync_WhenSourceFails_NeverInvokesOrAwaits* cases, using invocation counters)"
        status: pass
      - kind: unit
        ref: "ResultMapTests.cs#Map_Generic_RightAsync_WhenMapperThrows_ExceptionPropagatesAsFaultedTask, #Map_NonGeneric_BothAsync_WhenValueFactoryThrows_ExceptionPropagatesAsFaultedTask; ResultBindTests.cs#Bind_Generic_RightAsync_WhenFuncThrows_ExceptionPropagatesAsFaultedTask, #Bind_NonGeneric_BothAsync_WhenFuncThrows_ExceptionPropagatesAsFaultedTask"
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

duration: 18min
completed: 2026-07-16
status: complete
---

# Phase 2 Plan 3: Map and Bind Combinators Summary

**Railway-style Map (value transform, short-circuit on failure) and Bind (flatten-chain to another Result-returning operation) combinators for Result/Result<T>, each implemented across all 4 sync/async shapes (16 methods per file, 32 total), with Bind locked as the sole name for the D-12 "Bind/Then" mechanism.**

## Performance

- **Duration:** ~18 min
- **Tasks:** 2 completed
- **Files modified:** 4 (2 source, 2 test)

## Accomplishments

- `ResultMapExtensions.cs` declares exactly 8 `Map` overloads: generic `Result<TIn>` -> `Result<TOut>` (sync, Left-async, Right-async, Both-async) and non-generic `Result` -> `Result<TOut>` (same 4 shapes), each short-circuiting without invoking the mapper/valueFactory on failure and propagating the source's `Errors` into the new `Result<TOut>` (D-12, D-13).
- `ResultBindExtensions.cs` declares exactly 8 `Bind` overloads mirroring the same 2-variant/4-shape structure, flattening to `func`'s own `Result<TOut>`/`Result` on success (never re-wrapping — the railway mechanism) and short-circuiting on failure; the non-generic variant returns the exact original failed `Result` instance (reference-equal) on short-circuit, fully preserving `Status`/`Exception`.
- Every Right-async and Both-async overload confirmed, via invocation-counting delegates, to never invoke or await the mapper/continuation once the source has already failed (T-2-07).
- Every async overload confirmed to propagate an exception thrown inside the mapper/continuation as a faulted `Task` to the awaiting caller, rather than swallowing it (T-2-09).
- `Bind` is documented inline (class-level XML remarks) as the sole name for D-12's "Bind/Then" combinator — no `Then` alias exists anywhere in `Domain.Shared`.
- Full `Results/` test suite (all of plans 02-01, 02-02, and this plan) — 93 tests total — passes green together; `Domain.Shared.csproj` confirmed still zero `PackageReference` elements.

## Task Commits

Each task was committed atomically:

1. **Task 1: Map combinator — Result\<TIn\>->Result\<TOut\> and Result->Result\<TOut\> variants, all 4 shapes each** - `f551fc1` (feat)
2. **Task 2: Bind combinator — Result\<TIn\>->Result\<TOut\> and Result->Result variants, all 4 shapes each** - `6cf1822` (feat)

## Files Created/Modified

- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs` - 8 `Map` extension methods (2 variants x 4 shapes), XML remarks documenting the file-per-combinator convention and short-circuit fidelity note
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs` - 8 `Bind` extension methods (2 variants x 4 shapes), XML remarks documenting the Bind-only naming decision, short-circuit fidelity note, and flatten/railway behavior
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs` - 18 tests covering both `Map` variants across all 4 shapes plus 2 exception-propagation cases
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultBindTests.cs` - 22 tests covering both `Bind` variants across all 4 shapes, the flatten/chain-short-circuit case, the non-generic reference-equality preservation case, and 2 exception-propagation cases

## Decisions Made

- Followed this plan's explicit short-circuit fidelity note verbatim: the generic `Result<TIn>` -> `Result<TOut>` variants of both `Map` and `Bind` propagate only `.Errors` on short-circuit (not `Status`/`Exception`), matching RESEARCH.md Pattern 2's own cited code example. This is a locked, documented simplification, not a gap to "fix."
- `Bind` confirmed as the sole name for the D-12 "Bind/Then" combinator; no `Then` method was added anywhere, per the plan's explicit scope boundary.
- Test files needed an explicit delegate-type cast (e.g. `(Func<int, int>)(_ => throw ...)`) whenever a throw-only lambda was passed to an overloaded `Map`/`Bind` call site, since the compiler cannot disambiguate a bare `throw`-expression lambda between the sync and `Task`-returning overloads (CS0121 ambiguous call). This pattern is now established for any future combinator test file exercising Left/Right/Both-async overload sets with throwing delegates.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] Throw-only lambda arguments caused CS0121 ambiguous-overload errors against Left/Right/Both-async overload sets**
- **Found during:** Task 1 (first surfaced), same pattern also applied in Task 2
- **Issue:** Test cases asserting "func/mapper is never invoked on failure" used a bare `_ => throw new InvalidOperationException(...)` lambda passed directly to an overloaded `Map`/`Bind` call site (e.g. `result.Map<int, int>(_ => throw ...)`). Because a `throw`-expression-bodied lambda can implicitly convert to either `Func<TIn, TOut>` or `Func<TIn, Task<TOut>>`, the C# compiler could not select between the sync and Right-async overloads, producing CS0121 ambiguous-call errors at 4 call sites in `ResultMapTests.cs`.
- **Fix:** Cast each throw-only lambda explicitly to its intended delegate type before the call, e.g. `result.Map((Func<int, int>)(_ => throw new InvalidOperationException(...)))`. This disambiguates the overload resolution without changing test intent or introducing any production-code change.
- **Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs` (applied proactively to the equivalent throw-only cases already written in `ResultBindTests.cs` for Task 2, avoiding the same compile error there).
- **Verification:** `dotnet build` succeeds with 0 errors; all 18 `ResultMapTests` and 22 `ResultBindTests` pass.
- **Committed in:** `f551fc1` (Task 1); Task 2's `ResultBindTests.cs` was written with the cast pattern already applied, so no separate fix commit was needed for `6cf1822`.

---

**Total deviations:** 1 auto-fixed (Rule 3), applied consistently across both task's test files.
**Impact on plan:** No scope creep — a test-file-only compile-time disambiguation fix, no production code or behavior affected.

## Issues Encountered

None beyond the throw-only-lambda ambiguity documented above. The prior plans' note about MTP's `--filter-class`/`--filter-namespace` flags (not `--filter`) was applied correctly throughout this plan's verification.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

`Map` and `Bind` are complete, tested (93 total tests green in `Results/`), and compiling with zero third-party package references. Wave 3's sibling combinator plans (02-04 Ensure/Match, 02-05 OnSuccess/OnFailure, 02-06 Combine) now have both the `Result`/`Result<T>` foundation (02-01, 02-02) and the verified Left/Right/Both async-overload convention (this plan) to extend consistently.

No blockers.

---
*Phase: 02-domain-shared-result-result-t*
*Completed: 2026-07-16*

## Self-Check: PASSED

All 4 created files (`ResultMapExtensions.cs`, `ResultBindExtensions.cs`, `ResultMapTests.cs`, `ResultBindTests.cs`) verified present on disk; both task commit hashes (`f551fc1`, `6cf1822`) verified present in git log.
