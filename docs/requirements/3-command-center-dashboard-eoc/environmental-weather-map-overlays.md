# Environmental & Weather Map Overlays

## Overview

This doc composes three related capabilities onto UOP Map and the platform's existing telemetry substrate: (1) live external condition overlays — weather radar, lightning strikes, traffic congestion — via a retrofit to GIS & Mapping Services' Overlay Layer, adding a `live_feed` source type alongside its existing static uploads; (2) on-site **Environmental Sensor** readings (temperature, humidity, weather stations), following the same triad pattern Camera and Alarm Panel already established (a Location-extension position + an Item-extension physical unit, full custody/dedup/audit); (3) **Severe Weather Alert** ingestion, routed through Notifications Engine's existing alert mechanism with an optional one-tap Activate EOC Response suggestion at a tenant-configured severity threshold.

Per explicit user motivation — correlating environmental conditions with alarm telemetry to speed root-cause diagnosis (e.g., heat triggering an HVAC fan that jostles papers and false-trips a motion sensor) — this doc ensures Environmental Sensor readings are captured with the same Location-tagged, timestamped telemetry fidelity Activity Registry's Signal Disposition valve already established for alarm signals, making that correlation *possible*. Per explicit user direction, the actual correlation **view**/analysis is out of scope here — it belongs with Module 19's future **Alarm Pattern & Nuisance Analysis** (recurrence detection over telemetry, the pattern-to-report bridge already flagged there), which this doc's telemetry capture is deliberately shaped to feed.

## Actors & Roles

- **Dispatcher / Supervisor / EOC Coordinator** — views weather/lightning/traffic overlays and environmental sensor readings on UOP Map or a Wallboard.
- **Site / Tenant Admin** — configures the tenant's weather/traffic adaptor, registers Environmental Sensors, sets Severe Weather Alert thresholds.
- **Weather / Traffic adaptor** (external system) — the actual vendor feed, never spoken to by native protocol.

## User Stories

- As a **Dispatcher**, I want live weather radar and lightning strikes overlaid directly on the map I'm already watching, without switching to a separate weather app.
- As a **Tenant Admin**, I want to register an on-site temperature sensor the same way I'd register a camera, since it's real hardware we own and maintain.
- As an **Analyst** investigating a recurring nuisance alarm, I want the environmental telemetry (temperature, weather conditions) from that time captured and available, even if the actual pattern-matching happens in a future feature.
- As a **Supervisor**, I want a severe weather alert (tornado warning) to prompt me with a one-tap option to activate EOC Response, not force it automatically.
- As a **Dispatcher**, I want traffic congestion/road closures visible on the map during an evacuation, the same live-overlay treatment as weather.

## Functional Requirements

### Live overlay retrofit
1. GIS & Mapping Services' Overlay Layer *(retrofit)* gains a **`live_feed`** source type alongside its existing `kml`/`geojson`/`shapefile` — adaptor-driven, always-current, no versioning (there's nothing to snapshot, unlike an uploaded file). Weather radar, lightning strikes, and traffic congestion each register as a `live_feed` Overlay Layer instance, sourced from a tenant-configured weather/traffic adaptor — the same provider-adaptor pattern as every other external dependency on the platform.
2. A `live_feed` Overlay Layer toggles on/off exactly like any other Overlay Layer, on UOP Map or a Wallboard Display Profile zone — no new rendering mechanism; this doc introduces zero new map infrastructure.

### Environmental Sensor (triad pattern)
3. **Environmental Sensor Position** registers as a Location extension — the fixed place a sensor is mounted, following the same triad precedent Camera and Alarm Panel already established.
4. **Environmental Sensor** registers as an Item extension, inheriting Item Registry's base identity/dedup/custody/audit treatment unmodified — the third confirmed instance of this pattern, applied uniformly regardless of unit cost.
5. An **Environmental Sensor Mount Association** (single-current-value EntityAssociation, Position ↔ Sensor) links the two, mirroring Camera's 1:1 mount exactly (unlike Alarm Panel's one-to-many adaptation — one sensor unit occupies one position at a time).
6. Every Environmental Sensor reading (temperature, humidity, or whatever the unit reports) flows through Activity Registry's existing Signal Disposition **telemetry** tier, Location-tagged via its Sensor Position and timestamped — no new telemetry pipeline. This fidelity is deliberately what makes later root-cause correlation possible (see Overview); this doc captures the data, it does not build the correlation view itself.

### Severe Weather Alert
7. A **Severe Weather Alert**, ingested from the tenant's configured weather adaptor, routes through Notifications Engine's existing alert/category mechanism — no new delivery infrastructure.
8. At a tenant-configured severity threshold, a Severe Weather Alert additionally surfaces a one-tap **Activate EOC Response** suggestion, reusing Multi-Incident Console's existing action and confirmation gate unmodified — weather severity never bypasses that gate, it only offers the action more prominently.

## Data Model / Fields

