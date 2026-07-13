# Real-Time Delivery & Server-Side Timers

**Module:** 0 Platform Core
**Status:** Draft — added during the platform design review to close the "real-time delivery has no owner" defect; elicited (console-loss behavior, alarm re-fire, scale anchor, latency targets)

## Overview

Several shipped specs assume real-time behavior nobody owned: Unit Dispatch defers "the exact real-time delivery mechanism," Silent Mobile Dispatching's escalation net "must fire reliably even if the underlying push delivery itself failed" (implying server-side timers), Duration Watchdogs' persistent alarms assume a listening console, and the Active Incident Queue / Multi-Incident Console assume live-updating boards. This doc owns all of it — three mechanisms and one set of platform-wide targets:

1. **Live Update Channel** — the persistent server↔console connection streaming CQRS read-model deltas (queue cards/feeds, Unit State, map positions, alarm state) to connected clients, with honest disconnect behavior.
2. **Server-Side Timer Service** — the single owner of every escalation/watchdog schedule (Duration Watchdogs, Safety Check-in cycles, unacknowledged-dispatch escalation, missed-tour detection). Timers live and fire on the server, independent of any client being connected — a safety timer must never depend on a browser tab.
3. **Alarm State Service** — persistent alarms (Duration Watchdog `persistent` mode) are server-side state; consoles are speakers, not owners. A reconnecting console immediately re-fires anything still unresolved and unacknowledged.
4. **Platform baseline real-time targets** — the platform's first quantitative NFR anchors, site-scoped per elicitation: **the site is the design unit; the enterprise is a roll-up.** Every earlier doc's "near-real-time" / "must remain performant" acceptance criterion resolves against this doc's numbers unless it states stricter ones.

## Actors & Roles

- **Dispatcher / Console Operator / EOC viewer** — consumes live boards; experiences the disconnect/resync contract.
- **Guard/Officer (mobile)** — receives dispatch/alert delivery via Notifications Engine push; their phase taps and scans propagate to consoles within the console latency target.
- **Timer Service (system)** — evaluates schedules and publishes domain events; restart-safe.
- **Tenant Admin** — configures polling-fallback interval and any site-tier overrides via Settings & Preferences.

## User Stories

- As a **Dispatcher**, when my console loses connection I want it clearly frozen with an age counter — not silently stale — while it resyncs itself, and I want my override actions to keep working because my radio still does.
- As a **Supervisor**, I want an overdue safety check-in to alarm even if every console in the room was disconnected when it triggered — and to sound the moment one reconnects if it's still unresolved.
- As an **Officer**, I want my "arrived" tap visible to dispatch within a couple of seconds.
- As a **Security Director** with forty sites, I want enterprise dashboards rolled up across sites without pretending to be a single live board of a thousand officers.

## Functional Requirements

### Live Update Channel
1. Each connected client (dispatch console, wallboard, mobile app foreground) holds a persistent connection subscribed to **site-scoped channels** (a console typically watches one site or a small set; an EOC view may watch several). Enterprise-wide live subscription is deliberately not a supported shape — roll-up views are aggregation read-models (#12).
2. The channel streams CQRS read-model deltas; every message carries a **per-channel monotonic sequence number**. A client detecting a gap requests a snapshot resync (snapshot-then-stream) rather than continuing on missing data.
3. Every pushed update is RBAC/ABAC-filtered per the recipient's own permissions, re-evaluated per the established Authentication & Authorization rule — the channel never becomes a side-door around read permissions. (The per-push evaluation cost flagged as an open question there is bounded here by site-scoping: a channel's audience and data are both site-partitioned.)
4. **Disconnect contract (elicited): freeze + banner + auto-resync.** On connection loss the console keeps displaying its last state, visibly frozen: a prominent staleness banner with a live age counter (WCAG-compliant, never color-only), automatic reconnection attempts with backoff, and polling fallback at a Settings-registered interval while disconnected. **Actions remain enabled** — the dispatcher may know things the console doesn't (radio still works); confirmation gates provide the guard rail, not a lockout.
5. On reconnect: snapshot resync, then streaming resumes; the staleness banner clears only after the snapshot lands.

