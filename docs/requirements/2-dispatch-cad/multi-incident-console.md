# Multi-Incident Console

**Module:** 2 Dispatch/CAD
**Status:** Draft — elicited, ready for technical spec

## Overview

A **dockable, tabbed, IDE-style multi-panel workspace** for a Dispatcher juggling several concurrent situations at once — genuinely richer than Active Incident Queue's single flat list/card view, and built on top of it rather than duplicating it. Panels are pluggable via a **Panel Registry**, day one covering: **Map** (embeds GIS & Mapping Services' existing map view), **Queue** (embeds Active Incident Queue's read-model, pre-filtered per panel instance — e.g. a "Call Queue" panel is just a Queue panel filtered to Call cards), **Kanban** (a new grouped-column visualization over that same Active Incident Queue Card data, grouped by status by default), **Unit Roster** (a live list of on-duty officers and their current Unit State), and **Detail** (a specific Unit/Call/Incident/Location pinned as its own fully interactive working surface). Panels arrange into flexible dock zones, tab together within a zone, resize, and drag-rearrange.

**Panels are fully interactive, not previews.** A Detail panel is the pinned record's real working surface embedded in the layout — a Dispatcher adds an Incident Update, performs a Supervisor Review, logs a Dispatch phase, or invokes any registered Command/Action Bus action directly from the panel, reusing that record's owning module's own actions and rendering with no new per-type logic here. That's the actual point of a multi-incident workspace: work several situations at once without losing your place navigating between full pages.

**Layouts save and lock the same way Active Incident Queue's Queue Views already do** — a personal saved arrangement (Settings & Preferences identity chain), or a Tenant/Site Admin-defined, optionally locked default per role/site (location chain). Same mechanism, now applied to panel/dock configuration instead of filter config.

Two more pieces, both smaller and more self-contained:
- **Incident Escalation Control** — a one-click **Activate EOC Response** action on an Incident, confirmation-gated given its consequence, that creates a lightweight EOC Activation flag and publishes an automation-eligible domain event. Full EOC mechanics (staffing, wallboard, ICS roles) are explicitly out of scope — owned by Command Center (Module 3) and Emergency Management (Module 5), neither specified yet. This doc only establishes the trigger.
- **External Agency Handoff Log** — a thin Activity extension recording a handoff to an outside agency (local PD, fire), always resolving to a registered Organization Party (created inline if the agency isn't already registered — forcing pre-registration for a first-time handoff isn't realistic). Registers as a Feed-role type under Incident in Active Incident Queue, alongside Incident Update.

**This is the first feature needing a genuinely dockable multi-panel workspace.** Command Center Wallboard View (Module 3, not yet specified) looks like a strong candidate second consumer of the same Panel Registry/dock mechanism. Per the platform's established promotion pattern (a mechanism moves to Platform Core once a *second* real consumer actually needs it, not preemptively), this doc keeps the Panel Registry local for now — flagged here so it isn't rebuilt from scratch if/when Command Center confirms the same need.

## Actors & Roles

- **Dispatcher / Console Operator** — arranges panels, works multiple pinned records simultaneously, invokes Incident Escalation Control, logs Agency Handoff.
- **Supervisor** — same access, plus Activate EOC Response permission.
- **EOC Staff** — downstream consumer once an Incident is EOC-activated (full handoff mechanics deferred to Command Center/Emergency Management).
- **Tenant Admin** — defines/locks default layouts per role/site, configures EOC activation's step-up requirement.

## User Stories

- As a **Dispatcher**, I want a map, my call queue, my unit roster, and two Incidents I'm actively working all visible and interactive at once, instead of tabbing between full-page views and losing context.
- As a **Dispatcher**, I want to drag a call from "queued" to "dispatched" on a kanban board and have that actually trigger the real assignment flow, not just a visual reshuffle.
- As a **Dispatcher**, I want to log an Incident Update or perform a phase check directly from a pinned panel without navigating away from my whole layout.
- As a **Supervisor**, I want a saved default layout for my shift that a Dispatcher can start from and customize, rather than everyone rebuilding their workspace from scratch every login.
- As a **Supervisor**, I want one click to activate a full EOC response on a fast-developing Incident, with a confirmation step so it's never triggered by accident.
- As a **Dispatcher**, I want to log which outside agency an incident was handed off to, with a real contact and timestamp, for accountability and follow-up.

## Functional Requirements

### Panel Registry & dock
1. A **Panel Registry** defines the pluggable panel types available to this console. Day one: `map`, `queue`, `kanban`, `unit_roster`, `detail`. Future features may register additional panel types against this registry.
2. Panels arrange into dock zones (e.g., left/right/top/bottom/center), tab together within a zone, resize, and rearrange via drag — with a full non-drag keyboard-accessible alternative for every rearrangement action (see Non-Functional).
3. Multiple instances of the same panel type can be open simultaneously, each with its own instance config (e.g., two Detail panels pinning two different Incidents; two Queue panels with different filters).

### Panel types (day one)
4. **Map** panel embeds GIS & Mapping Services' existing map view — no new mapping logic introduced here.
5. **Queue** panel embeds Active Incident Queue's live read-model, with its own filter/sort/group config per instance (the same config shape as that doc's Queue View) — a "Call Queue" panel is simply a Queue panel pre-filtered to Call cards.
6. **Kanban** panel groups the same Active Incident Queue Card data into columns by a chosen dimension (default: status; also selectable: urgency tier, assigned unit). Dragging a card to a different column invokes that Activity's own existing status-transition or assignment action (e.g., dragging a Call card to a "Dispatched" column opens that Call's real assignment flow) — the kanban is a UI gesture over already-registered actions, never a new business rule of its own.
7. **Unit Roster** panel shows a live, filterable/sortable list of on-duty officers and their current Unit State (Status & State Monitors).
8. **Detail** panel pins a specific Card-role Activity (or an on-duty Unit) as a fully interactive working surface — every action, update, and status transition available on that record's own full view is available directly in the panel, reusing its owning module's actions and rendering with no new per-type logic here.

