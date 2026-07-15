# Lock Core & Cylinder Tracking

## Overview

Lock Core & Cylinder Tracking closes Key Ring Registry's forward reference — **Lock Core** now exists, resolving that doc's Key-Lock Operates Mapping pointer — and confirms the platform's **triad pattern** a fifth time (after Checkpoint, Camera, Alarm Panel, Environmental Sensor): **Lock Position** (a Location extension, the physical doorway/opening) + **Lock Core** (an Item extension, the swappable physical cylinder, full identity/dedup/custody/audit) linked by a single-current-value **Lock Mount Association**, mirroring Camera's Mount Association exactly — a full core swap is free history from that association, hardware replacement never severing the position's own incident/access history.

Two things a straight triad application doesn't cover on its own, both elicited: a **re-pin** (the same physical core, an internal keying change with no hardware swap) needs its own explicit record, since the Mount Association only captures *which* core is installed, not what's *inside* it; and locksmith work targeting a lock/key carries genuine additional security-sensitivity beyond an ordinary maintenance ticket, so Key Ring Registry's Locksmith Work Order is generalized and extended here rather than duplicated.

## Actors & Roles

- **Security/Access Admin** — maintains Lock Position/Core inventory, authorizes re-pins, manages restricted-keyway authorization.
- **Locksmith / vendor** *(external, referenced only)* — performs the physical work a Locksmith Work Order tracks.
- **Witness** *(any Security/Access Admin or Supervisor+ present during security-sensitive work)* — required for a Lock/Core-targeting work order's completion, per #4.

## Functional Requirements

### Triad: Lock Position + Lock Core + Mount Association
1. **Lock Position** registers as a Location extension — the fixed physical doorway/opening, following the triad's established shape exactly.
2. **Lock Core** registers as an Item extension — full identity/dedup/custody/audit treatment, the same as Vehicle/Camera/Alarm Panel/Environmental Sensor/Key.
3. **Lock Mount Association** (single-current-value EntityAssociation, Lock Position ↔ Lock Core) links the two — a full physical core swap closes the prior mount and opens a new one, giving "Core Replacement Logs" for free as that association's own history, with no separate log mechanism needed.

### Re-pinning (distinct from a swap)
4. A **Re-pin Record** (on the Lock Core itself, feature-local) captures an internal keying change to the *same* physical core — no Mount Association event occurs, since no hardware changed, so this is a genuinely separate record from #3, not a duplicate of it.

### Keyway restriction
5. A Lock Core carries `is_restricted_keyway` (bool) and an optional `keyway_authorization_ref` — a light field pair for a vendor-controlled/patented keyway system requiring authorization to duplicate or re-key, rather than a full separate restriction-tracking subsystem.

### Completing Key Ring Registry's forward reference
6. This doc's Lock Core is the real target of Key Ring Registry's **Key-Lock Operates Mapping** — that association's forward-referencing pointer resolves here with no data migration required, the same seam-closing pattern Clearance Profiles used for Access Credential Management's `clearance_profile_ref`.

### Locksmith Work Order, generalized and security-extended
7. *(Retrofit — Key Ring Registry)* **Locksmith Work Order**'s `applies_to` generalizes beyond Key/Key Ring to also accept a Lock Position/Core target — one shared locksmith-request ledger across both docs rather than two nearly-identical parallel mechanisms.
8. A Lock/Core-targeting Locksmith Work Order carries genuine additional security-sensitivity beyond an ordinary maintenance ticket, per explicit user direction — it gains `witnessed_by` (a second Security/Access Admin or Supervisor+ present during the work) and `security_verified_by`/`security_verified_at`, both required before such an order can reach `completed`; a Key/Key Ring-targeting order (already covered in Key Ring Registry) is unaffected and carries neither field.

## Data Model / Fields

**Lock Position** (Location extension; entity_id is the shared PK, FK → Location)
- coverage_note (optional — which opening/doorway)

**Lock Core** (Item extension; entity_id is the shared PK, FK → Item)
- is_restricted_keyway (bool), keyway_authorization_ref (nullable)

