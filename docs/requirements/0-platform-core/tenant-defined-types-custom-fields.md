# Tenant-Defined Types & Custom Fields

**Module:** 0 Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Generalizes a pattern first drafted locally in Courtesy Patrol (a bespoke `category_details` JSONB field) and then embedded directly into Entity Registry Core (`extended_fields` on `Entity`/`EntityAssociation`) into its own first-class Platform Core service: a **Custom Field Definition registry** and validation service that any "carrier" record type — not just Entity Registry Core's two TPT roots — can opt into. This is the single place a Tenant Admin defines self-service custom fields, and, further generalized here, entire **tenant-defined types** — without a platform release.

Two distinct capabilities, deliberately kept separate in scope and complexity:

1. **Custom Fields on an existing type** — a Tenant Admin adds a bounded, schema-validated field to an existing carrier (Person, Vehicle, a Domain Events Rule, a CLI Alias...). The well-trodden, low-risk case — reuses the `extended_fields` JSONB column already living on Entity Registry Core's TPT roots (and now added to two illustrative non-entity carriers, Domain Events Rule and CLI Alias, to prove the mechanism generalizes), governed by a Custom Field Definition this feature owns.
2. **Tenant-Defined Types** — a Tenant Admin defines a wholly new Extension Type or Association Type (e.g., "Access Badge Log") without any developer involvement. This is **not** a dynamic-DDL/schema-migration system — that would be a different, much riskier feature. Instead, a tenant-defined type registers normally against Entity Registry Core's Entity Type Registration / Association Type Registration (so it gets full standard treatment: dedup, merge, BOLO Flag eligibility, display labels, Entity Relationships & History timeline inclusion) but is flagged `is_tenant_defined = true` with **no concrete TPT table at all** — every one of its fields lives in `extended_fields`, validated against its own tenant-authored Custom Field Definitions. The cost is explicit and always-disclosed: a tenant-defined type's fields are never concrete-column-indexed the way a developer-built extension's are (the same tradeoff `extended_fields` already carries), and a **graduation path** (Open Questions) exists for the platform team to later promote a popular tenant-defined type into a real developer-built extension with concrete columns, migrating existing data.

**Reserved-key namespace protects forward compatibility**: every custom field key must use a reserved prefix (`custom_`), so a developer adding a real concrete column to a type in a future platform release can never collide with a tenant's already-defined custom field of the same name.

**Self-service, but capped, not gatekept.** A Tenant Admin can define custom fields and tenant-defined types without Platform Super Admin approval — that's the point — but the platform enforces sane per-tenant caps (max custom fields per carrier, max tenant-defined types) and gives Platform Super Admin standing audit visibility (definitions, not tenant data) plus the ability to disable a specific problematic definition without deleting the underlying data.

**Carrier-agnostic by design, not carrier-exhaustive at launch.** Any feature's record type can register as an eligible carrier by declaring a `carrier_key` (e.g., `entity_type:Person`, `association_type:ConveyanceOwnerAssociation`, `domain_events_rule`, `cli_alias`) against this registry. A carrier can also be **subdivided finer than a whole registered type** by the owning feature's own convention — Courtesy Patrol's Category Definition is the motivating example, selecting a schema per tenant-configurable category (`courtesy_patrol:category:jump_start`) rather than per whole Activity extension type. This doc owns the generic mechanism; it doesn't own or enumerate every possible carrier.

## Actors & Roles

- **Tenant Admin** — defines Custom Field Definitions and Tenant-Defined Types for their own tenant, within platform-enforced caps.
- **Platform Super Admin** — sets/adjusts per-tenant caps (potentially per tenant tier), has audit visibility across all tenants' definitions (not their data), can disable a specific definition.
- **Every platform feature/module** — may register its own record type as an eligible carrier; Entity Registry Core's Entity/EntityAssociation are the default/primary carriers, wired up first.
- **Records Admin** — no distinct role here; tenant-defined type records participate in Entity Registry Core's ordinary dedup/merge review like any other type.

## User Stories

- As a **Tenant Admin**, I want to add a custom field to our Vehicle records for our own internal tracking need, without waiting on a platform release.
- As a **Tenant Admin**, I want to define a whole new lightweight record type — say, an Access Badge Log — with a handful of fields I choose, without engineering involvement.
- As a **Platform Super Admin**, I want visibility into which tenants have defined how many custom fields and types, so I can catch runaway sprawl before it becomes a performance problem.
- As a **Platform Super Admin**, I want to disable one tenant's problematic custom field definition without touching their underlying data or affecting any other tenant.
- As a **Domain Events feature developer**, I want Rule records to support tenant-added custom metadata (e.g., an "owner team" tag) without building my own bespoke extensibility scheme.
- As a **Platform Architect**, I want a popular tenant-defined type to be promotable into a real developer-built extension with concrete, indexed columns in a future release, migrating existing data rather than starting over.

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