### Layouts
9. A Dispatcher can save a named panel arrangement as a personal layout (Settings & Preferences identity chain), starting from any admin default. A Tenant/Site Admin can define and optionally **lock** a default layout per role or site (location chain) — identical mechanism to Active Incident Queue's Queue Views, applied here to panel/dock configuration instead of filter config.

### Incident Escalation Control
10. **Activate EOC Response** registers as a Command/Action Bus action, invokable from an Incident's Detail panel or its Active Incident Queue card, gated by the platform's standard confirmation gate given it has no compensating action and real operational consequence. A tenant may additionally flag it as step-up-auth-required, per Authentication & Authorization's existing step-up mechanism.
11. Invoking it creates a lightweight **EOC Activation** record on the Incident and publishes an automation-eligible domain event — a Tenant Admin configures the actual notification/escalation behavior via Domain Events. Full EOC staffing/wallboard/ICS-role mechanics are explicitly out of scope, owned by Command Center (Module 3) and Emergency Management (Module 5) once those are specified.

### External Agency Handoff Log
12. **Agency Handoff Log** registers as a thin Activity extension: `origin_incident_ref` (direct field, fixed at creation, same non-EntityAssociation reasoning as Incident Update's own parent link), `receiving_agency_ref` (required, FK → Organization Party — created inline via Organization Registry's own record-creation flow if the agency isn't already registered), `contact_name`, `contact_info`, `handoff_time`, `notes`, `handed_off_by`.
13. Agency Handoff Log entries are immutable once created (a correction is a new entry, same discipline as Incident Update) and register as a **Feed**-role type under Incident in Active Incident Queue (retrofit, see Integrations), appearing alongside Incident Update entries on the Incident's card.

## Data Model / Fields

**Panel Registry** (local to this doc)
- panel_type_id, name (map, queue, kanban, unit_roster, detail), config_schema_ref, registered_by

**Console Layout** (Settings & Preferences registration)
- layout_id, tenant_id, owner_scope (a specific user, or a location-chain level for an admin default), name
- panel_instances[] (panel_type, dock_zone, tab_position, size, instance_config — e.g. a Queue panel's filter, a Kanban panel's group-by dimension, a Detail panel's target entity_id)
- locked (bool, admin-set)

**EOC Activation** (lightweight — full lifecycle deferred to Command Center / Emergency Management)
- activation_id, tenant_id, incident_ref
- activated_by, activated_at, step_up_verified (bool, when tenant-required)

**Agency Handoff Log** (thin Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- origin_incident_ref (direct field, fixed at creation)
- receiving_agency_ref (FK → Organization Party, required)
- contact_name, contact_info, handoff_time, notes, handed_off_by

## States & Transitions

**Console Layout:** created/edited freely by its owner; `locked` admin defaults block narrower override, same as Queue View's existing lock mechanic.

**EOC Activation:** created once, `active` — no further lifecycle owned here; downstream deactivation/resolution mechanics belong to Command Center/Emergency Management once specified.

**Agency Handoff Log:** created once, immutable.

## Integrations

- **Active Incident Queue (CAD Console)**: source of the Card read-model both Queue and Kanban panels render; Agency Handoff Log registers as a `feed`-role type under Incident (retrofit — a Queue Role Registration entry, same mechanism Incident Update already uses).
- **Command Center's Unified Operational Picture (UOP) Map**: source of the Map panel *(retrofit)* — the panel embeds that doc's full live composition (unit positions, Active Incident Queue activity pins, geofences, overlays), not a bare GIS & Mapping Services map view as originally described here; UOP Map registers itself as this Panel Registry's `map` type rather than the console defining a second, thinner map implementation.
- **Status & State Monitors**: source of the Unit Roster panel's live Unit State data.
- **Unit Dispatch & Proximity Routing, Call Intake & Logging, Incident Reporting & Management, Guard Tour & Checkpoint Verification, Patrol Management**: owning modules of whatever record a Detail panel pins — the panel reuses their own action surfaces and rendering unmodified.
- **Party Registry / Organization Registry**: source of `receiving_agency_ref`, including inline creation when the agency isn't already registered.
- **Command/Action Bus**: "Activate EOC Response," "Log agency handoff," and every panel/layout management action register as invokable actions across every surface; EOC activation reuses the existing confirmation gate (and, where tenant-flagged, step-up auth) rather than inventing new gating logic.
- **Authentication & Authorization**: source of the optional step-up requirement a tenant can flag on EOC activation.
- **Domain Events / Notifications Engine**: EOC activation publishes an automation-eligible event; actual notification/escalation behavior is Tenant Admin-configured, not hardcoded here.
- **Settings & Preferences**: owns Console Layouts (personal and admin-locked).
- **Command Center — Command Center Wallboard View (Module 3, future)**: a likely second consumer of this doc's Panel Registry/dock mechanism — if confirmed when that module is specified, promote the mechanism to Platform Core rather than rebuilding it there.
- **Command Center / Emergency Management (Modules 3 & 5, future)**: intended eventual owners of full EOC activation mechanics once an Incident's EOC Activation record exists — forward reference only, not built here.

## Permissions

| Action | Dispatcher | Supervisor | Tenant Admin |
|---|---|---|---|
| Arrange panels, save a personal layout | ✅ | ✅ | ✅ |
| Define/lock an admin default layout | ❌ | ❌ | ✅ |
| Add an Agency Handoff Log entry | ✅ | ✅ | ❌ |
| Activate EOC Response (confirmation-gated, optionally step-up) | ❌ | ✅ | ✅ |
| Configure EOC activation's step-up requirement | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Every panel's content must remain live/real-time, inheriting the freshness posture of its own source (Active Incident Queue's propagation-lag bound for Queue/Kanban, Status & State Monitors' own update cadence for Unit Roster) — this doc introduces no separate staleness budget.
- A Console Layout must persist across sessions/devices for its owner, reloading the same panel arrangement on next login.
- Panel rearrangement and Kanban card-column transitions must have a full keyboard-operable alternative to drag-and-drop (WCAG 2.1 requirement) — dragging can never be the only way to perform either action.
- Activate EOC Response must be genuinely hard to trigger by accident — the confirmation gate is mandatory regardless of tenant step-up configuration.
- WCAG 2.1 / Section 508 accessible dock/panel management, Kanban interaction, and Detail-panel action flows, day one.

## Acceptance Criteria

- [ ] A Dispatcher can open a Map, a Queue, a Unit Roster, and two Detail panels (for two different Incidents) simultaneously, each independently interactive.
- [ ] Adding an Incident Update from a Detail panel updates that Incident's real record with no separate navigation required.
- [ ] Dragging a Call card in a Kanban panel from "queued" to a "dispatched" column correctly invokes the real assignment flow rather than just moving the card visually.
- [ ] A Dispatcher can save a personal layout and have it reload on next login; a Tenant Admin-locked default layout cannot be overridden by a narrower personal arrangement.
- [ ] Every panel-rearrangement and Kanban drag action has a working keyboard-only equivalent.
- [ ] Invoking Activate EOC Response requires passing the confirmation gate before the EOC Activation record is created; with step-up configured, it also requires step-up verification.
- [ ] Activating EOC Response publishes a domain event a configured Domain Events rule can act on.
- [ ] Logging an Agency Handoff Log entry requires a resolved Organization Party — attempting to submit with an unregistered agency name prompts inline Organization creation rather than silently failing or accepting free text.
- [ ] An Incident's card in Active Incident Queue shows its Agency Handoff Log entries as Feed lines alongside its Incident Updates.
- [ ] Agency Handoff Log entries are immutable once created.

## Open Questions

- Exact panel-count/performance bounds (how many simultaneous panels before layout or data-freshness degrades) — a technical-spec-level concern.
- Exact drag-to-column business-rule mapping per Activity type in the Kanban panel (e.g., what happens if a dragged transition isn't actually valid for that record's current state) — needs a defined fallback (reject with explanation vs. open the normal action flow pre-filled), not fully specified here.
- Whether the Panel Registry/dock mechanism should promote to Platform Core once Command Center Wallboard View is specified and confirmed as a second consumer — flagged, not decided, per the platform's established promote-on-second-consumer pattern.
- Default EOC step-up requirement (on by default vs. tenant opt-in) — pending a broader Emergency Management posture once that module exists.
- Exact handoff-log content requirements beyond the fields specified (e.g., whether items/evidence transferred needs its own structured list rather than free-text notes) — deferred to Investigation Management's future evidence-tracking features if a real need surfaces.
