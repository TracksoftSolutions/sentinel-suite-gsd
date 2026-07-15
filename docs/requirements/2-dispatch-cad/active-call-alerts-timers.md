# Active Call Alerts & Timers

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

Two more timed alerts, both structurally identical to Status & State Monitors' Time-on-Scene Watchdog — a tenant-configurable duration threshold on a phase/status, Domain-Event-driven when exceeded — just watching different moments: **Pending Call Alarm** watches how long a Call sits `queued` without being dispatched; **Enroute Timer** watches how long a Dispatch sits `en_route` without reaching `on_scene`. Rather than building a second parallel timer system, both register as two more instances of that doc's now-**generalized Duration Watchdog mechanism** (retrofit — see Integrations): `(activity_type, watched_field, watched_value)` → duration, instead of the Dispatch-on-scene-only shape it started as. One mechanism, three watched phases today, easy to register a fourth later.

**Alarm delivery is tenant-configurable, not fixed.** Each Duration Watchdog instance (including Time-on-Scene, retroactively) now declares an `alarm_mode`: `one_shot` (a normal distinctive Notifications Engine alert) or `persistent` (keeps re-notifying at a configured resend interval until the underlying condition resolves on its own — the Call gets dispatched, the Dispatch reaches on scene — or a Dispatcher explicitly acknowledges it). This is the actual "sound alerts in the dispatch room" behavior MODULES.md describes, generalized so any watched phase can be configured that way, not hardcoded to Pending Call Alarm alone.

**Supervisor Notifications is one unified Critical Event Escalation Policy, not four disconnected rules.** Every critical-event-publishing trigger this module has built (Duration Watchdog exceeded — any instance — and missed Safety Check-in) can feed one tenant-configured policy: pick which event types matter, how much *additional* time past the original trigger is tolerated before it's genuinely a manager's problem, and who gets notified. A tenant can define more than one policy (e.g., a shorter fuse and a different escalation target for Critical-priority calls). Mechanically this still publishes an ordinary automation-eligible domain event Notifications Engine delivers — the policy is a coherent configuration surface over Domain Events, not new delivery infrastructure.

## Actors & Roles

- **Dispatcher / Console Operator** — sees and acknowledges alarms, works the underlying Call/Dispatch to resolve them.
- **Supervisor** — receives Critical Event Escalation Policy notifications for anything still unresolved past the additional delay.
- **Tenant Admin** — configures Duration Watchdog instances (including alarm_mode), and Critical Event Escalation Policies.

## User Stories

- As a **Dispatcher**, I want a critical call that's sat in the queue too long to trigger a real, hard-to-miss alarm in the dispatch room, not just a quiet notification I might not see.
- As a **Dispatcher**, I want an alarm to stop on its own once I actually dispatch the call, without me having to remember to silence it separately.
- As a **Dispatcher**, I want to see a warning if a unit has been en route far longer than expected for that distance/call type, in case they got into trouble or took a wrong turn.
- As a **Supervisor**, I want one place to configure "if any of these critical situations goes unresolved for this much longer, notify me," instead of hunting down and keeping four separate automation rules in sync.
- As a **Tenant Admin**, I want Critical-priority calls to escalate to a Supervisor faster than routine ones, without having to hand-author separate rules for every priority level.

## Functional Requirements

### Pending Call Alarm & Enroute Timer (Duration Watchdog instances)
1. **Pending Call Alarm** registers as a Duration Watchdog instance: `activity_type = call`, `watched_field = status`, `watched_value = queued` — a tenant-configurable duration, optionally narrowed by Call Type/Priority (e.g., Critical-priority calls alarm at 2 minutes, routine calls at 10).
2. **Enroute Timer** registers as a Duration Watchdog instance: `activity_type = dispatch`, `watched_field = phase`, `watched_value = en_route`.
3. Both reuse Status & State Monitors' generalized Duration Watchdog mechanism unmodified — this doc builds no separate threshold/domain-event infrastructure, only two more registered instances.

### Alarm delivery mode
4. Each Duration Watchdog instance's `alarm_mode` is tenant-configurable per instance (and, within an instance, further narrowable by Call Type/Priority same as duration itself): `one_shot` fires a single distinctive Notifications Engine alert; `persistent` keeps re-notifying at a configured `resend_interval`.
5. A `persistent` alarm auto-silences the moment its underlying watched condition resolves (the Call leaves `queued`, the Dispatch leaves `en_route`) — no lingering alert about something already handled. A Dispatcher can also explicitly silence it earlier via **Acknowledge Alarm** (Command/Action Bus action, audit-logged) while still actively working the resolution.

