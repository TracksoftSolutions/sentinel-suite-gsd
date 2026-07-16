---
phase: 01-domain-shared-guardclauses
plan: 03
subsystem: domain-kernel
tags: [guard-clauses, dotnet, domain-shared, tdd, callerargumentexpression, notnull]

# Dependency graph
requires:
  - phase: 01-domain-shared-guardclauses (01-01, 01-02)
    provides: MTP-native xUnit v3 test project scaffolding; IGuardClause marker interface + Guard.Against static entry point
provides:
  - "Guard.Against.Null<T> (2 overloads: class-constrained returning T, struct-constrained accepting Nullable<T> and returning unwrapped T)"
  - "Guard.Against.NullOrWhiteSpace(string?) delegating to Null first"
  - "Guard.Against.NullOrEmpty (2 overloads: string, IEnumerable<T>) delegating to Null first"
  - "13 passing xUnit v3 tests (pass + throw paths) for the entire Null guard family"
affects: [01-04, 01-05, 01-06, future Entity/EntityAssociation constructors across all 26 modules]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "GuardAgainst{Concept}Extensions naming convention (established in 01-02, extended here)"
    - "[NotNull] on input parameter + non-nullable declared return type T (never [return: NotNull]) for compiler nullable-flow analysis"
    - "[CallerArgumentExpression(nameof(input))] always targets the ordinary input parameter, never the this IGuardClause receiver"
    - "Guards that can reject empty/whitespace/empty-collection input delegate to Guard.Against.Null first so null throws ArgumentNullException before the emptiness check runs"
    - "Exception messages are fixed string literals referencing only parameterName, never interpolating the rejected value (Information-Disclosure mitigation)"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNullExtensions.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs
  modified: []

key-decisions:
  - "Followed RESEARCH.md/PATTERNS.md exactly: Null<T> as two overloads (where T : class / where T : struct), no [return: NotNull] anywhere in the file"
  - "NullOrWhiteSpace and both NullOrEmpty overloads delegate to Guard.Against.Null first per Pattern 2, guaranteeing ArgumentNullException (not ArgumentException) on null input"

patterns-established:
  - "Two-overload generic Null<T> pattern: this is the template every future nullable-generic guard in this kernel should follow"
  - "Delegate-to-Null-first pattern for any 'stronger' guard (NullOrWhiteSpace, NullOrEmpty) built on top of the null guard"

requirements-completed: [PRIM-01]

coverage:
  - id: D1
    description: "Guard.Against.Null<T> returns valid reference-type and nullable-value-type input unchanged, and throws ArgumentNullException with the correct captured parameter name on null input"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#Null_WhenReferenceTypeInputProvided_ReturnsSameInstanceUnchanged"
        status: pass
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#Null_WhenReferenceTypeInputIsNull_ThrowsArgumentNullExceptionWithCapturedParameterName"
        status: pass
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#Null_WhenNullableValueTypeInputProvided_ReturnsUnwrappedValue"
        status: pass
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#Null_WhenNullableValueTypeInputIsNull_ThrowsArgumentNullException"
        status: pass
    human_judgment: false
  - id: D2
    description: "Guard.Against.NullOrWhiteSpace/NullOrEmpty reject empty/whitespace-only input with ArgumentException, never leaking the raw rejected value in the exception message"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#NullOrWhiteSpace_WhenWhitespaceOnlyInputProvided_ThrowsArgumentExceptionWithoutLeakingValue"
        status: pass
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#NullOrEmpty_WhenEmptyStringProvided_ThrowsArgumentException"
        status: pass
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs#NullOrEmpty_WhenEmptyCollectionProvided_ThrowsArgumentException"
        status: pass
    human_judgment: false
  - id: D3
    description: "D-02/D-03 conventions honored: every guard method here uses CallerArgumentExpression (no explicit nameof(x) at call sites) and returns the validated input value unchanged on success"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs (all 13 [Fact] methods, pass paths assert returned value equals/same as input)"
        status: pass
    human_judgment: false

duration: 3min
completed: 2026-07-16
status: complete
---

