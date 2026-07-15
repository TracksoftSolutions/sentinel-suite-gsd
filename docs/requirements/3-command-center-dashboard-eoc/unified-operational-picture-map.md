# Unified Operational Picture (UOP) Map

## Overview

UOP Map is the platform's live, map-based situational-awareness console: on-duty unit positions (colored by Unit State), active Call/Incident/Dispatch pins (Active Incident Queue's Card read-model), Geofences, and reference Overlay Layers composed onto one screen, updated in real time. It is a **module-specific consumer** of GIS & Mapping Services' primitives — position feed, geofence engine, overlay rendering, map provider adaptor — exactly as that doc already anticipated ("Command Center's UOP Map... compose this feature's primitives into their own purpose-built UI and business logic. None of them re-implement tile serving, geofence math, or position storage"). This doc is that composition layer: no new mapping mechanism, a new console.

UOP Map exists in two framings that share one underlying view: a **dedicated, full-screen Command Center/EOC destination** (this doc's primary subject, including a multi-site rollup mode), and the same view **registered as the `map` panel type** inside Multi-Incident Console's existing Panel Registry — a Dispatcher pulls it up inline without leaving their multi-pane workspace. Multi-Incident Console's Map panel previously described itself as a bare embed of GIS & Mapping Services' raw map view; it is retrofitted here to embed this doc's fuller UOP Map composition instead (units + activity pins + geofences + overlays, not just tiles) — avoiding two competing map implementations at slightly different depths.

Elicited now, ahead of the MVP-blocking Module 9 (Location Hierarchy Designer, zones) and Module 7 (hazard-by-location) slices, per the deliberate consumers-before-mechanism elicitation-order decision — UOP Map is the heaviest consumer of both, so its shape should inform their design rather than the reverse. This doc does not speculate their fields; hazard/zone-hierarchy content is a forward-referenced future Overlay Layer, nothing more.

## Actors & Roles

- **Dispatcher** — primary day-to-day viewer/actor, typically single-site scope, Dispatcher Default layer preset.
- **Supervisor / EOC Staff** — broader default preset (adds Route Assignment rollups, multi-site rollup access where their grant spans multiple sites), same underlying permission.
- **Contractor site staff (Client Engagement)** — view scope bounded to the sites their active Engagement covers, never a Client's other sites or other Contractor relationships (existing Client Engagement rule, unchanged here).
- **Site/Tenant Admin** — configures Map View Presets, the geofence-highlight tenant default, and locks/unlocks them via Settings & Preferences.
- **Records Admin** — same viewing access as any permitted viewer, plus read-model projection health visibility (lag/staleness), same posture as Entity Relationships & History and Active Incident Queue.

## User Stories

- As a **Dispatcher**, I want to see every on-duty unit and active call/incident on one map, unit pins colored by their current state, so I can build situational awareness at a glance without cross-referencing separate lists.
- As a **Supervisor**, I want my default view to also show Route Assignment-level rollups, not just individual Patrols, matching what I already get in Active Incident Queue's Supervisor/EOC preset.
- As an **EOC Coordinator** overseeing several sites, I want a rollup view of per-site summary tiles so I can spot which site needs attention, then drill straight into that site's live map.
- As a **Dispatcher**, I want to click a unit or incident pin and get the same action menu I'd get elsewhere (assign, view detail) without leaving the map, with the same confirmation gates that already apply to those actions.
- As a **Dispatcher on a large campus**, I want nearby pins to cluster at zoom-out so the map stays legible with hundreds of units and activities in view, and to expand automatically as I zoom in.
- As a **Tenant Admin**, I want to set whether a triggered geofence visually flashes on the map by default, and let individual users override that for themselves.
- As a **Dispatcher**, I want to pull up the same map inline as a panel in Multi-Incident Console when I don't need the full-screen console.

## Functional Requirements

### Core composition
1. UOP Map is single-site by default — a user's landing view is their home/selected site's live map. It introduces no new position, activity, or geofence storage; it is a real-time read composition over existing sources (see Integrations).
2. Four layer types compose on the map, each independently toggleable:
   - **Unit Positions** — from GIS & Mapping Services' Position Record (on-duty guards/vehicles), pin color driven by Status & State Monitors' Unit State (`available`, `dispatched`, `en_route`, `on_scene`, `completed`, `out_of_service`); GPS-denied entities render at their checkpoint-scan-derived proxy position with a "last seen at [checkpoint], [time]" indicator, per GIS's existing fallback.
   - **Activity Pins** — Active Incident Queue's `card`-role Activities with a resolvable location, styled by that doc's Queue Urgency Mapping urgency tier; unmapped types render `unclassified`, same graceful-degradation rule as the queue itself.
   - **Geofences** — static shapes from GIS & Mapping Services (polygon/radius), rendered as drawn boundaries.
   - **Overlay Layers** — GIS & Mapping Services' versioned KML/GeoJSON/shapefile toggle layers (current version only, by default).
   - **Camera Positions** *(retrofit, by Live Camera Feed Ingestion)* — fixed pins for every registered Camera Position; clicking one opens that Camera's feed via embed or deep-link, per that doc's `embed_mode` resolution — off by default in both day-one presets (#5), available to any role permitted to view that Location.
   - **Alarm Zones** *(retrofit, by Alarm Panel Monitors & Panic Alerts)* — fixed pins for every registered Alarm Zone, styled by `zone_category` and current Signal Disposition-consumed state (armed, disarmed, alarm, trouble/fault) — off by default in both day-one presets (#5), same viewing posture as Camera Positions.
   - **Lock Positions** *(retrofit, by Lock Core & Cylinder Tracking)* — fixed pins for every registered Lock Position, following the identical triad-pin-layer convention as Camera Positions/Alarm Zones — off by default in both day-one presets (#5), same viewing posture.
3. Only **current position** is shown — no trail/breadcrumb. Any movement history, trail, or time-scrub is explicitly out of scope here and belongs entirely to Historical Playback Console (Module 3, not yet specified), which queries GIS's Position Record history directly.
4. UOP Map is always available as a normal console — it is not gated behind an active EOC Activation (Multi-Incident Console's lightweight escalation trigger). EOC Activation remains a separate, heavier incident-command escalation signal, not a prerequisite for ordinary situational-awareness viewing.

### Role-scoped default presets
5. A **Map View Preset** (Settings & Preferences-registered Definition) declares a default on/off state per layer type, scoped by role — mirroring Active Incident Queue's Dispatcher Default vs. Supervisor/EOC Default split. Ships with the same two day-one presets: **Dispatcher Default** (Unit Positions + Activity Pins on; Overlays/Geofences available but off by default) and **Supervisor/EOC Default** (adds Geofences on, plus Route Assignment rollup cards per Active Incident Queue's existing rollup-card treatment). A user may always manually toggle any layer beyond their default; an admin-locked preset blocks narrower override, same lock discipline used throughout Settings & Preferences.

### Interactivity
6. Clicking any pin opens that entity's/Activity's own already-registered Command/Action Bus action menu and Detail surface — the same one available from Active Incident Queue or Multi-Incident Console's Detail panel — never a parallel map-specific action set. Dispatching a unit from the map reuses Unit Dispatch & Proximity Routing's existing confirm-gated suggestion flow unmodified: the map never auto-assigns on click.

### Clustering
7. Pins that fall within a zoom-dependent proximity threshold collapse into a count-bubble cluster, expanding on zoom-in or on click; clustering applies independently per layer type (Unit Position clusters never merge with Activity Pin clusters, preserving what kind of thing a cluster represents). Exact threshold values are a technical/UX-tuning parameter, not user- or tenant-configurable at launch (see Open Questions).

### Multi-site rollup
8. A separate **rollup mode** shows one **Site Rollup Tile** per site the viewer has access to (via ordinary site-scoped RBAC/ABAC access, or — for Contractor staff — their active Client Engagement's site scope): unit-state counts, open-Activity counts by urgency tier, and an active-alarm indicator, refreshed at the ≤30s enterprise-rollup freshness Real-Time Delivery already established (consistent with that doc's explicit rule that enterprise-wide live subscription is not a supported shape). Selecting a tile switches into that site's full single-site live UOP Map. Rollup mode is available only to viewers whose grant spans more than one site; a single-site user has no rollup entry point to show.

### Geofence highlight
9. Whether a live geofence entry/exit/dwell trigger visually flashes/highlights on the map is a Settings & Preferences-registered boolean, tenant-set default with per-user override — the underlying Notifications Engine alert (already owned by GIS & Mapping Services' geofence evaluation engine) fires regardless of this display setting.

### Console registration
10. UOP Map registers as the `map` panel type in Multi-Incident Console's existing Panel Registry (retrofit — see Integrations), so the identical live composition is available both as its own full-screen destination and as an inline dockable panel, with no divergence between the two.

## Data Model / Fields

UOP Map introduces no new Entity, Activity, or EntityAssociation type — it is a read composition. Feature-local records:

**Map View Preset** (Settings & Preferences registration)
- preset_id, tenant_id, role_scope (dispatcher, supervisor_eoc, custom)
- default_layers{} (unit_positions: bool, activity_pins: bool, geofences: bool, overlays: bool)
- locked (bool, admin-set)

**Geofence Highlight Preference** (Settings & Preferences registration)
- tenant default (bool), user-level override (bool, nullable — falls back to tenant default when unset)

**Site Rollup Tile** (read-model projection, not persisted — computed on demand at ≤30s freshness)
- site_ref, display_label, unit_state_counts{} (per Unit State value), open_activity_counts_by_urgency{} (per Queue Urgency Mapping tier), active_alarm_flag, last_updated_at

**Cluster Config** (technical parameter, not tenant-facing — see Open Questions)
- per-layer zoom-to-distance clustering thresholds

## States & Transitions

UOP Map itself has no record lifecycle — it is a live console, not a governed record. Map View Preset follows Settings & Preferences' existing `active`/`archived` posture for any Definition. Site Rollup Tile is a live projection with no state of its own.

## Integrations

- **GIS & Mapping Services**: source of Position Record (unit positions, proxy fallback), Geofence (shapes, trigger events), Overlay Layer, and the map provider adaptor itself — this doc composes and renders, never re-implements.
- **Real-Time Delivery & Server-Side Timers**: UOP Map subscribes to the site-scoped Live Update Channel for position deltas, Activity/queue deltas, geofence trigger events, and alarm state, inheriting its disconnect contract (freeze + banner + age counter + polling fallback, actions stay enabled) and its ≤2s server-to-console latency target for safety-relevant updates; rollup tiles use the same doc's ≤30s enterprise-aggregation posture, never a live per-site subscription across sites at once.
- **Active Incident Queue (CAD Console)**: source of Activity Pins (Card-role Activities) and Queue Urgency Mapping for pin styling — resolves that doc's own flagged forward reference ("a natural downstream consumer of this same live queue for map-pin rendering").
- **Status & State Monitors**: source of Unit State for unit-pin coloring — resolves GIS & Mapping Services' own flagged forward reference for map-pin coloring by status.
- **Unit Dispatch & Proximity Routing**: a pin-click dispatch action reuses its existing confirm-gated suggestion flow unmodified.
- **Multi-Incident Console**: registers this doc's view as the `map` Panel Registry entry (retrofit — see below); Route Assignment rollup-card treatment reused as-is in the Supervisor/EOC preset.
- **Command/Action Bus**: every pin-click action is an existing registered action; this doc registers no new action types.
- **Settings & Preferences**: owns Map View Preset, Geofence Highlight Preference.
- **Authentication & Authorization**: RBAC/ABAC scoping bounds what any viewer's map query can ever return; UOP Map Viewer permission gates the console itself (see Permissions).
- **Tenant Management (Client Engagement)**: resolves rollup-mode site scope for Contractor staff to their active Engagement's site set, never beyond it.
- **Historical Playback Console (Module 3, not yet specified)**: explicit boundary — all trail/breadcrumb/time-scrub display belongs there; this doc is strictly current-state.
- **Module 9 (Location Hierarchy Designer, zones) and Module 7 (hazard-by-location) slices, not yet specified**: forward reference only — when built, hazard and zone-hierarchy content register as additional Overlay Layer content consumed here with no change to this doc.
- **Live Camera Feed Ingestion** *(retrofit)*: source of the Camera Positions layer (#2); a pin click opens that doc's feed viewer, embed or deep-link per its own `embed_mode` resolution — no camera-specific rendering logic introduced here.
- **Alarm Panel Monitors & Panic Alerts** *(retrofit)*: source of the Alarm Zones layer (#2); zone state comes from Activity Registry's Signal Disposition, not a new mechanism introduced here.

## Permissions

- **UOP Map Viewer** (new, site-scoped RBAC + ABAC overlay): required to open the console, in either the dedicated destination or the Multi-Incident Console panel form — same layered-gate discipline as Entity Relationships & History's Interaction Timeline Viewer: this permission gates the console itself, never bypasses each individual pin's own existing permission/ABAC filtering underneath (a viewer with UOP Map Viewer but no access to a specific sensitive Activity or Location still won't see that pin).
- Taking an action from a pin (dispatch, view detail, etc.) requires whatever permission the invoked Command/Action Bus action itself already requires — no separate "act from map" permission.
- Managing Map View Presets and the Geofence Highlight Preference's tenant default requires Site/Tenant Admin, same as any other Settings & Preferences-registered Definition.
- Rollup-mode site access is bounded by the viewer's existing multi-site RBAC/ABAC grant (or Client Engagement site scope) — no additional rollup-specific permission.

## Non-Functional / Constraints

- Latency and scale inherit Real-Time Delivery & Server-Side Timers' established baselines wholesale: ≤2s server-to-console for safety-relevant deltas, disconnect/resync contract identical to that doc's console pattern, and the same site-as-design-unit NFR tiers (up to the ~300-officer/~10-console large-campus ceiling, 50 promoted activities/min sustained, 500 signals/min telemetry bursts absorbed upstream by Activity Registry's Signal Disposition valve) — UOP Map must remain legible at that ceiling via clustering, not a separate scale target.
- Rollup Site Tiles refresh at ≤30s, consistent with the platform's enterprise-aggregation posture; no rollup view ever attempts a live per-pin subscription spanning multiple sites at once.
- UOP Map is inherently online-only — no offline capture concept applies to a live console view.
- Raw position pings remain high-volume and not individually audit-logged (existing GIS posture); UOP Map introduces no new audit-tier event of its own.

## Acceptance Criteria

- [ ] A Dispatcher opening UOP Map sees their home site's on-duty units (colored by current Unit State) and active Call/Incident/Dispatch pins (styled by urgency tier) without manual configuration, per the Dispatcher Default preset.
- [ ] A Supervisor's default view additionally shows Geofences and Route Assignment rollup cards, per the Supervisor/EOC Default preset.
- [ ] Toggling a layer off/on updates the map immediately without a page reload; an admin-locked preset blocks a user from disabling a locked layer.
- [ ] A GPS-denied unit displays at its last scanned checkpoint with a "last seen at" indicator rather than a stale or missing pin.
- [ ] Clicking an Activity pin opens that Activity's existing action menu/Detail surface; invoking Dispatch from it requires the same confirmation gate as invoking it from Unit Dispatch's own interface.
- [ ] A cluster of nearby pins collapses at zoom-out and expands on zoom-in or click, without merging Unit Position and Activity Pin clusters together.
- [ ] Opening rollup mode as a multi-site-scoped viewer shows one tile per accessible site with unit/activity counts and an alarm indicator refreshed within 30 seconds; selecting a tile opens that site's full live map. A single-site-scoped viewer sees no rollup entry point.
- [ ] A triggered geofence visually highlights on the map only when the resolved (user-then-tenant) Geofence Highlight Preference is on; the Notifications Engine alert fires either way.
- [ ] The same live view opens identically whether launched as the dedicated UOP Map destination or as a `map` panel inside Multi-Incident Console.
- [ ] A user without UOP Map Viewer permission cannot open the console at all; a user with it but without access to a specific sensitive Activity does not see that Activity's pin.
- [ ] A console disconnect freezes the display with an aging staleness banner while pin-click actions remain invocable; reconnect triggers snapshot resync and clears the banner.

## Open Questions

- Exact per-layer clustering distance/zoom thresholds — a technical-spec/UX-tuning concern; whether it should ever become tenant-configurable is left open, current default is platform-fixed.
- Precise site-scope resolution query when a viewer holds a mix of ordinary multi-site Tenant Role Grants and Client Engagement-scoped grants simultaneously — the rule (union of both, Engagement bounded to its own sites) is stated in Integrations but the exact resolution mechanics are an implementation detail, not fully specified here.
- Whether Overlay Layer content needs a new "operational" vs. "reference" categorization once Module 9/7 slices land (e.g., a hazard overlay a Dispatcher might want on by default vs. a rarely-used reference layer) — flagged for revisit when those modules are specified, not decided here.
- Whether a future large-enterprise deployment ever needs a rollup tier above "site" (e.g., a regional grouping) — no such need identified by any of the three MVP target customers; not speculated further here.
