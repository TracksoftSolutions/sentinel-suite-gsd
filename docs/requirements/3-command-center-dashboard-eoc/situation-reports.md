# Situation Reports (SITREPs)

## Overview

Situation Reports mirror ICS Role Mapping's two-tier split, one report type per tier. **SITREP** is the full ICS-209-style structured form, anchored to an active **EOC Activation** — periodic, recurring (Background Job Processing-driven), with per-ICS-Section status blocks (situation summary, resources committed, actions taken, planned actions, safety issues). **Incident Status Report** is its lighter Limited-ICS counterpart, anchored directly to any ordinary Incident — ad hoc only, no Section structure, no recurring cadence, for the trespasser-leaves-on-challenge case that's worth logging but never worth periodically reporting on.

Both reuse the platform's established report architecture end to end: an **AI Draft** working record (mutable, never auto-published — the same discipline AI-Assisted Incident Report Writing established for narrative content) is what a compiler reviews and explicitly **publishes** into an immutable Document via Document Registry's hash/version model, exactly like DAR/Incident Report's snapshot pattern. SITREP's recurring generation is a third consumer of Background Job Processing's recurring-job registry; its overdue alerting is a new Duration Watchdog instance, no new alerting mechanism; its narrative drafting is a new AI Context, the third consumer of AI/LLM Services' "AI proposes, human confirms" discipline.

## Actors & Roles

- **Incident Commander / Planning Section (Situation Unit)** — typically the SITREP compiler in `single_compiler` mode; always the final publisher regardless of authorship mode.
- **ICS Section Chief** (Operations, Planning, Logistics, Finance/Admin — via their current ICS Role Assignment) — contributes their own section's entry in `per_section_contribution` mode.
- **EOC Coordinator / Supervisor** — overrides cadence/authorship mode per-activation; generates ad hoc Incident Status Reports on ordinary Incidents.
- **Tenant Admin** — sets the tenant's default SITREP cadence interval and authorship mode via Settings & Preferences.
- **Any user with permission on the Incident/EOC Activation** — views published SITREPs/Incident Status Reports once they exist.

## User Stories

- As **Planning Section**, I want the system to auto-draft a new SITREP at each cadence interval from live incident data, so I'm not building it from scratch every few hours.
- As a **Section Chief** in `per_section_contribution` mode, I want to fill in only my own section's status and have it roll into one consolidated report, without needing visibility into every other section's raw data.
- As an **EOC Coordinator**, I want to override the default SITREP interval for this specific activation — every 2 hours during a fast-moving event, every 12 during a lull.
- As a **Supervisor**, I want to be notified if a SITREP goes overdue, so command doesn't lose situational awareness without anyone noticing.
- As a **Guard** at a mandatory-adoption tenant logging a minor Incident, I want a simple ad hoc Incident Status Report, not the full ICS-209 form's Section fields I don't need.
- As **Planning Section**, I want the Resources Committed block auto-populated from live Unit State counts rather than re-typing headcounts I could get wrong.
- As **Planning Section**, I want to review and edit an AI-drafted situation summary before publishing — never have it go out unreviewed.

## Functional Requirements

