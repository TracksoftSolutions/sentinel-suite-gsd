---
phase: 2
slug: domain-shared-result-result-t
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-07-16
---

# Phase 2 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 (`xunit.v3` 3.2.2) on Microsoft.Testing.Platform — established in Phase 1, unchanged |
| **Config file** | `SentinelSuite/global.json` (exists from Phase 1) |
| **Quick run command** | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter Results` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds (Results filter), ~30 seconds (full suite, includes Phase 1's Guards tests) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter Results`
- **After every plan wave:** Run `dotnet test` (full suite, includes Phase 1's Guards tests unaffected)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | PRIM-02 | — | `Result`/`Result<T>` compile with zero third-party `PackageReference` entries | build/static check | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` + manual `.csproj` inspection | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | Success/failure state transitions (`IsSuccess`/`IsFailure` correct per constructed status) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultConstructionTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | Error code/message propagation from a failure `Result` (`.Errors`, `.Error` convenience accessor) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultErrorTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | Failed `Result<T>.Value` throws `InvalidOperationException` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultOfTValueAccessTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | All named `ResultStatus` factories exist identically on `Result` and `Result<T>` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultStatusFactoryTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | T-2-01 (Information Disclosure — Exception leakage) | `CriticalError` carries the original `Exception`, and `Error` entry still satisfies non-empty `Message` (D-03) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultCriticalErrorTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | Map/Bind/OnSuccess/OnFailure/Match/Ensure — sync short-circuit-on-failure behavior | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter "ResultMapTests\|ResultBindTests\|ResultEnsureTests\|ResultMatchTests\|ResultOnSuccessOnFailureTests"` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | Async overloads (`Task<Result<T>>`) for each combinator, all four sync/async shapes | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultAsyncCombinatorTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-02 | — | `Result.Combine` — all-success passthrough, aggregated errors on any failure, mixed `Result`/`Result<T>` inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultCombineTests` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Task IDs marked TBD — the planner assigns concrete Plan/Wave/Task IDs; this table's rows must be threaded into each task's `<acceptance_criteria>` verbatim by requirement.*

---

## Wave 0 Requirements

- [ ] `SentinelSuite.Framework.Domain.Shared/Results/` — new sub-namespace, does not exist yet
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/Results/` — new test subfolder, does not exist yet
- [ ] Framework install: none — reuses Phase 1's existing `xunit.v3`/`coverlet.mtp` test project unchanged, no new `dotnet add package` needed

---

## Manual-Only Verifications

*None — all phase behaviors have automated verification. This is a pure Domain.Shared library phase (no UI, no external I/O, no persistence) — every behavior (construction, status factories, error propagation, fail-fast Value access, combinator chaining including async overloads, Combine aggregation) is expressible as an xUnit assertion.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
