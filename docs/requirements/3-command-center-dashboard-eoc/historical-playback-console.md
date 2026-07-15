# Historical Playback Console

## Overview

Historical Playback Console is the platform's time-scrubbing, synchronized-multi-track replay tool — the "flight recorder" counterpart to Unified Operational Picture (UOP) Map's live view, exactly the boundary UOP Map itself deferred here ("all trail/breadcrumb/time-scrub display... belongs entirely to Historical Playback Console... which queries GIS's Position Record history directly"). Like UOP Map, it introduces no new position or event storage: it is a read composition over already-established sources, replayed against a scrub cursor instead of a live subscription. This closes out Module 3 (9 of 9 features).

Three tracks compose onto one synchronized timeline, matching MODULES.md's original framing ("GPS Path Playbacks," "EOC Event Replays: syncing video feeds, CAD dispatches, and alarm logs"):

- **Spatial track** — UOP Map's own map composition (units, activity pins, geofences, overlays, camera positions, alarm zones), rendered at whatever moment the scrub cursor sits on, sourced from GIS & Mapping Services' Position Record history rather than the live feed.
- **Event track** — a chronological marker list driving the same cursor, reusing Entity Relationships & History's Interaction Timeline as the event feed (dispatch phase changes, incident updates, alarm signals) rather than a new event log.
- **Video track** — where a Camera's VMS adaptor declares time-addressable playback capability, its marker deep-links into the VMS's own recorded-footage viewer at the approximate timestamp — the platform never stores or streams the footage itself, consistent with the integrate-don't-replace boundary.

No new mapping, timeline, or telemetry mechanism is invented; this doc is entirely about the replay/scrub composition over sources every prior Module 2/3 doc already built.

## Actors & Roles

- **Supervisor / Investigator** — primary user: reviews a specific incident's response, a shift's activity, or a use-of-force/complaint investigation window.
- **Dispatcher** — occasional user, reviewing recent activity (e.g., confirming when a unit actually cleared a scene).
- **EOC Coordinator** — reviews an EOC Activation's full response after the fact.
- **Contractor site staff (Client Engagement)** — scoped to the sites their active Engagement covers, same as UOP Map.
- **Site/Tenant Admin** — grants the Historical Playback Viewer permission; no separate configuration surface of its own.

## User Stories

- As a **Supervisor**, I want to scrub back through a specific incident's timeline and watch responding units' positions, state changes, and alarm/dispatch events replay in sync, so I can reconstruct exactly what happened without piecing it together from separate reports.
- As an **Investigator**, I want to open playback directly from an Incident's detail view with the time window already bound to that incident's own lifecycle, so I don't have to manually figure out the right start/end times.
- As a **Supervisor**, I want to open a free-form playback session for a site and a time range I pick myself, for a shift review that isn't tied to any one incident.
- As a **Supervisor**, I want to click an event marker (an alarm signal, a dispatch phase change) and have the map cursor jump straight to that moment, rather than scrubbing manually to find it.
- As an **Investigator**, I want to click a camera marker near the time of interest and get taken straight into that camera's recorded footage at approximately the right moment, without hunting through the VMS separately.
- As a **Supervisor**, I want the console to clearly show me when a layer's data doesn't reach as far back as the time range I asked for, rather than silently showing an empty or misleadingly frozen map.

## Functional Requirements

