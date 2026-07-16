# Architecture Research

**Domain:** Hand-rolled .NET Clean Architecture / DDD framework kernel (ABP + Ardalis patterns, no package dependency)
**Researched:** 2026-07-15
**Confidence:** MEDIUM-HIGH (patterns are HIGH confidence, drawn directly from Ardalis's own template and ABP's official docs; the specific mapping onto Sentinel Suite's project names/namespaces is this researcher's synthesis — MEDIUM confidence, flag for a quick sanity check before Phase 1 lands code)

## Standard Architecture

### System Overview — Ardalis Clean Architecture (source pattern)

```
┌─────────────────────────────────────────────────────────────┐
│  Web  (composition root: DI wiring, controllers/endpoints)   │
│  depends on → UseCases, Infrastructure                        │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure  (EF Core, external services, adaptors)       │
│  depends on → UseCases, Core   (implements their interfaces)  │
├─────────────────────────────────────────────────────────────┤
│  UseCases / Application  (handlers, app services, DTOs)       │
│  depends on → Core only                                       │
├─────────────────────────────────────────────────────────────┤
│  Core / Domain  (entities, aggregates, domain services,        │
│  repository interfaces, specifications, domain events)        │
│  depends on → NOTHING project-local (zero project refs)       │
└─────────────────────────────────────────────────────────────┘
```

The Dependency Rule: source dependencies point inward only. Core has zero project references — it may reference small NuGet primitives (Ardalis's real template pulls in `Ardalis.SharedKernel`, `Ardalis.Specification`; Sentinel Suite hand-rolls the equivalent of that package *inside* Core/Domain instead, per the no-third-party-pattern-package constraint). UseCases depends only on Core. Infrastructure depends on Core + UseCases, implementing the interfaces both declare. Web is the composition root — it depends on UseCases + Infrastructure and wires everything together at startup. [Source: github.com/ardalis/CleanArchitecture, HIGH confidence — official template repo]

### System Overview — ABP layered solution (second source pattern, informs the Domain/Domain.Shared split specifically)

```
┌───────────────────────────────────────────────────────────┐
│  HttpApi / Web            (controllers, composition root)   │
├───────────────────────────────────────────────────────────┤
│  Application               (app service implementations)    │
│  Application.Contracts     (app service interfaces + DTOs)  │
├───────────────────────────────────────────────────────────┤
│  EntityFrameworkCore       (DbContext, repository impls)    │
├───────────────────────────────────────────────────────────┤
│  Domain                    (entities, aggregates, domain     │
│                              services, repository interfaces)│
├───────────────────────────────────────────────────────────┤
│  Domain.Shared              (enums, constants, cross-layer   │
│                               contracts — ZERO deps)         │
└───────────────────────────────────────────────────────────┘
```

Domain.Shared has no project dependency at all; every other project depends on it directly or indirectly. Domain depends on Domain.Shared for the constants/enums/contracts it needs. [Source: abp.io official docs, "Layered Solution: The Structure" and "Solution structure", HIGH confidence]

**Why Sentinel Suite needs both sources:** Ardalis's template answers "how do Domain/Application/Infrastructure/Web relate" (this milestone's `.csproj` stub question). ABP's template answers "what specifically is the Domain vs. Domain.Shared split" (a distinction Ardalis's template doesn't have at all — Ardalis just has one `Core` project). Sentinel Suite's existing scaffold already committed to the ABP-shaped split (`Domain` + `Domain.Shared`, Domain → Domain.Shared, Domain.Shared has zero refs — confirmed in the current `.csproj` files), so this doc treats that as fixed and grafts Ardalis's outer layers on top of it.

## Component Responsibilities (Sentinel Suite framework kernel)