### SITREP (Full, EOC Activation-anchored)
1. **SITREP** is anchored to an active EOC Activation, structured per **ICS Section** (command, operations, planning, logistics, finance_admin — the same taxonomy ICS Role Mapping's Position already uses): each Section carries a narrative, an auto-populated resources-committed snapshot, actions taken, and planned actions, plus an overall situation summary and next-update-due timestamp at the report level.
2. A recurring **Background Job** (registered per this doc, `trigger_kind = recurring`, idempotency key `sitrep:{eoc_activation_id}:{period_start}`) generates a new **SITREP Draft** for the active EOC Activation at the resolved interval (per-activation override, else tenant default — see Cadence & Authorship Configuration). The Draft is pre-populated with live data (see Resource Auto-Population, AI-Assisted Drafting) but is never itself the official report.
3. **Publishing** a SITREP Draft snapshots it into an immutable Document via Document Registry's existing hash/version model — the same point-in-time, never-retroactively-updated discipline as every other report type on the platform. A new period always produces a brand-new Draft; a published SITREP is never reopened or edited.

### Incident Status Report (Limited, Incident-anchored)
4. **Incident Status Report** is the lighter Limited-ICS counterpart: situation summary, resources committed, actions taken, planned actions — no Section fields, since Limited scope has no Section structure to report against. Generated ad hoc only (Command/Action Bus action, no recurring cadence, no Duration Watchdog) against any Incident, independent of whether an EOC Activation exists.
5. Follows the same Draft-then-publish two-step as SITREP (AI-assist consistency — see FR #8) even though generation is manual: a compiler reviews/edits before explicitly publishing to an immutable Document.

### Cadence & Authorship Configuration
6. A tenant-level **SITREP Cadence Policy** (Settings & Preferences-registered) sets the **default interval** (e.g., every 4/12/24 hours — exact presets are a content/config concern, not committed here) and the **default authorship mode**: **`single_compiler`** (one person, typically the IC or Planning Section, fills in every Section) or **`per_section_contribution`** (each Section Chief submits their own section's entry; a compiler assembles and publishes the whole). Both are overridable per-activation by the IC/EOC Coordinator at any point while the EOC Activation is active — the platform's established explicit-beats-default resolution chain, unchanged from every other tenant-default-with-override mechanism.
7. Deactivating an EOC Activation halts future recurring SITREP draft generation (retrofit — see Integrations); an already-generated, unpublished Draft remains publishable afterward as the activation's final SITREP.

### AI-Assisted Drafting
8. A new AI Context (`sitrep_narrative`) drafts the situation summary and/or per-Section narratives from live data — placeholders include the Incident/EOC Activation's Update timeline, live Unit State counts, and current Duration Watchdog states. Drafted content is marked `source = ai_generated` and is never part of the published Document until the compiler (or, in `per_section_contribution` mode, that section's contributor) explicitly reviews and approves it — the same "AI proposes, human confirms" discipline already established for Incident Update narratives, its third platform consumer after Incident Narrative and CLI-assist.

### Resource Auto-Population
9. Each Section's (or, for Incident Status Report, the whole report's) resources-committed block auto-populates from Status & State Monitors' live Unit State, grouped by `state`, for units relevant to the anchor's site — a compiler/Section Chief may add narrative context on top but doesn't hand-retype headcounts. The populated snapshot locks at publish time exactly like every other field — a SITREP never silently drifts from what Unit State shows afterward.

### Overdue Alerting
10. A SITREP that hasn't published within its resolved interval registers as a **Duration Watchdog** instance (reusing Active Call Alerts & Timers' generalized `(activity_type, watched_field, watched_value)` mechanism — no new alerting logic), optionally escalating via the tenant's configured **Critical Event Escalation Policy**. Overdue alerting never blocks any operational action, including EOC Activation deactivation — escalate, don't block, the same rule Feature Management established for quota caps.

## Data Model / Fields

**SITREP Cadence Policy** (Settings & Preferences registration, tenant-level)
- tenant_id, default_interval_hours, default_authorship_mode (single_compiler, per_section_contribution)

**EOC Activation** *(retrofit — additive fields only)*
- sitrep_interval_hours_override (nullable), sitrep_authorship_mode_override (nullable)

**SITREP Draft** (mutable working record, local to this doc)
- draft_id, eoc_activation_ref, period_start, next_update_due_at
- overall_situation_summary, command_post_ref (snapshot of the current Command Post Designation at generation time)
- status (drafting, published), compiled_by (nullable until published), published_document_ref (nullable, set on publish)

