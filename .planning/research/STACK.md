# Stack Research

**Domain:** Hand-rolled .NET Clean Architecture/DDD kernel (Domain-project abstractions only — no persistence, no app/API layer)
**Researched:** 2026-07-15
**Confidence:** MEDIUM (web search cross-checked against GitHub source files, Microsoft Learn, NuGet, and official ABP/Ardalis docs; no direct Context7/official-SDK doc tool was available this session — see Gaps)

## Executive framing

This milestone is not "pick a stack" in the usual sense — it's "reproduce specific, well-understood mechanics from two reference codebases (ABP, Ardalis) without taking the dependency." The table below is therefore organized around **mechanics to reproduce concretely**, not packages to install. Where a real third-party package earns its keep anyway (test-only tooling, which never ships in the deployed artifact and therefore doesn't touch the FedRAMP authorization boundary the same way), it's called out explicitly.

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

**Why test-project dependencies are treated differently from production ones:** test assemblies are never part of the deployed/authorized artifact — they don't ship to a FedRAMP boundary, don't run in production, and aren't part of the ATO's software inventory. The dependency-minimalism constraint in `PROJECT.md` is about the *runtime* dependency graph. That said, default to xUnit's built-in `Assert` first; only add Shouldly if the team decides fluent-assertion readability is worth even a test-only package.

### Explicitly excluded (confirm NOT to reference)

| Package | Why excluded |
|---------|--------------|
| `Volo.Abp.*` (all ABP NuGet packages: `Volo.Abp.Domain`, `Volo.Abp.Auditing`, `Volo.Abp.MultiTenancy`, `Volo.Abp.EventBus`, `Volo.Abp.Ddd.Domain`, etc.) | `PROJECT.md` constraint: borrow the *patterns*, never the packages. ABP pulls in a large module/DI graph (its own conventional-registration reflection scanner, its own settings/permission/feature-management subsystems) that is far more than this kernel needs and would itself need FedRAMP review as a dependency. |
| `Ardalis.Specification`, `Ardalis.Result`, `Ardalis.GuardClauses`, `Ardalis.SmartEnum` (and their `.EntityFrameworkCore`/`.EFCore`/`.AspNetCore` extension packages) | Explicit `PROJECT.md` constraint. Each is genuinely small (see mechanics below) — reproducing them in-house is a few hundred lines total, well below the threshold where taking the dependency is the better trade. |
| `MediatR` | Named explicitly in `PROJECT.md`'s Out of Scope. The domain-event dispatch this milestone needs (collect-on-aggregate, publish-before-UoW-commit) is a much narrower mechanism than MediatR's general in-process mediator/pipeline-behavior system; hand-rolling the narrower thing is strictly less code than adopting and constraining the general one. |
| `FluentAssertions` (v8+) | As of Jan 2025 it moved to the Xceed Community License — **commercial use requires a paid per-developer license** ($130/dev/yr, or $50/dev/yr for orgs under $1M revenue / ≤3 devs). Even as a test-only dependency this is a new procurement/licensing surface a solo-founder FedRAMP-track project should not take on. v7.x remains Apache-2.0 but is feature-frozen — don't pin to a dead branch either. Use Shouldly or plain `Assert` instead. Confidence: MEDIUM-HIGH (cross-checked InfoQ, dev.to, multiple independent posts, consistent story). |

## Hand-Roll vs. Depend — mechanics to reproduce, concretely

For each pattern named in `PROJECT.md`, here is what the reference implementation actually does mechanically, and the resulting verdict.

### 1. `Entity` / `AggregateRoot` base classes — HAND-ROLL (trivial, ~50-100 lines)

