# Alarm Panel Monitors & Panic Alerts

## Overview

Alarm Panel Monitors & Panic Alerts is Module 3's Command-Center consumption side of physical alarm-panel signals (intrusion, fire, panic-button) — per the same integrate-don't-replace boundary Live Camera Feed Ingestion established, it never speaks alarm-panel protocols directly. Module 19's future **Intrusion Alarm IP Listeners** and **Fire Panel Watchdogs** own the real vendor adaptors; every ingested signal here flows through Activity Registry's existing **Signal Disposition** valve, exactly like any other Module 19 integration.

Alarm Zone/Panel reuses Camera's triad pattern, adapted for real-world alarm topology: **Alarm Zone** (a Location extension — one monitored point, a door or glass-break sensor) and **Alarm Panel** (an Item extension — the physical controller hardware, full identity/dedup/custody/audit treatment like Vehicle/Camera) are linked by an **Alarm Panel Mount Association**. Unlike Camera's strict 1:1 mount, one physical Panel commonly monitors many Zones simultaneously, so multiplicity here is constrained only on the Zone side.

Panic Alerts have two genuinely distinct sources: a **panel-integrated panic button** (a physical duress button wired into an Alarm Panel, ingested exactly like any intrusion/fire signal through Signal Disposition) and a **mobile app SOS button** — platform-native, no vendor/adaptor involved at all, its own Activity extension with a deliberate, explicitly-flagged exception to the platform's confirmation-gate discipline: triggering it must never require hesitation.

## Actors & Roles

- **Guard / Officer** — triggers mobile SOS; may also stand down their own false alarm.
- **Dispatcher / Supervisor / EOC Coordinator** — monitors Alarm Zone status, acknowledges/stands down alarms, responds to SOS.
- **Site / Tenant Admin** — registers Alarm Zones/Panels, configures Signal Disposition mappings and zone categories.

## User Stories

- As a **Dispatcher**, I want tripped door/glass-break zones to show live on a dedicated alarm monitor panel and as pins on UOP Map, without hunting through a separate vendor console.
- As a **Dispatcher**, I want a fire panel status change to visually and audibly stand out from a routine intrusion trip, given the life-safety stakes.
- As a **Guard**, I want to press SOS on my phone in a genuine emergency and have it fire instantly — no confirmation step, no hesitation.
- As a **Supervisor**, I want an SOS trigger to immediately show me the officer's last known position and auto-create an Incident, so I'm not manually building one from scratch mid-crisis.
- As a **Supervisor**, I want to stand down a false SOS trigger cleanly, silencing it on every console, without erasing that it happened.
- As a **Tenant Admin**, I want a panel-integrated panic button to auto-dispatch by default, since a physical duress trip shouldn't wait on manual triage.
- As a **Tenant Admin**, I want to replace an aging alarm panel controller without losing any of its zones' incident history.

## Functional Requirements

### Alarm Zone & Alarm Panel (triad pattern, adapted)
1. **Alarm Zone** registers as a Location extension — one monitored point (a door, a glass-break sensor, a panic-button location), carrying a `zone_category` (intrusion, fire, panic, generic — tenant-extensible) used for console/map styling and as a Signal Disposition input.
2. **Alarm Panel** registers as an Item extension, inheriting Item Registry's base identity/dedup/custody/audit treatment unmodified — same treatment as Vehicle/Camera, since it's real tracked hardware.
3. An **Alarm Panel Mount Association** (EntityAssociation, Alarm Zone ↔ Alarm Panel) links the two, adapted from Camera's strict 1:1 triad: multiplicity is constrained only on the Zone side (at most one active Panel per Zone at a time) since one physical Panel commonly monitors many Zones simultaneously — replacing a Panel controller closes every affected Zone's current Mount Association and opens new ones, preserving each Zone's own identity and incident history unaffected.
4. Alarm Panel carries adaptor-reference fields (`external_panel_id`, `panel_adaptor_ref`) as a deliberately interim, Admin-entered stand-in for Module 19's future adaptors — flagged for reconciliation once those modules exist, same posture as Camera's VMS-reference fields.