**SITREP Section Entry** (child of SITREP Draft)
- entry_id, draft_ref, section (command, operations, planning, logistics, finance_admin)
- narrative, resources_committed_snapshot, actions_taken, planned_actions
- contributed_by (nullable — resolves via that section's current ICS Role Assignment), source (manual, ai_generated), ai_approved (bool)

**Incident Status Report** (local, Limited scope)
- report_id, incident_ref
- situation_summary, resources_committed_snapshot, actions_taken, planned_actions
- status (drafting, published), compiled_by (nullable until published), published_document_ref (nullable)

## States & Transitions

- **SITREP Draft / Incident Status Report:** `drafting` → `published` (terminal, immutable). A new period/ad hoc trigger always creates a fresh Draft — a published record is never reopened.
- **EOC Activation** *(no new states — retrofit only adds the recurring-job halt on `deactivated`, per FR #7)*.

## Integrations

- **ICS Role Mapping & Visual Org Chart** *(retrofit)*: EOC Activation gains the cadence/authorship override fields above; its `deactivated` transition additionally halts future recurring SITREP generation (light addition to that doc's existing deactivation cascade, which already closes Role Assignments and Command Post Designations). Section Entry's `contributed_by` resolves via ICS Role Assignment's current holder for that section; Command Post Designation is the source of `command_post_ref`.
- **Background Job Processing**: owns the recurring `sitrep_generate_draft` job type — one instance per active EOC Activation, isolation-tier-aware placement, idempotent per period, same registration discipline as every other recurring job on the platform.
- **AI / LLM Services**: owns the `sitrep_narrative` AI Context, its Prompt Template, and provider/BYO-key plumbing — this doc only declares the context and its placeholders, never talks to a provider directly.
- **Document Registry**: publish snapshots a Draft into an immutable Document, same hash/version model every other report type uses.
- **Status & State Monitors**: source of live Unit State counts auto-populating resources-committed snapshots.
- **Active Call Alerts & Timers**: overdue-SITREP is a registered Duration Watchdog instance; Critical Event Escalation Policy is the optional second-tier escalation surface, no new mechanism.
- **Incident Reporting & Management**: Incident Status Report is generated against an ordinary Incident using the same permission posture as that doc's existing participant-tagging actions.
- **Settings & Preferences**: owns the SITREP Cadence Policy tenant Definition.
- **Command/Action Bus**: Generate/Edit/Publish SITREP or Incident Status Report, and per-activation cadence/authorship override, all register as actions.
- **Command Center Wallboard View**: a published SITREP/Incident Status Report is a natural `org_chart`-adjacent or `detail`-panel target; no new panel type needed.

## Permissions

- **Configure the tenant's default SITREP Cadence Policy**: Tenant Admin.
- **Override cadence/authorship mode for a specific EOC Activation**: EOC Coordinator/IC.
- **Contribute a Section Entry** (`per_section_contribution` mode): the Person currently holding that section's ICS Role Assignment, or on-behalf-of by EOC Coordinator/Supervisor.
- **Compile/publish a SITREP** (either mode): IC/Planning Section role holder, or EOC Coordinator/Supervisor.
- **Generate/publish an ad hoc Incident Status Report**: same permission as tagging participants on that Incident (Incident Reporting & Management's existing posture) — Guard/Investigator (own/assigned incidents), Supervisor, Records Admin.
- **View a published SITREP/Incident Status Report**: inherits the underlying EOC Activation/Incident's existing RBAC/ABAC posture — no new permission introduced.

## Non-Functional / Constraints

- Recurring draft generation runs via Background Job Processing at the resolved interval; idempotent per `(eoc_activation_id, period_start)` — a re-triggered job for an already-generated period no-ops rather than duplicating a Draft.
- All auto-populated live-data fields (resources-committed counts, AI-drafted narrative) snapshot at generation/publish time — never live-updating inside a published Document, consistent with the platform's snapshot-immutability discipline used everywhere else.
- Overdue alerting is additive notification only — it never blocks EOC Activation deactivation, Incident closure, or any other operational action.

## Acceptance Criteria

- [ ] At the resolved interval (activation override, else tenant default), a new SITREP Draft auto-generates for an active EOC Activation with pre-populated Resources Committed counts from live Unit State.
- [ ] In `per_section_contribution` mode, a Section Chief can submit only their own section's entry; the compiler assembles and publishes the full SITREP once ready.
- [ ] In `single_compiler` mode, one person fills in every Section of the Draft before publishing.
- [ ] Publishing a SITREP Draft creates an immutable Document via Document Registry and locks the Draft permanently — it never mutates afterward, even if the underlying Incident/EOC Activation data later changes.
- [ ] AI-drafted narrative content never appears as final published content until explicitly approved by the compiler or contributing Section Chief.
- [ ] An EOC Activation with no SITREP published within its resolved interval triggers a Duration Watchdog instance, escalating per Critical Event Escalation Policy if configured, without blocking any operational action.
- [ ] Deactivating an EOC Activation halts future recurring SITREP generation; an already-generated unpublished Draft remains publishable as the activation's final SITREP.
- [ ] A Limited-ICS Incident (no EOC Activation) supports an ad hoc Incident Status Report — situation summary/resources/actions/planned actions only, no Section fields, no recurring cadence.
- [ ] Overriding the interval or authorship mode for one EOC Activation does not affect the tenant default other activations use.

## Open Questions

- Exact default cadence interval(s) (e.g., 4/12/24 hours) and whether tenants need more than one preset — content/config design, not committed here.
- Exact consolidation UX in `per_section_contribution` mode — whether the compiler is notified as each section submits, or must manually check completeness before publishing — a technical-spec/UX-flow decision.
- Whether Incident Status Report should later gain AI-assist/per-contributor structure if a tenant's Limited-ICS incidents turn out to need more than currently scoped — flagged, not solved.
- Whether an overdue SITREP should also register as a Command Center Wallboard View Health Signal (e.g., surfaced on a Health panel tile) — plausible, not committed here; a light future retrofit candidate once real usage patterns are known.
