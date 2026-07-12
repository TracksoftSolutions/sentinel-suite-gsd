# Organization Registry

**Module:** 0.5 Master Records
**Status:** Draft — elicited, ready for technical spec

## Overview

Organization Registry specifies **Organization**, the second Extension Type of the base **Party** Entity Type (per Party Registry — alongside Person), structured per NIEM Core's `nc:OrganizationType`. Organization is the canonical record for any company, agency, or business entity the platform references: subcontractor security agencies, client companies, vendors, mutual-aid partner agencies, and — per this doc's design — the tenant's own company where "owned by us" needs to be expressed as a real Party reference rather than a special-cased flag.

Like Vehicle, Organization is deliberately **uniform** — no base-level category field (vendor, client, subcontractor, tenant-company). Per the same pattern already established, an organization's *relationship* to the platform is expressed through associations with other records (a Contract's counterparty, a Site's managing company, a Subcontractor Agreement's agency) rather than a flat field that would mean different things to different consuming modules.

Organization inherits Party's identity, deduplication, merge, and BOLO-eligibility mechanics without reimplementing them.

## Actors & Roles

- **Contract/Client Coordinator, Subcontractor Coordinator, Tenant Admin** — create/maintain Organization records within their permission scope.
- **Records Admin** — resolves Entity Registry Core deduplication flags for Organization entities specifically.
- **Future consuming modules** (Subcontractor Management, Contract & Client Management, Vehicle/Conveyance Registry's ownership field, Document Registry's author field): reference Organization entity_ids or associate with them, rather than building their own company-record concept.

## User Stories

- As a **Subcontractor Coordinator**, I want to register a security subcontracting agency once and have every module that references it (schedules, roster verification, billing) use the same canonical record.
- As a **Fleet Coordinator**, I want a company-owned vehicle's `ConveyanceOwnerAssociation` to point to our own tenant's Organization record, using the same mechanism as a personally-owned vehicle, rather than a special "is this ours" flag.
- As a **Records Admin**, I want two Organization records for the same subcontracting agency (registered by two different Site Admins) flagged as a likely duplicate, consistent with how Person/Item/Location dedup already works.
- As a **Client Coordinator**, I want to register a client company's Organization record and later associate it with a Contract, a set of Sites, and a set of authorized Contacts (Persons) — all via associations, not fields baked into the Organization record itself.
- As a **Document Registry consumer** (future), I want a policy document's issuing author to be able to reference an Organization (e.g., "Corporate Security Department") as easily as a Person.

## Functional Requirements

### Base Organization fields (`nc:OrganizationType`-aligned)
1. **Name**: `organization_name` — a single organization name field (not structured like Person's given/sur name, since organization names don't decompose the same way).
2. **Description**: free-text description of the organization.
3. **Sub-unit structure**: parent/subsidiary nesting (e.g., a regional branch of a national security company) is recorded via the same `HierarchyAssociation` TPT subtype of EntityAssociation that Location Registry uses for its own parent/child nesting — not a separate, duplicated mechanism. Rich org-chart tooling, if ever needed, would be a future module's concern built on top of it.
4. **Contact information & identification**: inherited from Party Registry's base fields (`contact_information`, `identifications[]`) — an Organization's `identifications[]` populates categories relevant to a company (tax ID/EIN, business license number, DUNS number) using the structure Party already provides.
5. No base-level category field — an organization's purpose/relationship is expressed via association with other records, not a flat field here.
6. **Display label** (per Entity Registry Core's universal requirement): template strategy, `organization_name`.

### The tenant's own organization
7. A tenant may optionally designate one Organization record as representing **itself** (the tenant's own company) — this is what lets fields needing "owned by us vs. owned by an external party" (e.g., Vehicle's owner) resolve to a real Party reference in both cases, rather than a special third branch. Not every tenant needs to create this record; consuming fields treat an absent/null Party reference as an acceptable "unspecified/default" state where that's the more natural behavior for their use case (per that field's own doc).

