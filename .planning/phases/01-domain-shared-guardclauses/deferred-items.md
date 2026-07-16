# Deferred Items — Phase 01

Out-of-scope discoveries logged during plan execution. Not fixed — tracked for future cleanup.

## Pre-existing tracked build artifacts (obj/*.AssemblyInfo.cs, etc.)

- **Found during:** 01-01 Task 2 (`dotnet build SentinelSuite.slnx` verification step)
- **Issue:** `SentinelSuite.Framework.Domain/obj/**` and `SentinelSuite.Framework.Domain.Shared/obj/**` build artifacts were already committed to git in a prior commit, before any `.gitignore` existed. Running `dotnet build` regenerates these files with new timestamps, so they show as locally modified (`M`) even though no source changed.
- **Not fixed because:** out of scope for this plan (files_modified only covers the Tests project, `global.json`, and `.slnx`); untracking already-committed files is a separate repo-hygiene cleanup, not part of PRIM-01 test-infrastructure scaffolding.
- **Recommendation:** A future plan/task should run `git rm -r --cached` on the tracked `obj/`/`bin/` paths (now covered by the `.gitignore` added in 01-01 Task 2) to stop them from being tracked, then commit the removal separately.