| Component | Responsibility | This milestone |
|-----------|-----------------|-----------------|
| `SentinelSuite.Framework.Domain.Shared` | Zero-dependency primitives every other layer needs: `Result`/`Result<T>`, `Guard` (GuardClauses), `SmartEnum<T>` base, shared enums (e.g. tenant isolation tier), lightweight cross-layer contracts (`ICreationAuditable`, `IModificationAuditable`, `ISoftDelete`, `IMultiTenant`, `TenantId`) | **Real content.** Built first — everything else depends on it. |
| `SentinelSuite.Framework.Domain` | The taxonomy itself: `Entity`, `EntityAssociation`, TPT intermediate roots (`Party`/`Item`/`Location`/`Activity`/`Document`), domain-specific capability interfaces (`IMergeable`, `IDisplayLabeled`, etc.), the capability registry contract, domain events, Specification base, module-system abstraction | **Real content.** This milestone's actual deliverable. |
| `SentinelSuite.Framework.UseCases` | Ardalis's "UseCases" layer stand-in — app-service/handler shape, orchestrates Domain via repository interfaces and specifications; no HTTP, no persistence | **Stub only.** Empty project + `ProjectReference` to Domain. No code. |
| `SentinelSuite.Framework.Infrastructure` | EF Core `DbContext`, repository implementations, provider adaptors (GIS, notification, AI/LLM — per architecture-guidance §5), the capability registry's real (DB-backed) implementation | **Stub only.** Empty project + refs to Domain + UseCases. No code. |
| `SentinelSuite.Framework.Web` | Composition root: DI wiring, module loading/bootstrap, HTTP surface (later) | **Stub only.** Empty project + refs to UseCases + Infrastructure. No code. |

**Scope note on "Framework" vs. product modules:** these five projects are the *kernel* — shared base classes and conventions. The 26 product modules (Emergency Planning, Dispatch, etc.) are **not** built inside this solution. Later milestones create separate module solutions/projects that take a `ProjectReference` (or, once versioned, a package reference) to `SentinelSuite.Framework.Domain`/`.Shared` and build concrete entities (`Employee`, `Vehicle`, …) on top of the abstract TPT roots defined here. Do not let concrete module entities creep into `SentinelSuite.Framework.Domain` — the abstract-only rule (see Out of Scope in PROJECT.md) is also an architectural boundary, not just a milestone-scoping rule.

## Recommended Project Structure

### Solution layout — add now, keep empty except Domain/Domain.Shared

```
SentinelSuite/
├── SentinelSuite.slnx
├── SentinelSuite.Framework.Domain.Shared/         # EXISTS — zero project refs
│   └── SentinelSuite.Framework.Domain.Shared.csproj
├── SentinelSuite.Framework.Domain/                # EXISTS — refs Domain.Shared
│   └── SentinelSuite.Framework.Domain.csproj
├── SentinelSuite.Framework.UseCases/              # NEW STUB — refs Domain
│   └── SentinelSuite.Framework.UseCases.csproj
├── SentinelSuite.Framework.Infrastructure/        # NEW STUB — refs Domain, UseCases
│   └── SentinelSuite.Framework.Infrastructure.csproj
└── SentinelSuite.Framework.Web/                   # NEW STUB — refs UseCases, Infrastructure
    └── SentinelSuite.Framework.Web.csproj
```

Add all three new stub `.csproj` files with the correct `<ProjectReference>` chain and register them in `SentinelSuite.slnx` **now**, even though they contain zero source files, so:
- The dependency direction is enforced by the build graph from day one (a future accidental `Infrastructure → Domain.Shared`-skipping-`Domain` reference, or a `Domain → Infrastructure` reference, fails to compile immediately instead of surfacing as a design review comment two milestones later).
- Adding real content later is "drop files into an existing project," never "restructure the solution while code already depends on the old shape."
- This directly satisfies the Active requirement *"Clean Architecture solution layout... layout only in this milestone."*

