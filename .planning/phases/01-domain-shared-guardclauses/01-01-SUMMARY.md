---
phase: 01-domain-shared-guardclauses
plan: 1
subsystem: testing
tags: [dotnet, xunit, mtp, test-infra, domain-shared]

# Dependency graph
requires: []
provides:
  - "MTP-native xUnit v3 test project (SentinelSuite.Framework.Domain.Shared.Tests) wired into the solution"
  - "global.json routing dotnet test through Microsoft.Testing.Platform"
  - "Repo-root .gitignore for .NET build artifacts (bin/, obj/)"
affects: [01-02, 01-03, 01-04, 01-05, 01-06]

# Tech tracking
tech-stack:
  added: ["xunit.v3 3.2.2", "coverlet.mtp 10.0.1"]
  patterns: ["MTP-native test project scaffold (UseMicrosoftTestingPlatformRunner, no VSTest packages)"]

key-files:
  created:
    - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj
    - SentinelSuite/global.json
    - .gitignore
  modified:
    - SentinelSuite/SentinelSuite.slnx

key-decisions:
  - "Hand-authored the test .csproj instead of running dotnet new xunit / installing xunit.v3.templates, per RESEARCH.md Pitfall 5 (SDK's built-in template emits a VSTest-based project, not MTP-native)"
  - "Used coverlet.mtp (not coverlet.collector) for coverage, per RESEARCH.md Pitfall 4 (coverlet.collector is VSTest-only and incompatible with MTP-hosted projects)"
  - "Added a repo-root .gitignore for bin/ and obj/ since none existed and this task's build-verification step generated untracked build artifacts across all three projects"

patterns-established:
  - "MTP-native xUnit v3 .csproj shape: UseMicrosoftTestingPlatformRunner=true, TestingPlatformDotnetTestSupport=true, OutputType=Exe, no Program.cs, pinned exact package versions"

requirements-completed: [PRIM-01]

coverage:
  - id: D1
    description: "SentinelSuite.Framework.Domain.Shared.Tests project builds and is part of the solution, ready to host guard-clause tests"
    requirement: "PRIM-01"
    verification:
      - kind: other
        ref: "cd SentinelSuite && dotnet build SentinelSuite.slnx"
        status: pass
    human_judgment: false
  - id: D2
    description: "dotnet test routes through Microsoft.Testing.Platform once global.json exists at the solution root"
    requirement: "PRIM-01"
    verification:
      - kind: other
        ref: "SentinelSuite/global.json test.runner == Microsoft.Testing.Platform (validated as JSON)"
        status: pass
    human_judgment: false

# Metrics
duration: 15min
completed: 2026-07-16
status: complete
---

# Phase 01 Plan 1: MTP-native xUnit v3 Test Project Scaffolding Summary

**Hand-authored MTP-native xUnit v3 test project (xunit.v3 3.2.2 + coverlet.mtp 10.0.1) wired into SentinelSuite.slnx, with global.json routing dotnet test through Microsoft.Testing.Platform**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-07-16T03:33:00Z (approx)
- **Completed:** 2026-07-16T03:48:10Z
- **Tasks:** 2 completed
- **Files modified:** 4 (1 new csproj, 1 new global.json, 1 new .gitignore, 1 modified .slnx)

## Accomplishments
- Hand-authored `SentinelSuite.Framework.Domain.Shared.Tests.csproj` as an MTP-native xUnit v3 project (no VSTest packages), pinned to the audited `xunit.v3` 3.2.2 and `coverlet.mtp` 10.0.1 versions, referencing `SentinelSuite.Framework.Domain.Shared`
- Created `SentinelSuite/global.json` routing `dotnet test` through `Microsoft.Testing.Platform`
- Added the test project as a third `<Project Path=.../>` entry in `SentinelSuite.slnx` without disturbing the two pre-existing entries
- Confirmed the whole 3-project solution builds successfully with `dotnet build SentinelSuite.slnx` (0 warnings, 0 errors)

