---
phase: 01
slug: domain-shared-guardclauses
status: verified
# threats_open = count of OPEN threats at or above workflow.security_block_on severity (the blocking gate)
threats_open: 0
asvs_level: 1
created: 2026-07-16
---

# Phase 01 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Developer machine / CI build ↔ public NuGet registry | Test-project-only package installs (`xunit.v3.mtp-v2`, `coverlet.mtp`) pull compiled binaries from an external registry into the build | Compiled test-tooling binaries |
| N/A (01-02) | Pure static scaffolding types (`Guard`, `IGuardClause`) with no external/untrusted input surface | None |
| Any future caller (`Entity`/`EntityAssociation` constructors, UseCases validators, module code) → `Guard.Against.*` | The kernel's first real input-validation surface — every downstream constructor across 222 planned features will call through here | Untrusted argument values (strings, numerics, enums, collections) and their `CallerArgumentExpression`-captured source text |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-1-01 (01-01) | Tampering | NuGet package installs (`xunit.v3.mtp-v2`, `coverlet.mtp`) | high | mitigate | Both packages independently audited during research (no ASSUMED/SUS/SLOP findings); csproj pins exact versions (`3.2.2` / `10.0.1`), no floating ranges. Package id changed from `xunit.v3` to `xunit.v3.mtp-v2` post-authoring (commit `4d39b63`, post-merge gate fix for a `Microsoft.Testing.Platform` version conflict with `coverlet.mtp`) — still exact-pinned. | closed |
| T-1-02 (01-01) | Repudiation | Test project scaffolding | low | accept | Build-time-only scaffolding, no user-facing or persisted data; no audit trail meaningful at this layer. | closed |
| T-1-03 (01-02) | Tampering | `Guard.cs` / `IGuardClause.cs` | low | accept | Pure static scaffolding, no runtime input surface at this layer; validation logic and its mitigations land in Wave 2. | closed |
| T-1-04 (01-02) | Elevation of Privilege / design-drift | Convention bypass (future contributor hardcodes ad hoc validation instead of extending via `IGuardClause`) | low | accept | Enforced by documented naming convention (D-06) and code review discipline; bypass produces inconsistent style, not a runtime-exploitable vulnerability. | closed |
| T-1-01 (01-03) | Tampering | `GuardAgainstNullExtensions.cs` (`Null`, `NullOrEmpty`, `NullOrWhiteSpace`) | high | mitigate | Fails fast with a specific BCL exception before null/empty/whitespace propagates downstream; verified by 13 passing pass/throw unit tests. | closed |
| T-1-02 (01-03) | Information Disclosure | Null guard exception messages | high | mitigate | **Gap found and fixed.** Code review (CR-01) found `[CallerArgumentExpression]` captured raw call-site source text (e.g. string literals) as `parameterName`, which was then embedded in every exception message — defeating the "never leak the rejected value" mitigation whenever a caller passed a literal/inline expression. Fixed in commit `05805b2`: `Guard.SafeParamName()` sanitizer rejects non-identifier-shaped candidates before use; wired into every throw site across all 5 guard-extension files (confirmed via direct source read, not narrative). | closed |
| T-1-01 (01-04) | Tampering | `GuardAgainstRangeExtensions.cs` (`OutOfRange`, `EnumOutOfRange`) | high | mitigate | **Gap found and fixed.** Code review (CR-02) found `EnumOutOfRange` used `Enum.IsDefined`, incorrectly rejecting valid `[Flags]`-enum bitwise combinations (e.g. `Perm.Read \| Perm.Write`) — a foundational correctness risk for an access-control-heavy platform. Fixed in commit `65c1299`: detects `[FlagsAttribute]` and validates by mask instead. Confirmed via direct source read. 9 passing pass/throw tests cover the non-Flags path. | closed |
| T-1-02 (01-04) | Information Disclosure | Range/enum exception messages | high | mitigate | **Gap found and fixed.** Code review (CR-03) found `EnumOutOfRange`'s 3-argument `InvalidEnumArgumentException` constructor auto-embeds the rejected numeric value, inconsistent with every sibling guard. Fixed in commit `4bcb14c`: constructs a single-string message using only the sanitized `parameterName`. Confirmed via direct source read. | closed |
| T-1-01 (01-05) | Tampering | `GuardAgainstNumericExtensions.cs` (`Negative`, `NegativeOrZero`, `Zero`, `Default<T>`) | high | mitigate | Rejects invalid numeric signs and uninitialized-struct sentinels before reaching a downstream constructor; verified by 12 passing pass/throw tests. | closed |
| T-1-02 (01-05) | Information Disclosure | Numeric guard exception messages | high | mitigate | `Guard.SafeParamName()` sanitizer confirmed wired into all 4 numeric guard throw sites (direct source read). | closed |
| T-1-01 (01-06) | Tampering | `GuardAgainstInputExtensions.cs` / `GuardAgainstStringExtensions.cs` | high | mitigate | **Gaps found and fixed.** Code review found (a) `InvalidInput` did not guard its own `predicate` parameter, producing an unhelpful `NullReferenceException` (WR-01, fixed `a9c951f`); (b) `InvalidFormat` did not guard its own `regexPattern` parameter (WR-04, fixed `4e790d6`). Both confirmed via direct source read. 9 passing pass/throw tests cover the primary paths. | closed |
| T-1-02 (01-06) | Information Disclosure | `InvalidInput`/`StringTooShort`/`StringTooLong`/`InvalidFormat` exception messages | high | mitigate | Flagged in the plan as the highest-risk guard family for this threat (`InvalidInput`'s `T` is unconstrained). `Guard.SafeParamName()` sanitizer confirmed wired into every throw site in both files (direct source read). | closed |
| T-1-03 (01-06) | Tampering (supply-chain / scope-creep) | Exclusion of a NotFound-style guard requiring a non-BCL exception | low | accept | Deliberately deferred to Phase 4 (`DomainException`) rather than smuggling a premature domain-exception dependency into this phase; documented inline. | closed |

*Status: open · closed · open — below high threshold (non-blocking)*
*Severity: critical > high > medium > low — only open threats at or above workflow.security_block_on (high) count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-01 | T-1-02 (01-01) | Build-time-only test scaffolding has no runtime/persisted data surface; no repudiation-relevant audit trail is meaningful at this layer. | Plan author (01-01-PLAN.md) | 2026-07-15 |
| AR-02 | T-1-03 (01-02) | `Guard`/`IGuardClause` are pure static scaffolding with no runtime input surface; real input-validation threats are scoped to Wave 2 plans. | Plan author (01-02-PLAN.md) | 2026-07-15 |
| AR-03 | T-1-04 (01-02) | Convention-bypass risk is a code-style/process concern (naming convention D-06 + review discipline), not a runtime-exploitable vulnerability. | Plan author (01-02-PLAN.md) | 2026-07-15 |
| AR-04 | T-1-03 (01-06) | NotFound-style guard requiring a non-BCL exception deliberately deferred to Phase 4 (`DomainException`) rather than introducing a premature domain-exception dependency into this phase. | Plan author (01-06-PLAN.md) | 2026-07-15 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-07-16 | 13 | 13 | 0 | /gsd-secure-phase (L1 grep-depth, register authored at plan time — short-circuited per ASVS level 1; verified directly against current source for `mitigate`-disposition threats given 3 critical + 4 warning code-review findings had already surfaced and fixed real gaps against T-1-01/T-1-02 in 01-03 through 01-06) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-07-16
