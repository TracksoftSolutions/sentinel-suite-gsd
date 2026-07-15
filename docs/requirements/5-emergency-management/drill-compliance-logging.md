# Drill Compliance Logging

## Overview

Drill Compliance Logging is the lighter, simpler mechanism Exercise & Drill Planner deliberately deferred to it — routine regulatory fire drills, evacuations, and shelter-in-place drills, not designed-and-evaluated HSEEP exercises. No plan/execution split, no scenario injects, no evaluation scorecards: a **Compliance Drill Requirement** (the recurring regulatory obligation) and a **Compliance Drill Log** (the real occurrence), reusing established mechanisms throughout rather than building new ones.

While drafting this doc, a real naming collision surfaced and got fixed: Exercise & Drill Planner had registered its own `Exercise` occurrence under `activity_type = drill`, borrowed from Activity Registry's own illustrative base-type list — but this doc's regulatory compliance drills are a genuinely different record. **Retrofit — Exercise renamed from `activity_type = drill` to `activity_type = exercise`** (see [exercise-drill-planner.md](exercise-drill-planner.md) and [activity-registry.md](../0.5-master-records/activity-registry.md)), freeing `compliance_drill` as this doc's own distinct type. Colloquially both are "drills"; structurally they're unrelated records, and the field name now says so.

Three components, each reusing an established mechanism:

1. **Compliance Registries** — Compliance Drill Requirement (the recurring obligation) and Compliance Drill Log (the Activity extension logging a real occurrence), with overdue tracking reusing Key Custody & Auditing's dynamic-threshold Duration Watchdog mode a third time.
2. **Occupant Exit Reports** — an aggregate, interim stand-in for evacuation timing (total time, per-zone exit times), explicitly flagged for reconciliation once Module 6's future Muster Check-in App/Evacuation Roster Reconciliation exist and own real per-occupant roster data — the same deferred-integration posture DAR's Shift Window and EOC Logistics Hub's Resource Type Definition already established.
3. **Corrective Actions** — a failed Drill Component Check (a silent alarm, a blocked exit) auto-suggests a draft Improvement Action, reusing Improvement Plan (IP) Tracking's widened source mechanism with a new `compliance_drill` source type — the "automation proposes, human confirms" discipline Exercise & Drill Planner just established for Evaluation Scores, reused for a second, independent trigger.

## Actors & Roles

- **Safety Coordinator / Facility Safety Officer** — logs Compliance Drill occurrences, records exit times and component checks.
- **Site / Tenant Admin** — configures Compliance Drill Requirements (cadence, applicable sites).
- **Supervisor / Safety Director** — confirms an auto-suggested Improvement Action arising from a failed component check.
- **Any user with permission on the site** — views logged drills.

## User Stories

- As a **Safety Coordinator**, I want to log this quarter's fire drill in under a minute, with the required cadence tracked automatically so I don't have to remember it myself.
- As a **Safety Coordinator**, I want to record how long full evacuation took and note which zone was slowest, without needing a full occupant-by-occupant roster system that doesn't exist yet.
- As a **Safety Coordinator**, I want a failed alarm component discovered during a drill to become a tracked corrective action automatically, not a note I have to remember to act on later.
- As a **Site Admin**, I want to be notified if a required drill hasn't happened within its compliance window, without it blocking any other site operation.
- As a **Supervisor**, I want to review and confirm corrective actions arising from drill findings the same way I already review AAR/exercise-sourced ones.

## Functional Requirements

### Compliance Drill Requirement
1. A tenant/site-configured **Compliance Drill Requirement** carries `drill_type` (fire, evacuation, shelter_in_place, other), `required_cadence_days`, an optional free-text `regulation_reference`, and `site_ref` — the recurring regulatory obligation a site must satisfy.
2. **Overdue tracking** reuses Key Custody & Auditing's dynamic-threshold Duration Watchdog mode a third time (after Key Custody itself and AAR's Overdue Improvement Action): the watched threshold resolves to the requirement's own cadence measured from its most recent Compliance Drill Log, not a flat configured duration. Escalates via Notifications Engine/Critical Event Escalation Policy — never blocking any site operation, the platform's standing rule.

### Compliance Drill Log
3. **Compliance Drill Log** registers as its own Activity extension (`entity_id` shared PK, FK → Activity, `activity_type = compliance_drill`), optionally referencing a `requirement_ref` (nullable — an unscheduled or ad hoc drill is still loggable without a formal Requirement in place) — `drill_type`, `conducted_at`, `conducted_by`, `site_ref`, `participant_count` (nullable, an estimate is acceptable). Created once, immutable thereafter — a correction is a new log entry, not an edit.
4. Compliance Drill Log registers `is_mergeable = false` — a routine, timestamped occurrence with no duplicate-identity concept, the same explicit opt-out already declared for Checkpoint Scan and Safety Check-in.

### Occupant Exit Report
5. **Occupant Exit Report** (child of Compliance Drill Log) carries `total_evacuation_time_seconds` and `per_zone_exit_times[]` (zone reference, exit time, an occupant-count estimate) — an honest, aggregate-only interim stand-in. This is **not** a per-occupant muster/roster reconciliation — that real capability is explicitly deferred to Module 6's future Muster Check-in App and Evacuation Roster Reconciliation, neither built yet, the same deferred-integration posture applied throughout the platform's history whenever a later module clearly owns the fuller mechanism.

