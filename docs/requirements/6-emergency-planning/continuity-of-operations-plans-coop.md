# Continuity of Operations Plans (COOP)

## Overview

COOP (Module 6, 3/8) registers a tenant's essential business functions and the leadership succession order that keeps them running through a disruption — MODULES.md's own narrow framing: **Essential Function Registry** (prioritized list of functions that must survive) and **Succession Hierarchies** (legally aligned delegation maps). Per explicit user direction on how to approach reuse across this session — "the idea with the elicitation is finding overlap to generalize and then specify" — this doc finds two real overlaps with existing mechanisms and generalizes them, while keeping COOP's own concepts genuinely distinct rather than flattening them into what already exists.

Five elicited decisions, all the recommended option except where noted:

1. **Scope stays narrow, forward-referencing Module 22 (Business Continuity & Disaster Recovery, not yet built) for deeper mechanics.** Essential Function Registry gets a criticality tier and light recovery-relevant fields; real Business Impact Analysis scoring, RTO/RPO metrics, and full recovery-workflow orchestration are explicitly deferred to that future module — the same narrow-now/forward-reference-later discipline used throughout this session.
2. **Succession is Position-based, not Person-based, via a new interim Business Position registry** — continuity of authority attaches to an office/title ("the COO"), not a specific individual who might leave, the same "Person or Position, resolved at time of use" shape already proven by EOC Call-up Roster and ICS Role Assignment. Business Position is deliberately a light, plain-field interim stand-in (name only, no history) pending Module 8 Personnel, the same DAR Shift Window / EOC Call-up Roster deferred-integration posture — not the same concept as ICS Role Mapping's "Position" (which is incident-command-specific), a distinct name chosen to avoid the naming collision Drill Compliance Logging already caught once with "drill."
3. **COOP Activation is its own named Activity extension, deliberately independent of EOC Activation, but plugs into the same generalized activation pattern that mechanism already established.** Per explicit user direction — reuse the *mechanism*, keep the *concept* distinct — Checklist Run's anchor (already widened once by Incident Action Checklists to `eoc_activation`/`call`/`incident`) gains a fourth value, `coop_activation`; Checklist Template's `category` gains a fourth value, `coop_response`, mirroring `incident_response`'s addition exactly. A COOP event and an EOC emergency response are related-but-independent: a fire is an EOC Activation; the fact that it also renders a building unusable for months is a COOP Activation — the two can co-occur (an optional `related_eoc_activation_ref` cross-reference) or occur alone, and deactivating one never implicitly deactivates the other.
4. **COOP leadership uses Business Position/Succession Hierarchy, not ICS Role Assignment** — business-continuity authority (who leads recovery of a function) and incident-command authority (who's Incident Commander) are different leadership contexts, kept independent even when a COOP Activation and an EOC Activation co-occur.
5. **Essential Function forward-references the not-yet-built Alternate Site Registries with a bare `alternate_site_ref` pointer** (a Location, no new mechanism) — the same seam pattern used repeatedly (Key Ring Registry → Lock Core, Access Credential Management → Clearance Profiles); the richer relocation-site data (power/data/workspace verification) is that later doc's job.

## Actors & Roles

- **Continuity Coordinator / Site Admin** — authors Essential Functions, Business Positions, and Succession Hierarchies; activates/deactivates COOP.
- **Tenant Admin** — same authoring scope tenant-wide; invokes succession.
- **Business Position holder** — the currently-resolved individual whose authority is in question during an event.
- **Any authorized user** — views the Essential Function Registry and Succession Hierarchies read-only, per ordinary site RBAC/ABAC.

## User Stories

- As a **Continuity Coordinator**, I want to register our essential business functions with a criticality tier so we know what must be restored first after a disruption.
- As a **Continuity Coordinator**, I want to define who succeeds whom for key leadership positions by title, not by name, so the plan doesn't go stale the moment someone changes roles.
- As a **Continuity Coordinator**, I want to activate a COOP response independent of a full emergency response, since a building becoming unusable for months isn't always also an active emergency.
- As a **Tenant Admin**, I want to formally invoke succession for a specific position during an active COOP event, recording who assumed authority and when, for later legal and audit review.
- As a **Continuity Coordinator**, I want to link an Essential Function to its recovery documentation and, eventually, its alternate relocation site.

## Functional Requirements

### Essential Function (Document extension)
1. **Essential Function** registers as a Document extension (TPT: `entity_id` shared PK, FK → `Document.entity_id`) — inherits `document_title`/`description`/`version_history[]`/hash/authorship from Document, adding: `criticality_tier_ref` (a new tenant-configurable **Essential Function Criticality Definition**, `sort_order` for threshold comparison — the same shape as Call Priority/Incident Severity Definition), `owning_position_ref` (nullable, FK → Business Position), `succession_hierarchy_ref` (nullable, FK → Succession Hierarchy), `alternate_site_ref` (nullable, FK → Location — forward reference), and `dependencies[]` (free-text notes on systems/resources/staff the function needs).
2. **EssentialFunctionAttachmentAssociation** (EntityAssociation; `entity_id_a` = the Essential Function, `entity_id_b` = an attached Document) lets a function reference recovery-procedure documents — mirrors `PreplanAttachmentAssociation` exactly, no new attachment mechanism.
3. **Deep Business Impact Analysis scoring, RTO/RPO metrics, and full recovery-workflow orchestration are explicitly out of scope**, deferred to Module 22 when it's specified — `criticality_tier_ref`'s ordering is today's lightweight proxy for "how fast must this come back," not a real BIA.

### Business Position & Succession Hierarchy (interim, pending Module 8)
4. **Business Position** (tenant-configured registry, interim): `position_id`, `title`, `department` (nullable), `current_holder_ref` (nullable, FK → Person, inline-created if needed) — a plain field, deliberately not a tracked-history association, since this is an explicitly interim stand-in for real Personnel data. A vacant position surfaces as vacant everywhere it's referenced, never silently dropped — the same EOC Call-up Roster discipline.
5. **Succession Hierarchy** (tenant-authored, versioned): `name`, `scope_note` (e.g., "Executive Authority," "IT Operations Leadership"), an ordered `chain[]` of Business Position references defining delegation order for that scope of authority. Deliberately Position-based end to end.
6. An Essential Function's `succession_hierarchy_ref` is optional and independent — a Succession Hierarchy can stand alone (e.g., a tenant-wide executive chain with no single owning function) or be referenced by one or more Essential Functions.

### COOP Activation
7. **COOP Activation** registers as its own Activity extension (own `entity_id`, distinct from EOC Activation) — `status` (`active` → `deactivated`), `impacted_function_refs[]` (Essential Function references), `related_eoc_activation_ref` (nullable) for the case where a COOP event co-occurs with a formal emergency response, without either anchoring or containing the other.
8. **Checklist Run's `anchor_type` gains a fourth value, `coop_activation`; Checklist Template's `category` gains a fourth value, `coop_response`** *(retrofit, see [eoc-activation-checklists.md](../5-emergency-management/eoc-activation-checklists.md) and [incident-action-checklists.md](incident-action-checklists.md))* — the identical widened-anchor/category pattern Incident Action Checklists already used once, applied a second time. A `trigger_on_activation` Checklist Template auto-creates a Checklist Run the moment its matching activation kind fires (EOC Activation for `activation`/`system_verification`/`incident_response`-category templates that opt in, COOP Activation for `coop_response`-category templates) — the existing Domain Events rule widened to a second trigger source, no new automation mechanism.
9. **COOP Activation deliberately does not use ICS Role Assignment** — business-continuity leadership resolves through Business Position/Succession Hierarchy instead, kept independent of incident-command structure even when a COOP Activation and an EOC Activation co-occur via `related_eoc_activation_ref`.

### Invoke Succession
10. **Invoke Succession** (Command/Action Bus action, confirmation-gated **and** step-up authentication, audit-tier — given the real legal/authority-declaring weight MODULES.md itself flags with "legally aligned delegation maps") records that authority for a Business Position passed to the next holder in its Succession Hierarchy chain during an active COOP Activation. Creates an immutable **Succession Invocation** record — never silently mutating `current_holder_ref` itself, which stays a standing HR-ish fact Module 8 will eventually own properly; this is a temporary, event-scoped delegation record, not a permanent reassignment.

## Data Model / Fields

**Essential Function Criticality Definition** (tenant-configurable Definition)
- tier_id, tenant_id, name, sort_order

**Essential Function** (TPT level: `entity_id` is the shared PK, FK → `Document.entity_id`)
- entity_id (PK, FK → Document; document_title/description/version_history[]/hash inherited, not redefined here)
- criticality_tier_ref (FK → Essential Function Criticality Definition)
- owning_position_ref (nullable, FK → Business Position)
- succession_hierarchy_ref (nullable, FK → Succession Hierarchy)
- alternate_site_ref (nullable, FK → Location — forward reference, richer Alternate Site Registry data deferred)
- dependencies[] (free-text notes)

**EssentialFunctionAttachmentAssociation** (TPT level: `association_id` shared PK, FK → `EntityAssociation.association_id`)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Essential Function, entity_id_b = an attached Document)
- no extra fields beyond the base EntityAssociation shape

