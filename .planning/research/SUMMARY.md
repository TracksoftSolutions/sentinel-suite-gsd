# Project Research Summary

**Project:** Sentinel Suite — Framework Kernel (Milestone 1)
**Domain:** Hand-rolled .NET Clean Architecture / DDD domain-layer kernel (Domain + Domain.Shared projects only; no persistence, no app/API layer)
**Researched:** 2026-07-15
**Confidence:** MEDIUM-HIGH

## Executive Summary

This milestone is not a "pick a stack" exercise — it's "reproduce specific, well-understood mechanics from two reference codebases (ABP Framework, Ardalis Clean Architecture) without taking the dependency." Everything the platform needs — `Entity`/`AggregateRoot`, auditing, soft-delete, multi-tenancy contracts, domain events, a scoped-down module system, Specification, Result, GuardClauses, SmartEnum — has well-documented prior art and is genuinely small to hand-roll (50-250 lines each). No pattern surfaced in research is complex enough to justify taking a third-party dependency; the constraint in PROJECT.md ("borrow the patterns, never the packages") holds up under scrutiny. The one genuinely bespoke, high-risk piece with no external prior art is `EntityAssociation`'s "current-value-plus-history" shape — it deserves the most design discussion of anything in this milestone.

The recommended approach: build zero-dependency primitives first (`GuardClauses`, `Result`, `SmartEnum`, `DomainException` in `Domain.Shared`), stand up the five-project Clean-Architecture-shaped solution layout now (even with four empty stub projects) so the dependency direction is compiler-enforced from day one, then land `Entity` as the single highest-leverage keystone class before building `EntityAssociation`, the TPT abstract intermediate roots (Party/Item/Location/Activity/Document), the capability-interface scaffold, and a deliberately scoped-down module system. Two additions not currently in PROJECT.md's Active list are strongly recommended: a Value Object base class (structural equality/immutability — used across nearly every future module) and an explicit aggregate-root distinction (currently implicit, but it gates where domain events collect).

The main risks are all "looks done but isn't" traps that are cheap to prevent now and expensive to retrofit across 200+ downstream types later: leaving base classes open to inheritance "just in case" (fragile-base-class/yo-yo problem), under-scoping the module system's dependency-graph resolution (fine for 2-3 modules, breaks silently at scale), and treating domain-event dispatch timing or multi-tenancy resolution as solved by copying ABP's surface API without its underlying mechanics (outbox pattern, ambient AsyncLocal context, resolver-chain edge cases). None of these require infrastructure code this milestone — they require getting the Domain-layer *contracts* and *discipline* right so the next milestone (persistence + concrete types) doesn't inherit a design mistake baked into 200+ types.

## Key Findings

### Recommended Stack

.NET 10 (LTS through Nov 2028) and C# 14 are already the locked platform baseline — no change needed. The Domain project should carry **zero production NuGet dependencies**: every pattern named in PROJECT.md is hand-rolled. Test-project dependencies are treated differently (they never ship to the FedRAMP authorization boundary): xUnit v3 on Microsoft.Testing.Platform (native .NET 10 test host, replacing the legacy VSTest adapter), `coverlet.collector` for coverage, and plain xUnit `Assert` (not FluentAssertions 8+, which requires a paid Xceed license as of Jan 2025 — use Shouldly, MIT-licensed, only if fluent syntax proves worth it).

**Core technologies:**
- .NET 10 (LTS) — already the platform's locked stack; GA'd Nov 2025, confirmed via official release notes.
- C# 14 — ships with the SDK; no code in this kernel strictly requires its new features but nothing blocks using it.
- xUnit v3 + Microsoft.Testing.Platform — current, actively maintained, native .NET 10 test host; adopt from day one rather than the VSTest combo being phased out.

**Explicitly excluded:** `Volo.Abp.*`, `Ardalis.Specification`/`Result`/`GuardClauses`/`SmartEnum`, `MediatR`, `FluentAssertions` 8+. Each pattern is small enough (a few hundred lines total across all nine) that hand-rolling is the correct trade for a FedRAMP-minimalism-constrained solo-dev project.

