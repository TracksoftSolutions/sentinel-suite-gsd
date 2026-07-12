# Vehicle/Conveyance Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Vehicle/Conveyance Registry is the full first-class specification of Vehicle as an Extension Type of the base Item entity (per Item Registry), promoted from a thin stub to proper depth because — like Person — it's a cross-cutting core concern consumed by many modules (Dispatch/CAD tracking patrol units, Access Control's parking/BOLO workflows, Equipment/Assets & Vehicles' fleet management, Executive Protection's armored car registry, Supply Chain's cargo transport, Special Event coordination) rather than something owned by any single one of them. This mirrors MODULES.md's original structure, which listed Vehicle Registry as its own top-level Master Records feature parallel to Person Registry, not a footnote of another module.

The base Vehicle record is deliberately **uniform** — no category/type enum distinguishing "fleet" vs. "personal" vs. "visitor" vs. "vehicle of interest." Per the same pattern already established for Person (Employee/Visitor/Contractor as distinct Extension Types, not a flat category field on Person), a vehicle's *purpose or relationship* to the platform is expressed by which further extensions a consuming module attaches to it later (e.g., Equipment/Assets & Vehicles registering a "Fleet Assignment" extension; Access Control registering a "Registered Parking Vehicle" extension) — not by a category flag here. A vehicle with zero such extensions but an active BOLO Flag is simply a vehicle of interest; nothing else needs to change about its base record.

Vehicle inherits Item Registry's identity, deduplication, and custody-tracking mechanics, and Entity Registry Core's generic BOLO Flag mechanism, without reimplementing any of them.

## Actors & Roles

- **Fleet/Vehicle Coordinator, Site Admin** — create/maintain Vehicle records within their permission scope.
- **Any user with custody-transfer permission** — records who currently has/is operating a fleet vehicle, via Item Registry's shared custody mechanism.
- **Supervisor+ (elevated permission)** — creates/clears BOLO/Violation Flags on a vehicle, via Entity Registry Core's shared BOLO Flag mechanism.
- **Records Admin** — resolves Item Registry deduplication flags for Vehicle entities specifically.
- **Future consuming modules** (Equipment/Assets & Vehicles, Access Control, Executive Protection, Supply Chain & Cargo Security, Special Event & IAP): register their own further extensions on Vehicle when those modules are specified.

## User Stories

- As a **Fleet Coordinator**, I want to register a patrol vehicle with VIN, plate, make, model, color, and photo once, and have every module that touches vehicles (dispatch, maintenance, incidents) reference the same canonical record.
- As a **front desk Guard**, I want to record a visitor's personal vehicle with owner and plate information for parking authorization, without the system treating it any differently at the base-record level than a company fleet vehicle.
- As a **Fleet Coordinator**, I want to know a fleet vehicle is currently owned by our company but assigned to a specific guard this shift, using two distinct fields rather than one conflated concept.
- As a **Guard**, I want to scan a plate and see any active BOLO/Violation flag and any associated parking permit in one lookup, without needing to know which module actually manages permits.
- As a **Records Admin**, I want two vehicle records with the same VIN or plate+jurisdiction combination flagged as a likely duplicate, consistent with how Item Registry already governs deduplication.
- As an **Equipment/Assets & Vehicles module developer** (future), I want to register a "Fleet Assignment" extension on top of the existing Vehicle record instead of building a parallel vehicle concept, so fleet-specific data (assigned unit number, maintenance schedule) links cleanly to the same canonical vehicle.

## Functional Requirements

