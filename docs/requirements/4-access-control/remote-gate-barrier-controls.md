# Remote Gate & Barrier Controls

## Overview

Remote Gate & Barrier Controls closes out Module 4 (10/10) with two more confirmed triad-pattern instances and the same "invoke the upstream API, never own the hardware" boundary Camera PTZ already established. **Gate/Barrier** (Position + Operator, the sixth triad instance) and **Intercom** (Position + Unit, the seventh — deliberately kept separate from Camera per explicit user direction, even though the physical shape is similar, since a call/buzz is functionally distinct from a video feed) both get full Item/Location treatment. **Remote Open** is a confirmation-gated pass-through invocation of the PACS/gate-controller's own API (a new `gate_control` capability on PIAM Adaptor Registration) — the platform never speaks relay/actuator protocols natively, the identical boundary already drawn for PTZ. An incoming intercom call becomes a fourth trigger source for Live Camera Feed Ingestion's existing Camera Auto-Popup Mapping, so a Guard gets eyes on a buzzer press via the nearest mapped camera without a new visual mechanism.

## Actors & Roles

- **Dispatcher / Lobby Guard** — presses Remote Open, answers an intercom call.
- **Security/Access Admin** — configures Barrier Confirmation Policy, provisions Gate/Barrier and Intercom hardware records.
- **Site/Tenant Admin** — configures the PIAM Adaptor's `gate_control` capability.

## Functional Requirements

