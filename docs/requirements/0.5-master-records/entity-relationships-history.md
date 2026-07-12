# Entity Relationships & History

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Entity Relationships & History is the capstone Master Records feature — it doesn't introduce new base types or association kinds, it synthesizes everything already established across Entity Registry Core and the seven concrete Registry docs (Party/Person/Organization, Item/Vehicle, Location, Activity, Document) into two consumer-facing views per entity:

1. **Interaction Timeline** — a chronological feed of everything that happened involving an entity (per MODULES.md's literal sub-bullet: "all scheduled shifts, access events, incidents, or citations associated with the entity").
2. **Relationship Snapshot** — a point-in-time view of an entity's *currently active* associations (who's the current custody holder, owner, employer, emergency contacts right now) — implied by the feature's full name ("Relationships," not just "History") and structurally cheap to compute since it only needs active `EntityAssociation` rows, not the full history.

Both views are built as a **CQRS read-model projection** (per Event & Command Bus Architecture's Query side), subscribing to Entity, EntityAssociation, Activity, and BOLO Flag events and updating incrementally — not a live cross-table query fired on every page view. Because a merge redirects `entity_id_a`/`entity_id_b` on *every* affected EntityAssociation row (not just currently-active ones, per Entity Registry Core), a merge survivor's timeline and snapshot automatically reflect the full combined history and current relationships of both entities with no special-case handling here.

Every timeline entry and snapshot row renders using its source entity's registered `display_label_strategy` (per Entity Registry Core's universal display-label requirement) — this feature never implements its own type-specific rendering logic.

## Actors & Roles