### Critical Event Escalation Policy
6. A tenant-configurable **Critical Event Escalation Policy** lists which of this module's critical-event-publishing triggers feed it — any registered Duration Watchdog instance's exceeded event, or a missed Safety Check-in — an **additional delay** past the original trigger's own firing, and an **escalation target** (a role, e.g. "Shift Supervisor," or a specific person).
7. If the underlying condition remains unresolved for the policy's configured additional delay after the original trigger fired, the policy publishes its own escalation domain event targeting the configured Supervisor/role; Notifications Engine delivers it like any other notification.
8. A tenant may define more than one Critical Event Escalation Policy (different watched-event sets, delays, or targets, e.g. by call priority) — not limited to one global policy.
9. "Unresolved" is evaluated per source using that source's own already-established status, not redefined here: a Duration-Watchdog-sourced trigger is unresolved as long as the watched entity remains at the watched field/value; a missed-Safety-Check-in-sourced trigger is unresolved until superseded by a later confirmed check-in or the underlying Dispatch clears.

### Approaching-Deadline Reminder (promoted mechanism — retrofit, Mutual Aid Agreements Tracker)
10. **Approaching-Deadline Reminder** is a new sibling mechanism alongside Duration Watchdog — *before* a configured date rather than *after* a threshold — promoted here from Improvement Plan (IP) Tracking's locally-built Deadline Reminder Policy once a second real consumer ([Mutual Aid Agreements Tracker](../5-emergency-management/mutual-aid-agreements-tracker.md)) needed the identical shape, the platform's established "promote on second consumer" discipline. A registration is `(record_type, watched_date_field)` — e.g. `(improvement_action, target_completion_date)`, `(mutual_aid_agreement, effective_end)` — carrying a tenant/category-configurable `lead_time_offsets_days[]`.
11. Each configured offset fires **at most once** per watched record instance, tracked via a `reminders_sent[]` list on that instance — the exact debounce behavior IP Tracking's original mechanism already specified, unchanged by the promotion. Delivered through the existing Notifications Engine; never blocks any operational action.
12. **Deliberately not merged with Duration Watchdog** — a before-a-deadline check and an after-a-threshold check are structurally opposite comparisons; forcing both into one abstraction would have required every existing Duration Watchdog consumer (Pending Call Alarm, Enroute Timer, Key Custody's overdue key, SITREP's overdue check) to absorb a negative-offset concept none of them need. Two sibling mechanisms, run on the same Real-Time Delivery Timer Service infrastructure, kept structurally independent.

## Data Model / Fields

