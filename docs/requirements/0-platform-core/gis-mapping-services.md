# GIS & Mapping Services

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

GIS & Mapping Services is the foundational mapping infrastructure consumed by every map-based screen across the platform (Unified Operational Picture, Zone Mapping, Route Assessment, Perimeter GIS Designer, and others). It owns tile rendering/serving via a tenant-selectable provider adaptor, offline vector tile caching, live GPS position ingestion and history storage, the geofence data model and real-time evaluation engine, and custom overlay (KML/GeoJSON/ESRI shapefile) import and layer management. It resolves the PDD's open "GIS/mapping provider selection" question with a provider-adaptor architecture rather than a single hardcoded vendor.

Module-specific map screens elsewhere in the platform (Command Center's UOP Map, Facility & Zone Management's Zone Mapping, Executive Protection's Route Assessment, Special Event's Perimeter GIS Designer, etc.) are consumers: they compose this feature's primitives (tiles, geofences, overlays, position feed) into their own purpose-built UI and business logic. None of them re-implement tile serving, geofence math, or position storage.

## Actors & Roles

- **Platform Super Admin** — manages available map provider adaptors platform-wide.
- **Tenant Admin** — selects/configures the tenant's map provider (e.g., Esri for a government tenant, default open-source stack or Google for a commercial tenant), sets GPS retention policy within platform limits.
- **Site/Facility Admin** — uploads and manages custom overlays, authors geofences (via consuming features like Zone Mapping).
- **Guard/Officer (mobile)** — on-duty GPS position source; consumes offline-cached tiles.
- **Dispatcher / Supervisor / EOC staff** — consumers of live position plotting and geofence alerts via downstream features (Dispatch/CAD, Command Center).

## User Stories

- As a **Tenant Admin at a DOE facility**, I want to configure our tenant to use an Esri-based map provider we already have a license for, so we align with our existing GIS investment.
- As a **commercial Tenant Admin**, I want a sensible default map provider with no extra setup, so I don't have to make a GIS vendor decision to get started.
- As a **Guard**, I want the map tiles for my assigned patrol route to already be cached on my device before I head underground, so I'm not staring at a blank map with no signal.
- As a **Dispatcher**, I want to see a guard's live position while they're on shift, and nothing once they clock out, so tracking doesn't extend into their personal time.
- As a **Guard working an underground checkpoint route**, I want the map to show my last scanned checkpoint as my position when GPS is unavailable, so my supervisor still has a coarse sense of where I am.
- As a **Site Admin**, I want to draw a polygon geofence around a restricted zone and get an alert if anyone enters it after hours, so unauthorized access is caught immediately.
- As a **Facility Admin**, I want to upload a utility shapefile showing gas/water shutoffs and manage it as a toggleable overlay layer, so responders can turn it on only when needed.
- As an **EOC analyst**, I want to play back a vehicle's historical GPS track from an incident window, so I can reconstruct exactly where units were.

## Functional Requirements

### Map provider
1. Map rendering/tile serving is implemented via a **provider adaptor architecture**: the platform supports multiple backing providers (e.g., a default self-hostable open-source vector tile stack, Esri, Google, or other commercial providers) behind a common internal interface.
2. Tenant Admins select their tenant's active provider from the set of adaptors enabled for their deployment; DOE/air-gapped tenants are restricted to self-hostable adaptors with no external network calls.
3. Switching providers at the adaptor layer does not require changes to consuming features — they render against the common interface, not a specific vendor's API.

### GPS tracking
4. Live location tracking for a guard/officer is active only while they are on-duty (clocked in / active shift); no tracking occurs off-duty.
5. This feature is the system of record for GPS position history (the raw track log per entity — guard, vehicle), retained per a tenant-configurable retention policy.
6. The platform enforces a maximum default retention that tenants can only tighten (shorten), not loosen, absent explicit documented justification — balancing operational playback value against labor-privacy exposure.
7. Historical Playback Console (Module 3), Performance/KPI heatmap features (Module 12), and similar downstream features query this feature's position history rather than maintaining their own store.

### Underground / GPS-denied positioning
8. Where live GPS is unavailable, the system falls back to a **checkpoint-scan-derived proxy position**: the entity's displayed location becomes their last scanned checkpoint (from Guard Tour) with a "last seen at [checkpoint], [time]" indicator, rather than showing a stale or misleading GPS dot.

### Offline maps
9. Vector tiles are automatically pre-cached to a device based on the user's current assignment (post, patrol route, site) — no manual download step required for the common case — refreshed opportunistically when the device has good connectivity.
10. Cached tile data respects the device-level encryption-at-rest and retention-window mechanics defined in Offline Data Sync.

