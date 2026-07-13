# Tickets, Citations & Traffic Safety

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Two Activity extensions (per Activity Registry — Citation was already anticipated in that doc's base `activity_type` enum), plus an optional physical-book tracking layer:

1. **Citation** — a single Activity extension covering the whole ticket/citation spectrum from an informal courtesy notice through a formal, fine-bearing citation, differentiated by a tenant-configurable **category** (e.g., Courtesy Notice, Parking Violation, Formal Citation) rather than two structurally separate mechanisms — "Ticket" and "Citation" are the same underlying record.
2. **Traffic Control Activity** — a separate, lightweight Activity extension for logging traffic-direction/control work (an accident scene, an event, a construction zone) independent of whether any citation was ever issued.

Citation issuance follows the platform's standard **RBAC baseline + ABAC overlay** authorization model: any guard with the base "Issue Citation" grant can issue most categories, but a Category Definition can flag itself `requires_citation_authority`, adding an ABAC condition (e.g., an active Citation Authority certification) that hard-denies issuance without it — the certification record itself is a future Personnel/Armed Qualifications Registry concern, deferred like other cross-module certification dependencies; only the gate mechanism is specified here.

**Ticket Book tracking is included as a real, but optional, concept** — not every site uses physical citation pads, so it's tenant/site-configurable whether book tracking is required at all. Where it is, a Ticket Book (a checked-out physical number range) exists *alongside*, not instead of, every Citation's normal digital identity (client UUID + server-assigned sequential number, per Activity Registry's established offline-safe pattern) — the book's own sequence number is a separate, optional physical reference a Citation can carry, and every number in a checked-out book's range must eventually be accounted for as either an issued Citation or an explicitly voided entry, reconciled when the book is returned.

## Actors & Roles

- **Guard** — issues Citations (per their RBAC/ABAC grant), logs Traffic Control Activity, checks out/returns a Ticket Book where used.
- **Supervisor** — reviews/reconciles returned Ticket Books, resolves voided-ticket accounting, may issue Citations themselves.
- **Tenant Admin** — configures Citation Category Definitions (including `requires_citation_authority` and default fine amount), Violation Code catalog, and whether Ticket Book tracking is required (Settings & Preferences).
- **Records Admin** — resolves Entity Registry Core dedup flags on Citation and Traffic Control Activity, same as any other Activity type.

## User Stories

- As a **Guard**, I want to issue a parking violation citation with a photo of the vehicle and its plate, so there's clear evidence if it's disputed.
- As a **Tenant Admin**, I want to require a Citation Authority certification for our "Formal Citation" category but not our "Courtesy Notice" category, so untrained staff can still hand out warnings.
- As a **Guard** using a paper ticket book, I want the app to record which book and page number I wrote a citation on, alongside its normal digital record.
- As a **Supervisor**, I want to reconcile a returned ticket book and see clearly which numbers were issued, which were voided, and whether anything is unaccounted for.
- As a **Guard** directing traffic at an accident scene, I want to log that activity even though I didn't issue any citation.
- As a **Site Manager** at a location that doesn't use paper books at all, I want citation issuance to work exactly the same without ever being asked about a book.

## Functional Requirements

### Citation (Activity extension)
1. **Citation** registers as an Activity extension (per Activity Registry's already-anticipated `citation` activity_type), inheriting base identity, offline-safe numbering, participant/attachment/location associations, and dedup/merge.
2. `category` references a tenant-configurable Category Definition (mirroring DAR Entry/Courtesy Patrol's pattern), each optionally carrying `requires_citation_authority` (bool) and a `default_fine_amount`.
3. `violation_code` references a tenant-configurable Violation Code catalog entry (code, description, optional `statutory_reference` free text for jurisdictions with real citation authority, default fine amount) — nullable, since a courtesy notice may not cite any specific code.
4. `fine_amount` is a nullable decimal, defaulting from the category or violation code but editable per citation; this doc records the amount only — payment processing/collections is explicitly out of scope, deferred to a future billing/external system integration.
5. Subject **Vehicle** (and, when known, subject **Person** — driver/registered owner) are tagged via `ActivityParticipantAssociation`, the same generic mechanism every other Activity type uses; photo evidence via `ActivityAttachmentAssociation` to Document; location via `ActivityLocationAssociation`.
6. `issued_by`, `issued_at` are recorded on every Citation.

### Citation authority (RBAC baseline + ABAC overlay)
7. Issuing a Citation requires the baseline RBAC grant "Issue Citation," per the platform's standard authorization model.
8. When a Citation's category has `requires_citation_authority = true`, an ABAC condition (e.g., an active Citation Authority certification) is evaluated in addition to the RBAC grant; a failed condition hard-denies issuance with a visible reason and an audit entry, per Authentication & Authorization's established ABAC behavior. The certification record itself is not specified here — deferred to Personnel/Armed Qualifications Registry, not yet built.

### Ticket Book (optional, tenant/site-configurable)
9. Whether Ticket Book tracking is required at all is a Settings & Preferences-registered toggle, tenant/site-configurable — many locations never use physical books and see no book-related UI at all.
10. Where enabled, a **Ticket Book** is a checked-out physical number range (`range_start`–`range_end`) assigned to a Guard. A Citation issued while a book is checked out may optionally record `ticket_book_ref` and `book_sequence_number` — a separate, physical-paper reference distinct from the Citation's own digital client-UUID/server-number identity.
11. A number within a checked-out book's range that was written on paper but not turned into a real Citation (torn, mis-written, illegible) is recorded as a **voided entry** against that book/number, so the book's full range stays accounted for without requiring a matching Citation record for every number.
12. Returning a Ticket Book triggers **reconciliation**: a Supervisor confirms every number in the book's range is accounted for (an issued Citation or a voided entry) before the book is marked reconciled; any unaccounted-for number is flagged, not silently ignored.
13. A Ticket Book can also be marked **lost**, a distinct terminal state from returned/reconciled, given the real compliance seriousness of a missing citation pad.

### Traffic Control Activity (separate Activity extension)
14. **Traffic Control Activity** registers as its own Activity extension, independent of Citation: `category` (tenant-configurable — e.g., Accident Scene, Event Traffic Control, Construction Zone, Escort), `narrative`, standard base Activity timing (`started_at`/`concluded_at`), location, and participants (vehicles/persons involved) via the same generic association mechanisms.
15. No structural link is forced between a Traffic Control Activity and any Citation issued during it — both are ordinary Activities that surface naturally together in the same guard's DAR/timeline filtered by time/location, without needing an explicit cross-reference field.

## Data Model / Fields

**Citation** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- category (ref → Citation Category Definition), violation_code (ref → Violation Code, nullable)
- fine_amount (nullable, decimal)
- ticket_book_ref (nullable), book_sequence_number (nullable)
- issued_by, issued_at
- *(subject vehicle/person, photo evidence, and location are rows, not fields — ActivityParticipantAssociation / ActivityAttachmentAssociation / ActivityLocationAssociation)*

**Citation Category Definition** (Settings & Preferences registration)
- category_id, tenant_id, name, enabled
- requires_citation_authority (bool), default_fine_amount (nullable)

**Violation Code** (Settings & Preferences registration)
- code_id, tenant_id, code, description
- statutory_reference (nullable, free text), default_fine_amount (nullable)

**Ticket Book**
- book_id, tenant_id, book_identifier
- assigned_person_ref, range_start, range_end
- status (checked_out, returned, reconciled, lost)
- checked_out_at, returned_at (nullable), reconciled_at (nullable), reconciled_by (nullable)

**Ticket Book Voided Entry**
- void_id, tenant_id, ticket_book_ref, sequence_number
- voided_by, voided_at, reason (free text)

**Traffic Control Activity** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- category (ref → Traffic Control Category Definition)
- narrative
- *(location and participants are rows — ActivityLocationAssociation / ActivityParticipantAssociation)*

## States & Transitions

**Citation:** follows base Activity lifecycle — `open` → `in_progress` → `concluded` | `cancelled`.

**Ticket Book:** `checked_out` → `returned` → `reconciled` (every number accounted for) | `lost` (from `checked_out` at any time).

**Traffic Control Activity:** follows base Activity lifecycle unmodified — `open` → `in_progress` → `concluded` | `cancelled`.

## Integrations

- **Activity Registry**: Citation and Traffic Control Activity both register as Activity extensions; Citation fulfills the `citation` activity_type already anticipated there.
- **Entity Registry Core**: identity, dedup/merge, and display-label requirements for both, same as any other Activity type.
- **Authentication & Authorization**: source of the RBAC baseline grant and the ABAC Citation Authority condition mechanism; the certification data itself is a future Personnel/Armed Qualifications Registry dependency.
- **Item Registry / Vehicle-Conveyance Registry**: source of a Citation's subject Vehicle.
- **Party Registry**: source of a Citation's subject Person (driver/owner) when known, and Traffic Control Activity's involved persons.
- **Document Registry**: source of Citation's photo evidence via `ActivityAttachmentAssociation`.
- **Settings & Preferences**: owns Citation Category Definitions, Violation Codes, and the tenant/site toggle for whether Ticket Book tracking is required.
- **Daily Activity Reports (DAR)**: Citations and Traffic Control Activities are ordinary Activities, automatically picked up by any DAR filter matching guard/site/time window.
- **Shift Passdowns & Handover Notes**: an unreconciled Ticket Book, or a Citation category flagged for follow-up, is a natural Pass-On Rule candidate, same mechanism as elsewhere.
- **Structured Logging & Audit Trails**: Citation issuance, Ticket Book checkout/return/void/reconciliation, and ABAC citation-authority denials are all audit-tier events.
- **Command/Action Bus**: "Issue citation," "Check out ticket book," "Void ticket number," "Reconcile ticket book," "Log traffic control activity" register as invokable actions across every surface.
- **Guard Tour & Checkpoint Verification, Courtesy Patrol**: a Citation or Traffic Control Activity can be launched from a Checkpoint Scan or Courtesy Patrol via the same Command/Action Bus context-seeding launch-point mechanism already established, with location pre-filled.
- **Investigation Management (future)**: a Citation's eventual disposition/appeal (if a tenant needs formal dispute handling) is explicitly deferred — not built here (see Open Questions).
- **Personnel (future — Armed Qualifications Registry / Licensing & Guard Card Tracking)**: intended eventual source of the Citation Authority certification the ABAC condition checks — deferred, not built now.

## Permissions

| Action | Guard | Supervisor | Tenant Admin |
|---|---|---|---|
| Issue a Citation (category with no authority requirement) | ✅ | ✅ | ❌ |
| Issue a Citation (category requiring Citation Authority) | ✅ (if ABAC condition satisfied) | ✅ (if ABAC condition satisfied) | ❌ |
| Check out / return a Ticket Book | ✅ (own) | ✅ | ❌ |
| Void a ticket number | ✅ (own book) | ✅ | ❌ |
| Reconcile a returned Ticket Book | ❌ | ✅ | ✅ |
| Log a Traffic Control Activity | ✅ | ✅ | ❌ |
| Configure Category Definitions / Violation Codes / Ticket Book requirement toggle | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Citation and Traffic Control Activity creation must work fully offline, per the platform's established offline model — including recording a ticket book/sequence number without connectivity.
- Ticket Book reconciliation must clearly surface any unaccounted-for number in a returned book's range — never silently treat a gap as accounted for.
- ABAC citation-authority denials must be visible to the guard with a clear reason (per Authentication & Authorization's standard hard-deny behavior) and audit-logged, not a silent failure.
- WCAG 2.1 / Section 508 accessible citation issuance, ticket book management, and traffic control logging flows, day one.

## Acceptance Criteria

- [ ] Issuing a Citation with category "Parking Violation" (no authority requirement) succeeds for any Guard with the base RBAC grant.
- [ ] Issuing a Citation with category "Formal Citation" (`requires_citation_authority = true`) is hard-denied, with a visible reason and audit entry, for a Guard lacking the ABAC condition.
- [ ] A Citation's subject Vehicle and, when known, subject Person are both correctly tagged via `ActivityParticipantAssociation`.
- [ ] At a site with Ticket Book tracking disabled, citation issuance never surfaces any book-related field.
- [ ] Checking out a Ticket Book, issuing citations against some of its numbers, voiding others, and returning it correctly reconciles when every number in the range is accounted for.
- [ ] Attempting to reconcile a Ticket Book with an unaccounted-for number is rejected/flagged, not silently allowed.
- [ ] Marking a Ticket Book lost is distinct from returning it and is reflected in its status.
- [ ] A Traffic Control Activity can be logged with no Citation involved at all, and appears correctly in the guard's DAR filter view alongside any Citations from the same shift with no special-casing.
- [ ] A Citation launched from a Guard Tour Checkpoint Scan correctly pre-fills location from the checkpoint.

## Open Questions

- Citation disposition/appeal workflow (dismissed, upheld, appealed) — explicitly deferred; likely a future Investigation Management (Case Dispositions) integration point, not built here.
- Payment/fine collection processing — out of scope; this doc only records `fine_amount`, not payment status.
- Exact default Citation Category and Violation Code taxonomies shipped out of the box — pending UX/content design.
- Whether Ticket Book numbering needs to support non-sequential/custom book formats (some jurisdictions' pads aren't simple numeric ranges) — current model assumes a simple numeric range; not confirmed against real-world pad formats.
- Exact Citation Authority certification data model — deferred entirely to Personnel/Armed Qualifications Registry when that module is specified.
- Whether a voided ticket number needs its own photo/evidence (e.g., a photo of the torn/voided paper ticket) for audit purposes — not addressed here.
