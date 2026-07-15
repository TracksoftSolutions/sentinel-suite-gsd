# EOC Activation Checklists

## Overview

EOC Activation Checklists closes out Module 5 (8/8) with a generic **Checklist Template → Checklist Run** plan/execution mechanism — structurally the same "ordered steps, each independently completed" shape as Guard Tour's Tour Definition → Checkpoint Scan, applied here to procedural steps instead of physical checkpoints — serving both MODULES.md's Activation Checklists (escalation procedure) and System Checklists (backup power/radio/emergency-system verification) as two categories of one mechanism, plus a separate **Staff Call-up** broadcast built on the platform's existing internal Notifications Engine.

Four elicited decisions:

1. **One shared Checklist Template/Run mechanism, `category = activation` or `system_verification`** — avoids building two near-identical step-tracking mechanisms for what's structurally the same shape.
2. **Checklist tracking is informational only — it never gates or blocks Activate EOC Response**, the platform's standing rule that a compliance/tracking mechanism must never become an operational blocker (Guard Tour's verification-degrades-gracefully lesson, applied here to procedural checklists for the first time).
3. **Staff Call-up reaches a new, lightweight interim EOC Call-up Roster** (tenant-configured Persons and/or ICS Positions), not just current Position holders — real call-up needs to reach off-duty personnel not currently assigned to anything, explicitly flagged for reconciliation once Module 8 Personnel/scheduling exists.
4. **A Checklist Run auto-creates the moment EOC Activation fires**, for any Checklist Template flagged `trigger_on_activation` — the standard Domain Events trigger/effect split, no new automation mechanism.

## Actors & Roles

- **EOC Coordinator / Incident Commander** — works through Checklist Runs, sends Staff Call-up Broadcasts.
- **Any EOC role holder** — checks off individual Checklist Run items relevant to their own responsibility.
- **Site / Tenant Admin** — authors Checklist Templates, EOC Call-up Rosters, and Staff Call-up Templates.
- **EOC personnel (call-up recipient)** — acknowledges a Staff Call-up Broadcast.

## User Stories

- As an **EOC Coordinator**, the moment I activate EOC Response, I want a ready-to-work Activation Checklist already in front of me, not something I have to remember to start separately.
- As an **EOC Coordinator**, I want to skip a checklist item that doesn't apply this time without it blocking anything else I need to do.
- As a **facility engineer**, I want to run the System Checklist (backup power, radios, emergency systems) independent of any actual activation, as routine verification.
- As an **EOC Coordinator**, I want to broadcast a call-up notice to everyone on our EOC roster with one action, reaching people who aren't currently assigned to anything.
- As **EOC personnel**, I want to acknowledge a call-up notice with one tap, not a form to fill out.
- As an **EOC Coordinator**, I want to see who's acknowledged the call-up and who hasn't, so I know who to follow up with by phone.

## Functional Requirements

### Checklist Template (plan layer)
1. A tenant-authored **Checklist Template** carries `category` (`activation`, `system_verification`), `name`, an ordered `items[]` (each: `sequence_number`, `instruction_text`, `requires_note` — whether a note is expected, never enforced), and `trigger_on_activation` (bool, independent of category — a System Checklist can also auto-run on activation if a tenant wants immediate backup-power verification, or stay purely periodic/manual). Edits version rather than mutate in place, the same discipline as Route/Tour Definition.
1a. **`category` gains a third value, `incident_response`, and Checklist Template gains `full_procedure_ref` (nullable, forward-reference) and `is_platform_starter_content` (bool)** *(retrofit, by Incident Action Checklists)* — full mechanics owned by that doc; this doc's `items[]`/versioning shape is otherwise unchanged and unaware of the new category beyond accepting it as a value.

