# Patrol Management

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Patrol Management is the **umbrella assignment layer** Guard Tour & Checkpoint Verification sits underneath. Where Guard Tour owns checkpoint/route/scan mechanics, Patrol Management owns the higher-level question of "what kind of post is this guard or unit working" and the assignment itself — a **Post**, one of two kinds:

- **Fixed Post** — a guard stationed at a location, whose local patrols are literally a Guard Tour Route Assignment executing underneath the Post (per the earlier example: 4 tours of the same fixed-post location, spaced 1:30 apart, is one Tour Definition under one Route Assignment tied to that Post).
- **Mobile Patrol Unit** — a guard (and optionally a Vehicle, per Vehicle/Conveyance Registry) covering a broader patrol area or multiple sites, who may or may not also run checkpoint-based Guard Tour Routes on top of free-roam coverage.

Since Post Schedule Builder (Module 8) doesn't exist yet, **Post** is a deliberately lightweight, interim concept here — the same deferred-integration posture DAR took with Shift Window — flagged for reconciliation once real scheduling infrastructure exists.

A Mobile Patrol Unit's location tracking is **configurable, with a graceful fallback chain**: continuous GPS breadcrumb tracking when enabled and available; failing that, periodic scan/check-in updates (reusing Guard Tour's Checkpoint Scan mechanism where checkpoints exist along the patrol area, or a simpler manual check-in where they don't); failing that, a Supervisor or Dispatcher manually recording the unit's last known position from a radio check. All three update the same `last_known_location` on the unit — the platform never assumes GPS is always available.

**Ad hoc patrol requests are in scope now** (not deferred to Dispatch/CAD, since Module 2 doesn't exist yet and there's a real near-term need to task an available unit to an unplanned location) — a lightweight request → assign → fulfill flow, explicitly built so it becomes a natural dispatchable target once Dispatch/CAD exists, not something that module has to retrofit around.

## Actors & Roles

- **Guard** — works an assigned Post (fixed or mobile), executes Guard Tour Routes under it, updates their own Mobile Patrol Unit's location via check-in when GPS isn't active.
- **Supervisor** — creates/assigns Posts, creates and assigns ad hoc Patrol Requests, manually updates a unit's last known location from a radio check, configures a Post's tracking mode.
- **Tenant Admin** — sets tenant-default tracking-mode policy, configures baseline Domain Events rules (e.g., stale-location alerts).
- **Records Admin** — resolves Entity Registry Core dedup flags on Patrol Request (as an Activity extension), same as any other Activity type.

## User Stories

- As a **Supervisor**, I want to create a Fixed Post at the main gate and assign a guard and a Guard Tour Route to it in one place, rather than juggling separate assignment and route-assignment screens.
- As a **Supervisor**, I want to stand up a Mobile Patrol Unit covering our whole parking structure, assign a vehicle and a guard to it, and see its live position on the map.
- As a **Guard on a Mobile Patrol Unit** in a GPS-dead area, I want my periodic check-ins to still update my known location, so my Supervisor isn't flying blind.
- As a **Dispatcher-equivalent Supervisor**, I want to radio a mobile unit, confirm their position verbally, and log that as their current location when neither GPS nor a check-in is available.
- As a **Supervisor**, I want to send an available Mobile Patrol Unit to check out a report at Building C right now, without waiting for a full CAD system to exist.
- As a **Site Manager**, I want to see, at a glance, which Posts are currently staffed, fixed vs. mobile, and their current status.

## Functional Requirements