### Gate/Barrier as a triad
1. **Gate/Barrier Position** registers as a Location extension — the physical opening (a vehicle gate, dock door, or lobby turnstile).
2. **Gate/Barrier Operator** registers as an Item extension — the powered hardware (motor/relay), full identity/dedup/custody/audit treatment, the sixth confirmed triad instance.
3. **Gate Mount Association** (single-current-value EntityAssociation, Position ↔ Operator) links the two, mirroring every prior triad exactly — an operator swap never severs the position's own history.
4. Position carries `barrier_type` (vehicle_gate, dock_door, turnstile, other), driving the confirmation policy (#6).

### Remote Open
5. **Open Barrier** registers as a Command/Action Bus action — a pass-through invocation of the connected PACS/gate-controller's own API via PIAM Adaptor Registration's new `gate_control` capability, never a native relay/actuator protocol implementation — the identical boundary Live Camera Feed Ingestion's PTZ control already drew. Where no write-capable adaptor covers a barrier, no such action exists at all (the same "absent, not disabled" posture PTZ established for an adaptor with no PTZ capability).
6. A tenant-configurable **Barrier Confirmation Policy** (Settings & Preferences Definition, by `barrier_type`) sets whether Open Barrier requires confirmation — per explicit user direction, a genuine departure from Camera PTZ's unconditional stance: a vehicle gate or dock door may stay always-confirmation-gated while a lobby turnstile is configured lighter-weight, the module's recurring configurable-policy shape applied to physical consequence itself.
7. Every Open Barrier invocation is audit-tier — an immutable record of who triggered it, when, and under what confirmation state — closing MODULES.md's "Override Auditing" bullet directly via the platform's existing audit-tier discipline, no new logging mechanism required.

### Intercom, kept separate from Camera
8. **Intercom Position** (Location extension) and **Intercom Unit** (Item extension, full custody/dedup/audit) form their own triad, deliberately independent of Camera Position/Camera — per explicit user direction, a call/buzz is functionally distinct from a video feed even where the hardware co-locates.
9. An incoming intercom call registers as a **fourth trigger source** for Live Camera Feed Ingestion's existing Camera Auto-Popup Mapping *(retrofit)* — alongside Signal Disposition, Geofence, and Duration Watchdog — resolving which nearby Camera feed(s) auto-surface so a Guard gets visual context on who's calling, without a new visual mechanism.
10. **Answer Intercom** registers as a lighter-weight Command/Action Bus action (talking isn't the consequential act — opening the barrier is) — a pass-through audio/video call-answer via the adaptor, independent of and never implicitly triggering Open Barrier.

## Data Model / Fields

**Gate/Barrier Position** (Location extension; entity_id is the shared PK, FK → Location)
- barrier_type (vehicle_gate, dock_door, turnstile, other)

**Gate/Barrier Operator** (Item extension; entity_id is the shared PK, FK → Item)

**Gate Mount Association** (EntityAssociation — entity_id_a = Gate/Barrier Position, entity_id_b = Gate/Barrier Operator; association_id is the shared PK)
- mounted_at, removed_at (nullable)

**Intercom Position** (Location extension; entity_id is the shared PK, FK → Location)

**Intercom Unit** (Item extension; entity_id is the shared PK, FK → Item)

**Intercom Mount Association** (EntityAssociation — entity_id_a = Intercom Position, entity_id_b = Intercom Unit; association_id is the shared PK)
- mounted_at, removed_at (nullable)

**Barrier Confirmation Policy** (Settings & Preferences Definition)
- tenant_id/site_id, barrier_type, confirmation_required (bool)

**PIAM Adaptor Registration** *(retrofit)*
- sync_capabilities{} gains `gate_control` (bool)

## States & Transitions

**Gate/Barrier Position / Operator, Intercom Position / Unit:** ordinary Item/Location Registry lifecycle, unmodified. **Mount Associations:** `active` → `removed`, the same shape as every prior triad.

## Integrations

- **Location Registry / Item Registry**: base triad treatment for both Gate/Barrier and Intercom, reused wholesale.
- **PIAM Adaptor (multi-vendor)**: `gate_control` is a new capability alongside `visitor_sync`/`credential_sync`/`clearance_sync` — the same vendor set (CCure, Safelok, HID SAFE, others) already commonly controls gate/turnstile relays alongside badge readers, so this extends the existing adaptor family rather than a new one.
- **Live Camera Feed Ingestion** *(retrofit)*: gains intercom call as a fourth Camera Auto-Popup Mapping trigger source.
- **Command/Action Bus**: Open Barrier (confirmation-gated per policy) and Answer Intercom (lighter-weight) both register as actions.
- **Settings & Preferences**: owns Barrier Confirmation Policy.
- **Unified Operational Picture (UOP) Map** *(retrofit)*: gains Gate/Barrier and Intercom pin layers, off by default, the same triad-pin-layer convention as Camera/Alarm Zone/Lock Position.
- **Structured Logging & Audit Trails**: every Open Barrier and Answer Intercom invocation is audit-tier.

## Permissions

| Action | Site/Tenant Admin | Security/Access Admin | Dispatcher/Lobby Guard |
|---|---|---|---|
| Provision Gate/Barrier or Intercom hardware, configure Barrier Confirmation Policy, PIAM Adaptor `gate_control` | ✅ | ✅ | ❌ |
| Invoke Open Barrier | ✅ | ✅ | ✅ |
| Answer Intercom | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- Open Barrier is unavailable, not merely disabled, for any barrier with no `gate_control`-capable adaptor — the same "absent, not disabled" posture as PTZ.
- Barrier Confirmation Policy's per-`barrier_type` resolution must be enforced server-side, never a client-side toggle a console could bypass.
- Open Barrier's confirmation-gate UX inherits the platform's standard confirmation-gate treatment; no separate latency target is defined here beyond the standard confirmation-gate UX baseline.

## Acceptance Criteria

- [ ] Swapping a Gate/Barrier Operator at a Position closes the prior Mount Association and opens a new one, with no loss of the position's own history.
- [ ] Invoking Open Barrier on a `vehicle_gate` under a policy requiring confirmation cannot proceed without passing the confirmation gate; a `turnstile` configured without confirmation proceeds immediately.
- [ ] Every Open Barrier invocation, regardless of confirmation outcome, produces a discoverable, immutable audit-tier record.
- [ ] An incoming intercom call at a Position with a mapped nearby Camera auto-surfaces that Camera's feed, per the existing Camera Auto-Popup Mapping resolution chain.
- [ ] Answering an intercom call never itself opens the associated barrier — the two actions are confirmed independent.
- [ ] A barrier with no `gate_control`-capable adaptor shows no Open Barrier action at all, not a disabled one.

## Open Questions

- Exact gate-controller vendor coverage at launch for `gate_control` — technical-spec/roadmap, vendor-dependent like every other adaptor capability in this module.
- Whether a barrier ever needs its own forced/left-open signal distinct from Alarm Panel Monitors' Alarm Zone concept — same boundary Lock Core & Cylinder Tracking already drew for lock positions; kept out of this doc's scope for the identical reason.
- Exact intercom audio/video call-answer latency/quality expectations — technical-spec, adaptor-dependent.