**Lock Mount Association** (EntityAssociation — entity_id_a = Lock Position, entity_id_b = Lock Core; association_id is the shared PK)
- mounted_at, removed_at (nullable — null means current)

**Re-pin Record** (feature-local, on Lock Core)
- repin_id, core_ref, performed_at, performed_by
- reason, keying_note (free text), locksmith_work_order_ref (nullable)

**Key-Lock Operates Mapping** *(retrofit — Key Ring Registry)*
- entity_id_b now resolves to a real Lock Core.

**Locksmith Work Order** *(retrofit — Key Ring Registry)*
- applies_to gains lock_position_ref/lock_core_ref (mutually exclusive with the existing key_ref/key_ring_ref)
- witnessed_by (nullable — required for a Lock/Core-targeting order to complete)
- security_verified_by (nullable), security_verified_at (nullable — required for a Lock/Core-targeting order to complete)

## States & Transitions

**Lock Position / Lock Core:** ordinary Item/Location Registry lifecycle, unmodified.

**Lock Mount Association:** `active` (current mount) → `removed` (core swapped out), same shape as Camera's Mount Association.

**Locksmith Work Order** *(retrofit)*: `requested` → `scheduled` → `completed` — a Lock/Core-targeting order additionally requires `witnessed_by` and `security_verified_by`/`security_verified_at` populated before it may transition to `completed`; a Key/Key Ring-targeting order's transition is unaffected.

## Integrations

- **Location Registry / Item Registry**: base triad treatment, reused wholesale.
- **Key Ring Registry** *(retrofit)*: Key-Lock Operates Mapping's forward pointer resolves to this doc's Lock Core; Locksmith Work Order is generalized and security-extended here.
- **Unified Operational Picture (UOP) Map** *(retrofit)*: gains a Lock Positions pin layer, off by default alongside Camera Positions/Alarm Zones, following the same triad-pin-layer convention.
- **Structured Logging & Audit Trails**: every Mount Association change, re-pin, and Locksmith Work Order state transition is audit-tier.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Witness (Supervisor+/Admin) |
|---|---|---|---|
| Create/mount/replace a Lock Position or Lock Core | ✅ | ✅ | ❌ |
| Record a re-pin | ✅ | ✅ | ❌ |
| Create a Locksmith Work Order (any target) | ✅ | ✅ | ❌ |
| Provide witness/security verification on a Lock/Core order | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- A Lock/Core-targeting Locksmith Work Order's `completed` transition must be blocked at the platform level (not just UI-discouraged) until both `witnessed_by` and `security_verified_by`/`security_verified_at` are populated.
- A restricted-keyway Lock Core's `keyway_authorization_ref` is informational tracking only — the platform never itself enforces or validates the vendor's own authorization process.

## Acceptance Criteria

- [ ] Swapping a Lock Core at a Lock Position closes the prior Mount Association and opens a new one, giving a correct core-replacement history with no separate log entry required.
- [ ] Recording a re-pin on a Lock Core that stays physically mounted produces no Mount Association change, confirming the two are genuinely distinct records.
- [ ] A Key-Lock Operates Mapping created in Key Ring Registry before this doc existed resolves correctly to a real Lock Core with no migration step.
- [ ] A Locksmith Work Order can target either a Key/Key Ring or a Lock Position/Core, never both at once.
- [ ] A Lock/Core-targeting Locksmith Work Order cannot reach `completed` without both witness and security verification recorded; a Key/Key Ring-targeting order completes without either.
- [ ] UOP Map's Lock Positions pin layer is off by default and shows the same pin-click/permission behavior as Camera Positions and Alarm Zones.

## Open Questions

- Exact re-pin data capture depth (structured pin-configuration data vs. free-text keying notes) — technical-spec, likely bounded by what real locksmith documentation practice actually captures.
- Whether restricted-keyway authorization should ever gate (not just record) a re-key/duplication request — no target customer need identified for enforcement beyond tracking; not built here.
- Whether Lock Position ever needs its own forced/tamper signal distinct from Alarm Panel Monitors' Alarm Zone concept — deliberately kept separate here; a forced door is that doc's territory, not this one's, to avoid duplicating intrusion-detection modeling.