### Core composition & scrub mechanics
1. A **Playback Session** is a transient, feature-local scoping of site + time window + selected layers — never persisted as an Entity/Activity/EntityAssociation, mirroring UOP Map's "no new storage" posture. Closing the console discards it; there is no save/resume of a session's exact configuration (see #14 on the export decision).
2. A single **master scrub cursor** drives all three tracks in lockstep: dragging it, using play/pause/speed controls, or clicking an event/video marker all move the same cursor and re-render every layer at that instant.
3. The spatial track reuses UOP Map's exact layer set and toggles (Unit Positions, Activity Pins, Geofences, Overlay Layers, Camera Positions, Alarm Zones) and its clustering behavior unmodified — this doc changes *when* positions/state are read from, never *how* they're rendered.
4. **Unit Positions** at the cursor read GIS & Mapping Services' Position Record history (the existing GPS track log) for that instant, with the same GPS-denied proxy-position fallback UOP Map already uses (rendered at its nearest preceding checkpoint scan).
5. **Unit State** (pin color) at the cursor is derived from each officer's Dispatch record's own `phase_timestamps` at that instant — the same "read business-record timestamps, never the audit trail" discipline Historical CAD Log Reconstruction established. **Known, deliberately accepted gap**: a manual transition with no Dispatch phase backing it (e.g., `out_of_service` toggled outside any dispatch) has no timestamped business record to replay from and will not appear correctly in playback — flagged honestly, not silently patched, matching that doc's Log Source Registration precedent rather than reaching for Structured Logging & Audit Trails as a second source of truth.
6. **Activity Pins** at the cursor are reconstructed from Entity Relationships & History's Interaction Timeline entries with a resolvable location, filtered to those whose activity was open/attached as of the cursor's instant (created before, and not concluded/removed before, the cursor) — the same urgency-tier styling as UOP Map, degrading to `unclassified` for anything unmapped.
7. **Alarm Zones** and **Environmental Sensors** at the cursor reconstruct zone/sensor state from Activity Registry's Signal Disposition telemetry-tier sequence (per-signal timestamped metadata, per the platform's boundary-fidelity commitment) — the nearest preceding signal at or before the cursor determines the rendered state, same as GPS's nearest-preceding-fix convention for GPS-denied units.
8. **Camera Positions** render statically (mount history reuses Camera Mount Association's own active/removed sequence, so a camera relocated mid-window shows at its position as of the cursor); clicking one opens the video track (#11).

### Event track
9. The **Event Timeline panel** lists Entity Relationships & History's Interaction Timeline entries for the session's site and window (or the launching record's own scope, when entity-scoped — see #13), each rendered via its entity's registered display label exactly as that doc already specifies. Clicking an entry moves the master cursor to its timestamp; the currently-active entry (nearest at-or-before the cursor) is highlighted as the list auto-scrolls with playback.
10. No new event storage or event type is introduced — the panel is a windowed, chronological rendering of a read-model that already exists.

### Video track
11. A **Camera marker** appears on the Event Timeline (and as a click target on its map pin) only for Cameras whose linked VMS adaptor declares a new **historical playback capability** (retrofit to Live Camera Feed Ingestion's adaptor-declared capability set, alongside `embed_mode`/PTZ) — an adaptor that only supports live embed/deep-link contributes no video track entries. Clicking a capable marker deep-links into the VMS's own recorded-footage viewer, seeked to the approximate corresponding timestamp; the platform never stores, transcodes, or re-hosts the footage itself.
12. Timestamp alignment between the platform's clock and the VMS's own recorded timeline is best-effort (approximate seek), consistent with the adaptor pattern's disclosed per-vendor fidelity rather than an implied frame-accurate guarantee.

### Entry points
13. Historical Playback Console is reachable two ways, both landing on the identical console: an **entity-scoped quick-launch** from an Incident, Call, or EOC Activation's detail view (Command/Action Bus context-seeding, the platform's established launch-point pattern) with the time window auto-bound to that record's own lifecycle (received→cleared for a Call/Incident, active→deactivated for an EOC Activation) and the Event Timeline pre-filtered to that record's own descendant activity; and a **standalone free-form console** (site + manually chosen time range) for shift reviews or investigations not tied to one record. A user may widen or narrow an entity-scoped session's time window manually after opening it.

### Retention boundaries
14. Each track independently respects its own source's tenant-configured retention window (GIS's GPS Retention Policy for positions, Activity Registry's telemetry retention for signals, Entity Relationships & History's own data lifetime for events) — a requested window that exceeds a given track's retention renders that track only up to its actual boundary, with a visible **"no data before [date]"** indicator on that track specifically, rather than blocking the whole session or silently showing an empty/frozen layer. Different tracks may have different effective start points within the same session.

### No report/export
15. Historical Playback Console is a **view-only console, deliberately not a report-generation feature** — unlike DAR/Passdown/Incident Report/Historical CAD Log Reconstruction, no "snapshot to Document" action exists here. A Supervisor documenting findings from a playback review does so through the ordinary channels those records already provide (an Incident Update, a DAR entry) referencing the time range reviewed, not through this console.

## Data Model / Fields

Historical Playback Console introduces no new Entity, Activity, or EntityAssociation type — it is a read composition, like UOP Map. Feature-local, non-persisted concepts:

**Playback Session** (transient, client/query-state only — not a stored record)
- site_ref, window_start, window_end, launched_from (nullable — Incident/Call/EOC Activation ref, when entity-scoped), active_layers{} (same shape as UOP Map's Map View Preset)

**Retention Boundary** (computed, not stored)
- per-track effective earliest-available timestamp, resolved live from each source's own retention configuration at session open

**Historical Playback Capability** *(retrofit — Live Camera Feed Ingestion's VMS Adaptor Registration)*
- historical_playback_supported (bool, per adaptor, default false), seek_precision_notes (free text, adaptor-declared fidelity disclosure)

## States & Transitions

Historical Playback Console has no record lifecycle of its own — a live console composition, not a governed record, identical in this respect to UOP Map. A Playback Session exists only for the duration it's open in the UI.

## Integrations

- **GIS & Mapping Services**: source of Position Record history (the GPS track log) and its GPS Retention Policy, which bounds the spatial track's earliest replayable point.
- **Unified Operational Picture (UOP) Map**: source of the shared map composition, layer set, toggles, clustering, and pin-click action menu — this doc replays the identical rendering against historical data instead of a live subscription; UOP Map's own doc explicitly deferred all trail/time-scrub display here.
- **Entity Relationships & History**: source of the Event Timeline panel's entries (Interaction Timeline), windowed to the session's site/time range or the launching record's own scope.
- **Activity Registry**: source of Alarm Zone/Environmental Sensor historical state via the Signal Disposition telemetry tier's retained per-signal metadata; source of Activity Pin presence via each activity's own timestamped lifecycle.
- **Status & State Monitors**: Unit State at the cursor is derived from Dispatch's `phase_timestamps`, not a stored Unit State history (there isn't one) — the accepted gap for manually-transitioned states is recorded here (#5), not worked around.
- **Live Camera Feed Ingestion** *(retrofit)*: gains the Historical Playback Capability declaration on its VMS Adaptor Registration; this doc's video track consumes it to decide which Camera markers deep-link into recorded footage.
- **Alarm Panel Monitors & Panic Alerts, Environmental & Weather Map Overlays**: their Signal Disposition-fed telemetry is this doc's source for Alarm Zone/Environmental Sensor replay state — no changes to either doc required.
- **Command/Action Bus**: the entity-scoped quick-launch reuses the platform's existing context-seeding launch-point mechanism; this doc registers no new action types.
- **Structured Logging & Audit Trails**: explicitly **not** a data source for any track, matching Historical CAD Log Reconstruction's precedent — but opening a Historical Playback session is itself an audit-tier event (see Non-Functional), given the movement-history sensitivity that motivated the dedicated permission below.
- **Authentication & Authorization**: owns the new Historical Playback Viewer permission (see Permissions).
- **Tenant Management (Client Engagement)**: bounds a Contractor staff member's session site scope to their active Engagement, same as UOP Map.

## Permissions

- **Historical Playback Viewer** (new, site-scoped RBAC + ABAC overlay): required to open the console at all, layered on top of — never replacing — each individual pin's/entry's own existing permission/ABAC filtering, the same layered-gate discipline as UOP Map Viewer and Interaction Timeline Viewer. Kept deliberately distinct from UOP Map Viewer rather than folded into it: replaying an officer's full historical movement trail is a materially different exposure than seeing their live position, the same reasoning already applied to GIS's GPS Retention Policy discussion.
- Clicking a marker/pin to open its existing Detail surface requires whatever permission that surface already requires — no separate "act from playback" permission (playback is read-only regardless; no dispatch/assign actions are exposed here).
- Opening a video-track deep-link requires whatever the VMS adaptor's own authentication/authorization already requires on its side — the platform does not proxy or elevate access to the VMS.

## Non-Functional / Constraints

- Opening a Historical Playback session is an audit-tier event (who viewed which site/entity, what window, when) — the same visibility discipline BOLO Flag lifecycle and merge actions already get, warranted here specifically by the movement-surveillance sensitivity that justified a dedicated permission rather than reusing UOP Map Viewer.
- Each track's effective retention boundary must be computed and surfaced before or immediately upon session open, never discovered mid-scrub as a silent gap.
- Historical Playback Console is inherently online-only, like UOP Map — no offline capture concept applies to a read-only historical console.
- Scrub/playback responsiveness (smooth cursor drag, no visible re-render lag) is a technical-spec/UX-tuning concern; no platform-wide real-time NFR applies here since this is not a live channel subscription — Real-Time Delivery's latency targets are explicitly not the relevant baseline for a historical query.
- Video deep-link timestamp alignment is best-effort per adaptor-disclosed fidelity (#12), never guaranteed frame-accurate.

## Acceptance Criteria

- [ ] Opening playback for an Incident from its detail view launches the console with the time window pre-bound to that Incident's own received→cleared lifecycle and the Event Timeline pre-filtered to its descendant activity.
- [ ] Opening the standalone console lets a user pick a site and an arbitrary time range with no launching record required.
- [ ] Dragging the scrub cursor updates unit positions, activity pins, alarm zone/environmental sensor state, and the highlighted Event Timeline entry in lockstep, all reflecting the cursor's instant.
- [ ] A unit's position at a given cursor time matches GIS & Mapping Services' Position Record history for that instant; a GPS-denied unit renders at its nearest preceding checkpoint-scan proxy position.
- [ ] A unit's pin color at a given cursor time matches the state derivable from its Dispatch's `phase_timestamps` at that instant; a purely-manual state transition with no Dispatch backing is confirmed to not replay correctly, and this is documented as a known, accepted gap rather than silently wrong.
- [ ] Clicking an Event Timeline entry moves the master cursor to that entry's timestamp and updates every track.
- [ ] Clicking a Camera marker whose adaptor declares historical playback capability opens the VMS's recorded footage seeked near the corresponding timestamp; a Camera whose adaptor does not declare this capability shows no video-track entry at all.
- [ ] Requesting a time window that exceeds one track's retention window renders that track only up to its actual boundary with a visible "no data before" indicator, while other tracks with longer retention still render their full requested range.
- [ ] A user without Historical Playback Viewer cannot open the console even if they hold UOP Map Viewer; a user with Historical Playback Viewer but no access to a specific sensitive Activity does not see that Activity's historical pin or Event Timeline entry.
- [ ] Opening a playback session produces a discoverable audit-tier event recording who viewed what scope and window.
- [ ] The console exposes no "save as report" or "export to Document" action anywhere in its UI.
- [ ] A Contractor staff user's playback session is bounded to their active Client Engagement's site scope, identical to UOP Map's existing rule.

## Open Questions

- Exact scrub-bar UX (fixed playback speeds vs. continuous, minimum time granularity) — a technical-spec/UX-tuning concern, not decided here.
- Whether Activity Pin presence reconstruction (#6) ever needs a richer per-type status-history source than "resolvable Interaction Timeline entries with a location" for types whose lifecycle isn't well-represented by that timeline — no gap identified by any currently-specified Activity extension, revisit if one surfaces.
- Whether a future large-enterprise deployment ever needs multi-site simultaneous playback (reviewing a coordinated response across sites at once) — no such need identified by any of the three MVP target customers, consistent with UOP Map's own "site is the design unit" posture; not built here.
- Exact audit-event granularity for playback session viewing (per-session vs. per-scrub-action) — a technical-spec-level decision; per-session is assumed as the minimum bar.
- Whether Historical Playback Capability (VMS adaptor) should eventually be a required declaration for every VMS integration profile (mirroring Health Signal Registration's own open question about mandatory vs. permanently-opt-in registration) — left open, same framing as that doc.
