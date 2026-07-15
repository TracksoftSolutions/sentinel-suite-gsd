# Host Arrival Notifications

## Overview

Host Arrival Notifications is a thin composition over already-established mechanisms — it introduces no new delivery infrastructure, no new inbound-message-parsing capability, and no changes to how Visitor Kiosk App's check-in itself works. It fires the moment a Visit is checked in (Pre-Registration Portal/Visitor Kiosk App's territory, consumed here unmodified) and tracks whether/how the host acknowledged it, with a tenant-configurable choice between a pure FYI and a real lobby-holding gate, plus multi-tier escalation if nobody responds.

**Host Arrival Acknowledgment** — the record this doc is built around — reuses the "zero-friction-tap" receipt pattern already established by Safety Check-in and Silent Mobile Dispatching's Dispatch Acknowledgment: a lightweight, feature-local record distinct from Visit's own lifecycle, not an Activity extension in its own right (an acknowledgment receipt, not a trackable occurrence needing full Activity Registry treatment).

## Actors & Roles

- **Host** — receives the arrival notification, confirms via a one-tap link.
- **Backup/Delegate Host** *(optional, per-invitation or standing default)* — a second person notified if the primary host doesn't respond in time.
- **Front Desk / Lobby Guard** — sees live acknowledgment status for every checked-in Visit at their site; can release a visitor manually regardless of confirmation state.
- **Site/Tenant Admin** — configures Host Confirmation Policy and Host Arrival Escalation Policy.

## User Stories

- As a **Host**, I want a text/email/push notification the moment my visitor checks in, with a one-tap link to confirm.
- As a **Site Admin at a high-security site**, I want visitors held in the lobby until their host actively confirms, not just notified as an FYI.
- As a **Site Admin at a low-security site**, I want the notification to be a pure heads-up with no lobby-holding behavior, since Pre-Registration Portal's own approval already cleared the visitor.
- As a **Host**, I want to name a backup who can confirm on my behalf if I'm in a meeting and miss the first notification.
- As a **Lobby Guard**, I want to see at a glance which checked-in visitors are still awaiting host confirmation, and be able to release one myself if I can't wait any longer.
- As a **Lobby Guard**, I want to be automatically alerted if a host (and their backup) both fail to respond within a reasonable window, rather than a visitor waiting indefinitely with nobody aware.

## Functional Requirements

### Acknowledgment creation & delivery
1. A **Host Arrival Acknowledgment** is created automatically the instant a Visit reaches `checked_in` (Visitor Kiosk App or a front-desk manual check-in) — no separate trigger action, and Visit's own check-in mechanics (badge printing included) are entirely unmodified by this doc.
2. The host is notified through Notifications Engine's existing category/channel/preference resolution (a new `visitor_arrival` category) — whatever channel(s) the host's own preferences and the tenant's policy resolve to (push, email, SMS). Every channel carries the identical **one-tap confirmation link**; this doc introduces no inbound-SMS-reply-parsing mechanism, avoiding a new class of infrastructure for a single use case.