**Business Position** (tenant-configured registry, interim pending Module 8)
- position_id, tenant_id, title, department (nullable), current_holder_ref (nullable, FK → Person)

**Succession Hierarchy** (tenant-authored, versioned plan; not an Activity)
- hierarchy_id, tenant_id, name, scope_note, version, status (active, archived)
- chain[] (sequence_number, position_ref)

**COOP Activation** (Activity extension; entity_id is the shared PK, FK → Activity)
- status (active, deactivated)
- impacted_function_refs[] (Essential Function references)
- related_eoc_activation_ref (nullable)

**Succession Invocation** (local record, one per Invoke Succession action)
- invocation_id, position_ref, hierarchy_ref, invoked_position_holder_ref (FK → Person), invoked_by, invoked_at, coop_activation_ref

## States & Transitions

- **Essential Function:** inherits Document's `active` → `tombstoned` (merged away) → `active` (merge reversed) lifecycle unmodified.
- **Business Position:** plain create/edit/remove, no independent lifecycle.
- **Succession Hierarchy:** `active` → `archived`, standard versioned-Definition lifecycle, same discipline as Route/Checklist Template.
- **COOP Activation:** `active` → `deactivated`, mirrors EOC Activation's own lifecycle exactly.
- **Succession Invocation:** created once per invocation, immutable, append-only.