### Deduplication
8. Organization-specific match signals for Entity Registry Core's deduplication engine: exact match on tax ID/business license identification is highest confidence; organization name + address as a secondary strong signal; name-similarity-only as a fuzzy fallback. Never auto-merged, per Entity Registry Core's universal governance.

## Data Model / Fields

**Organization** (TPT level: entity_id is the shared PK, FK → Party.entity_id — structured per `nc:OrganizationType`)
- entity_id (PK, FK → Party; `identifications[]` and `contact_information` are inherited from Party, not redefined here)
- organization_name, description

*(Parent/subsidiary nesting is a `HierarchyAssociation` row — see Location Registry's Data Model for the shared subtype definition — not a field here.)*

## States & Transitions

**Organization:** `active` → `inactive` (e.g., a dissolved or no-longer-engaged organization) → `active` (reactivated). Independent of Entity Registry Core's standard `active`/`tombstoned` states, which apply on top (an inactive Organization can still later be tombstoned via merge).

## Integrations

- **Party Registry**: owns the base Party Entity Type Organization extends, including shared `identifications[]` and `contact_information`.
- **Entity Registry Core**: owns the shared identity mechanics (global ID, deduplication, merge) and generic BOLO Flag mechanism (available if a future module needs to flag an organization, e.g., a debarred vendor).
- **Location Registry**: source/owner of the `HierarchyAssociation` TPT subtype this doc reuses for parent/subsidiary nesting.
- **Vehicle/Conveyance Registry**: consumes Organization entity_ids as one possible resolution of a vehicle's `ConveyanceOwnerAssociation` (alongside Person and the tenant's own designated Organization).
- **Subcontractor Management, Contract & Client Management** (future): primary owners of the rich associations (contracts, service agreements, roster verification) that give an Organization record its practical meaning — this doc provides only the canonical identity.
- **Document Registry**: consumes Organization entity_ids as one possible resolution of a document's `DocumentAuthorAssociation`.
- **Entity Relationships & History**: consumes Organization entity IDs to build cross-module interaction timelines.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Contract/Subcontractor Coordinator | Records Admin |
|---|---|---|---|---|
| Create/view Organization record | ✅ | ✅ | ✅ | ✅ |
| Add/remove HierarchyAssociation (reparent) | ✅ | ✅ | ✅ (own scope) | ❌ |
| Designate the tenant's own Organization record | ✅ | ✅ (own tenant) | ❌ | ❌ |
| Resolve deduplication flags | ✅ | ✅ | ❌ | ✅ |

## Non-Functional / Constraints

- `HierarchyAssociation` must not permit cycles, consistent with Location Registry's identical constraint on its own use of the same subtype.
- Only one Organization record per tenant may be designated as "the tenant's own" at a time.
- WCAG 2.1 / Section 508 accessible Organization record views and creation flows, day one.

## Acceptance Criteria

- [ ] A base Organization record supports name, description, and inherited Party contact/identification fields, each independently optional; parent nesting is a HierarchyAssociation, not a field.
- [ ] Two Organization records with matching tax ID are flagged as a potential duplicate per Entity Registry Core's model, never auto-merged.
- [ ] A Tenant Admin designates an Organization record as the tenant's own; a Vehicle's ConveyanceOwnerAssociation can then correctly resolve to it.
- [ ] Attempting to create a `HierarchyAssociation` for an Organization that would create a cycle is rejected.
- [ ] A subsidiary Organization's active HierarchyAssociation correctly resolves to its parent company's Organization record.

## Open Questions

- Whether a formal org-chart/hierarchy visualization tool is ever needed beyond the basic `HierarchyAssociation` — deferred until a concrete module (likely Subcontractor Management or Contract & Client Management) surfaces the need.
- Exact NIEM release/version and precise `nc:OrganizationType` element names — same technical-spec-level verification task noted in the other Master Records docs.