### Expected Features

This is a framework-kernel milestone — "features" mean kernel abstractions/mechanics that 200+ downstream feature builds depend on, not end-user functionality.

**Must have (table stakes):**
- `Entity` abstract base: identity + equality (by concrete type + Id), tenant-scoping contract, granular audit interfaces, soft-delete contract, optimistic concurrency stamp
- Aggregate-root marker/distinction (currently implicit in PROJECT.md — must be resolved explicitly; it gates where domain events collect)
- Domain event collection convention (`IDomainEvent` marker + `AddDomainEvent`/`ClearDomainEvents`)
- `EntityAssociation` abstract base: named-kind + current-value-plus-history shape — highest design-risk item in the milestone, no external prior art
- TPT abstract intermediate roots: `Party`, `Item`, `Location`, `Activity`, `Document`
- GuardClauses, SmartEnum, Result, DomainException in `Domain.Shared`
- Multi-tenancy plumbing (contract/convention only — resolution and EF Core query-filter wiring are Infrastructure, next milestone)
- Clean Architecture solution layout (project separation + dependency-direction enforcement, layout only)
- xUnit tests for every kernel invariant, written as each piece lands

**Should have (differentiators, keep minimal):**
- Value Object base (structural equality, immutability) — **recommended addition, not currently in Active**
- Capability interface scaffold: `IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, `ICustodyTracked` + registry-authoritative discipline, treated as the *complete* deliverable, not a starting enumeration
- `ICapabilityResolver` interface stub (concrete implementation deferred)
- Extensible/extra-properties pattern (`IHasExtraProperties`-equivalent) — parallels the platform's existing `extended_fields` concept
- Specification pattern — ship only `IsSatisfiedBy(T)`, not the full EF-query-composition surface
- Module system — scoped to a manifest/marker convention (`IFrameworkModule` + `[DependsOn]` + topological sort), **not** a full DI-bootstrapper/conventional-registrar

**Defer (v2+):**
- Full ABP-style module bootstrapper with DI conventions and reflection-based auto-registration
- Dynamic feature/permission/settings management modules (persistence-dependent, no concrete consumer yet)
- Localization system wired to `IDisplayLabeled`
- Distributed/outbox domain event bus (the *decision* to eventually need an outbox must be documented now; the implementation is next milestone)
- Source-generator-based strongly-typed ID toolchain
- Generic `IRepository<T>` — never build this; define narrow, aggregate-specific repositories only once persistence exists

### Architecture Approach

Sentinel Suite's existing scaffold already committed to ABP's `Domain`/`Domain.Shared` split (Domain.Shared has zero project references; Domain depends only on Domain.Shared). This research grafts Ardalis's outer layers (`UseCases`, `Infrastructure`, `Web`) on top as empty stub projects with correct `ProjectReference` chains, so the dependency direction is compiler-enforced from day one rather than retrofitted when the second module needs it.

**Major components:**
1. `SentinelSuite.Framework.Domain.Shared` — zero-dependency primitives (Result, Guard, SmartEnum, audit/tenant/soft-delete *contracts*) that every other layer needs without depending on the entity taxonomy.
2. `SentinelSuite.Framework.Domain` — the actual taxonomy: `Entity`, `EntityAssociation`, TPT intermediate roots, capability interfaces, capability registry contract, domain events, Specification base, module-system abstraction. This milestone's real deliverable.
3. `SentinelSuite.Framework.UseCases` / `.Infrastructure` / `.Web` — empty stub projects this milestone, wired with correct references now to lock in the dependency direction before real content lands.

A recommended build order (waves, not the requirement-list order): Wave 0 (GuardClauses, Result, SmartEnum, solution layout stubs) → Wave 1 (auditing + multi-tenancy contracts) → Wave 2 (`Entity`, the keystone — nothing should parallelize against it) → Wave 3 (`EntityAssociation` + TPT roots, in parallel) → Wave 4 (module system abstraction) → Wave 5 (domain-event dispatch convention + capability scaffold + startup validation) → Wave 6 (Specification pattern, needs concrete roots to specify against). Tests land continuously with each wave, not batched at the end.

### Critical Pitfalls

1. **Tenant filter gaps** (raw SQL, `IgnoreQueryFilters()`, background jobs never getting tenant context) — the highest-blast-radius bug class in multi-tenant systems. Avoid by making tenant resolution ambient (`AsyncLocal`-backed, not `HttpContext`-only) and designing tenant/soft-delete filters as independently liftable from day one. This is a Domain-layer *contract* decision now, even though enforcement is Infrastructure later.
2. **Soft-delete/cascade conflicts** — either soft-delete doesn't cascade to dependents (orphaned-looking data) or a leftover DB-level `ON DELETE CASCADE` silently hard-deletes anyway. Decide and document cascade behavior per `Entity`/`EntityAssociation` relationship now; default all FKs to `Restrict`.
3. **Domain event dispatch timing** — dispatching before commit risks corrupting the transaction on handler failure; dispatching after commit risks silently losing events on a crash. This milestone should only build the persistence-agnostic collection API (`AddDomainEvent`/`ClearDomainEvents`) and explicitly document the outbox-pattern expectation as a carry-forward decision for the Infrastructure milestone — not attempt to solve dispatch timing now.
4. **Under-scoped module system** — a hand-rolled "just call `AddModuleX()` in order" module system loses dependency-graph resolution and initialization dedup, which works fine for 2-3 modules and silently breaks (diamond dependencies double-initializing, or order-dependent registration) once the platform's 26 planned modules exist. Build real topological-sort graph resolution and a diamond-dependency unit test in this milestone, not later.
5. **Base classes left open to inheritance "just in case"** — a `protected virtual` added to `Entity`/`Party` "for future flexibility" becomes a standing contract 200+ downstream types may override, turning a one-file edit into a multi-module refactor later. Seal every class in the taxonomy by default; require a named, current second-implementation justification for any virtual member; route real per-type variation through composition over capability interfaces instead.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Domain.Shared Primitives + Solution Layout
**Rationale:** Everything else depends on GuardClauses/Result/SmartEnum; the stub project layout is pure plumbing with zero content risk and should exist before any real code so the dependency direction is compiler-enforced immediately.
**Delivers:** `GuardClauses`, `Result`/`Result<T>`, `SmartEnum<T>`, `DomainException` in Domain.Shared; empty `UseCases`/`Infrastructure`/`Web` stub projects wired into the solution with correct `ProjectReference` chains.
**Addresses:** Table-stakes primitives; Clean Architecture solution layout requirement.
**Avoids:** Pitfall of restructuring the solution later when the second module lands (Anti-Pattern 4 in ARCHITECTURE.md).

### Phase 2: Cross-Cutting Contracts (Audit, Soft-Delete, Multi-Tenancy)
**Rationale:** `Entity` needs these contracts to exist before it can be written; multi-tenancy in particular must NOT be treated as a late concern — it's needed by `Entity` itself.
**Delivers:** `ICreationAuditable`, `IModificationAuditable`, `ISoftDelete`, `IMultiTenant`, `TenantId`, tenant isolation tier enum — contracts/shape only, no resolution machinery.
**Uses:** Domain.Shared primitives from Phase 1.
**Implements:** Cross-cutting contract layer named in ARCHITECTURE.md's Domain.Shared structure.

### Phase 3: Entity Base Class (the keystone)
**Rationale:** The single highest-leverage class in the milestone; everything downstream depends on it landing correctly. Must resolve the aggregate-root/domain-event-collection decision here, not later.
**Delivers:** `Entity` abstract base with identity/equality, tenant scoping, audit fields, soft-delete convention, concurrency stamp, domain-event collection API. Explicit decision on aggregate-root distinction.
**Addresses:** Entity identity/equality, aggregate-root decision, domain event collection (Table Stakes in FEATURES.md).
**Avoids:** Pitfall 5 (base classes left open "just in case") — apply sealed-by-default discipline starting here, as a review-gate precedent for every subsequent phase.

### Phase 4: EntityAssociation + TPT Abstract Roots
**Rationale:** Both depend only on `Entity` and are independent of each other — can be built in parallel. `EntityAssociation`'s current-value-plus-history shape is the highest original-design-risk item in the milestone and deserves dedicated discussion time.
**Delivers:** `EntityAssociation` abstract base (named-kind + current+history shape); `Party`/`Item`/`Location`/`Activity`/`Document` abstract intermediate roots.
**Addresses:** The two most bespoke, no-external-prior-art requirements in PROJECT.md.
**Avoids:** Pitfall 5 (re-verify sealed-by-default discipline here as the second-highest-leverage checkpoint); logs TPT feature-limitation gaps (complex/owned types) as an explicit note for the next milestone rather than a silent assumption.

### Phase 5: Value Object Base + Capability Interface Scaffold
**Rationale:** Value Objects are independent of the Entity spine and can be built any time after Phase 1; the capability scaffold needs concrete TPT roots (Phase 4) to have real targets to validate against.
**Delivers:** Value Object base (structural equality/immutability — recommended addition); `IMergeable`/`IDisplayLabeled`/`IOfflineCapturable`/`ICustodyTracked` + `ICapabilityResolver` interface stub + registry contract + startup validation pass (interface-vs-registry drift check).
**Addresses:** Recommended gap #1 (Value Object) from FEATURES.md; the capability-scaffold differentiator.
**Avoids:** Security Mistake in PITFALLS.md — treating the registry-vs-interface system as advisory rather than enforced; the startup validation pass must ship in this phase, not be deferred.

### Phase 6: Module System (scoped) + Specification Pattern
**Rationale:** Module system has no `Entity` dependency and could start earlier, but is sequenced last since it's lower-leverage; Specification needs concrete roots (Phase 4) to specify against.
**Delivers:** `IFrameworkModule`/`ISentinelModule` marker + `[DependsOn]`-equivalent + topological sort + diamond-dependency unit test; `Specification<T>`/`ISpecification<T>` with `IsSatisfiedBy` only (no EF query-composition surface).
**Addresses:** Module system and Specification pattern requirements, deliberately scoped down per FEATURES.md/ARCHITECTURE.md.
**Avoids:** Pitfall 4 (under-scoped module system) — the diamond-dependency test is a hard acceptance criterion for this phase, not optional.

### Phase Ordering Rationale

- Dependency chain drives the order: primitives → cross-cutting contracts → Entity (keystone) → EntityAssociation/TPT roots (parallel) → capability scaffold (needs roots) → module system/Specification (lowest urgency, some pieces need roots).
- Grouping follows ARCHITECTURE.md's explicit build-wave analysis, which is grounded directly in actual code dependencies rather than PROJECT.md's Active-list ordering.
- Every phase touching the Entity/EntityAssociation/TPT hierarchy carries the sealed-by-default review-gate criterion forward — this is a standing constraint, not a one-time phase item.
- Domain-event dispatch timing and multi-tenancy resolution mechanics are explicitly *not* resolved in this milestone — they are documented decisions carried forward to the Infrastructure/persistence milestone, flagged so the Domain-layer contracts already anticipate them (outbox-compatible event shape; ambient-context-compatible tenant contract).

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 4 (EntityAssociation + TPT roots):** `EntityAssociation`'s current-value-plus-history shape has no external prior art (confirmed across all four research files) — budget extra discussion/design time, possibly a dedicated `/gsd-discuss-phase` pass before planning.
- **Phase 5 (Capability scaffold):** the startup validation pass (interface-vs-registry drift check) is a novel mechanism combining compile-time and runtime sources of truth — worth a focused design pass.
- **Phase 6 (Module system):** the diamond-dependency graph-resolution requirement needs care even at small scale; worth confirming the topological-sort approach against a concrete test scenario during planning.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Primitives + layout):** GuardClauses/Result/SmartEnum/solution layout are well-documented, mechanically simple, and thoroughly covered in STACK.md/ARCHITECTURE.md already.
- **Phase 2 (Cross-cutting contracts):** standard interface-property-bag shapes, well-precedented by ABP's own docs.
- **Phase 3 (Entity base class):** shape is well-documented via ABP source; the main risk (sealed-by-default discipline) is a review-process concern, not a research gap.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | Web search cross-checked against ABP/Ardalis GitHub source, Microsoft Learn, NuGet; no first-party Context7 doc tool available this session. |
| Features | MEDIUM-HIGH | ABP/Ardalis prior art is well-documented and cross-checked; Sentinel-Suite-specific synthesis (EntityAssociation, TPT taxonomy) has no external prior art to verify against. |
| Architecture | MEDIUM-HIGH | Patterns (Ardalis template, ABP layered solution) are HIGH confidence, drawn from official sources; the specific mapping onto Sentinel Suite's project names/namespaces is researcher synthesis, MEDIUM confidence. |
| Pitfalls | MEDIUM | Cross-checked against Microsoft Learn EF Core docs, ABP official docs, dotnet/efcore GitHub issues, multiple independent engineering write-ups; no primary vendor confirmation of .NET 10-specific EF Core behavior beyond cited docs. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **ABP `ILocalEventBus` dispatch-timing mechanics** were confirmed via search-engine summaries, not direct source-tree verification — worth re-verifying with a first-party doc tool before finalizing the domain-event collection API's assumptions, since a subtle timing mistake here is expensive to discover late (per STACK.md Gaps).
- **Multi-tenancy resolver design is intentionally left as a platform-specific design task** — research explicitly recommends NOT copying ABP's resolver chain, but the replacement design itself belongs in Phase 2's planning, informed by `_DECISIONS.md`'s tiered isolation model, not further external research.
- **Value Object base and explicit aggregate-root distinction are not currently in PROJECT.md's Active requirements** — both are strongly recommended additions per FEATURES.md; confirm with the user/PROJECT.md owner before roadmap finalization whether to formally add them to scope.
- **EF Core TPT feature limitations (complex types, owned types) are out of scope for this milestone** but constrain type-shape decisions made now (e.g., don't design value-object fields assuming owned-type EF mapping) — flagged explicitly for the next milestone's persistence/EF Core technical-spec phase, not something this roadmap needs to resolve.
- **Domain-event outbox pattern is a carry-forward decision**, not resolved this milestone — the Domain-layer event shape (immutable, serializable, no infra references) must be designed now to not require changes when Infrastructure adds the outbox next milestone.

## Sources

### Primary (HIGH confidence)
- [ardalis/CleanArchitecture (GitHub, official template repo)](https://github.com/ardalis/CleanArchitecture)
- [ABP.IO Docs: Layered Solution — The Structure](https://abp.io/docs/latest/solution-templates/layered-web-application/solution-structure)
- [Modeling for Performance - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance)
- [Inheritance - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance)
- [Global Query Filters - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [What's New in EF Core 11 | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)
- devblogs.microsoft.com / dotnet/core release notes (.NET 10 GA/LTS confirmation)

### Secondary (MEDIUM confidence)
- [Entities | ABP.IO Documentation](https://abp.io/docs/latest/framework/architecture/domain-driven-design/entities) and related ABP docs (multi-tenancy, DDD implementation guide)
- [NuGet Gallery | ardalis profiles](https://www.nuget.org/profiles/ardalis) — GuardClauses, SmartEnum, Specification, Result source verification
- [dotnet/efcore GitHub issues #2266, #14451, #35025](https://github.com/dotnet/efcore) — TPT/owned-type/complex-type limitations
- InfoQ, dev.to posts on FluentAssertions Xceed licensing change (Jan 2025)
- codewithmukesh.com posts on EF Core 10 global query filters and soft-delete named filters

### Tertiary (LOW confidence)
- DeepWiki summaries of ardalis/CleanArchitecture and abpframework/abp (third-party summaries of primary sources)
- Community benchmark posts on TPT vs. TPH performance magnitude (directional, not first-party controlled benchmarks)

---
*Research completed: 2026-07-15*
*Ready for roadmap: yes*