## Integrations

- **Document Registry**: Essential Function's TPT base — hash/version/authorship mechanism, dedup/merge inherited for free.
- **Location Registry**: source of `alternate_site_ref` — a bare pointer today.
- **EOC Activation Checklists / Incident Action Checklists** *(retrofit)*: Checklist Run's `anchor_type` and Checklist Template's `category` each gain a fourth value (`coop_activation`/`coop_response`); the existing auto-create-on-activation Domain Events rule widens to a second trigger source.
- **Domain Events**: owns "COOP Activation created → auto-create matching Checklist Run(s)" alongside the existing EOC Activation rule.
- **Command/Action Bus**: Activate/Deactivate COOP Activation, Invoke Succession register as actions.
- **Authentication & Authorization**: Invoke Succession requires step-up authentication, per that doc's existing AAL-gated step-up mechanism.
- **Structured Logging & Audit Trails**: COOP Activation lifecycle and every Succession Invocation are audit-tier.
- **Module 8 (Personnel, not yet specified)**: forward reference only — Business Position is an interim stand-in for real job-title/HR data (holder history, formal appointment records), the same deferred posture as DAR's Shift Window and EOC Call-up Roster.
- **Module 22 (Business Continuity & Disaster Recovery, not yet specified)**: forward reference — deeper BIA scoring, RTO/RPO metrics, and full Recovery Workflows belong there; this doc keeps only `criticality_tier_ref` as a lightweight proxy.
- **Alternate Site Registries** (later, this module): forward reference — `alternate_site_ref` is a bare Location pointer today; richer relocation-site data is that doc's job.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Continuity Coordinator/Site Admin | Any authorized user |
|---|---|---|---|---|
| Author Essential Function / Business Position / Succession Hierarchy | ✅ | ✅ | ✅ (own scope) | ❌ |
| Activate / deactivate COOP Activation | ✅ | ✅ | ✅ | ❌ |
| Invoke Succession (confirmation-gated, step-up) | ✅ | ✅ | ✅ (own scope) | ❌ |
| View Essential Function Registry / Succession Hierarchy | ✅ | ✅ | ✅ | ✅ (per site RBAC/ABAC) |

