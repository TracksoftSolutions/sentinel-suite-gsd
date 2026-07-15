# Call Intake & Logging

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

**Call** registers as its own Activity extension (`activity_type = call`, already anticipated in Activity Registry), the universal CAD front door for anything needing a response — a phone call, a radio report, a walk-up, a self-service portal submission, or (later) an automated sensor/alarm trip or an external 911/PSAP feed. A Call is deliberately **not** an Incident: many calls resolve without ever becoming one (a false alarm, an info-only question, something handled on the phone) — a Call can spawn an Incident via the platform's existing launch-point mechanism (`escalated_from_ref`, already built), but a Call standing alone is a complete, valid, closeable record in its own right.

This doc scopes strictly to **intake and queue state** — receiving, triaging, prioritizing, and queuing a call, plus closing it with or without dispatch. Actual unit assignment, proximity routing, and unit status tracking are fully owned by the next feature, **Unit Dispatch & Proximity Routing**: a Call reaching `dispatched` status hands off to a Dispatch Activity extension that feature will define (referencing this Call via its own `source_call_ref`, the same downstream-points-to-upstream convention used everywhere else — `escalated_from_ref`, `origin_incident_ref`, `source_patrol_request_ref`). No Dispatch fields are speculated here.

**Call supersedes Patrol Management's ad hoc Patrol Request as the CAD front door.** Patrol Request was explicitly built as a stand-in "shaped to become a natural dispatchable target once Dispatch/CAD exists" — that moment is now. Patrol Request keeps its own simpler `requested → assigned → en_route → completed` lifecycle for direct Supervisor-to-unit tasking that doesn't need full call intake (e.g., a Supervisor personally sending a unit to check something out), but gains an optional `source_call_ref` (retrofit, see Integrations) so a Call can also result in a Patrol Request rather than only a future Dispatch record — one of potentially several fulfillment paths a triaged Call can take.

**Caller identity follows Courtesy Patrol's Requestor pattern**, not Incident's full-Party-association pattern: most calls (especially phone/radio) come from anonymous or one-off callers, so free-text caller name/callback number is the default, with an optional link to a real Party when the caller is already known (an occupant, employee, or someone identified during the call). Forcing a full Person record for every anonymous caller was rejected as the wrong tradeoff, same reasoning as Courtesy Patrol's requestor.

**Duplicate calls are handled differently from every other Activity type's dedup, and for a new reason.** Prior carve-outs (Checkpoint Scan) opted out of full tombstone/merge dedup because the record has no real "duplicate identity" risk. Call's carve-out is different: multiple calls about the same event are *not* noise to be merged away — the fact that five people called about the same fire is itself operationally and statistically meaningful (call volume, response-time-to-first-call, public-reporting-behavior analytics). Tombstoning four of them away would destroy that data. So a Call **never** goes through Entity Registry Core's heavy tombstone/merge flow at all; instead a dispatcher working the live queue can, in real time, **link** an incoming call to an already-open call via a lightweight `related_call_ref` — every linked call keeps its own full record, full caller, full timestamp; linking only groups them for queue/response purposes (one dispatch response, not five) without discarding any of them.

**Call Type and Call Priority are their own tenant-configurable Definitions**, independent of Incident's Severity Definition — a call's priority is about response urgency in the moment; an eventual incident's severity is about seriousness/liability after the fact. They're related in practice but deliberately not the same taxonomy or the same field.

## Actors & Roles

