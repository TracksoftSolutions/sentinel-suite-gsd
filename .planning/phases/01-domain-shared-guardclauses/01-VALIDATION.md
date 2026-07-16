---
phase: 1
slug: domain-shared-guardclauses
status: draft
nyquist_compliant: false
wave_0_complete: false
created: 2026-07-15
---

# Phase 1 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 (`xunit.v3` 3.2.2) on Microsoft.Testing.Platform (MTP) |
| **Config file** | none yet — `SentinelSuite/global.json` must be created this phase (Wave 0) |
| **Quick run command** | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests` |
| **Full suite command** | `dotnet test` (once `global.json` routes it through MTP) |
| **Estimated runtime** | ~5 seconds (small, dependency-free unit test suite) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests`
- **After every plan wave:** Run `dotnet test`
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 10 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| 01-01-01 | 01 | 0 | PRIM-01 | — | N/A (scaffolding) | build | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` | ❌ W0 | ⬜ pending |
| 01-0X-0X | TBD | 1 | PRIM-01 | T-1-01 | Guard.Against.Null throws ArgumentNullException with correct captured parameter name on null input; returns value on valid input | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter GuardAgainstNullTests` | ❌ W0 | ⬜ pending |
| 01-0X-0X | TBD | 1 | PRIM-01 | T-1-01 | Empty/range/enum-membership guards throw ArgumentException/ArgumentOutOfRangeException/InvalidEnumArgumentException correctly and pass valid input through unchanged | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter "GuardAgainstRangeTests|GuardAgainstNullTests"` | ❌ W0 | ⬜ pending |

*Exact Task IDs to be filled in by the planner once PLAN.md task numbering is assigned.*

---

## Wave 0 Requirements

- [ ] `SentinelSuite/global.json` — routes `dotnet test` through MTP on SDK 10+; does not exist
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/SentinelSuite.Framework.Domain.Shared.Tests.csproj` — new MTP-native test project; does not exist (hand-author per RESEARCH.md's Code Examples if `xunit.v3.templates` template naming is uncertain)
- [ ] `SentinelSuite.slnx` — needs the new test project added as a third `<Project Path=.../>` entry
- [ ] Framework install (optional): `dotnet new install xunit.v3.templates` — not required if hand-authoring the `.csproj`

---

## Manual-Only Verifications

*None — all phase behaviors have automated verification (build check + unit tests).*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 10s
- [ ] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
