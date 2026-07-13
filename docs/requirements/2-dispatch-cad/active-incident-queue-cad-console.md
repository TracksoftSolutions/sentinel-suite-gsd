# Active Incident Queue (CAD Console)

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

This is the payoff Activity Registry was built for. The console is a real-time **CQRS read-model projection over Activity Registry itself** (per Event & Command Bus Architecture's Query/Event bus pillars, the same materialized-projection discipline Entity Relationships & History already established for historical views) — surfacing **every Activity currently `open` or `in_progress`, platform-wide**, not a bespoke Call/Dispatch/Incident-only list. A currently-running Guard Tour Patrol, an open DAR, a queued Call, an in-progress Incident all appear through the exact same mechanism, filterable/sortable/groupable down from there. Critically, this means any future module's Activity extension (Alarm, Inspection, Drill, whatever Safety Management or Physical Security Integration Gateway eventually register) **automatically appears in this queue with zero console-side changes** the moment it's registered against Activity Registry — mirroring how Entity Relationships & History's timeline already proved this for history; this doc is that same universal mechanism's live/current-state counterpart.

**Default view is scoped, not everything at once.** The underlying query is universal, but a Dispatcher's default filter preset shows only Dispatch/CAD's own types (Call, Dispatch, Incident) plus Patrol Management's Activity types (Route Assignment, Patrol, Checkpoint Scan, Patrol Request, Patrol Finding, Courtesy Patrol) — the operationally relevant set for day-to-day dispatch work. A Dispatcher can broaden the filter to any registered Activity type at will; the default is a Settings & Preferences-registered preset, tenant-configurable, not a hard console limitation.

**Heterogeneous priority is normalized, not retrofitted.** Call has its own Priority Definition, Incident its own Severity Definition, and future types will have their own scales too — deliberately kept independent per feature, per existing decisions. Rather than retrofitting a new universal field onto every Activity extension (which would touch a dozen already-written docs for a benefit only this console needs), a tenant-configurable **Queue Urgency Mapping** normalizes `activity_type` + that type's own priority/severity value into a small shared urgency-tier vocabulary, purely at this doc's read-model layer — any type with no mapping entry simply sorts/groups as "Unclassified," ordered by time only, so an unmapped future Activity type degrades gracefully rather than breaking.

**Distinct from Multi-Incident Console (later, this module).** This doc is the **list/queue view**: see everything in scope, scan, filter, group, drill into one record. Multi-Incident Console is a different interaction mode — a multi-pane workspace for actively working several open incidents side-by-side — built on top of this doc's same underlying read-model, not a competing list.

Every row renders via its source Activity's own registered `display_label_strategy` (per Entity Registry Core's universal display-label requirement) — this console never implements per-type rendering logic. Drilling into a row navigates to that Activity's own detail view, owned by its extension's module; any action taken from there (assign a Dispatch, add an Incident Update) invokes that Activity's own already-registered Command/Action Bus actions — the console adds no new business-logic actions of its own.

## Actors & Roles

- **Dispatcher / Console Operator** — primary user; views, filters, sorts, groups the live queue, drills into individual Activities.
- **Supervisor / EOC Staff** — same viewing access, typically with a broader default scope (multi-site).
- **Tenant Admin** — configures default filter presets, the Queue Urgency Mapping, and locks/unlocks admin-defined default views via Settings & Preferences.
- **Records Admin** — same access as any queue viewer, plus visibility into read-model projection health (lag/staleness), same posture as Entity Relationships & History.

## User Stories

- As a **Dispatcher**, I want one screen showing every call, dispatch, and incident currently open at my site, sorted by urgency, so I never lose track of something in progress.
- As a **Dispatcher**, I want to group the queue by activity type so I can scan just the open Incidents separately from queued Calls when I need to.
- As a **Supervisor**, I want to broaden my view to include in-progress Patrols and DAR entries when I'm doing a shift-wide check, without that noise cluttering the Dispatcher's default screen.
- As a **Dispatcher**, I want the queue to update the instant a new call comes in or a unit clears, without me having to manually refresh.
- As a **Tenant Admin**, I want to define how our site's call priorities and incident severities map to one shared urgency scale, since Dispatchers need one consistent "what's most urgent" ordering across different record types.
- As an **EOC Analyst** during a broader response, I want to see every open Activity across every site in scope, not just one Dispatcher's local queue.
- As a **future Safety Management developer**, I want an Inspection Activity extension I register later to just show up in this queue automatically, without needing to modify this console.

## Functional Requirements

### Core query
1. The queue's core data source is every Activity, of any registered type/extension, with `status ∈ {open, in_progress}`, scoped to the sites/tenant the viewing user has RBAC/ABAC access to — a direct, universal query over Activity Registry, not a per-module aggregation.
2. An Activity leaving `open`/`in_progress` (reaching `concluded` or `cancelled`) drops out of the live queue automatically, following the source Activity's own lifecycle exactly — no separate queue-membership state to maintain.

