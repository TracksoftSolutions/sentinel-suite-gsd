# Unit Dispatch & Proximity Routing

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

**Dispatch** registers as its own Activity extension (`activity_type = dispatch`, already anticipated in Activity Registry), owning everything Call Intake & Logging deferred: assigning one or more on-duty officers to a queued Call, suggesting the nearest available officer, and tracking each assigned officer's own response phases (dispatched → en route → arrived → on scene → cleared) through to completion. A Call can have **multiple** Dispatch records — a multi-unit response (three officers sent to the same call) is three Dispatch rows, each independently phase-tracked, each pointing back to the same `source_call_ref`.

**The dispatchable "unit" is fundamentally an on-duty officer**, not a pre-configured post. Dispatch draws its candidate pool from currently on-duty Persons (per GIS & Mapping Services' existing on-duty gating — GPS position tracking, and therefore proximity eligibility, is already scoped to active-shift only) rather than requiring a Patrol Management Post to exist first. When an officer *does* have an active Post (Fixed or Mobile Patrol Unit), the Dispatch record optionally carries that context (`assigned_post_ref`) for continuity — a dispatched officer already running a mobile patrol keeps their Post; one with no Post assignment at all is equally dispatchable.

**Proximity routing is a straight-line nearest-available-officer suggestion, never an auto-assignment.** GIS & Mapping Services is a provider-adaptor architecture with no single hardcoded routing engine, so this doc commits only to what's honestly buildable without picking a routing-engine dependency: rank on-duty, available officers by straight-line distance (using GIS's Position Record, the actual system of record for GPS position) from the Call's location, when the Call has one. The Dispatcher always sees the ranked list and always confirms the assignment — this is a suggestion engine, not an autonomous one, consistent with the platform's "AI/automation proposes, human confirms" discipline applied here to a non-AI ranking. True driving-distance/ETA-aware routing is flagged as a future enhancement contingent on a routing-capable GIS adaptor, not committed to now.

**Availability now reads from Status & State Monitors' real Unit State record (retrofit).** An on-duty officer is "available" for proximity suggestion when their Unit State is `available` — this doc originally derived availability locally (no open Dispatch), a minimal signal explicitly flagged as a precursor pending **Status & State Monitors**, which now owns the fuller unit-status model (`available`/`dispatched`/`en_route`/`on_scene`/`completed`/`out_of_service`, including manually-set states no Dispatch drives) this doc's proximity suggestion consumes directly.

**This doc supersedes AI-Assisted Incident Report Writing's interim Response Timeline Event as the canonical response-phase timeline**, exactly as that doc flagged. Dispatch's own phase-timeline (dispatched, en_route, arrived, on_scene, cleared) is now the source of truth for a unit's response phases; Response Timeline Event is retrofitted to derive from the corresponding Dispatch record when one exists (see Integrations), rather than being independently logged a second time for the same real-world response.

## Actors & Roles

- **Dispatcher / Console Operator** — reviews a queued Call, requests proximity suggestions, assigns one or more officers, monitors phase progress, reassigns/cancels a Dispatch if needed.
- **Guard/Officer (the dispatched unit)** — receives a dispatch notification, self-logs their own phase progress (en route, arrived, on scene, cleared).
- **Supervisor** — can assign/reassign, and log or correct a Dispatch's phase on an officer's behalf, same posture already established for Mobile Patrol Unit location and Response Timeline Events.
- **Tenant Admin** — configures the dispatch phase list (if any tenant-level naming variance is needed) via Settings & Preferences.
- **Records Admin** — resolves Entity Registry Core dedup flags on Dispatch, same as any other Activity type.

## User Stories

- As a **Dispatcher**, I want to see which on-duty officers are actually free and closest to a call's location, ranked for me, rather than manually checking everyone's last known position myself.
- As a **Dispatcher**, I want to send two officers to the same call and track each one's progress independently, since they may arrive at very different times.
- As a **Guard**, I want to tap "En Route" and "Arrived" as I respond, and have that show up immediately on the dispatcher's console.
- As a **Supervisor**, I want to reassign a call to a different officer if the first one gets pulled into something else, without losing the original officer's partial timeline.
- As a **Dispatcher**, I want a call with no known location to still let me manually pick any available officer, since proximity ranking simply doesn't apply there.
- As an **Investigator later reviewing an escalated incident**, I want its response timeline to reflect exactly what dispatch already tracked, without a responder having to log the same phases twice.

