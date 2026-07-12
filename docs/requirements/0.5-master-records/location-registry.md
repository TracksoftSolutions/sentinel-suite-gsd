# Location Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Location Registry is the base Location Entity Type (per Entity Registry Core), structured per NIEM Core's `nc:LocationType` — name, category, structured address, geographic coordinate/boundary geometry, and generic identification. Unlike Vehicle (which deliberately has no base-level category, since its purpose is extension-driven), Location genuinely needs a base-level `location_type` because the applicable geometry differs by kind — a Site needs an address, a Zone needs a boundary polygon, a Room needs floor/room identifiers — this is structural, not a workflow distinction that belongs in an extension.

A Location's own geometry answers "where is this place, physically" — intrinsic identity, owned here. This is a related but distinct concept from GIS & Mapping Services' **Geofence** (a purpose-built alerting overlay with entry/exit/dwell triggers), which doesn't have to correspond 1:1 with any single location's footprint — a patrol geofence might span several locations, or a restricted-zone geofence might cover only part of one room. Where they *do* coincide, a Geofence can directly reuse a Location's own geometry rather than requiring a redundant independently-drawn duplicate shape.

Basic parent-child nesting (a Room's parent is a Floor, a Floor's parent is a Building) is recorded via **`HierarchyAssociation`** — a TPT subtype of Entity Registry Core's EntityAssociation, generic enough to be reused by Organization Registry's own parent/subsidiary nesting (and any future module needing self-referential parent/child structure) rather than each type inventing its own bespoke parent-link mechanism. The rich hierarchy-building tooling (drag-and-drop tree builder, depth configuration, account-manager association, status indicators) belongs to Facility & Zone Management's future Location Hierarchy Designer, built on top of `HierarchyAssociation` rather than reinventing it. Similarly, MODULES.md's "Utility & Contact Directory" gets a light reference surface here (with the facility-manager contact modeled as a `FacilityManagerAssociation`, not a plain field); the full directory (shut-off procedures, operational guides) is owned by Facility & Zone Management's future Utility Control Tracking — same deferral pattern as Vehicle's `associated_permits[]`.

## Actors & Roles

- **Site Admin, Facility Coordinator** — create/maintain Location records within their permission scope.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Location entities specifically.
- **Facility & Zone Management (future)** — owns the rich hierarchy-building UI and the full utility/contact directory built on top of this doc's basic structural fields.
- **GIS & Mapping Services** — consumes Location geometry directly, and may let a Geofence reference it rather than duplicate it.

## User Stories

- As a **Facility Coordinator**, I want to register "Building A" with its address and footprint polygon once, and have every module that references a site (dispatch, incidents, access control) use the same canonical record.
- As a **Site Admin**, I want a Room's location record to inherit its parent Floor and Building automatically through a simple parent link, without needing the full hierarchy-designer tool just to know "this room is in this building."
- As a **GIS engineer**, I want a Geofence for "all of Building A" to reuse Building A's own footprint geometry rather than requiring someone to redraw the identical polygon a second time.
- As a **Dispatcher**, I want a location's quick-glance facility manager contact and local law enforcement dispatch number available without navigating into a separate utility-management module.
- As a **Records Admin**, I want two location records with the same name and address flagged as a likely duplicate, so site references don't silently split across two records.
- As a **Facility & Zone Management developer** (future), I want to build the full hierarchy-designer and utility-directory tooling on top of this doc's basic parent link and light contact fields, rather than re-deriving location identity from scratch.

## Functional Requirements

