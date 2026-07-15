# Resource Logistics Catalog

## Overview

Resource Logistics Catalog builds the real, tenant-wide resource catalog EOC Logistics Hub explicitly deferred — that doc's `Resource Type Definition` and Bulk Resource Stock reservation logic were deliberately interim stand-ins, flagged for reconciliation once this feature existed. This doc supplies three things MODULES.md named and EOC Logistics Hub couldn't: a real **FEMA/NIMS-grounded resource taxonomy** (Category → Kind → Type/Tier, the same "check the real standard before inventing a field list" discipline already applied to NIEM Core for entities), a **Deployment Status dashboard** (`available`/`staged`/`deployed`/`decommissioned`), and **Reorder Warnings** for supply counts that fall below a configured minimum.

Three elicited decisions shape the doc:

1. **The catalog is a tenant-wide standing inventory, not an EOC-activation-scoped extension.** A supply room running low on barricades is a day-to-day readiness concern independent of whether an EOC is active — EOC Logistics Hub's Resource Request becomes one consumer (the EOC-time request→assign→deploy→return execution layer) drawing from this always-on catalog, not the catalog's owner.
2. **Individually-tracked resources are a new Item extension — `Catalog Item`** — reusing the platform's multi-extension allowance (one base Entity can carry several simultaneous extensions) rather than retrofitting Item Registry's base table or inventing a parallel tracking record. A fleet ambulance can be both a `Vehicle` extension (driving/plate data) and a `Catalog Item` extension (resource taxonomy, deployment status) on the same underlying Item — no conflict, no new mechanism.
3. **Deployment Status auto-derives from Resource Request's existing phase**, the same "state transitions in lockstep with a driving record" pattern Status & State Monitors already established for Unit State against Dispatch phase — never an independently, manually-toggled field that could drift out of sync while a request is open. `decommissioned` is the one genuinely independent, manual, admin-only transition.
4. **Reorder Warnings introduce a new, deliberately narrow mechanism — Stock Threshold Alert** — the platform's first *quantity*-threshold alert, distinct from Duration Watchdog's *duration*-threshold shape. Built local to this doc rather than forced into Duration Watchdog's time-based model; flagged as a plausible future generalization if a second quantity-threshold need appears (Ammunition Registry, Module 10), not pre-generalized now.

## Actors & Roles

- **Site / Tenant Admin** — authors Resource Definitions (the FEMA taxonomy), configures reorder thresholds, decommissions Catalog Items.
- **Logistics Section Chief** (via current ICS Role Assignment, when an EOC is active) — registers new Catalog Items, adjusts Bulk Resource Stock counts, receives reorder alerts.
- **Any user with catalog-view permission** — browses the readiness dashboard; submits an EOC Logistics Hub Resource Request drawing against this catalog.

## User Stories

- As a **Tenant Admin**, I want to define resource types using real NIMS Category/Kind/Type-Tier fields, so our readiness reporting matches what a mutual-aid partner or FEMA auditor actually expects.
- As a **Logistics Section Chief**, I want to see at a glance which Type-2 generators are available, staged, or already deployed, without cross-referencing individual Resource Requests myself.
- As a **Site Admin**, I want an automatic alert when our sandbag stock drops below the minimum we've set, so restocking happens before we're caught short.
- As a **Logistics Section Chief**, I want a resource's deployment status to update automatically the moment its Resource Request moves to staged or deployed — I shouldn't have to update two records for one real-world event.
- As a **Tenant Admin**, I want to decommission a generator that's failed inspection so it stops appearing as requestable, while keeping its full history intact.
- As an **EOC Coordinator**, I want the catalog's readiness dashboard available as a Wallboard panel during an activation, the same way every other live board is.

## Functional Requirements

### Resource Definition (FEMA/NIMS taxonomy)
1. A tenant-configurable **Resource Definition** carries `category` (law_enforcement, ems, fire, public_works, communications, equipment, supply, other — NIMS-seeded, tenant-customizable), `kind` (team, equipment, vehicle, supply), and an optional `type_tier` (1–4, capability-tiered per NIMS Resource Typing Library Tool convention — nullable, since not every resource warrants formal tiering, e.g. a generic supply). This replaces EOC Logistics Hub's interim flat `Resource Type Definition` *(retrofit — see Integrations)*.
2. `is_individually_tracked` carries over from EOC Logistics Hub's original shape unmodified: `true` routes to real Item Registry instances via the new **Catalog Item** extension (FR #3); `false` routes to a site-scoped **Bulk Resource Stock** quantity counter (FR #4) — no Item Registry involvement for bulk types, consistent with not forcing dedup/custody weight onto something that doesn't need it.

