# .NET Solution Structure & Build Order

**Status:** Living plan (not a feature/requirements doc)
**Audience:** Anyone scaffolding or sequencing the actual .NET solution, including AI-assisted implementation sessions
**Relationship to other docs:** `docs/requirements/_DECISIONS.md` and `docs/requirements/_INDEX.md` record *what the platform is* and in what order it was specified; `docs/architecture-guidance.md` records *how code should be shaped* (inheritance/interfaces/composition); **this doc records which .NET projects exist, how they're grouped into modules, and in what order they get built.** It defers to both — it never overrides a decision made there, and it only proposes structure for features that are already spec'd. Where a module below isn't spec'd yet (Modules 6, 8, 10–25 mostly), its entry here is a placeholder name, not a commitment.

**Correction on 2026-07-15:** an earlier draft of this doc wired everything up as literal `Volo.Abp.*` NuGet package references. That was wrong. Per `_DECISIONS.md`'s corrected wording: the platform is **based on ABP's architecture as a design reference — clean architecture/DDD layering, module conventions, base-entity/auditing/multi-tenancy shape — with zero runtime dependency on the actual ABP Framework package set.** Everything ABP would have shipped as an official module (Identity, Tenant Management, Feature Management, Setting Management, Background Jobs, Blob Storing, Audit Logging, Permission Management) is built from scratch here, patterned after ABP's design. Ordinary focused .NET/OSS libraries underneath our own abstractions (ASP.NET Core Identity, OpenIddict, Hangfire/Quartz, an object-storage SDK, EF Core itself) are fine and expected — the line is specifically "no dependency on the ABP application framework," not "reinvent cryptography."

This doc is written incrementally, same discipline as `_INDEX.md`/`_DECISIONS.md`: update it whenever a build-order or project-boundary decision is made, don't let it drift from what's actually been scaffolded.

---

## Guiding principles

1. **One solution, one custom modular monolith — not microservices.** Solo developer, no hard deadlines, "modular architecture enabling future team growth" is the pdd.md risk mitigation, not "ship 30 independently-deployable services." One `SentinelSuite.sln`, one deployable API host for the life of the MVP and probably well beyond. Splitting a module into its own deployable later is a *deployment* decision (containerize it separately), not a reason to over-fragment the source tree now.
2. **Module granularity = one module per real bounded context, not one per MODULES.md sub-feature.** DAR, Shift Passdowns, Guard Tour, Patrol Management, Courtesy Patrol, Tickets/Citations, Incident Reporting, and AI-Assisted Incident Writing are eight *features* but one *module* (`SecurityOperations`) — they share aggregates (Activity Registry extensions), share a database schema, and were never designed to be independently deployable. This keeps project count proportionate to ~25 bounded contexts rather than ~222 features.
3. **Pattern-match ABP's architecture; build every module ourselves.** No `Volo.Abp.*` package reference anywhere in the solution. Where `_DECISIONS.md` describes a concern as "loosely based on ABP" (Tenant Management, Feature Management, Background Jobs, Blob Storing, Setting Management, Identity/Account, Audit Logging, Permission Management), the plan is: build our own implementation with the same conceptual shape ABP's equivalent module has, using ordinary underlying .NET/OSS libraries where that's the sane engineering choice (ASP.NET Core Identity for password hashing, OpenIddict for OIDC, Hangfire/Quartz for job scheduling, a plain object-storage SDK), wrapped behind our own interfaces so the platform never has a hard dependency on any single one of those either (same adaptor discipline the GIS/AI provider decisions already established).
4. **Build order follows the same rule elicitation did, plus MVP priority.** Foundation before Master Records before feature modules, because that's the real dependency direction (every feature module's Activity/Party/Item/Location extensions sit on top of Master Records' TPT spine, which itself sits on top of several Platform Core mechanisms — Settings, Command/Action Bus, Tenant-Defined Types). Within that constraint, **MVP-in-scope modules (`docs/mvp.md`) are built before spec'd-but-deferred modules (3, 4, 5), which are built before not-yet-elicited modules (6, 8, 10–25).** Spec complete ≠ build-now, same way spec complete ≠ MVP-committed in `mvp.md`.
5. **Don't scaffold a module's projects before its requirements doc exists.** A project skeleton for Module 13 (Investigation Management) with no domain content is dead weight and will be wrong once elicitation actually happens. Projects get created when a module's turn in the build order arrives, not upfront for all 25.