### Tenant-Defined Types
8. A Tenant Admin can define a **Tenant-Defined Type**: a new Extension Type (of Party, Item, Location, Activity, or Document) or a new concrete EntityAssociation kind, registered normally against Entity Type Registration / Association Type Registration (`is_tenant_defined = true`) but with `concrete_schema_ref = null` — **no developer-built table is created**. Every field of a Tenant-Defined Type's records lives in `extended_fields`, validated against Custom Field Definitions scoped to that type's own `carrier_key`.
9. A Tenant-Defined Type gets full standard Entity Registry Core treatment automatically, since it's a real registered type: identity/dedup/merge, BOLO Flag eligibility, and Entity Relationships & History timeline inclusion — no special-casing anywhere else in the platform.
10. A Tenant-Defined Type's `display_label_strategy` (mandatory, per Entity Registry Core's universal requirement) must be a **template** strategy referencing one or more `extended_fields` keys by path — a tenant defining a new type also picks which of their own custom fields serves as its label.
11. A Tenant-Defined Type's dedup `match_signal_fields[]` may likewise reference `extended_fields` keys, at reduced matching-index performance compared to a concrete column (documented tradeoff, not a blocker).
12. Tenant-Defined Types are explicitly **not** a dynamic-DDL or schema-migration system — no new physical table is ever created at tenant request. This scoping decision is what keeps this feature buildable without runtime-schema risk; full custom-object systems (arbitrary indexing, relational integrity between tenant-defined types, tenant-defined permissions models) remain out of scope and are flagged as a distinct, larger future idea, not solved here.

### Caps & governance
13. Per-tenant caps — max Custom Field Definitions per carrier, max Tenant-Defined Types per tenant — are Settings & Preferences-registered values, defaulting to a platform-wide sane default and adjustable per tenant/tenant-tier by Platform Super Admin.
14. Platform Super Admin has standing read access to every tenant's Custom Field Definitions and Tenant-Defined Types (definitions/schema only, never the underlying tenant data those definitions govern) for audit/sprawl visibility.
15. Platform Super Admin can **disable** a specific Custom Field Definition or Tenant-Defined Type (e.g., a performance concern) without deleting any tenant data — disabling stops new writes to that field/type but preserves what's already stored, reversible.

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

**Tenant-Defined Type** (a specialization of Entity Type Registration / Association Type Registration, not a separate table)
- entity_type_id or association_type_id (same PK as Entity Type Registration / Association Type Registration)
- is_tenant_defined (bool, true), concrete_schema_ref (null, by definition)
- tenant_id (scoping a tenant-defined type strictly to the defining tenant — never platform-wide, unlike a developer-built type)

**Custom Field Cap** (Settings & Preferences registration)
- cap_id, tenant_id (nullable = platform default), carrier_key (nullable = applies generally) or scope = "tenant_defined_types"
- max_count

## States & Transitions

**Custom Field Definition:** `active` → `disabled` (Platform Super Admin override, or Tenant Admin retiring their own field) — disabling never deletes already-stored `extended_fields` data.

**Tenant-Defined Type:** follows Entity Type Registration's own registration lifecycle (no separate state machine) — `active` → `disabled` (same override semantics as a Custom Field Definition).

## Integrations