A future decision (not this milestone): whether `SentinelSuite.Framework.UseCases`/`Web` end up thin (framework only defines contracts; each product module gets its own `.UseCases`/`.Web`) or thick (the framework's own composition root hosts everything). Either way the *layout* above doesn't need to change — only content gets added.

### Domain project internal structure — namespaces/folders

```
SentinelSuite.Framework.Domain/
├── Entities/
│   ├── Entity.cs                      # abstract root — identity, TenantId, audit fields, soft-delete flag
│   ├── Party/
│   │   └── Party.cs                   # abstract intermediate root (Party : Entity) — never instantiated
│   ├── Item/
│   │   └── Item.cs                    # abstract intermediate root
│   ├── Location/
│   │   └── Location.cs                # abstract intermediate root
│   ├── Activity/
│   │   └── Activity.cs                # abstract intermediate root
│   └── Document/
│       └── Document.cs                # abstract intermediate root
│   # Concrete extensions (Employee, Vehicle, ...) do NOT land here — next milestone,
│   # in the product-module projects that reference this one.
├── Associations/
│   └── EntityAssociation.cs           # abstract root — named-kind pattern, current+history shape
│   # Named kinds (concrete EntityAssociation subtypes) are also next-milestone content,
│   # UNLESS a kind is field-less (see architecture-guidance §"earn-your-existence"),
│   # in which case a TPH-mapped named type may live here later as a technical-spec call —
│   # not a decision to make in this milestone.
├── Abstractions/
│   └── Capabilities/
│       ├── IMergeable.cs
│       ├── IDisplayLabeled.cs
│       ├── IOfflineCapturable.cs
│       ├── ICustodyTracked.cs
│       └── ...                        # one file per capability interface, as requirements dock's
│                                       # capability declarations acquire code counterparts
├── DomainEvents/
│   ├── IDomainEvent.cs
│   └── (Entity exposes the collection-on-aggregate API directly — see architecture-guidance
│         §1: base classes carry state shape; the event *list* is state, so it belongs on Entity
│         itself, not a separate marker interface)
├── Registries/
│   ├── ICapabilityRegistry.cs         # registration lookup contract — the "authoritative" source
│   ├── CapabilityRegistration.cs      # registration record shape (is_mergeable, display_label_strategy, ...)
│   ├── ICapabilityResolver.cs         # the "single service hides interface-vs-registry" contract
│   │                                    # (architecture-guidance §"Quick reference" + EF Core notes)
│   └── InMemoryCapabilityRegistry.cs  # dev/test reference implementation ONLY —
│                                       # a real persistent-backed implementation is Infrastructure's
│                                       # job in a later milestone; this milestone stays inside Domain
│                                       # per the "no infrastructure-layer code" scope boundary
├── Specifications/
│   ├── ISpecification.cs
│   └── Specification.cs               # Ardalis-style base, hand-rolled
├── Modules/
│   ├── ISentinelModule.cs             # module descriptor contract + declared dependencies
│   └── SentinelModule.cs              # abstract base with lifecycle hook signatures
│                                       # (the runtime LOADER that discovers/wires modules is a
│                                       # Web/composition-root concern — out of scope here; this
│                                       # milestone only needs the Domain-visible abstraction so
│                                       # capability-registry startup validation has something to
│                                       # hook into later)
└── MultiTenancy/
    └── (tenant *resolution* — reading a header/claim — is Web/Infrastructure's job later;
         this milestone only needs the Domain-side shape, which lives in Domain.Shared —
         see below — because Entity itself must implement IMultiTenant)
```

### Domain.Shared project internal structure

```
SentinelSuite.Framework.Domain.Shared/
├── Kernel/
│   ├── Result.cs                      # Result / Result<T> — Ardalis-style, hand-rolled
│   ├── Guard.cs                       # GuardClauses-style extension methods
│   └── SmartEnum.cs                   # SmartEnum<T> base — Ardalis-style, hand-rolled
├── Contracts/
│   ├── ICreationAuditable.cs          # CreatedAt / CreatedBy
│   ├── IModificationAuditable.cs      # ModifiedAt / ModifiedBy
│   ├── ISoftDelete.cs                 # IsDeleted (+ DeletedAt/DeletedBy if the audit decisions call for it)
│   └── IMultiTenant.cs                # TenantId property
├── MultiTenancy/
│   ├── TenantId.cs                    # strongly-typed identifier (not a bare Guid)
│   └── TenantIsolationTier.cs         # SmartEnum or enum: shared / dedicated_db / on_prem
└── Enums/
    └── (other genuinely cross-layer enums as they're identified — resist adding
        anything here that only Domain uses; that belongs in Domain instead)
```

### Structure Rationale

- **Why `Result`/`Guard`/`SmartEnum` live in Domain.Shared, not Domain:** these are pure, zero-domain-semantics utility types. `UseCases`/`Web` will want to return `Result<T>` and validate arguments with `Guard` without pulling in the entire entity taxonomy — exactly the reasoning ABP applies to enums/constants in Domain.Shared ("needed to be used by all layers"). If they lived in Domain, every future layer that wants a `Result<T>` would transitively depend on `Entity`, `EntityAssociation`, and the whole taxonomy, which is backwards.
- **Why the auditing/multi-tenancy *contracts* (`ICreationAuditable`, `IMultiTenant`, etc.) live in Domain.Shared but `Entity` (which implements them) lives in Domain:** the interfaces are simple property bags with no entity coupling — a future read-model DTO in the Application layer may want to expose `CreatedAt`/`CreatedBy` without depending on `Entity` itself. Domain.Shared is where "shape everyone needs" belongs; Domain is where "the taxonomy that uses that shape" belongs.
- **Why capability interfaces (`IMergeable`, etc.) live in Domain, not Domain.Shared, despite also being zero-dependency interfaces:** per architecture-guidance §2, they are specifically about the entity *taxonomy's* cross-cutting capabilities — meaningless without the concept of Entity/Party/Activity they cross-cut. Nothing outside Domain needs them without also needing Domain (Application/Web will already reference Domain to work with entities at all), so there's no Domain.Shared-style reuse argument for pushing them down.
- **Why the capability registry contract lives in Domain, and only a throwaway in-memory implementation ships this milestone:** the registry-is-authoritative discipline (architecture-guidance §3) is a Domain-level *rule* — Domain services need `ICapabilityResolver` to correctly answer "can this entity be merged" for both developer-built and tenant-defined types. But a real backing store is persistence, which is explicitly out of scope this milestone. Shipping the contract + a trivial in-memory implementation (used by unit tests) keeps the discipline enforceable now without violating the "Domain project only, no infrastructure" boundary — Infrastructure swaps in the real implementation later against the same interface, zero Domain changes required.
- **Why the module system's *loader* is out of scope but its *abstraction* isn't:** the Active requirement is explicitly "module system for organizing the framework into swappable pieces," which is satisfiable as a Domain-visible contract (`ISentinelModule`, declared dependencies) without any DI container wiring. The wiring is Web's composition-root job in a later milestone — building it now would mean writing Infrastructure/Web-shaped code inside Domain, which the milestone's own scope forbids.

## Architectural Patterns

### Pattern 1: TPT-mapped abstract taxonomy (identity inheritance)

**What:** `Entity` (abstract) → `Party`/`Item`/`Location`/`Activity`/`Document` (abstract intermediate roots, never instantiated) → concrete extensions (next milestone). Mirrors the Table-Per-Type EF Core mapping the data layer already committed to.
**When to use:** Only for the identity taxonomy itself — fields and invariants every descendant genuinely shares (tenant scoping, audit fields, status, identity). Never for behavior.
**Trade-offs:** Gives the type system and the data model the same story (no impedance mismatch between "what the domain is" and "what C# says it is"), but a 4-level TPT chain is a documented EF Core performance trap for hot read paths (multi-join, union-fanout). Mitigation is a later-milestone concern (CQRS read models preferred; TPH mapping permitted per-hierarchy where a kind adds no columns) — not something to solve in this milestone, but worth knowing the abstract roots being built now are the shape that will eventually need that mitigation.

**Example:**
```csharp
namespace SentinelSuite.Framework.Domain.Entities;

public abstract class Entity : IMultiTenant, ICreationAuditable, IModificationAuditable, ISoftDelete
{
    public Guid Id { get; protected set; }
    public TenantId TenantId { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? ModifiedAt { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

namespace SentinelSuite.Framework.Domain.Entities.Party;

public abstract class Party : Entity
{
    // shared Party-level fields only — never instantiated directly
}
```

### Pattern 2: Capability interfaces mirroring a runtime registry

**What:** Small, real-membered interfaces (`IMergeable`, `IDisplayLabeled`) implemented by exactly the concrete types that have the capability, cross-cutting the taxonomy. A parallel registry is the actual source of truth for tenant-defined subtypes, which can never implement a compile-time interface.
**When to use:** Any behavior only some types across the taxonomy have — not every capability belongs on `Entity`.
**Trade-offs:** Two representations of the same fact (interface + registration) can drift; the mitigation (a startup validation pass) is itself a piece of work this milestone should ship, not defer, because "we'll add the check later" is exactly how the two sources of truth quietly diverge in practice.

**Example:**
```csharp
namespace SentinelSuite.Framework.Domain.Abstractions.Capabilities;

public interface IMergeable
{
    bool CanMergeInto(Guid targetEntityId);
}

namespace SentinelSuite.Framework.Domain.Registries;

public interface ICapabilityResolver
{
    // Hides whether the answer came from `entity is IMergeable` (developer-built types)
    // or a registry lookup (tenant-defined subtypes) — callers never branch on which.
    bool IsMergeable(Entity entity);
}
```

### Pattern 3: Composition over template-method for cross-cutting workflows

**What:** Behavioral machinery (sync, dispatch, validation) is a service operating over capability interfaces, injected — never an abstract base workflow with per-type overrides.
**When to use:** Any process/workflow logic that would otherwise tempt a `virtual` method on `Entity` "just in case."
**Trade-offs:** More types (one service, N capability interfaces) vs. fewer types with deep override chains — architecture-guidance is explicit that the extra types are worth it; the override-chain alternative is the fragile-base-class/yo-yo problem by another name.

## Data Flow

### Dependency direction (explicit, enforced by ProjectReference)

```
SentinelSuite.Framework.Web
        │  references
        ▼
SentinelSuite.Framework.Infrastructure ──┐
        │  references                   │ references
        ▼                                ▼
SentinelSuite.Framework.UseCases ──> SentinelSuite.Framework.Domain
                                          │  references
                                          ▼
                                SentinelSuite.Framework.Domain.Shared
                                    (zero project references — leaf)
```

Read bottom-up for "what has zero knowledge of what": `Domain.Shared` knows about nothing else in the solution. `Domain` knows only `Domain.Shared`. `UseCases` knows only `Domain` (not Infrastructure, not Web). `Infrastructure` knows `Domain` + `UseCases` (it implements interfaces both declare) but never the reverse. `Web` is the only project allowed to know about everything, because it's the composition root.

**Enforcement this milestone:** the stub `.csproj` `<ProjectReference>` entries *are* the enforcement mechanism — get the arrows right now with empty projects and the compiler prevents any future accidental inward-pointing reference (e.g., `Domain` referencing `Infrastructure`) from ever landing.

### Key Data Flows (conceptual, mostly future-milestone but shaped by decisions made now)

1. **Domain event lifecycle:** an `Entity` method calls `AddDomainEvent(...)` → event sits in the aggregate's in-memory collection → (future) a dispatcher, wired at the `Web` composition root, drains and dispatches events after `SaveChanges`-equivalent, ABP-style local bus. This milestone builds only the collection API on `Entity` and the `IDomainEvent` marker; dispatch wiring is out of scope but the collection shape must not need to change when dispatch lands.
2. **Capability resolution:** calling code asks `ICapabilityResolver.IsMergeable(entity)` → resolver checks `entity is IMergeable` for developer-built types, falls through to `ICapabilityRegistry` lookup for tenant-defined subtypes → single boolean answer, caller never knows which path fired.
3. **Tenant scoping (shape only, this milestone):** `Entity.TenantId` is set at construction; the actual resolution-from-request and query-filtering machinery (a future `Infrastructure`/`Web` concern — EF Core global query filters, HTTP context extraction) has nothing to attach to yet if `TenantId` isn't already a first-class `Entity` field today. Get the field and its `IMultiTenant` contract right now even though nothing reads it yet.

## Scaling Considerations

This milestone ships no runtime, so "scaling" here means *code* scale — 26 modules and 222 features eventually depending on this kernel — not request throughput.

| Scale | Architecture Adjustments |
|-------|--------------------------|
| This milestone (kernel only, 0 modules) | Everything above. Keep Domain abstract-only; resist the temptation to add a first concrete type "to prove it works" — that's next milestone's job and its own requirements doc. |
| First few modules (Master Records: Person/Employee/Vehicle) | Each module gets its own `.Domain`/`.UseCases`/`.Infrastructure` projects referencing the Framework kernel projects — do not let module-specific entities live inside `SentinelSuite.Framework.Domain`. Watch for capability interfaces that seemed domain-taxonomy-specific in this milestone turning out to be needed by Application-layer DTOs — if that happens repeatedly, it's a signal (not yet a decision) that some capability interfaces should migrate to Domain.Shared. |
| Many modules (10+) | This is where the TPT performance trap (architecture-guidance §"EF Core practical notes") becomes real — CQRS read models for hot paths, TPH for field-less association kinds. Not this milestone's problem, but the abstract roots being designed now are exactly what will need that treatment, so don't add speculative fields to intermediate roots that aren't genuinely shared by every descendant (the earn-your-existence test applies to fields, not just types). |

### Scaling Priorities

1. **First bottleneck (near-term, code-scale not runtime-scale):** module boundary discipline — nothing stops a future contributor from adding a concrete `Employee : Person` class directly into `SentinelSuite.Framework.Domain` because it's "right there." The fix is process (code review against this doc), not code — there's no compiler enforcement for "don't add concrete types to the framework project."
2. **Second bottleneck (later, genuinely runtime):** TPT multi-join reads once real modules query across 4-level chains at volume — already documented and deferred correctly per architecture-guidance; flag it for the Master Records milestone's own research pass rather than solving it speculatively here.

## Anti-Patterns

### Anti-Pattern 1: God-class `Entity` (pushing capabilities into the base class)

**What people do:** add `IsMergeable`, `DisplayLabel`, `IsOfflineCapturable` etc. directly as members on `Entity` "because everything inherits from it anyway."
**Why it's wrong:** every capability added for one type's benefit lands on all of them; `Entity` becomes unreadable and every descendant carries fields it doesn't semantically have. Explicitly called out in architecture-guidance §2.
**Do this instead:** capability interfaces implemented only by the types that have them, mirrored by the runtime registry for tenant-defined subtypes.

### Anti-Pattern 2: Marker interfaces with no members

**What people do:** create `IParticipatesInX` with zero members just to type-tag a set of entities.
**Why it's wrong:** that fact is registry metadata wearing a costume — it can't be checked against tenant-defined subtypes at all, and it adds a type with no earned existence (fields/members/distinct behavior test, architecture-guidance §"earn-your-existence").
**Do this instead:** declare it in the registration and query the registry, or give the interface real members.

### Anti-Pattern 3: Deep virtual-method override chains up the TPT hierarchy

**What people do:** put `virtual` workflow methods on `Entity`/`Party`/`Activity` "as extension points" and override down the chain.
**Why it's wrong:** understanding one leaf type means reading every ancestor's half-implemented workflow (yo-yo problem, fragile base class problem) — explicitly flagged in architecture-guidance §1.
**Do this instead:** sealed classes by default, non-virtual members; behavioral machinery is composed services operating over capability interfaces, not base-class overrides.

### Anti-Pattern 4: Restructuring the solution layout when the second module lands

**What people do:** build only `Domain`/`Domain.Shared` this milestone (skipping the Ardalis outer layers "since there's no code for them yet"), then have to retrofit `UseCases`/`Infrastructure`/`Web` projects and rewire references once real modules need them.
**Why it's wrong:** it's exactly the rework this milestone's "layout only" requirement exists to prevent — the requirement explicitly asks for the stubs now, not "when needed."
**Do this instead:** create the empty stub projects with correct `ProjectReference` chains in this milestone, per the Recommended Project Structure above.

## Integration Points

### External Services

None this milestone (explicitly out of scope: EF Core, HTTP, any infrastructure-layer code). Documented here only so the *shape* is right when they do arrive:

| Service (future) | Integration Pattern | Notes |
|---------|---------------------|-------|
| GIS/Mapping, notification delivery, AI/LLM inference, SIEM export | Provider adaptor interface (architecture-guidance §5) — never called directly from feature code | Adaptor *interfaces* could reasonably be declared in `UseCases` (they're consumed by application-layer orchestration) with implementations in `Infrastructure`; that's a call for the milestone that actually builds a provider, not this one. |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `Domain.Shared` ↔ `Domain` | Compile-time reference, one direction (Domain → Shared) | Shared has zero outbound references — this is already true in the existing scaffold; preserve it. |
| `Domain` ↔ future `UseCases` | Compile-time reference (UseCases → Domain); UseCases orchestrates via repository interfaces + specifications declared in Domain | No code this milestone, but the capability-resolution service (`ICapabilityResolver`) is the kind of thing UseCases will call heavily later — keep its interface stable. |
| `Domain`/`UseCases` ↔ future `Infrastructure` | Infrastructure implements interfaces declared in Domain (repositories, `ICapabilityRegistry`'s real backing store) and UseCases (nothing yet) | Dependency points outward-to-inward only; Infrastructure never gets referenced back. |
| Framework kernel ↔ future product modules | Product modules take a `ProjectReference` to `SentinelSuite.Framework.Domain`/`.Shared` and build concrete types on the abstract roots | Not a project in this solution — a future solution/module boundary. Keep the kernel's public surface (abstract classes, capability interfaces, registry contracts) the seam module authors build against. |

## Build Order Within This Milestone

Grounded directly in the Active requirements list in `.planning/PROJECT.md`. Waves reflect actual code dependency, not requirement-list order — several requirements as listed have no dependency on each other and can be built in parallel; others are strictly sequenced.

**Wave 0 — zero-dependency primitives + solution layout (do first, any order within the wave):**
- Clean Architecture solution layout: create `UseCases`/`Infrastructure`/`Web` stub projects + wire `ProjectReference`s (zero content, zero dependencies — pure plumbing, no reason to defer)
- GuardClauses (build first within this wave — Result and SmartEnum's own constructors will want to validate arguments using Guard)
- Result pattern
- SmartEnum pattern
- *(all four land in `Domain.Shared`)*

**Wave 1 — cross-cutting contracts Entity needs (parallel with each other, after Wave 0):**
- Auditing & soft-delete base contracts (`ICreationAuditable`, `IModificationAuditable`, `ISoftDelete`)
- Multi-tenancy plumbing contracts (`IMultiTenant`, `TenantId`, tiered isolation enum) — contracts/shape only; resolution machinery is a future milestone
- *(both land in `Domain.Shared`; independent of each other, both required before Wave 2)*

**Wave 2 — the keystone:**
- `Entity` abstract base class (identity, tenant scoping via Wave 1's `IMultiTenant`, audit fields via Wave 1's auditing contracts, soft-delete convention, domain-event collection API using Wave 0's primitives for validation) — everything downstream depends on this landing correctly; do not parallelize anything against it.

**Wave 3 — parallel once Entity exists:**
- `EntityAssociation` abstract base class (same base contracts as Entity, applied to the named-kind/current+history shape — independent of the TPT intermediate roots below)
- Abstract TPT intermediate roots — `Party`, `Item`, `Location`, `Activity`, `Document` (each independent of the others, all depend only on `Entity`)

**Wave 4 — module system (can start as early as Wave 0 in practice, since it has no Entity dependency, but sequenced here because Wave 5 needs it):**
- Module system abstraction (`ISentinelModule`, dependency declarations, lifecycle hook *signatures* — no loader/DI wiring, that's Web's future job)

**Wave 5 — depends on Wave 3 (concrete capability targets to validate against) and Wave 4 (startup-validation hook):**
- Domain events: dispatch *convention* (the collection API itself was built in Wave 2; this wave is the ABP-style local-bus dispatch shape, wired conceptually through the Wave 4 module lifecycle even though the actual bus/loader is future work)
- Capability interface scaffold (`IMergeable`, `IDisplayLabeled`, etc.) + registry-is-authoritative discipline, including the startup validation pass (needs Wave 3's intermediate roots to have real targets to validate, and Wave 4's module lifecycle hook to run the check)

**Wave 6 — depends on Entity + TPT roots existing:**
- Specification pattern (Ardalis-style, hand-rolled) — specs are written against `Entity`/intermediate-root shapes; building this before Wave 3 lands means writing specs with nothing concrete to specify against.

**Continuous, not a wave:** unit test coverage (xUnit) — the requirement explicitly says "written as each piece lands," so tests for Wave 0 land with Wave 0, tests for Wave 2 land with Wave 2, etc. Do not batch testing into a final pass; the capability-registry-vs-interface drift check in particular (Wave 5) is exactly the kind of invariant that needs a test the moment it's written, not after.

**Explicit non-dependency worth calling out:** Multi-tenancy plumbing (Wave 1) does **not** need to wait on domain events or the module system — a common mistake is treating "multi-tenancy" as a late infrastructure concern. Here it's a Domain.Shared contract needed by `Entity` itself in Wave 2, so it must land in Wave 1, before Entity, not after.

## Sources

- [ardalis/CleanArchitecture (GitHub, official template repo)](https://github.com/ardalis/CleanArchitecture) — Core/UseCases/Infrastructure/Web layer definitions and dependency direction. HIGH confidence (official source).
- [Clean Architecture with ASP.NET Core — Ardalis (Steve Smith)](https://ardalis.com/clean-architecture-asp-net-core/) — author's own explanation of the pattern. HIGH confidence.
- [DeepWiki: Full Clean Architecture Template (ardalis/CleanArchitecture)](https://deepwiki.com/ardalis/CleanArchitecture/2-full-clean-architecture-template) — supplementary structural detail. MEDIUM confidence (third-party summary of the official repo, not primary).
- [ABP.IO Docs: Layered Solution — The Structure](https://abp.io/docs/latest/solution-templates/layered-web-application/solution-structure) — Domain vs Domain.Shared vs Application.Contracts vs Application vs EntityFrameworkCore vs HttpApi project responsibilities. HIGH confidence (official docs).
- [ABP.IO Docs: Solution structure (commercial template)](https://abp.io/docs/commercial/3.1/startup-templates/application/solution-structure) — corroborating detail on the Domain.Shared "zero dependency, depended on by everyone" rule. HIGH confidence (official docs).
- [ABP.IO Docs: Implementing Domain Driven Design](https://docs.abp.io/en/abp/4.2/Domain-Driven-Design-Implementation-Guide) — entities/aggregates/domain services placement rationale. HIGH confidence (official docs).
- Project-internal: `docs/architecture-guidance.md` (authoritative inheritance/interface/composition rules for this codebase — every "Pattern"/"Anti-Pattern" section above is a direct application of it, not independent research).
- Project-internal: `SentinelSuite/SentinelSuite.slnx`, `SentinelSuite.Framework.Domain/SentinelSuite.Framework.Domain.csproj`, `SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` — confirmed current empty-scaffold state (Domain → Domain.Shared reference exists; Domain.Shared has zero references) as the fixed starting point for this doc's recommendations.
- The specific mapping of Sentinel Suite requirements onto folders/namespaces/build waves is this researcher's synthesis of the above sources against `docs/architecture-guidance.md` and the Active requirements list in `.planning/PROJECT.md` — treat as MEDIUM confidence, worth a quick gut-check against actual Wave 2/3 code once `Entity` is written, since real code sometimes reveals a cleaner split than a research pass can predict.

---
*Architecture research for: hand-rolled .NET Clean Architecture/DDD framework kernel (Sentinel Suite Milestone 1)*
*Researched: 2026-07-15*