### Signal ingestion & disposition
5. Every Alarm Zone's tripped/state-change signal flows through Activity Registry's existing Signal Disposition valve unmodified (`display_only`/`telemetry`/`activity`/`incident_dispatch`) — this doc introduces no new signal pipeline, only consumes the established contract. A panel-integrated panic-button Zone's default disposition is recommended at `incident_dispatch` given duress severity, tenant-configurable like any other Signal Disposition instance; exact out-of-box defaults per `zone_category` are a content/config concern, not committed here.

### Console & map surfaces
6. A new **`alarm_monitor`** panel type registers into the shared Panel Registry (the catalog Multi-Incident Console, Command Center Wallboard View, ICS Role Mapping, and Live Camera Feed Ingestion already contribute to) — selectable in a personal Console Layout, a Wallboard Display Profile zone, or opened directly.
7. UOP Map *(retrofit)* gains Alarm Zone as an optional pin layer, styled by `zone_category` and current state (armed, disarmed, alarm, trouble/fault) — mirroring Camera Positions' own pin-layer treatment exactly.

### Panic Alerts — panel-integrated
8. A panel-integrated panic button is an ordinary Alarm Zone (`zone_category = panic`) — its trip event is not architecturally different from any other Signal Disposition-consumed signal; no separate mechanism is introduced for it.

### Panic Alerts — mobile SOS
9. **SOS Alert** registers as its own thin Activity extension, self-logged the instant an officer presses the mobile app's SOS button — triggering it deliberately bypasses the platform's standard confirmation gate, an explicit, flagged exception to that default discipline given the whole point of a panic button is zero hesitation.
10. Triggering captures the officer's current position (GIS Position Record — live GPS if available, else last-known) at the moment of trigger, immediately raises a persistent Alarm State via Real-Time Delivery's existing Alarm State Service (the same mechanism Duration Watchdog persistent alarms already use — reconnecting consoles re-fire it if still unacknowledged, exactly like any other persistent alarm), and automatically creates an Incident via the platform's established launch-point mechanism, location pre-filled from the captured position, `escalated_from_ref` set to the SOS Alert.
11. **Stand Down** (self or Supervisor/EOC Coordinator on-behalf-of) acknowledges/silences the Alarm State on every console — an ordinary action, not itself confirmation-gated given it's de-escalating, not escalating. Standing down never deletes or hides the SOS Alert or its spawned Incident — a real event occurred even if resolved as a false alarm, the same negative-outcome-gets-a-real-row discipline used throughout the platform.

## Data Model / Fields

**Alarm Zone** (Location extension; entity_id is the shared PK, FK → Location)
- zone_category (intrusion, fire, panic, generic), panel_adaptor_ref

**Alarm Panel** (Item extension; entity_id is the shared PK, FK → Item)
- external_panel_id, panel_adaptor_ref

**Alarm Panel Mount Association** (EntityAssociation — entity_id_a = Alarm Zone, entity_id_b = Alarm Panel; association_id is the shared PK)
- mounted_at, removed_at (nullable — null means current mount; at most one active row per Zone)

**SOS Alert** (thin Activity extension; entity_id is the shared PK)
- triggered_by (Person ref), triggered_at, captured_position_ref (GIS Position Record)
- status (active, stood_down), stood_down_by (nullable), stood_down_at (nullable)
- spawned_incident_ref (set at creation, via the launch-point mechanism)

## States & Transitions

- **Alarm Zone:** follows base Location lifecycle, unmodified; live state (armed/disarmed/alarm/trouble) is a Signal Disposition-consumed telemetry value, not stored here as a field of its own.
- **Alarm Panel:** follows base Item lifecycle, custody inherited unmodified from Item Registry.
- **Alarm Panel Mount Association:** `active` → `removed`, identical shape to Camera Mount Association.
- **SOS Alert:** `active` → `stood_down` (terminal; the record and its spawned Incident both persist).

## Integrations