### Base Location fields (NIEM Core `nc:LocationType`-aligned)
1. **Name & type**: `location_name` (friendly display name) and a base-level `location_type` (site, building, floor, room, zone, outdoor_area — extensible), since the fields that apply genuinely differ by kind.
2. **Address**: a structured address (street, city, state/region, postal code, country) for location types where an address is meaningful (typically site/building level).
3. **Geometry**: a geographic coordinate (point) and/or a boundary polygon, populated according to what's meaningful for the location's type — a point marker for a specific asset location, a polygon footprint for a site/building/zone.
4. **Floor/room identifiers**: `floor_identifier`, `room_identifier` for location types requiring indoor granularity.
5. **Identification**: a generic, repeatable `LocationIdentification` structure (id value + category, e.g., site_code, tax_parcel_id, building_permit_id) — mirroring Person's and Item's identification pattern.
6. **Description**: free-text description.
7. Every base field above is optional/nullable — a given location type populates only what's relevant to it.
7a. **Display label** (per Entity Registry Core's universal requirement): template strategy, `location_name` (falling back to `location_type` + address/coordinate summary for an unnamed location).

### Hierarchy (via shared HierarchyAssociation)
8. A Location's parent is recorded as an active `HierarchyAssociation` (`entity_id_a` = the child Location, `entity_id_b` = the parent Location) — a basic structural fact (this Room's parent is this Floor), not a plain self-referential field, so a location's containment history (e.g., a room reassigned to a different floor during a remodel) is preserved the same way ownership/custody history is.
9. Facility & Zone Management's future Location Hierarchy Designer builds its richer tooling (visual tree builder, depth configuration, account-manager association, status indicators) on top of `HierarchyAssociation` rather than maintaining a separate, disconnected hierarchy structure.

### Utility & Contact Directory (light reference)
10. A Location record holds a lightweight quick-glance surface: a `FacilityManagerAssociation` (`entity_id_a` = the Location, `entity_id_b` = a Party — the facility manager, always a real Person or Organization entity, no free-text fallback), a short list of `utility_shutoff_pointers[]` (label + location note, e.g., "Main water shutoff — basement, north wall"), and a `local_dispatch_number` (local law enforcement/emergency dispatch contact).
11. The full utility/contact directory — detailed shut-off procedures, operational guides, GIS-marked utility control points — is owned by Facility & Zone Management's future Utility Control Tracking feature; this doc's fields are a display surface, not the system of record for utility operations.

### Geometry vs. Geofence
12. A GIS & Mapping Services Geofence may optionally set a `derived_from_location_ref` pointing to a Location's own geometry, reusing it directly rather than requiring an independently-drawn duplicate shape when the two coincide. Geofences not tied to any single location's footprint (e.g., a patrol zone spanning several locations) continue to define their own independent geometry as already established in that doc.

### Deduplication
13. Location-specific match signals for Entity Registry Core's deduplication engine: name + address (exact or fuzzy) as a strong signal; coordinate proximity + name similarity as a secondary signal where address isn't populated (e.g., an outdoor zone). Never auto-merged, per Entity Registry Core's universal governance.

## Data Model / Fields

**Location** (TPT level: entity_id is the shared PK, FK → Entity.entity_id — structured per `nc:LocationType`)
- entity_id (PK, FK → Entity), tenant_id, location_name, location_type
- address (street, city, state_region, postal_code, country) — nullable
- coordinate (lat, long) — nullable
- geometry (boundary polygon) — nullable
- floor_identifier, room_identifier — nullable
- identifications[] (id_value, category: site_code/tax_parcel_id/building_permit_id/other)
- description
- utility_shutoff_pointers[] (label, location_note)
- local_dispatch_number

*(Parent location and facility manager contact are `HierarchyAssociation` and `FacilityManagerAssociation` rows, not fields here.)*

**HierarchyAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id — reusable by Location and Organization)
- association_id (PK, FK → EntityAssociation; entity_id_a = child, entity_id_b = parent)
- no extra fields beyond the base EntityAssociation shape

**FacilityManagerAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Location, entity_id_b = the managing Party)
- no extra fields beyond the base EntityAssociation shape

## States & Transitions

