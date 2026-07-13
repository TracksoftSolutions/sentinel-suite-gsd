# Code Architecture Guidance — Inheritance, Interfaces & Composition

**Status:** Guidance (not a feature/requirements doc)
**Audience:** Anyone writing platform code, including AI-assisted implementation sessions
**Relationship to other docs:** `docs/requirements/_DECISIONS.md` records *what the platform is*; this doc records *how the code that builds it should be shaped*. Where they touch (TPT, registries, tenant-defined subtypes), this doc defers to the requirements docs — it never overrides them.

## Why this doc exists

The platform's domain model is genuinely taxonomic — Employee IS-A Person IS-A Party IS-A Entity is what the security domain actually looks like, not an OO exercise imposed on it (NIEM, built by people modeling the same domain, has the same shape). The data layer already committed to this via Table-Per-Type inheritance. This doc sets the discipline for carrying that commitment into code deliberately: inheritance and abstract classes where the domain is a taxonomy, interfaces where it isn't, and composition where neither applies. The point is not "inheritance everywhere" or "composition over inheritance" dogma in either direction — it's putting each tool exactly where its shape matches the problem.

## The three-way rule

> **Inherit what a thing IS. Interface what a thing CAN. Compose what a thing USES.**

### 1. Inheritance & abstract classes — the identity taxonomy

- The concrete TPT hierarchies (**Entity** → Party/Item/Location/Activity/Document → their extensions; **EntityAssociation** → its named kinds) map one-to-one to C# class hierarchies. The data model and the type system tell the same story; EF Core's TPT mapping is the bridge.
- Roots and intermediate levels that are never instantiated directly (`Entity`, `Party`, `Activity`, `EntityAssociation`) are **abstract classes**: they hold the shared fields, identity mechanics, and invariants (tenant scoping, audit fields, status) that every descendant genuinely has.
- **Structural depth is fine; behavioral depth is not.** A four-level field/identity chain (Employee → Person → Party → Entity) is the domain's real shape. But deep chains of virtual-method overrides — where understanding one subclass means reading four ancestors' half-implemented workflows (the yo-yo problem, the fragile base class problem) — are how inheritance rots. Base classes carry *state shape and invariants*; they should carry *process logic* only when that logic is genuinely invariant for every descendant, forever.
- Default to **sealed** (or effectively-final) classes and non-virtual members unless a type is explicitly designed as an extension point. Openness to inheritance is a design commitment, not a default courtesy.

### 2. Interfaces — capabilities that cross-cut the taxonomy

- Capabilities deliberately do **not** follow the taxonomy: Checkpoint Scan IS an Activity but is NOT mergeable; Document is nowhere near Party but IS display-labeled. If capabilities are pushed into base classes because "everything inherits from Entity anyway," every capability added for one type's benefit lands on all of them, and `Entity` becomes a god class.
- Capabilities are therefore **interfaces**, implemented by exactly the concrete types that have them: `IMergeable`, `IDisplayLabeled`, `IBoloFlaggable`, `IOfflineCapturable`, `ICustodyTracked`, and so on as the requirements docs' capability declarations acquire code counterparts.
- C# default interface members are acceptable for genuinely shared capability logic; prefer them over pushing capability behavior into the class hierarchy.
- **A marker interface with no members is a smell.** If an interface exists only to say "this type participates in X," that fact is registry metadata (see below) wearing a costume — declare it in the registration and let code query the registry, or give the interface real members.

### 3. The runtime registry is authoritative — interfaces mirror it, never replace it

This is the platform-specific constraint that outranks any general OO preference:

- **Tenant-Defined Subtypes have no code class at all** (by explicit decision — see `_DECISIONS.md`). A tenant's "Wildlife Encounter" Activity kind exists only as an anchor-typed record plus a registration row. It can never implement a compile-time interface.
- Therefore every capability that tenant-defined subtypes can participate in **must be declared in the runtime registry** (`is_mergeable`, `display_label_strategy`, queue role, offline write class, carrier eligibility). The C# interfaces are the developer-side mirror of those declarations — a convenience for static typing on developer-built types — never a second source of truth.
- **When an interface implementation and a registration disagree, the registration wins at runtime.** Recommended enforcement: a startup validation pass asserting that every developer-built type's implemented capability interfaces match its registrations, failing fast on drift rather than letting the two quietly diverge.

### 4. Composition — services, workflows, process logic

- Behavioral machinery — sync, notification delivery, report generation, dispatch flows, validation services — is **composed and injected**, not inherited. A Patrol does not "inherit" how it syncs; the sync engine consumes anything `IOfflineCapturable`.
- Avoid template-method architectures (abstract base workflow + per-type overrides) for cross-module processes; prefer a service operating over capability interfaces. The registries' existing shape — features *register into* shared mechanisms (Settings, Command/Action Bus, Duration Watchdog, Carrier types) rather than subclassing them — is this principle already applied at the platform level. Code should follow the same grain.

## The earn-your-existence test

A class, subtype table, or interface earns its existence when it adds at least one of: **fields, members, or genuinely distinct behavior.** Otherwise it's a name pretending to be a type — use an enum, a discriminator value, or registry metadata instead.

- This is the same test the requirements docs already apply to data (`extended_fields` vs. a concrete column; a Tenant-Defined Subtype vs. a developer-built extension). Apply it uniformly to code.
- **Known standing exception, deliberately made:** the named `EntityAssociation` TPT kinds, several of which currently carry no extra fields — `_DECISIONS.md` chose real named types over a bare string discriminator for type-safety and FK-integrity reasons. That decision stands at the *requirements* level; whether each field-less kind is physically its own table (TPT) or a discriminator row on the base (TPH) with a named C# type over it is a **technical-spec implementation choice**, and this doc explicitly permits per-hierarchy TPH mapping where a kind adds no columns. Requirements name the types; physics may vary.

## EF Core / .NET practical notes

- TPT with 4-level chains means multi-join reads and union-fanout polymorphic queries — a documented EF Core performance trap. Mitigations, in order of preference: (1) CQRS read models for hot query paths (already the platform's stated query-side posture); (2) TPH mapping for hierarchies whose levels add few or no columns (associations especially); (3) targeted denormalization, last. Measure before assuming, but do not design hot paths that require walking the full chain per row.
- If the base-framework decision lands on ABP (or similar), platform base classes (`Entity`, auditing, tenant filtering) should *extend or adapt* the framework's equivalents rather than duplicating them — one identity spine, not two. Framework choice is a pending technical-spec decision; this guidance is framework-agnostic.
- Registry-mirroring interfaces should be cheap to check at runtime (`entity is IMergeable` on developer types; registry lookup for everything else) — a single capability-resolution service should hide which of the two answered, so calling code never branches on "developer-built vs. tenant-defined."

## Quick reference

| Situation | Tool |
|---|---|
| A thing's identity/fields fit the domain taxonomy | Class inheritance (TPT-mapped), abstract ancestors |
| A behavior only some types across the taxonomy have | Interface + registry declaration (registry authoritative) |
| Process/workflow logic operating on many types | Composed, injected service over capability interfaces |
| A "type" distinguished only by name, no fields/behavior | Enum / discriminator / registry metadata — not a class |
| Tenant needs a new kind of thing | Tenant-Defined Subtype (registration, no class) — never a runtime type |
| Base class wants a virtual method "just in case" | Don't. Sealed by default; extension points are designed, not defaulted |