- **Entity Registry Core**: owns the physical `extended_fields` JSONB storage on `Entity`/`EntityAssociation` and the Entity/Association Type Registration records a Tenant-Defined Type specializes; this feature owns the Custom Field Definition/validation/Carrier registry layered on top — retrofitted, see that doc's Extended Fields section, now phrased as consuming this feature rather than owning schema declaration itself.
- **Courtesy Patrol**: motivating first use case — its Category Definition is a feature-defined finer-grained carrier (`courtesy_patrol:category:<id>`) rather than a whole registered Entity Type.
- **Domain Events, CLI-Style Input**: first two non-entity carriers, retrofitted with an `extended_fields` column on Rule and Alias respectively, proving the mechanism generalizes beyond Master Records.
- **Settings & Preferences**: owns the tenant-tier cap values (#13) via its existing hierarchical engine, rather than this feature building its own override logic.
- **Structured Logging & Audit Trails**: every Custom Field Definition and Tenant-Defined Type creation, edit, and disable is an audit-tier event.
- **Entity Relationships & History, Command Palette, CLI-Style Input**: consume a Tenant-Defined Type's records exactly like any other Entity/Activity, via its declared display label — no special-casing.

## Permissions

| Action | Tenant Admin | Platform Super Admin |
|---|---|---|
| Define/edit a Custom Field Definition (own tenant, within cap) | ✅ | ✅ |
| Define/edit a Tenant-Defined Type (own tenant, within cap) | ✅ | ✅ |
| View custom field/type definitions across all tenants | ❌ | ✅ |
| Adjust per-tenant/tenant-tier caps | ❌ | ✅ |
| Disable a specific definition | ✅ (own tenant's own definitions) | ✅ (any tenant) |
| Register a new carrier type (developer, via feature build) | ❌ | ✅ |

## Non-Functional / Constraints

- `extended_fields` content, on any carrier, is never guaranteed indexed/queryable with concrete-column performance — a platform-wide, always-disclosed tradeoff of the whole mechanism, not something each consuming feature re-derives independently.
- Reserved-key (`custom_*`) enforcement must be airtight — a definition attempt using an unprefixed or already-reserved key is rejected outright, not just discouraged.
- Per-tenant caps must be enforced atomically against concurrent definition creation (no race allowing a tenant to exceed its cap).
- Disabling a definition must never be destructive — underlying `extended_fields` data for already-created records is preserved, only new/edited writes against a disabled definition are blocked.
- A Tenant-Defined Type's dynamic form rendering (letting a user create/edit records of a type whose fields are only known at runtime, from its Custom Field Definitions) is a genuine technical-spec-level UI challenge, flagged here as a real cost of this capability, not hand-waved.
- WCAG 2.1 / Section 508 accessible custom-field/type definition and dynamic-form-rendering flows, day one.

## Acceptance Criteria

- [ ] A Tenant Admin can define a `custom_preferred_shift` field on Person and have it validated on write without a platform deploy.
- [ ] Attempting to define a custom field with a key not prefixed `custom_` is rejected.
- [ ] A Tenant Admin defining custom fields up to their tenant's cap succeeds; the next attempt beyond the cap is rejected with a clear reason.
- [ ] A Tenant Admin can define a wholly new Tenant-Defined Type (e.g., "Access Badge Log") with three custom fields and a template display label referencing one of them, then create, dedup-flag, and merge two records of that type using entirely standard Entity Registry Core mechanics.
- [ ] A Domain Events Rule and a CLI Alias can each be given a tenant-defined custom field, proving the mechanism works on a non-entity carrier.
- [ ] Platform Super Admin can view (but not silently alter) every tenant's Custom Field Definitions and Tenant-Defined Types from one audit view.
- [ ] Platform Super Admin disabling a specific Custom Field Definition blocks new writes to it while leaving previously-stored data on existing records intact.
- [ ] Courtesy Patrol's per-category schema selection continues to work unchanged, now sourced from this feature's Custom Field Definition registry instead of a bespoke local mechanism.

## Open Questions

- Exact default cap values (max custom fields per carrier, max tenant-defined types per tenant) — pending real-usage-informed tuning, likely starting conservative.
- Exact **graduation path** mechanics: how a popular Tenant-Defined Type or Custom Field Definition gets promoted into a real developer-built concrete column/table in a future platform release, and how existing `extended_fields` data migrates into the new concrete storage without downtime or data loss — a significant technical-spec-level undertaking, not solved here.
- Whether Tenant-Defined Types ever need their own permission/RBAC model (who can create/view/edit records of a specific tenant-defined type) beyond the platform's existing per-feature RBAC/ABAC baseline, given there's no "owning feature" UI to hang permissions off of the way developer-built types have — likely needs a generic, type-agnostic permission surface, not designed here.
- Whether `select` field_type's `options[]` need their own tenant-editable list-management UI (similar to DAR Entry's category pattern) or are simpler static arrays — leaning toward the latter for v1.
- Full dynamic-form-rendering technical approach for Tenant-Defined Types (schema-driven UI generation) — flagged as a real technical-spec challenge, not detailed here.
- Whether this feature's caps should scale by tenant tier/subscription level (a plausible commercial lever) — noted as a possibility, not a commitment.
