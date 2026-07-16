# Pitfalls Research

**Domain:** Hand-rolled .NET Clean Architecture / DDD kernel (Entity/EntityAssociation TPT taxonomy, capability interfaces, tenant/audit/domain-event conventions) — foundation for 26 modules / 222 features
**Researched:** 2026-07-15
**Confidence:** MEDIUM (cross-checked against Microsoft Learn EF Core docs, ABP official docs, dotnet/efcore GitHub issues, and multiple independent engineering write-ups; no primary vendor confirmation of .NET 10-specific EF Core behavior beyond what's in the docs cited)

## Critical Pitfalls

### Pitfall 1: Tenant filter not applied on every query path (raw SQL, background jobs, `IgnoreQueryFilters` misuse)

**What goes wrong:**
A global query filter on `TenantId` looks like it solves multi-tenancy everywhere, but it silently doesn't cover: (1) raw SQL via `FromSqlRaw`/`FromSqlInterpolated`, which bypasses EF Core query filters entirely; (2) any code path that calls `IgnoreQueryFilters()` to legitimately see soft-deleted rows for an admin screen, which — pre-EF Core 10 named filters — strips the tenant filter too, not just the soft-delete filter; (3) background jobs, message-bus consumers, and any non-HTTP-request code path where "current tenant" was never set because tenant resolution was wired only into the ASP.NET Core request pipeline. A single missing tenant scope on a reporting or admin query is the highest-blast-radius bug class in multi-tenant systems — it returns *every* tenant's matching rows to whoever triggers it.

**Why it happens:**
The naive implementation treats "apply a `Where(e => e.TenantId == currentTenantId)` global filter" as the whole solution. It doesn't budget for: filters not applying to raw SQL, filters compounding with soft-delete in ways that can't be selectively lifted, or tenant context needing to be ambient (AsyncLocal-based, not just HTTP-context-based) so it survives into queued jobs and background workers.

**How to avoid:**
- Never allow raw SQL as an approved escape hatch in this kernel's query surface; if it's ever needed, require the tenant predicate to be part of the SQL template, not bolted on by convention.
- Design the tenant-filter and soft-delete-filter as independently liftable from day one — either via EF Core 10+ named filters (`IgnoreQueryFilters(["SoftDelete"])` while leaving tenant active) or by hand-rolling two separate filter predicates that compose, never one combined predicate.
- Make "current tenant" resolution ambient via an `ICurrentTenant`-equivalent service backed by `AsyncLocal<T>`, not `HttpContext`, so it flows into background jobs, event handlers, and outbox workers without being silently null (which — if the filter predicate does `e.TenantId == currentTenant.Id` with `currentTenant.Id` null/default — can return zero rows silently, which is *safer* than a leak but still a correctness bug that's easy to miss in testing).
- Write a startup-time or CI-time static check (or Roslyn analyzer) that flags any raw-SQL/`FromSql*` usage against a tenant-scoped entity type without an explicit tenant predicate in the same statement.

**Warning signs:**
- Any query written against a `DbSet<T>` where `T : Entity` uses `FromSqlRaw`/`FromSqlInterpolated`.
- Admin/reporting features that call `IgnoreQueryFilters()` "to see everything" — audit whether that also silently drops tenant scoping.
- Integration tests that only exercise the HTTP-request path (tenant resolved from claims) and never exercise a background-job or event-handler path with tenant context propagated manually.

**Phase to address:**
Multi-tenancy plumbing phase (data filtering convention) — but the ambient-context design decision needs to be made and reviewed *before* the Entity base class phase locks in how `TenantId` is read at query time, since retrofitting ambient-vs-request-scoped resolution after concrete types exist is expensive.

---

### Pitfall 2: Soft-delete doesn't cascade to dependents, or cascades via DB-level `ON DELETE CASCADE` and hard-deletes anyway

**What goes wrong:**
Two opposite failure modes, both common: (a) a parent entity is soft-deleted (`IsDeleted = true`), but its children/dependents are untouched — they remain queryable and "active" while pointing at a logically-deleted parent, producing orphaned-looking data in every downstream read; (b) the DB schema still has a real `ON DELETE CASCADE` foreign key configured (left over from before soft-delete was added, or added because "cascade delete" sounds like the right EF Core convention), so some code path that does issue a hard delete on the parent silently hard-deletes the entire dependent subtree instead of being caught and converted to a soft-delete.

**Why it happens:**
EF Core's cascade-delete behavior is a *hard*-delete mechanism by default; soft-delete is a query-filter/flag convention layered on top by the application, and EF Core has no built-in concept that connects the two. Nothing stops a hard `ON DELETE CASCADE` FK from coexisting with an app-level soft-delete convention — they were configured independently and nobody revisits the FK behavior once soft-delete is retrofitted.

**How to avoid:**
- Set FK delete behavior to `Restrict` (not `Cascade`) for every relationship from a soft-deletable entity, everywhere, as a kernel-level default enforced by convention/base-class configuration rather than left to per-type `OnModelCreating` code.
- Implement cascade *soft*-delete explicitly at the application layer (e.g., a `SaveChanges` interceptor or aggregate-level method that walks known owned/dependent collections and flips `IsDeleted` on children when the parent is soft-deleted) rather than relying on any DB mechanism.
- Decide and document, per `EntityAssociation`/`Entity` relationship, whether soft-deleting the "one" side should cascade to the "many" side, and encode that as an explicit kernel contract (e.g., an `ICascadeSoftDeletable` marker + registry, matching the registry-authoritative discipline already established for capabilities) rather than leaving it to each module author's judgment.

**Warning signs:**
- Any `OnDelete(DeleteBehavior.Cascade)` configuration anywhere in the codebase once soft-delete exists.
- Queries that join parent-to-child and return children whose parent `IsDeleted == true`.
- No `SaveChanges` interceptor or explicit test verifying cascade soft-delete behavior for at least one Entity → EntityAssociation relationship.

**Phase to address:**
Auditing & soft-delete base contracts phase — this needs an explicit design decision (cascade or not, and how) before any `EntityAssociation` relationships are built on top, since retrofitting cascade behavior after modules assume "delete parent, children stay" (or vice versa) is a breaking behavioral change.

---

### Pitfall 3: Domain events dispatched before the transaction commits, corrupting the write on handler failure — or dispatched after commit and silently lost on crash

**What goes wrong:**
There are exactly two naive choices and both are wrong in a different way. Dispatching collected domain events *before* `SaveChanges`/`CommitAsync` completes means a misbehaving handler (throws, or does its own DB write that fails) can abort or corrupt the very transaction that raised the event — the side effect and the fact being recorded become coupled in a way that shouldn't be. Dispatching *after* commit (the more common "fix") solves that but reopens the dual-write problem: if the process crashes between commit and dispatch, the event is lost forever with no record it should have fired, and there's no way to know which committed writes still owe an event.

**Why it happens:**
"Collect events on the aggregate, dispatch them somewhere in SaveChanges" is the ABP/ Clean Architecture pattern everyone reaches for, but the *timing* detail — same transaction vs. after commit, and what happens on partial failure — is exactly the part that's easy to leave unspecified when hand-rolling, because it "works" in every manual test (no crashes happen in dev).

**How to avoid:**
- Adopt the transactional Outbox pattern from the start: a `SaveChanges`/`DbContext` interceptor that, in the *same* transaction as the business-entity changes, serializes collected domain events into an `OutboxMessages`-equivalent table. A separate, decoupled dispatch step (background worker, polling, or a post-commit hook) reads and publishes them after the transaction is durably committed.
- This guarantees at-least-once delivery (the event row and the business data commit or roll back together) — but it requires handlers to be idempotent, since at-least-once means occasional redelivery.
- Since this milestone is Domain-layer only (no persistence/DbContext yet, per PROJECT.md's Out of Scope), the *domain-layer* contract to get right now is: define the collection-on-aggregate convention (`DomainEvents` list, `AddDomainEvent`/`ClearDomainEvents`) as pure in-memory state with no assumption about *when* it's read — leave dispatch timing as an explicit, documented seam for the infrastructure layer to implement via interceptor in the next milestone. Do not bake "dispatch on `SaveChanges`" logic into the Domain project, since Domain shouldn't know about persistence at all — but do document the outbox expectation now so infrastructure work doesn't reinvent this decision.

**Warning signs:**
- Any code that dispatches events synchronously inline with business logic before persistence, rather than collecting them for later drain.
- No test exercising "handler throws" to verify it doesn't corrupt the aggregate's own persisted state.
- No answer to "what happens if the process dies right after commit but before dispatch" documented anywhere.

**Phase to address:**
Domain events phase (collection-on-aggregate + dispatch convention) for the collection contract; explicitly flag the dispatch-timing/outbox decision as a **carry-forward decision for the next milestone's infrastructure/persistence layer**, not something this Domain-only milestone can fully resolve — but it must be named and written down now so the Domain-layer event shape (event immutability, serializability, no infrastructure references) doesn't have to change later to support an outbox.

---

### Pitfall 4: Hand-rolling ABP's module system as "just an ordered list of `IServiceCollection` extension calls" and losing dependency-graph resolution, transitive dependencies, and idempotent initialization

**What goes wrong:**
ABP's module system walks the full dependency graph from the startup module, resolves transitive dependencies from each module's declared *direct* dependencies only, topologically sorts, and — critically — ensures each module initializes exactly once even when reached via multiple paths (diamond dependencies). A hand-rolled "module system" that's really just "call `AddModuleX()`, then `AddModuleY()`, in the order I feel like registering them in `Program.cs`" reproduces none of this. It works fine for the first few modules being wired together by the same person in the same sitting, then breaks in one of two ways once the platform has 26 modules: (a) module B silently depends on module A having registered something first, and someone reorders the registration list without knowing why, or (b) two modules both depend on a shared third module and it gets initialized twice, double-registering services or re-running startup side effects.

**Why it happens:**
This is exactly the class of "reproduce the pattern, not the package" risk called out in the milestone context — the *visible* surface of ABP's module system (a `[DependsOn]`-attributed class) is trivial to copy; the *invisible* value (graph resolution, dedup, deterministic ordering under diamond dependencies) is the part that's easy to under-scope because it doesn't show up as a requirement until module count is high enough to create a diamond, which won't happen with the first 2-3 modules built and tested.

**How to avoid:**
- Build the module system with real dependency-graph resolution (topological sort over an explicit `[DependsOn]`-equivalent attribute or declaration) and initialization dedup from the first version, even though this milestone only needs to prove the layout works with a small number of modules — do not defer graph resolution as "we'll add that when we have more modules," because by the time it's needed, dozens of modules will already assume registration-order-independence.
- Write a unit test in *this* milestone that constructs a synthetic diamond dependency (Module D depends on B and C; both B and C depend on A) and asserts A initializes exactly once and before B/C, and D initializes last. This is cheap to write now and catches the exact failure mode before any real module depends on it.
- Explicitly scope out what ABP's module system does that this platform does NOT need yet (e.g., dynamic/conditional module loading, plugin-style runtime module discovery) rather than silently omitting it — write it down as a documented non-goal so a future contributor doesn't assume it exists.

**Warning signs:**
- No test exercising a diamond/shared-dependency scenario.
- Module registration order in `Program.cs`/composition root is manually curated rather than derived from declared dependencies.
- Any module's `ConfigureServices` assumes another module already ran, without that dependency being declared.

**Phase to address:**
Module system phase — must land with graph resolution and a dedup test, not deferred, because it is inherited by all 26 modules' composition roots and is expensive to retrofit once modules exist that assume flat/manual ordering.

---

### Pitfall 5: Base classes left open to inheritance "just in case," turning `Entity`/`Party`/`Activity` into extension points that 200+ concrete types quietly depend on

**What goes wrong:**
`docs/architecture-guidance.md` §1 already names the correct rule (sealed by default, extension points designed not defaulted) and its failure mode (fragile base class / yo-yo problem). Concretely, in *this* kernel, the failure mode looks like: someone adds a `protected virtual` method to `Entity` or `Party` "in case a future module needs to customize X," a handful of the first concrete types (built in the *next* milestone) override it because it's there and convenient, and now that virtual member is permanently part of the base class's contract — removing or changing its default behavior requires auditing every override across whichever modules landed by then. Because this base-class shape is inherited by every one of 222 planned features' domain types, a virtual-method extension point added carelessly in Milestone 1 doesn't cost one bad override — it costs N overrides across N future modules, each one a place where "understanding this concrete type" now requires reading 4 ancestors' half-implemented workflows (the exact yo-yo problem the guidance doc names).

**Why it happens:**
Virtual-by-default is C#'s cultural norm from pre-`sealed`-discipline codebases, and "what if a module needs to override this later" feels like free optionality when you're the one writing the base class. It isn't free — it's a standing commitment every future author of a derived type has to reason about, and by the time the cost is visible (three modules deep, someone needs to change base behavior and can't tell which overrides depend on the old behavior), it's a multi-module refactor instead of a one-file edit.

**How to avoid:**
- Every class in the `Entity`/`Party`/`Activity`/`Item`/`Location`/`Document`/`EntityAssociation` hierarchy is `sealed` unless it is one of the explicitly-named abstract intermediate roots that must remain open *specifically* to be inherited by concrete leaf types (that's a different thing from "open to arbitrary customization via virtual members" — an abstract class can be non-sealed for the sole purpose of being a base for concrete subtypes while still having zero `virtual` members).
- No `protected virtual` or `public virtual` member on any base class in this hierarchy without a written justification captured in an ADR-equivalent note, at write time, naming the concrete reason it must vary per-descendant — not a hypothetical future one. Enforce via code review checklist for this milestone, since there's no automated tool that reliably catches "this virtual isn't needed yet."
- Where per-type variation is genuinely needed (audit field population strategy, display-label logic, etc.), route it through composition (an injected service operating over a capability interface) per §4 of the guidance doc, not a template-method override — this is the same discipline the guidance doc already prescribes for behavioral machinery generally, applied specifically to the temptation to put it on the base class instead.
- Treat any request to "make this virtual just in case" during this milestone as a design smell requiring the requester to name the concrete second implementation that needs it *now* — if there isn't one, it doesn't go in.

**Warning signs:**
- Any `virtual` or `abstract` method on `Entity`, `Party`, `Activity`, `Item`, `Location`, `Document`, or `EntityAssociation` that isn't backed by at least two genuinely different concrete implementations already planned in the requirements docs.
- A concrete type overriding a base method to do something the base's default already almost does, with a small tweak (a strong signal the behavior should have been composed/parameterized, not overridden).
- Code review comments like "just in case a future module needs this" as the sole justification for non-sealed or virtual.

**Phase to address:**
This applies to *every* phase that touches the Entity/EntityAssociation/intermediate-root class hierarchy — it is not a single phase's concern but a standing constraint that should be a review gate on every PR touching the kernel's abstract classes. If the roadmap phases by kernel piece (Entity base class → EntityAssociation → capability interfaces → multi-tenancy → auditing → module system), this pitfall should be called out explicitly as an acceptance criterion in the **Entity base class phase** (first and highest-leverage place to get it right) and re-verified at the **EntityAssociation phase** (second-highest leverage, since it's the other TPT root), with a lightweight review checklist item carried into every subsequent phase.

---

### Pitfall 6: TPT's 4-level chain hits EF Core limitations beyond raw JOIN performance — complex types, owned types, and polymorphic query fan-out

**What goes wrong:**
The architecture-guidance doc already flags TPT's JOIN/union-fanout performance cost and names the right mitigations (CQRS read models, targeted TPH, denormalization). What it doesn't fully enumerate: TPT has had real *feature* limitations, not just performance ones, that specifically bite deep hierarchies. Historically (pre-EF Core 11), complex types (`ComplexProperty`, EF Core 8+) and JSON columns could not be used on any entity participating in a TPT/TPC hierarchy at all — meaning if `Party` or `Employee` wants a value-object-shaped field (an address, a name-structure) mapped as a complex type, that requires confirming the target EF Core version actually supports it on TPT entities. Separately, EF Core has never supported *owned entity types* participating in inheritance hierarchies (`efcore#14451`, still open/by-design) — so any pattern that would map a value object as an owned type (rather than a complex type) cannot be used on any type in the TPT chain. Polymorphic LINQ queries (`context.Set<Party>().OfType<Employee>()` or querying the abstract root and expecting the right derived columns) generate large `UNION ALL`/multi-`LEFT JOIN` SQL that gets worse combinatorially with hierarchy depth — a query against the `Party` root that needs to discriminate across `Employee`, `Contractor`, `Visitor`, etc. joins in every leaf table even if the caller only cares about one subtype, unless the query is written to target the concrete type directly.

**Why it happens:**
TPT's real-world constraints are undocumented in most day-to-day EF Core material (which focuses on "TPT vs TPH vs TPC" at a conceptual level) — the complex-type and owned-type restrictions are the kind of thing only discovered when a concrete type actually needs a value-object-shaped field, which is exactly the situation this platform's `Employee`/`Person`/`Party` types will hit immediately (names, addresses, contact info are natural value objects).

**How to avoid:**
- Before committing to complex types or owned types for value-object-shaped fields anywhere in the TPT hierarchy, verify against the actual target EF Core/.NET 10 version's release notes whether TPT + complex-types is supported (per this research, EF Core 11 added it — confirm the platform's actual EF Core version at persistence-layer implementation time, since this Domain-only milestone doesn't add EF Core yet but the type shapes chosen now constrain what's mappable later).
- Since this milestone is Domain-only (no EF Core/DbContext, per PROJECT.md), the concrete risk *now* is: don't design value-object-shaped fields on `Entity`/`Party`/etc. assuming a specific EF Core mapping strategy (owned type) will "just work" later — favor plain C# value-object types with EF Core Complex Type or simple scalar-column mapping in mind, and flag any value object that might need its *own* sub-hierarchy (a value object that itself varies by subtype) as a known TPT-mapping risk to resolve explicitly in the next milestone's technical-spec phase, not discovered mid-implementation.
- For polymorphic queries, design the CQRS read-model mitigation (already prescribed by the guidance doc) to be the *only* sanctioned way to query "give me all Parties regardless of subtype" — never let application code query the abstract root type directly in a hot path; that's precisely the union-fanout trap.

**Warning signs:**
- Any concrete type design that assumes an EF Core owned-type mapping for a nested value object, once persistence lands.
- Any application-layer query written against an abstract intermediate root (`Party`, `Activity`) rather than a concrete type or a CQRS read-model projection.
- No documented decision for which EF Core version this platform is pinned to when the persistence-layer milestone begins (EF Core's TPT feature surface has changed release-to-release: complex types added in EF Core 11).

**Phase to address:**
Not directly actionable in this Domain-only milestone (no EF Core dependency yet), but the **Entity/EntityAssociation base class phases** should avoid baking in any assumption about value-object mapping strategy, and this should be explicitly logged as an open question for the next milestone's persistence/EF Core technical-spec phase — include it in this milestone's "Gaps to Address" so it isn't silently forgotten.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Combining tenant-filter and soft-delete-filter into one `Where` predicate instead of two composable filters | Less code now | Can't selectively lift one without the other (pre-named-filters); admin "show deleted" screens accidentally leak cross-tenant | Never — split them from the start, it costs nothing extra now |
| Leaving `Entity`/`Party` base class methods `virtual` "for flexibility" | Feels safer, avoids a future "can't override" support request | Every future override is a standing base-class contract across 200+ types; base-class changes become high-risk refactors | Never in this milestone — only add virtual with a named, current second implementation |
| Dispatching domain events synchronously inline before deciding on outbox | Ships faster, works in manual testing | Handler failure corrupts business transaction; crash-between-commit-and-dispatch loses events silently in prod | Acceptable only as an explicitly-labeled placeholder with a tracked follow-up for the outbox implementation next milestone — not acceptable to ship unlabeled |
| Manual/ordered module registration instead of graph-resolved module system | Fewer moving parts for 2-3 modules | Diamond dependencies double-initialize or silently order-depend once module count grows | Never past the first couple of modules being wired by the same person in one sitting — this platform is headed to 26 modules, so build the graph resolver now |
| Designing value-object fields assuming EF Core owned-type mapping | Familiar, "obvious" EF Core pattern | Owned types are unsupported on any type participating in inheritance hierarchies — blocks the whole TPT chain later | Never for any type that will live inside the Entity/Party/Activity TPT chain |

## Integration Gotchas

Common mistakes when connecting to external services (relevant here: the "framework" being reproduced, not persistence integrations, since this milestone has no external service integrations).

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| ABP pattern reproduction (multi-tenancy, auditing, events, modules) | Copying the visible API shape (`ICurrentTenant`, `[DependsOn]`) without the invisible mechanics (resolver-chain fallback, dependency-graph dedup, outbox timing) | Treat each ABP pattern as "what problem does the *mechanism*, not the *interface*, solve" and verify the hand-rolled version handles the same edge cases (diamond deps, non-HTTP tenant context, transactional event persistence) before declaring it done |
| Ardalis pattern reproduction (Specification, Result, GuardClauses, SmartEnum) | Reproducing only the happy-path API surface and missing edge-case behavior the package handles (e.g., Specification's `Include`/paging/ordering composition, SmartEnum's reflection-based instance registry and equality) | Read the actual package source (even though not taking the dependency) for the specific edge cases it handles beyond the obvious API, and decide explicitly which ones this platform needs vs. can skip — don't assume "it's a small pattern" without checking |
| Future EF Core version pin (not this milestone, but constrained by decisions made here) | Assuming current TPT feature support (complex types, JSON columns) will remain in the version eventually adopted | Confirm actual EF Core version's TPT feature matrix at persistence-layer implementation time; don't let this Domain milestone assume a specific EF Core version's capabilities |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Querying abstract TPT roots (`Party`, `Activity`) directly instead of concrete types or read models | Query plans show multi-way LEFT JOIN/UNION across every leaf table in the hierarchy even when only one subtype's data is needed | Route all polymorphic reads through CQRS read-model projections (per architecture-guidance.md); never let feature code `context.Set<Party>()` in a hot path | Becomes visible in real query latency once >3-4 concrete subtypes exist under a root and the table has nontrivial row counts — likely by the second or third module built on this kernel |
| Missing indexes on `TenantId`/`IsDeleted` filter columns | Every query silently gets slower as row counts grow, with no obvious cause in application code (filter is invisible in LINQ, applied by EF Core under the hood) | Bake index requirements into the base-class's EF Core configuration convention (once persistence lands) so every TPT-mapped table indexes these columns by default, not per-type opt-in | Noticeable once any single tenant's table exceeds tens of thousands of rows |
| Cascade soft-delete implemented as a runtime walk of tracked navigation properties (Include-then-flip) | Works in tests with a few loaded entities; silently does nothing for large/untracked dependent trees since it only affects currently-tracked entities | Prefer set-based `ExecuteUpdateAsync` bulk cascade soft-delete for large dependent trees, reserve the tracked-entity walk for small/known-bounded child sets | Breaks silently (not an exception, just incomplete cascade) once a parent has more dependents than were ever `Include()`d in the deleting code path |

## Security Mistakes

Domain-specific security issues beyond general web security (this platform is explicitly FedRAMP/FISMA/NIST 800-53-bound).

| Mistake | Risk | Prevention |
|---------|------|------------|
| Tenant isolation enforced only at UI/middleware/application-service layer, not at the data-access layer | Any code path that reaches the DbContext directly (a new report, an admin tool, a future integration) bypasses tenant scoping entirely — the widest-blast-radius bug class in multi-tenant systems, and a plausible FedRAMP/GDPR-class finding given this platform's DOE/national-lab customer base | Enforce tenant scoping as close to the data as possible — global query filter as the baseline, with row-level security as a defense-in-depth option for the highest-sensitivity deployment tiers (dedicated_db/on_prem tenants per the platform's tiered isolation model) |
| Treating the registry-vs-interface capability system (guidance doc §3) as advisory rather than enforced | A tenant-defined subtype's registry declaration and a developer-built type's interface implementation silently drift (e.g., a type implements `ICustodyTracked` but its registry row says `is_custody_tracked = false`, or vice versa) — in a chain-of-custody or compliance-audit context, this is a data-integrity/audit-trail defect, not just a code-quality one | Implement the guidance doc's recommended startup validation pass (assert every developer-built type's implemented capability interfaces match its registrations, fail fast on drift) as a hard requirement of the capability-interface-scaffold phase, not an optional nice-to-have |
| Soft-delete treated as equivalent to data removal for compliance purposes | Soft-deleted records remain in the database and are recoverable via `IgnoreQueryFilters()` or raw SQL — if any regulatory requirement in this platform's scope (FISMA, DOE Orders) requires actual data destruction on a retention-expiry or right-to-erasure event, soft-delete alone doesn't satisfy it | Document explicitly, at the kernel level, that soft-delete is a *logical* delete convention only — any hard-deletion/purge requirement needs a separate, explicit mechanism, not an assumption that soft-delete covers it |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces — specific to this kernel milestone.

- [ ] **Entity base class:** Often missing an ambient (AsyncLocal-based) current-tenant/current-user resolution contract — verify it isn't implicitly assumed to be HTTP-request-scoped only, since Domain layer must stay persistence/HTTP-agnostic but the *contract* for how tenant/audit context reaches an entity still needs to be designed here.
- [ ] **EntityAssociation named-kind pattern:** Often missing the explicit TPT-vs-TPH-per-hierarchy decision documented per kind (guidance doc §"earn-your-existence test" already permits TPH for field-less kinds) — verify each named kind has a stated mapping-strategy decision, not a default assumption that all kinds are TPT.
- [ ] **Capability interface scaffold:** Often missing the startup validation pass asserting interface-implementation matches registry declaration — a capability interface with no enforcement is a compile-time-only mirror.
- [ ] **Domain events convention:** Often missing an explicit statement of dispatch-timing responsibility boundary (Domain collects; Infrastructure dispatches via outbox) — verify this boundary is written down, not just implied by "SaveChanges" being outside this milestone's scope.
- [ ] **Multi-tenancy plumbing:** Often missing a documented answer for "what happens when current tenant is null/unset" (default-deny/zero-rows vs. exception vs. silently unscoped) — verify this is an explicit decision, not whatever the first implementation happened to do.
- [ ] **Auditing & soft-delete base contracts:** Often missing the cascade-soft-delete decision per relationship (see Pitfall 2) — verify it's not silently deferred to "figure it out when we build the first EntityAssociation."
- [ ] **Module system:** Often missing a diamond-dependency test — verify one exists even with only 2-3 real modules currently defined.
- [ ] **Sealed-by-default discipline:** Often missing an actual review gate — verify there's a checklist item or lint rule catching new `virtual`/non-sealed additions to the taxonomy classes, not just a documentation reminder.

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|-----------------|
| Tenant filter gap discovered after modules built on top | HIGH | Audit every raw-SQL/`IgnoreQueryFilters()` usage platform-wide; add regression tests per discovered gap; likely requires a security disclosure/review given FedRAMP posture if any real tenant data was exposed, even in non-prod |
| Base class virtual member proliferation discovered late (3+ modules deep) | HIGH | Requires cataloging every override across every module, determining which are load-bearing vs. incidental, then a coordinated multi-module refactor to seal the base and move real variation to composition — budget this as a dedicated remediation phase, not a quick fix |
| Domain event lost due to dispatch-before-outbox design | MEDIUM | Retrofit the outbox pattern at the infrastructure layer (next milestone); for the Domain-layer contract itself, low cost if event collection was already designed persistence-agnostic per Pitfall 3's prevention — high cost if dispatch logic leaked into Domain types |
| Module system diamond-dependency bug found late | MEDIUM | Usually fixable by adding graph resolution retroactively, but requires re-auditing existing module registration order for latent bugs that were masked by luck (registration order happened to work) rather than correctness |
| TPT + owned-type mapping chosen and later found unsupported | MEDIUM | Requires migrating value-object fields from owned-type to complex-type or flattened-scalar mapping — a schema and code change, but contained to the affected value objects, not the whole hierarchy, if caught before many concrete types exist |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls (assumes a roadmap phased by kernel piece: Entity base class → EntityAssociation → capability interfaces → multi-tenancy → auditing/soft-delete → domain events → module system → Specification/Result/GuardClauses/SmartEnum).

| Pitfall | Prevention Phase | Verification |
|---------|-------------------|--------------|
| Tenant filter gaps (raw SQL, background jobs, `IgnoreQueryFilters` scope) | Multi-tenancy plumbing phase | Ambient tenant-context contract documented; test asserting tenant context survives into a non-HTTP-simulated call path; explicit decision on null-tenant behavior |
| Soft-delete cascade / DB cascade-delete conflict | Auditing & soft-delete base contracts phase | Explicit per-relationship cascade-soft-delete decision documented; at least one worked example test (once EntityAssociation exists) |
| Domain event dispatch timing / outbox boundary | Domain events phase | Written boundary statement: Domain collects only, Infrastructure dispatches transactionally; event shape reviewed for serializability/no infra references |
| Module system dependency-graph under-scoping | Module system phase | Diamond-dependency unit test passing; module registration derived from declared dependencies, not manual ordering |
| Base classes left open to inheritance "just in case" | Entity base class phase (primary), re-verified at EntityAssociation phase | Every `virtual`/non-sealed member on a taxonomy class has a named, current second-implementation justification; review checklist item carried into every subsequent kernel phase |
| TPT feature limitations (complex/owned types, polymorphic fan-out) beyond raw performance | Not a phase in this milestone (Domain-only, no EF Core) — logged as an explicit gap for the next milestone's persistence/EF Core technical-spec phase | Confirmed EF Core version's TPT+complex-type support before any value-object mapping strategy is finalized; CQRS read-model mitigation is the only sanctioned path for cross-subtype queries |
| ABP/Ardalis pattern reproduction under-scoping generally | Each pattern's own phase (multi-tenancy, auditing, events, module system, Specification, Result, GuardClauses, SmartEnum) | For each pattern, an explicit written note of which edge cases from the original package were evaluated and either covered or deliberately scoped out — not silently dropped |

## Sources

- [Modeling for Performance - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/performance/modeling-for-performance)
- [Inheritance - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/modeling/inheritance)
- [Entity Framework inheritance modelling, TPH v TPT v TPC with EF 8 on .NET 8 — dotnetbenchmarks.com](https://dotnetbenchmarks.com/benchmark/1040)
- [Entity Framework Core - Table-Per-Type (TPT) Is Not Supported, Is It? — Thinktecture](https://www.thinktecture.com/en/entity-framework-core/table-per-type-inheritance-support-part-1-code-first/)
- [Relational: TPT inheritance mapping pattern · Issue #2266 · dotnet/efcore](https://github.com/dotnet/efcore/issues/2266)
- [What's New in EF Core 11 | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)
- [Support complex types on entity types participating in inheritance hierarchies (TPH/TPT/TPC) · Issue #35025 · dotnet/efcore](https://github.com/dotnet/efcore/issues/35025)
- [Inheritance hierarchies that include owned entity types are not supported · Issue #14451 · dotnet/efcore](https://github.com/dotnet/efcore/issues/14451)
- [Global Query Filters - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Global Query Filters in EF Core - Soft Delete, Multi-Tenancy & Named Filters in .NET 10 - codewithmukesh](https://codewithmukesh.com/blog/global-query-filters-efcore/)
- [Soft Deletes in EF Core 10 - Interceptors, Named Filters & Cascade Delete - codewithmukesh](https://codewithmukesh.com/blog/soft-deletes-efcore/)
- [How to make soft delete in cascade with EF Core including navigation properties? · Issue #11240 · dotnet/efcore](https://github.com/dotnet/efcore/issues/11240)
- [Cascade Delete - EF Core | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/saving/cascade-delete)
- [EF Core Tenant Isolation: Global Query Filters for Secure Multi-Tenant SaaS | ByteCrate](https://bytecrate.dev/efcore-tenant-isolation-global-filters/)
- [Tenant ID Filtering in SaaS: ORM Filters, Query Helpers, and Scope Drift | Agnite Studio](https://agnitestudio.com/blog/tenant-id-filtering-saas/)
- [Multi-Tenancy | ABP.IO Documentation](https://abp.io/docs/latest/framework/architecture/multi-tenancy)
- [Data Filtering | ABP.IO Documentation](https://abp.io/docs/latest/framework/infrastructure/data-filtering)
- [Modularity | ABP.IO Documentation (Module-Development-Basics)](https://docs.abp.io/en/abp/latest/Module-Development-Basics)
- [Modularity and Dependency Injection | abpframework/abp | DeepWiki](https://deepwiki.com/abpframework/abp/2.3-event-bus-and-distributed-events)
- [Reliable Messaging in .NET: Domain Events and the Outbox Pattern with EF Core Interceptors - DEV Community](https://dev.to/stevsharp/reliable-messaging-in-net-domain-events-and-the-outbox-pattern-with-ef-core-interceptors-pjp)
- [Transactional Outbox Pattern in .NET EF Core: Manual, and Semi-Auto | Jordan Rowles - Medium](https://jordansrowles.medium.com/outbox-pattern-in-net-ef-core-manual-and-semi-auto-e6bd2fd26a98)
- [How To Use Domain Events To Build Loosely Coupled Systems - Milan Jovanović](https://milanjovanovic.tech/blog/how-to-use-domain-events-to-build-loosely-coupled-systems)
- [Entity Base Class · Enterprise Craftsmanship](https://enterprisecraftsmanship.com/posts/entity-base-class/)
- [Seedwork (reusable base classes and interfaces for your domain model) - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/seedwork-domain-model-base-classes-interfaces)
- [C# Tip: Mark a class as Sealed to prevent subclasses creation | Code4IT](https://www.code4it.dev/csharptips/sealed-classes/)
- [Fragile base class - Wikipedia](https://en.wikipedia.org/wiki/Fragile_base_class)
- `.planning/PROJECT.md` and `docs/architecture-guidance.md` (project-internal, authoritative for this milestone's constraints)

---
*Pitfalls research for: Hand-rolled .NET Clean Architecture/DDD kernel with deep TPT inheritance (Sentinel Suite Milestone 1)*
*Researched: 2026-07-15*
