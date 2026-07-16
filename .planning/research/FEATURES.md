# Feature Research

**Domain:** .NET Clean Architecture / DDD domain-layer kernel (framework, not end-user product)
**Researched:** 2026-07-15
**Confidence:** HIGH for prior-art facts (ABP Framework and Ardalis conventions are well-documented and cross-checked against official docs/GitHub); MEDIUM-HIGH for project-specific synthesis and recommendations below, since several of Sentinel Suite's Active requirements (`EntityAssociation` current-value-plus-history, the Party/Item/Location/Activity/Document TPT taxonomy) are bespoke to this platform and have no direct external prior art to verify against.

**Read this as:** what capabilities a *domain-layer kernel* (not an app) needs to be genuinely usable as the foundation for 200+ downstream feature builds. "Features" = kernel mechanics/abstractions, not user-facing functionality.

## Feature Landscape

### Table Stakes (Every Serious .NET DDD Kernel Has These)

Missing these = the kernel is not actually a DDD kernel, just a folder named "Domain."

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Entity identity + equality (by concrete type + Id, not reference/property values) | Foundational DDD tactical pattern (Evans/Vernon); prevents subtle bugs when entities cross layer boundaries, get detached from a change tracker, or land in collections/dictionaries | LOW | PROJECT.md Active names "Entity abstract base class: identity" but doesn't spell out equality semantics — make `Equals`/`GetHashCode` override explicit in requirements, not assumed. |
| Aggregate root marker / distinction | DDD's actual unit of consistency; determines where domain events collect and (later) where repository/transaction boundaries land | LOW–MEDIUM | **Gap:** PROJECT.md Active lists `Entity` and `EntityAssociation` but no explicit `AggregateRoot` concept. Decide now: either reproduce ABP's `Entity`/`AggregateRoot` split, or explicitly rule that every TPT root (Party/Item/Location/Activity/Document) is its own aggregate root for this milestone. Don't leave it implicit — it blocks the domain-event-collection decision below. |
| Domain event collection convention | Lets an aggregate say "something happened" without knowing who's listening; ABP, MediatR-based kernels, and hand-rolled kernels all converge on this shape | LOW | Already Active. Minimal shape: `IDomainEvent` marker + protected `List<IDomainEvent>` + `AddDomainEvent`/`ClearDomainEvents` methods. Depends on the aggregate-root decision above (where does the list live). |
| Base audit contracts (creation/modification, granular) | Every audit/compliance-driven domain (this one especially, given FedRAMP/DOE posture) needs "who/when created or changed this" on every entity, not bolted on later | LOW | Already Active. Recommend ABP's granular-interface shape (`IHasCreationTime`, `ICreationAuditedObject`, `IHasModificationTime`, `IModificationAuditedObject`) over one monolithic base class, to stay consistent with `docs/architecture-guidance.md`'s "interface what a thing CAN" rule. |
| Soft-delete contract | Compliance/audit trail requirements (can't hard-delete records with legal/investigative value) are core to this domain | LOW | Already Active (bundled with auditing bullet in PROJECT.md). Deletion should be a method (`Delete()`), not a public `IsDeleted` setter — keeps the anti-anemic discipline from day one. |
| Optimistic concurrency stamp | Prevents silent lost-update bugs once multiple actors (API, sync engine, background jobs) write the same aggregate | LOW | **Gap:** not named in PROJECT.md Active. Cheap to add now (one property/interface); expensive to retrofit across 200+ downstream types once EF Core lands next milestone. Recommend adding an `IHasConcurrencyStamp`-equivalent to `Entity` now even though enforcement (EF Core `RowVersion`/`ConcurrencyStamp` wiring) is Infrastructure, deferred. |
| Tenant-scoping contract | Multi-tenancy is a day-one platform requirement (shared / dedicated_db / on_prem tiers), and retrofitting a `TenantId` onto 200+ types later is exactly the kind of foundation mistake this milestone exists to prevent | LOW | Already Active (multi-tenancy plumbing bullet). Marker interface + `TenantId` field on `Entity`; actual query-filtering wiring is Infrastructure, correctly deferred per Out of Scope. |
| Guard clauses for invariant enforcement | An entity that can be constructed into an invalid state has no real invariants, no matter what else the kernel provides | LOW | Already Active. Zero dependencies — build first. Constructors/methods should throw via guard clauses; invalid state should never exist even transiently. |
| Value Object base (structural equality, immutability) | As core to DDD tactical patterns as Entity — Money, Address, PersonName-shaped concepts recur across virtually every one of the platform's 26 modules | LOW–MEDIUM | **Real gap.** Not named in PROJECT.md Active. A Value Object is an abstraction, not a concrete domain type — it fits squarely inside this milestone's "abstractions only" scope. Without it, downstream modules will either hand-roll ad hoc equality logic 200 times or default to modeling everything as an Entity, defeating the taxonomy discipline `architecture-guidance.md` establishes. **Recommend adding to Active for this milestone.** |
| Domain-level exception type | Guard-clause failures need somewhere consistent to throw to | LOW | Implied but not explicit in Active. A single `DomainException`/`BusinessRuleViolationException` base (thrown by guard clauses) gives the Result pattern and the future API error-mapping layer one consistent vocabulary instead of leaking BCL exception types (`ArgumentException`, etc.) as the domain's error surface. |
| TPT abstract intermediate roots (Party/Item/Location/Activity/Document) | This platform's domain is genuinely taxonomic (NIEM-shaped, per `architecture-guidance.md`); the data model already committed to TPT | MEDIUM | Already Active. Bespoke to this platform, no ABP/Ardalis prior art. Complexity is in correctly identifying *shared* fields at each level (what genuinely belongs at Party vs. what's Employee-specific, deferred to next milestone) — not in the class mechanics. |
| `EntityAssociation` named-kind + current-value-plus-history shape | This platform's relationship model (e.g., "currently reports to X, with history of past managers") is a core, recurring pattern across modules | MEDIUM–HIGH | Already Active. Also bespoke — no direct ABP/Ardalis prior art (their default entity/repository shapes don't bake in a "current value + audit trail of prior values" pattern). **Highest original-design-risk item in this milestone** — budget the most discussion/design time here, since getting it wrong means every downstream relationship type inherits the mistake. |