### Server-Side Timer Service
6. One platform service owns evaluation of every time-based trigger already specified elsewhere: **Duration Watchdog** instances (Time-on-Scene, Pending Call Alarm, Enroute Timer), **Safety Check-in** cycles, **unacknowledged-dispatch escalation** (Silent Mobile Dispatching), and **missed-tour detection** (Guard Tour's `missed` Patrol creation, which that doc already required to run server-side). Those docs own their rules; this service owns the clock.
7. Timers are **persistent and restart-safe**: schedules survive process restarts and fire late-but-fired after downtime, never silently skipped. A timer firing publishes the owning feature's domain event exactly as those docs specify — this service adds no alert paths of its own.
8. Timer evaluation is independent of client connectivity by construction — a safety check-in expires and escalates with zero consoles connected.

### Alarm State Service
9. A `persistent`-mode Duration Watchdog alarm is a **server-side alarm state** (active → resolved | acknowledged), not a console-side sound loop. Connected consoles render/sound it; acknowledgment (per Active Call Alerts & Timers' existing semantics) is recorded server-side and silences every console.
10. **Reconnect re-fire (elicited):** a console (re)connecting immediately receives — and audibly re-fires — every alarm still active and unacknowledged for its subscribed sites. Conditions resolved while it was disconnected stay silent; history is in the audit trail, not the speakers.

### Mobile delivery path
11. Device delivery (dispatch notifications, check-in prompts) remains owned by Notifications Engine (APNs/FCM mechanics, preferences, escalation chains); this doc contributes the **delivery targets** (#13) and the timer backstop: Silent Mobile Dispatching's "safety net must fire even if push failed" is satisfied because the acknowledgment timer runs in the Timer Service (#6), not on the device that may never have received the push.

### Platform baseline real-time targets (elicited — the site is the design unit)
12. **Site tiers** (a tenant is a roll-up of sites; no single live surface renders the enterprise in real time):
    - **Solo site**: 1 officer on shift, no dispatcher — mobile-only, no console; real-time obligations are push delivery and timer evaluation only.
    - **Mid site**: ~25 officers on shift, 1–2 consoles.
    - **Large campus** (design ceiling per site): ~300 officers on shift, ~10 concurrent consoles, ~50 promoted activities/minute sustained, storm bursts to ~500 signals/minute at the telemetry tier (absorbed by the Signal Disposition valve and storm collapse — promoted-Activity volume stays bounded).
    - **Tenant roll-up**: 1,000+ officers across many sites; enterprise dashboards and KPI views are aggregation read-models with **≤30s freshness**, explicitly not live boards.
13. **Latency targets** (within a site's channels, at large-campus load): **≤2 seconds** server-to-console for safety-relevant updates (phase taps, Unit State, queue changes, alarm firing); **≤5 seconds** server-to-device for push delivery where the OS push service cooperates (APnS/FCM variability disclosed, with the Timer Service backstop making delivery failure detectable rather than silent); timer firing accuracy within **±5 seconds** of schedule.
14. These numbers are the **platform-wide baseline**: any earlier doc's "near-real-time," "promptly," or "must remain performant with many concurrent Dispatchers" acceptance criterion is testable against #12–13 unless that doc states stricter targets. Registered in `_DECISIONS.md` as the resolution of the design review's "zero quantitative NFRs" defect.

## Data Model / Fields

**Channel Subscription** (transient, connection-scoped)
- connection_id, user_ref, site_refs[], channel_keys[], last_delivered_sequence

**Timer Schedule** (persistent)
- timer_id, tenant_id, owning_feature (duration_watchdog | safety_checkin | dispatch_ack_escalation | missed_tour | …)
- target_ref (the Dispatch/Call/Route Assignment/etc. being watched), fire_at / interval, status (armed, fired, cancelled)
- fired_at (nullable), late_fire (bool — fired after downtime recovery)

**Alarm State** (server-side, per Alarm State Service)
- alarm_id, tenant_id, site_ref, source_watchdog_instance_ref, target_activity_ref
- status (active, acknowledged, resolved), raised_at, acknowledged_by/at (nullable), resolved_at (nullable)

## States & Transitions

**Console connection:** `live` → `disconnected` (banner + age counter + polling fallback + actions enabled) → `resyncing` (snapshot requested) → `live` (banner clears on snapshot).

**Timer Schedule:** `armed` → `fired` (on time, or late-but-fired after downtime — never silently skipped) | `cancelled` (underlying condition resolved first).

**Alarm State:** `active` → `acknowledged` (server-side, silences all consoles) | `resolved` (condition cleared) — a console (re)connecting re-fires anything still `active` and unacknowledged.

## Integrations

- **Unit Dispatch & Proximity Routing**: resolves that doc's open question ("exact real-time delivery mechanism… not solved here") — console propagation via the Live Update Channel, device delivery via Notifications Engine, both against #13's targets.
- **Silent Mobile Dispatching**: acknowledgment-escalation timing owned by the Timer Service; the safety net no longer implicitly depends on successful push.
- **Status & State Monitors / Active Call Alerts & Timers**: Duration Watchdog and Safety Check-in evaluation runs in the Timer Service; `persistent` alarms live in the Alarm State Service with reconnect re-fire.
- **Guard Tour & Checkpoint Verification**: missed-tour detection's server-side schedule requirement is fulfilled here.
- **Active Incident Queue / Multi-Incident Console / future Module 3 wallboards**: consume the Live Update Channel; the Card/Feed read-model deltas are what streams.
- **Notifications Engine**: owns device push mechanics and preferences; this doc adds targets and the timer backstop, never a parallel delivery path.
- **API & Messaging Layer**: owns the physical connection surface (WebSocket endpoint conventions); this doc owns subscription semantics, sequencing, and the disconnect contract.
- **Authentication & Authorization**: per-push RBAC/ABAC filtering rule, bounded by site-scoped channels.
- **Settings & Preferences**: polling-fallback interval; any site-tier target overrides.
- **Structured Logging & Audit Trails**: alarm acknowledgments, late timer fires, and resync events are audit-relevant; routine deltas are not.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|---|
| Subscribe to a site's live channels | ✅ (own site, own-scope data) | ✅ (assigned sites) | ✅ | ✅ |
| Acknowledge a persistent alarm | ❌ | ✅ | ✅ | ❌ |
| Configure polling-fallback interval | ❌ | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- The targets in #12–13 are the platform-wide baseline real-time NFRs; regressions against them at the stated tier loads are release blockers for the affected surface.
- Timer Service downtime must produce late-but-fired timers with `late_fire` flagged — never silently skipped schedules; recovery fires in original schedule order.
- The staleness banner and alarm re-fire must be WCAG 2.1 / Section 508 compliant (non-color-only, screen-reader announced).
- Channel infrastructure must degrade gracefully under storm bursts: telemetry-tier volume (#12's 500 signals/minute) never reaches live channels directly — only promoted Activities and alarm state do (the Signal Disposition valve is the admission control).

## Acceptance Criteria

- [ ] A phase tap on an officer's device appears on a subscribed console within 2 seconds at large-campus tier load.
- [ ] Killing a console's connection freezes its display with a visible, aging staleness banner while actions remain invocable; restoring the connection snapshot-resyncs and clears the banner.
- [ ] A sequence gap on a channel triggers snapshot resync rather than silent continuation.
- [ ] A Safety Check-in expiring with zero consoles connected still creates its `missed` record and fires its domain event on schedule.
- [ ] Restarting the Timer Service mid-schedule produces a late-but-fired timer flagged `late_fire`, never a skipped one.
- [ ] A console reconnecting while a persistent alarm is still active and unacknowledged audibly re-fires it; one whose condition resolved during the disconnect stays silent.
- [ ] Acknowledging an alarm on one console silences it on every other connected console.
- [ ] A user's live channel never delivers an update their RBAC/ABAC permissions would deny on direct query.
- [ ] An enterprise roll-up dashboard reflects site activity within 30 seconds without holding live subscriptions to every site.

## Open Questions

- Physical transport specifics (SignalR vs raw WebSocket conventions, connection multiplexing, horizontal scale-out of channel state) — technical spec, owned jointly with API & Messaging Layer.
- Whether wallboard/EOC displays (Module 3, unspecified) need a dedicated unauthenticated-display device posture (shared-screen auth) — deferred to Module 3 elicitation; the channel semantics here are ready for it.
- Exact polling-fallback default interval — Settings-registered; pending UX design.
- Whether solo-site tenants (no console at all) warrant a trimmed mobile-only supervisor view consuming the same channels — deferred to the lone-officer market/persona decision already open in Offline Data Sync.