- **Location Registry**: Alarm Zone's base geometry/coordinates, reused wholesale.
- **Item Registry**: Alarm Panel's base identity/dedup/custody/audit treatment, reused wholesale.
- **Entity Registry Core**: Alarm Panel Mount Association's base EntityAssociation shape, adapted from Checkpoint/Camera's precedent.
- **Activity Registry (Signal Disposition)**: every Alarm Zone signal's promotion path, reused unmodified — this doc owns no signal pipeline of its own.
- **GIS & Mapping Services / Unified Operational Picture (UOP) Map**: Alarm Zone pin layer (retrofit); GIS Position Record is the source of SOS Alert's `captured_position_ref`.
- **Real-Time Delivery & Server-Side Timers**: SOS Alert's persistent Alarm State reuses that service's existing mechanism and reconnect-refire rule unmodified.
- **Multi-Incident Console / Command Center Wallboard View**: `alarm_monitor` panel type contributed to the shared Panel Registry.
- **Incident Reporting & Management**: SOS Alert's spawned Incident uses the platform's existing launch-point mechanism, no new escalation logic.
- **Command/Action Bus**: Stand Down registers as an action; SOS Alert's own trigger deliberately does *not* register as a gate-checked action, the explicit exception noted in FR #9.
- **Module 19 — Intrusion Alarm IP Listeners, Fire Panel Watchdogs (not yet specified)**: forward reference — the eventual real adaptors; Alarm Panel's adaptor-reference fields are an explicit interim stand-in, flagged for reconciliation once those modules exist.

## Permissions

- **Register/mount/replace an Alarm Zone or Alarm Panel**: Site/Tenant Admin.
- **Configure Signal Disposition mappings for Alarm Zone signals**: Site/Tenant Admin (existing Signal Disposition permission posture, unmodified).
- **Trigger a mobile SOS Alert**: any on-duty Guard/Officer, no gate.
- **Stand down an SOS Alert**: the triggering officer themselves, or Supervisor/EOC Coordinator on-behalf-of.
- **View Alarm Zone status / the `alarm_monitor` panel**: inherits the Zone's own Location-based RBAC/ABAC — no new permission introduced.

## Non-Functional / Constraints

- SOS Alert's position capture, Alarm State raise, and Incident creation must all complete within the platform's standard ≤2s safety-relevant latency target — this is explicitly a safety-critical path, not a routine console update.
- SOS Alert's trigger is the one documented, deliberate exception to the platform-wide confirmation-gate default — flagged here so a future feature doesn't quietly assume the same exception applies elsewhere without its own explicit justification.
- No native alarm-panel protocol infrastructure is built by the platform — this doc is strictly a consumption/monitoring layer over Signal Disposition and Module 19's future adaptors.

## Acceptance Criteria

- [ ] An Alarm Zone's tripped state renders on the `alarm_monitor` panel and, when the layer is enabled, as a UOP Map pin styled by its `zone_category`.
- [ ] Replacing an Alarm Panel controller preserves every affected Alarm Zone's identity and incident history; each Zone's prior Mount Association closes and a new one opens.
- [ ] A panel-integrated panic-button Zone's trip flows through the same Signal Disposition mechanism as any other Alarm Zone signal, with no special-cased pipeline.
- [ ] Pressing the mobile app's SOS button fires immediately with no confirmation step, capturing the officer's current position and raising a persistent Alarm State within the platform's standard safety-relevant latency target.
- [ ] An SOS Alert automatically creates an Incident with location pre-filled and `escalated_from_ref` set, with no manual step required.
- [ ] Standing down an SOS Alert silences its Alarm State on every subscribed console but leaves the SOS Alert record and its spawned Incident fully intact and queryable.
- [ ] A console reconnecting while an SOS Alert's Alarm State is still active and unacknowledged audibly re-fires it, per Real-Time Delivery's existing reconnect rule.

## Open Questions

- Exact out-of-box Signal Disposition defaults per `zone_category` (e.g., whether fire defaults stricter than intrusion) — content/config design, not committed here.
- Whether a "silent SOS" mode (triggering without any audible/visible cue on the officer's own device, for a coercion scenario) is worth adding — not requested, not committed here; flagged as a plausible future refinement.
- Exact reconciliation path once Module 19's real Intrusion Alarm IP Listeners / Fire Panel Watchdogs adaptors are specified — how Alarm Panel's interim reference fields migrate — not solved here.