### Default scope & filtering
3. A **default filter preset** limits the queue, out of the box, to Dispatch/CAD's own Activity types (Call, Dispatch, Incident) plus Patrol Management's (Route Assignment, Patrol, Checkpoint Scan, Patrol Request, Patrol Finding, Courtesy Patrol) — a Settings & Preferences registration, tenant-configurable, not hardcoded.
4. A viewer can filter by: activity type(s), status, site/location, assigned unit/person, urgency tier (see below), and an unassigned-only toggle. Filters compose (e.g., "open Incidents at Site B, unassigned").
5. A viewer can broaden or narrow the type filter to any Activity type registered platform-wide, not limited to the default preset's set.

### Sorting & grouping
6. Supported sort dimensions: time (received/started, oldest- or newest-first), urgency tier, status.
7. Supported group dimensions: by activity type, by site/location, by urgency tier, by assigned unit — groups are collapsible.

### Queue Urgency Mapping (cross-type normalization)
8. A tenant-configurable **Queue Urgency Mapping** maps `(activity_type, source_field, source_value)` — e.g., `(call, priority, P1-Emergency)` or `(incident, severity, Critical)` — to one of a small shared urgency tiers (e.g., Critical, High, Medium, Low). This lives entirely at this doc's read-model layer; it does not add a field to Call, Incident, or any other Activity extension's own table.
9. An Activity type/value combination with no mapping entry defaults to an **Unclassified** tier, sorted/grouped together and ordered by time only within that tier — an unmapped future Activity type degrades gracefully rather than erroring or being hidden.

### Rendering & drill-in
10. Each queue row shows: the source Activity's resolved `display_label`, activity type, status, urgency tier (if mapped), started/received time, and assigned unit (if applicable to that type) — a small, universal column set, no per-type rendering branches.
11. Selecting a row navigates to that Activity's own detail view, owned by its extension's module (a Call's own screen, an Incident's own screen, etc.) — this console performs no business-logic actions itself.

### Saved views
12. A Dispatcher can save a named filter/sort/group configuration as a personal view (Settings & Preferences identity chain). A Tenant/Site Admin can define and optionally **lock** a default view for a role or site (location chain) — narrowest-wins with locking, per Settings & Preferences' existing resolution engine, no new override logic invented here.

### Real-time updates
13. Initial queue load reads through the CQRS **Query bus** (RBAC/ABAC-filtered read-model, no side effects); subsequent changes (new Activity created, status/assignment changed) push incrementally via **Event bus** fan-out — the queue reflects change within a small, monitored propagation lag, not literally instantaneous, consistent with the same freshness posture Entity Relationships & History already established for its own projection.

## Data Model / Fields

