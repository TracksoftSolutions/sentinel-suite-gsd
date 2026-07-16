# Incident Action Checklists

## Overview

Incident Action Checklists is largely a retrofit/closure doc, in the spirit of BOLO & Trespass Alerts — the closest sibling mechanism, EOC Activation Checklists' Checklist Template/Run, already exists (ordered steps, checked/skipped, informational-only-never-blocking). What's genuinely new here is MODULES.md's own framing: **"Mobile Task Assignments: Assigning checklist actions to field personnel via mobile app"** — routing individual steps to specific responders, not just one person self-checking a list — plus a real starter catalog for the four named scenarios (active shooter, hazmat spill, severe weather, utility outage), and satisfying the full-procedure back-reference contract Pre-Incident Plans left for this doc.

Five elicited decisions, all the recommended option:

1. **Widen EOC Activation Checklists' Checklist Template/Run rather than build a parallel mechanism** — the platform's now-standard "promote on second consumer" move: a new `category = incident_response`, a widened anchor (Call, Incident, or EOC Activation, not just the latter), and an optional per-item assignment are all additive to the existing mechanism rather than a second "ordered steps you check off" system.
2. **Task assignment is per-item, to different field personnel, not per-checklist to one person** — matches MODULES.md's plural framing and real multi-unit response practice (Officer A secures the perimeter, Officer B evacuates a wing), and mobile-pushes each assignee their own task directly.
3. **A Checklist Run anchors to a Call, an Incident, or an EOC Activation** — the same widened-anchor shape already used for Operational Period/ICS Role Assignment — so a time-critical checklist (active shooter) can start the moment a matching Call comes in, without waiting for a formal Incident to exist first.
4. **A real starter catalog ships for the four MODULES.md-named scenarios**, explicitly and permanently flagged as illustrative, non-authoritative default content — a tenant's Safety/Security leadership must review and customize before relying on it operationally; the platform tracks whether a template has ever been edited so an unreviewed starter is visibly distinguishable from a reviewed one.
5. **Checklist Template gains `full_procedure_ref`**, fulfilling Pre-Incident Plans' explicit contract (that doc's FR #14) — a Checklist Template suggested by a Preplan can link back to that Preplan's own full procedure content, so an abbreviated field step can point to fuller detail.

## Actors & Roles

