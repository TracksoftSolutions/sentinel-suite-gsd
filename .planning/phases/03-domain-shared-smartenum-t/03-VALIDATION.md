---
phase: 3
slug: domain-shared-smartenum-t
status: draft
nyquist_compliant: true
wave_0_complete: false
created: 2026-07-16
---

# Phase 3 — Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit v3 (`xunit.v3.mtp-v2` 3.2.2) on Microsoft.Testing.Platform — established in Phase 1, unchanged |
| **Config file** | `SentinelSuite/global.json` (exists from Phase 1) |
| **Quick run command** | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnum` |
| **Full suite command** | `dotnet test` |
| **Estimated runtime** | ~10 seconds (SmartEnum filter), ~30 seconds (full suite, includes Phase 1's Guards tests and any landed Phase 2 Results tests) |

---

## Sampling Rate

- **After every task commit:** Run `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnum`
- **After every plan wave:** Run `dotnet test` (full suite, includes Phase 1's Guards tests and any landed Phase 2 Results tests unaffected)
- **Before `/gsd-verify-work`:** Full suite must be green
- **Max feedback latency:** 30 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Threat Ref | Secure Behavior | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|------------|-----------------|-----------|-------------------|-------------|--------|
| TBD | TBD | TBD | PRIM-03 | — | `SmartEnum<T>` / `SmartEnum<T,TValue>` compile with zero third-party `PackageReference` entries | build/static check | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` + manual `.csproj` inspection | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (Success Criterion 2) | — | Two `SmartEnum<T>`-derived fixture instances with the same underlying value are equal (D-08 amended: equality via `IEquatable<TValue>`) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumEqualityTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (Success Criterion 3) | — | `FromValue`/`FromName` succeed for valid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumFromValueFromNameTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (Success Criterion 4) | T-3-01 (Information Disclosure — raw lookup value in exception message) | `FromValue`/`FromName` throw `SmartEnumNotFoundException` (specific, catchable) for invalid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumNotFoundTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-04) | — | `TryFromValue`/`TryFromName` return `false` (not throw) for invalid inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumNotFoundTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-14) | — | `FromValue(value, defaultValue)` returns the supplied default instead of throwing on a miss | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumFromValueDefaultTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-07/D-08) | — | `IComparable<T>` sorts by underlying `Value`, for both int-backed and generic-value-backed forms | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumComparableTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-09) | — | Static `List`/`GetAll()` enumerates every defined instance | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumListTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-10 amended) | — | `==`, `!=`, `<`, `<=`, `>`, `>=` operators behave consistently with `Equals`/`CompareTo` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumOperatorTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-11) | — | `ToString()` returns `Name` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumToStringTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-01, generic-value form) | — | String-backed `SmartEnum<TEnum,string>` fixture exercises discovery + lookup + comparison end-to-end | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumGenericValueTests` | ❌ W0 | ⬜ pending |
| TBD | TBD | TBD | PRIM-03 (D-12/D-13) | — | Reflection-time failure on a badly-formed `SmartEnum`-derived fixture throws a specific, catchable, reproducible exception without corrupting other `SmartEnum` types (RESEARCH.md Pitfall 3) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter SmartEnumDiscoveryFailureTests` | ❌ W0 | ⬜ pending |

*Status: ⬜ pending · ✅ green · ❌ red · ⚠️ flaky*
*Task IDs marked TBD — the planner assigns concrete Plan/Wave/Task IDs; this table's rows must be threaded into each task's `<acceptance_criteria>` verbatim by requirement.*

---

## Wave 0 Requirements

- [ ] `SentinelSuite.Framework.Domain.Shared/SmartEnum/` — new sub-namespace, does not exist yet
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/SmartEnum/` — new test subfolder, does not exist yet
- [ ] Framework install: none — reuses Phase 1's existing `xunit.v3.mtp-v2`/`coverlet.mtp` test project unchanged, no new `dotnet add package` needed

---

## Manual-Only Verifications

*None — all phase behaviors have automated verification. This is a pure Domain.Shared library phase (no UI, no external I/O, no persistence) — every behavior (discovery, equality, comparison, lookup success/failure/default, operators, ToString, generic-value form) is expressible as an xUnit assertion.*

---

## Validation Sign-Off

- [ ] All tasks have `<automated>` verify or Wave 0 dependencies
- [ ] Sampling continuity: no 3 consecutive tasks without automated verify
- [ ] Wave 0 covers all MISSING references
- [ ] No watch-mode flags
- [ ] Feedback latency < 30s
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