### Catalog Item (individually-tracked resources)
3. **Catalog Item** registers as a new Item Registry extension (`entity_id` shared PK, FK → Item) carrying `catalog_resource_definition_ref`, `deployment_status`, and `home_location_ref` (where it returns to when not deployed). Per the platform's existing multi-extension allowance, a Catalog Item registration can coexist with any other Item extension already carried by the same base Item (a Vehicle that's also a fleet emergency resource carries both extensions on one entity) — no new mechanism, an existing allowance simply exercised for the first time by this doc.
4. `deployment_status` (`available`, `staged`, `deployed`, `decommissioned`) **auto-derives from the item's currently-open EOC Logistics Hub Resource Request, if any** *(retrofit hook — see Integrations)*: `assigned`/`in_transit` → `staged`; `deployed` → `deployed`; `returned`/`cancelled` → `available`. With no open Resource Request, status defaults to `available`. This is never independently editable while a request is open — the same one-directional sync discipline Status & State Monitors already established for Unit State against Dispatch phase.
5. **Decommission** is the one manual, admin-only, confirmation-gated transition, reachable from any non-`decommissioned` status — terminal, removes the item from the requestable/available pool, but preserves its full Item Registry history (custody, prior deployments) unmodified. A decommissioned Catalog Item can never be the target of a new Resource Request.

### Bulk Resource Stock
6. **Bulk Resource Stock** (site-scoped, one row per `(Resource Definition, site)`) carries `total_quantity`, `reserved_quantity` — the same reservation shape EOC Logistics Hub already specified, now owned here as the real mechanism rather than a local stand-in — plus a new `reorder_threshold` (nullable; unset means no reorder alerting for that stock row).
7. `available_quantity` is always `total_quantity - reserved_quantity`, computed, never independently stored — reservation/release still happens exactly as EOC Logistics Hub described (reserved at Resource Request assignment, released on return/consumption/cancellation).

### Stock Threshold Alert (Reorder Warnings)
8. A **Stock Threshold Alert Registration** exists per `(Resource Definition, site or tenant-default)`, carrying a `minimum_available_count` — for bulk types, compared against Bulk Resource Stock's `available_quantity`; for individually-tracked types, compared against a live count of that Resource Definition's Catalog Items currently `available`. Either shape reuses the same registration/comparison mechanism, just a different counted source.
9. Crossing **below** the configured minimum fires a Domain Event (`resource_below_reorder_threshold`) routed through the Notifications Engine to the Logistics Section Chief / Site Admin; crossing back **above** it clears the alert. A `last_alert_state` (`ok`/`below_threshold`) field debounces delivery — the alert fires once on the crossing, not on every subsequent check while still below, the same storm-avoidance instinct Signal Disposition's collapse-to-one-record already applied to a different domain.
10. Reorder alerting is additive notification only — it never blocks a Resource Request, a Bulk Resource Stock reservation, or any other operational action, the platform's standing "escalate, don't block" rule.

### Deployment Status Dashboard
11. A live, filterable read-model (by Category/Kind/Type-Tier, site, `deployment_status`) registers a new **`resource_catalog`** panel type into the shared Panel Registry (the catalog Multi-Incident Console and Command Center Wallboard View already promoted) — selectable in a personal Console Layout or a Wallboard Display Profile zone, zero new panel infrastructure, the same registration-only cost every prior Panel Registry contributor paid.

## Data Model / Fields

**Resource Definition** (tenant Definition/catalog record — not an Entity)
- definition_id, tenant_id, category, kind, type_tier (nullable, 1-4)
- name, unit_of_measure (nullable, bulk only), is_individually_tracked (bool)
- status (active, archived)

**Catalog Item** (Item extension; entity_id is the shared PK, FK → Item)
- catalog_resource_definition_ref
- deployment_status (available, staged, deployed, decommissioned)
- home_location_ref, decommissioned_by (nullable), decommissioned_at (nullable)
- active_resource_request_ref (nullable — the currently-open Resource Request driving `deployment_status`, if any)

**Bulk Resource Stock** (local, site-scoped)
- stock_id, catalog_resource_definition_ref, tenant_id, site_ref
- total_quantity, reserved_quantity, reorder_threshold (nullable)

**Stock Threshold Alert Registration**
- registration_id, catalog_resource_definition_ref, site_ref (nullable — null means tenant default)
- minimum_available_count, last_alert_state (ok, below_threshold)

## States & Transitions