- **Site Admin / IC / Safety Director** — authors and customizes Checklist Templates (including reviewing/editing starter content), starts Checklist Runs, assigns items to field personnel.
- **Dispatcher** — starts a Checklist Run from a matched Preplan's suggested list or manually from the catalog, assigns items, monitors run/assignment status.
- **Field Officer / Guard (assignee)** — receives a mobile-pushed task, completes or skips their assigned item(s); can also act on any unassigned item they're authorized to work.
- **Any authorized responder on the Incident/Call** — can check/skip any item, not only their own assigned ones (see FR #6) — assignment routes attention, it isn't an access gate.

## User Stories

- As a **Dispatcher**, when a Preplan surfaces on a matching Call, I want to start its suggested checklist(s) with one action, not re-find them in a catalog.
- As an **IC**, I want to assign "secure the perimeter" to Officer A and "evacuate wing 2" to Officer B on the same active-shooter checklist, each notified directly on their phone.
- As a **Field Officer**, I want my assigned task to reach me as a clear, one-tap-to-open mobile notification, not something I have to go looking for.
- As a **Field Officer**, I want to mark my own assigned step complete even if I'm briefly disconnected, and have it sync once I'm back online.
- As a **Safety Director**, I want a starter Active Shooter / Hazmat Spill / Severe Weather / Utility Outage checklist available out of the box, clearly marked as something my team needs to review and adapt, not a certified procedure we can rely on unreviewed.
- As an **IC**, I want to start a relevant checklist manually even when nothing auto-matched from a Preplan.

## Functional Requirements

### Widened Checklist Template/Run (retrofit — full base mechanics owned by [eoc-activation-checklists.md](../5-emergency-management/eoc-activation-checklists.md))
1. **Checklist Template gains a third `category` value, `incident_response`** *(retrofit)* — alongside the existing `activation`/`system_verification`, using the identical `items[]`/versioning/`trigger_on_activation` shape unmodified.
2. **Checklist Template gains `full_procedure_ref`** (nullable, forward-reference — a Preplan or a specific `PreplanAttachmentAssociation`'d Document) *(retrofit)*, satisfying Pre-Incident Plans' FR #14 contract. Auto-populated when a Checklist Template is authored inline from a Preplan's `suggested_checklist_refs[]` editing flow; settable directly otherwise.
3. **Checklist Run's anchor widens from EOC-Activation-only to `anchor_type` (`eoc_activation`, `call`, `incident`) + `anchor_ref`** (nullable — a System Checklist still runs standalone) *(retrofit)* — the same widened-anchor shape already proven for Operational Period and ICS Role Assignment. A `call`-anchored run that later escalates to an Incident is **not** automatically re-anchored — it stays attached to the Call it actually started against, the same "never silently rewrite what already happened" discipline used for plan-version pinning.
4. Starting a Checklist Run from a matched Preplan's `suggested_checklist_refs[]` (Command/Action Bus action, available wherever the Preplan is surfaced) pins the referenced template version and sets `anchor_ref` to the current Call/Incident — no new mechanics beyond ordinary Checklist Run creation.
5. A Checklist Run can also be **started manually from the full catalog** of `incident_response`-category templates on any open Call/Incident, regardless of any Preplan match — the same "browse manually" fallback already established for Preplans themselves.

### Per-item task assignment to field personnel
6. **Checklist Item Check gains `assigned_to_ref` (nullable, FK → Person), `assigned_by`, `assigned_at`** *(retrofit)*. **Assign Checklist Item** (Command/Action Bus action) sets these against an on-duty Person, independently per item — different items on the same Run can carry different assignees.
7. **Assignment routes attention, it does not gate who may act on an item** — any user authorized on the anchoring Call/Incident can check/skip any item, assigned or not, the same self-or-on-behalf-of posture used throughout the platform (a radio failure or a reassigned unit shouldn't leave a step permanently blocked on one specific person).
8. Assigning an item fires a new **Checklist Item Assigned** Notification Category (default High tier, tenant-adjustable per Notifications Engine's existing category-override mechanism) — mobile push, deep-linking directly to that item.
9. **Marking your own assigned item checked/skipped is offline-capturable** — an append to an existing record's timeline by its sole assigned actor, the same class as completing your own Patrol or Checkpoint Scan under the platform's offline three-class contract. Assigning an item to someone else remains an ordinary online Command/Action Bus action (it mutates a shared record).

### Starter catalog — illustrative, non-authoritative default content
10. **Four platform-provided starter Checklist Templates** ship at `category = incident_response`: Active Shooter Response, Hazmat Spill Response, Severe Weather Response, and Utility Outage Response (illustrative content below) — available to every tenant, editable/versionable like any other Checklist Template.
11. **`is_platform_starter_content` (bool)** starts `true` on every starter template and flips permanently to `false` the first time a tenant edits it — driving a persistent, non-dismissable UI indicator ("unreviewed platform default — customize before relying on this operationally") on any starter template that's never been touched. This is a genuine safety/liability control, not decoration.
12. Starter content is deliberately generic and grounded in well-established public frameworks (DHS/FBI Run-Hide-Fight for active shooter response; OSHA/EPA hazmat baseline practice; NOAA/FEMA severe weather guidance) rather than invented from scratch — but is explicitly **not** a certified or jurisdiction-specific procedure, and this doc makes no claim that it is.

**Active Shooter Response** (illustrative)
1. Call 911 / notify Dispatch immediately with location, direction of threat, and description if known.
2. Broadcast a lockdown/shelter-in-place alert to the affected zone(s) and, where safe, adjacent zones.
3. Direct occupants per Run-Hide-Fight guidance: evacuate via a safe route if one exists, otherwise secure in place (lock/barricade, silence phones, stay out of sight).
4. Guide arriving law enforcement to the scene; do not attempt direct engagement unless the tenant's own policy and personnel qualifications explicitly authorize it.
5. Establish and communicate a rally/staging point for responding law enforcement and mutual aid.
6. Begin accounting for personnel/occupants once the area is declared secure by law enforcement.
7. Preserve the scene and avoid disturbing evidence pending investigation.
8. Initiate post-incident occupant notification/reunification procedures.

**Hazmat Spill Response** (illustrative)
1. Evacuate the immediate area and establish a safety perimeter.
2. Identify the substance if safely possible (reference the Location's Preplan/attached safety data sheet if available) — do not approach without proper PPE/training.
3. Notify Dispatch/IC and, where required by substance/quantity, external hazmat response and regulatory agencies.
4. Shut off or isolate the source if it can be done safely (e.g., via a known Preplan Utility Point).
5. Restrict access/ventilation as appropriate to the substance.
6. Await qualified hazmat response before re-entry; do not attempt cleanup without proper certification.
7. Document exposure of any personnel for medical follow-up.
8. Log the incident for regulatory reporting as applicable.

**Severe Weather Response** (illustrative)
1. Monitor the National Weather Service / configured weather overlay feed for the active alert type.
2. Notify occupants of the appropriate protective action (shelter-in-place, evacuate, or move to a designated safe area).
3. Direct personnel to pre-identified shelter locations away from windows/exterior walls.
4. Suspend outdoor patrols/activities until the event passes.
5. Account for personnel/occupants once sheltered.
6. Monitor for follow-on hazards (flooding, downed lines, structural damage) before resuming normal operations.
7. Conduct a post-event facility damage/safety walk-through before reopening affected areas.

**Utility Outage Response** (illustrative)
1. Confirm scope of the outage (single building, campus-wide, provider-wide).
2. Verify life-safety systems (fire alarm, emergency lighting, access-control fail-safe/fail-secure state) are functioning on backup power.
3. Notify affected occupants and provide guidance (e.g., elevators out of service, use stairwells).
4. Contact the utility provider and/or facilities/engineering for restoration ETA.
5. Monitor access-controlled doors for their configured fail-safe/fail-secure state and post personnel if needed.
6. Log outage start/restoration time for reporting and any tenant SLA obligations.
7. Conduct a systems check once power is restored before standing down.

## Data Model / Fields

*(Base Checklist Template / Checklist Run / Checklist Item Check fields are owned by [eoc-activation-checklists.md](../5-emergency-management/eoc-activation-checklists.md); only new/retrofitted fields are listed here.)*

**Checklist Template** — retrofitted fields
- category gains `incident_response` (alongside existing `activation`, `system_verification`)
- full_procedure_ref (nullable, forward-reference — a Preplan or Document)
- is_platform_starter_content (bool, default true for shipped starters; permanently false after first tenant edit)

**Checklist Run** — retrofitted fields
- anchor_type (eoc_activation, call, incident), anchor_ref (nullable) — replaces the prior `eoc_activation_ref`-only field

**Checklist Item Check** — retrofitted fields
- assigned_to_ref (nullable, FK → Person), assigned_by, assigned_at

## States & Transitions

- **Checklist Template, Checklist Run, Checklist Item Check:** all inherit EOC Activation Checklists' unmodified lifecycle (`active`/`archived` versioned Template; `in_progress` → `completed` Run, never gated on item completeness; one-row-per-interaction Item Check, a correction is a new row).
- **`is_platform_starter_content`:** `true` → `false`, one-way, flipped the instant any edit is saved against a starter template.

## Integrations

- **EOC Activation Checklists** *(retrofit)*: owns the base Checklist Template/Run/Item Check mechanics this doc widens; this doc's `incident_response` category, widened anchor, and per-item assignment are additive, not a parallel mechanism.
- **Pre-Incident Plans**: source of `suggested_checklist_refs[]`, the trigger for starting a Checklist Run pre-suggested by a matched Preplan; this doc's `full_procedure_ref` satisfies that doc's own explicit contract (FR #14).
- **Notifications Engine**: new Checklist Item Assigned category, mobile push, deep-linking to the assigned task.
- **Offline Data Sync**: self-completion of an assigned item follows the platform's existing append-only, sole-actor-execution offline class — no new offline mechanism.
- **Call Intake & Logging / Incident Reporting & Management**: source of the Call/Incident records a Checklist Run can anchor to.
- **Command/Action Bus**: Start Checklist Run (from Preplan or manually), Assign Checklist Item, Check/Skip Item (inherited), Complete Run (inherited) register as actions.
- **Structured Logging & Audit Trails**: every assignment and item action is audit-tier, inherited from the base mechanism unmodified.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin/IC/Safety Director | Dispatcher | Field Officer (assignee) | Any authorized responder |
|---|---|---|---|---|---|---|
| Author/edit a Checklist Template (including starter content) | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ | ❌ |
| Start a Checklist Run (from a Preplan or manually) | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Assign a Checklist Item | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Check/skip an item (assigned or not) | ✅ | ✅ | ✅ | ✅ | ✅ (own + any) | ✅ |
| Receive assignment notification | — | — | — | — | ✅ | — |

## Non-Functional / Constraints

- Checklist Item Assigned push delivery meets Real-Time Delivery's standard ≤5s server-to-device target for safety-relevant pushes.
- An unreviewed starter template (`is_platform_starter_content = true`) must display its non-dismissable "unreviewed default" indicator everywhere it's referenced — the Template itself, any Preplan suggesting it, and any live Checklist Run started from it — never silently presented as tenant-approved procedure.
- Assignment/completion never blocks Run completion or any other operational action, consistent with the platform's standing compliance-never-gates rule (now confirmed a fourth module/doc in a row).
- Starter content carries no claim of jurisdictional, legal, or life-safety certification; this doc's own text and the platform's UI must not imply otherwise.
- WCAG 2.1 / Section 508 accessible authoring, assignment, and mobile task views, day one.

## Acceptance Criteria

- [ ] A matched Preplan's suggested checklist starts a Checklist Run pinned to the correct template version, anchored to the current Call/Incident, with `full_procedure_ref` resolving back to that Preplan.
- [ ] A Checklist Run can be started manually from the `incident_response` catalog on an Incident with no matching Preplan.
- [ ] A Checklist Run started against an open Call remains anchored to that Call even after the Call escalates to a formal Incident.
- [ ] Assigning two different items on the same Run to two different on-duty officers correctly notifies each of only their own item, via mobile push.
- [ ] An item with no assignee can still be checked/skipped by any authorized responder on the Incident.
- [ ] Marking your own assigned item complete while briefly offline syncs correctly once connectivity resumes, without loss or duplication.
- [ ] A never-edited starter Checklist Template visibly displays its unreviewed-default indicator; editing and saving it once permanently clears that indicator.
- [ ] Completing a Checklist Run with unaddressed/unassigned items succeeds — completion is never gated on full coverage.

## Open Questions

- Whether Checklist Item Assigned's default priority tier (High, not Critical) is right for every starter scenario, or whether active-shooter-specific assignments warrant Critical-tier delivery by default — a content/config tuning decision, not committed here.
- Exact jurisdiction-specific customization guidance to hand tenants alongside the starter catalog (e.g., a review checklist for their Safety/Security leadership) — a content/documentation task, not a requirements-doc-level decision.
- Whether a `call`-anchored Checklist Run should gain any lightweight visual linkage once its anchoring Call escalates to an Incident (beyond staying correctly anchored to the original Call) — not committed here, a plausible light touch-up once real usage patterns are observed.
