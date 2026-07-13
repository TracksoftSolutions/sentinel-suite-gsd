# Status & State Monitors

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

Three related mechanisms, all explicitly deferred here by earlier docs in this module:

1. **Unit State** — the real, stored on-duty officer status model (`available`, `dispatched`, `en_route`, `on_scene`, `completed`, `out_of_service`) that Unit Dispatch & Proximity Routing flagged as its own eventual owner, promoting that doc's minimal "derived, not stored" availability signal (open-Dispatch-or-not) into a genuine state machine. Unit State transitions **automatically** in lockstep with an officer's current Dispatch phase (`dispatched`/`en_route`/`arrived`≈`on_scene`/`cleared`≈`completed`), and can also be set **manually** for reasons no Dispatch drives (`out_of_service` for a break, training, equipment issue). `completed` is a deliberate resting state, not an instant bounce back to `available` — real dispatch practice needs a buffer for report-writing before an officer is truly back in service, so returning to `available` (or moving to `out_of_service`) is its own explicit action.
2. **Time-on-Scene Watchdog** — a tenant-configurable duration threshold (per Call Type/Priority) that, once exceeded by a Dispatch sitting in `on_scene`, publishes an automation-eligible domain event — the same trigger/effect split used everywhere else in the platform. No new stored field is needed: elapsed time is computed directly off Dispatch's own existing `phase_timestamps`.
3. **Officer Check-in Safety Timer** — for calls flagged hazardous, a recurring automated prompt requiring the dispatched officer to confirm they're OK at a tenant-configurable interval; a missed confirmation is its own real record (never a silent gap, per Guard Tour's established "a negative outcome deserves a real row" discipline) and escalates via Domain Events.

All three build directly on records already established in this module — Dispatch's phase-timeline, Call's type/priority taxonomy — rather than inventing a parallel tracking mechanism.

## Actors & Roles

- **Guard/Officer** — their own Unit State transitions automatically with their Dispatch phases; confirms Safety Check-in prompts; can manually go `out_of_service`/`available`.
- **Dispatcher / Console Operator** — views live Unit State across the roster, manually sets/clears a unit's `out_of_service` state, overrides a busy/out-of-service unit's availability when force-assigning, can manually trigger an ad hoc check-in ping.
- **Supervisor** — same as Dispatcher, plus receives Time-on-Scene and missed-check-in escalations.
- **Tenant Admin** — configures Time-on-Scene thresholds, Safety Check-in Policy (interval, trigger phase, grace period), and which Call Types default to requiring a safety check-in cycle.

## User Stories

- As a **Dispatcher**, I want to see which officers are truly available versus just not currently on a call, since someone can be `out_of_service` for a break without an open Dispatch.
- As an **Officer**, I want my status to update automatically as I move through a call instead of me having to separately flip a status toggle on top of logging my phase.
- As a **Dispatcher**, I want to force-assign an officer who's marked `out_of_service` in a genuine emergency, with a clear confirmation step so it's never an accident.
- As a **Supervisor**, I want a visual alert when a unit has been on scene far longer than typical for that kind of call, so I notice a problem before it becomes a crisis.
- As an **Officer responding to a flagged hazardous call**, I want the system to periodically check in with me, and I want a quick, single tap to confirm I'm fine.
- As a **Supervisor**, I want to be immediately notified if an officer misses a required safety check-in, so I can act on a real possibility that they're in trouble.
- As a **Tenant Admin**, I want to decide which call types require a safety check-in cycle and how often, since what counts as hazardous varies by site.

## Functional Requirements

