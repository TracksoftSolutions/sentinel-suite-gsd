# Activity Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Activity Registry gives full first-class depth to one of the five base Entity Types established in Entity Registry Core — `nc:ActivityType`-aligned records representing any occurrence the platform tracks: a call, an incident, a citation, an accident, an alarm, an inspection, a dispatch, a drill, and more. Per the user's own framing, these Activity subtypes are "the whole point of the platform," so this doc gets the same depth Person, Item, and Location received, not a thin stub.

Four mechanisms live here, generalized rather than duplicated per consuming module, all built on Entity Registry Core's Table-Per-Type EntityAssociation hierarchy:

1. **Participants and attachments** — `ActivityParticipantAssociation` (a Party or Item involved — witness, victim, suspect, subject, reporting_officer, responding_unit, involved_vehicle, seized_weapon) and `ActivityAttachmentAssociation` (a Document involved — evidence, report, citation_copy) are real, named TPT subtypes of EntityAssociation, not merely rows distinguished by a string. This **replaces** Person Registry's earlier "Incident Participant" extension, retired in favor of this shared mechanism. A car accident's participants include both the people involved (`ActivityParticipantAssociation` to a Party) and the vehicles (`ActivityParticipantAssociation` to an Item) through the same subtype, differing only in what `entity_id_b` resolves to.
2. **Location** — `ActivityLocationAssociation` replaces what was originally a plain `location_ref` field: an Activity's primary location is the active `ActivityLocationAssociation` row, giving correction history for free (e.g., an incident's location updated after initial dispatch) using the same single-current-value pattern as ownership and custody, rather than a bespoke correction-history mechanism.
3. **Offline-safe identification** — Activity numbering reuses Offline Data Sync's already-established pattern: a client-generated UUID at creation, with the official sequential display number (incident #, citation #) assigned once synced.
4. **Dedup/merge** — the same tombstone + reference-redirect + reversible model already established for Party/Item/Location, directly powering Dispatch/CAD's future "Incident Merging" feature.

This doc establishes only the base Activity mechanics and this shared machinery. Each owning module (Dispatch/CAD for Call, Security Operations for Incident and Citation, Physical Security Integration Gateway for Alarm, Safety Management for Inspection, Emergency Management for Drill, and so on) registers its own Activity extension with rich, type-specific fields when that module is specified.

**Every Entity, including every Activity extension, must have a display label — that requirement is platform-wide, owned by Entity Registry Core.** What's specific to Activity is *which* of the two label strategies applies: most base types (Person, Organization, Vehicle, Location, Document) use the simple **template** strategy — their natural fields already say what they are. Activity extensions are the type most likely to need the **computed** strategy instead, since a useful label for a heterogeneous cross-module **timeline** (Entity Relationships & History) — mixing calls, incidents, citations in sequence — is a synthesized one-line summary ("Incident #4521: Theft, Building A, concluded"), not a raw single-field readout. Each Activity extension provides its own computed-label logic when it's specified; Entity Relationships & History consumes whatever each extension provides, uniformly, through the same Entity Registry Core mechanism every other type's label goes through.

## Actors & Roles

- **Any platform user creating an Activity record** (via the owning module's own workflow — a Dispatcher logging a call, a Guard filing an incident) — the actual creation UI belongs to each owning module, not this doc.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Activity entities, confirms/rejects merges (e.g., Dispatch/CAD's Incident Merging).
- **Every future Activity-producing module developer** — registers its own Activity extension against this registry, and its own summary-generation logic for timeline rendering.

## User Stories

- As a **Dispatcher**, I want two separate 911-style calls about the same fire to be flagged as likely duplicates so I can merge them into one incident ticket, using the same reliable merge mechanism already proven for Person/Item/Location.
- As a **Records Admin**, I want a confirmed Activity merge to redirect every association — participant tags, attachments, location — to the surviving record, exactly like a Person or Item merge does.
- As a **Guard filing an incident offline**, I want my incident to get a real, usable ID immediately and its official incident number once I'm back in coverage.
- As an **Investigator**, I want to tag a witness (Person) and a suspect (Person) and an involved vehicle (Item) all as `ActivityParticipantAssociation` rows on one Incident, using the exact same mechanism regardless of which kind of entity each is.
- As an **Investigator**, I want to attach an evidence photo (Document) to an Incident via `ActivityAttachmentAssociation` with its own role, and correct an incident's initially-logged location later while retaining the original as history.
- As a **Supervisor viewing a person's profile**, I want to see every Activity they were ever a participant in — regardless of which module produced it — in one uniform timeline with a readable one-line summary per entry, without per-module custom integration.
- As a **future Physical Security Integration Gateway developer**, I want to register "Alarm" as an Activity extension and get global ID, dedup, participant tracking, location tracking, and cross-module history for free.

