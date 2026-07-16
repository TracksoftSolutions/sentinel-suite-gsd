# Requirements: Sentinel Suite — Framework Kernel (Milestone 1)

**Defined:** 2026-07-15
**Core Value:** Every one of Sentinel Suite's 222 planned features depends on getting the Entity/EntityAssociation taxonomy, multi-tenancy, and auditing conventions right once, in a dependency-minimal, FedRAMP-friendly kernel.

## v1 Requirements

Requirements for this milestone. Each maps to roadmap phases (populated during roadmap creation).

### Domain.Shared Primitives

- [x] **PRIM-01**: `GuardClauses` — hand-rolled argument/invariant validation helpers (Ardalis.GuardClauses-equivalent), zero NuGet dependency
- [x] **PRIM-02**: `Result` / `Result<T>` — hand-rolled operation-result pattern (Ardalis.Result-equivalent) for expected failure paths
- [ ] **PRIM-03**: `SmartEnum<T>` — hand-rolled type-safe enumeration base (Ardalis.SmartEnum-equivalent)
- [ ] **PRIM-04**: `DomainException` — a dedicated domain-level exception type, distinct from framework/infrastructure exceptions

### Solution Layout

- [ ] **LAYOUT-01**: Five-project Clean Architecture solution layout (`Domain.Shared`, `Domain`, `UseCases`, `Infrastructure`, `Web`) with the outer three as empty stub projects, correct `ProjectReference` chains, dependency direction compiler-enforced from day one

### Cross-Cutting Contracts

- [ ] **AUDIT-01**: Creation/modification auditing contracts (`ICreationAuditable`/`IModificationAuditable`-equivalent)
- [ ] **AUDIT-02**: Soft-delete contract (`ISoftDelete`-equivalent); cascade behavior per relationship documented, FKs default to `Restrict`
- [ ] **AUDIT-03**: Multi-tenancy contract (`IMultiTenant`-equivalent, `TenantId`, isolation-tier enum matching shared/dedicated_db/on_prem) — contract/shape only, no resolution machinery this milestone

### Entity Kernel

- [ ] **ENT-01**: `Entity` abstract base class — identity + equality (by concrete type + Id), tenant scoping, audit fields, soft-delete convention, optimistic concurrency stamp; sealed-by-default discipline applied from this class onward
- [ ] **ENT-02**: Explicit aggregate-root distinction, separate from plain `Entity` — resolves where domain events collect
- [ ] **ENT-03**: Domain event collection convention (`IDomainEvent` marker + `AddDomainEvent`/`ClearDomainEvents`), persistence-agnostic and outbox-compatible in shape (dispatch timing/outbox mechanism deferred to the Infrastructure milestone)
- [ ] **ENT-04**: Value Object base class — structural equality, immutability

### Association & Taxonomy

- [ ] **ASSOC-01**: `EntityAssociation` abstract base class — named-kind pattern, current-value-plus-history shape
- [ ] **ASSOC-02**: Abstract TPT intermediate roots — `Party`, `Item`, `Location`, `Activity`, `Document` — never instantiated directly

### Capability System

