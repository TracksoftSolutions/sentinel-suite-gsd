# Active Incident Queue (CAD Console)

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

This is the payoff Activity Registry was built for. The console is a real-time **CQRS read-model projection over Activity Registry itself** (per Event & Command Bus Architecture's Query/Event bus pillars, the same materialized-projection discipline Entity Relationships & History already established for historical views) — surfacing **every Activity currently `open` or `in_progress`, platform-wide**, not a bespoke Call/Dispatch/Incident-only list. Critically, this means any future module's Activity extension (Alarm, Inspection, Drill, whatever Safety Management or Physical Security Integration Gateway eventually register) **is queryable by this console with zero console-side code change** the moment it's registered against Activity Registry — mirroring how Entity Relationships & History's timeline already proved this for history; this doc is that same universal mechanism's live/current-state counterpart.

**The board is made of cards, not a flat row-per-Activity list — and not every open Activity is its own card.** A dispatcher thinks in terms of "an officer is touring Building 12 right now" (one card, the Patrol), not "37 separate rows, one per checkpoint scanned so far." So each Activity Type Registration declares one of three **queue roles**, resolved locally by this doc rather than retrofitted onto Activity Registry Core or any producing module's own schema (the same "normalize at the consumer, not the producer" call already made for Queue Urgency Mapping):
- **Card** — a standalone, top-level queue entry (a Call, an Incident, a Patrol, a Route Assignment).
- **Feed** — rolls up as a live, timestamped status-update line *inside* its parent's card, using that type's own already-established direct parent-reference field (no new field required on the producer) — a Checkpoint Scan nests inside the Patrol it belongs to (`patrol_ref`) exactly the way it already nests operationally; a Dispatch nests inside the Call it's fulfilling (`source_call_ref`), each assigned officer showing as their own status line with live phase progress; an Incident Update nests inside its Incident (`origin_incident_ref`) — which is just this doc formally recognizing what Incident Update's timeline already was.
- **None** — never appears on the board at all, even when broadened (e.g., DAR) — not operationally board-relevant.

A **Feed** Activity whose parent-link field is null (a self-initiated record with nothing to nest under — Courtesy Patrol with no `source_patrol_request_ref`, for instance) is promoted to standalone **Card** display instead of disappearing — nothing a type registers as queue-eligible is ever silently dropped for lack of a parent. Nesting is exactly **one level deep** (a card's direct Feed children only); a Feed type's own children are not transitively pulled up through it.

**Route Assignment is a Card too, but a different shape of one — a rollup, not a Feed-nesting parent.** A Dispatcher works individual Patrols in the moment; a Supervisor monitors whether a whole Route (potentially several Patrols across a shift) is actually getting covered. Rather than nesting every underlying Patrol as a Feed line under Route Assignment (which would need two-level nesting and would double-display each Patrol, since Patrol is already its own standalone Card), Route Assignment's card simply surfaces its own already-computed `completeness_pct` rollup (established in Guard Tour & Checkpoint Verification) via its computed `display_label` — a summary card with no Feed children at all, sidestepping the transitive-nesting problem entirely rather than solving it generically. Drilling into it goes to Route Assignment's own detail view (owned by Guard Tour & Checkpoint Verification), which lists its actual Patrols.

**Default view is scoped per role, not one universal default.** The underlying query is universal, but the out-of-box default preset differs by who's looking: a **Dispatcher's** default shows Call, Incident, Patrol, Patrol Request, Patrol Finding, and Courtesy Patrol (parentless) — the individual, in-the-moment units a Dispatcher actually acts on, deliberately excluding Route Assignment's coarser rollup. A **Supervisor/EOC** default adds Route Assignment on top of the Dispatcher set, for route-completion monitoring. Both are Settings & Preferences-registered presets (role-scoped, per the existing Saved View mechanism), tenant-configurable; either role can broaden further to any Card-role type registered platform-wide — this is a default-view difference, not a hard access restriction.

**Heterogeneous priority is normalized, not retrofitted.** Call has its own Priority Definition, Incident its own Severity Definition, and future types will have their own scales too — deliberately kept independent per feature, per existing decisions. Rather than retrofitting a new universal field onto every Activity extension (which would touch a dozen already-written docs for a benefit only this console needs), a tenant-configurable **Queue Urgency Mapping** normalizes `activity_type` + that type's own priority/severity value into a small shared urgency-tier vocabulary, purely at this doc's read-model layer — any type with no mapping entry simply sorts/groups as "Unclassified," ordered by time only, so an unmapped future Activity type degrades gracefully rather than breaking.

**Distinct from Multi-Incident Console (later, this module).** This doc is the **list/queue view**: see everything in scope, scan, filter, group, drill into one card. Multi-Incident Console is a different interaction mode — a multi-pane workspace for actively working several open incidents side-by-side — built on top of this doc's same underlying read-model, not a competing list.

Every card and every feed line renders via its own source Activity's registered `display_label_strategy` (per Entity Registry Core's universal display-label requirement) — this console never implements per-type rendering logic. Drilling into a card navigates to that Activity's own detail view, owned by its extension's module; any action taken from there (assign a Dispatch, add an Incident Update) invokes that Activity's own already-registered Command/Action Bus actions — the console adds no new business-logic actions of its own.

## Actors & Roles

- **Dispatcher / Console Operator** — primary user; views, filters, sorts, groups the live queue, drills into individual Activities. Default scope favors individual, actionable units (Patrols, Calls, Incidents) over coarser rollups.
- **Supervisor / EOC Staff** — same viewing access, typically with a broader default scope (multi-site, plus Route Assignment-level rollups for route-completion monitoring rather than just individual Patrols).
- **Tenant Admin** — configures default filter presets, the Queue Urgency Mapping, and locks/unlocks admin-defined default views via Settings & Preferences.
- **Records Admin** — same access as any queue viewer, plus visibility into read-model projection health (lag/staleness), same posture as Entity Relationships & History.

## User Stories

- As a **Dispatcher**, I want one screen showing every call and incident currently open at my site, sorted by urgency, so I never lose track of something in progress.
- As a **Dispatcher**, I want a Patrol card to show me each checkpoint as it's scanned, right there on the card, instead of 30 separate checkpoint-scan rows burying the actual calls and incidents I need to act on.
- As a **Dispatcher**, I want a multi-officer Call's card to show each assigned officer's own phase (en route, arrived, cleared) as a status line, so I can see the whole response's progress at a glance without opening each Dispatch individually.
- As a **Dispatcher**, I want to group the queue by activity type so I can scan just the open Incidents separately from queued Calls when I need to.
- As a **Supervisor**, I want to see whether a whole Route is actually getting covered across a shift, without having to mentally tally up a dozen individual Patrol cards myself.
- As a **Dispatcher**, I don't want a whole Route Assignment cluttering my board — I work individual Patrols, not the shift-long assignment they belong to.
- As a **Dispatcher**, I want the queue to update the instant a new call comes in or a unit clears, without me having to manually refresh.
- As a **Tenant Admin**, I want to define how our site's call priorities and incident severities map to one shared urgency scale, since Dispatchers need one consistent "what's most urgent" ordering across different record types.
- As an **EOC Analyst** during a broader response, I want to see every open Card-role Activity across every site in scope, not just one Dispatcher's local queue.
- As a **future Safety Management developer**, I want to register an Inspection Activity extension as a Card (or as a Feed under something else) and have it behave correctly on this board without needing to modify this console.

## Functional Requirements

### Core query
1. The queue's core data source is every Activity, of any registered type/extension, with `status ∈ {open, in_progress}`, scoped to the sites/tenant the viewing user has RBAC/ABAC access to — a direct, universal query over Activity Registry, not a per-module aggregation.
2. An Activity leaving `open`/`in_progress` (reaching `concluded` or `cancelled`) drops out of the live queue automatically, following the source Activity's own lifecycle exactly — no separate queue-membership state to maintain. This applies identically whether it was displayed as a Card or nested as a Feed line.

### Queue Role Registration (Card / Feed / None)
3. Every Activity type/extension that participates in this console at all is registered here (not on Activity Registry Core or any producing module's own doc) with a **queue role**: `card`, `feed`, or `none` (the default for anything unregistered — invisible to the board even when the type filter is broadened).
4. A `feed`-role registration also declares its **parent link field** — the existing direct field on that type that already points to its parent (e.g., Checkpoint Scan's `patrol_ref`, Dispatch's `source_call_ref`, Incident Update's `origin_incident_ref`, Courtesy Patrol's `source_patrol_request_ref`) — no new field is added to any producing type to support this.
5. A `feed`-role Activity whose parent link is set, and whose parent is itself a currently-displayed `card`, renders as a status-update line nested inside that card — never as its own top-level entry.
6. A `feed`-role Activity whose parent link is **null** (no parent to nest under) is promoted to standalone `card` display for as long as it remains parentless — a type registered as `feed` never silently vanishes from the board for lack of a parent.
7. Nesting is exactly one level: a card shows its own direct Feed children; a Feed child's own children (if any type were ever registered that deep) are not transitively pulled up through it.
8. A **Card**-role type is not required to have any Feed children at all — it may instead be a pure rollup card, surfacing its own already-computed summary fields (e.g., Route Assignment's `completeness_pct`) via its own `display_label_strategy`, with no nested status-update lines. This is deliberately how the Guard Tour hierarchy's actual three-tier depth (Route Assignment → Patrol → Checkpoint Scan) is handled: Route Assignment is a Card, but not one that nests Patrol as a Feed line (Patrol is already its own independent Card) — it just shows its own rollup, sidestepping the need for two-level nesting entirely rather than solving it generically. Drilling into a rollup card navigates to that Activity's own detail view, same as any other card.
9. At day one: `card` — Call, Incident, Patrol, Route Assignment (rollup-only, no Feed children, per #8), Patrol Request, Patrol Finding, Courtesy Patrol (only when parentless, per #6). `feed` — Dispatch (parent: Call), Checkpoint Scan (parent: Patrol), Incident Update (parent: Incident), Courtesy Patrol (parent: Patrol Request, when set). `none` — Daily Activity Report entries, and every other registered type by default until its owning module explicitly registers it otherwise.

### Default scope & filtering
10. **Default filter presets are role-scoped, not a single universal default.** A **Dispatcher Default** preset (Call, Incident, Patrol, Patrol Request, Patrol Finding, Courtesy Patrol) and a broader **Supervisor/EOC Default** preset (the same set, plus Route Assignment) both ship day one — Settings & Preferences registrations (role-scoped Saved Views, per Functional Requirement #19), tenant-configurable, not hardcoded to any one role permanently.
11. A viewer can filter by: activity type(s) (Card-role only — Feed types are never independently filterable as top-level rows, only visible nested within their card), status, site/location, assigned unit/person, urgency tier (see below), and an unassigned-only toggle. Filters compose (e.g., "open Incidents at Site B, unassigned").
12. A viewer can broaden or narrow the type filter to any `card`-role type registered platform-wide, not limited to their role's default preset's set (a Dispatcher can pull up Route Assignment rollups too, they're just not there by default). `none`-role types remain invisible regardless of filter settings — broadening the filter does not reach them; a type must be registered `card` or `feed` (with a parentless fallback) to ever appear.

### Sorting & grouping
13. Supported sort dimensions (cards only — Feed lines within a card are always ordered chronologically, oldest first): time (received/started, oldest- or newest-first), urgency tier, status.
14. Supported group dimensions: by activity type, by site/location, by urgency tier, by assigned unit — groups are collapsible.

### Queue Urgency Mapping (cross-type normalization)
15. A tenant-configurable **Queue Urgency Mapping** maps `(activity_type, source_field, source_value)` — e.g., `(call, priority, P1-Emergency)` or `(incident, severity, Critical)` — to one of a small shared urgency tiers (e.g., Critical, High, Medium, Low). This lives entirely at this doc's read-model layer; it does not add a field to Call, Incident, or any other Activity extension's own table. Only Card-role Activities carry an urgency tier — Feed lines inherit their parent card's tier implicitly rather than needing their own mapping.
16. An Activity type/value combination with no mapping entry defaults to an **Unclassified** tier, sorted/grouped together and ordered by time only within that tier — an unmapped future Card-role type degrades gracefully rather than erroring or being hidden.

### Rendering & drill-in
17. Each card shows: the source Activity's resolved `display_label`, activity type, status, urgency tier (if mapped), started/received time, assigned unit (if applicable to that type), and its live Feed line list (each Feed line showing its own resolved `display_label` and timestamp) — a small, universal card shape, no per-type rendering branches. A rollup card with no Feed children (Route Assignment) simply omits the Feed line list.
18. Selecting a card navigates to that Activity's own detail view, owned by its extension's module (a Call's own screen, an Incident's own screen, etc.); selecting an individual Feed line navigates to that Feed Activity's own record instead (e.g., a specific officer's Dispatch, a specific Checkpoint Scan) — this console performs no business-logic actions itself either way.

### Saved views
19. A Dispatcher or Supervisor can save a named filter/sort/group configuration as a personal view (Settings & Preferences identity chain), starting from their role's default preset. A Tenant/Site Admin can define and optionally **lock** a default view for a role or site (location chain) — narrowest-wins with locking, per Settings & Preferences' existing resolution engine, no new override logic invented here. The Dispatcher Default and Supervisor/EOC Default presets (Functional Requirement #10) are themselves just role-scoped Saved Views using this exact mechanism, not a separate concept.

### Real-time updates
20. Initial queue load reads through the CQRS **Query bus** (RBAC/ABAC-filtered read-model, no side effects); subsequent changes (new Activity created, status/assignment changed, a Feed child added to an open card, a rollup card's summary field recalculating) push incrementally via **Event bus** fan-out — the queue reflects change within a small, monitored propagation lag, not literally instantaneous, consistent with the same freshness posture Entity Relationships & History already established for its own projection.

## Data Model / Fields

**Queue Role Registration** (local to this doc — not a retrofit of Activity Registry Core or any producing module)
- registration_id, activity_type, queue_role (card, feed, none — default none)
- parent_link_field (required when queue_role = feed — the name of that type's own existing direct parent-reference field)
- registered_by (owning feature/module)

**Active Activity Queue Card** (read-model row — not a stored entity; the source Activity's own `entity_id` is the only identity)
- entity_id (source Activity's entity_id)
- activity_type, status, started_at
- display_label (resolved via Entity Registry Core's display_label_strategy)
- location_ref (nullable, from the source Activity's ActivityLocationAssociation, if present)
- assigned_unit_ref (nullable — resolved per type, e.g. a Patrol's assigned officer)
- urgency_tier (resolved via Queue Urgency Mapping; `unclassified` if no mapping entry matches)
- sensitivity_tags[] (carried from the source record, ABAC-filtered at read time, same discipline as Entity Relationships & History's Timeline Entry)
- feed_lines[] (ordered, chronological — see Status Update Feed Line, below)
- promoted_from_feed (bool — true when this card exists only because a `feed`-role Activity's parent link was null, per Functional Requirement #6)

**Status Update Feed Line** (read-model, nested within its parent card — not independently queryable as a top-level row)
- entity_id (source Feed Activity's entity_id)
- activity_type, display_label (resolved via Entity Registry Core's display_label_strategy)
- timestamp (the Feed Activity's own relevant timestamp — e.g. a Checkpoint Scan's scan time, a Dispatch phase's phase_timestamp, an Incident Update's update_timestamp)
- parent_card_entity_id (the Card this line is nested under)

**Queue Urgency Mapping** (Settings & Preferences registration)
- mapping_id, tenant_id, activity_type, source_field (e.g., priority, severity), source_value, urgency_tier

**Queue View** (saved filter/sort/group configuration — Settings & Preferences registration)
- view_id, tenant_id, owner_scope (a specific user, or a location-chain level for an admin default), name
- filter_config (activity_types[], statuses[], site_refs[], urgency_tiers[], assigned_only/unassigned_only)
- sort_config (dimension, direction), group_config (dimension)
- locked (bool, admin-set — blocks narrower override, per Settings & Preferences' existing lock mechanic)

## States & Transitions

**Card membership:** mirrors the source Activity's own lifecycle exactly — appears at `open`, remains through `in_progress`, disappears at `concluded`/`cancelled`. No independent card state.

**Card ↔ promoted-from-Feed transition:** a `feed`-role Activity's card status is re-evaluated on every relevant change — if a previously-parentless Feed Activity's parent link is later set (or its parent enters an open/in_progress state after being closed), it demotes from a standalone promoted card into a nested Feed line under that parent's card; the reverse (parent closes or link is cleared) promotes it back. Never a data loss event, purely a display-placement change.

**Read-model projection:** `current` (caught up) → `lagging` (behind a monitored threshold, surfaced to Records Admin) → `current` (catches up) — identical pattern to Entity Relationships & History's own projection.

## Integrations

- **Activity Registry**: the universal source of every queue entry — any registered Activity type/extension is queryable once given a Queue Role Registration, no bespoke per-module integration required beyond that registration, in the same spirit Entity Relationships & History already established for the historical view (though that feature's participation is fully automatic — this one requires the explicit `card`/`feed`/`none` opt-in described above, a deliberate difference given a live operational board's clutter/performance sensitivity versus a paginated per-entity history view).
- **Entity Registry Core**: source of the display-label mechanism every card and Feed line renders through.
- **Call Intake & Logging** (Call → card), **Unit Dispatch & Proximity Routing** (Dispatch → feed, parent Call), **Guard Tour & Checkpoint Verification** (Patrol → card; Checkpoint Scan → feed, parent Patrol; Route Assignment → card, rollup-only via its own `completeness_pct`, no Feed children), **Patrol Management** (Patrol Request → card; Post → not an Activity, not applicable), **Incident Reporting & Management** (Incident → card; Incident Update → feed, parent Incident), **Courtesy Patrol** (→ card when parentless, feed under Patrol Request otherwise): day-one Queue Role Registrations. Every other current and future Activity-producing module remains `none` (invisible) until it explicitly registers a role here.
- **Event & Command Bus Architecture**: owns the underlying CQRS Query bus (initial load) and Event bus (incremental push) infrastructure this projection is built on.
- **Settings & Preferences**: owns the Queue Urgency Mapping, default filter presets, and personal/admin-locked Saved Views, via its existing location + identity chain resolution — no new override mechanism invented here.
- **Command/Action Bus, Command Palette**: drill-in navigation and any action taken on a selected Activity reuse that Activity's own already-registered actions; this doc registers no new action types.
- **Authentication & Authorization**: source of the RBAC/ABAC scoping (site/tenant access) that bounds what a given viewer's queue query can ever return, and the per-entry sensitivity filtering applied at read time.
- **Multi-Incident Console (Module 2, future)**: a different, multi-pane interaction mode built on top of this doc's same underlying queue read-model — not a competing list; the precise UI hand-off between "select from the queue" and "work in Multi-Incident Console" is that future doc's own concern.
- **Command Center's Unified Operational Picture (UOP) Map (Module 3, future)**: a natural downstream consumer of this same live queue for map-pin rendering — a forward reference only, not built here.

## Permissions

| Action | Guard/Officer | Dispatcher | Supervisor/EOC Staff | Tenant Admin |
|---|---|---|---|---|
| View the Active Activity Queue (own scope) | ❌ (unless granted) | ✅ (Dispatcher Default) | ✅ (Supervisor/EOC Default, includes Route Assignment rollups) | ✅ |
| Broaden filter beyond own role's default preset | ❌ | ✅ | ✅ | ✅ |
| Save a personal Queue View | ❌ | ✅ | ✅ | ✅ |
| Define/lock an admin default Queue View (per role) or Urgency Mapping | ❌ | ❌ | ❌ | ✅ |
| View read-model projection lag/health | ❌ | ❌ | ✅ (if granted) | ✅ |

Holding queue-view access does not bypass per-entry ABAC/sensitivity filtering — a row a viewer isn't otherwise cleared to see is filtered out of the queue exactly as if they'd tried to open that Activity's own record directly, same discipline as Entity Relationships & History's Timeline Viewer.

## Non-Functional / Constraints

- Read-model propagation lag must stay within a defined, monitored bound, with staleness observable by a Records Admin — same requirement as Entity Relationships & History's own projection.
- Must remain performant and responsive with many concurrent Dispatchers watching a live board and a high Activity-creation/update volume — this is a persistent operational screen, not an occasional lookup. The Card/Feed model is itself a mitigation here: a high-volume, event-shaped `feed`-role type (Checkpoint Scan) never inflates the top-level row count the board has to render/sort/group, since it's only ever fetched as a nested detail of its already-displayed parent card.
- Unlike Interaction Timeline Viewer access (a deliberate per-entity lookup, itself audit-tier), simply having the console open and receiving live updates is not individually audit-logged per row — audit-tier logging remains owned by each underlying Activity's own actions (Call creation, Dispatch assignment, etc.), not duplicated here as a viewing log.
- Urgency-tier indicators must not rely on color alone (WCAG 2.1 non-color-only status indicator requirement, consistent with GIS & Mapping Services' own accessibility posture).
- WCAG 2.1 / Section 508 accessible list/grouping/filtering interactions, day one.

## Acceptance Criteria

- [ ] The default queue view for a Dispatcher shows open/in-progress Calls, Incidents, Patrols, Patrol Requests, Patrol Findings, and parentless Courtesy Patrols as cards, and nothing else, out of the box — no Route Assignment rollups.
- [ ] The default queue view for a Supervisor/EOC Staff includes everything the Dispatcher default shows, plus Route Assignment rollup cards.
- [ ] A Route Assignment card displays its own `completeness_pct` rollup via its computed display label and has no Feed line list, while the Patrol Activities underneath it continue to appear independently as their own cards (never double-displayed).
- [ ] A Patrol card shows each of its Checkpoint Scans as a chronological Feed line within the card, never as separate top-level queue rows.
- [ ] A multi-officer Call card shows each assigned officer's Dispatch as its own Feed line, reflecting that officer's current phase, never as separate top-level queue rows.
- [ ] An Incident card shows its Incident Updates as Feed lines within the card, consistent with how that timeline already behaved on the Incident's own detail view.
- [ ] A Courtesy Patrol with `source_patrol_request_ref` set nests as a Feed line under its Patrol Request's card; a self-initiated Courtesy Patrol with no Patrol Request appears as its own standalone card instead.
- [ ] Broadening the type filter to include a `card`-role Activity type outside the default preset correctly includes it without any console-side code change; attempting to broaden to a `none`-role type (e.g., Route Assignment) never surfaces it.
- [ ] A newly registered `card`-role Activity extension from a future module (simulated via a test type) is queryable and displays correctly once its Queue Role Registration is added, with zero further console changes; a same test type left unregistered (`none` by default) never appears.
- [ ] An Activity reaching `concluded` or `cancelled` disappears from the live queue automatically, whether it was displayed as a card or nested as a Feed line.
- [ ] Grouping by activity type correctly buckets cards; grouping by urgency tier correctly buckets a mix of Call- and Incident-sourced cards using the tenant's Queue Urgency Mapping.
- [ ] A Card-role Activity type/value with no Queue Urgency Mapping entry appears in an "Unclassified" tier, ordered by time, rather than erroring or being hidden.
- [ ] A new Call appearing, or a nested Dispatch's phase changing, is reflected in an open queue view within the defined propagation-lag bound, with no manual refresh required.
- [ ] Selecting a card navigates to that Activity's own detail view; selecting a Feed line navigates to that Feed Activity's own record instead. No action is performed by the console itself either way.
- [ ] A Dispatcher can save a personal Queue View and have it be their default on next login; a Tenant Admin-locked default view cannot be overridden by a narrower personal preference.
- [ ] A viewer without RBAC/ABAC access to a given site's Activities never sees that site's entries in their queue, regardless of filter settings.
- [ ] A Records Admin can observe read-model projection lag/health for the queue.

## Open Questions

- Exact urgency-tier vocabulary and count (e.g., 4 tiers vs. 5) and default out-of-box Queue Urgency Mapping — pending UX/content design.
- Whether a just-cleared card (or a promoted-from-feed card) should linger briefly in the queue (a "recently cleared" grace display) before disappearing, versus dropping instantly — not committed here; a UI-polish decision for technical spec.
- **Genuine transitive (2+ level) Feed nesting remains unsolved.** Guard Tour's real chain is Route Assignment → Patrol → Checkpoint Scan; this doc resolves it specifically by making the middle tier (Patrol) the Card and the top tier (Route Assignment) a separate rollup Card with no Feed children at all — sidestepping the need for transitive nesting via a rollup-field summary rather than solving the general case. If a future module needs a genuine card→feed→feed structure where a true intermediate detail line (not just a summary stat) is needed at the middle tier, this doc's registration model needs a real extension, not assumed to just work.
- Whether Dispatcher (single-site) vs. Supervisor/EOC (multi-site) default scope needs its own explicit Settings & Preferences-registered policy, or is fully derived from each role's existing RBAC/ABAC site access — current default assumes the latter (no separate scope-policy concept introduced), revisit if a real gap surfaces.
- Whether future modules registering a new Activity extension should be expected/required to also register a Queue Role and a Queue Urgency Mapping entry, or whether `none`-by-default is an acceptable permanent state for most types — current default leans toward `none` being genuinely fine (most Activity types, like DAR entries, aren't board-relevant), with `card`/`feed` registration being a deliberate opt-in a module author considers, not a required step.
- Precise UI hand-off mechanics between this queue and Multi-Incident Console — deferred to that doc.