ABP's shape: `Entity<TKey>` holds `Id` and overrides equality via an `EqualityComponents` sequence (so two entities of the same type with the same `Id` compare equal). `BasicAggregateRoot<TKey> : Entity<TKey>` adds nothing structural — it's the marker for "this is a root, not a child entity." `AggregateRoot<TKey> : BasicAggregateRoot<TKey>` additionally implements `IHasExtraProperties` (an `ExtraPropertyDictionary`, ABP's EAV escape hatch) and `IHasConcurrencyStamp` (a `Guid ConcurrencyStamp` regenerated on save, used for optimistic concurrency).

**What to reproduce:** an abstract `Entity` base (identity + equality-by-id, per `architecture-guidance.md`'s "inherit what a thing IS") and a marker distinction for aggregate roots if the kernel needs one for domain-event dispatch scoping (see #5). The platform's own `extended_fields` mechanism (per `_DECISIONS.md`) already plays the role ABP's `ExtraPropertyDictionary` plays — don't reproduce a second EAV mechanism, wire the existing one in as the Domain-project field/property instead. Concurrency stamping (a `Guid`/`byte[]` rowversion) is worth including in the base shape now even though persistence is a later milestone, since it's a field-shape decision that TPT mapping will care about later — cheap to decide once.

Confidence: MEDIUM (GitHub source file confirmed class hierarchy directly via WebFetch; auditing subclass list confirmed via GitHub directory listing).

### 2. Auditing & soft-delete base contracts — HAND-ROLL (trivial, ~100-150 lines)

ABP does **not** put all audit fields on one class. It layers them as a deliberate opt-in chain: `CreationAuditedEntity<TKey>` (`CreatorId`, `CreationTime`) → `AuditedEntity<TKey>` (adds `LastModifierId`, `LastModificationTime`) → `FullAuditedEntity<TKey>` (adds `ISoftDelete.IsDeleted`, `DeleterId`, `DeletionTime`). Each level corresponds to a marker interface (`ICreationAuditedObject`, `IAuditedObject`, `IFullAuditedObject`, `ISoftDelete`) — the *interfaces* are the actual contract; the base classes are convenience implementations of them. Soft-delete itself is not "logic on the entity" — `IsDeleted` is just a bool property; the actual behavior (auto-set on delete, auto-filter on query) lives entirely in the ORM integration (an EF Core global query filter conditioned on `!IsDeleted`), which is out of scope for this Domain-only milestone but should be designed for now.

**What to reproduce:** the interface layer (`ICreationAudited`, `IAudited`, `ISoftDelete`, matching `architecture-guidance.md`'s "interface what a thing CAN" rule — a `Party` and an `EntityAssociation` both need auditing but auditing isn't taxonomic) plus base-class conveniences for the common combination every `Entity`/`EntityAssociation` root actually needs (tenant scoping + audit fields + soft-delete are called out in `PROJECT.md`'s Active requirements as living directly on `Entity`, not as opt-in — so unlike ABP's fully separable chain, this kernel can put the common case straight on `Entity` and reserve the interface-layer opt-in pattern for cases where a concrete type genuinely doesn't want one of the four). Decide explicitly whether every level of the TPT chain re-declares audit fields or only the root does (recommendation: only the root — shared PK means shared row for audit purposes, consistent with `architecture-guidance.md`'s existing shared-PK TPT design).

Confidence: MEDIUM.

### 3. Multi-tenancy resolution & data filtering — HAND-ROLL (moderate, ~150-250 lines), matches platform's own tiered model better than ABP's out of the box

ABP's shape: `ICurrentTenant` is an ambient-context service (backed by `AsyncLocal<T>`, so it flows correctly across `async`/`await` without manual threading) exposing `Id`/`Name`/`IsAvailable`. Resolution runs a **chain of `ITenantResolveContributor` implementations** in a fixed priority order — claims-from-authenticated-user first (for security, so a malicious query string can't override an authenticated tenant claim), then query string, then route segment, then header, then cookie — the first contributor that finds a value wins. Data filtering is an EF Core global query filter (`TenantId == currentTenant.Id`) applied automatically to every entity implementing `IMultiTenant`, suspendable via a `using (currentTenant.Change(null)) { ... }` block for legitimate cross-tenant host-level queries.

**What to reproduce:** the `ICurrentTenant`-equivalent ambient-context interface + `AsyncLocal`-backed implementation (this part is genuinely almost identical no matter whose code you read — it's the standard .NET ambient-context pattern), and an `IMultiTenant`/`ITenantScoped` marker interface for `Entity` to carry `TenantId`. **Do not reproduce ABP's exact resolver chain** — `PROJECT.md`/`_DECISIONS.md` describe a different, more specific model (shared / dedicated_db / on_prem tiered isolation, contractor↔client dual-tenancy via Client Engagement) that doesn't map cleanly onto ABP's claim/query-string/route/header/cookie chain built for a single shared-DB SaaS shape. The query-filter mechanics (EF Core global filter conditioned on ambient tenant) are the right shape to keep; the *resolution* logic is genuinely platform-specific and should be designed against `_DECISIONS.md`'s tenancy model directly, not copied from ABP.

Confidence: MEDIUM.

### 4. Local domain-event dispatch — HAND-ROLL (moderate, ~150-200 lines)

ABP's shape is the most load-bearing mechanic to get right, and it's more specific than "raise an event": `ILocalEventBus.PublishAsync` does **not** dispatch immediately — it queues the event, and registered `ILocalEventHandler<TEvent>` handlers run just before the current Unit of Work completes (i.e., right before/around `SaveChanges`), so a handler exception rolls back the whole UoW rather than leaving a half-applied side effect. Any entity implementing `IGeneratesDomainEvents` (which `AggregateRoot` does by default) carries a private collection with `AddLocalEvent(...)` and `GetLocalEvents()`/`ClearLocalEvents()`; a SaveChanges-time interceptor walks every tracked aggregate root, drains its events, and publishes them through an in-process bus (essentially a `Dictionary<Type, List<handlerFactory>>` resolved per-publish via DI). Separately, ABP auto-generates generic `EntityCreatedEventData<T>`/`EntityUpdatedEventData<T>`/`EntityDeletedEventData<T>`/`EntityChangedEventData<T>` events for *every* entity on every save, with zero opt-in code required.

**What to reproduce, concretely:** (a) an `AddDomainEvent(IDomainEvent)`/`ClearDomainEvents()`/read-only `DomainEvents` collection on the aggregate-root base (the "collection-on-aggregate" half `PROJECT.md` names explicitly); (b) an `IDomainEventHandler<TEvent>` interface and a small in-process dispatcher; (c) the *dispatch timing decision* — even though this milestone has no persistence layer to hook a SaveChanges interceptor into yet, design the collection API now so a later Infrastructure-layer UoW/interceptor can drain-and-publish before commit without changing the Domain-layer contract. Skip ABP's auto-generated generic entity-changed events for this milestone — they're an Infrastructure/EF-integration concern, not a Domain-layer one, and nothing in `PROJECT.md`'s Active list asks for them yet.

Confidence: MEDIUM.

### 5. Module system — HAND-ROLL, but treat as genuinely lower priority (moderate, ~100-150 lines for the minimal version)

ABP's shape: every module is a class inheriting `AbpModule`, decorated with `[DependsOn(typeof(OtherModule))]` attributes. ABP topologically sorts the resulting dependency graph at startup and calls `PreConfigureServices → ConfigureServices → PostConfigureServices` on every module in dependency order, then `OnApplicationInitialization`. Separately, a `ConventionalRegistrar` reflects over loaded assemblies and auto-registers any class implementing `ITransientDependency`/`IScopedDependency`/`ISingletonDependency` into DI, removing the need to hand-wire every service.

**What to reproduce:** for *this* milestone (Domain-project layout only, per `PROJECT.md`'s Active requirements: "Module system for organizing the framework into swappable pieces"), the minimal useful version is (a) a `IFrameworkModule` marker/base with a `ConfigureServices(IServiceCollection)` hook and (b) a `[DependsOn]`-style attribute plus a small topological sort — genuinely small, ABP's DAG-sort is a textbook Kahn's-algorithm implementation, not a hard problem. **Do not build the conventional-registrar reflection scanner this milestone** — it's a nice-to-have DX convenience, not a domain-kernel invariant, and reflection-based auto-registration is exactly the kind of thing worth deferring until there's a second/third module proving the pattern is needed (consistent with the platform's own stated "generalize on second consumer" discipline recorded repeatedly in `_DECISIONS.md`).

Confidence: MEDIUM.

### 6. Specification pattern — HAND-ROLL (small, ~150-200 lines for a useful subset)

`Ardalis.Specification`'s `Specification<T>` stores query intent as composable `Expression<Func<T,bool>>` lists (a small custom `OneOrMany<T>` struct avoids `List<T>` allocation in the common single-expression case — a micro-optimization, not essential to reproduce), plus paging (`Skip`/`Take`), tracking flags (`AsNoTracking`, `IgnoreQueryFilters`, `AsSplitQuery`), and ordering/include expressions. A fluent `Query` builder (`ISpecificationBuilder<T>`) exposes `.Where(...)`, `.OrderBy(...)`, `.Include(...)`. `SpecificationEvaluator.Default.GetQuery(IQueryable<T>, spec)` applies the stored expressions onto an `IQueryable<T>` — this is the one piece that's genuinely EF-Core-specific (translating `.Include` chains) and therefore belongs in a later Infrastructure milestone, not this one. `Evaluate()`/`IsSatisfiedBy()` support pure in-memory (non-EF) specification checking against `IEnumerable<T>` — this half is exactly what a Domain-only milestone can build now with zero persistence dependency.

**What to reproduce this milestone:** the `ISpecification<T>` contract (`Expression<Func<T,bool>> Criteria`, `IsSatisfiedBy(T entity)`) and a base `Specification<T>` that composes `Where` expressions with `&&`/`||` via `Expression.AndAlso`/`Expression.Invoke` rebinding (the one mildly fiddly bit — expression-tree parameter substitution — but well-documented and under 30 lines). Defer the EF-`IQueryable` evaluator to the persistence milestone; a Domain-project Specification only needs to answer "does this in-memory candidate satisfy this rule," which is exactly what invariant/policy checks in the Domain layer need.

Confidence: MEDIUM.

### 7. Result pattern — HAND-ROLL (trivial, ~80-120 lines)

`Ardalis.Result`'s `Result`/`Result<T>` carry a `ResultStatus` enum (`Ok, Created, Error, Forbidden, Unauthorized, Invalid, NotFound, NoContent, Conflict, CriticalError, Unavailable`), an optional `Value`, an `IEnumerable<string> Errors`, and `IEnumerable<ValidationError> ValidationErrors` for the `Invalid` case, with static factory methods per status and implicit `T → Result<T>` conversion for ergonomic returns. The HTTP-status-code mapping lives in a *separate* `Ardalis.Result.AspNetCore` package — irrelevant to this milestone (no API layer yet) and arguably irrelevant even later if the platform's own error/response conventions (referenced elsewhere in `_DECISIONS.md`) differ from Ardalis's status set.

**What to reproduce:** a generic `Result<T>` (success/value or failure/errors) is genuinely trivial — this is one of the smallest patterns on this list. Recommend trimming `ResultStatus` to only the statuses the Domain layer's own invariant/validation failures actually need (`Ok`, `Invalid`, `NotFound`, `Conflict` are likely the real set for domain-level operations; `Forbidden`/`Unauthorized`/`CriticalError` are Application/API-layer concerns that don't belong on a Domain-layer Result type) rather than porting Ardalis's full HTTP-flavored enum wholesale — the full enum is itself evidence the type was designed with an HTTP boundary in mind, which this milestone explicitly doesn't have.

Confidence: MEDIUM.

### 8. GuardClauses — HAND-ROLL (trivial, ~100-150 lines for the useful subset)

This is the single smallest pattern on the list, and its "hard part" is a naming/extensibility convention, not logic. The entire mechanism: `IGuardClause` is a **memberless marker interface**; `Guard` is a sealed class with a private constructor and a `public static IGuardClause Against { get; } = new Guard();` singleton. Every actual clause (`Null`, `NullOrEmpty`, `NegativeOrZero`, `OutOfRange`, `InvalidFormat`, ...) is a C# **extension method** on `IGuardClause`, so `Guard.Against.Null(x, nameof(x))` resolves via ordinary extension-method lookup against the singleton. There is no reflection, no runtime magic — the marker interface exists purely so third parties (or your own code, in a different namespace) can add new guard clauses as extension methods without modifying the core class. Note this pattern is `architecture-guidance.md`'s own named example of a "marker interface with no members is a smell — unless it earns its keep as an extensibility seam," which `IGuardClause` genuinely is (a documented, intentional exception, not a violation of that doc's own rule).

**What to reproduce:** the `IGuardClause` marker + `Guard.Against` singleton, plus whatever handful of clauses the kernel's invariants actually need (`Null`, `NullOrWhiteSpace`, `NegativeOrZero`, `OutOfRange`, `Default` are almost certainly the real working set for entity/value-object invariants — don't port all ~40 of Ardalis's clauses speculatively).

Confidence: MEDIUM.

### 9. SmartEnum — HAND-ROLL (small, ~100-150 lines)

`Ardalis.SmartEnum`'s `SmartEnum<T>` is an abstract base implementing `IComparable`/`IEquatable<T>`, exposing `int Value` and `string Name`; concrete "members" are `public static readonly` fields on the derived class (`public static readonly OrderStatus Draft = new(1, "Draft");`). A static, lazily-built list (populated via one-time reflection over the declaring type's — and its base types' — public static readonly fields, cached after first access) backs `FromValue(int)`, `FromName(string)`, `TryFromValue`, and `.List`. EF Core persistence support ships as a *separate* `Ardalis.SmartEnum.EFCore` package providing a `ValueConverter<TEnum, int>` — irrelevant to this Domain-only milestone.

**What to reproduce:** the base class + one-time reflection-backed member list is genuinely the whole pattern. This is a good fit for the platform's registry-authoritative discipline (`architecture-guidance.md` §3) for closed, code-owned enumerations specifically — e.g. anything that is NOT a Tenant-Defined Subtype (those, per explicit platform decision, have no code class at all and must go through the runtime registry, never SmartEnum). Be precise in the roadmap about where SmartEnum is and isn't appropriate: it's for developer-owned, compile-time-closed sets (e.g., a fixed `ResultStatus`-style status set); it is **not** a substitute for the runtime-registry mechanism that governs tenant-extensible taxonomies.

Confidence: MEDIUM.

## Genuinely hard-to-reproduce parts — none found that justify the dependency

Across all eight patterns and the module system, nothing surfaced in this research is nontrivial enough to flip the recommendation toward taking the dependency. The largest of the nine (multi-tenancy resolution, domain-event dispatch, module system) are each 150-250 lines of well-understood, widely-documented C# — ambient context via `AsyncLocal`, an in-process pub/sub keyed by `Type`, and Kahn's-algorithm topological sort, respectively. None require anything ABP or Ardalis do that isn't public, standard .NET idiom. This confirms `PROJECT.md`'s own premise (these patterns are "minimal effort to reproduce in-house") rather than surfacing a counterexample.

The one place genuine complexity exists is **not a pattern named in the question** — it's EF Core's TPT query-translation machinery itself (multi-table JOIN/UNION generation for polymorphic queries). That is the ORM's own internals, not something anyone reproduces by hand; it's consumed by using EF Core (already the platform's chosen ORM), not reimplemented. See the TPT section below for why this matters for a *later* milestone even though this milestone has no persistence code.

## EF Core TPT mapping — informational for this milestone, load-bearing for the next

Not implemented this milestone (Domain project only, no `DbContext`), but the Entity→Party→Person→Employee 4-level chain this kernel's abstractions must eventually support has concrete, well-documented EF Core mechanics worth designing the Domain-layer shape against now:

- **Shared PK, not a second FK/pointer pair.** TPT maps each level to its own table; the derived table's PK is also its FK back to the base table's PK (one shared identity value threaded through every table in the chain) — this is exactly the shape `_DECISIONS.md` already committed to for both `Entity` and `EntityAssociation` hierarchies ("the row's primary key **is** `entity_id`... eliminating the earlier redundant `extension_id` + `entity_id` pointer pair"). Confirms the Domain-layer `Id` on `Entity` should be the one identity value every derived C# class inherits unchanged — no per-level ID property.
- **Configuration is `.UseTptMappingStrategy()`** (EF Core 5+; implicit as the default strategy in most configurations when no discriminator/table-sharing is set up) — an Infrastructure-layer concern for the next milestone, not this one, but confirms nothing about the Domain-layer class shape needs to anticipate EF-specific attributes/fluent config; POCO classes are sufficient.
- **Performance is the real trap, and it's a documented one, not a hypothetical.** Querying a derived type requires INNER JOINing every table from the queried level up to the root; querying the base type *polymorphically* (i.e., "give me all `Party` rows regardless of concrete subtype") requires a UNION-based fan-out across every leaf table. Microsoft's own performance-modeling documentation states TPT measurably underperforms TPH in most benchmarks specifically because of this join/union cost. `architecture-guidance.md` already has this exactly right (§EF Core practical notes: prefer CQRS read models for hot paths, TPH for field-less hierarchy levels, denormalization last) — this research corroborates that guidance rather than changing it. Nothing to adjust here; flag for the persistence milestone to actually build the CQRS read-model mitigation rather than defer it, since the perf trap is real and well-attested, not a theoretical concern.

Confidence: HIGH for the mechanics (Microsoft Learn is the primary/official source and was directly consulted), MEDIUM for the "how bad is bad" magnitude claims (community benchmarks, not a controlled first-party benchmark from this session).

## Testing Stack

| Component | Recommendation | Why |
|-----------|----------------|-----|
| Test framework | `xunit.v3` (3.2.x) | Current, actively maintained, native .NET 10 support. |
| Test runner/host | `Microsoft.Testing.Platform` (MTP) | .NET 10 SDK ships MTP natively; xUnit v3 is built on it directly (no VSTest adapter layer). Set the project up on MTP from day one rather than the legacy `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` combo, since that combo is the one being phased out, not the one to adopt fresh in 2026. |
| Assertions | xUnit's own `Assert.*` (default); `Shouldly` (MIT) only if fluent syntax proves worth a test-only dependency | **Do not use FluentAssertions 8+** — paid license (Xceed Community License, $130/dev/yr) as of Jan 2025. FluentAssertions 7.x is free but feature-frozen; don't pin new work to a dead branch. |
| Coverage | `coverlet.collector` | Standard, cross-platform, works with MTP. |
| Test doubles / mocking | Not needed yet | This milestone has no services to mock (no persistence, no external adaptors) — kernel invariant tests operate directly on Domain types. Defer the mocking-library decision (`NSubstitute` is the common low-ceremony choice in this ecosystem) to whichever milestone first needs to fake a collaborator; don't pre-adopt one speculatively. |

**What "unit test coverage for kernel invariants" concretely means this milestone**, per `PROJECT.md`'s Active requirement: xUnit tests directly exercising the hand-rolled mechanics above as they land — `Entity` identity/equality, soft-delete flag behavior, tenant-scoping field presence, domain-event collection add/clear semantics, Specification `IsSatisfiedBy` composition (`&&`/`||`), `Result` success/failure factory correctness, `GuardClauses` throw behavior, `SmartEnum` `FromValue`/`FromName` round-tripping, and the startup validation pass `architecture-guidance.md` recommends (asserting a developer-built type's implemented capability interfaces match its registry declarations) once that registry-mirroring mechanism exists. No infrastructure, no database, no HTTP — every one of these is a pure in-memory unit test against POCOs, which is exactly what a Domain-only milestone should be able to fully cover.

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