---

## Standard per-module project template

Every module (everything below Tier 0) follows this project set:

| Project | Contains |
| --- | --- |
| `SentinelSuite.<Module>.Domain.Shared` | Enums, constants, error codes, localization resources shared by every layer |
| `SentinelSuite.<Module>.Domain` | Aggregate roots/entities (TPT extensions where applicable), domain services, repository interfaces, domain events raised |
| `SentinelSuite.<Module>.Application.Contracts` | DTOs, application service interfaces, permission definitions, `AiContext`/Command-Bus-action declarations where the module registers any |
| `SentinelSuite.<Module>.Application` | Application service implementations, mapping profiles, authorization checks (RBAC baseline; ABAC overlay hooks call into the shared Auth module) |
| `SentinelSuite.<Module>.EntityFrameworkCore` | EF Core entity configurations (`IEntityTypeConfiguration<T>`), repository implementations *(migrations live once, at the composed DbContext level — see note below, not per module)* |
| `SentinelSuite.<Module>.HttpApi` | REST controllers |
| `SentinelSuite.<Module>.HttpApi.Client` | *(Only when another module needs to call this one over HTTP rather than in-process — rare in a monolith; skip by default, add when a real cross-boundary HTTP need appears, e.g. a future split-out service)* |
| `SentinelSuite.<Module>.Tests` | One test project per module, added when the module is built, not upfront |

