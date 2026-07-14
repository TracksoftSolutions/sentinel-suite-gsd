# Command Center Wallboard View

## Overview

Command Center Wallboard View is the unattended, admin-configured counterpart to Multi-Incident Console: wall-mounted TV arrays in a dispatch room or EOC rendering live situational data with nobody logged in at the screen. It confirms Multi-Incident Console's flagged prediction — Wallboard is the second real consumer of that doc's Panel Registry — and resolves the promotion the second-consumer pattern calls for, but only halfway, deliberately: the **panel_type catalog and each panel's rendering component** (Map, Queue, Kanban, Unit Roster, Detail, and a new `health` type added here) is retrofitted as a shared, cross-doc catalog rather than something Multi-Incident Console alone owns. *Arrangement* stays consumer-specific — nobody interactively drags a wall-mounted TV, so Wallboard introduces its own admin-authored **Display Profile** rather than reusing Console Layout's personal drag/dock/resize model.

This also resolves Real-Time Delivery & Server-Side Timers' own flagged open question ("does wallboard/EOC display need a dedicated unauthenticated-display device posture") with a real mechanism: a **Display Device**, a non-interactive identity an Admin provisions and explicitly scopes, since there is no human session to derive RBAC/ABAC from.

## Actors & Roles

- **Site / Tenant Admin** — provisions Display Devices, authors and assigns Display Profiles, registers Health Signals, sets the sensitivity ceiling on each device.
- **Dispatcher / Supervisor / EOC Staff** — ambient consumers of what's on screen; also benefit directly from the shared panel catalog by optionally pinning the new `health` panel into their own personal Multi-Incident Console layout.
- **Display Device** (non-human actor) — the provisioned screen itself: read-only, no login, renders exactly what its viewing scope and sensitivity ceiling permit.
- **Records Admin** — same read-model projection health visibility posture as other live-board docs, extended to Health Signal Registration's own health.

## User Stories

- As a **Site Admin**, I want to provision a wall-mounted screen as a Display Device with a one-time pairing token, so it displays live operational data without anyone ever logging into it.
- As a **Site Admin**, I want to build a Display Profile that assigns different panels to each screen in a TV array, with some screens rotating between views on a timer.
- As a **Site Admin**, I want to cap what a lobby-visible wallboard can ever render, so a BOLO subject's details or a sensitive Incident never appears there regardless of which panel is assigned to it.
- As a **Dispatcher or Supervisor**, I want a signage-only wallboard to flash a full-screen alarm the instant a persistent alarm fires, and return to normal the moment it's acknowledged from any console.
- As a **Dispatcher** who actively works off one of the wallboard screens as part of my own flow (e.g., a dedicated Queue monitor), I want that screen to show a non-blocking alarm indicator instead of a full-screen takeover, so an unrelated unit's missed check-in doesn't obscure what I'm actively watching — while still never letting me miss that the alarm happened.
- As a **Supervisor**, I want a System Health tile showing connectivity, active-tour counts, and integration status, so I notice a problem before someone reports it to me.
- As a **Site Admin**, I want an integration that hasn't registered a health signal to simply not appear on the health tile rather than error, so this doc's mechanism doesn't need a change every time a new integration ships.
- As a **Dispatcher**, I want to optionally pin the same Health panel into my own personal Multi-Incident Console layout, since it's the same shared panel catalog Wallboard uses.

## Functional Requirements

### Shared panel substrate (retrofit)
1. The panel_type catalog and rendering components first established in Multi-Incident Console's Panel Registry (`map`, `queue`, `kanban`, `unit_roster`, `detail`) are retrofitted as a cross-doc shared catalog rather than owned exclusively by that doc — the platform's promote-on-second-consumer pattern, confirmed here rather than left flagged. This doc contributes one new panel type: **`health`** (a configurable grid of Health Signal tiles, see below). Any catalog panel type is selectable both in Multi-Incident Console's personal Console Layout and in this doc's Display Profile — a Dispatcher can pin `health` to their own dock exactly as an Admin can assign it to a wallboard zone.
2. *Arrangement* stays deliberately consumer-specific: Console Layout (Multi-Incident Console) remains a personal, draggable, live-user mechanism; **Display Profile** (this doc) is an admin-authored, non-interactive, per-screen assignment. Neither reuses the other's arrangement model — only the underlying panel catalog/renderers are shared.