**Active Activity Queue Entry** (read-model row — not a stored entity; the source Activity's own `entity_id` is the only identity)
- entity_id (source Activity's entity_id)
- activity_type, status, started_at
- display_label (resolved via Entity Registry Core's display_label_strategy)
- location_ref (nullable, from the source Activity's ActivityLocationAssociation, if present)
- assigned_unit_ref (nullable — resolved per type, e.g. a Dispatch's assigned_person_ref)
- urgency_tier (resolved via Queue Urgency Mapping; `unclassified` if no mapping entry matches)
- sensitivity_tags[] (carried from the source record, ABAC-filtered at read time, same discipline as Entity Relationships & History's Timeline Entry)

**Queue Urgency Mapping** (Settings & Preferences registration)
- mapping_id, tenant_id, activity_type, source_field (e.g., priority, severity), source_value, urgency_tier

**Queue View** (saved filter/sort/group configuration — Settings & Preferences registration)
- view_id, tenant_id, owner_scope (a specific user, or a location-chain level for an admin default), name
- filter_config (activity_types[], statuses[], site_refs[], urgency_tiers[], assigned_only/unassigned_only)
- sort_config (dimension, direction), group_config (dimension)
- locked (bool, admin-set — blocks narrower override, per Settings & Preferences' existing lock mechanic)

## States & Transitions

**Queue Entry membership:** mirrors the source Activity's own lifecycle exactly — appears at `open`, remains through `in_progress`, disappears at `concluded`/`cancelled`. No independent queue-entry state.

**Read-model projection:** `current` (caught up) → `lagging` (behind a monitored threshold, surfaced to Records Admin) → `current` (catches up) — identical pattern to Entity Relationships & History's own projection.

## Integrations

- **Activity Registry**: the universal source of every queue entry — any registered Activity type/extension automatically participates, no bespoke per-module integration required, exactly as Entity Relationships & History already established for the historical view.
- **Entity Registry Core**: source of the display-label mechanism every row renders through.
- **Call Intake & Logging, Unit Dispatch & Proximity Routing, Guard Tour & Checkpoint Verification, Patrol Management, Incident Reporting & Management, Daily Activity Reports, Tickets/Citations & Traffic Safety, Courtesy Patrol**: default-preset-included Activity type sources; every other current and future Activity-producing module is queryable by broadening the filter, with zero console-side change required when a new type is registered.
- **Event & Command Bus Architecture**: owns the underlying CQRS Query bus (initial load) and Event bus (incremental push) infrastructure this projection is built on.
- **Settings & Preferences**: owns the Queue Urgency Mapping, default filter presets, and personal/admin-locked Saved Views, via its existing location + identity chain resolution — no new override mechanism invented here.
- **Command/Action Bus, Command Palette**: drill-in navigation and any action taken on a selected Activity reuse that Activity's own already-registered actions; this doc registers no new action types.
- **Authentication & Authorization**: source of the RBAC/ABAC scoping (site/tenant access) that bounds what a given viewer's queue query can ever return, and the per-entry sensitivity filtering applied at read time.
- **Multi-Incident Console (Module 2, future)**: a different, multi-pane interaction mode built on top of this doc's same underlying queue read-model — not a competing list; the precise UI hand-off between "select from the queue" and "work in Multi-Incident Console" is that future doc's own concern.
- **Command Center's Unified Operational Picture (UOP) Map (Module 3, future)**: a natural downstream consumer of this same live queue for map-pin rendering — a forward reference only, not built here.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor/EOC Staff | Tenant Admin |
|---|---|---|---|---|
| View the Active Activity Queue (own scope) | ❌ (unless granted) | ✅ | ✅ | ✅ |
| Broaden filter beyond default preset | ❌ | ✅ | ✅ | ✅ |
| Save a personal Queue View | ❌ | ✅ | ✅ | ✅ |
| Define/lock an admin default Queue View or Urgency Mapping | ❌ | ❌ | ❌ | ✅ |
| View read-model projection lag/health | ❌ | ❌ | ✅ (if granted) | ✅ |

Holding queue-view access does not bypass per-entry ABAC/sensitivity filtering — a row a viewer isn't otherwise cleared to see is filtered out of the queue exactly as if they'd tried to open that Activity's own record directly, same discipline as Entity Relationships & History's Timeline Viewer.

## Non-Functional / Constraints

- Read-model propagation lag must stay within a defined, monitored bound, with staleness observable by a Records Admin — same requirement as Entity Relationships & History's own projection.
- Must remain performant and responsive with many concurrent Dispatchers watching a live board and a high Activity-creation/update volume — this is a persistent operational screen, not an occasional lookup.
- Unlike Interaction Timeline Viewer access (a deliberate per-entity lookup, itself audit-tier), simply having the console open and receiving live updates is not individually audit-logged per row — audit-tier logging remains owned by each underlying Activity's own actions (Call creation, Dispatch assignment, etc.), not duplicated here as a viewing log.
- Urgency-tier indicators must not rely on color alone (WCAG 2.1 non-color-only status indicator requirement, consistent with GIS & Mapping Services' own accessibility posture).
- WCAG 2.1 / Section 508 accessible list/grouping/filtering interactions, day one.

## Acceptance Criteria

- [ ] The default queue view for a Dispatcher shows open/in-progress Calls, Dispatches, Incidents, and Patrol Management Activity types, and nothing else, out of the box.
- [ ] Broadening the type filter to include an Activity type outside the default preset (e.g., DAR entries) correctly includes it without any console-side code change.
- [ ] A newly registered Activity extension from a future module (simulated via a test type) automatically appears in the universal query when included in a filter, with zero console changes.
- [ ] An Activity reaching `concluded` or `cancelled` disappears from the live queue automatically.
- [ ] Grouping by activity type correctly buckets rows; grouping by urgency tier correctly buckets a mix of Call- and Incident-sourced rows using the tenant's Queue Urgency Mapping.
- [ ] An Activity type/value with no Queue Urgency Mapping entry appears in an "Unclassified" tier, ordered by time, rather than erroring or being hidden.
- [ ] A new Call appearing, or a Dispatch's phase changing, is reflected in an open queue view within the defined propagation-lag bound, with no manual refresh required.
- [ ] Selecting a queue row navigates to that Activity's own detail view; no action is performed by the console itself.
- [ ] A Dispatcher can save a personal Queue View and have it be their default on next login; a Tenant Admin-locked default view cannot be overridden by a narrower personal preference.
- [ ] A viewer without RBAC/ABAC access to a given site's Activities never sees that site's entries in their queue, regardless of filter settings.
- [ ] A Records Admin can observe read-model projection lag/health for the queue.

## Open Questions

- Exact urgency-tier vocabulary and count (e.g., 4 tiers vs. 5) and default out-of-box Queue Urgency Mapping — pending UX/content design.
- Whether a just-cleared Activity should linger briefly in the queue (a "recently cleared" grace display) before disappearing, versus dropping instantly — not committed here; a UI-polish decision for technical spec.
- Whether Dispatcher (single-site) vs. Supervisor/EOC (multi-site) default scope needs its own explicit Settings & Preferences-registered policy, or is fully derived from each role's existing RBAC/ABAC site access — current default assumes the latter (no separate scope-policy concept introduced), revisit if a real gap surfaces.
- Whether future modules registering a new Activity extension should be expected/required to also register a Queue Urgency Mapping entry and default-preset inclusion, or whether that stays purely optional/tenant-driven — leaning optional (the universal query and Unclassified-tier fallback already guarantee correctness without it), not mandated here.
- Precise UI hand-off mechanics between this queue and Multi-Incident Console — deferred to that doc.