**Location:** `active` → `tombstoned` (merged away, per Entity Registry Core's standard model) → `active` (merge reversed). No location-specific lifecycle beyond the standard Entity Registry Core states.

**HierarchyAssociation, FacilityManagerAssociation:** follow Entity Registry Core's shared EntityAssociation lifecycle unmodified (`active` → `removed`, with a new `active` row when reassigned — the sequence over time is the containment/management history).

## Integrations

- **Entity Registry Core**: owns the base Location TPT mechanics (global ID, deduplication, merge) and the base EntityAssociation shape `HierarchyAssociation`/`FacilityManagerAssociation` extend.
- **GIS & Mapping Services**: consumes Location geometry directly for map rendering; Geofences may reference a Location's geometry via `derived_from_location_ref` rather than duplicating it.
- **Facility & Zone Management**: future owner of the rich Location Hierarchy Designer (built on this doc's `HierarchyAssociation`) and full Utility Control Tracking (built on this doc's light contact/utility fields).
- **Organization Registry**: reuses `HierarchyAssociation` for its own parent/subsidiary nesting, rather than a duplicate mechanism.
- **Structured Logging & Audit Trails**: location creation, hierarchy changes, and merges are audit-tier events.
- **Every module that references a physical place** (scheduling, dispatch, incidents, access control, and beyond): consumes Location entity IDs as the stable cross-reference key.
- **Entity Relationships & History**: consumes Location entity IDs to build cross-module interaction timelines (e.g., every incident/access event at a given site).

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin/Facility Coordinator | Records Admin |
|---|---|---|---|---|
| Create/view Location record | ✅ | ✅ | ✅ | ✅ |
| Add/remove HierarchyAssociation (reparent) | ✅ | ✅ | ✅ (own scope) | ❌ |
| Add/remove FacilityManagerAssociation, edit utility quick-reference fields | ✅ | ✅ | ✅ (own scope) | ❌ |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ✅ |

## Non-Functional / Constraints

- Geometry storage/format must be directly consumable by GIS & Mapping Services' rendering pipeline without a translation step, given how tightly the two are related.
- `HierarchyAssociation` must not permit cycles (a location cannot be its own ancestor, directly or transitively) — enforced at write time.
- WCAG 2.1 / Section 508 accessible location record views and creation flows, day one.

## Acceptance Criteria

- [ ] A base Location record supports name, type, address, coordinate/geometry, floor/room identifiers, identification(s), and description, each independently optional per what's relevant to its type.
- [ ] A Room location's active `HierarchyAssociation` correctly points to its Floor, and that Floor's to its Building, without requiring the full Facility & Zone Management hierarchy tool to exist yet.
- [ ] A GIS Geofence set with `derived_from_location_ref` pointing to a Location correctly renders/evaluates using that location's own geometry, with no separately drawn duplicate shape.
- [ ] Two Location records with matching name and address are flagged as a potential duplicate per Entity Registry Core's model, never auto-merged.
- [ ] Attempting to create a `HierarchyAssociation` that would create a cycle is rejected.
- [ ] Reassigning a Room to a different Floor creates a new active `HierarchyAssociation` while the prior one becomes removed, preserving containment history.
- [ ] A location's `FacilityManagerAssociation` and local dispatch number display correctly whether populated or empty, without requiring Facility & Zone Management's full utility directory to be implemented.

## Open Questions

- Full location_type taxonomy beyond the illustrative set (site, building, floor, room, zone, outdoor_area) — to be finalized alongside Facility & Zone Management's Location Hierarchy Designer.
- Exact geometry storage format (e.g., GeoJSON vs. a platform-specific representation) — a technical-spec-level decision aligned with GIS & Mapping Services' chosen provider adaptors.
- Whether Location ever needs its own BOLO-Flag-style use case (e.g., flagging a condemned or under-threat location) — Entity Registry Core's generic BOLO Flag mechanism is available to any entity type already, so no new work would be required if a future module needs this; not speculatively built out here.
- Exact NIEM release/version and precise `nc:LocationType` element names — same technical-spec-level verification task noted in the other Master Records docs.