### Display Device
3. A Site/Tenant Admin provisions a **Display Device**: a non-interactive identity representing one physical screen, paired via a one-time admin-generated token — the same no-user-login posture as any shared/kiosk hardware.
4. Because there is no human session to derive RBAC/ABAC from, the provisioning Admin explicitly grants the Display Device a **viewing scope** (site + which panel types/layers it may render), capped at whatever the provisioning Admin could themselves see — never broader.
5. The Admin additionally sets a **max sensitivity tier** on the Display Device — an explicit ceiling (e.g., excludes BOLO-flagged subjects, sensitivity-classified Incidents) independent of, and in addition to, viewing scope, since a wall-mounted, often publicly-visible screen is a materially different exposure than a logged-in Dispatcher's own console. The ceiling applies uniformly across every panel the device ever renders, regardless of Display Profile content, and is enforced server-side — the filtered content is never sent to the device to begin with, not hidden client-side.
6. A Display Device reports a heartbeat; connectivity itself registers as a Health Signal (#15) — a dark or unreachable wallboard is visible to Supervisors as a health condition, not a silent absence.
7. Revoking a Display Device immediately blanks its screen and invalidates its pairing token; re-provisioning requires a new token, the same posture as revoking any other credential.

### Display Profile
8. A **Display Profile** defines one or more **screen zones** (a single screen, or a zone within a multi-monitor array). Each zone is independently either **static** (one fixed panel + its instance config — e.g., "screen 2 = Queue panel filtered to this site") or **rotation** (an ordered list of panel configs cycling on a configurable interval — e.g., "screen 3 alternates Map / Health every 30 seconds").
9. A Display Profile assigns to one or more Display Devices; an admin-locked profile blocks a narrower override, the same lock discipline used throughout Settings & Preferences.

### Alarm Alert Banner
10. A Display Device rendering a persistent, active, unacknowledged alarm from Real-Time Delivery's Alarm State Service (scoped to the device's site) always shows a visible indication — this cannot be turned off entirely, since a wallboard is precisely the safety-relevant surface a persistent alarm exists to reach. **How** it's shown is a per-Display-Device `alarm_banner_mode`, admin-set at provisioning (or edited later): **`full_screen_takeover`** (default — overrides whatever its zones are currently displaying, the right default for a pure situational-awareness/signage screen) or **`overlay_banner`** (a non-blocking strip at the top/bottom; zone content stays fully visible underneath — for a screen a Dispatcher is actively working from as part of their own flow, where a full-screen interrupt over someone else's missed check-in would obscure what they're actively monitoring). Either mode reverts/clears automatically the instant the alarm is acknowledged (from any console, per the service's existing "silences every console" rule) or resolved. Audio (where the device has speakers) is unaffected by banner mode — sound continues to signal urgency even when the visual is non-blocking. No new alarm logic either way; this is purely a rendering choice over existing server-side alarm state.
11. A Display Device cannot itself acknowledge an alarm — it has no user session. Acknowledgment happens from an actual console per Real-Time Delivery's existing permission table; the wallboard's banner clears the moment it does, same as every other subscribed console.