### Checklist Run (execution layer, Activity extension)
2. **Checklist Run** registers as its own Activity extension (`entity_id` shared PK, FK → Activity) — `template_ref` (pinned version), `category`, `eoc_activation_ref` (nullable — a System Checklist can run standalone, independent of any activation), `status` (`in_progress` → `completed`).
2a. **`eoc_activation_ref` widens to `anchor_type` (`eoc_activation`, `call`, `incident`) + `anchor_ref`** (nullable — standalone still allowed) *(retrofit, by Incident Action Checklists)* — this doc's own EOC-Activation-anchored usage is just `anchor_type = eoc_activation`, unaffected in behavior.
3. **A Checklist Run auto-creates for every `trigger_on_activation` Checklist Template the moment EOC Activation fires** — a Domain Events rule (EOC Activation created → auto-create Checklist Run per matching template), the standard trigger/effect split already used everywhere else (e.g., Signal Disposition's `incident_dispatch` tier), no new automation mechanism. A Checklist Run not tied to any activation is started manually (Command/Action Bus action).
4. **Checklist Item Check** (child of Checklist Run) records one interaction per template item: `status` (`checked`, `skipped`), `checked_by`, `checked_at`, optional `note`. **Skipping is always allowed, with no forced justification** — the same graceful-degradation instinct Guard Tour's verification methods already established: track the gap, don't block the work. A correction to an already-acted-on item is a new Checklist Item Check row for the same `item_ref` (the latest is what displays; full history is retained) — the same "correction is a new row, not an edit" discipline Exercise & Drill Planner's Inject Delivery/Evaluation Score already established.
4a. **Checklist Item Check gains `assigned_to_ref` (nullable, FK → Person), `assigned_by`, `assigned_at`** *(retrofit, by Incident Action Checklists)* — this doc's own EOC/System checklists don't use per-item assignment and are unaffected; assignment never gates who may check/skip an item (see that doc's FR #7).
5. Marking a Checklist Run `completed` is an explicit action by whoever's working it, available at any point regardless of how many items are `checked` vs. `skipped` — **this never gates or blocks Activate EOC Response or any other operational action**, the platform's standing rule.

### EOC Call-up Roster and Staff Call-up
6. A tenant-configured **EOC Call-up Roster** carries `entries[]`, each resolving to either a specific `person_ref` or an `ics_position_ref` ("whoever currently holds this Position") — an interim mechanism, explicitly flagged for reconciliation once Module 8 Personnel/scheduling exists and can supply a real on-call roster. A Position-based entry with no current holder surfaces as vacant, not notified, never a silent failure.
7. A **Staff Call-up Template** carries reusable message content with `{placeholder}` substitution (the same notation convention AI/LLM Services' Prompt Templates use, borrowed here as plain string templating — no AI generation involved).
8. **Send Staff Call-up Broadcast** (Command/Action Bus action, EOC-Activation-anchored) delivers the resolved message to every entry across the selected roster(s) via the existing internal Notifications Engine — explicitly the platform's internal-user notification model, not Module 17's future external/occupant broadcast system, consistent with the boundary `_DECISIONS.md` already drew between the two.
9. Each recipient can **Acknowledge** the broadcast — the same zero-friction-tap receipt pattern already established for Dispatch Acknowledgment, Host Arrival Acknowledgment, and Safety Check-in — surfaced on a live roster view showing who's acknowledged and who hasn't, so the EOC Coordinator knows who to follow up with directly.

## Data Model / Fields

**Checklist Template** (tenant-authored, versioned plan; not an Activity)
- template_id, tenant_id, category (activation, system_verification, incident_response — retrofit), name, version, status (active, archived)
- trigger_on_activation (bool)
- items[] (item_id, sequence_number, instruction_text, requires_note)
- full_procedure_ref (nullable, forward-reference — retrofit, see [incident-action-checklists.md](../6-emergency-planning/incident-action-checklists.md))
- is_platform_starter_content (bool — retrofit, see [incident-action-checklists.md](../6-emergency-planning/incident-action-checklists.md))

**Checklist Run** (Activity extension; entity_id is the shared PK, FK → Activity)
- template_ref (pinned version), category
- anchor_type (eoc_activation, call, incident), anchor_ref (nullable — retrofit, widened from `eoc_activation_ref`, see [incident-action-checklists.md](../6-emergency-planning/incident-action-checklists.md))
- status (in_progress, completed), started_at, completed_at (nullable)

**Checklist Item Check** (child of Checklist Run)
- check_id, run_ref, item_ref (from template)
- status (checked, skipped), checked_by, checked_at, note (nullable)
- assigned_to_ref (nullable, FK → Person), assigned_by, assigned_at (retrofit, see [incident-action-checklists.md](../6-emergency-planning/incident-action-checklists.md))

**EOC Call-up Roster** (tenant-configured list)
- roster_id, tenant_id, name
- entries[] (entry_id, person_ref [nullable], ics_position_ref [nullable] — exactly one set per entry)

**Staff Call-up Template** (tenant-configured message content)
- template_id, tenant_id, name, message_body (`{placeholder}` substitution)

**Staff Call-up Broadcast** (local record, one per send)
- broadcast_id, eoc_activation_ref, roster_refs[], template_ref, sent_by, sent_at
- recipient_acknowledgments[] (person_ref, acknowledged_at [nullable])

## States & Transitions

- **Checklist Template:** `active` → `archived`, standard versioned-Definition lifecycle.
- **Checklist Run:** `in_progress` → `completed` (explicit action, not gated on item completeness).
- **Checklist Item Check:** created once per interaction; a correction is a new row for the same item, not an edit.
- **Staff Call-up Broadcast:** sent once, immutable; recipient acknowledgments append as they arrive.

## Integrations

- **ICS Role Mapping & Visual Org Chart**: source of the EOC Activation trigger for auto-created Checklist Runs; `ics_position_ref` roster entries resolve against current ICS Role Assignment holders.
- **Domain Events**: owns the "EOC Activation created → auto-create matching Checklist Run(s)" rule, standard trigger/effect split.
- **Notifications Engine**: delivery channel for Staff Call-up Broadcast — internal-only, unmodified, explicitly not Module 17's future external broadcast system.
- **Command/Action Bus**: Start/Complete Checklist Run, Check/Skip Item, Send Staff Call-up Broadcast, and Acknowledge Call-up all register as actions.
- **Structured Logging & Audit Trails**: every checklist item action and broadcast send/acknowledgment is audit-tier.
- **Module 8 (Personnel, not yet specified)**: forward reference only — EOC Call-up Roster is an interim stand-in for real on-call/scheduling data, the same deferred-integration posture used throughout the platform's history (DAR's Shift Window, EOC Logistics Hub's earlier stand-ins).
- **Pre-Incident Plans** *(retrofit)*: introduces a structurally similar but deliberately separate Preplan Notification List (ordered, per-plan escalation contacts vs. this doc's simultaneous, EOC-wide broadcast roster) — kept as two mechanisms given the different delivery semantics, but either can **Import Entries** from the other (a one-time copy of `person_ref`/`ics_position_ref` rows, not a live shared list) to avoid re-entering an overlapping contact set.
- **Incident Action Checklists** *(retrofit)*: widens this doc's Checklist Template/Run/Item Check mechanism with a third category (`incident_response`), a Call/Incident-capable anchor (alongside this doc's own EOC-Activation anchor), per-item field-personnel assignment, and a starter catalog — all additive; this doc's own Activation/System Checklist behavior is unchanged.

## Permissions

| Action | Site/Tenant Admin | EOC Coordinator / Incident Commander | Any EOC role holder | Call-up recipient |
|---|---|---|---|---|
| Author Checklist Template / EOC Call-up Roster / Staff Call-up Template | ✅ | ❌ | ❌ | ❌ |
| Start / complete a Checklist Run | ✅ | ✅ | ✅ | ❌ |
| Check / skip an individual item | ✅ | ✅ | ✅ | ❌ |
| Send a Staff Call-up Broadcast | ✅ | ✅ | ❌ | ❌ |
| Acknowledge a Staff Call-up Broadcast | — | — | — | ✅ |
| View a Checklist Run / broadcast acknowledgment status | ✅ | ✅ | ✅ | inherits site RBAC/ABAC |

## Non-Functional / Constraints

- Checklist Run completion, and any individual item's checked/skipped status, never blocks Activate EOC Response, deactivation, or any other operational action.
- Skipping a checklist item never requires justification — a note is always optional, never enforced.
- Staff Call-up acknowledgment is a single tap, no typed input required, consistent with every other zero-friction-tap receipt on the platform.

## Acceptance Criteria

- [ ] Activating EOC Response auto-creates a Checklist Run for every active, `trigger_on_activation` Checklist Template, pre-populated and ready to work.
- [ ] A System Checklist Template with `trigger_on_activation = false` can still be started manually, standalone, with no EOC Activation in effect.
- [ ] Skipping a checklist item succeeds with no note provided and does not block completing the Checklist Run.
- [ ] Marking a Checklist Run `completed` with unaddressed items succeeds — completion is never gated on 100% item coverage.
- [ ] An EOC Call-up Roster entry referencing a currently-vacant ICS Position surfaces as vacant in the broadcast recipient list, not silently dropped.
- [ ] Sending a Staff Call-up Broadcast delivers to every resolved recipient across the selected roster(s) via the existing internal Notifications Engine.
- [ ] A recipient's Acknowledge action is a single tap and is reflected on the live roster view without requiring a page reload beyond the platform's standard console latency target.
- [ ] Correcting an already-checked item creates a new Checklist Item Check row rather than overwriting the prior one — both remain queryable.

## Open Questions

- Exact starter content for Activation and System Checklist Templates (specific escalation steps, specific backup-power/radio/emergency-system items) — a content/config design task, not committed here.
- Whether System Checklist Runs should support a recurring/periodic schedule independent of any EOC Activation (e.g., a monthly backup-power test via Background Job Processing) — plausible given the Run's anchor is already nullable/standalone-capable, not committed here.
- Whether Checklist Run completion should feed Command Center Wallboard View's Health Signal Registration (e.g., "System Checklist last verified 3 days ago") — a plausible light retrofit, not committed here.
- Exact reconciliation once Module 8 Personnel exists and can supply a real on-call roster — forward reference only.