### Base Vehicle fields (`nc:ConveyanceType`-aligned, extending Item)
1. Vehicle is registered as an Extension Type of Item (per Item Registry's established pattern), inheriting Item's base fields — description, brand/make, model, color, identification(s), value, status, photo(s) — without duplication.
2. Vehicle-specific core fields: license plate, plate-issuing jurisdiction/state, model year, body style.
3. VIN is recorded through Item's generic `identifications[]` structure (category: `vin`) rather than a Vehicle-specific field, consistent with Item Registry's identification model; VIN is the primary high-confidence dedup signal for vehicles (per Item Registry's established match signals).
4. The base Vehicle record carries no category/type enum — a vehicle's purpose or relationship to the platform is expressed by further extensions consuming modules attach to it, not a flat field here.
4a. **Display label** (per Entity Registry Core's universal requirement, overriding Item's own default): template strategy, `license_plate (plate_jurisdiction) — brand_make model_name`.

### Ownership vs. custody
5. **Ownership** is a `ConveyanceOwnerAssociation` — a TPT subtype of Entity Registry Core's EntityAssociation (`entity_id_a` = the Vehicle, `entity_id_b` = a Party — Person or Organization) — distinct from Item Registry's `CustodyAssociation` (who currently possesses/is operating the vehicle right now). A company fleet vehicle's active `ConveyanceOwnerAssociation` points to the tenant's own designated Organization record but its `CustodyAssociation` rotates shift to shift between guards; a visitor's personal vehicle's owner is that visitor (a Person) and typically has no active CustodyAssociation at all. No `ConveyanceOwnerAssociation` present defaults to "owned by the tenant" for tenants that haven't bothered creating a self-representing Organization record.
6. Modeling ownership as an association rather than a plain field gets a natural ownership history for free (the sequence of active/removed `ConveyanceOwnerAssociation` rows, e.g., across a resale) and the same add/remove audit trail as every other association, without a bespoke history mechanism.

### Permit association
7. A Vehicle record holds a lightweight `associated_permits[]` list (permit type, reference ID, basic status) reflecting what the vehicle is currently authorized for (parking pass, authorization tag, lease agreement), so a vehicle's record shows this at a glance.
8. The full permit lifecycle — issuance, renewal, revocation, eligibility rules — is owned by Access Control's future Clearance Profiles/Credential Management docs; this doc's `associated_permits[]` is a reference/display surface, not the system of record for permit workflow.

### BOLO / Violation Flagging
9. Vehicle uses Entity Registry Core's generic BOLO Flag mechanism directly (already established in Item Registry) — no vehicle-specific governance logic. A vehicle with an active BOLO Flag and no other extensions is, in practice, a "vehicle of interest," with no separate data structure needed for that concept.

### Deduplication & custody
10. Vehicle-specific dedup signals (VIN exact match highest confidence; plate + jurisdiction combination as a secondary strong signal; make + model + color as a fuzzy fallback) extend Item Registry's already-established match-signal model — never auto-merged, per that doc's universal governance.
11. Custody transfer for a Vehicle (e.g., assigning a patrol car to a guard for a shift) uses Item Registry's `CustodyAssociation` mechanism unmodified.

## Data Model / Fields

**Vehicle** (TPT level: entity_id is the shared PK, FK → Item.entity_id)
- entity_id (PK, FK → Item), license_plate, plate_jurisdiction, model_year, body_style
- associated_permits[] (permit_type, reference_id, status)

*(VIN, make, model, color, photos, value, and status all live on the base Item record per Item Registry — not duplicated here. Ownership is a `ConveyanceOwnerAssociation` row, not a field here.)*

**ConveyanceOwnerAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Vehicle, entity_id_b = the owning Party)
- no extra fields beyond the base EntityAssociation shape

## States & Transitions

**Vehicle:** no independent lifecycle beyond the base Item's status (active, decommissioned, lost, disposed, in_repair) — a Vehicle is simply an Item with vehicle-specific fields, consistent with Item Registry's existing model.

**ConveyanceOwnerAssociation:** follows Entity Registry Core's shared EntityAssociation lifecycle unmodified (`active` → `removed`, with a new `active` row on resale — the sequence over time is the ownership history).

**BOLO/Violation Flag:** follows Entity Registry Core's shared BOLO Flag lifecycle unmodified.

## Integrations

- **Item Registry**: owns the base Item mechanics (identity, dedup, CustodyAssociation, base fields) Vehicle extends without reimplementing.
- **Entity Registry Core**: owns the generic BOLO Flag mechanism and the base EntityAssociation shape `ConveyanceOwnerAssociation` extends.
- **Party Registry, Organization Registry**: source of the Party entity `ConveyanceOwnerAssociation` resolves to (a Person or the tenant's own/an external Organization).
- **Access Control**: future owner of the full permit/parking-pass/authorization-tag lifecycle; this doc's `associated_permits[]` is a reference surface into that system.
- **GIS & Mapping Services**: vehicle position tracking (live GPS) consumes the Vehicle's underlying Item entity_id as its stable reference.
- **Equipment/Assets & Vehicles & Resources, Executive Protection & Secure Transit, Supply Chain & Cargo Security, Special Event & IAP**: future registrants of further Vehicle extensions (fleet assignment, armored car specs, cargo transit details, event vehicle credentials) — each building on this doc's canonical Vehicle record rather than creating a parallel concept.
- **Security Operations (Tickets/Citations)**: future source of citation records a vehicle's BOLO/Violation Flag context field (`citation_ref`, per Entity Registry Core) may reference.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Fleet/Vehicle Coordinator | Supervisor+ | Records Admin |
|---|---|---|---|---|---|
| Create/view Vehicle record | ✅ | ✅ | ✅ | ✅ | ✅ |
| Add/remove ConveyanceOwnerAssociation | ✅ | ✅ | ✅ (own scope) | ❌ (unless also granted) | ❌ |
| Record custody transfer | ✅ | ✅ | ✅ (own scope) | ✅ | ❌ (unless also granted) |
| View/manage associated permits (reference only) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Create/clear BOLO/Violation Flag *(per Entity Registry Core's BOLO Flag mechanism)* | ✅ | ✅ | ❌ | ✅ (own scope, step-up) | ❌ (unless also granted) |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Ownership changes are audited for free via ConveyanceOwnerAssociation's standard EntityAssociation add/remove trail — no bespoke audit mechanism needed.
- `associated_permits[]` must degrade gracefully if Access Control's permit system is not yet implemented/enabled for a given tenant — an empty list, not an error.
- WCAG 2.1 / Section 508 accessible Vehicle record views and creation flows, day one.

## Acceptance Criteria

- [ ] A Vehicle record correctly inherits Item Registry's base fields (make, model, color, photo, value, status) without duplicating them in the Vehicle extension's own schema.
- [ ] A fleet vehicle's active ConveyanceOwnerAssociation (the tenant) remains constant while its active CustodyAssociation (current holder) changes across shifts, verified as two independent association hierarchies.
- [ ] A visitor's personal vehicle has a `ConveyanceOwnerAssociation` resolving to that visitor's Person record, and requires no active CustodyAssociation to be fully valid.
- [ ] A company fleet vehicle's `ConveyanceOwnerAssociation` resolves to the tenant's designated Organization record (or is absent, defaulting to tenant-owned), distinct from its rotating custody.
- [ ] Reselling a vehicle creates a new active ConveyanceOwnerAssociation while the prior one becomes removed, giving a correct ownership history with no separate history table.
- [ ] Two Vehicle records sharing the same VIN are flagged as a potential duplicate; two records sharing the same plate+jurisdiction but different VINs are also flagged, per the established match-signal model.
- [ ] Creating a BOLO/Violation Flag on a Vehicle uses the identical Entity Registry Core mechanism already verified for Person and generic Item BOLO flagging — no separate implementation.
- [ ] A vehicle with an active BOLO Flag and no other extensions functions correctly as a "vehicle of interest" with no additional data structure required.
- [ ] `associated_permits[]` displays correctly whether empty (no Access Control integration yet) or populated (referencing a real permit record).
- [ ] A future extension (e.g., a stubbed "Fleet Assignment" extension) can be registered on top of an existing Vehicle record without modifying this doc's schema.

## Open Questions

- Exact structure of `associated_permits[]` entries and how they resolve to Access Control's future permit records — to be finalized when Access Control's Clearance Profiles/Credential Management is specified.