### Health Signal Monitor
12. A new **Health Signal Registration** (local to this doc, same registration-pattern discipline as Active Incident Queue's Queue Role Registration): any feature or adaptor registers a named signal — either **status** type (`healthy` / `degraded` / `down`, resolved by a status-query callback into the owning feature) or **metric** type (a live number, e.g., an active-tour count) — with a display label.
13. The `health` panel renders every currently-registered signal as a tile; an integration that hasn't registered simply doesn't appear (graceful, not an error) — no doc-wide retrofit required each time a new integration ships, the same precedent set by Historical CAD Log Reconstruction's Log Source Registration.
14. A status-query callback that errors or times out resolves to **`unknown`**, distinct from `degraded`/`down` — the platform is honest about "we don't know" versus "we checked, and it's unhealthy."
15. Day-one registrants (light retrofit — each feature keeps owning its own underlying status field, only adding a registration entry that points at it): Structured Logging & Audit Trails' SIEM Integration status, Background Job Processing's job engine health, GIS & Mapping Services' Map Provider Config connectivity, Real-Time Delivery's per-site Live Update Channel connection health, this doc's own Display Device heartbeat (#6), and Guard Tour & Checkpoint Verification's active-tour count (metric type).

## Data Model / Fields

**Display Device**
- device_id, tenant_id, site_ref, name
- assigned_display_profile_ref
- viewing_scope (panel types/layers permitted, capped at provisioning Admin's own access)
- max_sensitivity_tier (ceiling, excludes flagged-sensitive records regardless of profile content)
- alarm_banner_mode (full_screen_takeover [default], overlay_banner — cannot be set to "none"/off)
- provisioning_token (one-time, admin-generated, single-use)
- status (active, revoked), last_seen_at (heartbeat)

**Display Profile**
- profile_id, tenant_id, name
- screen_zones[] (zone_id, mode: static | rotation; static: panel_type + instance_config; rotation: rotation_list[panel_type + instance_config] + rotation_interval_seconds)
- locked (bool, admin-set)

**Health Signal Registration** (local to this doc)
- registration_id, signal_key, display_label, signal_type (status, metric), status_query_ref (callback into owning feature), registered_by

**Health Signal Reading** (live, computed per poll — not persisted)
- signal_key, value (status enum or metric number), last_updated_at, resolved_to_unknown (bool, set when the callback errored/timed out)

**Panel Registry** (shared catalog, physically defined in Multi-Incident Console — retrofit)
- panel_type_id gains `health` alongside `map`, `queue`, `kanban`, `unit_roster`, `detail`

## States & Transitions

**Display Device:** `provisioned` (token issued, unpaired) → `active` (paired, heartbeating) → `revoked` (blanked, token invalidated) — re-provisioning issues a new device record, not a reactivation of the revoked one.

**Display Profile:** `active` → `archived`, same posture as any Settings & Preferences-registered Definition; editing a live-assigned profile takes effect on next rotation tick or immediately for a static zone, no versioning/pinning needed since a wallboard has no "in-progress execution" to protect (unlike Guard Tour's plan/execution split).

**Alarm banner (per Display Device):** silent → flashing (persistent + active + unacknowledged, scoped to device's site) → silent (acknowledged elsewhere or resolved) — driven entirely by Alarm State Service, no independent state of its own.

## Integrations

- **Multi-Incident Console**: source of the shared panel_type catalog and its rendering components (retrofit — Panel Registry is no longer exclusively owned by that doc); this doc's Display Profile is a distinct, non-interactive arrangement mechanism over the same catalog.
- **Real-Time Delivery & Server-Side Timers**: every Display Device subscribes to its site's Live Update Channel for panel data and Alarm State; the Alarm Alert Banner is a direct rendering of that service's existing persistent-alarm state and reconnect re-fire rule — resolves that doc's own flagged open question about a dedicated unauthenticated-display device posture.
- **Authentication & Authorization**: RBAC/ABAC bounds what a Display Device's viewing scope can ever include, capped at the provisioning Admin's own access; the sensitivity-ceiling filter is enforced server-side alongside ordinary ABAC.
- **Settings & Preferences**: owns Display Profile locking, the same admin-lock discipline used platform-wide.
- **Unified Operational Picture (UOP) Map**: the `map` panel type a Display Profile or Console Layout selects is UOP Map's live composition (per that doc's own retrofit of Multi-Incident Console's Map panel) — Wallboard introduces no separate map rendering.
- **Structured Logging & Audit Trails, Background Job Processing, GIS & Mapping Services, Real-Time Delivery & Server-Side Timers, Guard Tour & Checkpoint Verification**: day-one Health Signal registrants (#15) — each retains its own status field, contributing only a registration entry here.
- **Historical Playback Console (Module 3, not yet specified)**: no overlap — Wallboard is strictly live/current-state, same boundary UOP Map already established.

## Permissions

- **Provision/revoke a Display Device, author/assign a Display Profile, manage Health Signal Registration**: Site/Tenant Admin.
- **View a wallboard's rendered output**: not a per-viewer permission — bounded entirely by the Display Device's own viewing scope and sensitivity ceiling, since there is no login at the screen. Anyone physically present sees exactly what the device is permitted to show, nothing more.
- **Acknowledge a persistent alarm**: not possible from a wallboard (no session) — inherits Real-Time Delivery's existing console permission table unchanged; the banner clears when acknowledged elsewhere.
- **Pin a shared panel type (including `health`) into a personal Console Layout**: inherits Multi-Incident Console's existing permissions, unchanged by this doc.

## Non-Functional / Constraints

- Latency and disconnect behavior inherit Real-Time Delivery's established contract wholesale: ≤2s server-to-console for safety-relevant deltas (alarm state foremost), site-scoped Live Update Channel subscription, freeze + visible staleness + polling fallback + snapshot resync on reconnect — a Display Device's own disconnect is rendered as visible staleness, never silently frozen-looking-live.
- Health Signal polling/refresh interval is a technical-spec tuning parameter (metrics aren't safety-relevant like alarms, so a slower cadence than the 2s console target is expected and acceptable) — see Open Questions.
- The sensitivity ceiling and viewing scope are enforced server-side at the point of composing what's pushed to a Display Device — never a client-side hide, consistent with the platform's established RBAC/ABAC filtering discipline of never trusting a client to conceal what it already received.
- A Display Device's provisioning token is single-use and admin-generated; standard credential hygiene (expiry if unused) applies, exact mechanics deferred to technical spec.
- Wallboard is inherently online-only, same posture as UOP Map — no offline capture concept applies to an unattended live display.

## Acceptance Criteria

- [ ] A newly provisioned Display Device displays nothing until paired with its one-time token, then renders its assigned Display Profile without any login prompt.
- [ ] A Display Device's rendered content never includes a record above its configured max sensitivity tier, even if that record would otherwise fall within its viewing scope's site/panel access.
- [ ] Revoking a Display Device blanks its screen immediately and its prior pairing token no longer authenticates.
- [ ] A Display Profile's rotation zone cycles through its configured panel list at the configured interval; a static zone never changes without an explicit profile edit.
- [ ] A persistent, unacknowledged alarm scoped to a Display Device's site triggers that device's configured `alarm_banner_mode` — a full-screen override for `full_screen_takeover` devices, a non-blocking strip with zone content still visible for `overlay_banner` devices; acknowledging it from any other console clears the indicator on the wallboard within the platform's standard propagation latency.
- [ ] There is no way to configure a Display Device to show no alarm indication at all — `alarm_banner_mode` accepts only `full_screen_takeover` or `overlay_banner`.
- [ ] The `health` panel shows a tile for every currently-registered Health Signal and cleanly omits any unregistered integration rather than erroring.
- [ ] A Health Signal whose status-query callback fails or times out shows `unknown`, not `degraded`/`down` and not blank.
- [ ] A Dispatcher can add the `health` panel type to their own personal Multi-Incident Console layout using the same shared catalog Wallboard draws from.
- [ ] An admin-locked Display Profile cannot be overridden by reassigning a different profile to one of its devices without unlocking it first.

## Open Questions

- Exact default Health Signal polling/refresh interval — a technical-spec tuning concern, not committed here.
- Whether Health Signal Registration should eventually become a required step for every new Activity-generating integration (especially Module 19's future upstream adaptors), or remain permanently opt-in — the same open framing Active Incident Queue left for Queue Role Registration; not resolved here.
- Exact Display Device pairing-token expiry/rotation mechanics — standard credential hygiene, deferred to technical spec.
- Whether Platform Super Admin needs cross-tenant Display Device inventory visibility (e.g., for fleet-managing hardware across many tenants) — no target customer need identified yet; not speculated further here.
