# Exercise & Drill Planner (HSEEP-aligned)

## Overview

Exercise & Drill Planner resolves the forward reference both After-Action Reports and Improvement Plan (IP) Tracking left open — the `exercise` anchor and `drill` source type. It builds the platform's second full plan/execution pair (after Guard Tour's Route → Route Assignment): a versioned **Exercise Design** (HSEEP form fields, scenario injects, evaluation criteria) paired with **Exercise**, the real Activity extension occurrence — the same anchor AAR was already reserving and the same source IP Tracking already reserved.

This doc is deliberately scoped to HSEEP's three designed exercise types (tabletop, functional, full-scale) — **not** the routine regulatory fire-drill/evacuation logging MODULES.md separately names as Drill Compliance Logging (the very next doc), which stays its own lighter, simpler mechanism. An organization plans and evaluates a full-scale exercise here; it logs Tuesday's fire drill there.

Four elicited decisions:

1. **Real plan/execution split, confirmed** — Exercise Design is the versioned plan (never itself an Activity, mirrors Route/Tour Definition exactly); Exercise is the Activity extension for one real occurrence.
2. **Exercise Fidelity Mode is tenant/design-configurable, not fixed** — a tabletop or Limited Scope Performance Test can stay a self-contained narrative/inject-response log with zero live records; a functional or full-scale exercise can instead run in **live simulation**, where participants create real Incident/Dispatch/Call/etc. records exactly as in live operations, each flagged training via a new base-level Activity Registry field. Per explicit user direction ("can be done either way... adds realism on exact control surfaces, but can also scale way down for LSPTs"), both modes are first-class, not one a compromise fallback for the other.
3. **HSEEP form templates stay this doc's own structured plan fields**, not routed through ICS Forms Engine — consistent with AAR's own choice; a versioned planning document has a different shape and cadence than that engine's Operational-Period-cyclic operational forms.
4. **A below-target Evaluation Score auto-suggests a draft Improvement Action** (`source_type = drill`) that a Lead Evaluator/Exercise Director must explicitly confirm — the "automation proposes, human confirms" discipline applied to exercise scoring for the first time.

Live-simulation training data is a genuinely new, well-bounded cross-cutting concern: a training-flagged Incident must never be confusable with a real one on a live console. This doc retrofits Activity Registry with the flag itself and states the filtering/visual-treatment contract every live-operational consumer (Active Incident Queue, UOP Map, Multi-Incident Console, Command Center Wallboard View, Notifications Engine) must honor — the same registration-and-consumer-filters discipline Signal Disposition and Queue Role opt-in already established, applied to a new axis.

## Actors & Roles

- **Exercise Director** — authors/versions Exercise Designs, schedules Exercises, confirms auto-suggested Improvement Actions.
- **Lead Evaluator** — oversees Evaluation Criteria scoring, can also confirm auto-suggested Improvement Actions.
- **Controller** — delivers Scenario Injects during a live Exercise.
- **Evaluator** — records Evaluation Scores against criteria during/after the Exercise.
- **Participant** — responds to injects; in `live_simulation` mode, operates real platform record types exactly as in live operations.
- **Site / Tenant Admin** — sets the tenant-default Exercise Fidelity Mode and training-notification policy.

## User Stories

- As an **Exercise Director**, I want to design a full-scale exercise once and reuse the same design for next year's re-run, without rebuilding objectives/injects/criteria from scratch.
- As a **Controller**, I want to deliver each scenario inject at roughly its planned time and log exactly when it went out.
- As an **Evaluator**, I want to score each objective against its criteria in real time, not reconstruct it afterward from memory.
- As an **Exercise Director** running a full-scale exercise, I want participants to actually create a real Dispatch and Incident so they practice on the exact console they'd use in a real event — clearly marked training so nobody mistakes it for a live call.
- As an **Exercise Director** running a small tabletop, I want a lightweight narrative log with no live records at all — this doesn't need the weight of the full-scale option.
- As a **Dispatcher not participating in an exercise**, I want training-flagged activity to stay off my live console by default, with an unmistakable banner if I ever do view it.
- As a **Lead Evaluator**, I want a below-target score to propose a corrective action automatically, so a real finding doesn't get lost between the hotwash and the eventual AAR.
- As an **Exercise Director**, once the exercise concludes, I want to generate its AAR the same way I would for a real EOC Activation or Incident.