- [ ] **CAP-01**: Capability interface scaffold (e.g. `IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, `ICustodyTracked`)
- [ ] **CAP-02**: `ICapabilityResolver` interface stub + registry contract (concrete backing store deferred to the Infrastructure milestone)
- [ ] **CAP-03**: Startup validation pass asserting every developer-built type's implemented capability interfaces match its registrations — fails fast on drift, registration wins at runtime
- [ ] **CAP-04**: Extensible/extra-properties pattern (`IHasExtraProperties`-equivalent), paralleling the platform's existing `extended_fields` concept

### Module System

- [ ] **MOD-01**: Module system — `IFrameworkModule` marker + `[DependsOn]`-equivalent declaration + topological-sort dependency resolution + diamond-dependency handling. Deliberately scoped down: no DI bootstrapper, no reflection-based auto-registration (no host exists yet to bootstrap)

### Query Patterns

- [ ] **QUERY-01**: Specification pattern — `Specification<T>`/`ISpecification<T>` with `IsSatisfiedBy(T)` only; no EF query-composition surface this milestone (meaningless without persistence)

### Testing & Quality

- [ ] **TEST-01**: xUnit v3 (Microsoft.Testing.Platform) test project(s) covering every kernel invariant above, written as each piece lands rather than batched at the end

## v2 Requirements

Deferred to a future milestone (persistence/Infrastructure). Tracked but not in the current roadmap.

### Framework Evolution

- **FWEVO-01**: Full ABP-style module bootstrapper with DI conventions and reflection-based auto-registration
- **FWEVO-02**: Distributed/outbox domain event bus implementation (the decision to eventually need one is captured now via ENT-03's outbox-compatible event shape; the mechanism itself is next milestone)
- **FWEVO-03**: Multi-tenancy resolution + EF Core global-query-filter wiring (AUDIT-03 ships the contract only this milestone)
- **FWEVO-04**: Dynamic feature/permission/settings management modules (persistence-dependent, no concrete consumer yet)
- **FWEVO-05**: Localization system wired to `IDisplayLabeled`
- **FWEVO-06**: Source-generator-based strongly-typed ID toolchain

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Concrete domain types (Person, Employee, Vehicle, etc.) | This milestone is abstractions only; concrete types are the Master Records milestone (`docs/requirements/0.5-master-records/`) |
| EF Core persistence / `DbContext` / infrastructure-layer implementation | This milestone is the Domain project only |
| Application/API layer, any HTTP surface | Deferred to a future milestone |
| ABP NuGet packages, Ardalis pattern-library NuGet packages (`Ardalis.Specification`, `Ardalis.Result`, `Ardalis.GuardClauses`, `Ardalis.SmartEnum`), MediatR | Dependency minimalism — every dependency lengthens FedRAMP authorization; all these patterns are small enough to hand-roll |
| FluentAssertions 8+ | Requires a paid per-developer Xceed license as of Jan 2025; use plain xUnit `Assert` or MIT-licensed Shouldly instead |
| Generic `IRepository<T>` | Never build this — define narrow, aggregate-specific repositories only once persistence exists |
| Domain-event dispatch timing / outbox mechanism | Deferred to the Infrastructure milestone; only the persistence-agnostic collection API (ENT-03) ships now |
| Multi-tenancy resolution & EF Core query-filter wiring | Deferred to the Infrastructure milestone; only the contract (AUDIT-03) ships now |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PRIM-01 | Phase 1 | Complete |
| PRIM-02 | Phase 2 | Complete |
| PRIM-03 | Phase 3 | Pending |
| PRIM-04 | Phase 4 | Pending |
| LAYOUT-01 | Phase 5 | Pending |
| AUDIT-01 | Phase 6 | Pending |
| AUDIT-02 | Phase 7 | Pending |
| AUDIT-03 | Phase 8 | Pending |
| ENT-01 | Phase 9 | Pending |
| ENT-02 | Phase 10 | Pending |
| ENT-03 | Phase 10 | Pending |
| ENT-04 | Phase 11 | Pending |
| ASSOC-01 | Phase 12 | Pending |
| ASSOC-02 | Phase 13 | Pending |
| CAP-01 | Phase 14 | Pending |
| CAP-02 | Phase 15 | Pending |
| CAP-03 | Phase 16 | Pending |
| CAP-04 | Phase 17 | Pending |
| MOD-01 | Phase 18 | Pending |
| QUERY-01 | Phase 19 | Pending |
| TEST-01 | Phase 20 | Pending |

**Coverage:**

- v1 requirements: 21 total (corrected from the initial 20-count during roadmap creation — the enumerated PRIM/AUDIT/ENT/CAP/etc. lists above sum to 21, and REQUIREMENTS.md's opening count was off by one)
- Mapped to phases: 21/21 ✓
- Unmapped: 0 ✓ (100% coverage — see .planning/ROADMAP.md for phase details and success criteria)

---
*Requirements defined: 2026-07-15*
*Last updated: 2026-07-15 after roadmap revision (finer-grained phase split per user feedback — traceability re-populated, 21/21 requirements mapped across 20 phases; each Ardalis-branded pattern equivalent — GuardClauses, Result/Result<T>, SmartEnum<T>, Specification<T> — now lands in its own dedicated phase)*