- **Resource Definition:** `active` → `archived`, standard Definition lifecycle.
- **Catalog Item:** `available` ⇄ `staged` ⇄ `deployed` → `available` (all driven by the linked Resource Request's phase, FR #4); any non-terminal status → `decommissioned` (manual, admin-only, confirmation-gated, terminal).
- **Bulk Resource Stock:** no status field — currency is purely `total_quantity`/`reserved_quantity`, unchanged from EOC Logistics Hub's original shape.
- **Stock Threshold Alert Registration:** `last_alert_state` toggles `ok` ⇄ `below_threshold` on each crossing, driving alert fire/clear.

## Integrations

- **EOC Logistics Hub** *(retrofitted — superseded)*: `Resource Type Definition` is replaced by this doc's real `Resource Definition`; Resource Request's `resource_type_ref` now resolves here, and `assigned_item_ref` resolves specifically to a Catalog Item. Resource Request's own request→assign→deploy→return lifecycle, Kanban/Queue-Role registration, and custody-transfer-on-assignment mechanics are all unmodified — this doc only supplies the catalog those requests draw from and the status-sync hook described in FR #4. Bulk Resource Stock's field shape is unchanged, just now owned here as the real mechanism.
- **Item Registry**: Catalog Item's base treatment (identity, dedup, custody, audit) inherited unmodified; the multi-extension allowance is exercised for the first time by this doc's coexistence with Vehicle/Camera/etc.
- **Domain Events / Notifications Engine**: Stock Threshold Alert's crossing event routes through the existing Domain Events → Notifications Engine path, no new delivery infrastructure.
- **Command Center Wallboard View / Multi-Incident Console**: `resource_catalog` is a sixth cross-doc Panel Registry contributor (after `health`, `org_chart`, `camera`, `alarm_monitor`).
- **Settings & Preferences**: owns tenant-default reorder-threshold configuration where a Stock Threshold Alert Registration has no site-level override.
- **Command/Action Bus**: Register Catalog Item, Decommission, Adjust Stock Quantity, and Configure Reorder Threshold all register as actions.
- **Structured Logging & Audit Trails**: Decommission and every Bulk Resource Stock quantity adjustment are audit-tier.
- **Module 10 — Calibration & Maintenance Alerts (not yet specified)**: forward reference only — maintenance/inspection scheduling for Catalog Items is explicitly deferred, the same posture Key Ring Registry's Replacement Schedule already used before that module existed. Not built here.
- **Mutual Aid Agreements Tracker (Module 5, not yet specified)**: explicitly separate — this catalog tracks the tenant's own resources; mutual-aid partner capability/contract data stays EOC Logistics Hub's Mutual Aid Organization concept, unaffected by this doc.

## Permissions

| Action | Site/Tenant Admin | Logistics Section Chief | Any catalog viewer |
|---|---|---|---|
| Author/archive a Resource Definition | ✅ | ❌ | ❌ |
| Configure a Stock Threshold Alert Registration | ✅ | ❌ | ❌ |
| Register a new Catalog Item / adjust Bulk Resource Stock quantities | ✅ | ✅ | ❌ |
| Decommission a Catalog Item | ✅ | ❌ | ❌ |
| View the catalog / Deployment Status dashboard | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- `deployment_status` sync from Resource Request phase is one-directional and server-enforced — no client surface may set `staged`/`deployed`/`available` directly while an open Resource Request exists; only Decommission is an independent write.
- Stock Threshold Alert delivery is debounced via `last_alert_state` — a stock row sitting continuously below threshold produces exactly one alert until it recovers, never a repeated notification per check cycle.
- Deployment Status dashboard updates propagate via the Live Update Channel at the platform's standard ≤2s console target, same as every other live board.
- Decommissioning is confirmation-gated but never blocks an already-open Resource Request against that item from completing its return step — the item simply becomes ineligible for any *new* request the moment it's decommissioned.

## Acceptance Criteria

- [ ] A Resource Definition can be authored with a real Category/Kind/Type-Tier combination, matching NIMS Resource Typing conventions.
- [ ] An individually-tracked Catalog Item is a real Item Registry record with full custody/dedup/audit treatment, and can carry a second, unrelated Item extension (e.g. Vehicle) simultaneously without conflict.
- [ ] Assigning a Resource Request to a Catalog Item automatically transitions that item's `deployment_status` to `staged`, with no separate manual status update.
- [ ] Returning or cancelling a Resource Request automatically reverts the linked Catalog Item's `deployment_status` to `available`.
- [ ] Decommissioning a Catalog Item removes it from the requestable pool immediately while its full history remains queryable.
- [ ] A Bulk Resource Stock row whose `available_quantity` crosses below its `reorder_threshold` fires exactly one Notifications Engine alert; it does not re-fire on subsequent checks while still below threshold.
- [ ] A Bulk Resource Stock row that recovers above its threshold clears the alert state, and a later re-crossing fires a fresh alert.
- [ ] The Deployment Status dashboard, filtered by Category/Kind/Type-Tier or site, reflects live status changes within the platform's standard console latency target.
- [ ] The `resource_catalog` panel type is selectable in both Multi-Incident Console's Console Layout and Command Center Wallboard View's Display Profile, drawing from the same shared Panel Registry catalog.
- [ ] A Resource Request submitted against a decommissioned Catalog Item is rejected — decommissioned items are never assignable.

## Open Questions

- Exact NIMS Category/Kind/Type-Tier seed vocabulary — a content/config design task, not committed here.
- Maintenance/calibration/inspection scheduling for Catalog Items — forward reference to Module 10's future Calibration & Maintenance Alerts, not built here.
- Procurement/purchasing workflow (how new stock or equipment actually gets acquired) is explicitly out of scope — this doc tracks readiness/deployment state of resources already on hand, not acquisition; flagged as a possible future Module 15/billing touchpoint if ever needed, not solved here.
- Whether Stock Threshold Alert should later generalize with Duration Watchdog into one unified value-or-duration "Watchdog" abstraction if a second quantity-threshold need appears elsewhere (e.g. Ammunition Registry, Module 10) — flagged, not solved now, consistent with the platform's promote-on-second-consumer discipline.
- Whether a dual-extension resource (e.g. an ambulance that's both Vehicle and Catalog Item) needs any special display-label disambiguation beyond ordinary multi-extension label resolution — not solved here.