**One shared DbContext, not one per module.** Despite the module-per-bounded-context split above, this is a single physical database per tenant-isolation-tier (full DB isolation vs. logical, per `_DECISIONS.md`'s tenant isolation decision) — so `EntityFrameworkCore` projects register their entity configurations into one composed `SentinelSuiteDbContext`, rather than each module owning an independent DbContext/migration history. This avoids the multi-DbContext cross-module-FK pain that would otherwise fight the TPT Entity/EntityAssociation architecture directly, and it's the one place a monolith should behave like a monolith even though the surrounding code is modularized.

**GraphQL is host-level, not per-module.** Per `_DECISIONS.md` ("GraphQL for the internal web/mobile frontend; versioned REST for external integrations... both sit on the same underlying business logic/permission layer"), GraphQL types/resolvers (HotChocolate, a plain library dependency — not part of "the ABP framework") live in the Host project and delegate to the same Application-layer services the REST controllers call — no parallel `.GraphQL` project per module.

---

## Module map

### Tier 0 — the Framework kernel (multiple projects, not one)

Zero product features anywhere in this tier — pure scaffolding, patterned after (never dependent on) the ABP repo's own kernel packages. **This is not a single `SentinelSuite.Framework` project** — that was wrong in an earlier draft of this doc. A shared kernel has to obey the same Dependency Rule as everything built on top of it: source dependencies only point inward, so an outer/infrastructure concern (EF Core, ASP.NET Core) must be *physically incapable* of being referenced from Domain, not just conventionally discouraged. ABP enforces exactly this by shipping its own kernel as separate packages rather than one `Volo.Abp.Core` blob — confirmed against the real repo (`framework/src`): `Volo.Abp.Ddd.Domain.Shared`, `Volo.Abp.Ddd.Domain`, `Volo.Abp.Ddd.Application.Contracts`, `Volo.Abp.Ddd.Application`, `Volo.Abp.Auditing.Contracts` vs. `Volo.Abp.Auditing`, `Volo.Abp.EntityFrameworkCore`, `Volo.Abp.AspNetCore.Mvc` are all distinct packages precisely so a domain project can't accidentally end up depending on EF Core. Our kernel mirrors that split:

| Project | Depends on | Contains |
| --- | --- | --- |
| `SentinelSuite.Framework.Domain.Shared` | *(nothing but BCL)* | Pure interfaces/marker types with zero logic: `ISoftDelete`, `IMultiTenant`, `IHasCreationTime`/`IAudited`/`IFullAudited` (auditing *contracts*, not the hash-chain implementation), domain-level exception base types. The innermost possible ring — every other Framework project, and every product module's `Domain` project, can reference this freely with nothing dragged in behind it. |
| `SentinelSuite.Framework.Domain` | `.Domain.Shared` | `Entity<TKey>`, `AggregateRoot<TKey>`, `IRepository<T>`/`IRepository<T,TKey>` (interfaces only), `IUnitOfWork` (interface only), domain service base class, business-rule/validation base types. **No EF Core reference at all.** This is the project Master Records' `Entity`/`EntityAssociation` TPT roots (`architecture-guidance.md`) extend, and the one every product module's own `.Domain` project depends on. |
| `SentinelSuite.Framework.Application.Contracts` | `.Domain.Shared` | `IApplicationService` marker interface, base DTOs (`EntityDto<TKey>`, `PagedResultDto<T>`, `ListResultDto<T>`), standard paging/sorting input DTOs. Deliberately does **not** depend on `.Domain` — an application-contracts layer should only need shared primitives, never concrete domain types, so a client-facing contract package never leaks a domain entity shape. |
| `SentinelSuite.Framework.Application` | `.Domain`, `.Application.Contracts` | `ApplicationService` base class, the object-mapping convention (a thin wrapper we own over whatever mapping library sits underneath — Mapperly/AutoMapper — same adaptor discipline as everywhere else), a validation pipeline base. |
| `SentinelSuite.Framework.EntityFrameworkCore` | `.Domain` | The one project in this tier allowed to reference the EF Core package. Generic repository implementation, the multi-tenant global query filter wiring, the EF-Core-backed `IUnitOfWork` implementation, and the base conventions each product module's own `.EntityFrameworkCore` project composes into the one shared `SentinelSuiteDbContext`. Nothing in `.Domain` or `.Application` ever references this project — only the Host's composition root and each module's own EFCore project do. |
| `SentinelSuite.Framework.AspNetCore` | `.Application.Contracts` | Host-layer conventions: standard API response envelope, exception-handling middleware, correlation-id middleware, auto-controller/routing conventions. Referenced by `HttpApi.Host` and each module's `.HttpApi` project — never by `Domain`/`Application`. |
| `SentinelSuite.Framework.Modularity` | *(nothing but BCL/DI abstractions)* | The module-registration/bootstrap convention — our own thin stand-in for ABP's `IAbpModule`: a base class/interface each product module's own `Module` class implements to register its services and contribute its EF Core entity configurations to the composed host. Genuinely orthogonal to Domain/Application/Infrastructure (it's about *wiring*, not modeling), so it stays its own project rather than living inside `.Domain`. |

The dependency graph this produces, arrows meaning "depends on":

```
Domain.Shared  <───────────────┐
     ▲                          │
     │                          │
   Domain  <── EntityFrameworkCore
     ▲
     │
Application.Contracts  <── AspNetCore
     ▲
     │
 Application

Modularity  (standalone — no inward deps, referenced by every module + Host)
```

Domain-event raising/dispatch's *base contract* lives in `.Domain` (an aggregate can raise an event); the full Event/Command/Query Bus that actually routes and delivers those events is Tier 2's `CommandSystem` module, built on top of this, not part of the kernel itself.

### Tier 1 — Core platform systems (each a from-scratch build of an ABP-shaped concern)

Full project template each. These are genuinely substantial builds now that there's no framework package to lean on — sequence them first because Tier 2/3/4 all assume they exist:

| Order | Module | Scope | Underlying library it may wrap (not a framework dependency) |
| --- | --- | --- | --- |
| 1 | `SentinelSuite.IdentityAccount` | Users, roles, credentials, MFA/AAL method catalog, external IdP federation (SAML/OIDC), local login, self-service password reset channel catalog | ASP.NET Core Identity for credential storage/hashing; OpenIddict for OIDC/SAML federation |
| 2 | `SentinelSuite.PermissionManagement` | Permission definition tree, role/user grants — the RBAC baseline the ABAC overlay narrows | — |
| 3 | `SentinelSuite.TenantManagement` | Tenant provisioning, isolation tier, Edition/entitlements, lifecycle, **Client Engagements** (contractor↔client dual-tenancy) | — |
| 4 | `SentinelSuite.FeatureManagement` | Boolean + quota-kind feature flags resolved per tenant/edition | — |
| 5 | `SentinelSuite.BackgroundJobs` | Durable job/scheduled-task registry, platform-enforced idempotency via registered dedup keys, retry/dead-letter, isolation-tier-aware placement | Hangfire or Quartz.NET, behind our own scheduling interface |
| 6 | `SentinelSuite.BlobStorage` | Adaptor-pattern file storage, tenant-scoped content-hash dedup, malware scan-then-quarantine, orphan cleanup | A plain object-storage SDK per adaptor (filesystem, S3-compatible, Azure Blob) |
| 7 | `SentinelSuite.AuditLogging` | Structured/immutable audit trail, hash-chain tamper-evidence | — |

### Tier 2 — Platform Core custom modules (no ABP-shaped precedent at all; fully bespoke either way)

| Order | Module | Feature(s) it owns | Why this position |
| --- | --- | --- | --- |
| 1 | `SentinelSuite.Settings` | Settings & Preferences | Nearly every other module registers configurable settings against this from day one |
| 2 | `SentinelSuite.CommandSystem` | Event/Command/Query Bus, Domain Events, Command/Action Bus, Command Palette, CLI-Style Input | "Unified action registry" — any feature exposing user-facing actions registers here; needed before the first feature module ships a UI action |
| 3 | `SentinelSuite.RealTimeDelivery` | Real-Time Delivery & Server-Side Timers | Owns the live console channel + Duration Watchdog primitive that Module 2 (Dispatch) leans on heavily |
| 4 | `SentinelSuite.Notifications` | Notifications Engine | Internal-only scope (never Module 17's mass-notification engine); needed once any workflow needs an alert |
| 5 | `SentinelSuite.TenantDefinedTypes` | Tenant-Defined Types & Custom Fields | `extended_fields` governance; several Master Records features assume this exists |
| 6 | `SentinelSuite.Gis` | GIS & Mapping Services | Needed starting Module 1 (Patrol location) and used everywhere after |
| 7 | `SentinelSuite.OfflineSync` | Offline Data Sync | Append-only outbox contract; needed before any mobile-facing feature ships |
| 8 | `SentinelSuite.ApiMessaging` | API & Messaging Layer (API keys, webhook subscriptions — REST/GraphQL hosting itself lives in the Host) | Needed once any external integration surface exists |
| 9 | `SentinelSuite.AiLlmServices` | AI / LLM Services | First real consumer is Module 1's AI-Assisted Incident Report Writing (last Module 1 feature) — fine to build just ahead of it, not day one |
| 10 | `SentinelSuite.GlobalSearch` | Global Search & Data Indexing | First real consumer (Command Palette) is a fast-follow per `mvp.md` — build before that ships, not before MVP |
| 11 | `SentinelSuite.BulkImport` | Bulk Import & Data Migration | Needed before the first production tenant onboards with legacy data, not before the first feature module |

### Tier 3 — `SentinelSuite.MasterRecords`

One module: Entity Registry Core, Party Registry, Person Registry, Organization Registry, Item Registry, Vehicle/Conveyance Registry, Location Registry, Activity Registry, Document Registry, Entity Relationships & History. This is the TPT identity spine (`Entity`/`EntityAssociation` roots, per `architecture-guidance.md`) — every feature module below extends it. Build immediately after Tier 1/2's foundation modules exist, before any feature module.

### Tier 4 — Feature modules (one per MODULES.md numbered category)

| # | Module name | Status | MVP? |
| --- | --- | --- | --- |
| 1 | `SentinelSuite.SecurityOperations` | Spec'd complete | **Yes** |
| 2 | `SentinelSuite.DispatchCad` | Spec'd complete | **Yes** |
| 3 | `SentinelSuite.CommandCenterEoc` | Spec'd complete | Deferred — "wallboards wait" |
| 4 | `SentinelSuite.AccessControl` | Spec'd complete | Deferred |
| 5 | `SentinelSuite.EmergencyManagement` | Spec'd complete | Deferred |
| 6 | `SentinelSuite.EmergencyPlanning` | Next to be elicited | No |
| 7 | `SentinelSuite.SafetyManagement` | Not elicited *except* the MVP hazard-by-location slice | **Slice only** |
| 8 | `SentinelSuite.Personnel` | Not elicited; out of MVP by design | No |
| 9 | `SentinelSuite.FacilityZoneManagement` | Not elicited *except* the MVP Location Hierarchy/Zone slice | **Slice only** |
| 10 | `SentinelSuite.EquipmentAssetsVehiclesResources` | Not elicited | No |
| 11 | `SentinelSuite.SubcontractorManagement` | Not elicited; out of MVP (contract-firm surface) | No |
| 12 | `SentinelSuite.PerformanceKpiReporting` | Not elicited | No |
| 13 | `SentinelSuite.InvestigationManagement` | Not elicited; fast-follow slice named in `mvp.md` | No (fast-follow) |
| 14 | `SentinelSuite.PolicyDocumentManagement` | Not elicited | No |
| 15 | `SentinelSuite.ContractClientManagement` | Not elicited; out of MVP (contract-firm surface) | No |
| 16 | `SentinelSuite.LostFound` | Not elicited; fast-follow named in `mvp.md` | No (fast-follow) |
| 17 | `SentinelSuite.MassNotification` | Not elicited | No |
| 18 | `SentinelSuite.ThreatIntelligenceOsint` | Not elicited | No |
| 19 | `SentinelSuite.PhysicalSecurityIntegrationGateway` | Not elicited | No |
| 20 | `SentinelSuite.K9SpecializedUnits` | Not elicited | No |
| 21 | `SentinelSuite.ComplianceAudits` | Not elicited; fast-follow slice named in `mvp.md` | No (fast-follow) |
| 22 | `SentinelSuite.BcDr` | Not elicited | No |
| 23 | `SentinelSuite.ExecutiveProtection` | Not elicited | No |
| 24 | `SentinelSuite.SpecialEventIap` | Not elicited | No |
| 25 | `SentinelSuite.SupplyChainCargoSecurity` | Not elicited | No |

Known cross-module coupling worth remembering for build order (from `_DECISIONS.md`, not exhaustive): `DispatchCad` supersedes `SecurityOperations`' ad hoc Patrol Request as the CAD front door — build `SecurityOperations` first. `CommandCenterEoc` composes `Gis`, `DispatchCad`'s Active Incident Queue, and `RealTimeDelivery` — build those three first (already satisfied by the tier ordering above). `AccessControl` leans on `MasterRecords`' BOLO Flag and Item custody mechanisms directly. The Module 9/7 MVP slices depend on `MasterRecords`' Location Registry and, per `mvp.md`, are elicited *after* `CommandCenterEoc` even though built before it (elicitation order and build order diverge here — see Build Order below).

---

## Host / composition layer

- **`SentinelSuite.HttpApi.Host`** — the one deployable API host (ASP.NET Core, plain — not an ABP host). References every module's `.HttpApi` and `.Application` project. Serves versioned REST for external integrations/webhooks and a GraphQL endpoint (HotChocolate) for the internal React/Next.js web frontend and React Native mobile app — both frontends are separate repos/solutions, out of scope for this doc. Kiosk Devices, Display Devices, and the future Client Portal are additional bounded, unauthenticated-but-scoped surfaces on this same host (distinct auth policies/token types), not separate hosts, unless a real scaling or air-gapped-deployment reason forces a split later.
- **`SentinelSuite.DbMigrator`** — console app; applies the composed `SentinelSuiteDbContext` EF Core migrations and runs seed data contributors across all modules. Run on every deploy, including the DOE self-hosted Docker path.
- **Background job execution** — runs in-process within `HttpApi.Host` for MVP. A dedicated `SentinelSuite.BackgroundWorker.Host` is a real future option once `BackgroundJobs`' isolation-tier-aware placement needs on-prem tenants' jobs to never touch shared compute — don't build it before that's an actual deployment, per principle 5.
- **No server-rendered MVC/Blazor UI host.** The frontend is API-first React/Next.js per `pdd.md`'s tech stack, consuming `HttpApi.Host` only.

---

## Build order (phased)

### Phase 0 — Walking skeleton
The seven Tier 0 kernel projects, built as real code in dependency order — `Domain.Shared` → `Domain` + `Application.Contracts` (parallel, both only need `Domain.Shared`) → `Application` + `EntityFrameworkCore` + `AspNetCore` (parallel, each needs one of the two prior) → `Modularity` (standalone, any time). Empty `HttpApi.Host` + `DbMigrator` referencing the kernel, CI pipeline, auth (IdP federation + local accounts, via `IdentityAccount`) working end-to-end with no other product features yet.

### Phase 1 — Core platform systems
Tier 1 modules, in the order listed in the Tier 1 table (`IdentityAccount` → `PermissionManagement` → `TenantManagement` → `FeatureManagement` → `BackgroundJobs` → `BlobStorage` → `AuditLogging`), then Tier 2 modules in the order listed in the Tier 2 table.

### Phase 2 — Master Records
`SentinelSuite.MasterRecords` — the TPT spine. Nothing in Phase 3 can start in earnest before this exists, even partially (Party/Person/Item/Location/Activity/Document can land incrementally, but the `Entity`/`EntityAssociation` roots must land first).

### Phase 3 — MVP feature modules (order per `mvp.md`)
1. `SecurityOperations` (Module 1, all 8 features)
2. `DispatchCad` (Module 2, all 8 features)
3. `FacilityZoneManagement` — **MVP slice only**: Location Hierarchy Designer + Zone Mapping (needs elicitation first — flagged in `mvp.md` as "after Module 3," which is an *elicitation*-order note; nothing stops building the slice's code once its requirements doc exists, even though `CommandCenterEoc` itself is build-deferred)
4. `SafetyManagement` — **MVP slice only**: hazmat/NFPA-704-by-location + dispatch-context hazard warnings (needs elicitation first)

MVP ships when Phase 3 is done. This is the point `docs/mvp.md`'s scope gates are checking against.

### Phase 4 — Spec'd-but-deferred modules
`CommandCenterEoc` (3), `AccessControl` (4), `EmergencyManagement` (5) — already fully spec'd, build after MVP ships, in this order (matches elicitation order and known coupling).

### Phase 5 — Not-yet-elicited modules
Everything else, in MODULES.md numeric order per the standing instruction that "module order is itself the priority order" (`_RESUME.md`) — **elicit the requirements doc first, then scaffold the module's projects, then build.** Fast-follow items called out in `mvp.md` (Command Palette/CLI-Style Input/Tenant-Defined Subtypes UI exposure, `LostFound`, `InvestigationManagement` slice, `ComplianceAudits` slice, self-hosted AI inference adaptor) jump the numeric queue the same way they jump it in `mvp.md`.

---

## Open questions

1. **Tier 1 library choices.** ASP.NET Core Identity + OpenIddict for `IdentityAccount`, Hangfire or Quartz.NET for `BackgroundJobs` are proposed defaults, not yet a `_DECISIONS.md`-level commitment — confirm before Phase 1 scaffolding starts.
2. **`CommandSystem` bundling.** Five spec'd features (bus, Domain Events, Command/Action Bus, Command Palette, CLI-Style Input) are proposed as one module. They were split into five *requirements docs* deliberately, but that doesn't necessarily mean five *projects* — confirm the one-module read is right before it's scaffolded.
3. **GraphQL library.** HotChocolate is assumed (the standard .NET GraphQL server, an ordinary library dependency, not a framework choice); not yet an explicit `_DECISIONS.md` entry. Worth a real decision line once `ApiMessaging`/Host GraphQL wiring is actually built.
4. **Repo layout.** This doc assumes the backend (.NET) and the two frontends (React/Next.js web, React Native mobile) live in separate repos/solutions. Confirm before Phase 0 scaffolding, since it affects CI and versioning setup.