## Functional Requirements

### Dispatch (Activity extension)
1. **Dispatch** registers as an Activity extension, fulfilling Activity Registry's already-anticipated `dispatch` activity_type: inherits base identity, offline-safe numbering, and display-label requirements.
2. `source_call_ref` (direct field, fixed at creation) points back to the originating Call — the downstream-points-to-upstream convention used throughout the platform.
3. `assigned_person_ref` (direct field, fixed at creation) identifies the on-duty officer this Dispatch record tracks. `assigned_post_ref` (nullable) and `assigned_vehicle_ref` (nullable) carry Post/vehicle context when the officer has an active Post assignment — neither is required.
4. A Call with multiple responders produces multiple Dispatch records, one per assigned officer, each independently phase-tracked, all sharing the same `source_call_ref`.

### Proximity suggestion & assignment
5. When a Dispatcher requests suggestions for a queued Call that has a location, the system ranks currently **available** on-duty officers (see availability, below) by straight-line distance from their last known position (GIS Position Record) to the Call's location, nearest first.
6. A Call with no location skips ranking entirely — the Dispatcher assigns manually from the full available-officer list.
7. The Dispatcher always makes the final assignment call — no suggestion auto-assigns. Assigning an officer creates their Dispatch record and transitions the Call to `dispatched` (per Call Intake & Logging's lifecycle).
8. **Availability** (for suggestion purposes only) is superseded by Status & State Monitors' real Unit State record (retrofit — see Integrations): an on-duty officer is available if their Unit State is `available` — this doc no longer re-derives availability from Dispatch's own open/closed phase set independently.
9. A Dispatcher may still manually assign an officer the system doesn't list as available (e.g., a supervisor judgment call to pull someone off a lower-priority task) — availability filtering narrows the default suggestion list, it never hard-blocks an override; assigning an officer currently `out_of_service` specifically requires passing Status & State Monitors' confirmation-gate override.

### Phase tracking
10. A Dispatch record tracks its assigned officer's own response phases: `dispatched` (set automatically at assignment) → `en_route` → `arrived` → `on_scene` → `cleared`, each with its own `phase_timestamp`.
11. Phases are logged **self- or on-behalf-of**, the identical posture already established for Mobile Patrol Unit location and Response Timeline Events: the assigned officer taps their own phase, or a Supervisor/Dispatcher logs it on their behalf (e.g., relayed by radio).
12. A logged phase timestamp is correctable after the fact, with a retained correction history (audit-logged), same as Response Timeline Event's existing correction mechanic.
13. A Dispatch may be **cancelled** or **reassigned** before reaching `cleared` — cancelling frees the officer for availability purposes immediately; a reassignment creates a new Dispatch record for the new officer while the original is marked `cancelled`, preserving its partial phase history rather than overwriting it. Pulling an officer off a call specifically *for a higher-priority call* is not a cancel — it is a **preemption** (#17), a distinct terminal state so reporting can tell the two apart.
14. A Call's own status only reaches its terminal dispatch-related state once every non-cancelled, non-preempted-without-resumption Dispatch under it reaches `cleared` — the exact rollup/notification mechanics are a technical-spec-level concern, but at minimum a Dispatcher must be able to see at a glance which of a multi-unit call's Dispatch records are still open.

### Stacking, queueing & preemption
15. A tenant-configurable **Dispatch Stacking Policy** (Settings & Preferences-registered: `single_active` | `queued`) governs whether an officer may hold more than one unresolved Dispatch. Under **either** policy, at most **one** Dispatch per officer is ever *active* (in the `dispatched` → `cleared` phase range) at a time — this is what keeps Unit State honestly single-valued (see Status & State Monitors retrofit).
    - `single_active` (platform default): assigning a Call to an officer with an unresolved Dispatch requires explicitly resolving the existing one as part of the assignment — preempt it (#17) or cancel it — via the Command/Action Bus confirmation gate. Nothing waits silently on a busy officer.
    - `queued`: the new Dispatch is created in a **`queued`** phase, joining that officer's ordered personal queue (ordered by `queued_at`). Queued Dispatches are visible to both the Dispatcher (roster/console) and the officer (their own device), but drive nothing — no phase timeline, no Unit State effect — until activated.
16. **Queue advance is always an explicit human action, never automatic.** The officer (self) or a Dispatcher/Supervisor (on-behalf-of, e.g., radio-relayed) activates the next queued Dispatch, transitioning it `queued` → `dispatched`. Advancing is explicitly permitted while the officer's Unit State is `completed` — taking the next call temporarily cuts short the report-writing buffer; the Completed State Policy governs return-to-available, not queue advance. Consistent with the platform's established explicit-action-over-silent-automation posture (patrol starts, safety check-ins), the system never silently launches an officer at the next call.
17. **Preemption**: a Dispatcher may preempt an officer's active Dispatch to assign a higher-priority Call. The active Dispatch transitions to a terminal **`preempted`** state — a real, queryable row (same discipline as `missed` Patrols), recording `preempted_by_dispatch_ref` (the new Dispatch that displaced it) and preserving all partial phase history. Preemption passes the Command/Action Bus confirmation gate before executing.
18. **The interrupted call's disposition is the Dispatcher's per-preemption choice**: return it to the pending pool for reassignment (any officer — including the original one later), or, under the `queued` policy, re-queue it to the same officer's queue. Either way, the resumed response is a **new** Dispatch record with `resumes_dispatch_ref` pointing at the preempted row — never a reopening of the preempted Dispatch itself.
19. `cancelled` vs. `preempted` vs. `resumes_dispatch_ref` together make a call's full response chain walkable and honest: reporting (Module 12's future Response Time Analytics) can distinguish an abandoned response from an officer pulled for higher priority, and can reconstruct a preempt-and-resume sequence end to end.

## Data Model / Fields

**Dispatch** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- source_call_ref (direct field, fixed at creation)
- assigned_person_ref (direct field, fixed at creation)
- assigned_post_ref (nullable), assigned_vehicle_ref (nullable)
- phase (queued, dispatched, en_route, arrived, on_scene, cleared)
- phase_timestamps{} (one per phase reached, set as each is logged)
- queued_at (nullable — set when created into `queued` under the `queued` stacking policy; ordering key for the officer's personal queue)
- preempted_by_dispatch_ref (nullable — set when this Dispatch is preempted, pointing to the displacing Dispatch)
- resumes_dispatch_ref (nullable — set on a new Dispatch created to resume a previously preempted response)
- source_location_ref (nullable — meaningful for `en_route`, where the officer started from; same field shape as Response Timeline Event's)
- corrected (bool), correction_history[] (prior value, changed_by, changed_at)
- recorded_by (per-phase-log, self or on-behalf-of)
- reassigned_to_ref (nullable — set on the original Dispatch when reassigned, pointing to the replacement Dispatch record)
- silent_delivery (nullable bool), radio_bypass (nullable bool), acknowledged_at (nullable) — retrofit, see [silent-mobile-dispatching.md](silent-mobile-dispatching.md)

## States & Transitions

**Dispatch:** (`queued` — only under the `queued` stacking policy, exits solely via explicit human advance or cancellation →) `dispatched` → `en_route` → `arrived` → `on_scene` → `cleared` (terminal) | `cancelled` (terminal, reachable from `queued` or any non-cleared phase; `reassigned_to_ref` set if the cancellation was specifically a reassignment rather than an outright cancel) | `preempted` (terminal, reachable from any active phase via confirmation-gated Dispatcher preemption; `preempted_by_dispatch_ref` always set).

**Invariant:** an officer has at most one Dispatch in the active range (`dispatched`–`on_scene`) at any moment, under either stacking policy — enforced at assignment, queue-advance, and preemption time.

## Integrations

- **Call Intake & Logging**: source of `source_call_ref`; a Call reaching `dispatched` status is what this doc's assignment flow produces. Multiple Dispatch records may share one Call.
- **Activity Registry**: Dispatch registers as an Activity extension, fulfilling the `dispatch` activity_type already anticipated there.
- **GIS & Mapping Services**: source of Position Record (the system of record for on-duty officer GPS position) that proximity ranking reads from directly — this doc does not maintain its own position store, consistent with every other consumer of that feature.
- **Patrol Management**: source of Post/Mobile Patrol Unit context (`assigned_post_ref`, `assigned_vehicle_ref`) when a dispatched officer has one; a Post is never required for an officer to be dispatchable.
- **Entity Registry Core**: identity, display-label, and standard dedup/merge for Dispatch, same as any other Activity extension.
- **AI-Assisted Incident Report Writing — retrofit**: Response Timeline Event gains an optional `source_dispatch_ref` (nullable, FK → Dispatch). When an Incident is escalated from a Call that has a corresponding Dispatch record for the responding officer, that officer's `dispatched`/`en_route`/`arrived`/`on_scene`/`cleared` phase timestamps are sourced from the linked Dispatch record rather than independently logged a second time — Response Timeline Event's `call_received` phase continues to source from the originating Call's own `received_at` (Call Intake & Logging). A self-initiated Incident with no upstream Call/Dispatch (the case Response Timeline Event was originally built for) is unaffected and continues logging phases directly, `source_dispatch_ref` simply staying null.
- **Settings & Preferences**: owns any tenant-level dispatch phase-list configuration, and the **Dispatch Stacking Policy** (`single_active` | `queued`, plus the queue depth cap under `queued`).
- **Guard Tour & Checkpoint Verification — retrofit**: an in-progress Patrol whose officer's Dispatch activates (assignment or queue advance) auto-transitions to that doc's new `interrupted` status with `interrupted_by_dispatch_ref` set — a real row explaining any resulting missed checkpoints, never a silent gap. See that doc for resume mechanics.
- **Domain Events / Notifications Engine**: Dispatch creation (a new assignment) publishes an automation-eligible event, letting a Tenant Admin configure the actual "notify the assigned officer" behavior, consistent with the platform's trigger/effect split.
- **Structured Logging & Audit Trails**: Dispatch creation, every phase log/correction, cancellation, and reassignment are audit-tier events.
- **Command/Action Bus**: "Request proximity suggestions," "Assign officer to call," "Log dispatch phase," "Reassign dispatch," "Cancel dispatch" register as invokable actions across every surface.
- **Status & State Monitors (Module 2)**: as anticipated, now owns the real Unit State record this doc's proximity suggestion filters on directly (`state = available`), superseding this doc's original open-Dispatch-or-not derivation; also owns the confirmation-gate override required to assign an `out_of_service` officer.
- **Silent Mobile Dispatching (Module 2)**: owns Dispatch's `silent_delivery`/`radio_bypass`/`acknowledged_at` fields (retrofit above) and the notification delivery/acknowledgment mechanics built on top of this doc's own assignment and phase-tracking records.
- **Active Incident Queue (CAD Console), Multi-Incident Console, Active Call Alerts & Timers (Module 2, future)**: downstream consumers of Dispatch's phase state for real-time queue/console rendering — not built here.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|---|
| Request proximity suggestions for a call | ❌ | ✅ | ✅ | ❌ |
| Assign an officer to a call | ❌ | ✅ | ✅ | ❌ |
| Log own dispatch phase | ✅ | ❌ | ❌ | ❌ |
| Log/correct a dispatch phase on another officer's behalf | ❌ | ✅ | ✅ | ❌ |
| Reassign/cancel a dispatch | ❌ | ✅ | ✅ | ❌ |
| Configure dispatch phase list | ❌ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Proximity suggestion must run fast enough for live dispatcher use — ranking a modest on-duty roster by straight-line distance is not expected to be a performance concern, but must degrade gracefully (clearly show "no position available" rather than silently omitting an otherwise-available officer with stale/missing position data).
- Dispatch phase logging must work fully offline (an officer confirming "arrived" in a dead zone), consistent with the platform's offline-first model, syncing and reconciling like any other Activity.
- A cancelled/reassigned Dispatch's partial phase history must never be deleted or overwritten — it remains queryable for accountability even though it no longer counts toward the officer's availability.
- WCAG 2.1 / Section 508 accessible dispatch-assignment console and phase-logging flows, day one.

## Acceptance Criteria

- [ ] Requesting proximity suggestions for a Call with a location returns available on-duty officers ranked nearest-first by straight-line distance from GIS Position Record.
- [ ] Requesting suggestions for a Call with no location returns the full available-officer list, unranked, with no error.
- [ ] Assigning an officer to a Call creates a Dispatch record, sets its phase to `dispatched`, and transitions the Call to `dispatched` status.
- [ ] A Call with three assigned officers produces three independent Dispatch records, each with its own phase progression.
- [ ] An officer with an active Dispatch does not appear in the default available-officer suggestion list; under `single_active` policy, assigning them anyway forces an explicit confirmation-gated preempt-or-cancel of their current Dispatch; under `queued` policy, the new Dispatch is created `queued` with no effect on the active one.
- [ ] A queued Dispatch activates only via explicit advance by the officer or a Dispatcher/Supervisor on-behalf-of — never automatically — including from the officer's `completed` Unit State.
- [ ] Preempting an active Dispatch marks it `preempted` (not `cancelled`) with `preempted_by_dispatch_ref` set and full partial phase history preserved, and prompts the Dispatcher to choose the interrupted call's disposition (pending pool vs. re-queue to the same officer under `queued` policy).
- [ ] A resumed response to a preempted call is a new Dispatch with `resumes_dispatch_ref` set, and the full preempt/resume chain is queryable.
- [ ] At no point, under either stacking policy, can one officer hold two Dispatches in the active phase range simultaneously.
- [ ] A Guard logging "En Route" then "Arrived" on their own Dispatch correctly timestamps each phase and is visible to the Dispatcher in near-real-time.
- [ ] A Supervisor can log a phase on an officer's behalf and later correct a previously-logged timestamp, with the correction retained in history.
- [ ] Reassigning a Dispatch marks the original `cancelled` (with `reassigned_to_ref` set) and creates a new Dispatch for the replacement officer, preserving the original's partial phase history.
- [ ] An Incident escalated from a Call with an associated Dispatch record correctly derives its Response Timeline Event phase timestamps from that Dispatch, with no duplicate manual logging required.
- [ ] A self-initiated Incident with no originating Call continues to log Response Timeline Events directly, unaffected by this doc's retrofit.
- [ ] A Dispatch created offline receives a usable client UUID immediately and syncs/reconciles correctly once connectivity returns.

## Open Questions

- Exact Call-level rollup/status behavior once all of a multi-unit call's Dispatch records reach `cleared` — flagged as a technical-spec-level concern in Functional Requirement #14, not fully specified here.
- Whether/when driving-distance or ETA-aware routing becomes a real requirement, and which GIS provider adaptors would need to support it — deferred until a concrete need or adaptor capability is identified.
- Exact real-time delivery mechanism for dispatch notifications to the assigned officer's device — owned by Notifications Engine, not solved here.
- Whether Dispatch needs its own tenant-configurable phase list (beyond the fixed dispatched/en_route/arrived/on_scene/cleared vocabulary) for sites with different response-lifecycle terminology — current default is a fixed platform list, mirroring Response Timeline Event's original phase-list decision; revisit if a real tenant need surfaces.
- ~~Full unit-status model~~ — resolved: see [status-state-monitors.md](status-state-monitors.md), which now owns Unit State and this doc's availability check reads from directly.
- Queue depth cap default under the `queued` stacking policy (tenant-configurable; a small default like 3 seems right so a queue never silently becomes a backlog) — pending real customer input.
- Whether a queued Dispatch should generate its Silent Mobile Dispatching acknowledgment at queue time or only at activation — leaning activation-only (acknowledging a call you can't act on yet is noise), to be confirmed when a real tenant uses `queued` mode.
