# Live Camera Feed Ingestion

## Overview

Per the platform's strategic integrate-don't-replace boundary, this doc never speaks VMS protocols directly — no RTSP/WebRTC ingestion infrastructure, no camera-management system of its own. It is the Command Center's *consumption* side: embedding or deep-linking whatever a configured VMS adaptor exposes into UOP Map, Command Center Wallboard View, and Multi-Incident Console.

Camera itself reuses Guard Tour & Checkpoint Verification's established **triad pattern** — "a place with an attached, swappable physical asset," explicitly flagged there as the template for exactly this kind of concept (its own example: "an alarm sensor mount"). **Camera Position** (a Location extension — the fixed mounting point/coverage area) and **Camera** (an Item extension — the physical hardware unit, full identity/dedup/custody/audit treatment, same as Vehicle) are linked by a single-current-value **Camera Mount Association**, mirroring Checkpoint's own tag-attachment mechanism exactly: a position's identity and incident/alarm history survive hardware replacement.

The real VMS device registry and protocol adaptor are explicitly Module 19's future **VMS Camera Stream Ingestion**, not yet specified. Camera's VMS-reference fields here (external camera ID, adaptor reference, embed capability) are a deliberately interim, Admin-entered stand-in — the same deferred-integration posture DAR's Shift Window used standing in for Post Schedule Builder — flagged for reconciliation once that module exists.

## Actors & Roles

- **Site / Tenant Admin** — registers Camera Items and Camera Positions, mounts/replaces hardware, configures each Camera's VMS adaptor reference and Auto-Popup Mappings.
- **Dispatcher / Supervisor / EOC Coordinator** — views camera feeds from UOP Map pins, Wallboard panels, or Multi-Incident Console; issues PTZ commands where available.
- **VMS adaptor** (external system) — the actual system a Camera's feed/control commands pass through to; never spoken to directly by protocol, only via its own adaptor.

## User Stories

- As a **Dispatcher**, I want to click a camera icon on the UOP Map and see its live feed — embedded if the VMS supports it, deep-linked if not — without leaving the console.
- As an **EOC Coordinator**, I want an alarm or geofence trip near a camera to automatically surface that camera's feed on the relevant console/wallboard, without hunting for which camera covers that area.
- As a **Tenant Admin**, I want to explicitly pin specific cameras to specific alarm/signal types when the default location-based match isn't precise enough — a zone covered by five cameras shouldn't pop all five for a minor sensor.
- As a **Supervisor**, I want to pan/tilt/zoom a camera during an active incident if our VMS supports it, with a confirmation step since it physically moves hardware everyone else relies on too.
- As a **Tenant Admin**, I want a camera's hardware replaced without losing the mounting position's history of what's been seen/alarmed there before.
- As a **Tenant Admin**, I want Camera treated as a real trackable asset (dedup, custody, audit) since it's inventory we own and maintain, not just a pointer into someone else's system.

## Functional Requirements

### Camera Position & Camera (triad pattern)
1. **Camera Position** registers as a Location extension — the fixed place or coverage area a camera is mounted at or aimed toward, following Checkpoint's triad precedent exactly.
2. **Camera** registers as an Item extension, inheriting Item Registry's base identity/dedup/custody/audit treatment (make/model/color/photo, current holder + transfer history) unmodified — same treatment as Vehicle, per the user's explicit direction that a camera is a genuine trackable physical asset, not merely a reference into an external system.
3. A **Camera Mount Association** (single-current-value EntityAssociation, Camera Position ↔ Camera) links the two, mirroring Checkpoint's tag-attachment association exactly: replacing a camera's hardware closes the prior Mount Association and opens a new one — the Position's identity and every alarm/incident history tied to it survive unaffected.
4. Camera carries VMS-reference fields (`external_vms_camera_id`, `vms_adaptor_ref`, `embed_mode`) as a deliberately interim, Admin-entered stand-in for Module 19's future VMS Camera Stream Ingestion — flagged for reconciliation once that module exists, not built here.

### Feed display
5. `embed_mode` — resolved from a Camera's `vms_adaptor_ref` capability declaration — is either **`embed`** (an iframe/native-player widget rendered directly in the Command Center surface) or **`deep_link`** (opens the VMS's own console in a new tab/window). This is the platform's established provider-adaptor pattern (GIS, AI, Notifications) applied here: never assume uniform capability across VMS vendors.
6. A new **`camera`** panel type registers into the shared Panel Registry (the catalog Multi-Incident Console, Command Center Wallboard View, and ICS Role Mapping already contribute to) — selectable in a personal Console Layout, a Wallboard Display Profile zone, or opened directly from a UOP Map pin popover.
7. UOP Map *(retrofit)* gains Camera Position as an optional pin layer — clicking a camera pin opens its feed via #5, either inline or in the `camera` panel.

### PTZ control
8. Where a Camera's `vms_adaptor_ref` declares PTZ capability, a **Move Camera** action registers on the Command/Action Bus — a confirmation-gated pass-through invocation of the VMS's own PTZ API, given the real physical consequence of moving hardware other consoles rely on too, never a Sentinel-Suite-native protocol implementation (same boundary precedent as "remote gate controls" elsewhere in the platform). Where the adaptor doesn't declare PTZ, no such action exists at all — a Dispatcher deep-links to the VMS's own control UI instead.