## Task Commits

Each task was committed atomically:

1. **Task 1: Hand-author the MTP-native xUnit v3 test project** - `fc5529a` (feat)
2. **Task 2: Wire global.json + slnx, confirm solution-wide build** - `b1cea4a` (feat)

**Plan metadata:** committed alongside this SUMMARY (see final commit)

## Files Created/Modified
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj` - New MTP-native xUnit v3 test project scaffold; no source files yet (empty, ready for Wave 2 test files)
- `SentinelSuite/global.json` - Routes `dotnet test` through `Microsoft.Testing.Platform` on the .NET 10 SDK
- `SentinelSuite/SentinelSuite.slnx` - Added third `Project Path` entry for the new test project
- `.gitignore` - New repo-root gitignore for `bin/`/`obj/` build artifacts (see Deviations)

## Decisions Made
- Hand-authored the `.csproj` rather than installing `xunit.v3.templates`/running `dotnet new xunit3`, per RESEARCH.md's documented fallback (Pitfall 5) — guaranteed-correct and avoids depending on a template package not yet confirmed installed in this environment.
- Used `coverlet.mtp` 10.0.1 exactly (not `coverlet.collector`, which CLAUDE.md's Testing Stack table recommends but which RESEARCH.md Pitfall 4 confirms is VSTest-only and incompatible with MTP) — this supersedes CLAUDE.md for this one line item per RESEARCH.md's explicit correction.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical] Added repo-root .gitignore for .NET build artifacts**
- **Found during:** Task 2 (solution-wide build verification)
- **Issue:** No `.gitignore` existed anywhere in the repository. Running `dotnet build SentinelSuite.slnx` (required by Task 2's verification step) generated `bin/`/`obj/` directories across all three projects, including entirely new untracked build output for the new Tests project. Leaving generated build artifacts untracked/uncontrolled is a repo-hygiene gap that would otherwise resurface on every future plan's build step.
- **Fix:** Added `.gitignore` at the repo root ignoring `[Bb]in/`, `[Oo]bj/`, and `*.user`.
- **Files modified:** `.gitignore` (new)
- **Verification:** `git status --short` after the build shows the new Tests project's `bin/`/`obj/` no longer appear as untracked.
- **Committed in:** `b1cea4a` (Task 2 commit)

**Note:** Some `obj/*.AssemblyInfo.cs` and `.cache` files under `SentinelSuite.Framework.Domain` and `SentinelSuite.Framework.Domain.Shared` were already tracked in git from a prior commit (before any `.gitignore` existed), predating this plan. These continue to show as locally modified after each build (timestamp-only changes) but were left untouched — untracking already-committed files is out of scope for this plan's `files_modified`. Logged to `.planning/phases/01-domain-shared-guardclauses/deferred-items.md` for a future cleanup pass.

---

**Total deviations:** 1 auto-fixed (1 missing critical - Rule 2)
**Impact on plan:** Necessary repo hygiene surfaced directly by this plan's own build-verification step. No scope creep — the pre-existing tracked build artifacts were explicitly left alone and deferred instead of fixed.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `SentinelSuite.Framework.Domain.Shared.Tests` is buildable, empty, and wired into the solution — Wave 2 plans (01-03 through 01-06) can immediately add test files under `SentinelSuite.Framework.Domain.Shared.Tests/Guards/` and run them via `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter <ClassName>`.
- `dotnet test` will route through MTP now that `global.json` exists at the solution root.
- No blockers for Plan 01-02.

---
*Phase: 01-domain-shared-guardclauses*
*Completed: 2026-07-16*

## Self-Check: PASSED

- FOUND: SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj
- FOUND: SentinelSuite/global.json
- FOUND: .gitignore
- FOUND: .planning/phases/01-domain-shared-guardclauses/01-01-SUMMARY.md
- FOUND: commit fc5529a
- FOUND: commit b1cea4a
