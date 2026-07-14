# ICS Role Mapping & Visual Org Chart

## Overview

This doc completes the lifecycle Multi-Incident Console deliberately left thin: **EOC Activation** is retrofitted from a lightweight trigger record into a full Activity extension (`active` → `deactivated`), and this doc builds the ICS command-staffing mechanism over it. It reuses the platform's established plan/execution split (Guard Tour's Route → Route Assignment) for command structure: a tenant-configurable **ICS Org Chart Template** (plan, versioned) is staffed via **ICS Role Assignment** — a heavy EntityAssociation Extension linking a Person to an ICS anchor, carrying `position_ref`/`start`/`end`, current-holder-plus-history shaped exactly like `CustodyAssociation`/`ConveyanceOwnerAssociation`. **Incident Command Post Registry** needs no new Location extension — a command post is a role a place plays for an activation's duration, captured as a lightweight time-bound designation association over Location Registry's existing geometry.

ICS adoption is deliberately two-tiered, not one-size-fits-all: **Full ICS** (the entire Section/Branch hierarchy) stands up only via EOC Activation, exactly as originally scoped — available, never forced. **Limited ICS** (a small, tenant-customizable subset of Positions — typically Incident Commander, Communications, and Support) can be assigned directly to **any Incident**, with no EOC Activation prerequisite at all. This matters for tenants with a mandatory NIMS/ICS adoption posture (a DOE national lab, most notably) that need at minimum-viable command accountability on ordinary incidents that never escalate to a full EOC response. An **ICS Adoption Policy**, tenant-configured, governs whether Limited ICS is merely available (default, every other target customer) or expected on every Incident.

Under `mandatory_limited`, enforcement lives at the point an officer is actually associated with the Incident, not as a separate closure-time check — this is the one seam this doc narrows relative to its first draft, not a reduction in ICS Role Assignment's own weight (position_ref/start/end, confirmation gating, and full audit history all stay exactly as specified below, for both scopes). Concretely: the officer who creates/self-initiates the Incident is automatically given an ICS Role Assignment for Incident Commander — the same automatic-default moment Incident Reporting & Management already uses for its plain `reporting_officer` tag, just resolving to a real ICS Role Assignment instead, for a mandatory-adoption tenant. Associating any *additional* officer or unit with that Incident then requires explicitly picking which Limited Position they're filling — the association cannot complete silently as a generic "responding officer" the way it does at an `optional`-policy tenant. A minor incident (e.g., a trespasser who leaves on challenge, no additional units needed) gets its IC for free at creation and needs nothing else — there's no separate step to forget and nothing left to check later.

## Actors & Roles

- **Incident Commander / Section Chiefs / other ICS role holders** — real Person entities; may be platform users or inline-created external/mutual-aid personnel.
- **EOC Coordinator / Supervisor** — activates/deactivates the EOC Activation, assigns/relieves ICS roles (Limited or Full), designates/relocates the Command Post.
- **Site / Tenant Admin** — authors and versions the ICS Org Chart Templates (Full and Limited), sets the tenant's ICS Adoption Policy.
- **Dispatcher** — read-only viewer of the org chart for situational awareness.

## User Stories

- As an **EOC Coordinator**, when I activate EOC Response I want an ICS org chart stood up from a reusable template, not built from scratch each incident.
- As a **Supervisor**, I want to assign a specific person to Incident Commander — myself or on someone's behalf — with the assignment visible on the chart and its own start/end history.
- As an **Incident Commander**, I want to relieve myself and hand off to another person with a clean handoff record, not an ambiguous overlap.
- As a **Dispatcher**, I want to see who currently holds each ICS role from the Queue/Detail view or a Wallboard org-chart panel.
- As an **EOC Coordinator**, I want to designate a facility as the Incident Command Post and relocate it later if needed, with full history preserved.
- As a **Supervisor**, I want deactivating the EOC Activation to automatically close out every open role assignment and command post designation — no stale "still active" command staff after the incident stands down.
- As a **national-lab Tenant Admin** operating under a mandatory ICS adoption policy, I want a small, customizable subset of ICS roles (Incident Commander, Communications, Support) assignable to any Incident directly — not gated behind a full EOC activation — so ordinary incidents that never escalate still get minimum-viable command accountability.
- As a **Guard** who creates an Incident at a mandatory-adoption tenant, I want to become Incident Commander automatically the moment I log it — I shouldn't have to take a second, separate action just to satisfy a compliance requirement on a trespasser who already left.
- As a **Dispatcher** adding a second officer to an Incident at a mandatory-adoption tenant, I want to be required to pick their ICS role right then, not discover later that nobody ever did.

## Functional Requirements

