# EOC Logistics Hub

## Overview

EOC Logistics Hub is the resource-coordination console for an active EOC Activation — resource requests (generators, barricades, vehicles, bulk supplies) tracked request → assign → deploy → return, and a directory of external mutual-aid organizations with basic agreement terms. It reuses established mechanisms throughout rather than inventing new ones: Patrol Management's ad hoc Patrol Request precedent for the Resource Request lifecycle (an Activity extension anchored to the EOC Activation), Active Incident Queue's Queue Role Registration plus Multi-Incident Console's Kanban panel for the live booking board (no new board mechanism), Item Registry's custody tracking for individually-tracked resources (full identity/dedup/audit, same treatment as Vehicle and Camera), and Agency Handoff Log's inline-Organization-Party pattern for Mutual Aid Registry entries.

The full tenant-wide resource catalog (procurement, maintenance schedules) and the formal mutual-aid contract/approval lifecycle are both explicitly Module 5's future **Resource Logistics Catalog** and **Mutual Aid Agreements Tracker**, neither built yet. This doc's Resource Type Definition and Mutual Aid Organization records are deliberately interim stand-ins — richer than a bare pointer, per explicit user direction on the mutual-aid side, but still flagged for reconciliation once those modules exist, the same deferred-integration posture DAR's Shift Window and Live Camera Feed Ingestion's VMS-reference fields already established.

**Superseded (retrofit): [Resource Logistics Catalog](../5-emergency-management/resource-logistics-catalog.md) is now the real mechanism this doc's Resource Type Definition stood in for.** `Resource Type Definition` is replaced by that doc's FEMA/NIMS-taxonomy `Resource Definition`; individually-tracked resources now register as a `Catalog Item` Item extension there, and that extension's `deployment_status` auto-derives from this doc's own Resource Request phase (assigned/in_transit → staged, deployed → deployed, returned/cancelled → available) — a retrofit hook, not a change to Resource Request's own request→assign→deploy→return lifecycle below, which stays exactly as specified. Bulk Resource Stock's field shape is unchanged, just now owned there as the real mechanism instead of a local stand-in. Mutual Aid Organization is untouched — Mutual Aid Agreements Tracker remains unbuilt.

## Actors & Roles

- **Logistics Section Chief** (via their current ICS Role Assignment, `section = logistics`) — primary resource-request approver/assigner, manages the Mutual Aid Registry.
- **EOC Coordinator / Supervisor** — approves/assigns on the Logistics Section Chief's behalf when the position is unfilled, same self-or-on-behalf-of posture used throughout the platform.
- **Requesting officer / Section Chief** (any ICS role holder) — submits a Resource Request.
- **Site / Tenant Admin** — configures Resource Type Definitions.

## User Stories

- As a **Section Chief**, I want to request a generator for my area and see its status move from requested to staged to deployed on a live board.
- As **Logistics Section Chief**, I want bulk consumables (barricades, water cases) tracked by quantity without the overhead of tagging each individual unit.
- As **Logistics Section Chief**, I want to check an external mutual-aid organization's capabilities and current agreement terms before calling them for support.
- As an **EOC Coordinator**, I want resource requests to show up on the same Kanban board I already use for incident status, not a separate disconnected tool.
- As a **Tenant Admin**, I want to define which resource types are individually tracked (vehicles, generators) versus bulk (sandbags) once, not per request.
- As **Logistics Section Chief**, I want assigning a specific generator to a request to automatically transfer its custody, not require a separate manual custody update.

## Functional Requirements

### Resource Type Definition
1. A tenant-configurable **Resource Type Definition** (name, unit_of_measure, `is_individually_tracked` bool) is this doc's interim resource catalog stand-in — explicitly flagged for reconciliation with Module 5's future Resource Logistics Catalog, not built here.
2. `is_individually_tracked = true` types (generators, vehicles) are backed by real Item Registry instances (or existing Vehicle instances) — full identity/dedup/custody/audit treatment inherited unmodified. `is_individually_tracked = false` types (barricades, water cases) are tracked as a simple local quantity counter — no Item Registry involvement at all, consistent with not forcing weight onto something that doesn't need dedup/custody history.

