# Tenant-Defined Types & Custom Fields

**Module:** 0 Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Generalizes a pattern first drafted locally in Courtesy Patrol (a bespoke `category_details` JSONB field) and then embedded directly into Entity Registry Core (`extended_fields` on `Entity`/`EntityAssociation`) into its own first-class Platform Core service: a **Custom Field Definition registry** and validation service that any "carrier" record type — not just Entity Registry Core's two TPT roots — can opt into. This is the single place a Tenant Admin defines self-service custom fields, and, further generalized here, **tenant-defined subtypes** of existing concrete types — without a platform release.

Two distinct capabilities, deliberately kept separate in scope and complexity:

1. **Custom Fields on an existing type** — a Tenant Admin adds a bounded, schema-validated field to an existing carrier (Person, Vehicle, a Domain Events Rule, a CLI Alias...). The well-trodden, low-risk case — reuses the `extended_fields` JSONB column already living on Entity Registry Core's TPT roots (and now added to two illustrative non-entity carriers, Domain Events Rule and CLI Alias, to prove the mechanism generalizes), governed by a Custom Field Definition this feature owns.
2. **Tenant-Defined Subtypes** *(corrected from an earlier draft that let a tenant register free-floating first-class types with no concrete table — that skewed the original intent; see `_DECISIONS.md`)* — a Tenant Admin defines a new **kind of an existing concrete, developer-built type** that the platform didn't foresee: a new kind of Activity ("Wildlife Encounter"), a new kind of Item, a new kind of Document. A Tenant-Defined Subtype is **always anchored on a concrete type** — its records ARE real records of that anchor (real rows in the anchor's TPT chain), carrying the subtype designation plus any custom fields via `extended_fields`, validated against tenant-authored Custom Field Definitions scoped to the subtype. It therefore inherits the anchor's entire platform treatment for free — lifecycle, display-label mechanics, `is_mergeable` posture, offline write classes, timeline inclusion, queue participation — *because there is nothing new to treat*: a Wildlife Encounter simply is an Activity, everywhere Activities go. This is the same pattern the platform already uses ad hoc all over (Call Type Definitions, DAR Entry categories, Courtesy Patrol categories), generalized and given custom-field support. There is **no** free-floating tenant-defined type: nothing a tenant creates exists outside a concrete anchor, and this remains **not** a dynamic-DDL/schema-migration system. The cost stays explicit and always-disclosed: subtype custom fields are never concrete-column-indexed (the `extended_fields` tradeoff), and the **graduation path** (Open Questions) is now cleaner — promoting a popular subtype means the platform team builds a real concrete extension of the same anchor and migrates the subtype's records into it.

**Reserved-key namespace protects forward compatibility**: every custom field key must use a reserved prefix (`custom_`), so a developer adding a real concrete column to a type in a future platform release can never collide with a tenant's already-defined custom field of the same name.

**Self-service, but capped, not gatekept.** A Tenant Admin can define custom fields and tenant-defined types without Platform Super Admin approval — that's the point — but the platform enforces sane per-tenant caps (max custom fields per carrier, max tenant-defined types) and gives Platform Super Admin standing audit visibility (definitions, not tenant data) plus the ability to disable a specific problematic definition without deleting the underlying data.

**Carrier-agnostic by design, not carrier-exhaustive at launch.** Any feature's record type can register as an eligible carrier by declaring a `carrier_key` (e.g., `entity_type:Person`, `association_type:ConveyanceOwnerAssociation`, `domain_events_rule`, `cli_alias`) against this registry. A carrier can also be **subdivided finer than a whole registered type** by the owning feature's own convention — Courtesy Patrol's Category Definition is the motivating example, selecting a schema per tenant-configurable category (`courtesy_patrol:category:jump_start`) rather than per whole Activity extension type. This doc owns the generic mechanism; it doesn't own or enumerate every possible carrier.

## Actors & Roles

- **Tenant Admin** — defines Custom Field Definitions and Tenant-Defined Subtypes for their own tenant, within platform-enforced caps.
- **Platform Super Admin** — sets/adjusts per-tenant caps (potentially per tenant tier), has audit visibility across all tenants' definitions (not their data), can disable a specific definition.
- **Every platform feature/module** — may register its own record type as an eligible carrier; Entity Registry Core's Entity/EntityAssociation are the default/primary carriers, wired up first.
- **Records Admin** — no distinct role here; a subtype's records participate in Entity Registry Core's ordinary dedup/merge review exactly as their anchor type does.

## User Stories

