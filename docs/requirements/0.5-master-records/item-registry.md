# Item Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Item Registry generalizes MODULES.md's "Vehicle Registry" into the base Item Entity Type (per Entity Registry Core), structured per NIEM Core's `nc:ItemType` — the same generic physical-object/property concept NIEM uses for evidence, seized property, and personal effects. This mirrors Person Registry's pattern: a shared base entity with universal fields, extended by type-specific profiles.

Beyond identity, Item Registry establishes **`CustodyAssociation`** — a TPT subtype of Entity Registry Core's EntityAssociation — as the shared custody/possession-tracking mechanism, not because Vehicle strictly needs it today, but because nearly every future item-tracking module (Weapons Inventory, Key Custody, Lost & Found, Digital/Physical Evidence Tracking, K9 Special Equipment) fundamentally needs "who currently holds this, and its transfer history," and rebuilding that per-module would duplicate exactly the kind of logic Entity Registry Core exists to centralize. There is no separate `current_holder`/`custody_history` mechanism on the Item table itself: the current holder is simply whichever `CustodyAssociation` row is `active` for a given Item, and history is the full set of `active`→`removed` rows over time — the same pattern Entity Registry Core already establishes for any single-current-value relationship. An Item with no active CustodyAssociation is, by definition, in storage/unassigned.

**Vehicle**, Item's first and most consequential Extension Type, is specified in full depth in its own dedicated doc — [vehicle-conveyance-registry.md](vehicle-conveyance-registry.md) — rather than as a stub here, since (like Person) it's a cross-cutting core concern consumed by many modules, not owned by any single one. This doc establishes only the base Item mechanics Vehicle (and every future extension) builds on.

## Actors & Roles

- **Fleet/Vehicle Coordinator, Site Admin** — create/maintain Vehicle profiles within their permission scope.
- **Any user with custody-transfer permission** (scope varies by future consuming module — armory staff for weapons, key control for keys, etc.) — records custody transfers on Item records.
- **Supervisor+ (elevated permission)** — creates/clears BOLO Flags on Vehicle (and any future Item extension) via Entity Registry Core's shared mechanism.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Item entities specifically.
- **Every future item-tracking module developer** (Equipment/Assets/Weapons, Lost & Found, K9 Special Equipment) — registers its own Item Extension Type against this registry.

## User Stories

- As a **Fleet Coordinator**, I want to register a patrol vehicle with its VIN, plate, make, model, and photo, and have it globally trackable across dispatch, incidents, and maintenance logs by one stable ID.
- As a **Guard**, I want to scan a vehicle's plate and see if it's BOLO-flagged, using the exact same underlying mechanism that flags a person, so the platform behaves consistently regardless of what's being flagged.
- As a **Records Admin**, I want two vehicle records with the same VIN flagged as a likely duplicate, so I can review and merge them rather than have dispatch history split across two records for the same vehicle.
- As a **future Weapons Inventory developer**, I want to register "Weapon" as an Item extension and get global ID, dedup (by serial number), and custody tracking for free, rather than building my own chain-of-custody log from scratch.
- As an **Armory Custodian** (illustrative future consumer), I want an item's custody transfer to require the same rigor (audit trail, current holder always known) regardless of whether it's a weapon, a key, or a piece of tactical gear, because the underlying mechanism is shared.
- As a **Supervisor**, I want creating a BOLO flag on a stolen/wanted vehicle to require the same justification, expiration, and step-up authentication as flagging a person, since both are equally consequential if done wrongly.

## Functional Requirements