## Functional Requirements

### Base Activity fields (`nc:ActivityType`-aligned)
1. **Type**: a base-level `activity_type` (call, incident, citation, accident, alarm, inspection, dispatch, drill — extensible).
2. **Identification**: numbering follows Offline Data Sync's established pattern exactly — a client-generated UUID at creation, with the server assigning the official sequential display number once synced.
3. **Status**: a generic activity lifecycle — `open` → `in_progress` → `concluded` | `cancelled`, with `concluded` reopenable to `in_progress`. Extension types layer their own richer status nuance on top.
4. **Date/time**: `started_at`, `concluded_at` (nullable until concluded).
5. **Description**: free-text description.
6. Every base field above is optional/nullable.

### Participant and attachment associations
7. `ActivityParticipantAssociation` (`entity_id_a` = the Activity, `entity_id_b` = a Party or Item) and `ActivityAttachmentAssociation` (`entity_id_a` = the Activity, `entity_id_b` = a Document) are named TPT subtypes of EntityAssociation, each carrying no extra fields beyond the base shape (role + audit is sufficient). Role vocabulary may be a controlled list per activity_type (defined by that type's owning module) or a general fallback list at the base level.
8. A Participant/attachment association is added/removed by users with permission on the specific Activity (governed by that Activity extension's own owning-module permissions, not by this doc).
9. This mechanism is the sole participant/attachment-tracking mechanism for Activities platform-wide — no extension type or other base entity (including Person) maintains a separate, parallel structure.

### Location association
10. `ActivityLocationAssociation` (`entity_id_a` = the Activity, `entity_id_b` = a Location) replaces a plain `location_ref` field. The active row is the Activity's current primary location; setting a new active row (removing the prior one) is how a location correction is recorded, giving correction history for free via the same single-current-value association pattern used elsewhere.

### Deduplication & merge
11. Activity-specific match signals for Entity Registry Core's deduplication engine: same activity_type + overlapping time window + same/nearby location (via `ActivityLocationAssociation`), or shared participant(s), surfaces a likely-duplicate flag for human review. Never auto-merged.
12. Confirming an Activity merge follows the same canonical-survivor + reference-redirect + tombstone + reversible model as Party/Item/Location, redirecting every EntityAssociation row (participant, attachment, location) referencing the merged-away Activity.

### Extension types & display summaries
13. Future Activity extensions (Call, Incident, Citation, Accident, Alarm, Inspection, Dispatch, Drill) are each registered by their owning module when specified, inheriting base identity, offline-safe numbering, participant/attachment/location tracking, and dedup/merge for free.
14. Each Activity extension provides its own summary-generation logic (e.g., an Incident's summary might read "Incident #4521: Theft, Building A, concluded") for Entity Relationships & History's timeline to consume uniformly, without that feature needing type-specific rendering logic per extension.

## Data Model / Fields

**Activity** (TPT level: entity_id is the shared PK, FK → Entity.entity_id — structured per `nc:ActivityType`)
- entity_id (PK, FK → Entity), tenant_id, activity_type
- client_uuid (offline-assigned), display_number (server-assigned on sync)
- status (open, in_progress, concluded, cancelled)
- started_at, concluded_at (nullable)
- description

*(Participants, attachments, and location are association rows, not fields here.)*

**ActivityParticipantAssociation** (TPT level: association_id shared PK, FK → EntityAssociation.association_id)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Activity, entity_id_b = a Party or Item)
- no extra fields beyond the base EntityAssociation shape

**ActivityAttachmentAssociation** (TPT level)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Activity, entity_id_b = a Document)
- no extra fields beyond the base EntityAssociation shape

**ActivityLocationAssociation** (TPT level)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Activity, entity_id_b = a Location)
- no extra fields beyond the base EntityAssociation shape

## States & Transitions

**Activity:** `open` → `in_progress` → `concluded` (reopenable → `in_progress`) | `cancelled`. Independent of Entity Registry Core's standard `active`/`tombstoned` states, which apply on top.

**ActivityParticipantAssociation, ActivityAttachmentAssociation, ActivityLocationAssociation:** follow Entity Registry Core's shared EntityAssociation lifecycle unmodified (`active` → `removed`). For location specifically, a new active row on correction leaves the prior one `removed`, forming location history.

## Integrations