## Non-Functional / Constraints

- Invoke Succession requires step-up authentication and passes the Command/Action Bus confirmation gate given its legal/authority-declaring weight — no exceptions or bypass paths.
- A vacant Business Position never silently fails — surfaces as vacant wherever referenced (Succession Hierarchy display, Essential Function ownership), the same discipline already established for vacant ICS Positions and EOC Call-up Roster entries.
- COOP Activation and EOC Activation remain independently activatable/deactivatable — deactivating one never implicitly deactivates the other, even when `related_eoc_activation_ref` links them.
- WCAG 2.1 / Section 508 accessible authoring and activation flows, day one.

## Acceptance Criteria

- [ ] Registering an Essential Function with a criticality tier, an owning Business Position, and an attached recovery-procedure Document succeeds and is independently versioned per Document Registry's model.
- [ ] A Business Position with no `current_holder_ref` surfaces as vacant wherever referenced, never silently omitted.
- [ ] Activating a COOP Activation independent of any EOC Activation succeeds; setting `related_eoc_activation_ref` links it to a co-occurring EOC Activation without either anchoring the other.
- [ ] Deactivating an EOC Activation that has a related COOP Activation does not deactivate the COOP Activation, and vice versa.
- [ ] A `trigger_on_activation` Checklist Template registered under `category = coop_response` auto-creates a Checklist Run the moment a COOP Activation fires, and does not fire on an EOC Activation.
- [ ] Invoking Succession for a vacant or unavailable Business Position during an active COOP Activation requires step-up authentication and creates an immutable, audit-tier Succession Invocation record.
- [ ] Essential Function's `alternate_site_ref` correctly resolves to a real Location today, with no dependency on Alternate Site Registries existing yet.

## Open Questions

- Exact reconciliation once Module 8 Personnel exists and can supply real job-title/HR data for Business Position (holder history, formal appointment records) — forward reference only, same posture as DAR's Shift Window.
- Exact reconciliation once Module 22 (BC/DR) is specified for deeper BIA scoring/RTO-RPO metrics/full Recovery Workflows onto Essential Function — this doc keeps only a criticality-tier proxy.
- Exact reconciliation once Alternate Site Registries (later, this module) is specified for the richer relocation-site data `alternate_site_ref` only bare-points to today.
- Whether COOP Activation should ever need something ICS-Role-Assignment-shaped of its own (a "Continuity Role Assignment"), rather than relying solely on Business Position/Succession Hierarchy — considered and deliberately not built now, since this doc's own decision keeps the two leadership contexts independent; flagged if a future need to bridge them appears.
- Whether Invoke Succession should ever support automatic (non-human-triggered) invocation under narrowly defined conditions (e.g., a Business Position holder confirmed unreachable for N hours) — not committed here, a plausible future Duration-Watchdog-style generalization once real usage patterns are observed.
