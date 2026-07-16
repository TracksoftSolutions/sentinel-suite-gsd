<!-- GSD:project-start source:PROJECT.md -->

## Project

**Sentinel Suite**

Sentinel Suite is a unified platform for security operations, dispatch, emergency management, safety, and compliance — serving both in-house facility security departments and contract security companies, from a single guard on shift up to DOE GOCO-operated National Laboratories. **This milestone (Milestone 1) is not a customer-facing feature.** It's the custom Clean Architecture/DDD domain kernel — the Entity/EntityAssociation TPT foundation, capability-interface conventions, and tenant/audit/domain-event plumbing — that every one of the platform's 26 modules and 222 planned features will be built on top of.

**Core Value:** Every one of Sentinel Suite's 222 planned features depends on getting the Entity/EntityAssociation taxonomy, multi-tenancy, and auditing conventions right once, in a dependency-minimal, FedRAMP-friendly kernel — get this foundation wrong and every module built on it inherits the mistake.

### Constraints

- **Dependency minimalism**: Prefer hand-rolled implementations over third-party packages whenever reproducing the pattern is minimal effort — every added dependency lengthens the FedRAMP authorization process. Take a real dependency only when reproducing it in-house is genuinely nontrivial.
- **No ABP or Ardalis package dependency**: Borrow the *patterns* (multi-tenancy plumbing, auditing/soft-delete, domain events, module system, Specification, Result, GuardClauses, SmartEnum, Clean Architecture layout) — never the NuGet packages themselves.
- **Tech stack**: .NET 10.0, C# nullable + implicit usings enabled (per existing `.csproj` scaffolds).
- **Compliance**: DOE Orders, FISMA/NIST 800-53, and FedRAMP posture must be considered from this first layer of code onward.
- **Domain modeling discipline**: Must follow `docs/architecture-guidance.md`'s three-way rule (inherit what a thing IS, interface what a thing CAN, compose what a thing USES) and its runtime-registry-is-authoritative rule for tenant-defined subtypes.
- **Timeline**: Greenfield, no hard deadline; phased module-by-module rollout, this is the first phase.

<!-- GSD:project-end -->

<!-- GSD:stack-start source:research/STACK.md -->

## Technology Stack

## Executive framing

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET | 10.0 (LTS) | Runtime/SDK | GA'd Nov 11 2025, LTS through Nov 14 2028 — already the platform's locked stack per `PROJECT.md`/existing `.csproj` scaffolds. Confidence: HIGH (dotnet/core release notes, devblogs.microsoft.com). |
| C# | 14 | Language | Ships with .NET 10 SDK; field-backed properties and extension members are minor conveniences for this milestone (guard clauses, specification builders) but nothing in this kernel *requires* C# 14 — it's simply what the SDK gives you. Confidence: HIGH. |
| Domain project targeting | `net10.0`, nullable + implicit usings enabled | Already the existing scaffold state (`SentinelSuite.Framework.Domain`, `.Shared`) | No change needed; confirmed correct baseline. |

### Supporting Libraries — Production (Domain project)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| *(none)* | — | — | Deliberately zero production NuGet dependencies in the Domain project this milestone. Everything in `<question>` (Entity/AggregateRoot, auditing, soft-delete, multi-tenancy, domain events, module system, Specification, Result, GuardClauses, SmartEnum) is hand-rolled per `PROJECT.md`'s explicit constraint. See "Hand-Roll vs. Depend" below for the mechanics each one requires. |

