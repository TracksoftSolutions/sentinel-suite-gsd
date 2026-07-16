---
phase: 01-domain-shared-guardclauses
plan: 02
subsystem: domain-shared
tags: [guard-clauses, dotnet, domain-shared, extensibility]

# Dependency graph
requires:
  - phase: 01-domain-shared-guardclauses (plan 01)
    provides: MTP-native xUnit v3 test project scaffolding, global.json, .slnx wiring
provides:
  - IGuardClause empty marker interface (extensibility anchor for guard-clause extension methods)
  - Guard static entry point exposing Guard.Against as a lazily-constructed IGuardClause singleton
  - Documented D-06 naming convention (GuardAgainst{Concept}Extensions / {Module}GuardExtensions) for all future guard extensions
  - Inline Pitfall-6 rebuttal distinguishing IGuardClause from the domain-capability marker-interface anti-pattern
affects: [01-domain-shared-guardclauses plans 03-06 (guard-method extension classes), any future module adding its own Guard.Against.X guard]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IGuardClause zero-member extensibility anchor: guard methods attach as extension methods, never as members of a closed class"
    - "Guard.Against singleton dispatch point — call sites never construct Guard directly"
    - "GuardAgainst{Concept}Extensions naming for framework-level guard extension classes; {Module}GuardExtensions for downstream-module guards"

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/IGuardClause.cs
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs
  modified: []

key-decisions:
  - "Guard.Against typed as IGuardClause (D-05), not a concrete class, so every guard method is an extension method — zero edits to Domain.Shared needed for any future module's guards"
  - "Documented D-06 naming convention inline in Guard.cs XML remarks rather than in a separate doc, so the precedent travels with the code Wave 2 plans will extend"

requirements-completed: [PRIM-01]

coverage:
  - id: D1
    description: "IGuardClause zero-member marker interface compiles with inline Pitfall-6 rebuttal documentation"
    requirement: "PRIM-01"
    verification:
      - kind: unit
        ref: "grep -c 'interface IGuardClause' Guards/IGuardClause.cs && grep -c 'namespace SentinelSuite.Framework.Domain.Shared.Guards' Guards/IGuardClause.cs"
        status: pass
    human_judgment: false
  - id: D2
    description: "Guard static entry point exposes Guard.Against as IGuardClause; Domain.Shared remains free of third-party PackageReference elements; dotnet build succeeds"
    requirement: "PRIM-01"
    verification:
      - kind: integration
        ref: "cd SentinelSuite && dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj"
        status: pass
      - kind: unit
        ref: "grep -c PackageReference SentinelSuite.Framework.Domain.Shared.csproj -> 0"
        status: pass
    human_judgment: false

# Metrics
duration: 8min
completed: 2026-07-16
status: complete
---

# Phase 01 Plan 02: IGuardClause + Guard Foundation Summary

**Zero-member `IGuardClause` extensibility anchor plus `Guard.Against` static entry point, compiling dependency-free in Domain.Shared, with the D-06 naming convention and Pitfall-6 marker-interface rebuttal both documented inline.**

## Performance

- **Duration:** 8 min
- **Started:** 2026-07-16T03:50:00Z
- **Completed:** 2026-07-16T03:53:00Z
- **Tasks:** 2 completed
- **Files modified:** 2

## Accomplishments
- `IGuardClause.cs` — empty marker interface in `SentinelSuite.Framework.Domain.Shared.Guards`, with an XML doc comment that explicitly distinguishes it from the domain-capability marker-interface smell `docs/architecture-guidance.md` warns against (no registry, no runtime capability query, no domain concept behind it)
- `Guard.cs` — sealed `Guard : IGuardClause` class with a private constructor and a public static `Against` property (lazily-constructed singleton), giving call sites the literal `Guard.Against.X(...)` shape required by D-01/D-11
- D-06's naming convention documented in `Guard.cs`'s XML remarks: `GuardAgainst{Concept}Extensions` for framework-level guards in this assembly, `{Module}GuardExtensions` for any downstream module's own guards
- Confirmed `SentinelSuite.Framework.Domain.Shared.csproj` still declares zero `PackageReference` elements after both files were added
- `dotnet build` of `SentinelSuite.Framework.Domain.Shared.csproj` succeeds with 0 warnings, 0 errors

## Task Commits

Each task was committed atomically:

1. **Task 1: IGuardClause marker interface + Pitfall-6 rationale doc** - `368e74d` (feat)
2. **Task 2: Guard static entry point + D-06 naming-convention doc** - `de1c310` (feat)

**Plan metadata:** (recorded below after this SUMMARY commit)

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/IGuardClause.cs` - Zero-member extensibility marker interface with Pitfall-6 rebuttal doc comment
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` - Static `Guard.Against` entry point + D-06 naming-convention XML remarks

## Decisions Made
None beyond what was already locked in `01-CONTEXT.md` (D-01, D-05, D-06, D-10, D-11) — plan executed exactly as specified, no new architectural decisions required.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `Guard.Against` is a syntactically valid, type-checked `IGuardClause` expression, ready for Wave 2 plans (01-03 through 01-06) to attach `GuardAgainstNullExtensions`, `GuardAgainstRangeExtensions`, `GuardAgainstNumericExtensions`, `GuardAgainstInputExtensions`, and `GuardAgainstStringExtensions` as extension methods.
- No blockers. Domain.Shared remains a zero-third-party-dependency project.

---
*Phase: 01-domain-shared-guardclauses*
*Completed: 2026-07-16*