### EOC Activation lifecycle (retrofit)
1. EOC Activation is promoted from Multi-Incident Console's lightweight record to a full Activity extension — the same "the moment it has a real start/complete lifecycle, it's an Activity" rule already applied to Route Assignment. States: `active` → `deactivated` (Supervisor/EOC Coordinator action, confirmation-gated given operational consequence).
2. Deactivating an EOC Activation auto-closes (sets `end`) every still-open ICS Role Assignment and Command Post Designation against it.

### ICS Org Chart Template (plan layer)
3. A tenant-configurable **ICS Org Chart Template**, one of two independently authored **scopes**: **`full`** (the entire Section/Branch hierarchy, seeded from a standard ICS/NIMS starter set) and **`limited`** (a small subset — seeded default: Incident Commander, Communications, Support). Both are versioned sets of **Position** definitions (title, section — command/operations/planning/logistics/finance_admin, `parent_position_ref` for reporting line, `is_command_staff` bool). A tenant customizes either scope's Position set freely — Limited is not required to be a literal subset of Full's Positions, though it typically will be in practice. Exact starter vocabulary for either scope is a content/UX concern, not committed here.
4. Template edits version rather than mutate in place (same discipline as Route/Tour Definition) — a standing chart pins the template version it started against; a later template edit never retroactively changes an in-progress chart's structure.