**Duration Watchdog** (Status & State Monitors' generalized mechanism — this doc registers two more instances, no separate table)
- `(call, status, queued)` — Pending Call Alarm
- `(dispatch, phase, en_route)` — Enroute Timer
- *(retrofit — Access Control's Key Custody & Auditing)* the watched-record kind generalizes beyond `activity_type` alone to also cover a registered EntityAssociation kind (its first non-Activity consumer, watching `(custody_association, status, active)` for an overdue key); the threshold itself gains an optional dynamic `resolution_mode` (a resolved target timestamp — e.g., the holder's current DAR Shift Window end — with a flat-duration fallback) alongside the original fixed-duration shape every earlier instance in this doc uses.

**Approaching-Deadline Reminder Registration** (new — retrofit, promoted from IP Tracking)
- registration_id, record_type, watched_date_field, tenant_id, category (nullable)
- lead_time_offsets_days[], cc_supervisor (bool, where applicable to the consumer)
- *(per-instance)* reminders_sent[] — tracked on the watched record itself, e.g. Improvement Action or Mutual Aid Agreement

**Critical Event Escalation Policy** (Settings & Preferences registration)
- policy_id, tenant_id, name
- watched_event_types[] (specific Duration Watchdog instances by threshold_id, and/or `safety_checkin_missed`)
- additional_delay, escalation_target (role or specific person_ref)
- enabled

## States & Transitions

**Duration Watchdog alarm (any instance):** `not_triggered` → `alarming` (`one_shot`: fires once; `persistent`: repeats at `resend_interval`) → `silenced` (auto, on condition resolve; or explicit Acknowledge Alarm).

**Critical Event Escalation Policy trigger:** watches a fired critical event → `escalated` (additional delay elapses with condition still unresolved) | never fires (condition resolves before the additional delay elapses).

## Integrations

- **Status & State Monitors — retrofit**: Watchdog Threshold generalized into the reusable Duration Watchdog mechanism (`activity_type`/`watched_field`/`watched_value` instead of Dispatch-on-scene-only), gaining `alarm_mode`/`resend_interval`; this doc's two instances and the Critical Event Escalation Policy both consume that doc's mechanics unmodified.
- **Call Intake & Logging**: source of Call's `status = queued` (watched by Pending Call Alarm) and its Call Type/Priority narrowing dimensions.
- **Unit Dispatch & Proximity Routing**: source of Dispatch's `phase = en_route` (watched by Enroute Timer).
- **Domain Events / Notifications Engine**: owns actual delivery for both Duration Watchdog alarms and Critical Event Escalation Policy notifications — this doc only publishes triggering events, never a hardcoded alert path.
- **Active Incident Queue (CAD Console)**: a Call or Dispatch card shows an "alarming" visual flag when its Duration Watchdog is active — a computed display attribute, consistent with Time-on-Scene Watchdog's own established integration note, never relying on color alone.
- **Command/Action Bus**: "Acknowledge Alarm" registers as an invokable action across every surface.
- **Settings & Preferences**: owns Critical Event Escalation Policy registrations and (via the retrofitted mechanism) Duration Watchdog instance configuration.
- **Structured Logging & Audit Trails**: every alarm state transition, acknowledgment, and Critical Event Escalation Policy firing is an audit-tier event.

## Permissions

| Action | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|
| Acknowledge an alarm | ✅ | ✅ | ❌ |
| Configure Duration Watchdog instances (durations, alarm_mode) | ❌ | ❌ | ✅ |
| Configure Critical Event Escalation Policies | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Duration Watchdog evaluation and Critical Event Escalation Policy firing must both happen promptly (near-real-time), consistent with the platform's existing latency expectations for safety/escalation-relevant automation.
- A persistent alarm's resend behavior must respect its configured interval exactly — never spamming faster than configured, and never silently stopping before the condition resolves or is acknowledged.
- An audible alarm must always have a non-audible, equally noticeable equivalent (a persistent visual flag, not just a badge) — a Deaf/hard-of-hearing Dispatcher must be able to notice and act on an alarm exactly as fast as one who can hear it, day one, not a later accessibility pass.
- WCAG 2.1 / Section 508 accessible alarm/acknowledgment flows and Critical Event Escalation Policy configuration, day one.

## Acceptance Criteria

- [ ] A Call left `queued` past its configured Pending Call Alarm threshold publishes a Duration Watchdog domain event and, if the instance's `alarm_mode = persistent`, keeps re-alerting at the configured resend interval.
- [ ] A Dispatch left `en_route` past its configured Enroute Timer threshold behaves identically.
- [ ] A `persistent` alarm auto-silences the moment its watched condition resolves, with no explicit acknowledgment required.
- [ ] A Dispatcher can explicitly Acknowledge Alarm to silence a still-open persistent alarm while continuing to work the underlying resolution.
- [ ] A Critical Event Escalation Policy configured to watch Pending Call Alarm and missed Safety Check-in events correctly fires its own escalation only when the underlying condition is still unresolved after the policy's additional delay — not immediately on the original trigger.
- [ ] Two Critical Event Escalation Policies with different watched-event sets, delays, and targets can coexist and fire independently.
- [ ] An alarming Call or Dispatch is visually flagged on its Active Incident Queue card without relying on color alone.
- [ ] A Records/Tenant Admin can confirm every alarm state transition and escalation firing is a discoverable audit-tier event.

## Open Questions

- Exact default Pending Call Alarm / Enroute Timer durations, default `alarm_mode`, and default resend intervals — pending UX/content design and real customer input.
- Whether multiple simultaneous persistent alarms need their own console-level volume/UX management (e.g., a "silence all" affordance) to avoid overwhelming a busy dispatch room — not addressed here, a technical-spec/UX-level concern flagged for follow-up.
- Whether Enroute Timer thresholds should eventually be distance/ETA-aware rather than a flat duration, once Unit Dispatch & Proximity Routing's own deferred driving-distance/ETA routing capability exists — current default stays a flat, tenant-configured duration, consistent with that doc's own honest scoping.
- Full set of event types eligible to feed a Critical Event Escalation Policy beyond day one's two sources (Duration Watchdog instances, missed Safety Check-in) — extensible as future critical-event-publishing mechanisms are built, not exhaustively enumerated here.