### Differentiators (ABP-Style Value-Adds Worth Reproducing Now — Selectively)

Not required for a bare DDD kernel, but valuable enough that reproducing the *pattern* now (not the ABP package) pays for itself across 200+ downstream features. Each entry states what to keep minimal.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Extensible/extra-properties pattern (`IHasExtraProperties`-equivalent) | ABP's schema-less bag-of-properties mechanism lets tenant-defined/custom fields exist without a migration — directly parallels the `extended_fields` vs. concrete-column concept `architecture-guidance.md` already names as this platform's pattern | MEDIUM | Worth reproducing now (it's an interface + dictionary shape — an abstraction, in scope). The persistence (JSON column mapping) is Infrastructure, correctly deferred. |
| Capability-resolution abstraction (`ICapabilityResolver`-equivalent) | Formalizes `architecture-guidance.md`'s explicit rule ("a single capability-resolution service should hide which of the two answered — developer-built vs. tenant-defined") as a real Domain-layer interface now | MEDIUM | Defining the interface now lets `IMergeable`/`IDisplayLabeled`/etc. be designed against a real contract instead of a hand-waved one. Concrete implementation (reflection + registry lookup) is Infrastructure, next milestone. |
| Module system — **scoped down** | Organizing a 26-module, 222-feature platform into swappable pieces has real long-term value | MEDIUM | Already Active, but full ABP module system (DI-driven bootstrapper, `IModule` lifecycle hooks, options pattern) is YAGNI — there's no host/DI container in a Domain-only project to bootstrap yet. **Recommend scoping to:** (a) folder/namespace convention for organizing Domain into logical sub-areas, (b) a lightweight declarative module-manifest marker (e.g., a simple `IFrameworkModule` marker with a `Dependencies` list) a later milestone's real bootstrapper can read. |
| Specification pattern — **minimal shape only** | Decouples business-rule/query predicates from the types that consume them | LOW | Already Active. Ship only `abstract Specification<T> { bool IsSatisfiedBy(T candidate) }`-equivalent. Do **not** build Ardalis.Specification's full query-composition surface (`Include`/`ThenInclude`, ordering, paging, caching) — that's EF-Core-query-shaping machinery, meaningless without a persistence layer. |
| Result pattern — **placed in Domain.Shared** | Avoids exceptions-as-control-flow at layer boundaries | LOW | Already Active. Nuance worth flagging: Result is more natural at the Application layer (command/query handlers) than deep inside aggregate methods, which conventionally throw for invariant violations. Recommend Domain.Shared placement so Application can consume it next milestone without deep-coupling it into aggregate internals now. |
| SmartEnum pattern | Strongly-typed enum replacements with behavior, avoiding primitive int/string enums scattered across 222 features | LOW | Already Active. Zero dependencies — place in `Domain.Shared` (Application/Infrastructure will need it too, later). |
| The 4 named capability interfaces (`IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, `ICustodyTracked`) | Proves the capability-interface + registry-authoritative pattern with real, already-decided examples | LOW each | Already Active, explicitly scoped as a "scaffold." Treat these 4 as the *complete deliverable* — proving the pattern, not a down payment on enumerating every future capability across 26 modules. |
| `IDisplayLabeled` contract kept simple (returns `string`, not a localization key) | Capability needs a display-label mechanism now | LOW | In scope, but wiring it to a real localization system (ABP's `ILocalizableString`/`IStringLocalizer`) is not — keep the contract to a plain string return for this milestone. |

### Anti-Features (Common Over-Builds This Kernel Should Explicitly Avoid)

Every one of these has a plausible-sounding justification. All are traps for a Domain-only, 200+-feature-foundation milestone.

| Anti-Feature | Why Requested | Why Problematic | Alternative |
|--------------|----------------|------------------|-------------|
| Generic `IRepository<T>` (or any repository interface) in Domain | "Feels like the repository pattern DDD requires" | Encourages loading entities outside their aggregate boundary, leaks query concerns into Domain, and is already Out of Scope this milestone (no persistence) | Define **no** repository interface this milestone. When persistence lands, define narrow, aggregate-specific repositories only for genuine consistency boundaries — never one generic CRUD interface. |
| Full ABP module bootstrapper / DI conventions now | "Module system" is literally in PROJECT.md Active | There's no host or DI container in a Domain-only project to bootstrap against — building this now is speculative generality with nothing to validate it against | Ship the lightweight manifest/marker convention (Differentiators table above); build the real bootstrapper when Infrastructure/Web layers exist. |
| Anemic base classes (property-bag `Entity`/`EntityAssociation` with public setters everywhere) | Fastest thing to type | Sets the anemic precedent for all 200+ downstream types; directly contradicts the reason DDD was chosen; makes audit/soft-delete fields mutable from anywhere instead of via invariant-protecting methods | Protected/private setters; behavior methods (`Delete()`, `Touch()`, `AddDomainEvent()`) instead of public property mutation; constructors that run guard clauses. |
| Virtual "extension point" methods on `Entity`/`Party`/`Activity` "just in case" | Feels safer to over-provide extensibility since 200+ features build on this | Directly contradicts `architecture-guidance.md`'s explicit rule: "sealed by default... extension points are designed, not defaulted." Deep virtual chains are the yo-yo/fragile-base-class problem the same doc names | Seal by default. Add a genuine extension point deliberately, with a name and a reason, when one actually emerges — never preemptively. |
| Dynamic feature/permission/settings management modules (ABP's FeatureManagement/PermissionManagement/SettingManagement) | ABP bundles these; the research question explicitly names them as things ABP adds beyond bare DDD | All three require persistence (per-tenant stored values), typically an API/UI surface, and dynamic runtime lookup — none of which exist yet this milestone; there's nothing real to validate the abstraction against | Defer entirely. Evaluate later whether Sentinel Suite needs ABP's dynamic-feature-flag model vs. the platform's own simpler "Settings & Preferences" concept (already named separately in the requirements docs). |
| MediatR-style reflection-based event dispatch machinery | Domain events need to notify something eventually | Full pipeline-behavior/handler-auto-discovery is Infrastructure/Application machinery; PROJECT.md explicitly says hand-rolled, no MediatR | Domain only needs the `IDomainEvent` marker + collection-on-aggregate convention this milestone; the dispatcher that walks the list and invokes handlers is Infrastructure, next milestone. |
| Source-generator-based strongly-typed IDs | Strongly-typed IDs (`PersonId` vs. bare `Guid`) are a well-known DDD hygiene win with slick generator tooling available | A source-generator toolchain is a new build-pipeline dependency and failure surface for a FedRAMP-constrained solo-dev project; premature when no concrete ID-bearing types exist yet | If wanted, hand-write one generic minimal `EntityId<T>` wrapper struct — or defer the decision entirely to next milestone when concrete types make the tradeoff concrete. |
| Exhaustively enumerating every future capability interface now | Feels efficient to "just design the whole interface catalog" while already in kernel headspace | 26 modules / 222 features haven't been analyzed for capability needs yet; interfaces invented without a concrete consumer are exactly the "marker interface with no members is a smell" trap `architecture-guidance.md` warns about | Ship the 4 named example interfaces from PROJECT.md Active as the pattern proof; let real capability interfaces get added module-by-module as downstream milestones actually need them. |

## Feature Dependencies

```
Domain.Shared primitives (no deps, build first)
  GuardClauses ──┐
  SmartEnum ─────┼── all zero-dependency, foundational
  Result ────────┘
  DomainException ──requires──> GuardClauses (guard failures throw it)

Entity (identity + equality)
  └──requires──> GuardClauses (constructor invariant enforcement)
  └──requires──> Aggregate-root distinction decision (where do domain events live?)
  └──composed-with──> Concurrency stamp, Tenant-scoping contract, Audit contracts, Soft-delete contract

AggregateRoot marker/distinction ──requires──> Entity
Domain event collection convention ──requires──> AggregateRoot marker/distinction (or Entity, if the decision is "every entity is its own root")

EntityAssociation ──requires──> Entity (association endpoints reference Entity-typed parties)
TPT abstract intermediate roots (Party/Item/Location/Activity/Document) ──requires──> Entity

Value Object base ──independent of──> Entity (parallel track, no dependency either direction)

Capability interface scaffold (IMergeable, IDisplayLabeled, IOfflineCapturable, ICustodyTracked)
  └──conceptually depends on──> registry-authoritative discipline (documented, not code-dependent)
Capability-resolution abstraction (ICapabilityResolver) ──enhances──> Capability interface scaffold

Specification pattern ──requires──> Entity (specs are typed over T : Entity)
Extensible/extra-properties pattern ──requires──> Entity (the property bag hangs off Entity)

Module system (scoped) ──requires──> Clean Architecture solution layout (needs project boundaries to declare modules across)
Clean Architecture solution layout ──independent──> (foundational, do alongside Domain.Shared primitives)

Unit test coverage ──requires──> everything above (written as each piece lands, per PROJECT.md)

[Next milestone] Concrete domain types ──requires──> Entity, EntityAssociation, TPT roots, Value Object base, Capability interfaces (ALL of this milestone)
[Next milestone] EF Core persistence ──requires──> Concurrency stamp, Tenant-scoping contract, Soft-delete contract (Domain-level contracts must exist before Infrastructure wires them)
[Next milestone] Domain event dispatcher ──requires──> Domain event collection convention
[Next milestone] Repository interfaces ──requires──> AggregateRoot marker/distinction (repos are defined per aggregate root, never generically)
```

### Dependency Notes

- **Everything requires Domain.Shared primitives:** `GuardClauses`, `SmartEnum`, `Result`, and a `DomainException` base have zero dependencies and are consumed by nearly everything else. Build these first.
- **The aggregate-root decision gates domain events:** you cannot finalize the domain-event-collection convention until you decide whether `Entity` itself collects events or only a distinct `AggregateRoot` subtype does. Resolve this early — it's a one-time decision that everything else assumes.
- **`EntityAssociation` and the TPT roots both depend on `Entity`,** but not on each other — they can be designed in parallel once `Entity` is stable.
- **Value Objects are independent of Entity** and can be built in parallel with the identity spine — there's no reason to sequence them after `Entity`.
- **The module system depends on the Clean Architecture solution layout existing** (it needs project boundaries to organize modules across), but both are largely mechanical and can be finished last without blocking the domain-modeling work.
- **Next milestone's concrete types depend on the entire output of this milestone** — this is the whole point of the milestone, and the reason getting `EntityAssociation`'s shape right now carries outsized risk.

## MVP Definition

Reframed for a kernel milestone: "launch" = what ships as this milestone's Domain project; "v1.x" = what the *next* milestone (concrete Master Records types + persistence) needs from this one; "v2+" = deferred ABP-style capabilities that are legitimate future differentiators but would be scope creep now.

### Launch With (This Milestone — Domain Abstractions Only)

- [ ] `GuardClauses`, `SmartEnum`, `Result`, `DomainException` in `Domain.Shared` — zero-dependency primitives everything else needs
- [ ] `Entity` abstract base: identity + equality, tenant-scoping contract, granular audit interfaces, soft-delete contract, concurrency stamp
- [ ] Explicit aggregate-root decision + domain event collection convention (`IDomainEvent` + `AddDomainEvent`/`ClearDomainEvents`)
- [ ] `EntityAssociation` abstract base: named-kind + current-value-plus-history shape
- [ ] TPT abstract intermediate roots: `Party`, `Item`, `Location`, `Activity`, `Document`
- [ ] Value Object base (structural equality, immutability) — **recommended addition, not in current PROJECT.md Active**
- [ ] Capability interface scaffold: `IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, `ICustodyTracked` + documented registry-authoritative discipline
- [ ] `ICapabilityResolver`-equivalent interface stub (concrete impl deferred)
- [ ] Extensible/extra-properties interface (`IHasExtraProperties`-equivalent) — **recommended addition**
- [ ] Specification pattern, minimal `IsSatisfiedBy(T)` shape only
- [ ] Multi-tenancy plumbing: tenant resolution + data-filtering *convention* (not the EF Core implementation)
- [ ] Module system, scoped down to a manifest/marker convention (not a DI bootstrapper)
- [ ] Clean Architecture solution layout: project separation + dependency-direction enforcement (layout only)
- [ ] xUnit tests for every kernel invariant and capability interface, written as each piece lands

### Add After Validation (Next Milestone — Master Records Build-Out)

- [ ] Concrete domain types (Person, Employee, Vehicle, etc.) built on this kernel
- [ ] EF Core persistence, `DbContext`, TPT mapping implementation, actual tenant query-filter wiring
- [ ] Domain event dispatcher / handler resolution (Infrastructure)
- [ ] `ICapabilityResolver` concrete implementation + startup validation pass (interface-vs-registry drift check)
- [ ] Aggregate-specific repository interfaces (only once real aggregates and persistence exist)

### Future Consideration (v2+ — Deliberately Deferred)

- [ ] Full ABP-style module system with DI bootstrapping and configuration options pattern
- [ ] Dynamic feature/permission/settings management modules
- [ ] Localization system wired to `IDisplayLabeled`
- [ ] Distributed/outbox domain event bus (multi-process/service event delivery)
- [ ] Source-generator-based strongly-typed ID toolchain

## Feature Prioritization Matrix

"Value" here means how much of the 200+-feature backlog depends on this being right; "Cost" means implementation + design-risk complexity.

| Feature | Value to Backlog | Implementation Cost | Priority |
|---------|-------------------|----------------------|----------|
| `Entity` identity/equality + audit/soft-delete/tenant/concurrency contracts | HIGH | LOW | P1 |
| Aggregate-root decision + domain event collection | HIGH | LOW | P1 |
| `EntityAssociation` current-value-plus-history | HIGH | HIGH | P1 |
| TPT abstract intermediate roots | HIGH | MEDIUM | P1 |
| Value Object base | HIGH | LOW-MEDIUM | P1 (recommended add) |
| GuardClauses / SmartEnum / Result / DomainException | HIGH | LOW | P1 |
| Capability interface scaffold (4 named) | MEDIUM-HIGH | LOW | P1 |
| Specification pattern (minimal) | MEDIUM | LOW | P1 |
| Multi-tenancy plumbing (convention only) | HIGH | LOW-MEDIUM | P1 |
| Extensible/extra-properties pattern | MEDIUM | MEDIUM | P2 |
| `ICapabilityResolver` interface stub | MEDIUM | LOW | P2 |
| Module system (scoped manifest) | LOW-MEDIUM | LOW | P2 |
| Clean Architecture layout enforcement | MEDIUM | LOW | P1 |
| Full ABP module bootstrapper | LOW (this milestone) | HIGH | P3 / defer |
| Dynamic feature/permission/settings mgmt | LOW (this milestone) | HIGH | P3 / defer |

**Priority key:** P1 = must have for this milestone to be "genuinely done." P2 = should have, cheap enough to include if time allows. P3 = defer to a future milestone.

## Prior Art Comparison

| Concern | ABP Framework | Ardalis Clean Architecture | This Project (Sentinel Suite Kernel) |
|---------|----------------|------------------------------|----------------------------------------|
| Entity base | `Entity`/`AggregateRoot` + audited variants (`AuditedEntity`, `FullAuditedAggregateRoot`, etc.) | Minimal `EntityBase`, often paired with a `SharedKernel` project | Hand-rolled `Entity` (+ recommended `AggregateRoot` distinction), matching ABP's granular-audit-interface shape without the package dependency |
| Domain events | Built-in local + distributed event bus, raised on aggregates | Not opinionated (left to consumer, often MediatR-based) | Hand-rolled: marker + collection-on-aggregate only; dispatcher deferred to Infrastructure (explicitly no MediatR) |
| Multi-tenancy | `IMultiTenant` interface + reflection-driven global EF Core query filter + pluggable tenant resolvers | Not a built-in concern | Convention only this milestone (marker + `TenantId`); EF Core filter wiring deferred, matching the platform's tiered isolation model |
| Specification | Full specification pattern, tightly integrated with repositories | `Ardalis.Specification` package: predicate + query-composition (Include, paging, ordering) | Minimal `IsSatisfiedBy(T)` predicate only this milestone — query-composition is meaningless without persistence, deferred |
| Guard clauses | Present but less central than Ardalis's | `Ardalis.GuardClauses` is the canonical small package for this | Hand-rolled equivalent, no package dependency (FedRAMP minimalism) |
| Value objects | Built-in `ValueObject` base (structural equality) | Present in template's Domain project | **Gap in current PROJECT.md Active — recommended addition**, same shape as both prior-art frameworks |
| Module system | Hundreds of small NuGet/npm packages, `IModule` lifecycle, DI-driven bootstrapper, pre-built app modules (Identity, Tenant Mgmt, etc.) | Not a first-class concern (single solution, project-per-layer) | Scoped down to a lightweight manifest/marker convention this milestone — full bootstrapper deferred (no host/DI container exists yet) |
| Dynamic feature/permission/settings management | First-class modules (Feature Management, Permission Management, Setting Management), persistence-backed | Not present | Explicitly deferred — persistence-dependent, no concrete consumer this milestone |

## Explicit Mapping to PROJECT.md

| PROJECT.md Active Requirement | Category Assigned | Notes |
|---|---|---|
| `Entity` abstract base class (identity, tenant scoping, audit, soft-delete) | Table Stakes | Confirmed correct scope; recommend making equality semantics and concurrency stamp explicit sub-requirements. |
| `EntityAssociation` abstract base class | Table Stakes | Confirmed; highest design-risk item — no external prior art, budget extra design time. |
| TPT abstract intermediate roots (Party/Item/Location/Activity/Document) | Table Stakes | Confirmed; bespoke to platform, correctly in scope as abstractions (no concrete extensions). |
| Capability interface scaffold (4 named interfaces + registry discipline) | Differentiator | Confirmed correctly scoped as a "scaffold" — treat the 4 named interfaces as the complete deliverable, not a starting enumeration. |
| Domain events: collection + dispatch convention | Table Stakes (collection) / correctly deferred (dispatch mechanics) | Collection convention is table stakes now; full dispatcher is Infrastructure, correctly out of this milestone's actual scope even though the requirement names "dispatch convention" — interpret that as documenting the *convention*, not building a working dispatcher. |
| Multi-tenancy plumbing | Differentiator (vs. bare DDD) | Confirmed correctly scoped as *convention*, not the EF Core query-filter implementation (which needs persistence, out of scope). |
| Auditing & soft-delete base contracts | Table Stakes | Confirmed; recommend the granular-interface shape over one monolithic base. |
| Module system (ABP-inspired) | Differentiator — **recommend scoping down** | Full ABP module system is scope creep this milestone (no DI container to bootstrap); scope to manifest/marker convention only. |
| Specification pattern | Differentiator — **recommend scoping down** | Ship `IsSatisfiedBy(T)` only; the query-composition surface (Include/paging/ordering) is Infrastructure-dependent, deferred. |
| Result pattern | Differentiator | Confirmed; recommend `Domain.Shared` placement since Application will consume it too. |
| GuardClauses | Table Stakes | Confirmed; zero dependencies, build first. |
| SmartEnum | Differentiator | Confirmed; place in `Domain.Shared`. |
| Clean Architecture solution layout (layout only) | Table Stakes | Confirmed correctly scoped — layout/dependency-direction only, no premature Application/Infrastructure content. |
| xUnit test coverage | Table Stakes | Confirmed; written incrementally as each piece lands, per PROJECT.md. |

| PROJECT.md Out of Scope Item | Research Confirms Correctly Excluded? |
|---|---|
| Concrete domain types (Person, Employee, Vehicle, etc.) | Yes — these depend on every table-stakes item in this milestone; correctly sequenced as next milestone. |
| EF Core persistence / `DbContext` / infrastructure code | Yes — Domain-level *contracts* (concurrency stamp, tenant marker, soft-delete) should exist now so Infrastructure has something to wire to later, but no EF Core code belongs in this milestone. |
| Application/API layer, HTTP surface | Yes — Result pattern placement in `Domain.Shared` anticipates this without building it now. |
| Depending on real ABP NuGet packages | Yes — every recommendation above is "reproduce the pattern," never "take the dependency." |
| `Ardalis.*` packages, MediatR, etc. | Yes — hand-rolled minimal shapes recommended throughout (especially: skip Ardalis.Specification's full query-composition surface, skip MediatR's reflection-based dispatch). |

### Gaps Found (Recommend Adding to Active for This Milestone)

1. **Value Object base class** — structural equality/immutability abstraction, same category as `Entity`/`EntityAssociation` (an abstraction, not a concrete type), currently absent from Active.
2. **Explicit aggregate-root marker/distinction** — currently implicit; blocks a clean answer to "where do domain events live."
3. **Optimistic concurrency stamp contract** — cheap now, expensive to retrofit across 200+ types later.
4. **Domain-level exception type** — implied by GuardClauses but not named; needed for a consistent error vocabulary before the Result pattern and later API error-mapping.
5. **Extensible/extra-properties pattern** — parallels the platform's already-decided `extended_fields` concept from `architecture-guidance.md`; worth adding now while it's still just an abstraction.

## Sources

- [Domain Driven Design: Domain Layer | ABP.IO Documentation](https://abp.io/docs/9.0/framework/architecture/domain-driven-design/domain-layer)
- [Entities | ABP.IO Documentation](https://abp.io/docs/latest/framework/architecture/domain-driven-design/entities)
- [Understanding the Domain and Application Layers in ABP Framework | ABP.IO Medium](https://medium.com/volosoft/understanding-the-domain-and-application-layers-in-abp-framework-83bcf8c531ec)
- [Multi-Tenancy | ABP.IO Documentation](https://abp.io/docs/latest/framework/architecture/multi-tenancy)
- [Multi Tenancy Architecture with .NET | ABP.IO](https://abp.io/architecture/multi-tenancy)
- [Tenant Management Module | ABP.IO Documentation](https://docs.abp.io/en/abp/latest/Modules/Tenant-Management)
- [ABP Architecture | ABP.IO Documentation](https://abp.io/docs/9.0/framework/architecture)
- [NuGet Gallery | ardalis (GuardClauses, SmartEnum, Specification, Result profiles)](https://www.nuget.org/profiles/ardalis)
- [ardalis (Steve "Ardalis" Smith) · GitHub](https://github.com/ardalis)
- [NuGet Gallery | Ardalis.CleanArchitecture.Template](https://www.nuget.org/packages/Ardalis.CleanArchitecture.Template/)
- [Applying Clean Architecture to ASP.NET Core Apps — Steve Smith](https://devintxcontent.blob.core.windows.net/showcontent/Speaker%20Presentations%20Fall%202019/Clean%20Architecture%20with%20ASP.NET%20Core%20(1).pdf)
- [bliki: Anemic Domain Model — Martin Fowler](https://martinfowler.com/bliki/AnemicDomainModel.html)
- [Anemic domain model — Wikipedia](https://en.wikipedia.org/wiki/Anemic_domain_model)
- [Clean Architecture in .NET: Moving Beyond Generic Repositories — DEV Community](https://dev.to/korirmoze/clean-architecture-in-net-moving-beyond-generic-repositories-and-leveraging-iservicescopefactory-11gb)
- Internal: `.planning/PROJECT.md`, `docs/architecture-guidance.md` (this platform's own inheritance/interface/composition rules and the three-way rule, referenced throughout)

---
*Feature research for: .NET Clean Architecture / DDD domain kernel (Sentinel Suite Milestone 1)*
*Researched: 2026-07-15*
