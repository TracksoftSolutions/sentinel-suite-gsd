# Entity Registry Core

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Entity Registry Core is a **cross-cutting service layer** built on **Table-Per-Type (TPT) inheritance** — a single shared primary key runs through an entire type hierarchy, with each level (base and every extension) holding only the columns it adds, joined back to its parent by that same key. Two independent TPT hierarchies exist:

- **Entity** — the identity hierarchy. Every real-world thing the platform tracks is, at its root, an Entity: `entity_id` (PK), `tenant_id`, `entity_type`, `status`, audit fields. Five base types extend Entity directly — **Party** (`nc:EntityType`, Person-or-Organization; Party Registry), **Item** (`nc:ItemType`; Item Registry), **Location** (`nc:LocationType`; Location Registry), **Activity** (`nc:ActivityType`; Activity Registry), **Document** (`nc:DocumentType`; Document Registry) — and every other type in the platform (Person, Organization, Vehicle, Weapon, Incident, Call, Policy, and so on) extends one of those five, possibly several levels deep (e.g., Employee extends Person extends Party extends Entity). At every level, the row's primary key **is** `entity_id` — not a separate ID pointing back via a foreign key — so there is exactly one identity per real-world thing, not a redundant pair of IDs.
- **EntityAssociation** — the relationship hierarchy, structured identically. `association_id` (PK), `tenant_id`, `entity_id_a` (FK → Entity), `entity_id_b` (FK → Entity), `role`, `added_by`, `added_at`, `status`. Concrete relationship kinds — `ConveyanceOwnerAssociation`, `CustodyAssociation`, `EmergencyContactAssociation`, `HierarchyAssociation`, `ActivityParticipantAssociation`, `ActivityAttachmentAssociation`, `ActivityLocationAssociation`, `DocumentAuthorAssociation`, `FacilityManagerAssociation`, and more as needed — are real TPT subtypes of EntityAssociation, not a generic table distinguished only by a string column. Because both `entity_id_a` and `entity_id_b` reference the single `Entity` table (not the N different concrete typed tables directly), the constraint "points to a valid registered thing" is real and database-enforceable regardless of which concrete type is on either side — literally anything can be associated with anything through this one hierarchy.

**Naming note, since NIEM itself has a concept literally called "Entity":** NIEM Core's `nc:EntityType` specifically means "a Person or an Organization." To avoid colliding with "Entity Registry Core" (this platform's own name for the whole service layer, which covers all five base types, not just Person-or-Organization), the NIEM `nc:EntityType` concept is called **Party** throughout this platform.