### Unit State
1. **Unit State** is a feature-local, per-on-duty-officer record — not an Entity Registry Core citizen, updated in place rather than versioned, the same posture as Patrol Management's Post `last_known_location` — holding one current `state`: `available`, `dispatched`, `en_route`, `on_scene`, `completed`, `out_of_service`.
2. State transitions automatically, driven by the officer's own Dispatch phase: a new Dispatch assignment sets `dispatched`; the Dispatch's own phase logging (`en_route`, `arrived`/`on_scene`, `cleared`) drives the matching Unit State transition (`arrived` maps to `on_scene`; `cleared` maps to `completed`) in lockstep, no separate logging action required.
3. `completed` is a resting state, not automatic — an officer or Supervisor must explicitly transition from `completed` to either `available` ("back in service") or `out_of_service` (e.g., going to write a report, taking a break next).
4. `out_of_service` is set and cleared manually (by the officer themselves or a Supervisor/Dispatcher on their behalf, the platform's established self-or-on-behalf-of posture), independent of any Dispatch, with an optional `out_of_service_reason` (tenant-configurable short list or free text).
5. Assigning a new Dispatch to an officer currently `out_of_service` (a Dispatcher's deliberate override) requires passing the Command/Action Bus's existing confirmation gate before the assignment proceeds — reusing that established mechanism rather than inventing new override logic.
6. Every Unit State transition is an audit-tier event (per Structured Logging & Audit Trails), consistent with how every other state-bearing mechanism in the platform is logged; Unit State itself is not a versioned history table (the current value is what's tracked in place, same as Post's location).

### Time-on-Scene Watchdog
7. A tenant-configurable **Watchdog Threshold** (a duration, settable per Call Type/Priority or as a flat platform default) applies to a Dispatch's time spent in `on_scene`, computed directly from that Dispatch's own `phase_timestamps` — no new field required on Dispatch.
8. Exceeding the threshold publishes an automation-eligible domain event; a Tenant Admin configures the actual notification/escalation behavior via Domain Events, per the platform's standard trigger/effect split — this doc never hardcodes an alert path.
9. A Dispatch exceeding its threshold is visually flagged wherever it's already rendered (its Feed line on the Active Incident Queue's parent Call card) — a computed display attribute, not a stored one, and never relying on color alone (non-color-only status indicator, per the platform's established accessibility discipline).

### Officer Check-in Safety Timer
10. A tenant-configurable **Safety Check-in Policy** sets the recurring interval, which Dispatch phase(s) trigger the cycle (default: `on_scene`; optionally also `en_route`), and the grace period before a prompt counts as missed.
11. **Call Type Definition** (Call Intake & Logging) gains an optional `default_requires_safety_checkin` field (retrofit, see Integrations) — a tenant marks which call types are hazardous by default. A Dispatcher can override this per specific Call/Dispatch (explicit override beats the type default — the same "explicit beats default resolver" precedence already established for Command/Action Bus parameters).
12. Once a qualifying Dispatch reaches its trigger phase, the system automatically begins issuing recurring check-in prompts at the configured interval — no officer opt-in action required to start the cycle.
13. Each prompt creates a **Safety Check-in** occurrence: the officer confirms with a single, zero-friction tap (same low-friction-input discipline as Canned Quick Phrase); a confirmation within the grace period marks it `confirmed`. One that times out marks `missed` — a real, permanent row, never a silent gap, per Guard Tour's established discipline that a negative outcome deserves its own record.
14. A `missed` Safety Check-in publishes its own automation-eligible domain event, letting a Tenant Admin configure the actual escalation behavior (e.g., notify the Supervisor, initiate a welfare check) via Domain Events.
15. A Dispatcher or Supervisor can also manually trigger an **ad hoc check-in** on any dispatched officer at any time, outside the recurring hazardous-call cycle, using the same underlying confirm/missed mechanism.
16. The check-in cycle for a given Dispatch ends when that Dispatch reaches `cleared` (Unit State `completed`) or is cancelled — no further prompts issue after that point.

## Data Model / Fields

**Unit State** (feature-local, mutable in place — not an Entity Registry Core citizen)
- unit_state_id, tenant_id, person_ref (the on-duty officer)
- state (available, dispatched, en_route, on_scene, completed, out_of_service)
- source_dispatch_ref (nullable — the Dispatch record currently driving this state, when applicable)
- out_of_service_reason (nullable — tenant-configurable short list or free text)
- state_since (timestamp of the most recent transition)

**Watchdog Threshold** (Settings & Preferences registration)
- threshold_id, tenant_id, call_type (nullable — null = platform default), call_priority (nullable, further narrows), duration

**Safety Check-in Policy** (Settings & Preferences registration)
- policy_id, tenant_id, interval, trigger_phases[] (default: [on_scene]), grace_period

**Call Type Definition** (retrofit — adds one field to the existing table in call-intake-logging.md)
- default_requires_safety_checkin (bool, default false)

**Safety Check-in** (thin Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- dispatch_ref (direct field, fixed at creation — same non-EntityAssociation reasoning as Incident Update's own parent link)
- due_at, confirmed_at (nullable)
- status (pending, confirmed, missed)
- method (recurring_hazard_cycle, ad_hoc_dispatcher_initiated)

## States & Transitions

**Unit State:** `available` ⇄ `out_of_service` (manual, either direction) | `available` → `dispatched` (new Dispatch assignment; from `out_of_service` requires confirmation-gate override) → `en_route` → `on_scene` → `completed` (all auto, mirroring the driving Dispatch's own phase) → `available` | `out_of_service` (manual, explicit).

**Safety Check-in:** `pending` → `confirmed` (officer taps within grace period) | `missed` (grace period elapses with no confirmation) — terminal either way, never re-opened; a missed cycle's next scheduled interval creates a fresh, independent occurrence.

## Integrations

- **Unit Dispatch & Proximity Routing — retrofit**: that doc's minimal "derived, not stored" availability signal (an on-duty officer is available if they have no open Dispatch) is superseded by this doc's real Unit State record — proximity suggestion now filters on `state = available` directly rather than re-deriving it from Dispatch's own open/closed phase set. The confirmation-gate override for assigning an `out_of_service` officer (Functional Requirement #5) is the concrete mechanism behind that doc's own "a Dispatcher may still manually assign an officer the system doesn't list as available" allowance.
- **Call Intake & Logging — retrofit**: Call Type Definition gains `default_requires_safety_checkin`.
- **Activity Registry**: Safety Check-in registers as a thin Activity extension, inheriting identity, offline-safe numbering, and standard treatment.
- **Domain Events / Notifications Engine**: owns the actual notification/escalation behavior for both Time-on-Scene threshold breaches and missed Safety Check-ins — this doc only publishes the triggering domain events.
- **Active Incident Queue (CAD Console)**: a Dispatch Feed line (nested under its parent Call card) renders its officer's current Unit State and a Time-on-Scene visual flag when applicable — both computed display attributes, no schema change needed to that doc's own data model. A Patrol card's `assigned_unit_ref` similarly reflects the assigned officer's live Unit State.
- **Command/Action Bus**: "Set unit out of service," "Return unit to available," "Trigger ad hoc check-in," "Confirm check-in" register as invokable actions across every surface; assigning an `out_of_service` officer reuses the existing confirmation-gate mechanism rather than a new one.
- **GIS & Mapping Services**: a natural downstream consumer of Unit State for map-pin coloring by status — a forward reference only, not built here.
- **Structured Logging & Audit Trails**: every Unit State transition, Watchdog Threshold breach, and Safety Check-in confirmation/miss is an audit-tier event.
- **Settings & Preferences**: owns Watchdog Threshold and Safety Check-in Policy configuration.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|---|
| View live Unit State across the roster | ❌ (own only) | ✅ | ✅ | ✅ |
| Manually set/clear own `out_of_service` | ✅ | ✅ (on behalf of) | ✅ (on behalf of) | ❌ |
| Force-assign an `out_of_service` officer (confirmation-gated) | ❌ | ✅ | ✅ | ❌ |
| Confirm own Safety Check-in | ✅ | ❌ | ❌ | ❌ |
| Trigger an ad hoc check-in on another officer | ❌ | ✅ | ✅ | ❌ |
| Configure Watchdog Thresholds / Safety Check-in Policy | ❌ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Unit State transitions driven by Dispatch phase logging must work fully offline, consistent with Dispatch's own offline-first model, and reconcile correctly on sync.
- Time-on-Scene and missed-check-in domain event evaluation must fire promptly (near-real-time), consistent with the platform's existing latency expectations for safety/escalation-relevant automation (Incident severity escalation, Patrol Request creation).
- A missed Safety Check-in must never be silently dropped or overwritten — it is a permanent record even after the underlying Dispatch later clears normally.
- Watchdog and check-in visual indicators must not rely on color alone (WCAG 2.1 non-color-only status indicator requirement, consistent with the platform's established accessibility discipline).
- WCAG 2.1 / Section 508 accessible Unit State display, override confirmation, and check-in confirmation flows, day one.

## Acceptance Criteria

- [ ] An officer's Unit State automatically transitions through `dispatched` → `en_route` → `on_scene` → `completed` as their Dispatch's own phases are logged, with no separate status action required.
- [ ] An officer's Unit State remains `completed` until explicitly moved to `available` or `out_of_service` — it never auto-reverts to `available` on its own.
- [ ] A Dispatcher can manually set an on-duty officer to `out_of_service` with an optional reason, and clear it back to `available`.
- [ ] Assigning a Dispatch to an `out_of_service` officer requires passing the confirmation gate before the assignment completes.
- [ ] A Dispatch left in `on_scene` past its configured Watchdog Threshold publishes a domain event a configured Domain Events rule can act on, and is visually flagged on its Active Incident Queue Feed line.
- [ ] A Call Type marked `default_requires_safety_checkin` automatically begins a recurring check-in cycle once its Dispatch reaches the configured trigger phase, with no manual opt-in.
- [ ] An officer confirming a check-in prompt within the grace period marks it `confirmed`; letting the grace period lapse marks it `missed` and publishes a domain event.
- [ ] A Dispatcher can manually trigger an ad hoc check-in on any currently-dispatched officer outside the recurring cycle.
- [ ] The check-in cycle stops issuing new prompts once its Dispatch clears or is cancelled.
- [ ] Every Unit State transition and Safety Check-in outcome is a discoverable audit-tier event.

## Open Questions

- Exact precedence when an on-duty officer has more than one concurrently open Dispatch (a multi-tasking override case Unit Dispatch & Proximity Routing already allows) — current default is that Unit State reflects whichever Dispatch most recently changed phase; not deeply resolved here.
- Exact default Watchdog Threshold durations and Safety Check-in intervals/grace periods — pending UX/content design and likely informed by real customer input.
- Whether `out_of_service_reason` needs to be a full tenant-configurable Definition (like Call Type) or stays simple free text/short fixed list — leaning toward a short fixed list at launch, revisit if reporting needs (Module 12) require richer categorization.
- Whether a missed Safety Check-in should automatically also flag the underlying Call/Incident for supervisor review, or stay a purely notification-driven escalation — current default is notification-only via Domain Events, no automatic record-level flag; revisit if a real gap surfaces.
- Whether Time-on-Scene watchdog thresholds ever need to vary by site/location in addition to call type/priority — not addressed here, a Settings & Preferences location-chain extension if a real need surfaces.