# Phase 1 Plan 3: Null Guard Family Summary

**Implemented Guard.Against.Null<T> (two overloads), NullOrWhiteSpace, and NullOrEmpty (string + IEnumerable<T>) with full RED/GREEN TDD gates and 13 passing xUnit v3 tests**

## Performance

- **Duration:** 3 min (task execution); ~1 hour wall-clock session including context load
- **Started:** 2026-07-15T22:04:33-06:00 (first RED commit)
- **Completed:** 2026-07-15T22:06:51-06:00 (final GREEN commit)
- **Tasks:** 2 completed
- **Files modified:** 2 (1 created source, 1 created test)

## Accomplishments
- `Null<T>` implemented as two overloads (`where T : class` returning `T`; `where T : struct` accepting `Nullable<T>` and returning the unwrapped value) — the phase's most load-bearing pattern, matching Ardalis's exact mechanics with `[NotNull]` on the input parameter and no `[return: NotNull]` (avoids CS8825)
- `NullOrWhiteSpace` and `NullOrEmpty` (string + `IEnumerable<T>`) all delegate to `Guard.Against.Null` first, guaranteeing `ArgumentNullException` (not `ArgumentException`) on a null input, per RESEARCH.md Pattern 2
- Every throw path uses a fixed string-literal exception message referencing only `parameterName` — never the rejected value — satisfying the T-1-02 Information-Disclosure mitigation in the plan's threat model
- 13 xUnit v3 `[Fact]` tests cover pass + throw paths for the entire Null guard family; confirmed RED (compile failure) before each implementation commit and GREEN (all passing) after

## Task Commits

Each task followed the RED → GREEN TDD cycle with separate commits:

1. **Task 1: Null&lt;T&gt; (two overloads) + NullOrWhiteSpace**
   - `e252068` (test) - add failing test for Null guard family
   - `d9cb76d` (feat) - implement Null<T> and NullOrWhiteSpace guards
2. **Task 2: NullOrEmpty (string + IEnumerable&lt;T&gt; overloads)**
   - `53701fa` (test) - add failing test for NullOrEmpty guard family
   - `a7a5d1c` (feat) - implement NullOrEmpty (string + IEnumerable<T>) guards

_TDD tasks produced test → feat commit pairs, no refactor commit needed (implementations were correct on first GREEN pass)._

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNullExtensions.cs` - `Null<T>` (2 overloads), `NullOrWhiteSpace`, `NullOrEmpty` (2 overloads: string, `IEnumerable<T>`)
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs` - 13 `[Fact]` tests covering pass/throw paths for the full Null guard family

## Decisions Made
- None beyond the plan's explicit instructions — implementation follows RESEARCH.md/PATTERNS.md verbatim (two-overload `Null<T>`, no `[return: NotNull]`, delegate-to-Null-first for stronger guards, fixed exception message strings)

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. `dotnet test --solution SentinelSuite.slnx --filter GuardAgainstNullTests` did not accept the `--filter` flag as expected (produced MTP CLI help text with zero tests run); worked around by running the unfiltered `dotnet test --solution SentinelSuite.slnx` instead, which correctly discovered and ran all tests in the solution including the new `GuardAgainstNullTests`. This is a tooling-invocation note, not a deviation from the plan's code — no fix was needed to plan-scoped files, so no deviation entry was logged.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `Guard.Against.Null`, `NullOrWhiteSpace`, `NullOrEmpty` are ready for use by every future `Entity`/`EntityAssociation` constructor
- Wave 2 plans 01-04 (Range/Enum guards), 01-05, 01-06 can proceed independently — no shared file conflicts with this plan's `GuardAgainstNullExtensions.cs`
- No blockers identified

---
*Phase: 01-domain-shared-guardclauses*
*Completed: 2026-07-16*

## Self-Check: PASSED

- FOUND: SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNullExtensions.cs
- FOUND: SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs
- FOUND commit: e252068
- FOUND commit: d9cb76d
- FOUND commit: 53701fa
- FOUND commit: a7a5d1c