**Why TPT, and why the earlier non-TPT design was wrong to reject full genericity outright:** the prior draft of this doc kept `Entity` as a thin cross-reference row with a `concrete_record_ref` *pointer* to a separately-keyed concrete table (e.g., Person's own `extension_id`). That works, but it's needless indirection and a redundant identifier — TPT's shared-PK approach means Person's row *is* identified by `entity_id`, full stop, with no second ID to reconcile. It also means the same inheritance mechanism now applies uniformly to *both* entities and relationships, rather than entities using one pattern (extension tables) and associations using a different one (a discriminator string plus optional "extension" tables) — one pattern, understood once, applied everywhere.

This still isn't the EAV/generic-blob anti-pattern: TPT's base tables (`Entity`, `EntityAssociation`) hold only genuinely uniform data (an ID, a type, audit fields; two references, a role, audit fields) — never the heterogeneous field content of a Person vs. an Activity vs. a Document, which stays in concrete, independently-indexable tables at their own level of the hierarchy. Genericity is applied exactly where heterogeneity is the actual feature being modeled (identity, and arbitrary relatability between types) and nowhere else. This is also specifically the right tradeoff *for this platform*: Sentinel Suite's entity types are added by developers building new modules (a new Registry doc, a new TPT level), not defined by tenants at runtime — a platform whose core value proposition was "let users define arbitrary custom object types" (e.g., Salesforce, ServiceNow) would reasonably weigh this differently and lean more generic.

Base types and field naming are modeled to align with **NIEM Core** (`nc:EntityType`/`nc:PersonType`/`nc:OrganizationType`/`nc:LocationType`/`nc:ItemType`/`nc:ActivityType`/`nc:DocumentType`) as an internal modeling discipline — not full NIEM XML/IEPD schema conformance at launch. This keeps a future NIEM-conformant exchange (CJIS-adjacent law enforcement BOLO sharing, DOE national lab system interoperability, CAD-to-CAD mutual aid dispatch, many of which use NIEM-based standards) a mapping exercise rather than a data model rewrite. "Entity Registry Core," "Entity," and "EntityAssociation" as platform-level TPT roots are not themselves NIEM concepts.

## Actors & Roles

- **Every platform feature/module** — registers entity types, extension types, and association types against this core registry rather than implementing independent identity or relationship management.
- **Data Steward / Records Admin** — reviews and resolves flagged potential duplicates, performs merges and un-merges.
- **Platform Super Admin** — manages the type registry itself (which entity types, extension types, and association types exist platform-wide).
- **Any platform user with appropriate permission** — creates/updates entity records and associations through the owning feature.

## User Stories

- As a **Records Admin**, I want the system to flag two Person records with matching name and phone as likely duplicates, so I can review and decide whether to merge them rather than have it happen automatically and risk misattributing someone's history.
- As a **Records Admin**, I want a confirmed merge to redirect every reference — including every association row on either side — to the surviving record, so nothing breaks and nothing is silently lost.
- As a **Records Admin**, I want to be able to reverse a merge I later discover was wrong, with a full audit trail of what changed.
- As a **Personnel module developer**, I want to register "Employee" as an extension of Person (itself extending Party extending Entity), sharing one primary key through the whole chain, so I get global ID, dedup, and relationship/history tracking for free.
- As a **Fleet Coordinator**, I want a vehicle's ownership recorded as a `ConveyanceOwnerAssociation` rather than a plain field, so ownership changes carry their own audit-of-when-recorded and a natural history via soft-removal, without me having to build that separately.
- As a **Database Architect**, I want every concrete type — Person, Item, Activity, ConveyanceOwnerAssociation, CustodyAssociation — to have its own real table with its own indexes, joined to its ancestor by a shared key, not a generic blob or a discriminator-string table.
- As a **Platform Architect**, I want our entity model to already look like NIEM Core's taxonomy, so a future NIEM-conformant exchange is a mapping exercise, not a re-architecture.
- As a **Supervisor viewing a person's profile**, I want to see every call, incident, citation, accident, and asset they were ever linked to in one timeline, sourced from one association hierarchy, without per-module custom integration code.

## Functional Requirements

### Entity — the identity hierarchy
1. **Entity** is the TPT root: `entity_id` (PK), `tenant_id`, `entity_type` (a discriminator, kept as a practical query aid alongside the "which subtype table has a matching row" TPT mechanism itself, to avoid a fan-out join across every known subtype just to determine type), `status`, `created_at`, `created_by`.
2. A feature registers a base **Entity Type** (Party, Item, Location, Activity, Document) or an **Extension Type** of one — declaring the type, its owning feature, its lifecycle model, and (for extensions) its parent in the chain. Chains nest arbitrarily deep (Employee → Person → Party → Entity).
3. At every level of the chain, the row's primary key **is** `entity_id`, shared with — and foreign-keyed to — the level directly above it. There is one identity per real-world thing, never a second ID layered on top.
4. Entity Type field naming and structure follow NIEM Core taxonomy and conventions where a corresponding NIEM concept exists, as an internal modeling discipline (not full IEPD/XML schema conformance at launch).
5. A single base entity (e.g., one Person) can carry multiple simultaneous extensions of different kinds where the domain allows it (e.g., a Person who is both an Employee and, via a BOLO Flag, a flagged subject), each independently statused — extensions are additive, not mutually exclusive, except where a chain is strictly linear (Employee only ever extends Person, not also Visitor, on the same Person record simultaneously, since those represent different relationships to the same underlying person and would need their own separate records if genuinely concurrent — handled per Person Registry's own rules).

### Global Entity IDs & tenant isolation
6. Every Entity receives a globally unique ID **within its owning tenant** — tenant-scoped, respecting the isolation model established in Authentication & Authorization; no platform-wide ID shared across tenants by default.
7. This `entity_id` is the linking key referenced across every other module — scheduling, dispatch, incidents, access logs, and beyond.
8. Where the same physical person legitimately has entity records in more than one tenant (e.g., a subcontracting agency's guard working sites across multiple client tenants), those records remain **separate, tenant-scoped entities** — never merged or structurally shared. A separate, tightly controlled cross-tenant identity bridge (owned by Authentication & Authorization) links them at the account/identity level, not at the Entity Registry data level.
9. Deduplication and matching operate strictly within a single tenant's own data — never across the tenant isolation boundary.

### Deduplication, merge & reversal
10. The registry runs algorithmic matching (on fields declared per Entity Type as match signals) to surface **potential duplicate** Entity pairs.
11. No match, regardless of confidence score, is ever auto-merged — every potential duplicate is flagged and routed to a Records Admin for explicit human review (confirm merge, reject as not-a-duplicate, or defer).
12. A rejected potential-duplicate pairing is recorded so it doesn't repeatedly re-flag on subsequent matching passes.
13. Confirming a merge designates one Entity as the **canonical survivor**; every reference to the merged-away Entity across every module — including every `EntityAssociation` row where it appears as `entity_id_a` or `entity_id_b` — is redirected to the survivor.
14. The merged-away Entity becomes a **tombstone** — inactive, retained (not deleted), pointing to the survivor.
15. A completed merge can be **reversed** by an authorized Records Admin, restoring the tombstoned Entity to active status and reverting redirected references, with the reversal itself fully audit-logged alongside the original merge.

### BOLO Flag (generic, cross-entity-type, unary)
16. Any Entity — regardless of base type or extension depth — can carry a **BOLO Flag**: a single generic, governed mechanism rather than each type reimplementing its own. This is deliberately *not* modeled as an EntityAssociation, since it's an annotation on one Entity, not a relationship between two.
17. Creating or activating a BOLO Flag requires an elevated permission, a recorded justification, and a mandatory expiration date, after which it automatically lapses.
18. Both creating and clearing a BOLO Flag require step-up authentication (per Authentication & Authorization).
19. Every BOLO Flag creation, clearance, and expiration is an audit-tier event.
20. A BOLO Flag may reference a supporting Document (e.g., a trespass notice, a stolen-vehicle report) via a direct nullable field on the flag record itself — this is intrinsic to the flag's own record, not a separately-tracked relationship needing its own association row.

### EntityAssociation — the relationship hierarchy
21. **EntityAssociation** is the TPT root for relationships: `association_id` (PK), `tenant_id`, `entity_id_a` (FK → Entity), `entity_id_b` (FK → Entity), `association_type` (discriminator, same rationale as Entity's), `role`, `added_by`, `added_at`, `status` (active, removed).
22. A concrete relationship kind (e.g., `ConveyanceOwnerAssociation`) is a TPT subtype of EntityAssociation: its own table, its own primary key shared with and FK'd to the base `association_id`, holding only whatever extra fields that kind needs beyond role/audit (many kinds need none at all — the base shape is sufficient, and the subtype table exists purely to give the relationship a real, named type rather than relying on the `association_type` string alone).
23. `status` transitions `active` → `removed` (soft-removal, never hard-deleted) so that "this was once associated" survives for audit purposes and, for relationship kinds representing a single current value (ownership, custody, primary location), the sequence of active/removed rows over time *is* that value's history — no separate history mechanism needed.
24. A merge redirects `entity_id_a`/`entity_id_b` on every affected EntityAssociation row (at any level of its own TPT chain) to the survivor, exactly as it redirects any other reference.
25. Which concrete association types exist is determined by the base type docs and consuming modules that need them — this doc establishes the shared TPT pattern and governance (audit-tier logging of every add/remove), not an exhaustive catalog.

### Display labels (universal — every Entity, at every TPT level)
26. Every registered Entity Type and Extension Type — at every level of the TPT chain, from the most generic (Party) to the most specific (Employee) — must declare a **display label strategy**, so any generic reference to that entity (a search result, a timeline entry, a CLI context chip, an audit-log line naming what was affected) can render a human-readable label without caller-side type-switching. This is a platform-wide requirement owned here, not something each consuming UI feature reinvents per type.
27. Two strategies satisfy the requirement: a **template** (a simple field readout or field-composition — e.g., a Person's `person_name.full_name`, an Organization's `organization_name`, a Location's `location_name`, a Document's `document_title`, a Vehicle's `plate_jurisdiction: make model`) for types whose natural fields already say what they are; or a **computed label** (custom summary-generation logic, not a fixed template) for types whose label needs real synthesis across several fields — most notably Activity extensions, where a useful label ("Incident #4521: Theft, Building A, concluded") draws on type, display number, category, an associated location's own label, and status, not a single field.
28. This requirement applies only within the **Entity** hierarchy. It explicitly does **not** apply to `EntityAssociation` rows (of any concrete kind), `PotentialDuplicate`, `MergeRecord`, `BOLOFlag`, or audit-log entries — these are metadata *about* entities and the system, not entities themselves, and have no display label of their own. When one of these is rendered in a UI (e.g., an audit-log line reading "Incident #4521 merged into Incident #4519"), it does so by looking up and using the display labels of the entities it references, not by having a label of its own.

## Data Model / Fields

**Entity** (TPT root)
- entity_id (PK, tenant-scoped, globally unique within tenant), tenant_id, entity_type
- status (active, tombstoned)
- created_at, created_by

**[Base Type / Extension Type]** (TPT level, e.g., Party, Person, Employee — one table per level, per that type's own Registry doc)
- entity_id (PK, FK → parent level's entity_id)
- only the fields that level adds

**Entity Type Registration** (registered by a feature — catalog/metadata only, not a field store)
- entity_type_id, owning_feature, name, base_type (party, item, location, activity, document)
- is_extension_of (nullable — parent type in the chain)
- has_independent_lifecycle (bool, extension types only)
- concrete_schema_ref (pointer to where this type's fields are defined)
- match_signal_fields[] (which fields the dedup engine matches on)
- display_label_strategy (template: a field-composition string; or computed: a reference to that type's custom summary-generation logic) — required for every registration, no default

**Potential Duplicate**
- pair_id, tenant_id, entity_id_a, entity_id_b
- match_signals[] (field, similarity_score), overall_confidence
- status (pending_review, confirmed_merge, rejected_not_duplicate)
- reviewed_by (nullable), reviewed_at (nullable)

**Merge Record**
- merge_id, tenant_id, survivor_entity_id, tombstoned_entity_id
- reference_redirect_log[] (module, reference_type, reference_id)
- merged_by, merged_at
- reversed (bool), reversed_by (nullable), reversed_at (nullable)

**Cross-Tenant Identity Bridge** (owned by Authentication & Authorization, referenced here)
- bridge_id, account_id, linked_entity_refs[] (tenant_id, entity_id)

**BOLO Flag** (generic, any Entity — unary, not an association)
- flag_id, tenant_id, entity_id (FK → Entity)
- justification, created_by, created_at
- expires_at, status (active, expired, cleared)
- cleared_by (nullable), cleared_at (nullable), creation_step_up_verified (bool), clearance_step_up_verified (bool)
- supporting_document_ref (nullable, FK → Document)

**EntityAssociation** (TPT root)
- association_id (PK), tenant_id
- entity_id_a (FK → Entity), entity_id_b (FK → Entity)
- association_type (discriminator, e.g., vehicle_ownership, custody, emergency_contact, hierarchy, activity_participant, activity_attachment, activity_location, document_authorship, facility_manager)
- role, added_by, added_at, status (active, removed)

**[Concrete Association Type]** (TPT level, e.g., ConveyanceOwnerAssociation — one table per kind, defined in whichever base type doc needs it)
- association_id (PK, FK → EntityAssociation.association_id)
- only the extra fields that kind adds, if any

## States & Transitions

**Entity:** `active` → `tombstoned` (merged away, points to survivor) → `active` (merge reversed).

**Extension levels:** independent per level's own lifecycle where declared (e.g., an Employee level: `active` → `inactive`/`terminated`) — not tied to an ancestor level's own active/tombstoned status except that tombstoning the root Entity renders every level beneath it inactive for practical purposes (data survives, redirected to the survivor's chain where applicable).

**Potential Duplicate:** `pending_review` → `confirmed_merge` (triggers Merge Record) | `rejected_not_duplicate` (suppresses re-flagging).

**BOLO Flag:** `active` (step-up verified at creation) → `cleared` (step-up verified) | `expired` (automatic, at expires_at).

**EntityAssociation (any concrete kind):** `active` (added) → `removed` (soft-removed). For single-current-value kinds (ownership, custody, primary location), the currently-active row is "the current value"; removed rows are its history.

## Integrations

- **Party Registry, Item Registry, Location Registry, Activity Registry, Document Registry**: the five base-type consumers, each owning their own concrete TPT levels beneath Entity.
- **Person Registry, Organization Registry**: the two Party extensions.
- **Vehicle/Conveyance Registry**: consumer of `ConveyanceOwnerAssociation`; Item Registry's own custody mechanism is `CustodyAssociation`.
- **Dispatch/CAD, Security Operations, Physical Security Integration Gateway, Safety Management, and every future module producing a trackable occurrence**: future registrants of Activity extensions.
- **Authentication & Authorization**: owns the cross-tenant identity bridge and the step-up authentication mechanism BOLO Flags require.
- **Notifications Engine**: BOLO Flag lifecycle notifications.
- **Entity Relationships & History**: builds the cross-module timeline from the Entity and EntityAssociation hierarchies directly, rendering each entry via that entity's registered display label.
- **Command Palette, CLI-Style Input**: render search results and context chips via each referenced entity's registered display label, with no per-surface type-switching logic.
- **Structured Logging & Audit Trails**: entity creation, extension/level changes, association add/remove, merge, and merge-reversal are all audit-tier events.
- **Every module that references a person, organization, vehicle, location, activity, or document**: consumes `entity_id` as the stable cross-reference key.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Records Admin | Standard User |
|---|---|---|---|---|
| Register a new Entity/Extension/Association Type (developer, via feature build) | ✅ | ❌ | ❌ | ❌ |
| Create/update entity records (via owning feature) | ✅ | ✅ | ✅ | ✅ (per own RBAC/ABAC, per feature) |
| Add/remove an EntityAssociation (via owning feature) | ✅ | ✅ | ❌ (unless also granted) | ✅ (per own RBAC/ABAC, per feature) |
| Review potential duplicates | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ |
| Confirm/reverse a merge | ✅ | ✅ (own tenant) | ✅ (own scope, if granted) | ❌ |
| Create/clear a BOLO Flag (any entity type) | ✅ | ✅ | Supervisor+ only, step-up required | ❌ |

## Non-Functional / Constraints

- Deduplication matching must run without ever exposing data across the tenant isolation boundary.
- Merge/reversal must be transactionally safe against reference redirection — a partially-completed merge (some references redirected, others not) must not be possible.
- NIEM Core alignment is a modeling-time discipline verified during technical spec review, not a runtime validation requirement at launch.
- `entity_id` must be stable for the lifetime of the record, including through a merge (the survivor's ID never changes).
- BOLO Flag creation/clearance auditability and step-up enforcement must be airtight regardless of entity type.
- The EntityAssociation table (and its TPT subtypes) must be indexed for both directions of lookup (`entity_id_a`, `entity_id_b`) plus `association_type`, since "everything associated with entity X" is a common query regardless of which side X was recorded on.
- TPT joins (Entity → base type → extension → ... ) must be efficient enough for common-path reads (e.g., loading a full Employee record) not to require an unreasonable join depth in practice — a technical-spec-level concern (e.g., materialized/denormalized read models per the platform's CQRS query side, per Event & Command Bus Architecture) if join depth becomes a real performance issue.
- Resolving a display label must be cheap enough to compute in bulk (e.g., rendering 50 timeline entries or search results at once) — template-strategy labels should require no extra query beyond the entity's own already-loaded fields; computed-strategy labels (Activity extensions) should be cacheable/precomputable if generation proves expensive, a technical-spec-level concern.

## Acceptance Criteria

- [ ] Registering Person as an extension of Party (itself extending Entity) and Employee as an extension of Person correctly shares one `entity_id` through the whole chain — no second ID is generated at any level.
- [ ] A single Person Entity can carry an active Employee level and an active BOLO Flag simultaneously.
- [ ] Two Person records with matching name and phone are flagged as a potential duplicate and never auto-merge.
- [ ] Confirming a merge redirects every reference, including every EntityAssociation row on either side, to the survivor; the merged-away Entity becomes a tombstone.
- [ ] Reversing a completed merge restores the tombstoned Entity and reverts redirected references, fully audit-visible.
- [ ] Entity records and matching for Tenant A are never visible to or matched against Tenant B's data.
- [ ] Creating a BOLO Flag on a Vehicle and on a Person both go through the identical governance mechanism.
- [ ] A BOLO Flag past its expiration date automatically shows as expired.
- [ ] `ConveyanceOwnerAssociation` correctly links a Vehicle Entity to a Party Entity with real foreign-key integrity to both, sharing EntityAssociation's base shape plus no extra fields.
- [ ] `EmploymentAssociation` (or similarly extra-fields-bearing kind) correctly stores its extra fields (e.g., start/end dates) at its own TPT level without altering the base EntityAssociation table's shape.
- [ ] Setting a new `CustodyAssociation` active row for an Item automatically leaves the prior active row as `removed`, giving a correct custody history with no separate history table.
- [ ] A query for "everything associated with Entity X" correctly returns matches whether X was recorded as `entity_id_a` or `entity_id_b`.
- [ ] Every registered Entity Type and Extension Type (Party, Person, Organization, Item, Vehicle, Location, Activity, and every Activity extension, Document) resolves a non-empty display label via its declared strategy.
- [ ] A template-strategy type's label updates automatically when its underlying field changes (e.g., a Person's label reflects a corrected name immediately, no separate label field to keep in sync).
- [ ] A computed-strategy type (an Activity extension) produces a synthesized label drawing on more than one field, verified to differ meaningfully from a raw single-field readout.
- [ ] An audit-log line, a `MergeRecord`, and an `EntityAssociation` row are confirmed to have no display label of their own — their rendering uses the labels of the entities they reference.
- [ ] Attempting to register a new Entity Type without a `display_label_strategy` is rejected.

## Open Questions

- Exact match-signal algorithm and confidence-scoring approach — to be defined during technical spec.
- Whether additional base entity types beyond Party/Item/Location/Activity/Document are ever needed — none currently anticipated.
- Full catalog of concrete EntityAssociation subtypes is built out incrementally as each base type doc and consuming module needs a new relationship kind — not exhaustively enumerated here.
- Whether `entity_id_a`/`entity_id_b` need a defined ordering convention per association_type (e.g., is `entity_id_a` always the "owning" side) — to be settled during technical spec, likely per-type convention.
- TPT join-depth performance strategy (denormalized read models vs. deep joins at query time) — a technical-spec-level decision.
- Exact template syntax for `display_label_strategy` (e.g., a simple field-path interpolation language vs. something richer) — a technical-spec-level decision.
- Whether computed-strategy labels (Activity extensions) are generated on-demand at render time or precomputed/cached and invalidated on relevant field changes — a technical-spec-level decision, likely informed by whichever proves necessary for the acceptable-latency non-functional requirement above.
- Exact list of Activity extensions to register at launch vs. incrementally — deferred to each producing module's own doc.
- Full mapping of platform field names to their NIEM Core equivalents — a technical-spec-level exercise.
- Whether/when to invest in actual NIEM IEPD conformance remains deferred until a concrete external exchange requirement is identified.
