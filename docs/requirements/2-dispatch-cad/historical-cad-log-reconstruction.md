# Historical CAD Log Reconstruction

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

Not a new logging system. Dispatch logs are exactly DAR-shaped: a **filtered, chronological view over Activity Registry**, directly reusing DAR's established "report = filtered Activity view, snapshotted to an immutable Document" architecture — the same pattern already proven by Passdown and Incident Report. **This is explicitly not sourced from Structured Logging & Audit Trails** — that system is a separate, background, rarely-accessed compliance mechanism, and this doc doesn't rebuild reconstruction from it. High-precision timestamps (what MODULES.md's original "audit logs" wording was really asking for) are already sitting on the actual business records: Dispatch's `phase_timestamps`, Incident Update's `update_timestamp`, Call's `received_at`/`triaged_at`/`queued_at`/`closed_at`, and so on — this doc reads those directly.

**Two filter axes, both DAR-shaped** (person/site/time there; dispatcher-or-activity/time here):

1. **Dispatcher/shift axis** — filter by a specific dispatcher (Person) and a time window (an interim Shift Window, same deferred-pending-Post-Schedule-Builder posture DAR already established, or a custom range). Produces a **strictly chronological interleave** of every action that dispatcher performed, across however many different Calls/Incidents/Patrols they touched during that window — actions on Incident 1, then 3, then 1, then 2 show in exactly that order. This is the genuinely new shape here: it's the opposite grouping from Active Incident Queue's Card/Feed model, where each Activity's own timeline is naturally coherent in isolation (Incident 1's updates, Patrol 3's scans) but a single dispatcher's real sequence of actions cuts across all of them.
2. **Activity axis** — filter by a specific Call/Incident/Dispatch/Patrol and a time window. Produces a complete, potentially multi-dispatcher/multi-unit chronological reconstruction of everything that happened on that one situation, recursively walking its full descendant tree via each type's existing direct parent-ref field — **unbounded depth**, unlike Active Incident Queue's deliberate one-level Card/Feed nesting cap. That cap existed for live-board clutter/performance reasons; a point-in-time, on-demand reconstruction has neither constraint, so a genuine Route Assignment → Patrol → Checkpoint Scan walk (the case Active Incident Queue explicitly punted on) is fully in scope here.

Either axis's result can be snapshotted into an immutable **Reconstruction Report** Document, identical discipline to every other Report-shaped feature in the platform — for a formal review, an internal-affairs inquiry, a legal proceeding, or a performance review.

## Actors & Roles

- **Supervisor** — runs reconstructions for review/performance purposes, generates ad hoc Reconstruction Reports.
- **Records Admin** — same access, plus resolves any dedup-adjacent data-quality concerns feeding into the reconstruction.
- **Investigator** — future consumer, likely feeding Investigation Management's eventual Case Files.
- **Tenant Admin** — registers which Activity/Feed types participate in dispatcher-axis reconstruction (Log Source Registration).

## User Stories

- As a **Supervisor**, I want to reconstruct exactly what one dispatcher did during their shift, in the literal order they did it, to review their handling of a busy night.
- As a **Supervisor**, I want to reconstruct everything that happened on one specific Incident, across every dispatcher and unit involved, for a full after-the-fact review.
- As an **Investigator**, I want a reconstructed timeline I can trust reflects real business timestamps already on the record, not a separately-maintained log that could drift from what actually happened.
- As a **Records Admin**, I want to generate an immutable, point-in-time snapshot of a reconstruction for a legal hold, so it can never be silently altered after the fact.

## Functional Requirements

### Dispatcher/shift-axis reconstruction
1. A reconstruction request specifies a dispatcher (Person) and a time window (an interim Shift Window, or a custom range).
2. The result pulls every Activity/Feed-type record where that dispatcher is the recorded actor, per each type's registered **Log Source Registration** (see below), within the time window.
3. Results are ordered strictly by each record's own relevant timestamp, interleaved across every Activity the dispatcher touched — never regrouped by parent Activity, even though several entries may belong to different Calls/Incidents.

### Activity-axis reconstruction
4. A reconstruction request specifies a root Card-role Activity (a Call, Incident, Patrol, Dispatch, etc.) and a time window.
5. The result recursively includes every descendant record reachable via existing direct parent-ref fields (reusing Active Incident Queue's already-registered `parent_link_field` values where applicable) — at any depth, not capped at one level.
6. Results are ordered strictly chronologically, showing every contributing dispatcher's/unit's actions interleaved as they actually occurred.

### Log Source Registration
7. Each Activity/Feed type participating in dispatcher-axis reconstruction registers (locally to this doc, not a retrofit to Activity Registry Core) which existing field identifies its acting dispatcher: e.g., Dispatch's `recorded_by`, Incident Update's `author_ref`, Agency Handoff Log's `handed_off_by`, Safety Check-in's `confirmed_by`.
8. A type with no registered actor field is excluded from dispatcher-axis results but can still appear on the activity axis, since that axis doesn't require actor attribution — it walks structure, not authorship.

### Report snapshot
9. **Generate Reconstruction Report** snapshots either axis's filtered result into an immutable Document (Document Registry's hash/version model), fixed at generation time and never retroactively updated — identical discipline to DAR Report, Passdown, and Incident Report.

## Data Model / Fields

**Log Source Registration** (local to this doc)
- registration_id, activity_type, actor_field (nullable — the field on this type that identifies the acting dispatcher, for dispatcher-axis reconstruction)

**Reconstruction Report** (Document extension — Document Registry's base fields apply)
- entity_id (PK, FK → Document)
- axis (dispatcher, activity)
- dispatcher_ref (nullable, set only for dispatcher axis), time_window (dispatcher axis)
- root_activity_ref (nullable, set only for activity axis), time_window (activity axis)
- included_entry_refs[] (snapshot, as of generation, in reconstructed chronological order)
- generated_by, generated_at

## States & Transitions

**Reconstruction Report:** created once, immutable — same as DAR Report/Incident Report.

## Integrations

- **Daily Activity Reports (DAR)**: this doc directly reuses DAR's established "report = filtered Activity Registry view, snapshotted to Document" architecture — the next consumer of that pattern after DAR, Passdown, and Incident Report, now applied to a dispatcher/shift-or-activity dual-axis instead of a single guard/site axis.
- **Activity Registry**: the universal source of every reconstructed record, same as every other consumer of that registry.
- **Active Incident Queue (CAD Console)**: source of the `parent_link_field` registrations the activity axis reuses for its tree walk — this doc deliberately does not inherit that doc's one-level nesting cap, since it was a live-board constraint, not a data limitation.
- **Document Registry**: source of the immutable Reconstruction Report snapshot mechanism.
- **Structured Logging & Audit Trails**: explicitly NOT a data source for this doc — that system remains the platform's separate, background compliance audit mechanism; this doc reconstructs from Activity Registry's own business-record timestamps instead.
- **Entity Registry Core**: source of the display-label mechanism every reconstructed entry renders through.
- **Investigation Management (future)**: a likely future consumer of activity-axis reconstruction for Case File assembly.

## Permissions

| Action | Dispatcher | Supervisor | Records Admin | Tenant Admin |
|---|---|---|---|---|
| Run a dispatcher-axis reconstruction (own shift) | ✅ | ✅ | ✅ | ❌ |
| Run a dispatcher-axis reconstruction (another dispatcher's shift) | ❌ | ✅ | ✅ | ❌ |
| Run an activity-axis reconstruction | ❌ | ✅ | ✅ | ❌ |
| Generate a Reconstruction Report | ❌ | ✅ | ✅ | ❌ |
| Configure Log Source Registrations | ❌ | ❌ | ❌ | ✅ |

A reconstruction is still filtered per the viewer's own ABAC/sensitivity rules at read time — same discipline as Entity Relationships & History's Interaction Timeline Viewer: the aggregate view never surfaces an entry the viewer couldn't see by opening that record directly.

## Non-Functional / Constraints

- Dispatcher-axis reconstruction must remain accurate under real interleaving load — a busy shift working several concurrent Calls/Incidents must reconstruct in exact chronological order, not activity-grouped order, even under high entry volume.
- Activity-axis reconstruction's unbounded-depth walk must stay performant for a deep hierarchy (Route Assignment → Patrol → Checkpoint Scan) without the live-board latency budget Active Incident Queue is held to — this is an on-demand report, not a continuously-rendered screen.
- A generated Reconstruction Report's immutability must be enforced at the data layer, identical requirement to DAR Report and Incident Report.
- ABAC/sensitivity filtering of individual reconstructed entries must be enforced at read time, per current viewer, same as Entity Relationships & History's own discipline.

## Acceptance Criteria

- [ ] A dispatcher-axis reconstruction for a dispatcher who worked Incident 1, then Incident 3, then back to Incident 1, then Incident 2 shows entries in that exact interleaved order — never regrouped by parent Activity.
- [ ] An activity-axis reconstruction for a Route Assignment correctly includes its Patrols and their Checkpoint Scans, three levels deep, in chronological order.
- [ ] A type with no registered actor field is correctly excluded from dispatcher-axis results but still appears correctly on an activity-axis reconstruction that includes it.
- [ ] Generating a Reconstruction Report produces an immutable Document; no code path can mutate `included_entry_refs[]` after generation.
- [ ] A Supervisor without ABAC clearance for a specific classified entry does not see it in either axis's reconstruction, even though the rest of the reconstruction is visible.
- [ ] Confirmed: no reconstruction query in this doc reads from Structured Logging & Audit Trails.

## Open Questions

- Several Module 2 records don't yet carry a per-action actor field the dispatcher axis needs for full fidelity (e.g., Call's `triaged_at`/`queued_at` transitions have no paired "by" field in call-intake-logging.md's current data model) — today's Log Source Registration covers what's already available and degrades gracefully by excluding unregistered types from the dispatcher axis; a future light retrofit sweep across affected docs may be needed for complete per-action attribution, not undertaken here.
- Dispatch's `recorded_by` is currently a single field rather than one per phase timestamp (per unit-dispatch-proximity-routing.md) — exact fidelity for "who logged which specific phase" in a dispatcher-axis reconstruction may need a light retrofit there; flagged, not solved here.
- Exact default Shift Window handling — same interim, pending-Post-Schedule-Builder posture already flagged throughout Module 1/2, not re-litigated here.
- Performance bounds for a very high-volume shift or a very deep activity-axis walk — a technical-spec-level concern.
- Whether Investigation Management's future Case Files should consume this doc's activity-axis reconstruction directly as a building block — deferred until that module is specified.