- As a **Tenant Admin**, I want to add a custom field to our Vehicle records for our own internal tracking need, without waiting on a platform release.
- As a **Tenant Admin**, I want to define a new kind of Activity our operation needs — say, a Wildlife Encounter log — with a handful of fields I choose, without engineering involvement, and have it behave like any other Activity everywhere (DAR pickup, timeline, queue).
- As a **Platform Super Admin**, I want visibility into which tenants have defined how many custom fields and subtypes, so I can catch runaway sprawl before it becomes a performance problem.
- As a **Platform Super Admin**, I want to disable one tenant's problematic custom field definition without touching their underlying data or affecting any other tenant.
- As a **Domain Events feature developer**, I want Rule records to support tenant-added custom metadata (e.g., an "owner team" tag) without building my own bespoke extensibility scheme.
- As a **Platform Architect**, I want a popular tenant-defined subtype to be promotable into a real developer-built concrete extension of the same anchor in a future release, migrating its records rather than starting over.

## Functional Requirements

### Custom Field Definition
1. A **Custom Field Definition** declares: `carrier_key` (which registered type or feature-defined sub-carrier it applies to), `field_key` (reserved-namespace-prefixed, `custom_*`), `label`, `field_type` (text, number, boolean, select — with `options[]` for select), `required` (bool), `tenant_id`.
2. Every custom field key is validated at definition time to ensure it uses the reserved `custom_` prefix, guaranteeing no future platform-added concrete column can ever collide with it.
3. A carrier's applicable Custom Field Definitions are resolved by `carrier_key`; a record with no applicable definitions simply has an empty/unused `extended_fields`.
4. This feature owns the **validation service** consuming any carrier's `extended_fields` write and the Custom Field Definitions matching its `carrier_key`: unrecognized keys rejected, missing `required` keys rejected, missing non-required keys always allowed through (never a hard block on saving the record itself).