- **Dispatcher / Console Operator** — receives and logs calls (phone/radio/walk-up), triages, prioritizes, queues, links duplicates, closes calls with or without dispatch.
- **Guard/Field Responder** — may self-log a call encountered in the field (radio to dispatch, or direct entry); receives dispatch handoff from a queued call (mechanics owned by the next doc).
- **Occupant/Tenant/Site User** — submits a self-service call-in via portal/app, selecting from a tenant-configured call type list.
- **Supervisor** — reviews the queue, reprioritizes, reassigns, closes calls, resolves ambiguous duplicate-link decisions.
- **Tenant Admin** — configures Call Type and Call Priority Definitions, intake-channel settings.
- **Records Admin** — resolves Entity Registry Core dedup flags where they still apply (see Functional Requirements — Call opts out of the heavy merge flow, but the caller's own Party record, if linked, still participates in ordinary Person dedup).

## User Stories

- As a **Dispatcher**, I want to log an incoming phone call in seconds — caller name/callback if given, what they're reporting, roughly where — without being forced to identify the caller or pin an exact location before I can save it.
- As a **Dispatcher**, I want a call automatically prioritized from its call type's default, but I want to be able to override that priority myself if the caller's actual description sounds worse or better than the type implies.
- As a **Dispatcher**, I want to see three separate calls about smoke in the same building and link them together instantly, so I only send one unit, while still keeping each caller's own report on file.
- As an **Occupant**, I want to submit a non-emergency maintenance-adjacent security concern through the app and pick from a short list of what kind of issue it is, without having to call anyone.
- As a **Supervisor**, I want a call that was never dispatched (handled on the phone, confirmed false alarm) to close cleanly without an artificial dispatch step.
- As a **Supervisor**, I want a call that turns out to need a full incident write-up to escalate into an Incident with the call's details already carried over, the same way a Patrol Finding escalates today.
- As a **Tenant Admin**, I want to define our own call types and priority levels, since what counts as "urgent" differs by site and industry.

## Functional Requirements

### Call (Activity extension)
1. **Call** registers as an Activity extension, fulfilling Activity Registry's already-anticipated `call` activity_type: inherits base identity, offline-safe numbering, standard participant/attachment/location associations, and display-label requirements.
2. `call_type` references a tenant-configurable Call Type Definition (e.g., Alarm, Welfare Check, Suspicious Activity, Noise Complaint, Info Request, Maintenance Concern — extensible per tenant).
2a. **Call Type Definition gains an optional `parent_call_type_ref`** (self-referential, nullable) *(retrofit, by Pre-Incident Plans)* — lets a tenant express a broader type generalizing a narrower one (e.g., "Fire" is the parent of "Grease Fire"), the same self-hierarchy shape already used elsewhere (`HierarchyAssociation`), applied here as a plain field since Call Type Definition is a lightweight Settings & Preferences registration, not a full Entity Registry Core citizen. Consumed by Pre-Incident Plans' most-specific-match resolution; has no effect on Call intake/triage/routing otherwise.
3. `priority` references a tenant-configurable Call Priority Definition (e.g., P1-Emergency, P2-Urgent, P3-Routine); each Call Type Definition may declare a default priority, auto-applied at intake but always dispatcher-overridable.
4. `intake_method` records how the call arrived: `phone`, `radio`, `walk_up`, `self_service`, `automated_sensor` (forward reference only — actual auto-generation from alarms/IoT is deferred to Module 19's future Automated Dispatch Generation), `external_ingest` (forward reference only — 911/PSAP or CAD-to-CAD ingest is deferred to Module 19's Integration Gateway, not built here).
5. **Caller**: `caller_name`, `caller_contact` (free text, capturing an unregistered/anonymous caller), with an optional `caller_party_ref` linking to a known/registered Party — never mandatory, same pattern as Courtesy Patrol's Requestor.
6. `narrative` (free text — what was reported).
7. **Location** is tagged via `ActivityLocationAssociation`, same as any other Activity, but is **nullable** — some calls (a general info request, an admin question) genuinely have none.
8. Self-service submissions (via portal/app) present a tenant-configured subset of Call Type Definitions appropriate for self-service use (not every internal dispatcher-only type needs to be occupant-facing); the submitting user is auto-populated as `caller_party_ref` when authenticated.

### Triage, queue & duplicate linking
9. **Call lifecycle**: `received` → `triaged` (type/priority confirmed) → `queued` → one of `dispatched` (handoff to the next doc's Dispatch mechanism), `resolved_no_dispatch` (handled without sending a unit), or `duplicate` (linked to another call, see below) → `closed`.
10. A Dispatcher can **link** an incoming or queued Call to an already-open Call via `related_call_ref`, marking the linked call `duplicate` and grouping it under the primary call for queue/response purposes. This is a real-time, dispatcher-driven action — **not** Entity Registry Core's tombstone/merge mechanism; every linked Call retains its own full record (caller, narrative, timestamps) rather than being redirected or hidden. A `duplicate`-status Call still closes when its primary call closes, and remains independently queryable for reporting (e.g., "how many separate callers reported this event").
11. A Call closed as `resolved_no_dispatch` requires a `resolution_note` (free text — how it was handled without dispatch, e.g., "confirmed false alarm via camera," "caller withdrew report," "resolved by phone").
12. Reaching a configured priority level (e.g., a Call logged at `P1-Emergency`) publishes an automation-eligible domain event, letting a Tenant Admin configure notification/escalation behavior via Domain Events — the same trigger/effect split used platform-wide, not a hardcoded alert path.

### Escalation & fulfillment paths
13. `escalated_from_ref`-style linkage runs in reverse here: a Call is itself eligible as a launch point for creating an Incident via the platform's existing mechanism (an Incident created from a Call sets its own `escalated_from_ref` to the Call, with location/narrative context pre-filled) — no changes needed to Incident's doc.
14. A Call may alternatively result in a **Patrol Request** (Patrol Management) for direct-unit tasking that doesn't need full dispatch queue mechanics — the resulting Patrol Request sets `source_call_ref` back to the originating Call (retrofit, see Integrations).
15. A Call transitioning to `dispatched` hands off to a Dispatch Activity extension owned entirely by **Unit Dispatch & Proximity Routing** (next doc); this doc only guarantees the Call reaches that status and is discoverable by `call_id` — no Dispatch-side fields are defined here.

## Data Model / Fields

**Call** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- call_type (ref → Call Type Definition), priority (ref → Call Priority Definition)
- intake_method (phone, radio, walk_up, self_service, automated_sensor, external_ingest)
- caller_name, caller_contact (free text), caller_party_ref (nullable, FK → Party)
- narrative
- related_call_ref (nullable, direct field — set when linked as a duplicate; points to the primary Call)
- resolution_note (nullable, required when status = resolved_no_dispatch)
- received_at, triaged_at (nullable), queued_at (nullable), closed_at (nullable)
- *(location is a row, not a field — standard `ActivityLocationAssociation`, nullable)*

**Call Type Definition** (Settings & Preferences registration)
- call_type_id, tenant_id, name, enabled
- default_priority (ref → Call Priority Definition, nullable)
- self_service_eligible (bool — whether this type is offered on the occupant-facing portal/app)
- default_requires_safety_checkin (bool, default false — retrofit, see [status-state-monitors.md](status-state-monitors.md); Dispatcher-overridable per specific Call/Dispatch)
- default_silent_delivery, default_radio_bypass (bool, default false — retrofit, see [silent-mobile-dispatching.md](silent-mobile-dispatching.md); Dispatcher-overridable per specific Dispatch)
- parent_call_type_ref (nullable, self-FK → Call Type Definition — retrofit, see [pre-incident-plans-preplans.md](../6-emergency-planning/pre-incident-plans-preplans.md))

**Call Priority Definition** (Settings & Preferences registration)
- priority_id, tenant_id, name, enabled, sort_order (for threshold comparisons, e.g., "P1 or higher")

## States & Transitions

**Call:** `received` → `triaged` → `queued` → `dispatched` | `resolved_no_dispatch` | `duplicate` → `closed`. A `duplicate`-status Call closes automatically when its primary (`related_call_ref`) Call closes. `dispatched` handoff mechanics (and any further status a live dispatch introduces) are owned by Unit Dispatch & Proximity Routing.

## Integrations

- **Activity Registry**: Call registers as an Activity extension, fulfilling the `call` activity_type already anticipated there. Deliberately opts out of Entity Registry Core's standard tombstone/merge dedup (see Functional Requirements #10) — a new, distinct rationale from Checkpoint Scan's opt-out (reporting-value preservation, not absence-of-identity-risk), worth carrying forward as a third example of the "don't inherit the heavy mechanism by default" lesson.
- **Entity Registry Core**: identity, display-label, and — where `caller_party_ref` is set — the linked Party still participates in ordinary Person dedup/merge; the Call record itself never does.
- **Party Registry**: source of `caller_party_ref` when a caller is known/identified.
- **Location Registry**: source of the optional `ActivityLocationAssociation` target.
- **Incident Reporting & Management**: a Call is an established launch point for creating an Incident (`escalated_from_ref` set on the Incident, per Incident's existing mechanism) — no changes needed there.
- **Patrol Management**: Patrol Request gains an optional `source_call_ref` field (retrofit — see cross-doc note below) so a Call can result in direct-unit tasking without full dispatch mechanics.
- **Settings & Preferences**: owns Call Type Definitions (including self-service eligibility and default priority) and Call Priority Definitions.
- **Domain Events / Notifications Engine**: priority-threshold reached (e.g., a call logged at P1) publishes an automation-eligible event; a Tenant Admin-configured rule decides actual notification/escalation behavior.
- **Daily Activity Reports (DAR) / Shift Passdowns**: Calls are ordinary Activities, automatically picked up by any DAR filter or Pass-On Rule matching guard/site/time window, with zero Call-side special-casing, same integration DAR's own doc already anticipated for future Activity types.
- **Structured Logging & Audit Trails**: Call creation, triage/priority changes, duplicate linking, and closure are all audit-tier events.
- **Unit Dispatch & Proximity Routing (next doc)**: owns everything from `dispatched` onward — unit assignment, routing, the Dispatch Activity extension itself (which will set its own `source_call_ref` back to this doc's Call).
- **Status & State Monitors**: reads `default_requires_safety_checkin` off Call Type Definition to decide whether a Dispatch fulfilling this Call automatically begins a recurring officer check-in cycle.
- **Silent Mobile Dispatching**: reads `default_silent_delivery`/`default_radio_bypass` off Call Type Definition as part of its delivery-style/radio-posture resolution chain for a Dispatch fulfilling this Call.
- **Physical Security Integration Gateway (Module 19, future)**: intended eventual source of `intake_method = automated_sensor` (Automated Dispatch Generation) and `intake_method = external_ingest` (911/PSAP, CAD-to-CAD) — both explicitly deferred, flagged here only as forward references, not built now.

**Cross-doc retrofit note:** [patrol-management.md](../1-security-operations/patrol-management.md)'s **Patrol Request** data model gains an optional `source_call_ref` field (FK → this doc's Call) so a Patrol Request can originate from call intake. This is additive — Patrol Request's existing fields and lifecycle are unchanged, and a self-initiated or directly-Supervisor-tasked Patrol Request with no `source_call_ref` remains fully valid.

## Permissions

| Action | Guard/Field Responder | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|---|
| Log a call (phone/radio/walk-up) | ✅ | ✅ | ✅ | ❌ |
| Submit a self-service call-in | ✅ (as occupant/user) | ✅ | ✅ | ❌ |
| Triage/prioritize/queue a call | ❌ | ✅ | ✅ | ❌ |
| Link a call as a duplicate | ❌ | ✅ | ✅ | ❌ |
| Close a call (with or without dispatch) | ❌ | ✅ | ✅ | ❌ |
| Escalate a call to an Incident / Patrol Request | ✅ (own logged call) | ✅ | ✅ | ❌ |
| Configure Call Type / Priority Definitions | ❌ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Call logging must work fully offline (a field-logged radio call in a dead zone), per the platform's established offline model, syncing and reconciling like any other Activity.
- Duplicate linking is a fast, real-time dispatcher action — must not require waiting on an async dedup-matching pass; standard Entity Registry Core matching still runs for the linked `caller_party_ref`, if present, on its own normal cadence.
- Priority-threshold domain event evaluation must fire promptly (near-real-time), consistent with Incident's own severity-escalation latency expectations, given both feed the same urgency-driven response posture.
- Self-service call-type lists must be filterable/orderable independently from the full internal dispatcher-facing list, so occupant-facing UI doesn't leak internal-only categories.
- WCAG 2.1 / Section 508 accessible call intake (both dispatcher console and self-service submission), triage, and closure flows, day one.

## Acceptance Criteria

- [ ] Logging a phone call with only a caller name and narrative (no location, no registered caller) succeeds and produces a valid, closeable Call.
- [ ] A Call Type Definition's `default_priority` auto-populates a new Call's priority, and a Dispatcher can override it before or after triage.
- [ ] Linking a second call to an already-open call sets `related_call_ref`, marks the second call `duplicate`, and both calls remain independently visible with their own full caller/narrative data.
- [ ] Closing a primary call automatically closes every Call linked to it as a duplicate.
- [ ] A Call closed as `resolved_no_dispatch` requires and stores a `resolution_note`.
- [ ] Creating an Incident from a Call correctly sets the Incident's `escalated_from_ref` to the Call and pre-fills location/narrative context.
- [ ] Creating a Patrol Request from a Call correctly sets the Patrol Request's `source_call_ref`.
- [ ] A Call logged at a configured escalation priority publishes a domain event a configured Domain Events rule can act on.
- [ ] An occupant submitting a self-service call-in only sees Call Type Definitions marked `self_service_eligible`.
- [ ] A Call record, once created, never enters Entity Registry Core's tombstone/merge dedup flow, regardless of how many duplicate calls are linked to it.
- [ ] A Call created offline (field radio report in a dead zone) receives a usable client UUID immediately and syncs/reconciles correctly once connectivity returns.

## Open Questions

- Exact default Call Type and Call Priority taxonomies shipped out of the box — pending UX/content design.
- Whether `related_call_ref` needs to support a chain (call C links to B which links to A) vs. always pointing directly to one true primary — current default assumes direct-to-primary; revisit if multi-hop linking proves necessary during technical spec.
- Exact self-service portal/app UX (how an occupant picks a type, whether they see queue/response status back) — deferred to UX design and to Mass Notification & Crisis Communications' broader occupant-facing posture, not solved here.
- Whether a Call needs its own SLA/response-time-target field now or whether that's fully owned by Module 12's future Response Time Analytics reading off Call/Dispatch timestamps — current default is the latter (no SLA field here), flagged for confirmation once that module is specified.
- Full mechanics of `automated_sensor`/`external_ingest` intake — entirely deferred to Module 19 Physical Security Integration Gateway, not solved here.
