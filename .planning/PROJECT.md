# Sentinel Suite

## What This Is

Sentinel Suite is a unified platform for security operations, dispatch, emergency management, safety, and compliance — serving both in-house facility security departments and contract security companies, from a single guard on shift up to DOE GOCO-operated National Laboratories. **This milestone (Milestone 1) is not a customer-facing feature.** It's the custom Clean Architecture/DDD domain kernel — the Entity/EntityAssociation TPT foundation, capability-interface conventions, and tenant/audit/domain-event plumbing — that every one of the platform's 26 modules and 222 planned features will be built on top of.

## Core Value

Every one of Sentinel Suite's 222 planned features depends on getting the Entity/EntityAssociation taxonomy, multi-tenancy, and auditing conventions right once, in a dependency-minimal, FedRAMP-friendly kernel — get this foundation wrong and every module built on it inherits the mistake.

## Business Context

- **Customer**: In-house facility/campus security departments first (national labs, large hotels, casinos); contract security firms post-MVP. This milestone has no direct customer surface — it's foundation work.
- **Revenue model**: Commercial SaaS (primary) + self-hosted Docker for DOE/secure facilities.
- **Success metric**: N/A for this milestone — success here is "the next milestone can build real domain types on top of this without fighting the kernel."
- **Strategy notes**: [docs/pdd.md](../docs/pdd.md) (Project Design Document), [docs/mvp.md](../docs/mvp.md) (living MVP scope ledger)

## Requirements

### Validated

- [x] GuardClauses (Ardalis-style, hand-rolled — no `Ardalis.GuardClauses` package) — Validated in Phase 1: Domain.Shared: GuardClauses

### Active

- [ ] `Entity` abstract base class: identity, tenant scoping, audit fields, soft-delete convention, optimistic concurrency stamp
- [ ] Explicit aggregate-root distinction (separate from plain `Entity`) — resolves where domain events collect
- [ ] Value Object base class: structural equality, immutability
- [ ] `EntityAssociation` abstract base class: named-kind pattern, current-value-plus-history shape
- [ ] Abstract TPT intermediate roots — `Party`, `Item`, `Location`, `Activity`, `Document` — never instantiated directly
- [ ] Capability interface scaffold (e.g. `IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, `ICustodyTracked`) with the registry-is-authoritative discipline documented and enforceable
- [ ] Domain events: collection-on-aggregate + dispatch convention (ABP-style local event bus pattern, hand-rolled)
- [ ] Multi-tenancy plumbing: tenant resolution + data filtering convention, matching the platform's tiered isolation model (shared / dedicated_db / on_prem)
- [ ] Auditing & soft-delete base contracts (creation/modification audit, soft-delete — ABP-style, hand-rolled)
- [ ] Module system for organizing the framework into swappable pieces (ABP-inspired)
- [ ] Specification pattern (Ardalis-style, hand-rolled — no `Ardalis.Specification` package)
- [ ] Result pattern (Ardalis-style, hand-rolled — no `Ardalis.Result` package)
- [ ] SmartEnum pattern (Ardalis-style, hand-rolled — no `Ardalis.SmartEnum` package)
- [ ] Clean Architecture solution layout: Core/UseCases/Infrastructure/Web-equivalent project separation with dependency direction enforced (layout only in this milestone — only Domain has real content)
- [ ] Unit test coverage (xUnit) for kernel invariants and capability interfaces, written as each piece lands

### Out of Scope

- Concrete domain types (Person, Employee, Vehicle, etc.) — this milestone is abstractions only; concrete types are the next milestone (Master Records build-out per `docs/requirements/0.5-master-records/`)
- EF Core persistence, `DbContext`, any infrastructure-layer code — this milestone is the Domain project only
- Application/API layer, any HTTP surface — deferred
- Depending on the actual ABP NuGet packages — explicitly borrowing patterns, not the framework
- Third-party pattern-library packages (`Ardalis.Specification`, `Ardalis.Result`, `Ardalis.GuardClauses`, `Ardalis.SmartEnum`, MediatR, etc.) wherever reproducing the pattern is minimal effort — every dependency lengthens FedRAMP authorization

## Context

- `SentinelSuite/SentinelSuite.Framework.Domain` and `SentinelSuite.Framework.Domain.Shared` already exist as empty .NET 10.0 project scaffolds (nullable + implicit usings enabled) — no domain code written yet.
- The full platform's requirements have already been extensively elicited in a prior session, independent of GSD: [docs/pdd.md](../docs/pdd.md) (PDD), [docs/mvp.md](../docs/mvp.md) (living MVP scope ledger), [docs/MODULES.md](../docs/MODULES.md) (26-module / 222-feature catalog), `docs/requirements/_DECISIONS.md` (200+ cross-cutting architectural decisions logged across Modules 0–6), and [docs/architecture-guidance.md](../docs/architecture-guidance.md) (the inheritance/interface/composition rules this kernel must follow).
- This milestone builds the code foundation those requirements docs describe conceptually but do not themselves implement.
- Solo founder/developer, AI-assisted, 20+ years of hands-on security-industry domain expertise (NREL, Gaylord Rockies opening, a major Colorado casino).

## Constraints

- **Dependency minimalism**: Prefer hand-rolled implementations over third-party packages whenever reproducing the pattern is minimal effort — every added dependency lengthens the FedRAMP authorization process. Take a real dependency only when reproducing it in-house is genuinely nontrivial.
- **No ABP or Ardalis package dependency**: Borrow the *patterns* (multi-tenancy plumbing, auditing/soft-delete, domain events, module system, Specification, Result, GuardClauses, SmartEnum, Clean Architecture layout) — never the NuGet packages themselves.
- **Tech stack**: .NET 10.0, C# nullable + implicit usings enabled (per existing `.csproj` scaffolds).
- **Compliance**: DOE Orders, FISMA/NIST 800-53, and FedRAMP posture must be considered from this first layer of code onward.
- **Domain modeling discipline**: Must follow `docs/architecture-guidance.md`'s three-way rule (inherit what a thing IS, interface what a thing CAN, compose what a thing USES) and its runtime-registry-is-authoritative rule for tenant-defined subtypes.
- **Timeline**: Greenfield, no hard deadline; phased module-by-module rollout, this is the first phase.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Build a custom framework inspired by ABP + Ardalis, rather than depend on ABP itself | Full control and dependency minimalism for FedRAMP, while keeping ABP's proven patterns (multi-tenancy, modularity, auditing) | — Pending |
| Milestone 1 scope = Domain kernel abstractions only — no concrete types, no persistence layer | Get the taxonomy and conventions right once before building 222 features on top of it | — Pending |
| Hand-roll Ardalis-style patterns (Specification, Result, GuardClauses, SmartEnum) instead of taking the NuGet packages | Every dependency lengthens FedRAMP authorization; these patterns are small enough to reproduce in-house | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-07-15 after initialization*