### Post (interim, pending Post Schedule Builder)
1. **Post** declares `post_type` (fixed, mobile), a name, and either a `location_ref` (fixed posts — a specific place) or a `patrol_area_ref` (mobile — a broader Location, e.g. a Zone or multi-building Site, representing the unit's coverage area).
2. A Post has an assignment: `assigned_person_ref` (Guard) and, for mobile posts, an optional `assigned_vehicle_ref` (per Vehicle/Conveyance Registry — a mobile unit isn't necessarily vehicle-based; foot/bike patrol is valid with no vehicle).
3. A Post's assignment period is a start/end time range, optionally linked to a DAR Shift Window when the assigned Guard's shift is being clocked — not required, since not every Post assignment corresponds 1:1 to a tracked shift.
4. A Fixed Post's local patrols are fulfilled by a Guard Tour Route Assignment referencing this Post (`post_ref`, added to Guard Tour's existing Route Assignment — see cross-doc note below); a Mobile Patrol Unit may optionally have Guard Tour Routes assigned the same way, or operate with no fixed checkpoints at all.

### Mobile Patrol Unit tracking
5. A Mobile Patrol Unit's `tracking_mode` is configurable per unit (defaulting from a tenant-wide Settings & Preferences value): `gps_continuous`, `scan_checkin`, or `radio_manual`.
6. Regardless of mode, the unit maintains a single `last_known_location` (+ timestamp + source) — GPS breadcrumb updates it continuously when active; a Checkpoint Scan or simple manual check-in updates it periodically; a Supervisor/Dispatcher manually recording a radio check updates it directly, always tagged with which source produced it.
7. A unit's location falls back through the chain automatically — if GPS drops, the last GPS-sourced position remains the best-known location (with its age visibly stale) until a check-in or manual update refreshes it; the platform never silently fabricates a position.

### Ad hoc Patrol Request
8. A **Patrol Request** registers as an Activity extension: `requested_by`, `target_location_ref`, `priority`, `notes`, and a lifecycle from `requested` through `assigned` → `en_route` → `completed` (or `cancelled`).
9. A Supervisor (or future Dispatcher role) assigns an available Post (typically a Mobile Patrol Unit, though a Fixed Post's guard can be temporarily diverted) to fulfill the request.
10. Fulfillment is logged directly on the Patrol Request (arrival timestamp, completion note); the assigned unit may optionally also perform Guard Tour checkpoint scans while there if the target location has any, but that's incidental, not required.
11. Creating a Patrol Request publishes an automation-eligible domain event so a Tenant Admin can configure notification/escalation behavior (e.g., notify the assigned unit immediately) via Domain Events, consistent with the platform's "trigger/condition owned by Domain Events, effect owned by Command/Action Bus" split.

## Data Model / Fields

**Post** (interim, feature-local — not an Entity Registry Core citizen; revisit once Post Schedule Builder exists)
- post_id, tenant_id, post_type (fixed, mobile)
- name, location_ref (fixed) or patrol_area_ref (mobile)
- assigned_person_ref, assigned_vehicle_ref (nullable, mobile only)
- assignment_start, assignment_end (nullable), shift_window_ref (nullable)
- tracking_mode (mobile only: gps_continuous, scan_checkin, radio_manual — defaults from Settings & Preferences)
- last_known_location (geo-point or location_ref), last_known_location_source (gps, scan, radio_check), last_known_location_at
- status (active, ended)

**Patrol Request** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- requested_by, target_location_ref, priority
- assigned_post_ref (nullable until assigned)
- notes
- courtesy_patrol_ref (nullable — FK → Courtesy Patrol, when this request's fulfillment detail was logged as one; retrofit, see [courtesy-patrol.md](courtesy-patrol.md))
- source_call_ref (nullable — FK → Call, when this request originated from CAD call intake rather than a self-initiated or direct Supervisor tasking; retrofit, see [call-intake-logging.md](../2-dispatch-cad/call-intake-logging.md))
- requested_at, assigned_at (nullable), completed_at (nullable)

## States & Transitions

**Post:** `active` (assigned, within assignment window) → `ended` (assignment period closes).

**Patrol Request:** `requested` → `assigned` → `en_route` → `completed` | `cancelled` (cancellable from `requested` or `assigned`).

**Post `last_known_location`:** updated in place on every GPS ping / check-in / manual update — not a versioned history table (age/staleness is derived from `last_known_location_at`, not a log of every prior position); a full breadcrumb trail, if retained, is a GIS & Mapping Services-level concern (map track history), not owned here.

## Integrations

- **Guard Tour & Checkpoint Verification**: Route Assignment gains an optional `post_ref` field (retrofit — see cross-doc note) linking a checkpoint-based route to the Post it fulfills; a Fixed Post's patrols are executed entirely through that existing mechanism.
- **Vehicle/Conveyance Registry**: source of `assigned_vehicle_ref` for a vehicle-based Mobile Patrol Unit.
- **Party Registry (Person)**: source of `assigned_person_ref`.
- **Location Registry**: source of both `location_ref` (fixed posts) and `patrol_area_ref` (mobile coverage areas).
- **Daily Activity Reports (DAR)**: a Patrol Request, once created, is an ordinary Activity and is automatically picked up by any DAR filter matching its assigned guard/site/time window.
- **Shift Passdowns & Handover Notes**: an incomplete/cancelled Patrol Request or a Post ending with a stale `last_known_location` are natural default Pass-On Rule candidates, configured the same way as Guard Tour's missed-tour flag — no bespoke integration needed.
- **Settings & Preferences**: owns the tenant-default `tracking_mode` policy.
- **Domain Events / Notifications Engine**: Patrol Request creation, and a unit's `last_known_location` going stale past a configurable threshold, both publish automation-eligible events rather than a hardcoded alert path.
- **GIS & Mapping Services**: renders Post locations/coverage areas and each Mobile Patrol Unit's current `last_known_location`; continuous GPS breadcrumb trail rendering (when `tracking_mode = gps_continuous`) is a GIS-level concern consuming this doc's location updates, not implemented here.
- **Command/Action Bus**: "Create Post," "Assign Post," "Update unit location (manual)," "Request patrol," "Assign patrol request" register as invokable actions across every surface.
- **Call Intake & Logging (Module 2 Dispatch/CAD)**: as anticipated, Call now generalizes/supersedes Patrol Request as the CAD front door; Patrol Request gains `source_call_ref` (retrofit above) so a triaged Call can result in direct-unit tasking here, while Patrol Request keeps its own simpler lifecycle for tasking that bypasses full call intake entirely.
- **Courtesy Patrol**: when a Patrol Request is fulfilled as a courtesy service (an escort, jump start, tire change, welfare check), the assigned unit's detailed record is a Courtesy Patrol, optionally referenced back via `courtesy_patrol_ref` — Patrol Request stays the generic dispatch wrapper; Courtesy Patrol owns the richer category-specific detail.
- **Post Schedule Builder (Module 8, future)**: intended eventual replacement/reconciliation point for Post's interim assignment model, same deferred-integration posture as DAR's Shift Window.

**Cross-doc retrofit note:** [guard-tour-checkpoint-verification.md](guard-tour-checkpoint-verification.md)'s **Route Assignment** data model gains an optional `post_ref` field (FK → this doc's Post) so a checkpoint-based route can declare which Post it's fulfilling. This is additive — Route Assignment's existing `person_ref`/time-range fields are unchanged, and a Route Assignment with no `post_ref` remains valid for a guard/route pairing with no Post concept in play.

## Permissions

| Action | Guard | Supervisor | Tenant Admin |
|---|---|---|---|
| Create/end a Post, assign guard/vehicle | ❌ | ✅ | ✅ |
| Configure a Post's tracking mode | ❌ | ✅ | ✅ (tenant default) |
| Manually update a unit's location (radio check) | ❌ | ✅ | ✅ |
| Check in (scan/manual) to update own location | ✅ (own Post) | ✅ (own Post, if assigned) | ❌ |
| Create a Patrol Request | ❌ | ✅ | ✅ |
| Assign a Patrol Request to a Post | ❌ | ✅ | ✅ |
| Fulfill/complete an assigned Patrol Request | ✅ (own assignment) | ✅ (own assignment) | ❌ |
| Configure stale-location / tracking-mode defaults | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- GPS breadcrumb updates (when active) must be battery/bandwidth-conscious on mobile devices — likely governed by Settings & Preferences' existing Network Profile mechanism rather than a bespoke throttling scheme here.
- A stale `last_known_location` must be visually distinguishable (age-based) wherever it's rendered, never presented as if it were current.
- Check-in and manual location updates must work fully offline / degrade gracefully to eventual sync, consistent with the platform's general offline model.
- Patrol Request assignment must prevent double-assignment of the same Post to two simultaneously active requests without an explicit override — a technical-spec-level concern.
- WCAG 2.1 / Section 508 accessible Post management, check-in, and patrol-request flows, day one.

## Acceptance Criteria

- [ ] Creating a Fixed Post and assigning a Guard Tour Route to it via `post_ref` correctly ties the route's execution to that Post.
- [ ] Creating a Mobile Patrol Unit with `tracking_mode = gps_continuous` updates `last_known_location` from device GPS pings.
- [ ] Switching a unit to `tracking_mode = scan_checkin` (e.g., GPS unavailable) correctly updates `last_known_location` from check-in events instead.
- [ ] A Supervisor manually recording a radio-check position updates `last_known_location` with `source = radio_check` and the correct timestamp.
- [ ] A unit with no location update past a configurable threshold is visually flagged stale and publishes a domain event.
- [ ] A Supervisor can create a Patrol Request, assign it to an available Mobile Patrol Unit, and the unit can mark it completed.
- [ ] A Patrol Request appears correctly in the assigned guard's DAR filter view with no DAR-side special-casing.
- [ ] A Post ending (assignment period closes) correctly transitions to `ended` and stops accepting new check-ins/location updates.

## Open Questions

- Exact stale-location threshold and escalation default — pending a baseline Domain Events rule design.
- Whether a Mobile Patrol Unit can have more than one assigned guard simultaneously (a two-person unit) — not addressed here, current model assumes one `assigned_person_ref` per Post.
- Whether Patrol Request priority levels map to a fixed enum or a tenant-configurable list (mirroring DAR Entry's category pattern) — leaning toward tenant-configurable but not confirmed.
- Full breadcrumb-trail retention/replay (a continuous track, not just current position) is explicitly deferred to GIS & Mapping Services / a future Historical Playback Console (Module 3) — this doc only owns the current `last_known_location`.
- Whether ending a Post with an unresolved/unassigned Patrol Request still attached should block closure or just warn — not addressed here.
