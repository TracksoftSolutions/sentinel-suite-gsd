# Alternate Site Registries

## Overview

Alternate Site Registries closes Module 6 (Emergency Planning, 8/8), resolving Continuity of Operations Plans' own reserved forward reference — the richer relocation-site data (power/data/workspace verification) that doc explicitly deferred here. **Alternate Site Designation** is the fourth Module 6 instance of the Document-extension, Location-anchored pattern already established by Preplan, Threat Directory Entry (HIRA), and Essential Function (COOP) — no new base architecture, just this module's proven shape applied to a fourth concept.

Two elicited decisions:

1. **Essential Function's single `alternate_site_ref` widens to an ordered `alternate_site_refs[]`** *(retrofit — COOP)* — real continuity planning commonly designates a primary and a secondary/tertiary backup for the same critical function, not just one. Many-to-many either direction: one Alternate Site Designation can serve multiple Essential Functions, and a function can rank several.
2. **Transit Plan Guides reuse Checklist Template's ordered-steps shape, plan-definition only — no Checklist Run.** True turn-by-turn GIS routing is out of scope platform-wide (Unit Dispatch already deferred real driving-distance/ETA routing pending a routing-capable adaptor); a Transit Plan Guide is reference content staff read during relocation, not a compliance task that gets checked off — the platform's first deliberately partial reuse of the Checklist mechanism (the plan half only, no execution-tracking half).

## Actors & Roles

- **Continuity Coordinator / Site Admin** — designates Alternate Sites, records resource verification, authors Transit Plan Guides. (Same actors COOP already established.)
- **Facility Coordinator** — performs and records power/data/workspace verification checks.
- **Tenant Admin** — same authoring scope tenant-wide.
- **Any authorized user** — views Alternate Site detail sheets and Transit Plan Guides read-only, per ordinary site RBAC/ABAC.

## Functional Requirements

