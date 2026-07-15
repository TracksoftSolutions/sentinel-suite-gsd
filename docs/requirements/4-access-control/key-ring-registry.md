# Key Ring Registry

## Overview

Key Ring Registry opens the platform's physical-mechanical-key cluster of Module 4 (this doc, then Key Custody & Auditing, then Lock Core & Cylinder Tracking) — a genuinely different domain from the digital PACS/credential docs before it: no adaptor, no PIAM vendor, just physical hardware and the platform's own already-proven Item Registry/custody/hierarchy mechanisms applied a further time. **Key** and **Key Ring** both register as Item extensions — full identity/dedup/custody/audit, the same treatment as Vehicle/Camera/Alarm Panel/Access Credential — confirming Item Registry's own explicit forward reference ("Key Custody... will register their own Item extensions later and inherit this rather than rebuilding chain-of-custody logic each time").

This doc owns the **key side** of key↔lock: master-keying hierarchy (reusing the platform's generic `HierarchyAssociation` a third time) and the key↔lock operates-mapping, forward-referencing **Lock Core** (the next doc, which hasn't been specified yet — this doc's mapping carries a bare pointer, populated once it exists, the same seam Access Credential Management left for Clearance Profiles).

## Actors & Roles

- **Security/Access Admin** — maintains the Key/Key Ring inventory, authors master-keying hierarchy and key↔lock mappings, manages Replacement Schedules.
- **Locksmith / vendor** *(external, referenced only)* — the recipient of a Locksmith Work Order; the platform tracks the order, never the locksmith's own scheduling system.
- **Supervisor+** — requests a Locksmith Work Order.

## User Stories

- As a **Security Admin**, I want every physical key and key ring tracked as a real, identifiable asset, the same way vehicles and cameras are.
- As a **Security Admin**, I want to record that a Grand Master key operates every lock its subordinate Master and Change keys do, without re-entering the same lock list three times.
- As a **Security Admin**, I want to map a specific key to the specific lock cores it opens, even before I've fully inventoried the lock hardware itself.
- As a **Security Admin**, I want a recurring reminder — and an actual locksmith work order — generated automatically as a key or ring approaches its scheduled rekey/replacement cycle.

## Functional Requirements

### Key and Key Ring as Item extensions
1. **Key** registers as an Item extension (`key_type`: change_key, sub_master, master, grand_master, other; `key_number`, the physical stamped identifier if any) — full Item Registry identity/dedup/custody/audit treatment, unmodified.
2. **Key Ring** registers as its own Item extension (`ring_label`) — a real trackable physical object, not a bare logical grouping, since a whole ring going missing is its own custody event distinct from any single key on it.
3. **Key Ring Membership** (single-current-value EntityAssociation, Key ↔ Key Ring, the same shape as Camera's Mount Association) — a physical key sits on at most one ring at a time; moving it between rings closes the prior membership and opens a new one, preserving history.

### Master-keying hierarchy
4. A Key's place in a master-keying hierarchy (Grand Master → Master → Sub-Master → Change Key) is captured via the platform's existing generic **`HierarchyAssociation`** (Key ↔ Key) — the same mechanism Location and Organization already use for parent/child nesting, applied a third time rather than inventing a key-specific hierarchy relationship.

### Key↔Lock mapping
5. **Key-Lock Operates Mapping** registers as a new EntityAssociation kind (Key ↔ Lock Core) — Lock Core doesn't exist yet (next doc), so this carries a bare forward-referencing pointer, populated once that doc is specified, the identical seam pattern Access Credential Management left for Clearance Profiles.
6. **A key's own explicit mappings are the base truth; a higher-hierarchy key's full "what does this open" set is derived, not duplicated.** A Master key's operable-lock list is computed by rolling up its own explicit mappings plus every subordinate key's mappings via the `HierarchyAssociation` chain (#4) — a Grand Master's record is never redundantly populated with a copy of every lock its dozens of subordinate Change Keys individually open.

### Replacement scheduling
7. A **Replacement Schedule** (per Key or Key Ring) sets a recurring cadence, registering as a real Background Job Processing recurring job — per explicit user direction, built now rather than deferred to Module 10's future Calibration & Maintenance Alerts. On each trigger, it (a) sends a `key_replacement_due` Notifications Engine reminder to Security/Access Admin, and (b) auto-creates a **Locksmith Work Order** in `requested` status, tracking the request through to completion — the platform never talks to a locksmith's own scheduling system, this is a request/status ledger only, the same lightweight request-tracking weight as Patrol Request/Resource Request, not a full workflow engine.
8. A Locksmith Work Order may also be created manually (Supervisor+, not only via a Replacement Schedule trigger) for an unscheduled need (a lost key, a suspected compromise).

## Data Model / Fields

**Key** (Item extension; entity_id is the shared PK, FK → Item)
- key_type (change_key, sub_master, master, grand_master, other)
- key_number (nullable)

**Key Ring** (Item extension; entity_id is the shared PK, FK → Item)
- ring_label

**Key Ring Membership** (EntityAssociation — entity_id_a = Key, entity_id_b = Key Ring; association_id is the shared PK)
- attached_at, removed_at (nullable — null means current)

**Key-Lock Operates Mapping** (new EntityAssociation kind — entity_id_a = Key, entity_id_b = Lock Core *(forward reference, next doc)*)
- mapped_at

*(`HierarchyAssociation` reused unmodified for Key↔Key master-keying — no new fields, per Entity Registry Core's existing shape.)*

**Replacement Schedule** (feature-local)
- schedule_id, applies_to (key_ref or key_ring_ref, exactly one set), cadence, next_due_at

**Locksmith Work Order** (feature-local, lightweight)
- order_id, key_ref (nullable), key_ring_ref (nullable), status (requested, scheduled, completed)
- requested_at, requested_by, completed_at (nullable), notes

## States & Transitions

**Key / Key Ring:** ordinary Item Registry lifecycle (active/retired), unmodified.

**Key Ring Membership:** `active` (current) → `removed` (moved to a different ring or unassigned), same shape as any single-current-value association.

**Locksmith Work Order:** `requested` → `scheduled` → `completed`.

## Integrations

- **Item Registry**: base identity/dedup/custody/audit treatment for both Key and Key Ring, reused wholesale — confirms that doc's own explicit forward reference to Key Custody.
- **Entity Registry Core**: `HierarchyAssociation` reused for master-keying; a new Key-Lock Operates Mapping association kind registered.
- **Lock Core & Cylinder Tracking** (next doc, forward reference only): the target side of Key-Lock Operates Mapping; this doc's pointer is populated once that doc exists.
- **Background Job Processing**: Replacement Schedule's recurring cadence is a new consumer of the existing recurring-job registry.
- **Notifications Engine**: owns delivery of the `key_replacement_due` reminder category.
- **Key Custody & Auditing** (next doc): consumes Key/Key Ring's Item Registry custody mechanism for actual handout/return tracking — this doc stops at inventory/mapping/scheduling, never custody transfer mechanics themselves.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Supervisor+ |
|---|---|---|---|
| Create/retire Key or Key Ring records | ✅ | ✅ | ❌ |
| Author master-keying hierarchy, Key-Lock mappings | ✅ | ✅ | ❌ |
| Configure a Replacement Schedule | ✅ | ✅ | ❌ |
| Manually create a Locksmith Work Order | ✅ | ✅ | ✅ |
| View inventory/mappings | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- A Master/Grand Master key's rolled-up operable-lock list must be computed cheaply enough for routine display (e.g., viewing a Grand Master's profile) without an unreasonable join depth — a technical-spec-level concern if the hierarchy grows deep, the same caveat Entity Registry Core already carries for TPT join depth generally.
- Locksmith Work Order is a status ledger only — no SLA or vendor-integration guarantee is implied; a locksmith's own scheduling remains entirely outside the platform.

## Acceptance Criteria

- [ ] A physical key and a key ring are both independently searchable, dedup-checked Item Registry citizens with their own custody history.
- [ ] Moving a key from one ring to another closes the prior Key Ring Membership and opens a new one, with both visible in history.
- [ ] Querying a Grand Master key's operable locks returns its own explicit mappings plus every subordinate key's mappings, with no duplicated storage per lock.
- [ ] A Key-Lock Operates Mapping created before Lock Core & Cylinder Tracking exists is confirmed to hold a valid forward pointer once that doc's records exist, requiring no data migration.
- [ ] A Replacement Schedule's recurring trigger produces both a Notifications Engine reminder and a new Locksmith Work Order in `requested` status, with no manual step required.
- [ ] A manually created Locksmith Work Order (no Replacement Schedule involved) follows the identical status lifecycle as an auto-created one.

## Open Questions

- Exact cadence/unit granularity for Replacement Schedule (calendar interval vs. usage-count-based) — technical-spec.
- Whether Locksmith Work Order ever needs a real vendor-facing integration (e-mail/API to an actual locksmith service) versus staying a purely internal ledger — no target customer need identified yet; not built here.
- Whether a Key's rolled-up operable-lock computation should be cached/precomputed once hierarchies grow large — flagged as a likely technical-spec concern, not resolved here.
