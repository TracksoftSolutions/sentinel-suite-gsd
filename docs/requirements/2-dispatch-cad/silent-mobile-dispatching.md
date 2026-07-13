# Silent Mobile Dispatching

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

Delivers a Dispatch assignment's full context (call type, location, priority, notes, caller info) straight to the assigned officer's device, so a Dispatcher doesn't have to narrate it over voice radio — and does so along **two independent axes**, not one bundled "silent mode":

1. **Delivery style** — whether the notification alerts audibly/vibrates normally, or delivers silently (vibrate-minimal or fully silent) for situations where a phone chime could be dangerous (an undercover officer, a suspect nearby, a domestic-violence call).
2. **Radio posture** — whether the Dispatcher is expected to also call the assignment out over voice radio, or relies on the app alone to cut radio-channel congestion.

A given Dispatch can be silent on one axis and normal on the other (e.g., a busy-night radio-bypass dispatch with a completely normal audible phone alert, or a genuinely sensitive call that's silent on delivery but still radio-confirmed for redundancy). Neither axis invents new delivery infrastructure — both ride on Notifications Engine's existing push/channel/category mechanism and Offline Data Sync's existing offline-queuing behavior; this doc's own scope is the resolution rule for which mode applies, the dispatch-specific **Acknowledgment** receipt, and unacknowledged-dispatch escalation.

**Resolution follows the platform's established explicit-beats-default precedence**: a Dispatcher's explicit per-Dispatch flag beats a Call Type's configured default, which beats (for delivery style only) the officer's own personal Notification Policy preference (an existing Notifications Engine mechanism, not a new one). Radio posture has no personal-preference fallback — it's an operational call, not a personal one — so it falls through to a platform default (normal radio callout expected) when neither a Dispatcher flag nor a Call Type default sets it.

**Acknowledgment is its own lightweight receipt, separate from Dispatch phase logging.** Reaching `en_route` already proves an officer responded, but a Dispatcher watching a silently-delivered dispatch needs to know sooner whether it was even seen — before the officer is necessarily ready to commit to a phase. A single zero-friction tap (the same low-friction-input pattern as Canned Quick Phrase and Safety Check-in) marks it acknowledged; an unacknowledged silent dispatch past a configurable grace period escalates via Domain Events, the same trigger/effect split used everywhere else — this doc never hardcodes a fallback action (re-alert audibly, prompt the Dispatcher to fall back to radio, etc.), a Tenant Admin configures it.

## Actors & Roles

- **Guard/Officer** — receives the dispatch notification on their device, taps to acknowledge, proceeds to log their own Dispatch phases as normal.
- **Dispatcher / Console Operator** — sets delivery style/radio posture per Dispatch when a situational need overrides the defaults, monitors acknowledgment status.
- **Supervisor** — receives unacknowledged-dispatch escalations.
- **Tenant Admin** — configures Call Type defaults for both axes, the acknowledgment grace period, and Domain Events rules for unacknowledged escalation.

## User Stories

- As a **Dispatcher**, I want to send a domestic-violence call to an officer's phone with a silent, vibrate-only alert, so approaching the scene doesn't tip anyone off.
- As a **Dispatcher** on a busy night, I want to send routine dispatches to officers' devices without calling every one out over the radio, freeing the channel for anything urgent.
- As an **Officer**, I want a single tap to tell the Dispatcher "I saw this" the moment I get a silent dispatch, without it counting as me saying I'm already en route.
- As a **Supervisor**, I want to know if a silently-dispatched officer hasn't even acknowledged the call after several minutes, since a normal radio dispatch would have gotten an immediate verbal response.
- As a **Tenant Admin**, I want certain call types to default to silent delivery automatically, so a Dispatcher doesn't have to remember to flag every sensitive call type by hand.

## Functional Requirements

### Delivery style & radio posture
1. Each Dispatch carries two independent, resolvable settings: **delivery style** (`audible` or `silent`) and **radio posture** (`radio_expected` or `radio_bypass`).
2. Resolution precedence for **delivery style**: an explicit per-Dispatch Dispatcher flag, then the originating Call Type's configured default, then the assigned officer's own personal Notification Policy preference (existing Notifications Engine mechanism), then platform default (`audible`).
3. Resolution precedence for **radio posture**: an explicit per-Dispatch Dispatcher flag, then the originating Call Type's configured default, then platform default (`radio_expected`) — no personal-preference fallback, since this is an operational dispatching decision, not a personal device setting.
4. **Call Type Definition** (Call Intake & Logging) gains two optional fields (retrofit, see Integrations): `default_silent_delivery` and `default_radio_bypass`.
5. A Dispatch's radio posture is surfaced clearly on the Dispatcher's own console (e.g., a "Silent — no radio callout" indicator) — the platform has no control over physical radio hardware (out of scope, adjacent to Module 19's future integration territory); this is an expectation-setting flag for the Dispatcher, not a technical suppression of anything.

### Notification delivery
6. The dispatch notification is delivered through Notifications Engine's existing push/channel infrastructure under a dedicated **Dispatch Assignment** category, rendering the assignment's full context (call type, location, priority, notes, caller info where available) so the officer has everything needed without a verbal briefing.
7. Delivery in poor/no connectivity follows Offline Data Sync's existing queuing behavior unmodified — no new offline mechanism is introduced here.

### Acknowledgment
8. An officer acknowledges a delivered dispatch with a single, zero-friction tap — the same low-friction-input discipline as Canned Quick Phrase and Safety Check-in — setting `acknowledged_at` on the Dispatch record. This is distinct from and does not require any Dispatch phase progress.
9. A tenant-configurable **grace period** bounds how long an unacknowledged Dispatch is tolerated before it's considered stale.
10. A Dispatch still unacknowledged past its grace period publishes an automation-eligible domain event, letting a Tenant Admin configure the actual escalation behavior (re-alert audibly, prompt the Dispatcher to fall back to a radio call, notify a Supervisor) via Domain Events — this doc never hardcodes the fallback action.
11. Acknowledgment applies once per Dispatch, at first tap; a Dispatch that's already progressed to a later phase (e.g., `en_route` logged without a separate acknowledgment tap) is treated as implicitly acknowledged — an officer who's clearly already responding shouldn't be blocked or flagged as unacknowledged.

## Data Model / Fields

**Dispatch** (retrofit — adds fields to the existing table in unit-dispatch-proximity-routing.md)
- silent_delivery (nullable bool — explicit per-Dispatch override; null falls through the resolution chain)
- radio_bypass (nullable bool — explicit per-Dispatch override; null falls through the resolution chain)
- acknowledged_at (nullable — set on first Acknowledge tap, or implicitly on first phase log if no explicit tap occurred first)

**Call Type Definition** (retrofit — adds fields to the existing table in call-intake-logging.md)
- default_silent_delivery (bool, default false)
- default_radio_bypass (bool, default false)

**Dispatch Assignment Notification Category** (Notifications Engine registration)
- category_key (`dispatch_assignment`), placeholder fields (call_type, location, priority, notes, caller_info)

## States & Transitions

**Dispatch acknowledgment:** `unacknowledged` → `acknowledged` (explicit tap, or implicitly on first phase log) — set once, never reverted.

## Integrations

- **Unit Dispatch & Proximity Routing — retrofit**: Dispatch gains `silent_delivery`, `radio_bypass`, and `acknowledged_at`.
- **Call Intake & Logging — retrofit**: Call Type Definition gains `default_silent_delivery` and `default_radio_bypass`.
- **Notifications Engine**: owns the actual push/channel delivery mechanics, the Dispatch Assignment category, and the officer's personal Notification Policy preference this doc's delivery-style resolution falls through to — this doc registers the category and consumes the existing mechanism rather than building new delivery infrastructure.
- **Offline Data Sync**: delivery in poor/no connectivity follows its existing queuing behavior unmodified.
- **Real-Time Delivery & Server-Side Timers (retrofit)**: the unacknowledged-dispatch escalation timer runs in that doc's server-side Timer Service — the safety net fires reliably even if push delivery itself failed, exactly as this doc's constraint requires, without depending on the officer's device.
- **Domain Events**: owns the actual unacknowledged-escalation behavior; this doc only publishes the triggering event.
- **Settings & Preferences**: owns the acknowledgment grace period configuration.
- **Command/Action Bus**: "Set delivery style," "Set radio posture," "Acknowledge dispatch" register as invokable actions across every surface.
- **Active Incident Queue (CAD Console)**: a Dispatch's Feed line (nested under its Call card) can surface an unacknowledged/stale visual indicator, a computed display attribute, no schema change needed to that doc.
- **Status & State Monitors**: acknowledgment is deliberately independent of Unit State — an unacknowledged dispatch doesn't block or alter the officer's Unit State transitions, which remain driven purely by Dispatch phase.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|---|
| Acknowledge own dispatch | ✅ | ❌ | ❌ | ❌ |
| Set delivery style/radio posture on a specific Dispatch | ❌ | ✅ | ✅ | ❌ |
| Configure Call Type defaults for both axes | ❌ | ❌ | ❌ | ✅ |
| Configure acknowledgment grace period / escalation rules | ❌ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Notification delivery must be prompt (near-real-time) even under the `silent` delivery style — silence changes alert behavior, not delivery latency.
- A missed/unacknowledged silent dispatch must never fail silently at the platform level — the grace-period escalation is the safety net, and must fire reliably even if the underlying push delivery itself failed.
- WCAG 2.1 / Section 508 accessible dispatch-notification and acknowledgment flows, day one — including for delivery styles that intentionally suppress audible/haptic alerting, which must not become an accessibility gap for officers who rely on those cues.

## Acceptance Criteria

- [ ] A Dispatch with no explicit flags and a Call Type default of `default_silent_delivery = true` delivers silently; one with no Call Type default falls through to the officer's personal Notification Policy preference.
- [ ] A Dispatcher's explicit per-Dispatch delivery-style flag overrides both the Call Type default and the officer's personal preference.
- [ ] A Dispatch flagged `radio_bypass = true` shows a clear "no radio callout expected" indicator on the Dispatcher console.
- [ ] An officer tapping Acknowledge sets `acknowledged_at` without affecting Dispatch phase or Unit State.
- [ ] An officer who logs `en_route` without a prior explicit Acknowledge tap is treated as implicitly acknowledged.
- [ ] A Dispatch left unacknowledged past the configured grace period publishes a domain event a configured Domain Events rule can act on.
- [ ] Dispatch notification delivery in a connectivity gap queues and delivers correctly once connectivity returns, using Offline Data Sync's existing mechanism with no new offline logic.

## Open Questions

- Exact default grace period and default Call Type flags for delivery style/radio posture — pending UX/content design and real customer input.
- Whether radio posture ever needs a richer state than a binary flag (e.g., "radio callout delayed until officer arrives") — not addressed here, current default is binary.
- Whether an escalation action should be able to automatically flip a stale silent dispatch back to `audible` delivery as a fallback, or whether that's purely a Tenant Admin-configured Domain Events effect — leaning toward the latter (no hardcoded fallback), consistent with the trigger/effect split used throughout.