### Base Item fields (NIEM Core `nc:ItemType`-aligned)
1. **Category**: an item category classification (e.g., vehicle, weapon, asset, found_item, key, equipment), extensible — each future consuming module registers its own category value(s) rather than this doc enumerating all of them.
2. **Description, brand/make, model**: general identifying fields per `nc:ItemType`.
3. **Color**: per `nc:ItemType`'s color attribute.
4. **Identification**: a generic, repeatable `ItemIdentification` structure (an ID value + a category, e.g., serial_number, VIN, asset_tag, barcode) — mirroring Person Registry's Identification pattern, flexible enough to cover whatever identifier a given item type actually uses.
5. **Value**: monetary value, relevant to future depreciation/insurance/loss-prevention features.
6. **Status**: a generic lifecycle status (active, decommissioned, lost, disposed, in_repair) — extension types may layer their own additional lifecycle nuance on top (e.g., a Vehicle's own maintenance status), but the base status always reflects the item's overall existence state.
7. **Photo(s)**: canonical reference image(s) for the item.
8. Every base field above is optional/nullable — a given item type populates only what's relevant to it.
8a. **Display label** (per Entity Registry Core's universal requirement): template strategy, `brand_make model_name` where populated, falling back to `item_category: [primary identification]` for sparse records (e.g., a found item with no brand/model on file).

### Custody / possession tracking
9. `CustodyAssociation` links an Item (`entity_id_a`) to whoever/wherever currently holds it (`entity_id_b` — typically a Person or Location entity) via the standard EntityAssociation shape; no extra fields beyond role/audit are needed for the base case.
10. Recording a custody transfer means: set the prior active `CustodyAssociation` row (if any) to `removed`, and create a new `active` row — an audit-tier, atomic operation, so the current holder is never ambiguous mid-transfer. This is exactly Entity Registry Core's general single-current-value association pattern, not a bespoke mechanism.
11. Future modules needing richer custody semantics (e.g., a formal chain-of-custody with witness signatures for evidence, or a checkout/check-in workflow for armory weapons) register their own further-extended association subtype (e.g., a `WeaponCustodyAssociation` extending `CustodyAssociation`) with the extra fields they need, rather than maintaining a separate, disconnected possession record.

### Extension types
12. **Vehicle** is registered as an Item Extension Type per this mechanism; its full field set, ownership model, permit association, and BOLO/Violation flagging are specified in [vehicle-conveyance-registry.md](vehicle-conveyance-registry.md), not duplicated here.
13. Future Item extensions (Weapon, Asset, Found Item, Key, Tactical Equipment) will each be registered by their owning module (Equipment/Assets/Weapons, Lost & Found, K9 & Specialized Unit Operations) when those modules are specified, each inheriting base identity, dedup, and custody tracking for free.

### BOLO Flag
14. Any Item extension (Vehicle, and future types) uses Entity Registry Core's generic **BOLO Flag** mechanism directly — the identical governance (elevated permission, mandatory justification, mandatory expiration, step-up authentication to create/clear, audit-tier logging) already established for Person's BOLO/Trespass Subject.

### Deduplication
15. Item-specific match signals for Entity Registry Core's deduplication engine: exact match on serial number, VIN, or barcode/asset tag drives high-confidence flagging; where no strong unique identifier is available, a combination of make + model + distinguishing features can still flag a probable duplicate for human review. Never auto-merged, per Entity Registry Core's universal governance. (Vehicle-specific signals, including plate+jurisdiction, are detailed in Vehicle/Conveyance Registry.)

## Data Model / Fields

**Item** (TPT level: entity_id is the shared PK, FK → Entity.entity_id — structured per `nc:ItemType`)
- entity_id (PK, FK → Entity), tenant_id, item_category
- description, brand_make, model_name, color
- identifications[] (id_value, category: serial_number/vin/asset_tag/barcode/other)
- value, status (active, decommissioned, lost, disposed, in_repair)
- photo_refs[]

**CustodyAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Item, entity_id_b = current holder — Person or Location)
- no extra fields beyond the base EntityAssociation shape

**Vehicle** — see [vehicle-conveyance-registry.md](vehicle-conveyance-registry.md) for full data model.

## States & Transitions

**Item:** `active` → `decommissioned` | `lost` | `disposed` | `in_repair` → `active` (e.g., repaired item returns to service). Independent of custody state.

**Custody (via CustodyAssociation):** no active row (`in_storage`, implicit) → `active` (`held_by:<entity>`) → `removed` + new `active` row (`held_by:<different_entity>`) | `removed` with no replacement (`in_storage`) — Entity Registry Core's standard EntityAssociation lifecycle, unmodified.

**BOLO Flag:** follows Entity Registry Core's shared BOLO Flag lifecycle unmodified (`active` → `cleared` | `expired`).

## Integrations

- **Entity Registry Core**: owns the base Item TPT mechanics (global ID, deduplication, merge), the generic BOLO Flag mechanism, and the base EntityAssociation shape `CustodyAssociation` extends.
- **Authentication & Authorization**: step-up authentication for BOLO creation/clearance (via Entity Registry Core's mechanism); RBAC gating of custody-transfer and value-field visibility.
- **Structured Logging & Audit Trails**: custody transfers, BOLO lifecycle events, and merges are audit-tier.
- **Vehicle/Conveyance Registry**: the full specification of Item's first and most consequential Extension Type — see that doc rather than this one for anything Vehicle-specific.
- **Equipment/Assets/Vehicles & Resources, Lost & Found, K9 & Specialized Unit Operations**: future registrants of additional Item Extension Types (Weapon, Asset, Found Item, Tactical Equipment), each building on this doc's identity, dedup, and custody-tracking foundation.
- **Entity Relationships & History**: consumes Item entity IDs and CustodyAssociation history to build cross-module interaction timelines.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Fleet/Asset Coordinator | Supervisor+ | Records Admin |
|---|---|---|---|---|---|
| Create/view base Item profile | ✅ | ✅ | ✅ | ✅ | ✅ |
| View item value | ✅ | ✅ | ✅ (if granted) | ✅ (if granted) | ✅ |
| Record a custody transfer | ✅ | ✅ | ✅ (own scope) | ✅ | ❌ (unless also granted) |
| Create/clear BOLO/Violation Flag *(per Entity Registry Core's BOLO Flag mechanism)* | ✅ | ✅ | ❌ | ✅ (own scope, step-up) | ❌ (unless also granted) |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Custody transfer (removing the prior active CustodyAssociation row and creating a new one) must be atomic and immediately consistent — the current holder must never be ambiguous, given how many future high-stakes modules (weapons, evidence, keys) depend on this being reliably correct.
- BOLO/Violation Flag governance is inherited unmodified from Entity Registry Core — no vehicle-specific relaxation of permission, justification, expiration, or step-up requirements.
- Item value and identification numbers are treated as sensitive fields subject to the same visibility/audit rules established for Person's sensitive fields, given their relevance to theft/loss-prevention and insurance contexts.
- WCAG 2.1 / Section 508 accessible item profile and custody-transfer flows, day one.

## Acceptance Criteria

- [ ] A base Item record supports category, description, brand/make/model, color, identification(s), value, status, and photo(s), each independently optional.
- [ ] Registering "Vehicle" as an Item Extension Type correctly inherits base identity/dedup/custody mechanics without reimplementing them.
- [ ] Recording a custody transfer atomically removes the prior active CustodyAssociation row (if any) and creates a new one; the item's current holder is never ambiguous mid-transfer.
- [ ] An Item with no active CustodyAssociation is correctly treated as in storage/unassigned, with no special "in_storage" record required.
- [ ] Querying an Item's custody history returns its full set of active/removed CustodyAssociation rows in order, with no separate history table involved.
- [ ] Two Item records (of any extension type) sharing a strong unique identifier are flagged as a potential duplicate per this doc's model, never auto-merged.
- [ ] A future registration of a new Item Extension Type (e.g., a stubbed "Weapon" type) correctly inherits global ID, dedup, and custody tracking without requiring changes to this doc's mechanisms.

## Open Questions

- Full item_category taxonomy is built out incrementally as each future module (Equipment/Assets/Vehicles & Resources, Lost & Found, K9 & Specialized Unit Operations) registers its own categories — not enumerated here.
- Whether custody transfer ever requires a receiving-party confirmation/acceptance step (vs. a one-sided recording by the transferring party) — likely varies by future consuming module (e.g., weapons custody might require dual confirmation; general assets might not) and is deferred to those docs.
- Exact NIEM release/version and precise `nc:ItemType`/`nc:ConveyanceType` element names — same technical-spec-level verification task noted in Entity Registry Core and Person Registry.