- **Entity Registry Core**: owns the base Activity TPT mechanics (global ID, deduplication, merge) and the base EntityAssociation shape all three association subtypes here extend.
- **Offline Data Sync**: source of the client-UUID + server-assigned-display-number pattern this doc reuses unmodified.
- **Party Registry**: Person and Organization entities participate in Activities exclusively through `ActivityParticipantAssociation`.
- **Item Registry / Vehicle-Conveyance Registry**: Item entities (including vehicles) participate through the same mechanism.
- **Document Registry**: Documents attach to Activities through `ActivityAttachmentAssociation`.
- **Location Registry**: source of the Location entity `ActivityLocationAssociation` points to.
- **Structured Logging & Audit Trails**: Activity creation, status changes, association add/remove, and merges are audit-tier events.
- **Dispatch/CAD, Security Operations, Physical Security Integration Gateway, Safety Management, Emergency Management, and every future occurrence-producing module**: future registrants of Activity extensions and their own summary-generation logic. Dispatch/CAD's "Incident Merging" feature specifically consumes this doc's merge mechanism.
- **Entity Relationships & History**: the next feature; consumes Activity entities, their associations, and each extension's summary output to build the platform's cross-module timeline.

## Permissions

Base Activity record permissions (create, edit, conclude) are governed by each Activity extension's owning module — not by this doc. This doc's own permission surface is limited to the shared mechanisms:

| Action | Platform Super Admin | Tenant Admin | Records Admin | Any user with permission on the specific Activity |
|---|---|---|---|---|
| Add/remove ActivityParticipantAssociation or ActivityAttachmentAssociation | ✅ | ✅ | ❌ (unless also granted) | ✅ (per that Activity's own governing permission) |
| Add/remove ActivityLocationAssociation (correct location) | ✅ | ✅ | ❌ (unless also granted) | ✅ (per that Activity's own governing permission) |
| Resolve deduplication flags / confirm-reject merges | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ |
| Reverse a merge | ✅ | ✅ (own tenant) | ✅ (own scope, if granted) | ❌ |

## Non-Functional / Constraints

- Offline-created Activities must behave identically to the general offline-record model already specified.
- Merge must correctly redirect every association row (participant, attachment, location) on both sides — a merged-away Activity's associations must all resolve correctly against the survivor.
- Dedup matching must run within tenant isolation boundaries, per Entity Registry Core's universal rule.
- Each Activity extension's summary-generation logic must degrade gracefully (a sensible default, e.g. "[activity_type] #[display_number]") if that extension hasn't implemented a richer summary yet, so the timeline never shows a broken/empty entry.
- WCAG 2.1 / Section 508 accessible participant-tagging, location-correction, and merge-review flows, day one.

## Acceptance Criteria

- [ ] Registering "Incident" as an Activity Extension Type correctly inherits base identity, numbering, participant/attachment/location tracking, and dedup/merge without reimplementing them.
- [ ] An Activity created offline receives a usable client UUID immediately and its official display number once synced.
- [ ] A Person and an Item can both be tagged on the same Activity via `ActivityParticipantAssociation` with different roles, each correctly resolving via the shared `Entity` anchor.
- [ ] A Document can be attached to an Activity via `ActivityAttachmentAssociation` with its own role.
- [ ] Correcting an Activity's location creates a new active `ActivityLocationAssociation` while the prior one becomes removed, preserving the correction history.
- [ ] Two Activities of the same type, overlapping time window, and shared participant are flagged as a potential duplicate, never auto-merged.
- [ ] Confirming an Activity merge redirects every participant, attachment, and location association to the survivor and tombstones the merged-away Activity, reversibly, with full audit trail.
- [ ] A Person's profile view correctly lists every Activity they participated in, sourced entirely from `ActivityParticipantAssociation` rows.
- [ ] Dispatch/CAD's (future, stubbed) Incident Merging feature successfully invokes this doc's merge mechanism rather than an independent implementation.
- [ ] A stubbed Activity extension with no custom summary logic still produces a sensible default timeline entry rather than a broken/empty one.

## Open Questions

- Full activity_type taxonomy and per-type participant role vocabularies are built out incrementally as each producing module is specified.
- Exact dedup match-signal weighting for Activities — to be tuned during technical spec.
- Whether Activity ever needs its own BOLO-Flag-style use case — Entity Registry Core's generic mechanism is available if needed; not speculatively built out here.
- Exact shape of the computed-label contract (a required method every extension implements vs. some other mechanism) — governed by Entity Registry Core's `display_label_strategy` (computed variant), finalized during technical spec.
- Exact NIEM release/version and precise `nc:ActivityType`/association element names — same technical-spec-level verification task noted in the other Master Records docs.
