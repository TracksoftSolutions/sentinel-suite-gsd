# Hazard Identification & Risk Assessment (HIRA)

## Overview

HIRA (Module 6, 6/8) builds the general risk-assessment methodology MODULES.md frames narrowly (Risk Assessment Matrix, Threat Directories, HIRA Reports for executive planning) — evaluating named threats (weather, human, structural, and beyond) against probability/impact scores, with a real history per threat rather than a single static snapshot.

Four elicited decisions:

1. **A combined shape, not a single pattern picked wholesale**: **Threat Directory Entry** registers as a Document extension — Preplan-shaped, versioned, carrying the threat's narrative/reference content — while each actual scoring occurrence is its own recurring **Risk Assessment** Activity extension, Compliance Drill Log-shaped, child-linked to the directory entry. This gives both rich versioned reference content (mitigation notes, description) and a real, queryable risk-score-over-time history, rather than forcing one shape to do both jobs.
2. **Threat Directory Entry anchors to an optional Location** (any granularity, matching Preplan's own pattern exactly), closing Pre-Incident Plans' own forward reference: a located, elevated-risk threat contributes to that doc's `hazard_flags[]` dispatch-context surfacing mechanism; an unlocated, tenant/site-wide strategic threat (e.g., "Cyber Disruption") stays purely in the executive register.
3. **A high-risk finding is a bare forward reference to Mitigation Task Tracker (next doc, not built here)**, not routed through the existing Improvement Action / IP Tracking mechanism — the same seam pattern used repeatedly (Key Ring Registry → Lock Core, Access Credential Management → Clearance Profiles, Preplan's `suggested_checklist_refs[]` → Incident Action Checklists), since that next doc's own budget-tracking framing suggests a genuinely different record shape than Improvement Action carries.
4. **Reassessment reuses the existing Approaching-Deadline Reminder mechanism** (Preplan Review Reminder, Mutual Aid Agreement renewal) — no new alerting infrastructure, never blocking any operational action.

## Actors & Roles

- **Safety Coordinator / Facility Safety Officer** — creates/edits Threat Directory Entries, performs Risk Assessments.
- **Site / Tenant Admin** — configures Threat Type Definition and Risk Tier Definition.
- **EOC Coordinator / Emergency Manager** — reviews the live Threat Directory and generates HIRA Reports.
- **Executive / read-only viewer** — consumes generated HIRA Reports only.
- **Dispatcher / Responder** — consumes `hazard_flags[]` surfaced via Preplans; no direct HIRA-authoring role.

## User Stories

- As a **Safety Coordinator**, I want to register a named threat once (e.g., "Seismic Risk" or "Winter Storm at Loading Dock") and score it periodically, so I can see whether our risk posture is improving or worsening over time.
- As a **Safety Coordinator**, I want to tie a location-specific hazard to the actual Location, so a dispatched responder sees it automatically without me maintaining a second list.
- As a **Tenant Admin**, I want our own risk-tier thresholds (what counts as High vs. Critical) configurable, not hardcoded to a value that doesn't fit our operation.
- As an **Emergency Manager**, I want a compiled risk summary I can hand to executives without manually assembling one from a dozen individual assessments.
- As a **Safety Coordinator**, I want a reminder before a threat's assessment goes stale, without it blocking anything else I'm doing.

## Functional Requirements

### Threat Type Definition
1. **Threat Type Definition** (tenant/site Definition) carries `category` (`weather`, `human`, `structural`, `other` — MODULES.md's own three named buckets plus an escape hatch), `name`, `description`, `status` (`active`/`archived`) — the same lightweight tenant-registered-type shape as Call Type Definition.

### Threat Directory Entry (Document extension)
2. **Threat Directory Entry** registers as a Document extension (TPT: `entity_id` shared PK, FK → `Document.entity_id`) — inherits `document_title`/`description`/`version_history[]`/hash/`DocumentAuthorAssociation` from Document unmodified, adding: `threat_type_ref`, `location_ref` (nullable, FK → Location, any granularity), `mitigation_notes`, `next_review_due_date`. Mirrors Preplan's own Document-extension shape exactly.
3. **A located Threat Directory Entry whose latest Risk Assessment resolves to a tenant-flagged "surfaces on dispatch" Risk Tier contributes a `hazard_flags[]` entry to Preplans' dispatch-context surfacing mechanism** *(retrofit — closes that doc's own forward reference)*. An unlocated entry never surfaces there, staying purely in the executive HIRA register.
4. Threat Directory Entry gets the same dedup discipline as Preplan: an identical `(threat_type_ref, location_ref)` pair on two active entries surfaces to Entity Registry Core's existing human-reviewed dedup workflow as a likely duplicate — never auto-merged.

### Risk Assessment (Activity extension, recurring)
5. **Risk Assessment** registers as its own Activity extension (`activity_type = risk_assessment`), one row per scoring occurrence, child-linked to its Threat Directory Entry via `directory_entry_ref` — the Document-shaped reference register with an Activity-shaped occurrence layered on top, the combined pattern elicited decision #1 established.
6. Carries `probability_score` and `impact_score` (tenant-configurable scale, default 1–5 each), a computed `risk_score` (`probability_score × impact_score`), `assessed_by`, `assessed_at`, `notes`.
7. `risk_score` resolves to a tenant-configurable **Risk Tier Definition** (`sort_order` + threshold range — the same shape as Call Priority/Incident Severity/Essential Function Criticality Definition), e.g. Low/Medium/High/Critical.
8. A Risk Assessment resolving to a tenant-flagged "requires mitigation" Risk Tier reserves a nullable `mitigation_task_ref` forward pointer, resolved entirely by Mitigation Task Tracker (next doc) — no mechanics specified here, matching the bare-forward-reference seam pattern used throughout the platform.
9. Risk Assessment registers `is_mergeable = false` — a routine, timestamped scoring occurrence with no duplicate-identity concept, the same explicit opt-out already declared for Checkpoint Scan/Safety Check-in/Compliance Drill Log.

### Reassessment Reminder
10. Threat Directory Entry's `next_review_due_date` is watched by a new **Approaching-Deadline Reminder** registration (`record_type = threat_directory_entry`, `watched_date_field = next_review_due_date`) *(retrofit — Active Call Alerts & Timers)* — the same lead-time-before-a-deadline mechanism already used for Preplan Review Reminder and Mutual Aid Agreement renewal, zero new alerting infrastructure. Never blocks any operational action.

### HIRA Report (derived executive compilation)
11. **HIRA Report** is a derived companion Document, shaped like AAR's Executive AAR Briefing — zero independent authoring, every field computed at generation time from currently-active Threat Directory Entries and each one's latest Risk Assessment, filterable by threat category/site/date range. Each generation is its own immutable Document — two reports generated at different times can legitimately disagree as new assessments land in between.
12. Generation is **ad hoc-only** (Command/Action Bus action, "Generate HIRA Report") — no recurring-cadence auto-generation, since an executive compilation is pulled on demand, unlike SITREP's periodic cadence.

## Data Model / Fields

**Threat Type Definition** (tenant/site Definition)
- type_id, tenant_id, site_ref (nullable)
- category (weather, human, structural, other), name, description
- status (active, archived)

**Threat Directory Entry** (Document extension; entity_id shared PK, FK → Document.entity_id)
- threat_type_ref
- location_ref (nullable, FK → Location)
- mitigation_notes
- next_review_due_date (nullable)

**Risk Assessment** (Activity extension; entity_id shared PK, FK → Activity.entity_id; activity_type = risk_assessment)
- directory_entry_ref
- probability_score, impact_score
- risk_score (computed)
- risk_tier_ref
- mitigation_task_ref (nullable — forward reference, Mitigation Task Tracker's job)
- assessed_by, assessed_at, notes

**Risk Tier Definition** (tenant Definition, Settings & Preferences-registered)
- tier_id, tenant_id, name, sort_order, min_score, max_score
- requires_mitigation (bool), surfaces_on_dispatch (bool)

**HIRA Report** (Document extension; entity_id shared PK, FK → Document.entity_id)
- generated_at, generated_by
- filter_criteria (threat_category, site_ref, date_range)
- summary_entries[] (directory_entry_ref, threat_type, location_ref, latest_risk_score, risk_tier_ref, trend — computed from the entry's Risk Assessment history)

## States & Transitions

- **Threat Type Definition / Risk Tier Definition:** `active` → `archived`, standard tenant Definition lifecycle.
- **Threat Directory Entry:** created, edited (new version via `version_history[]`), archived — standard Document-extension lifecycle, no separate status machine.
- **Risk Assessment:** created once, immutable — no status machine; a re-assessment is a new row, not an edit.
- **HIRA Report:** generated once per invocation, immutable Document snapshot.

## Integrations

- **Document Registry**: Threat Directory Entry's and HIRA Report's shared base (hash/version/authorship).
- **Location Registry**: source of Threat Directory Entry's optional `location_ref`.
- **Pre-Incident Plans (Preplans)** *(retrofit)*: Threat Directory Entry contributes `hazard_flags[]` entries, closing that doc's own forward reference — Preplans requires no structural change, only a real producer for a field it already reserved.
- **Entity Registry Core**: dedup/merge review for Threat Directory Entry's `(threat_type_ref, location_ref)` match signal.
- **Active Call Alerts & Timers** *(retrofit)*: gains `threat_directory_entry` as a new Approaching-Deadline Reminder `record_type` registrant.
- **Mitigation Task Tracker** (next doc, forward reference only): resolves Risk Assessment's `mitigation_task_ref` — no mechanics specified here.
- **Command/Action Bus**: Perform Risk Assessment, Generate HIRA Report register as actions.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin | Safety Coordinator | EOC Coordinator/Emergency Manager | Executive (read-only) |
|---|---|---|---|---|---|---|
| Configure Threat Type Definition / Risk Tier Definition | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Create/edit Threat Directory Entry | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Perform Risk Assessment | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| View Threat Directory / Risk Assessment history | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Generate HIRA Report | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| View HIRA Report | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- Risk Tier Definition thresholds and the `requires_mitigation`/`surfaces_on_dispatch` flags are tenant-configurable, never fixed platform defaults — a DOE lab and a hotel will weight severity differently.
- Reassessment reminders never block any operational action — the platform's standard compliance-tracking-never-gates discipline.
- A Threat Directory Entry's `hazard_flags[]` contribution (FR #3) is audit-tier, the same as every other dispatch-context-affecting mechanism.
- HIRA Report generation is read-only and side-effect-free — never mutates the underlying Threat Directory Entries or Risk Assessments it summarizes.

## Acceptance Criteria

- [ ] Creating a Threat Directory Entry with no `location_ref` succeeds and never appears in Preplans' `hazard_flags[]` surfacing.
- [ ] Creating a located Threat Directory Entry, performing a Risk Assessment that resolves to a `surfaces_on_dispatch` Risk Tier, correctly contributes a `hazard_flags[]` entry visible on Preplans' dispatch-context surfacing for that Location.
- [ ] Two Risk Assessments on the same Threat Directory Entry, at different times, both persist independently — the directory entry's history shows a real score trend, not just the latest value.
- [ ] A Risk Assessment resolving to a `requires_mitigation` Risk Tier reserves a nullable `mitigation_task_ref`, unresolved until Mitigation Task Tracker exists.
- [ ] A Threat Directory Entry approaching its `next_review_due_date` fires the Approaching-Deadline Reminder at the configured lead time, and never blocks any other action if it lapses unaddressed.
- [ ] Generating a HIRA Report produces an immutable Document reflecting current Threat Directory/Risk Assessment state at that moment; a later generation after new assessments land produces a legitimately different snapshot.
- [ ] An identical `(threat_type_ref, location_ref)` pair on two active Threat Directory Entries surfaces as a likely duplicate via Entity Registry Core's existing dedup workflow.

## Open Questions

- Exact default Risk Tier Definition scale/thresholds (Low/Medium/High/Critical cut points) — a content concern, not resolved here.
- Whether Threat Directory Entry's `hazard_flags[]` contribution should eventually reconcile with the separate, not-yet-built Module 7 Hazmat & Chemical Registries / NFPA 704 Placard Mapping pair — both are legitimate feeds into the same mechanism (a chemical-specific placard hazard vs. a general structural/weather/human threat); not resolved here, flagged for whenever that Module 7 pair is specified.
- Mitigation Task Tracker's own record shape (owner, budget tracking, status lifecycle) is entirely that doc's job — this doc only reserves `mitigation_task_ref` as a bare forward pointer.
- Whether Risk Assessment's probability/impact scale should be a fixed 1–5 or itself tenant-configurable (a 1–3 or 1–10 scale) — a content/technical-spec concern, not committed here.