### Drill Component Check and Corrective Actions
6. **Drill Component Check** (child of Compliance Drill Log) records `component_type` (alarm, exit_door, signage, other), an optional `location_ref`, `status` (pass/fail), and notes — one row per checked component during the drill.
7. **A `fail` status auto-drafts a proposed Improvement Action** *(retrofit — see [improvement-plan-ip-tracking.md](improvement-plan-ip-tracking.md))*, with a new `source_type = compliance_drill` (`source_ref` = this Compliance Drill Log's entity_id), pre-filled recommendation text referencing the failed component. It never becomes a real, tracked Improvement Action without explicit confirmation by a Supervisor/Safety Director — the same "automation proposes, human confirms" discipline Exercise & Drill Planner just established for below-target Evaluation Scores, now reused for an independent, structurally different trigger (a failed pass/fail check rather than a numeric score).

## Data Model / Fields

**Compliance Drill Requirement** (tenant/site Definition)
- requirement_id, tenant_id, site_ref
- drill_type (fire, evacuation, shelter_in_place, other), required_cadence_days, regulation_reference (nullable)
- status (active, archived)

**Compliance Drill Log** (Activity extension; entity_id is the shared PK, FK → Activity; activity_type = compliance_drill)
- requirement_ref (nullable), drill_type, conducted_at, conducted_by, site_ref, participant_count (nullable)

**Occupant Exit Report** (child of Compliance Drill Log)
- report_id, drill_log_ref, total_evacuation_time_seconds
- per_zone_exit_times[] (zone_ref, exit_time_seconds, occupant_count_estimate)

**Drill Component Check** (child of Compliance Drill Log)
- check_id, drill_log_ref, component_type (alarm, exit_door, signage, other)
- location_ref (nullable), status (pass, fail), notes
- suggested_improvement_action_ref (nullable — set once a `fail` auto-drafts one)

## States & Transitions

- **Compliance Drill Requirement:** `active` → `archived`, standard Definition lifecycle.
- **Compliance Drill Log / Occupant Exit Report / Drill Component Check:** created once, immutable — no status machine; a correction is a new row.

## Integrations

- **Improvement Plan (IP) Tracking** *(retrofitted)*: gains `compliance_drill` as a fourth `source_type` value, populated by FR #7.
- **Exercise & Drill Planner** *(retrofitted)*: `Exercise`'s `activity_type` is renamed from `drill` to `exercise`, resolving the naming collision this doc's own `compliance_drill` type would otherwise have created.
- **Activity Registry** *(retrofitted)*: base `activity_type` illustrative example list updated to reflect the `exercise`/`compliance_drill` split.
- **Active Call Alerts & Timers**: overdue Compliance Drill Requirement is a registered Duration Watchdog instance (dynamic threshold), no new alerting mechanism.
- **Location Registry**: source of `location_ref`/`zone_ref` references throughout.
- **Notifications Engine**: delivery channel for overdue-requirement alerts, unmodified.
- **Command/Action Bus**: Log Compliance Drill, Record Component Check, Record Occupant Exit Report, and Configure Compliance Drill Requirement all register as actions.
- **Structured Logging & Audit Trails**: every Compliance Drill Log and failed Drill Component Check is audit-tier.
- **Module 6 (Emergency Planning, not yet specified)**: forward reference — Muster Check-in App and Evacuation Roster Reconciliation will own real per-occupant roster data; Occupant Exit Report's aggregate metrics are flagged for reconciliation once that module exists, not built now.
- **Module 21 (Compliance, Self-Assessments & Audits, not yet specified)**: forward reference only — a future Compliance Dashboard likely rolls this data up; not built here.

## Permissions

| Action | Site/Tenant Admin | Safety Coordinator | Supervisor / Safety Director | Any permitted viewer |
|---|---|---|---|---|
| Configure a Compliance Drill Requirement | ✅ | ❌ | ❌ | ❌ |
| Log a Compliance Drill / record exit times / component checks | ✅ | ✅ | ✅ | ❌ |
| Confirm an auto-suggested Improvement Action | ❌ | ❌ | ✅ | ❌ |
| View logged drills / exit reports | ✅ | ✅ | ✅ | inherits site RBAC/ABAC |

## Non-Functional / Constraints

- Overdue-requirement alerting never blocks any operational action.
- Occupant Exit Report's aggregate metrics are never presented as equivalent to a real per-occupant muster reconciliation — the gap is honest and flagged, not silently papered over.
- A failed Drill Component Check never auto-creates a real Improvement Action without explicit human confirmation, regardless of how routine or clear-cut the failure appears.

## Acceptance Criteria

- [ ] A Compliance Drill Requirement with `required_cadence_days = 90` and no Compliance Drill Log in the last 90 days raises a Duration Watchdog instance without blocking any site operation.
- [ ] Logging a Compliance Drill Log with no `requirement_ref` succeeds — an ad hoc/unscheduled drill is still loggable.
- [ ] An Occupant Exit Report's per-zone exit times are recorded and queryable per drill, with no dependency on any per-occupant roster mechanism.
- [ ] A Drill Component Check marked `fail` produces a proposed Improvement Action that does not become real until a Supervisor/Safety Director explicitly confirms it.
- [ ] A confirmed corrective action carries `source_type = compliance_drill` and a valid `source_ref` pointing at the originating Compliance Drill Log.
- [ ] `Exercise`'s `activity_type` reads `exercise`, not `drill`, with no remaining collision against this doc's `compliance_drill` type.
- [ ] Compliance Drill Log records never surface as dedup candidates in Entity Registry Core's merge review queue.

## Open Questions

- Exact jurisdiction/regulation reference structure (free text vs. a real Compliance Standards Matrix once Module 21 exists) — not committed here.
- Whether Occupant Exit Report should retrofit to consume Module 6's real roster data once that module exists — forward reference only.
- Exact per-zone exit-time capture mechanism (manual entry vs. a future IoT/badge-scan-based automatic timing) — a content/technical-spec concern, not committed here.