## Functional Requirements

### Exercise Design (plan layer)
1. A tenant-authored **Exercise Design** carries `exercise_type` (`tabletop`, `functional`, `full_scale` — HSEEP's operations/discussion-based taxonomy collapsed to the three MODULES.md names), `objectives[]`, `scenario_summary`, and a default **Exercise Fidelity Mode** (`narrative_only`/`live_simulation`) that a scheduled Exercise may override. Edits version rather than mutate in place — the same discipline as Route/Tour Definition — and a standing Exercise pins the design version it started against.
2. **Scenario Inject Template** (child of Exercise Design): `sequence_number`/`planned_offset_minutes`, `inject_type` (message, injected_data, simulated_signal), content, and optional expected-response notes — the plan-level catalog Controllers deliver from during the live Exercise.
3. **Evaluation Criterion** (child of Exercise Design): `objective_ref`, `criterion_text`, `scoring_scale` (e.g., met/partially_met/not_met, or numeric), and an optional `target_threshold` — the value below which a score is considered a finding worth a corrective action (FR #9).

### Exercise (execution layer, Activity extension)
4. **Exercise** registers as its own Activity extension (`entity_id` shared PK, FK → Activity, `activity_type = drill` — Activity Registry's own base type list already anticipated this value) — `exercise_design_ref` (pinned version), `scheduled_start`/`scheduled_end`, `actual_start`/`actual_end`, `status` (`scheduled` → `in_progress` → `concluded`/`cancelled`), and `fidelity_mode` (resolved from the Design's default, with an explicit per-Exercise override — the platform's standard explicit-beats-default chain — and pinned once the Exercise starts).
5. **This Exercise IS the `exercise` anchor type After-Action Reports reserved, now reachable.** *(Retrofit — see [after-action-reports.md](after-action-reports.md).)* A `concluded` Exercise is eligible for the same Generate AAR Draft action as a deactivated EOC Activation or concluded Incident — Timeline Reconstruction for an Exercise anchor includes Inject Delivery events and Evaluation Scores alongside (for `live_simulation` exercises) the ordinary activity-axis tree of real records created during it.

### Inject delivery and scoring
6. **Inject Delivery** registers as a lightweight child Activity extension of Exercise (mirroring Checkpoint Scan's event-within-Patrol shape): `exercise_ref`, `scenario_inject_template_ref` (nullable — an ad hoc, unplanned inject is allowed), `delivered_at`, `delivered_by` (Controller), `participant_response_notes`.
7. **Evaluation Score** records one evaluator's judgment against one criterion, optionally scoped to a specific participant/unit when the criterion is individually rather than exercise-wide scored: `exercise_ref`, `evaluation_criterion_ref`, `evaluator_ref`, `scored_participant_ref` (nullable), `score_value`, `notes`.
8. **This is the `drill` source type Improvement Plan (IP) Tracking reserved, now reachable.** *(Retrofit — see [improvement-plan-ip-tracking.md](improvement-plan-ip-tracking.md).)*
9. A recorded Evaluation Score at or below its criterion's `target_threshold` auto-drafts a proposed Improvement Action (`source_type = drill`, `source_ref = this Exercise's entity_id`, pre-filled recommendation text referencing the criterion and score) — never auto-created outright. A Lead Evaluator or Exercise Director must explicitly confirm it before it becomes a real, tracked Improvement Action — the same "automation proposes, human confirms" discipline already applied to AI drafts and watchlist matches, now applied to exercise scoring for the first time. This lets a real finding get logged at the hotwash, independent of and prior to whatever the Exercise's own eventual formal AAR later covers.

### Exercise Fidelity Mode and training data
10. **`narrative_only` mode**: the Exercise's own records (Inject Delivery, Evaluation Score) are the entire captured record — no live operational records of any kind are created. Appropriate default for tabletop and Limited Scope Performance Test-scale exercises.
11. **`live_simulation` mode**: participants create real platform records (Incident, Call, Dispatch, and so on) using the exact same workflows as live operations — full realism on the actual control surfaces they'd use in a genuine event. Every such record carries a new base-level Activity Registry field, `training_exercise_ref` *(retrofit — see [activity-registry.md](../0.5-master-records/activity-registry.md) #19)*, set for the duration of participant activity within the Exercise.
12. **Default operational consumers filter out training-flagged Activities.** Active Incident Queue, UOP Map, Multi-Incident Console, and Command Center Wallboard View all default to `is_training = false` — a live console never shows exercise traffic mixed into real operational data by default. Each of these already has its own explicit-opt-in viewing mechanism (a Saved View, a Display Profile, a Console Layout); an **Exercise/Training View** toggle (reusing that same mechanism, not a new one) shows training-flagged activity instead, with a **mandatory, persistent, non-suppressible "TRAINING" visual treatment** — never rendered in a way indistinguishable from a real record, a hard safety property given the well-documented real-world risk of drill/real confusion.
13. **Notifications for training-flagged Activities never reach a non-participant looking indistinguishable from a real alert.** A tenant-configurable Training Notification Policy sets either delivery restricted to enrolled Exercise participants only, or platform-wide delivery with a mandatory, non-removable `[TRAINING]` prefix — the Notifications Engine's existing delivery mechanism, one new routing rule, no new infrastructure.

## Data Model / Fields

**Exercise Design** (tenant-authored, versioned plan; not an Activity)
- design_id, tenant_id, version, status (active, archived)
- exercise_type (tabletop, functional, full_scale)
- objectives[], scenario_summary
- default_fidelity_mode (narrative_only, live_simulation)
- participating_org_refs[] (nullable — Mutual Aid Organization/Organization Party references)

**Scenario Inject Template** (child of Exercise Design)
- template_id, design_ref, sequence_number, planned_offset_minutes
- inject_type (message, injected_data, simulated_signal), content, expected_response_notes (nullable)

**Evaluation Criterion** (child of Exercise Design)
- criterion_id, design_ref, objective_ref, criterion_text
- scoring_scale, target_threshold (nullable)

**Exercise** (Activity extension; entity_id is the shared PK, FK → Activity; activity_type = drill)
- exercise_design_ref (pinned version)
- scheduled_start, scheduled_end, actual_start (nullable), actual_end (nullable)
- status (scheduled, in_progress, concluded, cancelled)
- fidelity_mode (narrative_only, live_simulation — resolved and pinned at start)

**Inject Delivery** (Activity extension; entity_id is the shared PK, FK → Activity)
- exercise_ref, scenario_inject_template_ref (nullable)
- delivered_at, delivered_by, participant_response_notes

**Evaluation Score** (local child record of Exercise)
- score_id, exercise_ref, evaluation_criterion_ref, evaluator_ref
- scored_participant_ref (nullable), score_value, notes
- suggested_improvement_action_ref (nullable — set once FR #9's auto-draft fires)

## States & Transitions

- **Exercise Design:** `active` → `archived`, standard versioned-Definition lifecycle.
- **Exercise:** `scheduled` → `in_progress` → `concluded`/`cancelled` (terminal). `concluded` is what makes it AAR-eligible.
- **Inject Delivery / Evaluation Score:** created once, immutable thereafter — a correction is a new row, not an edit, consistent with the platform's audit-first posture for point-in-time records.

## Integrations

- **After-Action Reports** *(retrofitted)*: a `concluded` Exercise is now a reachable `anchor_type = exercise`; Timeline Reconstruction for that anchor includes Inject Delivery/Evaluation Score alongside any live-simulation records.
- **Improvement Plan (IP) Tracking** *(retrofitted)*: a confirmed auto-suggested Improvement Action is the first reachable use of `source_type = drill`.
- **Activity Registry** *(retrofitted)*: gains `training_exercise_ref` (nullable, FK → Exercise) and a derived `is_training` boolean at the base Activity level — this doc's Exercise is what populates it; every other Activity-producing module inherits the field automatically, with zero action required unless that module's own live views need the default-filter behavior described in FR #12.
- **Active Incident Queue / UOP Map / Multi-Incident Console / Command Center Wallboard View**: each must default-filter `is_training = true` out of its standard view and offer an explicit Exercise/Training View toggle with mandatory TRAINING visual treatment, per FR #12 — a light behavioral contract on each, not a data-model change to any of them.
- **Notifications Engine**: owns delivery of the new Training Notification Policy routing rule, per FR #13 — no new delivery infrastructure.
- **Mutual Aid Organization (EOC Logistics Hub)**: source of `participating_org_refs[]` when an exercise involves outside agencies.
- **Command/Action Bus**: Author/version Exercise Design, Schedule/Start/Conclude Exercise, Deliver Inject, Record Evaluation Score, and Confirm Suggested Improvement Action all register as actions.
- **Structured Logging & Audit Trails**: Exercise lifecycle transitions and every Evaluation Score are audit-tier.
- **Drill Compliance Logging (Module 5, next doc)**: explicitly separate and lighter-weight — routine regulatory fire/evacuation drills are not modeled as an Exercise Design/Exercise pair; this doc's mechanism is reserved for designed, evaluated HSEEP exercises.

## Permissions

| Action | Site/Tenant Admin | Exercise Director / Lead Evaluator | Controller / Evaluator | Participant |
|---|---|---|---|---|
| Author/version Exercise Design | ✅ | ✅ | ❌ | ❌ |
| Schedule/start/conclude an Exercise | ✅ | ✅ | ❌ | ❌ |
| Deliver a Scenario Inject | — | ✅ | ✅ (Controller) | ❌ |
| Record an Evaluation Score | — | ✅ | ✅ (Evaluator) | ❌ |
| Confirm an auto-suggested Improvement Action | — | ✅ | ❌ | ❌ |
| Configure tenant-default Fidelity Mode / Training Notification Policy | ✅ | ❌ | ❌ | ❌ |
| Operate real record types during `live_simulation` | inherits the underlying module's own real-operations permissions | | | ✅ |

## Non-Functional / Constraints

- A training-flagged record is never rendered in any default live view in a way indistinguishable from a real one — this is a hard, non-configurable safety property, not a tenant preference.
- `training_exercise_ref` is set once, at record creation within an active `live_simulation` Exercise, and is never retroactively applied or removed — a real record and a training record are never the same row viewed two ways.
- Auto-suggested Improvement Actions (FR #9) never become real Improvement Actions without explicit human confirmation — no threshold, however far below target, bypasses this gate.
- Exercise Design edits version rather than mutate in place; a scheduled or in-progress Exercise's pinned design version never shifts under it mid-exercise.

## Acceptance Criteria

- [ ] An Exercise Design can be versioned, with an already-scheduled Exercise continuing to reference the version it was created against after a later edit.
- [ ] A `narrative_only` Exercise produces zero live operational records — only Inject Delivery and Evaluation Score entries.
- [ ] A `live_simulation` Exercise's participant-created Incident carries `training_exercise_ref` set and `is_training = true`, and does not appear in Active Incident Queue's default view.
- [ ] Toggling a console into Exercise/Training View surfaces training-flagged records with a persistent, non-removable TRAINING indicator.
- [ ] A concluded Exercise is eligible for Generate AAR Draft, identically to a deactivated EOC Activation or concluded Incident.
- [ ] Recording an Evaluation Score at or below its criterion's `target_threshold` produces a proposed Improvement Action that does not become real until a Lead Evaluator/Exercise Director explicitly confirms it.
- [ ] A confirmed auto-suggested Improvement Action carries `source_type = drill` and a valid `source_ref` pointing at the originating Exercise.
- [ ] A Training Notification Policy set to participants-only never delivers a training-flagged alert to a non-participant's device.
- [ ] Cancelling an Exercise before `concluded` makes it ineligible for AAR generation.

## Open Questions

- Exact HSEEP form field granularity (IPOSC/SITMAN/MSEL-equivalent structured sections) beyond objectives/scenario summary — a content/config design task, not committed here.
- Whether `live_simulation` needs any structural safeguard beyond the training flag and view-filtering contract (e.g., a hard block preventing a training-flagged Incident from ever triggering a real Domain Events automation like auto-dispatch) — flagged as a real risk worth a closer look at technical-spec time, not resolved here.
- Whether Evaluation Criteria should support per-participant weighted scoring/aggregate exercise grades — not committed, MODULES.md's "scorecards" framing is satisfied by the raw score records themselves for now.
- Exact reconciliation with a future Module 12 performance-reporting feature that will need to exclude `is_training` records from real KPIs — forward reference only.