- **Interaction Timeline Viewer** (a dedicated permission, not automatically implied by other access) — views an entity's aggregated timeline/snapshot.
- **Records Admin** — same access as any Timeline Viewer, plus visibility into read-model projection health (lag/staleness).
- **Every module surfacing an entity profile page** (a Person's profile, a Vehicle's record, an Incident's detail view) — consumes this feature's timeline/snapshot rather than building its own cross-module aggregation.

## User Stories

- As a **Supervisor**, I want to see every call, incident, citation, custody transfer, and BOLO event involving a person in one chronological feed, without navigating to five different modules.
- As a **Fleet Coordinator**, I want a vehicle's Relationship Snapshot to show its current owner and current custodian at a glance, without scanning its full history to figure out "what's true right now."
- As a **Records Admin**, I want a merge survivor's timeline to automatically include the merged-away entity's full history — nothing missing, nothing duplicated.
- As a **Tenant Admin**, I want viewing an entity's full interaction timeline to require its own permission, since the aggregate view is a bigger privacy/liability surface than any single underlying fact, even for a user who could technically see each fact individually.
- As a **Timeline Viewer** without clearance for a specific classified document, I want that document's timeline entry to be filtered out (or redacted) even though I otherwise have permission to view timelines, so the aggregate view never becomes a way to see more than my normal access allows.
- As a **Supervisor viewing a busy long-tenured guard's timeline**, I want to filter by category and date range, since the raw feed could otherwise run to thousands of entries.

## Functional Requirements

### Interaction Timeline
1. A chronological, entity-keyed feed combining: Activities the entity participated in or was attached to (via `ActivityParticipantAssociation`, `ActivityAttachmentAssociation`); Activities where the entity was the primary location (via `ActivityLocationAssociation`, for Location entities); every `EntityAssociation` active↔removed transition involving the entity, on either side, as its own entry (a custody transfer, an ownership change, a hierarchy reassignment, an emergency-contact add/remove); and BOLO Flag lifecycle events (creation, clearance, expiration) on the entity.
2. Each entry carries: timestamp, category, a reference to its source record, and a display label resolved via that source's own `display_label_strategy` — this feature does not maintain its own copy of rendering logic per source type.
3. Supports filtering by category and date range, and pagination, given that a long-tenured entity's raw feed can run large.
4. **Scheduled shifts and access-control events** (explicitly named in MODULES.md's own description) are not built as timeline sources by this doc — Personnel's future scheduling feature and Access Control's future badge/access-event logging haven't been specified yet. This doc defines the integration point (any future module can register itself as a timeline source, keyed by `entity_id`, contributing entries through the same read-model mechanism) without inventing their data model prematurely.

### Relationship Snapshot
5. A point-in-time view of only the entity's currently **active** EntityAssociation rows (both as `entity_id_a` and `entity_id_b`), grouped by association kind (ownership, custody, employment, emergency contacts, hierarchy, authorship, facility management, and so on), each row showing the counterpart entity's display label and the role.
6. Computed from the same read-model as the timeline, but structurally lighter — no historical rows to traverse, just the current active set.

### Read-model & freshness
7. Both views are maintained as a materialized CQRS read-model projection, subscribing to the relevant domain events (per Event & Command Bus Architecture) rather than computed live on each request; a small, acceptable propagation lag is expected rather than instantaneous consistency.
8. Tenant isolation applies identically to every other Master Records mechanism: a subcontractor's cross-tenant identity never causes tenant A's timeline entries to appear when viewing that person within tenant B.

### Merge handling
9. No special-case logic is needed here: because Entity Registry Core's merge redirects every EntityAssociation row (historical and active) to the survivor, the survivor's timeline and snapshot are automatically complete and correct once the read-model catches up to the merge event.

## Data Model / Fields

**Timeline Entry** (read-model)
- entry_id, tenant_id, subject_entity_id (whose timeline this belongs to)
- timestamp, category (activity_participation, activity_attachment, activity_location, association_change, bolo_flag_event)
- source_ref (the Activity, EntityAssociation, or BOLO Flag record this entry represents)
- display_label (resolved at projection time via the source's display_label_strategy)
- sensitivity_tags[] (carried from the source record, used to filter per viewer's ABAC at read time)

**Relationship Snapshot Row** (read-model)
- subject_entity_id, association_kind (association_type or concrete subtype name)
- counterpart_entity_id, counterpart_display_label, role
- since (the active row's added_at)

## States & Transitions

**Read-model projection (per entity or globally):** `current` (caught up to the latest source events) → `lagging` (behind a defined threshold, surfaced to Records Admin/ops) → `current` (catches up). Mirrors the projection-lag pattern already established in Event & Command Bus Architecture.

## Integrations

- **Entity Registry Core**: source of Entity, EntityAssociation (all subtypes), BOLO Flag, and merge events this feature projects from; source of the display-label mechanism every entry renders through.
- **Event & Command Bus Architecture**: owns the underlying CQRS/read-model infrastructure this feature's projection is built on.
- **Party Registry, Item Registry, Location Registry, Activity Registry, Document Registry** (and every extension beneath them): implicit sources — any entity or association registered against Entity Registry Core automatically participates, no bespoke per-module integration required.
- **Structured Logging & Audit Trails**: viewing an entity's timeline/snapshot is itself an audit-tier event, consistent with the Audit Viewer pattern.
- **Authentication & Authorization**: source of the Interaction Timeline Viewer permission and the ABAC attributes used to filter individual entries by sensitivity, independent of whether the viewer holds the aggregate-view permission.
- **Personnel (future scheduling), Access Control (future access-event logging)**: future timeline-source registrants, per the deferred integration point noted above.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Records Admin | Interaction Timeline Viewer | Standard User |
|---|---|---|---|---|---|
| View an entity's Interaction Timeline | ✅ | ✅ (own tenant) | ✅ (own scope) | ✅ (own scope, per granted permission) | ❌ (unless granted) |
| View an entity's Relationship Snapshot | ✅ | ✅ (own tenant) | ✅ (own scope) | ✅ (own scope, per granted permission) | ❌ (unless granted) |
| View read-model projection lag/health | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ |

Holding the Interaction Timeline Viewer permission is necessary but not sufficient to see every entry: individual entries are still filtered per the viewer's own ABAC/data-scope, exactly as if they'd viewed each underlying record directly — the permission gates the *aggregate view*, it never bypasses field-level sensitivity rules already established elsewhere (e.g., a classified Document's timeline entry, a medical-alert-adjacent entry).

## Non-Functional / Constraints

- Read-model propagation lag must stay within a defined, monitored bound — a Records Admin should be able to tell if a specific entity's timeline is stale, not just assume freshness.
- Timeline queries must remain performant for entities with years of accumulated history — pagination and indexed date-range/category filtering are mandatory, not an afterthought.
- ABAC/sensitivity filtering of individual timeline entries must be enforced at read time (per current viewer, per current attribute state), not baked into the entry at projection time, since a viewer's clearance or an entity's classification can change after the entry was projected.
- Tenant isolation enforcement is non-negotiable and identical to every other Master Records mechanism.
- WCAG 2.1 / Section 508 accessible timeline and snapshot views, day one.

## Acceptance Criteria

- [ ] An entity's Interaction Timeline correctly includes entries from Activity participation/attachment/location associations, EntityAssociation transitions, and BOLO Flag events, each with a correctly resolved display label.
- [ ] Filtering the timeline by category and date range returns the correct subset; pagination works correctly for an entity with a large history.
- [ ] An entity's Relationship Snapshot shows only currently-active associations, correctly grouped by kind, with no historical/removed rows included.
- [ ] Merging two entities results in the survivor's timeline and snapshot correctly reflecting the full combined history and current relationships of both, once the read-model catches up.
- [ ] A user without the Interaction Timeline Viewer permission cannot view an entity's aggregate timeline or snapshot, even if they could view some underlying records individually.
- [ ] A Timeline Viewer lacking ABAC clearance for a specific classified entry does not see that entry, even though they can see the rest of the timeline.
- [ ] Tenant A's timeline entries never appear when viewing an entity within Tenant B, even for a subcontractor's linked cross-tenant identity.
- [ ] A Records Admin can observe projection lag/staleness for the read-model and confirm it stays within the defined bound under normal load.
- [ ] The deferred scheduling/access-event integration point exists (a documented registration mechanism) without requiring Personnel or Access Control to be specified yet.

## Open Questions

- Exact propagation-lag SLA for the read-model — a technical-spec-level decision.
- Exact filtering/pagination UX (infinite scroll vs. paged, default category filters) — a technical-spec/UI-level decision.
- Whether Relationship Snapshot needs its own separate permission from Interaction Timeline Viewer, or the two are always granted together — leaning toward one combined permission at launch, revisited if a concrete need to separate them surfaces.
- Exact registration mechanism for future timeline sources (Personnel's scheduling, Access Control's access events) — to be finalized when those modules are specified, using whatever pattern Entity Registry Core's registration model establishes by then.