### ICS Adoption Policy
5. A tenant-level **ICS Adoption Policy** (Settings & Preferences-registered) governs Limited ICS's posture: **`optional`** (default — officers/units associate with an Incident via Incident Reporting & Management's existing plain `reporting_officer`/`responding_unit` participant roles, exactly as already specified there; ICS Role Assignment remains available as a separate, deliberate action for anyone who wants formal command accountability, but nothing defaults to it) or **`mandatory_limited`** (officer/unit association with an Incident *is* ICS Role Assignment — see FR #8 below; there is no parallel plain participant tag for officers at a mandatory-adoption tenant). Non-officer participants (witness, victim, suspect, involved vehicle, seized weapon) are entirely untouched by this policy either way.

### ICS Role Assignment (execution layer)
6. Standing up a **Full** chart happens only when EOC Response is activated, anchored to the EOC Activation, instantiating its Position set from the pinned template version. **Limited** Positions need no separate stand-up step — the tenant's current Limited template version is simply always available against any Incident.
7. **ICS Role Assignment** is an EntityAssociation Extension linking a Person (entity_id_a) to its ICS anchor (entity_id_b — either an **Incident**, for Limited ICS, or an **EOC Activation**, for Full ICS; the association layer doesn't care which concrete Activity type it points at, same as every other EntityAssociation), carrying `position_ref`, `start`, `end` (nullable while active), `assigned_by`. The current holder is simply the row with `end` null; history is the active/removed sequence — identical shape to `CustodyAssociation`. This is unchanged regardless of ICS Adoption Policy — the record's own weight (structured position reference, real start/end lifecycle, audit history) is the same whether it was created deliberately or by the default described next.
8. **The seam with the incident system, narrowed for `mandatory_limited` tenants**: rather than a plain participant tag plus a separate, disconnected ICS assignment step, associating an officer/unit with the Incident *is* creating an ICS Role Assignment. The officer who creates/self-initiates the Incident is automatically given one for Incident Commander — the same automatic-tagging moment Incident Reporting & Management already uses for its plain `reporting_officer` default (retrofit — see Integrations), just resolving to a real ICS Role Assignment instead of a flat participant role for a mandatory-adoption tenant. Associating any additional officer or unit with that Incident requires explicitly specifying which Limited Position they're filling as part of that same action — it cannot complete as a generic, position-less "responding officer" the way it silently does at an `optional`-policy tenant. Because Incident Commander is filled automatically the moment any officer is attached at all, there is nothing left to verify later — no separate closure-time check is needed (this doc's first draft proposed one; it's retracted here as unnecessary once enforcement moved to the point of association).
9. Assigning or relieving a role — whether via the default above or a deliberate action — is self- or on-behalf-of, the platform's established posture (Patrol starts, Safety Check-ins, Dispatch phases). Assigning/relieving a **Command Staff** Position (`is_command_staff = true` — Incident Commander, Section Chiefs) additionally passes the standard confirmation gate given real operational consequence; a tenant may flag it step-up-required. Non-Command-Staff assignment is a lighter action, no gate.
10. Only one active ICS Role Assignment per Position per anchor (Incident or EOC Activation) at a time — an explicit relieve-then-assign, never a silent overwrite, the same constrain-multiplicity-at-the-source discipline already applied to Dispatch Stacking Policy.
11. A role holder always resolves to a real, registered Person entity. A first-time mutual-aid/external role holder is inline-created, the same posture Agency Handoff Log already established for an unregistered Organization.
12. An Incident that later triggers Activate EOC Response gets a separate, fresh set of Full ICS Role Assignments anchored to the new EOC Activation — its prior Limited ICS Role Assignments (anchored to the Incident itself) remain intact and historically visible, never migrated or merged automatically. Re-assigning the same person to the equivalent Full Position is an ordinary manual assignment, not an automatic carry-forward (see Open Questions).

### Incident Command Post Registry
13. A Command Post is an ordinary Location (no new extension type — existing geometry/coordinates reused wholesale) plus a lightweight, time-bound **Command Post Designation** (EOC Activation ↔ Location, `start`/`end`). Relocating the command post ends the current designation and starts a new one against a different (or reused) Location, preserving full history rather than overwriting a single pointer. Command Post Designation is a Full-ICS-only concept — a Limited ICS chart on an ordinary Incident has no command post of its own.

### Visual Org Chart
14. The org chart renders the Position hierarchy (reporting lines via `parent_position_ref`) with each Position's current holder, or explicitly vacant, for whichever anchor (Incident or EOC Activation) it's pinned to. A new **`org_chart`** panel type registers into the shared Panel Registry (the catalog Multi-Incident Console and Command Center Wallboard View already promoted) — selectable in a personal Console Layout, a Wallboard Display Profile zone, or this doc's own dedicated view, with zero new panel infrastructure.
15. A vacant Position renders visibly as vacant, never silently omitted — an EOC Coordinator or Supervisor staffing a chart needs to see the gap, not infer it.

## Data Model / Fields

**EOC Activation** (retrofit — now a full Activity extension; entity_id is the shared PK)
- incident_ref, activated_by, activated_at, step_up_verified
- status (active, deactivated), deactivated_by, deactivated_at
- org_chart_template_version_ref (pinned at chart stand-up)

**ICS Adoption Policy** (Settings & Preferences registration, tenant-level)
- tenant_id, policy (optional, mandatory_limited)

**ICS Org Chart Template** (tenant Definition, versioned)
- template_id, tenant_id, scope (full, limited), version, name, status (active, archived)

**Position**
- position_id, template_version_ref, title, section (command, operations, planning, logistics, finance_admin), parent_position_ref (nullable), is_command_staff (bool)

**ICS Role Assignment** (EntityAssociation Extension — entity_id_a = Person, entity_id_b = Incident [Limited scope] or EOC Activation [Full scope]; association_id is the shared PK)
- position_ref, start, end (nullable — null means current holder), assigned_by
- *(at a `mandatory_limited` tenant, this record — not a plain `ActivityParticipantAssociation` role — is what an officer/unit's presence on an Incident resolves to; see FR #8 and the retrofit to Incident Reporting & Management's FR #4)*

**Command Post Designation** (EntityAssociation — entity_id_a = EOC Activation, entity_id_b = Location; association_id is the shared PK)
- start, end (nullable — null means current designation), designated_by

## States & Transitions

- **EOC Activation:** `active` → `deactivated` (terminal; cascades close to open Full-ICS Role Assignments and Command Post Designations).
- **ICS Org Chart Template:** `active` → `archived` per version, same as any versioned Definition, independently for each scope.
- **ICS Role Assignment / Command Post Designation:** no separate status field — current vs. historical is purely `end is null`, same discipline as Custody's active/removed sequence.
- **Incident (retrofit — see Integrations):** no lifecycle change — under `mandatory_limited`, compliance is guaranteed structurally at the moment an officer is first associated with the Incident (FR #8), so Supervisor Review's existing sign-off gate is untouched by this doc.

## Integrations

- **Multi-Incident Console**: source of EOC Activation (retrofit to a full Activity extension here) and the shared Panel Registry this doc contributes `org_chart` to; Activate EOC Response now creates the richer Activity-extension record and is the sole trigger for Full ICS.
- **Incident Reporting & Management** *(retrofit)*: FR #4's automatic `reporting_officer` tag-on-creation, and its plain `responding_unit` participant role for additional officers, are both superseded by ICS Role Assignment for officers/units at a `mandatory_limited` tenant — see that doc's own retrofit note. Non-officer participant roles (witness, victim, suspect, involved_vehicle, seized_weapon) are entirely unaffected.
- **Entity Registry Core / Party Registry / Person Registry**: ICS Role Assignment's Person side, including inline creation for unregistered external/mutual-aid personnel.
- **Location Registry**: Command Post Designation's Location side — geometry/coordinates reused wholesale, no new extension.
- **Command/Action Bus**: assign/relieve role (Limited or Full), deactivate EOC Activation, and relocate command post all register as actions; Command Staff assignment reuses the existing confirmation gate (and step-up where tenant-flagged), no new gating logic.
- **Command Center Wallboard View**: `org_chart` is selectable in a Display Profile zone — e.g. permanent EOC-room signage during an activation.
- **Active Incident Queue**: EOC Activation, now a real Activity, is eligible for optional queue role registration (`card`) at Supervisor/EOC discretion — a light retrofit, not mandatory. Limited ICS Role Assignments render from the Incident's own existing card, no separate queue entry.
- **Settings & Preferences**: owns the ICS Adoption Policy registration.
- **After-Action Reports (Module 5, not yet specified)**: forward reference — command-history reconstruction ("who held what role when") reads ICS Role Assignment's start/end directly, across both scopes; no new mechanism anticipated when that module is specified.
- **Personnel / a future certification-tracking feature (Module 8, not yet specified)**: forward reference only — role assignment does not gate on ICS certification/qualification data today, since none exists yet, the same deferred posture Citation used for `requires_citation_authority`.

## Permissions

- **Activate/deactivate EOC Activation**: existing Multi-Incident Console permission, unchanged.
- **Author/version an ICS Org Chart Template (either scope) or set the tenant's ICS Adoption Policy**: Site/Tenant Admin.
- **Assign/relieve a Role Assignment (Limited or Full)**: EOC Coordinator/Supervisor; self-assignment permitted for non-Command-Staff Positions. Command Staff assignment/relief additionally requires passing the confirmation gate (and step-up where tenant-flagged).
- **View the org chart**: inherits the existing RBAC/ABAC posture of the Incident/EOC Activation itself — no new permission introduced.
- **Designate/relocate the Command Post**: EOC Coordinator/Supervisor.

## Non-Functional / Constraints

- Org chart updates (assignment, relief, deactivation) propagate via the Live Update Channel at the platform's standard ≤2s console target, same as every other live board.
- No new audit-tier event type is introduced — ICS Role Assignment and Command Post Designation writes are ordinary EntityAssociation changes, already covered by Structured Logging & Audit Trails.

## Acceptance Criteria

- [ ] Activating EOC Response stands up an org chart from the tenant's current ICS Org Chart Template version, and pins that exact version to the EOC Activation.
- [ ] Assigning a person to a Position that already has an active holder is rejected/blocked until an explicit relieve step runs first — it never silently overwrites the prior holder.
- [ ] Deactivating an EOC Activation closes every open ICS Role Assignment and Command Post Designation tied to it in the same action.
- [ ] A first-time external/mutual-aid role holder is inline-created as a real Person entity — the flow never accepts a plain-text name in its place.
- [ ] Relocating a Command Post ends the current designation and creates a new one; both remain independently queryable as history.
- [ ] The `org_chart` panel type is selectable in Multi-Incident Console's Console Layout and Command Center Wallboard View's Display Profile identically, both drawing from the same shared Panel Registry catalog.
- [ ] A vacant Position renders visibly as vacant on the org chart, not omitted from the display.
- [ ] Assigning/relieving a Command Staff Position requires passing the confirmation gate before the change commits; a non-Command-Staff Position does not.
- [ ] A Limited ICS Position can be assigned directly to any Incident with no EOC Activation in effect.
- [ ] At an `optional`-policy tenant, officers/units associate with an Incident via the plain `reporting_officer`/`responding_unit` participant roles exactly as Incident Reporting & Management already specifies, with zero ICS Role Assignments unless someone deliberately creates one.
- [ ] At a `mandatory_limited`-policy tenant, the officer who creates an Incident is automatically given an ICS Role Assignment for Incident Commander with no separate action required.
- [ ] At a `mandatory_limited`-policy tenant, associating an additional officer/unit with an Incident cannot complete without an explicit Limited Position selection — there is no silent generic "responding officer" fallback.
- [ ] A minor Incident with only an Incident Commander assigned (e.g., a trespasser who left on challenge) has nothing further required of it — no separate check blocks Supervisor Review sign-off, because there was never anything left unresolved.
- [ ] An Incident's existing Limited ICS Role Assignments remain queryable unchanged after that Incident triggers Activate EOC Response and a separate Full ICS chart stands up.

## Open Questions

- Exact starter ICS/NIMS Position vocabulary and default template content, for both scopes — a content/UX design task, not committed here.
- Whether ICS certification/qualification gating should block Position assignment once a Personnel/certification feature (Module 8) exists — forward reference only, not solved here.
- Whether the Command-Staff-vs-not confirmation-gating split should become tenant-configurable per Position rather than the fixed `is_command_staff` boolean — flagged, not resolved.
- Exact UX for "requires explicit Position selection" on an additional officer/unit — whether the association action simply can't submit without it, or a picker auto-opens the moment a second officer is added — a technical-spec/UX-flow decision, not committed here.
- Whether an Incident escalating to full EOC Activation should offer a one-click "carry forward" convenience (pre-filling the Full chart's equivalent Position with whoever already held the matching Limited role) rather than requiring a fresh manual assignment — a UX nicety, not committed here (see FR #12).
