# Party Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Party Registry establishes **Party** — NIEM Core's `nc:EntityType`, meaning "a Person or an Organization" — as a base Entity Type in its own right (per Entity Registry Core), one level in the platform's Table-Per-Type inheritance chain: `Entity` → `Party` → {`Person`, `Organization`} → {`Employee`, `Visitor`, ...}. **Person Registry** and **Organization Registry** each extend Party via TPT — their own table, sharing the same `entity_id` primary key, joined back to Party's row — inheriting identity, deduplication, merge, and BOLO-eligibility mechanics while defining their own much richer, type-specific fields and further sub-extensions.

Party's own base fields are deliberately minimal — genuinely shared between a Person and an Organization: identification numbers, contact information, and a discriminator. NIEM's `nc:EntityType` is mostly a typing/reference convenience rather than a rich shared schema, and this doc follows that: Party is never created "as itself" (every real record is specifically a Person or specifically an Organization by the time it's created) — it exists so that fields needing "could be either" flexibility (Vehicle's owner, a document's author, a contract's responsible party) can reference one Party type instead of an awkward multi-branch field.

## Actors & Roles

- **Person Registry, Organization Registry** — the two concrete extensions of Party; own their own respective richer schemas.
- **Every module needing a "person-or-organization" reference** (Vehicle/Conveyance Registry's owner, Document Registry's author, future Contract Management's responsible party) — references a Party entity_id rather than building its own person-or-org branching logic.
- **Records Admin** — resolves Entity Registry Core deduplication flags at the Party level (in addition to the richer Person- or Organization-specific dedup each extension defines).

## User Stories

- As a **Vehicle/Conveyance Registry consumer**, I want a `ConveyanceOwnerAssociation` to reference "the owner" as a single Party entity_id, resolving to either a Person or an Organization, rather than a three-way conditional field.
- As a **Document Registry consumer**, I want a document's author to be a Party reference, since policies are often authored by an individual but issued by a company/department.
- As a **future Contract Management developer**, I want a contract's responsible party to reference Party directly, since it could be an individual contractor or a subcontracting agency.
- As a **Records Admin**, I want base-level identification (e.g., a tax ID that happens to also be useful for matching) to be checked once at the Party level, without Person and Organization each reimplementing identical identification-matching logic.

## Functional Requirements

### Base Party fields (`nc:EntityType`-aligned, minimal)
1. **Discriminator**: `party_type` (person, organization) — set once at creation and immutable; determines which extension (Person or Organization) the record actually is.
2. **Identification**: a generic, repeatable `PartyIdentification` structure (id value + category) — the same shape Person Registry and Item Registry already use, hoisted to Party level since both Person and Organization need identification numbers (SSN/driver's license for a Person; tax ID/business license for an Organization).
3. **Contact information**: an associated `ContactInformation` structure (telephone, email, structured address) — hoisted to Party level for the same reason, consistent with the association pattern already established in Person Registry.
4. Party itself carries no name field — Person's structured `PersonName` and Organization's `OrganizationName` are different enough in shape (individual given/sur name vs. a single organization name) that forcing a shared "name" field at the Party level would misrepresent both; each extension defines its own.

### Extension requirement
5. Every Party record must have exactly one extension — either a Person extension or an Organization extension — matching its `party_type`; a bare, unextended Party record is not a valid, useful state (mirroring how a bare Item without any meaningful category is unusual but not prohibited — Party's case is stricter: unlike Item, Party's whole purpose is to be either a Person or an Organization). Because of this, Party itself declares no `display_label_strategy` of its own (per Entity Registry Core's universal display-label requirement) — the label always comes from whichever extension, Person or Organization, is actually present.

### Deduplication
6. Party-level match signals are minimal (identification number exact match) and run before/alongside each extension's own richer matching (Person's name+DOB+phone; Organization's legal name+tax ID+address) — a Party-level identification match is treated as a very high-confidence signal regardless of which extension is involved. Never auto-merged, per Entity Registry Core's universal governance.

## Data Model / Fields

**Party** (TPT level: entity_id is the shared PK, FK → Entity.entity_id — structured per `nc:EntityType`)
- entity_id (PK, FK → Entity), tenant_id, party_type (person, organization)
- identifications[] (id_value, category)
- contact_information (telephone[], email[], addresses[])

## States & Transitions

**Party:** follows Entity Registry Core's standard model (`active` → `tombstoned` → `active` on merge reversal) unmodified. `party_type` is immutable — a merge always occurs between two Parties of the same type (a Person can't merge with an Organization).

## Integrations

- **Entity Registry Core**: owns the base Party Entity Type mechanics (global ID, deduplication, merge, BOLO-eligibility).
- **Person Registry, Organization Registry**: the two concrete extensions building on this base.
- **Vehicle/Conveyance Registry, Document Registry, and every future module needing a person-or-organization reference**: consume Party entity_id as their reference type.

## Permissions

Party-level permissions mirror whichever extension (Person or Organization) governs the actual record — this doc defines no separate permission surface of its own.

## Non-Functional / Constraints

- `party_type` must never be changeable after creation — a record understood to be an Organization cannot later "become" a Person; a genuine data-entry error is corrected by tombstoning/recreating, not by mutating the discriminator.
- Party's minimal base schema must not become a dumping ground for fields that actually belong on one extension but not the other — new fields default to living on Person or Organization specifically unless there's a clear, symmetric need on both.

## Acceptance Criteria

- [ ] Creating a Person record correctly creates one row at the Entity level, one at the Party level, and one at the Person level, all sharing the same entity_id.
- [ ] Creating an Organization record correctly creates one row at the Entity level, one at the Party level, and one at the Organization level, all sharing the same entity_id.
- [ ] A `ConveyanceOwnerAssociation` referencing "Party" resolves correctly to either a Person or an Organization without a type-specific branch in the consuming code/schema.
- [ ] Identification numbers set at the Party level are visible/usable by both Person- and Organization-specific dedup logic without duplication.
- [ ] Attempting to merge a Person-typed Party with an Organization-typed Party is rejected.

## Open Questions

- Whether any future base-level Party fields beyond identification/contact-information emerge as genuinely shared needs between Person and Organization — none anticipated now; revisit only if a concrete symmetric need surfaces.
- Exact NIEM release/version and precise `nc:EntityType` element names — same technical-spec-level verification task noted in the other Master Records docs.