### Confirmation mode
3. A tenant/site-scoped **Host Confirmation Policy** (Settings & Preferences Definition) sets `mode`: `acknowledgment_only` or `gated`.
   - **3a. `acknowledgment_only`** — tapping the link records confirmation; it has no power over the Visit itself, which already stands on Pre-Registration Portal's own approval/screening. This is a pure FYI.
   - **3b. `gated`** — until confirmed, the Front Desk/Lobby Guard's console surfaces the Visit as **awaiting host confirmation**, a live status distinct from (and layered on top of) Visit's own `checked_in` state — Visit creation and badge printing are unaffected; this is a procedural expectation surfaced to the Guard, not a block on record creation. A Guard may always invoke **Release Visitor** (#6) to let the visitor proceed without waiting for confirmation, recorded distinctly as a non-confirmed release.

### Escalation
4. A **Host Arrival Escalation Policy** (Settings & Preferences Definition) independently sets `backup_escalation_minutes` (nullable) and `lobby_escalation_minutes` (nullable, expected greater than the backup threshold) — the same independently-configurable dual-threshold shape Visitor Kiosk App's Overdue Visit Policy already established, reused a second time. Each registers its own Duration Watchdog instance over `(host_arrival_acknowledgment, status, pending)`; crossing the backup threshold notifies the resolved backup host (#5) if one exists, crossing the lobby threshold notifies Front Desk/Security via the existing Critical Event Escalation Policy — no new alerting mechanism either time. Neither threshold blocks anything else; a still-open acknowledgment can be confirmed by host or backup at any point, even after an escalation tier has already fired.
5. **Backup host resolution follows the platform's explicit-beats-default resolution chain**: an explicit backup named on this specific Pre-Registration (`explicit_backup_host_ref`, retrofit) wins when set; otherwise the host's own standing default (`default_backup_host_ref`, retrofit to Employee); otherwise there is no backup tier and escalation skips straight to the lobby threshold.

### Guard release
6. **Release Visitor** registers as a Command/Action Bus action, available to Front Desk/Security regardless of Host Confirmation Policy mode — a no-op status note under `acknowledgment_only` (nothing was ever gating), but a recorded override under `gated`: the resulting Host Arrival Acknowledgment status (`guard_released_no_confirmation`) is honestly distinguishable from an actual host/backup confirmation, the same negative/exception-outcome-deserves-a-real-row discipline used throughout the platform.

## Data Model / Fields

**Host Arrival Acknowledgment** (feature-local, not an Activity extension)
- ack_id, visit_ref (FK → Visit), host_ref, resolved_backup_host_ref (nullable, resolved at creation per #5)
- status (pending, confirmed, guard_released_no_confirmation)
- confirmed_by (nullable — host or backup Person ref), confirmed_at (nullable)
- backup_escalated_at (nullable, timestamp marker), lobby_escalated_at (nullable, timestamp marker)
- confirmation_link_token (single-use, tied to this record)

**Host Confirmation Policy** (Settings & Preferences Definition)
- tenant_id/site_id, mode (acknowledgment_only, gated)

**Host Arrival Escalation Policy** (Settings & Preferences Definition)
- tenant_id/site_id, backup_escalation_minutes (nullable), lobby_escalation_minutes (nullable)

**Employee** *(retrofit — Person Registry)*
- default_backup_host_ref (nullable, FK → Person/Employee)

**Pre-Registration** *(retrofit — Pre-Registration Portal)*
- explicit_backup_host_ref (nullable, FK → Person/Employee — per-invitation override, takes precedence over the host's standing default)

## States & Transitions

**Host Arrival Acknowledgment:** `pending` → `confirmed` (host or backup taps the link) | `guard_released_no_confirmation` (Guard override under `gated` mode) — terminal either way; `backup_escalated_at`/`lobby_escalated_at` are timestamp markers recording that a tier fired, not states of their own, and don't block a later confirmation from landing normally.

## Integrations

- **Visitor Kiosk App**: source of the Visit check-in event this doc's Acknowledgment is created against; this doc introduces no change to Visit's own lifecycle or check-in mechanics.
- **Pre-Registration Portal** *(retrofit)*: gains `explicit_backup_host_ref`, an optional per-invitation backup-host override.
- **Person Registry** *(retrofit)*: Employee gains `default_backup_host_ref`, an optional standing backup.
- **Notifications Engine**: owns delivery for the new `visitor_arrival` category, resolved through its existing channel/preference mechanism — no new delivery infrastructure.
- **Active Call Alerts & Timers (Duration Watchdog)**: both escalation thresholds are new registered instances of the existing generalized mechanism.
- **Status & State Monitors / Active Call Alerts & Timers (Critical Event Escalation Policy)**: the lobby-tier escalation routes through the existing policy, unmodified.
- **Command/Action Bus**: Confirm (via the tap link) and Release Visitor both register as actions.
- **Active Incident Queue**: a `gated`-mode Visit awaiting confirmation is visible on the existing Visit `card` (Visitor Kiosk App's retrofit) with no new board mechanism — this doc adds no separate queue entry.

## Permissions

| Action | Site/Tenant Admin | Host / Backup Host | Front Desk/Security |
|---|---|---|---|
| Confirm arrival (tap link) | — | ✅ (own/delegated invitations only, via token) | ❌ |
| View live acknowledgment status for a site | ✅ | ❌ (their own invitations only) | ✅ |
| Release Visitor | ❌ | ❌ | ✅ |
| Configure Host Confirmation Policy / Host Arrival Escalation Policy | ✅ | ❌ | ❌ |

## Non-Functional / Constraints

- A confirmation link token is single-use and tied to exactly one Host Arrival Acknowledgment — no replay across visits or reuse after the record reaches a terminal status.
- Neither escalation threshold, nor an open `gated`-mode acknowledgment, ever blocks Visit's own check-in or check-out actions — consistent with the platform's standing rule that a compliance/notification mechanism must not become an operational blocker on the records it watches.
- Guard Release is an audit-tier event, distinctly logged as a non-confirmed override when it bypasses an open `gated` acknowledgment.

## Acceptance Criteria

- [ ] Checking in a Visit automatically creates a Host Arrival Acknowledgment and delivers a notification to the host through their resolved channel(s), with an identical confirmation link regardless of channel.
- [ ] Under `acknowledgment_only`, a visitor proceeds past the lobby immediately regardless of whether the host has confirmed; the Guard's console shows confirmation status for information only.
- [ ] Under `gated`, the Guard's console shows an unconfirmed Visit as awaiting host confirmation until the host/backup confirms or a Guard invokes Release Visitor.
- [ ] With no explicit or default backup configured, crossing the backup escalation threshold produces no backup notification and escalation proceeds directly to the lobby threshold.
- [ ] With an explicit per-invitation backup set, escalation notifies that backup rather than the host's standing default.
- [ ] A Release Visitor action under `gated` mode with no prior confirmation results in a Host Arrival Acknowledgment status of `guard_released_no_confirmation`, distinguishable from `confirmed` in every report/view that surfaces it.
- [ ] A confirmation link is rejected as invalid if reused after the Acknowledgment already reached a terminal status.
- [ ] Neither the backup nor lobby escalation threshold blocks the visitor's own Visit check-in/check-out at any point.

## Open Questions

- Whether backup host resolution should eventually draw from a real distribution/group directory once Module 17's User Group Directory exists, rather than a single-person backup field — flagged for reconciliation then, same deferred-integration posture as DAR's Shift Window pending Post Schedule Builder.
- Exact confirmation-link expiry mechanics (does it expire independent of the Acknowledgment's own terminal status) — technical-spec.
- Whether a `gated`-mode site ever needs a third, harder floor (e.g., a hard timeout that auto-releases the visitor rather than only alerting the lobby) — no target customer need identified; not built here, a lobby Guard already has Release Visitor available at any time.