### Geofencing
11. Geofences support both arbitrary **polygon** shapes (drawn boundaries) and simple **radius-around-point** circles.
12. The evaluation engine supports three trigger types: **entry** (crossing in), **exit** (crossing out), and **dwell** (present longer than a configured duration).
13. This feature owns the geofence data model and real-time evaluation engine (given a shape + trigger type, it fires entry/exit/dwell events); consuming features (Zone Mapping, Perimeter GIS Designer, Route Assessment, BOLO vehicle-zone alerts, etc.) are authoring UIs for their specific purpose, all persisted through this shared model.
14. Geofence trigger events are routed through the Notifications Engine using each consuming feature's own notification category configuration (this feature fires the event; consuming features define what notification it produces).
14a. A geofence may optionally derive its geometry directly from a Location Registry entity's own footprint (`derived_from_location_ref`) rather than requiring an independently-drawn duplicate shape, when the desired alerting boundary coincides exactly with a known location's extent (e.g., "alert on entry to Building A" reusing Building A's own recorded polygon). Geofences without a natural 1:1 location correspondence (e.g., a patrol zone spanning several locations) continue to define independent geometry.

### Custom overlays
15. Site/Facility Admins can upload KML, GeoJSON, or ESRI shapefile files as custom overlays.
16. Uploaded files are validated for format/geometry correctness on import and converted to the platform's internal representation.
17. Overlays are managed as named, versioned layers (e.g., "Utility Lines," "Evacuation Routes") that can be toggled on/off per map view without requiring re-upload for minor view changes; version history is retained.

## Data Model / Fields

**Map Provider Config** (per tenant)
- tenant_id, active_adaptor (identifier), adaptor_credentials (encrypted, if applicable), fallback_adaptor (nullable, for degraded-connectivity self-hosted fallback)

**Position Record**
- position_id, tenant_id, entity_type (guard, vehicle), entity_ref
- source (gps, checkpoint_proxy), coordinates (or checkpoint_ref if proxy), accuracy, timestamp
- shift_ref (links to active on-duty session, null/absent when off-duty — tracking simply doesn't occur)

**Geofence**
- geofence_id, tenant_id, name, owning_feature (Zone Mapping, Route Assessment, etc.)
- shape_type (polygon, radius), geometry (nullable if derived_from_location_ref is set)
- derived_from_location_ref (nullable — a Location Registry entity_id; when set, geometry is read directly from that location's own footprint rather than independently drawn/duplicated)
- trigger_types[] (entry, exit, dwell), dwell_threshold (if applicable)
- active (bool), created_by, created_at

**Geofence Event**
- event_id, geofence_id, entity_ref, trigger_type, timestamp

**Overlay Layer**
- layer_id, tenant_id, name, source_format (kml, geojson, shapefile, live_feed)
- current_version, version_history[] (version, uploaded_by, uploaded_at, file_ref) — n/a for `live_feed`, see states below
- default_visibility (bool)
- feed_adaptor_ref (nullable, required when source_format = live_feed) *(retrofit, by Environmental & Weather Map Overlays — a `live_feed` layer is adaptor-driven and always-current: weather radar, lightning, traffic congestion. No versioning applies since there's nothing to snapshot, unlike an uploaded file.)*

**Offline Tile Cache Manifest** (device-local)
- device_id, cached_regions[] (site/zone/route refs), last_refreshed_at

**GPS Retention Policy**
- tenant_id, retention_period (≤ platform max default)

## States & Transitions

**Position tracking session:** `off-duty` (no tracking) → `on-duty-active` (tracking live) → `off-duty` (clock-out, tracking stops immediately). No intermediate paused state — tracking is binary, gated by shift status.

**Geofence:** `draft` → `active` → `inactive` (disabled without deletion, preserves history) → `deleted` (soft-delete, event history retained).

**Overlay Layer:** `uploading` → `validating` → `active` (current version) | `rejected` (validation failure, with reason surfaced to uploader). New uploads to an existing layer create a new version; prior versions remain in history but are not rendered by default. *(Retrofit — `live_feed` layers skip upload/validation entirely: `active`/`inactive` only, toggled the moment a feed adaptor is configured.)*

## Integrations

- **Offline Data Sync**: offline tile caching and position-tracking data queuing follow that feature's local storage, encryption, and retention mechanics.
- **Notifications Engine**: geofence trigger events feed into consuming features' notification categories.
- **Guard Tour & Checkpoint Verification**: source of checkpoint-scan data used for GPS-denied proxy positioning.
- **Structured Logging & Audit Trails**: geofence authoring changes, overlay uploads, and provider configuration changes are audit-tier events; raw position pings are high-volume and are not individually audit-logged (only access/export of position history is).
- **Settings & Preferences**: Network Profiles influence tile pre-caching bandwidth behavior; the Map Provider Config and GPS Retention Policy in this doc's data model are registered as Setting Definitions against that feature's shared hierarchical config engine rather than implemented as standalone override mechanisms.
- **Command Center (UOP Map, Historical Playback)**, **Facility & Zone Management (Zone Mapping)**, **Executive Protection (Route Assessment)**, **Special Event (Perimeter GIS Designer)**, **Access Control (BOLO vehicle-zone alerts)**: consumers of this feature's tile rendering, geofencing, position feed, and overlay primitives.
- **Location Registry**: source of location geometry a Geofence may directly reuse via `derived_from_location_ref` instead of an independently-drawn duplicate shape.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site/Facility Admin | Guard/Officer | Dispatcher/EOC |
|---|---|---|---|---|---|
| Enable/configure available provider adaptors | ✅ | ✅ (own tenant, select from enabled set) | ❌ | ❌ | ❌ |
| Set GPS retention policy | ✅ (max default) | ✅ (own tenant, ≤ max) | ❌ | ❌ | ❌ |
| Author/edit geofences | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ (via consuming feature roles) |
| Upload/manage custom overlays | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ |
| View live position feed | ✅ | ✅ | ✅ (own scope) | ✅ (own) | ✅ (assigned scope) |
| View/export historical position data | ✅ | ✅ (own tenant) | ✅ (own scope, if granted) | ✅ (own) | ✅ (if granted) |

## Non-Functional / Constraints

- DOE/air-gapped tenants must be able to operate the full mapping stack (tile serving, geofence evaluation, overlay rendering) with zero external network dependency when using a self-hostable adaptor.
- GPS tracking must hard-stop at clock-out — no tracking data generated or stored for off-duty periods, addressed as a labor-privacy compliance requirement, not just a UX nicety.
- Position ingestion must handle high write volume (many active guards reporting frequently) without degrading live-map responsiveness.
- Offline tile caching must respect device storage limits — auto-cache scope (post/route/site) should be bounded, not unbounded regional pre-fetch.
- WCAG 2.1 / Section 508 accessible map interactions (keyboard navigation, non-color-only status indicators) — acknowledging that full map accessibility has inherent limits, addressed via accessible data-table alternatives to raw map views where feasible.
- Overlay import validation must reject malformed/oversized files gracefully with a clear error, not silently fail or crash rendering.

## Acceptance Criteria

- [ ] A DOE tenant can configure and use a self-hosted map provider adaptor with no external network calls; a commercial tenant can select a different adaptor (e.g., Google or the default stack) independently.
- [ ] A guard's live position appears on relevant dispatch/EOC views only while clocked in, and disappears (no further updates) immediately upon clock-out.
- [ ] Historical Playback Console can query and replay a stored position track for a given entity and time window without maintaining its own position store.
- [ ] When a guard enters a zone with no GPS signal, their displayed position falls back to their last scanned checkpoint with a clear "last seen" indicator.
- [ ] A guard's assigned patrol route tiles are cached on their device automatically before they lose connectivity, without a manual download action.
- [ ] A polygon geofence with an entry trigger fires a geofence event when a tracked entity crosses into it; a dwell-trigger geofence fires only after the configured duration elapses.
- [ ] An uploaded GeoJSON overlay validates, renders as a toggleable layer, and a subsequent re-upload creates a new version while the prior version remains in history.
- [ ] A malformed shapefile upload is rejected with a clear validation error, not a silent failure.
- [ ] GPS position history older than the tenant's configured retention period is no longer retrievable.

## Open Questions

- Specific list of provider adaptors to build at launch (beyond an illustrative default open-source stack, Esri, and Google) — to be prioritized during technical spec against actual early customer requirements.
- Platform-enforced maximum GPS retention default value — needs a specific decision informed by labor/privacy legal review during technical spec.
- Exact auto-cache scope sizing (e.g., current post + route only, vs. current post + adjacent zones) — to be tuned during technical spec against real device storage/bandwidth constraints.
- Whether indoor/underground supplementary positioning hardware (BLE beacons, Wi-Fi RTT) becomes a future roadmap item beyond checkpoint-proxy positioning — deferred, checkpoint-proxy is the day-one answer.