### Resource Request (execution layer)
3. **Resource Request** registers as an Activity extension anchored to the EOC Activation — `resource_type_ref`, `quantity_requested` (bulk) or `assigned_item_ref` (individually-tracked, once assigned), `requested_by`, `staging_location_ref`, status (`requested` → `assigned` → `in_transit` → `staged` → `deployed` → `returned`/`consumed` | `cancelled`) — the same request→assign→fulfill shape Patrol Management's ad hoc Patrol Request already established, applied here to physical resources instead of patrol tasking.
4. Assigning an individually-tracked Resource Request to a specific Item triggers an ordinary Item Registry custody transfer (current holder becomes the requesting party/location) — no new custody mechanism; returning the resource is the corresponding custody transfer back.
5. A bulk Resource Request's quantity is simply reserved against the Resource Type's tracked total at assignment and released on return/consumption — no custody chain, since a bulk resource has no individual identity to track.
6. Resource Request registers a **card** Queue Role (Active Incident Queue's existing registration mechanism) so it's live-board-visible, and is Kanban-groupable by status in Multi-Incident Console's existing Kanban panel — no new board mechanism; dragging a card between columns invokes the same status-transition action as any other Kanban-managed Activity.

### Mutual Aid Registry
7. A **Mutual Aid Organization** entry always resolves to a registered Organization Party (inline-created if not already registered) — the same mandatory-real-entity, inline-creation discipline Agency Handoff Log already established for external agencies, never free text.
8. Each entry carries tenant-configurable capability tags (e.g., "Heavy Equipment," "Water Rescue," "Additional Personnel") and basic agreement fields (`effective_start`, `effective_end`, `scope_notes`) — richer than a bare directory entry, but explicitly flagged for reconciliation with Module 5's future Mutual Aid Agreements Tracker, which will own the fuller contract/approval/renewal lifecycle.
9. An expired agreement (`effective_end` passed) is surfaced, not hidden — a Logistics Section Chief should see a lapsed mutual-aid relationship rather than silently losing visibility into it.

## Data Model / Fields

**Resource Type Definition** (tenant Definition)
- type_id, tenant_id, name, unit_of_measure, is_individually_tracked (bool), status (active, archived)

**Bulk Resource Stock** (local — only for `is_individually_tracked = false` types)
- stock_id, resource_type_ref, tenant_id, site_ref, total_quantity, reserved_quantity

**Resource Request** (Activity extension; entity_id is the shared PK)
- resource_type_ref, quantity_requested (nullable — bulk only), assigned_item_ref (nullable — individually-tracked only)
- eoc_activation_ref, requested_by, staging_location_ref (nullable)
- status (requested, assigned, in_transit, staged, deployed, returned, consumed, cancelled)

**Mutual Aid Organization** (local — wraps an Organization Party reference)
- entry_id, organization_ref (FK → Organization Party, inline-creatable), tenant_id
- capability_tags[], effective_start, effective_end (nullable), scope_notes

## States & Transitions

- **Resource Type Definition:** `active` → `archived`, standard Definition lifecycle.
- **Resource Request:** `requested` → `assigned` → `in_transit` → `staged` → `deployed` → `returned` | `consumed` (bulk-consumable path); `cancelled` reachable from any point before `deployed`.
- **Mutual Aid Organization entry:** no separate status field — currency is derived from `effective_start`/`effective_end` (a nullable end means an ongoing agreement).

## Integrations

- **Item Registry / Vehicle-Conveyance Registry**: source of individually-tracked resource instances and their custody mechanism, reused unmodified.
- **Patrol Management**: source of the request→assign→fulfill Activity-extension precedent this doc applies to physical resources.
- **Active Incident Queue / Multi-Incident Console**: Resource Request's Queue Role Registration (`card`) and Kanban-panel groupability, both mechanisms reused wholesale.
- **ICS Role Mapping & Visual Org Chart**: the Logistics Section Chief's current ICS Role Assignment is the natural request-approver identity; `staging_location_ref` may reference the same Location as Command Post Designation or a distinct staging area.
- **Party Registry / Organization Registry**: Mutual Aid Organization's Organization Party side, including inline creation.
- **Command/Action Bus**: Request/Assign/Deploy/Return a resource, and Add/Edit a Mutual Aid Organization entry, all register as actions.
- **Module 5 — Resource Logistics Catalog and Mutual Aid Agreements Tracker (not yet specified)**: forward reference — both Resource Type Definition and Mutual Aid Organization's agreement fields are explicit interim stand-ins, flagged for reconciliation once those modules exist.

## Permissions

- **Configure Resource Type Definitions**: Site/Tenant Admin.
- **Submit a Resource Request**: any ICS role holder on the active EOC Activation.
- **Approve/assign/deploy/return a Resource Request**: Logistics Section Chief (current ICS Role Assignment), or EOC Coordinator/Supervisor on-behalf-of.
- **Add/edit a Mutual Aid Organization entry**: Logistics Section Chief or Tenant Admin.
- **View resource requests / the Mutual Aid Registry**: inherits the EOC Activation's existing RBAC/ABAC posture — no new permission introduced.

## Non-Functional / Constraints

- Resource Request updates propagate via the Live Update Channel at the platform's standard ≤2s console target, same as every other live board.
- Bulk quantity reservation/decrement is a simple counter operation — no distributed-locking/oversell concern is solved here beyond ordinary transactional consistency (flagged if real contention surfaces at scale, not expected to be an issue at this feature's scope).
- No new audit-tier event type is introduced — Resource Request transitions and Mutual Aid Organization edits are ordinary Activity/EntityAssociation writes, already covered by Structured Logging & Audit Trails.

## Acceptance Criteria

- [ ] An individually-tracked resource type's instances are real Item Registry records with full custody/dedup/audit treatment identical to Vehicle.
- [ ] A bulk resource type tracks only a quantity counter — no Item Registry record is ever created for it.
- [ ] Assigning an individually-tracked Resource Request to a specific Item transfers its custody automatically; no separate manual custody update is required.
- [ ] A Resource Request appears on the same Kanban board as Incident cards, groupable by its own status column, using the existing Kanban panel with zero new board logic.
- [ ] Adding a Mutual Aid Organization entry for an unregistered agency prompts inline Organization creation — the flow never accepts free text in its place.
- [ ] An expired Mutual Aid Organization agreement (past `effective_end`) remains visible in the registry, clearly marked expired rather than hidden.
- [ ] Cancelling a Resource Request at any point before `deployed` correctly releases its reserved bulk quantity or un-assigns its Item.

## Open Questions

- Exact reconciliation path once Module 5's real Resource Logistics Catalog and Mutual Aid Agreements Tracker are specified — how Resource Type Definition and Mutual Aid Organization's interim fields migrate into those richer mechanisms — not solved here.
- Whether bulk resource reservation needs real concurrency control (e.g., two simultaneous requests over-committing the last few units) at genuinely large-campus scale — flagged, not resolved; current design assumes ordinary transactional consistency is sufficient.
- Whether a Resource Request should support partial fulfillment (e.g., 10 barricades requested, only 6 available) as a first-class state rather than requiring two separate requests — not committed here.