**Environmental Sensor Position** (Location extension; entity_id is the shared PK, FK → Location)
- *(no additional fields beyond base Location)*

**Environmental Sensor** (Item extension; entity_id is the shared PK, FK → Item)
- external_sensor_id, sensor_adaptor_ref, reading_types[] (e.g., temperature, humidity)

**Environmental Sensor Mount Association** (EntityAssociation — entity_id_a = Sensor Position, entity_id_b = Sensor; association_id is the shared PK)
- mounted_at, removed_at (nullable — null means current mount)

**Overlay Layer** *(retrofit — additive)*
- `source_format` gains `live_feed` alongside `kml`/`geojson`/`shapefile`
- feed_adaptor_ref (nullable, required when `source_format = live_feed`)

**Severe Weather Alert Threshold** (tenant Settings & Preferences registration)
- tenant_id, severity_level, eoc_suggestion_enabled (bool)

## States & Transitions

- **Environmental Sensor Position:** follows base Location lifecycle, unmodified.
- **Environmental Sensor:** follows base Item lifecycle, custody inherited unmodified.
- **Environmental Sensor Mount Association:** `active` → `removed`, same shape as Camera Mount Association.
- **Overlay Layer (`live_feed` instances):** `active`/`inactive` only — no versioning/validation pipeline, since there's no uploaded file to validate.

## Integrations

- **GIS & Mapping Services**: Overlay Layer retrofit (`live_feed` source type); overlays render through that doc's existing map/toggle UI unmodified.
- **Location Registry / Item Registry / Entity Registry Core**: Environmental Sensor Position/Sensor/Mount Association triad, reused wholesale — the third instance of this pattern after Camera and Alarm Panel.
- **Activity Registry (Signal Disposition)**: Environmental Sensor readings flow through the existing telemetry tier — no new pipeline.
- **Unified Operational Picture (UOP) Map / Command Center Wallboard View**: `live_feed` Overlay Layers and Environmental Sensor readings render through those docs' existing layer/panel mechanisms.
- **Notifications Engine**: Severe Weather Alert's delivery mechanism, reused unmodified.
- **Multi-Incident Console**: Activate EOC Response is reused unmodified as the severe-weather suggestion's target action — no new gating logic.
- **Module 19 — Alarm Pattern & Nuisance Analysis (not yet specified)**: forward reference — this doc's Environmental Sensor telemetry is explicitly captured to feed that future feature's root-cause correlation/pattern detection; no correlation view is built here, per explicit user direction.

## Permissions

- **Configure the tenant's weather/traffic adaptor**: Tenant Admin.
- **Register/mount/replace an Environmental Sensor or Sensor Position**: Site/Tenant Admin.
- **Configure Severe Weather Alert thresholds**: Tenant Admin.
- **View overlays / sensor readings**: inherits the existing UOP Map/Wallboard viewing posture — no new permission.
- **Trigger Activate EOC Response from a severe weather suggestion**: existing Multi-Incident Console permission, unchanged.

## Non-Functional / Constraints

- `live_feed` Overlay Layer refresh cadence is adaptor-declared, not a platform-wide fixed interval — some providers refresh radar every few minutes, others less often; this doc imposes no artificial floor/ceiling.
- Environmental Sensor telemetry volume is expected to be low-to-moderate (periodic readings, not a high-frequency signal storm) — no storm-collapse mechanism is needed here, unlike Activity Registry's alarm-signal valve.
- No new streaming/media infrastructure — weather/traffic overlays are adaptor-rendered tiles/vectors, same as GIS's existing map tile provider.

## Acceptance Criteria

- [ ] A tenant-configured weather adaptor's radar/lightning data renders as a toggle-able `live_feed` Overlay Layer on UOP Map, using the exact same toggle UI as a static KML overlay.
- [ ] An Environmental Sensor's readings are queryable as Location-tagged, timestamped telemetry via Signal Disposition, identical in shape/fidelity to alarm-signal telemetry.
- [ ] Replacing an Environmental Sensor unit preserves its Position's identity; the prior Sensor's Mount Association closes and a new one opens.
- [ ] A Severe Weather Alert at or above the configured threshold surfaces a one-tap Activate EOC Response suggestion; tapping it still requires passing the existing confirmation gate.
- [ ] A Severe Weather Alert below threshold delivers as an ordinary Notifications Engine alert with no EOC suggestion attached.
- [ ] Traffic congestion overlay data renders identically to weather radar — both are `live_feed` Overlay Layer instances, no special-cased traffic logic.

## Open Questions

- Exact severity-threshold vocabulary/scale for Severe Weather Alert (tenant-defined categories vs. a standard NWS-aligned scale) — content/config design, not committed here.
- Whether Environmental Sensor readings ever need their own dedicated correlation/analytics view before Module 19's Alarm Pattern & Nuisance Analysis exists — explicitly deferred per user direction; noted here as the motivating future use, not built now.
- Exact `live_feed` adaptor capability declaration (refresh interval, supported layer types per vendor) — forward-referenced, technical-spec concern.