### Carrier registration
5. Any feature's record type registers as an eligible carrier by declaring a `carrier_key` here — a **Carrier Type Registration**, catalog-only, no field data of its own. Entity Registry Core's Entity Type Registration and Association Type Registration are the default/primary carriers, auto-eligible (every registered Entity/Extension/Association Type is automatically a valid `carrier_key`, no separate registration step needed for that specific case, since Entity Registry Core already carries `extended_fields` on its TPT roots).
6. A feature may **subdivide** its own carrier finer than "the whole type," using its own naming convention for `carrier_key` (e.g., Courtesy Patrol's `courtesy_patrol:category:<category_id>`) — this doc doesn't enforce a single granularity, only that whatever key a Custom Field Definition targets resolves unambiguously at validation time.
7. Two non-entity carriers are wired up now, to prove the mechanism generalizes beyond Master Records: Domain Events **Rule** and CLI-Style Input's **Alias** both gain an `extended_fields` column and register as carriers — retrofits to [domain-events.md](domain-events.md) and [cli-style-input.md](cli-style-input.md).

### Tenant-Defined Subtypes (always anchored on a concrete type)
8. A Tenant Admin can define a **Tenant-Defined Subtype**: a new tenant-named kind of an existing concrete, developer-built type. The subtype declares its **anchor** — any concrete type in the Entity hierarchy (a base type like Activity or Document, or a concrete extension like Item's Vehicle) or, for relationship kinds, the base EntityAssociation (a tenant-defined association kind is a base association row with a tenant-defined `association_type` value). A subtype's records are **real records of the anchor** — real rows in the anchor's TPT chain, with the subtype designation carried on the Entity root (`tenant_subtype_ref`, nullable, concrete and filterable) — plus custom fields in `extended_fields`, validated against Custom Field Definitions scoped to the subtype's `carrier_key`. **No developer-built table is created, and no type exists outside its anchor.**
9. A Tenant-Defined Subtype inherits its anchor's entire platform treatment automatically — identity/dedup/merge posture (including the anchor's `is_mergeable`), BOLO Flag eligibility, offline write classes, Entity Relationships & History timeline inclusion, Active Incident Queue participation — because its records *are* anchor records; there is no special-casing anywhere else in the platform and nothing new to register against those mechanisms.
10. Display labels come from the **anchor's existing `display_label_strategy`**, with the subtype's name available as a token to templates and computed logic (so a Wildlife Encounter Activity labels like any Activity, reading "Wildlife Encounter #217: …" rather than a generic "Activity #217"). A tenant defining a subtype never authors label mechanics.
11. Dedup matching uses the anchor's `match_signal_fields[]` by default; a subtype may **additionally** declare match signals referencing its own `extended_fields` keys, at reduced matching-index performance compared to a concrete column (documented tradeoff, not a blocker). A subtype can never widen mergeability beyond its anchor (`is_mergeable = false` anchors stay excluded).
12. Tenant-Defined Subtypes are explicitly **not** a dynamic-DDL or schema-migration system — no new physical table is ever created at tenant request. This scoping decision is what keeps this feature buildable without runtime-schema risk; full custom-object systems (free-floating tenant-defined types, arbitrary indexing, relational integrity between tenant-defined structures, tenant-defined permissions models) remain out of scope and are flagged as a distinct, larger future idea, not solved here.

### Caps & governance
13. Per-tenant caps — max Custom Field Definitions per carrier, max Tenant-Defined Subtypes per tenant — are Settings & Preferences-registered values, defaulting to a platform-wide sane default and adjustable per tenant/tenant-tier by Platform Super Admin.
14. Platform Super Admin has standing read access to every tenant's Custom Field Definitions and Tenant-Defined Subtypes (definitions/schema only, never the underlying tenant data those definitions govern) for audit/sprawl visibility.
15. Platform Super Admin can **disable** a specific Custom Field Definition or Tenant-Defined Subtype (e.g., a performance concern) without deleting any tenant data — disabling stops new writes to that field/type but preserves what's already stored, reversible.

## Data Model / Fields

**Carrier Type Registration** (catalog/metadata only)
- carrier_key (PK, e.g. `entity_type:Person`, `association_type:ConveyanceOwnerAssociation`, `domain_events_rule`, `cli_alias`, or a feature-defined finer sub-key like `courtesy_patrol:category:jump_start`)
- owning_feature, description
- storage_column_ref (which physical `extended_fields`-bearing table/column this carrier's records live on)

**Custom Field Definition**
- field_definition_id, tenant_id, carrier_key (FK → Carrier Type Registration, or a feature-defined sub-key)
- field_key (`custom_*`, reserved namespace), label, field_type (text, number, boolean, select), options[] (select only)
- required (bool)
- status (active, disabled)
- created_by, created_at

**Tenant-Defined Subtype**
- subtype_id (PK), tenant_id (strictly scoped to the defining tenant — never platform-wide)
- anchor_ref (required — the concrete developer-built type this subtype specializes: an Entity Type Registration entry, or EntityAssociation for a tenant-defined association kind)
- name, description
- additional_match_signal_keys[] (nullable — optional `extended_fields` keys added to the anchor's own match signals, #11)
- status (active, disabled)
- created_by, created_at
- *(Records of the subtype carry `tenant_subtype_ref` → this row on their Entity root; their `entity_type` remains the anchor's — the subtype is a designation within the anchor, not a new discriminator value.)*

**Custom Field Cap** (Settings & Preferences registration)
- cap_id, tenant_id (nullable = platform default), carrier_key (nullable = applies generally) or scope = "tenant_defined_subtypes"
- max_count

## States & Transitions

**Custom Field Definition:** `active` → `disabled` (Platform Super Admin override, or Tenant Admin retiring their own field) — disabling never deletes already-stored `extended_fields` data.

**Tenant-Defined Subtype:** `active` → `disabled` (same override semantics as a Custom Field Definition — new records of the subtype blocked, existing records untouched and still readable as ordinary anchor records).

## Integrations

- **Entity Registry Core**: owns the physical `extended_fields` JSONB storage on `Entity`/`EntityAssociation`, the `tenant_subtype_ref` column on the Entity root, and the type registrations subtypes anchor to; this feature owns the Custom Field Definition/validation/Carrier registry and the Tenant-Defined Subtype catalog layered on top — retrofitted, see that doc's Extended Fields section.
- **Courtesy Patrol**: motivating first use case — its Category Definition is a feature-defined finer-grained carrier (`courtesy_patrol:category:<id>`) rather than a whole registered Entity Type.
- **Domain Events, CLI-Style Input**: first two non-entity carriers, retrofitted with an `extended_fields` column on Rule and Alias respectively, proving the mechanism generalizes beyond Master Records.
- **Settings & Preferences**: owns the tenant-tier cap values (#13) via its existing hierarchical engine, rather than this feature building its own override logic.
- **Structured Logging & Audit Trails**: every Custom Field Definition and Tenant-Defined Type creation, edit, and disable is an audit-tier event.
- **Entity Relationships & History, Command Palette, CLI-Style Input**: consume a subtype's records exactly like any other record of its anchor type, via the anchor's display label mechanics — no special-casing.

## Permissions

| Action | Tenant Admin | Platform Super Admin |
|---|---|---|
| Define/edit a Custom Field Definition (own tenant, within cap) | ✅ | ✅ |
| Define/edit a Tenant-Defined Subtype (own tenant, within cap) | ✅ | ✅ |
| View custom field/subtype definitions across all tenants | ❌ | ✅ |
| Adjust per-tenant/tenant-tier caps | ❌ | ✅ |
| Disable a specific definition | ✅ (own tenant's own definitions) | ✅ (any tenant) |
| Register a new carrier type (developer, via feature build) | ❌ | ✅ |

## Non-Functional / Constraints

- `extended_fields` content, on any carrier, is never guaranteed indexed/queryable with concrete-column performance — a platform-wide, always-disclosed tradeoff of the whole mechanism, not something each consuming feature re-derives independently.
- Reserved-key (`custom_*`) enforcement must be airtight — a definition attempt using an unprefixed or already-reserved key is rejected outright, not just discouraged.
- Per-tenant caps must be enforced atomically against concurrent definition creation (no race allowing a tenant to exceed its cap).
- Disabling a definition must never be destructive — underlying `extended_fields` data for already-created records is preserved, only new/edited writes against a disabled definition are blocked.
- A Tenant-Defined Subtype's dynamic form rendering (the anchor's concrete fields plus custom fields known only at runtime from Custom Field Definitions) is a genuine technical-spec-level UI challenge, flagged here as a real cost of this capability, not hand-waved.
- WCAG 2.1 / Section 508 accessible custom-field/type definition and dynamic-form-rendering flows, day one.

## Acceptance Criteria

- [ ] A Tenant Admin can define a `custom_preferred_shift` field on Person and have it validated on write without a platform deploy.
- [ ] Attempting to define a custom field with a key not prefixed `custom_` is rejected.
- [ ] A Tenant Admin defining custom fields up to their tenant's cap succeeds; the next attempt beyond the cap is rejected with a clear reason.
- [ ] A Tenant Admin can define a Tenant-Defined Subtype anchored on Activity (e.g., "Wildlife Encounter") with three custom fields, then create records of it that appear in DAR filters, the Interaction Timeline, and the Active Incident Queue exactly as ordinary Activities do — labeled with the subtype name via the anchor's label mechanics, filterable by `tenant_subtype_ref`, and dedup/merging per the anchor's own posture.
- [ ] A Domain Events Rule and a CLI Alias can each be given a tenant-defined custom field, proving the mechanism works on a non-entity carrier.
- [ ] Platform Super Admin can view (but not silently alter) every tenant's Custom Field Definitions and Tenant-Defined Types from one audit view.
- [ ] Platform Super Admin disabling a specific Custom Field Definition blocks new writes to it while leaving previously-stored data on existing records intact.
- [ ] Courtesy Patrol's per-category schema selection continues to work unchanged, now sourced from this feature's Custom Field Definition registry instead of a bespoke local mechanism.

## Open Questions

- Exact default cap values (max custom fields per carrier, max tenant-defined subtypes per tenant) — pending real-usage-informed tuning, likely starting conservative.
- Exact **graduation path** mechanics: how a popular Tenant-Defined Subtype or Custom Field Definition gets promoted into a real developer-built concrete extension/column of the same anchor in a future platform release, and how existing `extended_fields` data migrates into the new concrete storage without downtime or data loss — simpler than the pre-correction free-floating-type version (the anchor rows already exist; migration is subtype-designation → concrete extension rows), but still a real technical-spec-level undertaking, not solved here.
- Whether Tenant-Defined Subtypes ever need per-subtype permissions (who can create/view/edit records of a specific subtype) beyond inheriting the anchor type's existing RBAC/ABAC — plausibly yes for sensitive subtypes; likely an ABAC condition on `tenant_subtype_ref` rather than a new mechanism, not designed here.
- Whether `select` field_type's `options[]` need their own tenant-editable list-management UI (similar to DAR Entry's category pattern) or are simpler static arrays — leaning toward the latter for v1.
- Full dynamic-form-rendering technical approach for Tenant-Defined Subtypes (schema-driven UI generation over the anchor's form) — flagged as a real technical-spec challenge, not detailed here.
- Whether this feature's caps should scale by tenant tier/subscription level (a plausible commercial lever) — noted as a possibility, not a commitment.