### Supporting Libraries — Test project only

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `xunit.v3` | 3.2.x (current as of Jan 2026) | Test framework | Standard .NET 10 test framework; see Testing Stack section. Confidence: MEDIUM. |
| `Microsoft.Testing.Platform` (MTP) | SDK-native on .NET 10 | Test runner/host | Replaces the legacy VSTest adapter path (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`); native in the .NET 10 SDK. Confidence: MEDIUM. |
| `coverlet.collector` | latest | Code coverage | Standard cross-platform coverage collector, works with MTP. Low-risk, test-only, near-universal. Confidence: MEDIUM. |
| **`Shouldly`** (optional) or plain `Assert.*` | latest (MIT) | Fluent assertions | **Not** FluentAssertions — see licensing note below. If fluent assertion syntax is wanted, Shouldly is MIT-licensed and free; otherwise xUnit's own `Assert` class is zero-dependency and entirely sufficient for a kernel's invariant tests. Recommend starting with plain `Assert` (true zero extra test dependency) and only reaching for Shouldly if assertion readability becomes a real pain point. Confidence: MEDIUM. |

### Explicitly excluded (confirm NOT to reference)

| Package | Why excluded |
|---------|--------------|
| `Volo.Abp.*` (all ABP NuGet packages: `Volo.Abp.Domain`, `Volo.Abp.Auditing`, `Volo.Abp.MultiTenancy`, `Volo.Abp.EventBus`, `Volo.Abp.Ddd.Domain`, etc.) | `PROJECT.md` constraint: borrow the *patterns*, never the packages. ABP pulls in a large module/DI graph (its own conventional-registration reflection scanner, its own settings/permission/feature-management subsystems) that is far more than this kernel needs and would itself need FedRAMP review as a dependency. |
| `Ardalis.Specification`, `Ardalis.Result`, `Ardalis.GuardClauses`, `Ardalis.SmartEnum` (and their `.EntityFrameworkCore`/`.EFCore`/`.AspNetCore` extension packages) | Explicit `PROJECT.md` constraint. Each is genuinely small (see mechanics below) — reproducing them in-house is a few hundred lines total, well below the threshold where taking the dependency is the better trade. |
| `MediatR` | Named explicitly in `PROJECT.md`'s Out of Scope. The domain-event dispatch this milestone needs (collect-on-aggregate, publish-before-UoW-commit) is a much narrower mechanism than MediatR's general in-process mediator/pipeline-behavior system; hand-rolling the narrower thing is strictly less code than adopting and constraining the general one. |
| `FluentAssertions` (v8+) | As of Jan 2025 it moved to the Xceed Community License — **commercial use requires a paid per-developer license** ($130/dev/yr, or $50/dev/yr for orgs under $1M revenue / ≤3 devs). Even as a test-only dependency this is a new procurement/licensing surface a solo-founder FedRAMP-track project should not take on. v7.x remains Apache-2.0 but is feature-frozen — don't pin to a dead branch either. Use Shouldly or plain `Assert` instead. Confidence: MEDIUM-HIGH (cross-checked InfoQ, dev.to, multiple independent posts, consistent story). |

## Hand-Roll vs. Depend — mechanics to reproduce, concretely

### 1. `Entity` / `AggregateRoot` base classes — HAND-ROLL (trivial, ~50-100 lines)

### 2. Auditing & soft-delete base contracts — HAND-ROLL (trivial, ~100-150 lines)

### 3. Multi-tenancy resolution & data filtering — HAND-ROLL (moderate, ~150-250 lines), matches platform's own tiered model better than ABP's out of the box

### 4. Local domain-event dispatch — HAND-ROLL (moderate, ~150-200 lines)

### 5. Module system — HAND-ROLL, but treat as genuinely lower priority (moderate, ~100-150 lines for the minimal version)

### 6. Specification pattern — HAND-ROLL (small, ~150-200 lines for a useful subset)

### 7. Result pattern — HAND-ROLL (trivial, ~80-120 lines)

### 8. GuardClauses — HAND-ROLL (trivial, ~100-150 lines for the useful subset)

### 9. SmartEnum — HAND-ROLL (small, ~100-150 lines)

## Genuinely hard-to-reproduce parts — none found that justify the dependency

## EF Core TPT mapping — informational for this milestone, load-bearing for the next

- **Shared PK, not a second FK/pointer pair.** TPT maps each level to its own table; the derived table's PK is also its FK back to the base table's PK (one shared identity value threaded through every table in the chain) — this is exactly the shape `_DECISIONS.md` already committed to for both `Entity` and `EntityAssociation` hierarchies ("the row's primary key **is** `entity_id`... eliminating the earlier redundant `extension_id` + `entity_id` pointer pair"). Confirms the Domain-layer `Id` on `Entity` should be the one identity value every derived C# class inherits unchanged — no per-level ID property.
- **Configuration is `.UseTptMappingStrategy()`** (EF Core 5+; implicit as the default strategy in most configurations when no discriminator/table-sharing is set up) — an Infrastructure-layer concern for the next milestone, not this one, but confirms nothing about the Domain-layer class shape needs to anticipate EF-specific attributes/fluent config; POCO classes are sufficient.
- **Performance is the real trap, and it's a documented one, not a hypothetical.** Querying a derived type requires INNER JOINing every table from the queried level up to the root; querying the base type *polymorphically* (i.e., "give me all `Party` rows regardless of concrete subtype") requires a UNION-based fan-out across every leaf table. Microsoft's own performance-modeling documentation states TPT measurably underperforms TPH in most benchmarks specifically because of this join/union cost. `architecture-guidance.md` already has this exactly right (§EF Core practical notes: prefer CQRS read models for hot paths, TPH for field-less hierarchy levels, denormalization last) — this research corroborates that guidance rather than changing it. Nothing to adjust here; flag for the persistence milestone to actually build the CQRS read-model mitigation rather than defer it, since the perf trap is real and well-attested, not a theoretical concern.

## Testing Stack

| Component | Recommendation | Why |
|-----------|----------------|-----|
| Test framework | `xunit.v3` (3.2.x) | Current, actively maintained, native .NET 10 support. |
| Test runner/host | `Microsoft.Testing.Platform` (MTP) | .NET 10 SDK ships MTP natively; xUnit v3 is built on it directly (no VSTest adapter layer). Set the project up on MTP from day one rather than the legacy `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` combo, since that combo is the one being phased out, not the one to adopt fresh in 2026. |
| Assertions | xUnit's own `Assert.*` (default); `Shouldly` (MIT) only if fluent syntax proves worth a test-only dependency | **Do not use FluentAssertions 8+** — paid license (Xceed Community License, $130/dev/yr) as of Jan 2025. FluentAssertions 7.x is free but feature-frozen; don't pin new work to a dead branch. |
| Coverage | `coverlet.collector` | Standard, cross-platform, works with MTP. |
| Test doubles / mocking | Not needed yet | This milestone has no services to mock (no persistence, no external adaptors) — kernel invariant tests operate directly on Domain types. Defer the mocking-library decision (`NSubstitute` is the common low-ceremony choice in this ecosystem) to whichever milestone first needs to fake a collaborator; don't pre-adopt one speculatively. |

## Confidence Assessment

| Area | Confidence | Reason |
|------|------------|--------|
| ABP mechanics (Entity/AggregateRoot, auditing chain, event bus timing, module DAG) | MEDIUM | Web search cross-checked against ABP's own official docs (abp.io/docs) and direct GitHub source-file fetches (`AggregateRoot.cs`, auditing directory listing); no first-party Context7 doc-lookup tool was available this session to push this to HIGH. |
| Multi-tenancy resolution chain specifics (contributor ordering) | MEDIUM | Confirmed via official ABP docs; the *recommendation* to diverge from ABP's exact resolver chain is this researcher's synthesis against `_DECISIONS.md`, not an external source — treat that specific recommendation as reasoned inference, not a verified fact. |
| Ardalis pattern internals (Specification, Result, GuardClauses, SmartEnum) | MEDIUM | Cross-checked against GitHub source files (`Guard.cs` fetched directly), official docs sites (specification.ardalis.com, result.ardalis.com), and NuGet listings for current versions. |
| EF Core TPT mechanics | HIGH for mechanics, MEDIUM for performance magnitude | Microsoft Learn is a primary/official source and was directly consulted for the mapping mechanics; performance claims rest on community benchmark posts, not a first-party controlled benchmark. |
| .NET 10 / xUnit v3 / MTP current-ness | MEDIUM-HIGH | .NET 10 GA date and LTS window confirmed via devblogs.microsoft.com and dotnet/core release notes (official). xUnit v3/MTP status confirmed via xunit.net's own docs pages, dated as recently as May 2026 in search results. |
| FluentAssertions licensing | MEDIUM-HIGH | Multiple independent, mutually consistent sources (InfoQ, dev.to posts from different authors) all describing the same Jan 2025 Xceed license change and pricing — internally consistent enough to treat as reliable despite no single canonical primary source consulted directly. |

## Gaps to Address

- **No first-party doc-lookup tool (Context7/official SDK docs) was available this session** — all findings came from `WebSearch`/`WebFetch` against public web content. If Context7 or an equivalent becomes available later, it would be worth re-verifying the ABP `AggregateRoot`/auditing class hierarchy and the exact `ILocalEventBus` dispatch-timing mechanics directly against ABP's source tree rather than search-engine summaries of it, before finalizing the domain-event dispatch design (item #4 above) — that one is the most behaviorally load-bearing of the nine patterns and the one place a subtle timing mistake (e.g., publishing before vs. after the UoW actually commits) would be expensive to discover late.
- **Multi-tenancy resolver design is intentionally left as a platform-specific design task, not a copy target** — this file recommends *not* copying ABP's resolver chain, but does not itself design the replacement (that belongs in the roadmap/phase planning for the multi-tenancy plumbing requirement, informed directly by `_DECISIONS.md`'s tiered isolation model and Client Engagement dual-tenancy rules, not by further ABP research).
- **Module system scope is deliberately minimized** in this file (skip the conventional-registrar reflection scanner) — worth flagging explicitly to whoever plans that phase so "module system" doesn't scope-creep into rebuilding ABP's full DI-convention engine when `PROJECT.md`'s actual requirement is narrower ("organizing the framework into swappable pieces").
- **EF Core TPT is out of scope for this milestone's actual implementation** — the section above is forward-looking context for the next milestone, not a decision this milestone needs to act on, beyond confirming the Domain-layer `Id`/shared-PK shape is already correctly anticipated.

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:

- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