### Auto-popup
9. A triggering event — a Signal Disposition-promoted Activity, a Geofence trigger, or a Duration Watchdog alarm — resolves which Camera(s) to auto-surface using the platform's established explicit-beats-default resolution chain (the same precedence already used for Command/Action Bus parameters): an explicit tenant-configured **Camera Auto-Popup Mapping** (trigger type/value → specific Camera(s)) wins when one exists; otherwise every Camera Position sharing that event's Location (or an ancestor/zone) is suggested by default.
10. A matched Camera's feed auto-surfaces on every subscribed console/wallboard via the existing Live Update Channel — no new push infrastructure.

## Data Model / Fields

**Camera Position** (Location extension; entity_id is the shared PK, FK → Location)
- coverage_note (optional free text — what the camera actually sees)

**Camera** (Item extension; entity_id is the shared PK, FK → Item)
- external_vms_camera_id, vms_adaptor_ref, embed_mode (embed, deep_link — resolved from adaptor, admin-overridable)
- ptz_capable (bool, resolved from adaptor)

**Camera Mount Association** (EntityAssociation — entity_id_a = Camera Position, entity_id_b = Camera; association_id is the shared PK)
- mounted_at, removed_at (nullable — null means current mount)

**Camera Auto-Popup Mapping** (local tenant Definition)
- mapping_id, tenant_id, trigger_type (signal_disposition_activity, geofence_event, duration_watchdog_alarm), trigger_value_ref, camera_refs[]

## States & Transitions

- **Camera Position:** follows base Location lifecycle, unmodified.
- **Camera:** follows base Item lifecycle (active/tombstoned via Entity Registry Core), custody/current-holder inherited unmodified from Item Registry.
- **Camera Mount Association:** `active` → `removed` on hardware swap — identical shape to Checkpoint's own tag association.

## Integrations

- **Location Registry**: Camera Position's base geometry/coordinates, reused wholesale.
- **Item Registry**: Camera's base identity/dedup/custody/audit treatment, reused wholesale — no special-casing versus Vehicle.
- **Entity Registry Core**: Camera Mount Association's base EntityAssociation shape, following Checkpoint's precedent exactly.
- **GIS & Mapping Services / Unified Operational Picture (UOP) Map**: Camera Position renders as an optional map pin layer (retrofit); Geofence trigger events are one Auto-Popup source.
- **Activity Registry (Signal Disposition)**: a promoted Activity is a second Auto-Popup trigger source.
- **Active Call Alerts & Timers**: a Duration Watchdog alarm is a third Auto-Popup trigger source — no new alerting mechanism.
- **Multi-Incident Console / Command Center Wallboard View**: `camera` panel type contributed to the shared Panel Registry.
- **Command/Action Bus**: Move Camera (PTZ pass-through) registers as a confirmation-gated action.
- **Real-Time Delivery & Server-Side Timers**: the Live Update Channel delivers auto-popup surfacing to every subscribed console.
- **Module 19 — VMS Camera Stream Ingestion (not yet specified)**: forward reference — the eventual real device registry/protocol adaptor; this doc's Camera VMS-reference fields are an explicit interim stand-in, flagged for reconciliation once that module is specified, same posture as DAR's Shift Window pending Post Schedule Builder.

## Permissions

- **Register/mount/replace a Camera or Camera Position**: Site/Tenant Admin.
- **Configure Camera Auto-Popup Mapping**: Site/Tenant Admin.
- **View a camera feed**: inherits the Camera Position's own Location-based RBAC/ABAC — no new permission introduced.
- **Issue a Move Camera (PTZ) command**: Supervisor/EOC Coordinator, confirmation-gated.

## Non-Functional / Constraints

- No RTSP/WebRTC ingestion infrastructure is built by the platform — feed rendering is strictly embed-or-deep-link per adaptor declaration.
- PTZ pass-through commands inherit the platform's standard confirmation-gate UX, no separate latency target defined here.
- This doc introduces no new streaming/media infrastructure of its own; it is a composition layer, matching UOP Map's own "no new mapping mechanism" precedent.

## Acceptance Criteria

- [ ] Clicking a camera pin on UOP Map opens its feed via embed or deep-link, matching that Camera's resolved `embed_mode`.
- [ ] Replacing a Camera's physical hardware preserves the Camera Position's identity and its full incident/alarm history; the old Camera's Mount Association closes and a new one opens for the replacement unit.
- [ ] An explicit Camera Auto-Popup Mapping for a trigger type/value wins over the default location-based suggestion.
- [ ] A geofence trip with no explicit mapping surfaces every Camera whose Camera Position shares that geofence's Location.
- [ ] Move Camera is unavailable — not just disabled, absent — for a Camera whose adaptor declares no PTZ capability.
- [ ] Move Camera requires passing the confirmation gate before the command is sent to the VMS adaptor.
- [ ] A Camera's dedup/merge/custody behavior is identical to Vehicle's, inherited from Item Registry with zero special-casing.
- [ ] The `camera` panel type is selectable in Multi-Incident Console's Console Layout and Command Center Wallboard View's Display Profile identically, both drawing from the same shared Panel Registry catalog.

## Open Questions

- Exact VMS adaptor capability-declaration schema (what fields Module 19 will eventually define for embed support, PTZ support, stream URLs) — forward reference only, not committed here.
- Whether Camera Position needs its own coverage-area geometry (a cone/polygon) distinct from a point, for finer auto-popup precision — flagged, not resolved; current default is Location-level matching only.
- Whether PTZ preset positions (saved views) are worth modeling now or deferred entirely to the VMS's own UI — not committed here.