### Alternate Site Designation (Document extension)
1. **Alternate Site Designation** registers as a Document extension (TPT: `entity_id` shared PK, FK → `Document.entity_id`) — inherits `document_title`/`description`/`version_history[]`/hash/`DocumentAuthorAssociation` from Document, adding: `location_ref` (FK → Location, required — the alternate facility itself; may be a newly created Location if not already tracked, e.g. a leased external building, or an existing tenant Site serving double duty as another site's backup), `site_type` (`owned`, `leased`, `partner`, `reciprocal`), `capacity_notes`, `transit_plan_checklist_ref` (nullable, FK → Checklist Template).
2. **`resource_specifications[]`** (type: `power`, `data_connectivity`, `workspace_capacity`, `other`; `detail`; `verified` bool; `verified_by`; `verified_at`) — the same typed-list-with-verification shape Preplan's `safety_structures[]` already established, giving MODULES.md's "verification charts" real, per-item structure rather than free text.
3. **An unverified resource specification is surfaced honestly, never blocking** — an Essential Function can reference an Alternate Site Designation with zero verified resources; the gap is visible on the detail sheet, not hidden or enforced against, the platform's standard compliance-tracking-never-gates discipline.
4. Contact information for the site (property manager, facility contact) is **not duplicated here** — it's already available via the referenced Location's own `FacilityManagerAssociation`, reused as-is.
5. **Dedup match signal**, matching Preplan/Threat Directory Entry's own discipline: an identical `location_ref` on two active Alternate Site Designations surfaces to Entity Registry Core's human-reviewed dedup workflow as a likely duplicate — never auto-merged.

### Transit Plan Guide (Checklist Template, plan-only)
6. **Checklist Template gains a fifth `category` value, `transit_plan`** *(retrofit — EOC Activation Checklists, after `activation`/`system_verification`/`incident_response`/`coop_response`)* — an ordered list of steps ("proceed to X, then Y") authored the same way any other Checklist Template is, reused here purely for its ordered-list shape.
7. **No Checklist Run is ever created for `transit_plan`** — a deliberate, first-of-its-kind partial reuse: this category exists purely as reference guidance staff read during an actual relocation, not an operational task with per-item checked/skipped tracking. `anchor_type` is not extended to accommodate it, since nothing ever anchors a Run to it.
8. A Transit Plan Checklist Template is a reusable, independent definition — one Alternate Site Designation's `transit_plan_checklist_ref` may point at a Template shared with another designation if the route/procedure is genuinely identical (e.g., two functions relocating to the same building), or its own dedicated one.

### Essential Function retrofit (ranked alternate sites)
9. **Essential Function's `alternate_site_ref` (singular) is retired for `alternate_site_refs[]`** *(retrofit — COOP)*: an ordered list of `(rank, alternate_site_designation_ref)` — `rank = 1` is primary, `rank = 2` is secondary, and so on. Existing single-value data migrates to a one-entry, `rank = 1` list; the field now points at this doc's richer Alternate Site Designation instead of a bare Location.
10. **A designated backup EOC needs no new mechanism**: MODULES.md's own "designated backup EOC" framing is satisfied by ICS Role Mapping's existing Command Post Designation simply referencing an Alternate Site Designation's `location_ref` like any other Location when an EOC Activation relocates — zero new field, zero new integration, the association layer already accepts any Location.

## Data Model / Fields

**Alternate Site Designation** (Document extension; entity_id shared PK, FK → Document.entity_id)
- location_ref (FK → Location, required)
- site_type (owned, leased, partner, reciprocal)
- resource_specifications[] (type: power, data_connectivity, workspace_capacity, other; detail; verified: bool; verified_by; verified_at)
- capacity_notes
- transit_plan_checklist_ref (nullable, FK → Checklist Template)

**Checklist Template** *(retrofit — EOC Activation Checklists)*
- category gains `transit_plan` (alongside activation, system_verification, incident_response, coop_response)

**Essential Function** *(retrofit — COOP)*
- alternate_site_ref (removed)
- alternate_site_refs[] (rank, alternate_site_designation_ref) — new

## States & Transitions

- **Alternate Site Designation:** created, edited (new version via `version_history[]`) — standard Document-extension lifecycle, no separate status machine.
- **Transit Plan Guide (Checklist Template, `category = transit_plan`):** standard Template versioning (edits create a new version) — never has a Run, so no execution lifecycle applies.

## Integrations

- **Continuity of Operations Plans (COOP)** *(retrofit)*: Essential Function's alternate-site reference widens from singular to a ranked list, now pointing at this doc's richer record.
- **Location Registry**: source of Alternate Site Designation's `location_ref`, and of the referenced Location's own `FacilityManagerAssociation` for site contact info.
- **EOC Activation Checklists** *(retrofit)*: Checklist Template's `category` catalog gains `transit_plan`.
- **ICS Role Mapping & Visual Org Chart**: Command Post Designation may reference an Alternate Site Designation's Location when an EOC relocates — no structural change to that doc.
- **Entity Registry Core**: dedup/merge review for Alternate Site Designation's `location_ref` match signal.
- **Document Registry**: Alternate Site Designation's hash/version/authorship base.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Continuity Coordinator/Site Admin | Facility Coordinator | Any authorized user |
|---|---|---|---|---|---|
| Create/edit Alternate Site Designation | ✅ | ✅ | ✅ | ❌ | ❌ |
| Record resource verification | ✅ | ✅ | ✅ | ✅ | ❌ |
| Author Transit Plan Guide (Checklist Template) | ✅ | ✅ | ✅ | ❌ | ❌ |
| Rank an Alternate Site on an Essential Function | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Alternate Site detail sheet / Transit Plan Guide | ✅ | ✅ | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- Resource verification status (`verified`/`verified_by`/`verified_at`) is always visibly disclosed on the detail sheet — never implying a resource is confirmed when it isn't.
- Ranking or re-ranking an Essential Function's `alternate_site_refs[]` never blocks on any unverified resource at the target site.
- Transit Plan Guide edits are versioned exactly like any other Checklist Template — an in-flight relocation reads whatever version was current when the guide was opened, consistent with the platform's plan-mutability-during-active-execution discipline, even though no Run tracks the occurrence.

## Acceptance Criteria

- [ ] Creating an Alternate Site Designation against a newly-created Location (not previously tracked) succeeds.
- [ ] An Essential Function can rank two Alternate Site Designations (primary, secondary) and both display in rank order.
- [ ] An Alternate Site Designation with zero verified `resource_specifications[]` entries can still be ranked on an Essential Function, with the gap visibly disclosed.
- [ ] A Transit Plan Checklist Template (`category = transit_plan`) never produces a Checklist Run under any circumstance.
- [ ] Two Alternate Site Designations sharing the same `location_ref` surface as a likely duplicate via Entity Registry Core's dedup workflow.
- [ ] An Alternate Site Designation's referenced Location's `FacilityManagerAssociation` contact correctly displays on the detail sheet with no duplicated contact fields on the designation itself.

## Open Questions

- Whether resource verification should eventually gain a recurring re-verification reminder (Approaching-Deadline Reminder, matching Preplan Review Reminder/HIRA's reassessment reminder) — a plausible future enhancement, not built here since MODULES.md doesn't name a cadence requirement for this doc specifically.
- Whether Transit Plan Guide should eventually gain a lightweight execution/acknowledgment layer (e.g., "I've read this guide") if real usage shows staff need a confirmation record — explicitly not built now, since MODULES.md frames this as a reference guide, not a compliance task.
- Exact `site_type` taxonomy (owned/leased/partner/reciprocal) is a content concern; a `reciprocal` arrangement (a mutual pre-agreement with another organization to host each other during a disruption) may eventually want to cross-reference Mutual Aid Agreements Tracker's Organization-to-Organization shape — flagged, not resolved here.
