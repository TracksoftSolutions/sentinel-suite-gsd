# After-Action Reports (AAR)

## Overview

AAR closes out an EOC Activation or Incident with a compiled retrospective: what happened (Timeline Reconstruction), what should change (Improvement Action Plan), and a lighter summary for leadership (Executive AAR Briefing). Four elicited decisions shape it, two of them deliberate departures from what this module's prior docs would predict:

1. **AAR is its own independent Activity extension, not a Form Definition inside ICS Forms Engine** — per explicit user direction. ICS Forms Engine stays scoped to the periodic, Operational-Period-driven FEMA 200-series catalog it was built for; AAR is a one-time, post-hoc retrospective compiled after the fact, closer in shape to DAR/Incident Report's original report pattern (Draft → Publish, immutable Document snapshot via Document Registry) than to a recurring operational form. No new report mechanism is invented — this doc reuses that original pattern directly, the same one ICS Forms Engine itself generalized SITREP out of.
2. **Improvement Action Plan gets the full tracked-task lifecycle here, not a lightweight seed handed to the next doc** — per explicit user direction, a deliberate departure from this module's usual seam pattern (Access Credential Management → Clearance Profiles, Key Ring Registry → Lock Core). An Improvement Action is a real, independently-lifecycled record (owner, target date, status, evidence) that keeps evolving after its parent AAR Document publishes — the same "the report snapshots a point in time, the underlying live record keeps evolving" split DAR already established for its own Activities. **Improvement Plan (IP) Tracking (next doc) is flagged as the likely promoter of this mechanism** — MODULES.md frames it as a central registry aggregating corrective tasks "from drills or incidents" (plural sources), suggesting it generalizes/rolls this doc's per-AAR list up across many AARs and future drill-sourced items, rather than reinventing the task record itself. Not resolved here — flagged for that doc's own elicitation round.
3. **AAR anchors to both EOC Activation and Incident** (the platform's established Full/Limited widened-anchor shape, reused a third time after Operational Period and ICS Role Assignment), with a future **Exercise** anchor type forward-referenced to Module 5's not-yet-built Exercise & Drill Planner — HSEEP's actual primary AAR trigger, not buildable yet since that module doesn't exist.
4. **Executive AAR Briefing is a derived companion, never independently authored** — generated on demand from an already-published AAR's own fields (situation summary excerpt, Improvement Action counts by status, top recommendations), itself Document-snapshotted for shareability but with zero manually-entered content of its own, so it can never drift from the AAR it summarizes.

Timeline Reconstruction reuses Historical CAD Log Reconstruction's activity-axis mechanism directly (an anchor's full descendant tree, unbounded depth — already solved by that doc) and links in any ICS Forms Engine Documents published for the same anchor (SITREPs, ICS-207, ICS-214 Activity Logs) as supplementary source material, without pulling AAR itself through that engine.

## Actors & Roles

- **Incident Commander / EOC Coordinator / Safety Director-equivalent** — compiles and publishes the AAR; generates the Executive Briefing.
- **Improvement Action owner** (any assigned Person, platform user or not) — updates their own action's status, attaches evidence.
- **Supervisor** — reviews overdue Improvement Actions, may reassign or on-behalf-of an unfilled owner.
- **Any user with permission on the anchor** — views the published AAR and Executive Briefing.

## User Stories

- As an **Incident Commander**, once an EOC Activation deactivates, I want to compile an AAR pulling in the full timeline without re-assembling it by hand.
- As a **Safety Director**, I want a short executive briefing I can hand to leadership without making them read the full AAR.
- As an **Incident Commander**, I want to assign a specific recommendation to a specific owner with a real due date, not just write "someone should fix this" in a narrative paragraph.
- As an **Improvement Action owner**, I want to mark my action complete and attach proof (a photo, a signed training log) directly to the record.
- As a **Supervisor**, I want to be notified when an Improvement Action's due date passes without it being marked complete.
- As an **EOC Coordinator**, I want a minor Incident that never triggered a full EOC Activation to still be eligible for a lightweight AAR if it's worth a formal retrospective.
- As a **compiler**, I want the AAR to reference the activation's published SITREPs and ICS-214 logs as source material, without re-typing what's already documented there.

## Functional Requirements

### AAR Report (independent Activity extension)
1. **AAR Report** registers as its own Activity extension (`entity_id` shared PK, FK → Activity), anchored via `anchor_type`/`anchor_ref` to an **EOC Activation** or an **Incident** — the same widened-anchor shape ICS Role Assignment and Operational Period already established. A third `exercise` anchor type is reserved in the field but not yet reachable — forward reference only, pending Exercise & Drill Planner.
2. Generating an AAR Draft is a manual, compiler-initiated action (Command/Action Bus), available once the anchor has closed (EOC Activation `deactivated`, Incident `concluded`) — never auto-triggered on closure. Beginning a retrospective is a deliberate human decision, the same instinct behind every other manual-trigger-only mechanism on the platform (Generate Report, Publish SITREP).
3. **Timeline Reconstruction** embeds/links Historical CAD Log Reconstruction's existing activity-axis view for the anchor (its full descendant tree, unbounded depth — already resolved by that doc, no new mechanism) plus references to any ICS Forms Engine Documents published against the same anchor (SITREP/ICS-209, ICS-207, ICS-214 Activity Logs) as supplementary reading — links, not re-ingested data.
4. Narrative fields (`situation_summary`, `what_went_well`, `what_needs_improvement`) are manually authored, with an AI-assist option via a new `aar_narrative` AI Context (AI/LLM Services) drafting from the anchor's Timeline Reconstruction data — the same "AI proposes, human confirms" discipline every prior narrative-generation consumer already follows; drafted content is `source = ai_generated` and never enters the published Document until the compiler explicitly approves it.
5. **Publishing** snapshots the AAR Draft — narrative, linked timeline, and the current Improvement Action list at that moment — into an immutable Document via Document Registry's hash/version model, exactly like every other report-shaped feature on the platform. A published AAR is never reopened; a new retrospective on the same anchor (e.g., after further review) is a fresh AAR Report instance.

### Improvement Action Plan (full lifecycle, built here)
6. **Improvement Action** registers as its own Activity extension (`aar_report_ref` — direct field, fixed at creation, the same non-EntityAssociation parent-link reasoning Incident Update and Response Timeline Event already use), carrying `recommendation`, an optional `category` tag (an ICS Section value or free tag), `proposed_owner_ref` (a real Person, inline-created if not already registered — the durable/dedup-worthy-relationship camp, not a free-text assignee), `target_completion_date`, `actual_completion_date` (nullable), and `status` (`open` → `in_progress` → `completed`/`cancelled`).
6a. **Retrofit — [Improvement Plan (IP) Tracking](improvement-plan-ip-tracking.md):** `aar_report_ref` widens to `source_type` (`aar_report`, `ad_hoc`, `drill` [reserved]) + `source_ref` (nullable) — an Improvement Action created during AAR drafting still sets `source_type = aar_report`/`source_ref = this AAR's entity_id`, unchanged in every other respect; the widening only adds two new ways an Improvement Action can otherwise come to exist (a direct ad hoc entry, and a reserved future Drill/Exercise origin), owned entirely by that doc.
7. **An Improvement Action created during AAR drafting keeps its own independent lifecycle after the parent AAR publishes** — its status, owner, and evidence remain live and editable indefinitely, exactly like DAR's underlying Activities stay live after a DAR Document snapshots them. The published AAR Document shows the Improvement Action list as it stood at publish time; the live records themselves are the ongoing source of truth for whether the work actually got done.
8. **Evidence** attaches to an Improvement Action via Document Registry's existing upload/attachment mechanism (photos, signed training logs) — no new file-handling mechanism, the same reuse pattern every other evidence/attachment need on the platform already follows.
9. **Overdue Improvement Action** registers as a Duration Watchdog instance watching `(improvement_action, status, open` or `in_progress)` against a **dynamic threshold** (`target_completion_date` directly) — reusing Key Custody & Auditing's dynamic-threshold resolution mode a second time, now on an Activity rather than an EntityAssociation. Escalates via Notifications Engine to the owner and Supervisor, optionally the tenant's Critical Event Escalation Policy — never blocking anything, the platform's standing "escalate, don't block" rule.

### Executive AAR Briefing (derived companion)
10. **Executive AAR Briefing** generation is only available once its parent AAR has published — it can never summarize a still-mutable Draft. Every field is computed at generation time from the published AAR: a situation-summary excerpt, Improvement Action counts by status, and the top-N recommendations by category — zero independently-authored content.
11. Each generation is itself snapshotted into its own immutable Document via Document Registry — a briefing generated today and one generated next month (after several Improvement Actions have since completed) are two distinct, independently point-in-time Documents, never one that silently updates.

## Data Model / Fields

**AAR Report** (Activity extension; entity_id is the shared PK, FK → Activity)
- anchor_type (eoc_activation, incident, exercise [reserved, not yet reachable]), anchor_ref
- situation_summary, what_went_well, what_needs_improvement (narrative; each carries `source`: manual, ai_generated)
- status (drafting, published), compiled_by (nullable until published), published_document_ref (nullable)

**Improvement Action** (Activity extension; entity_id is the shared PK, FK → Activity)
- aar_report_ref (direct field, fixed at creation)
- recommendation, category (nullable)
- proposed_owner_ref (Person, inline-creatable)
- target_completion_date, actual_completion_date (nullable)
- status (open, in_progress, completed, cancelled)
- evidence_document_refs[] (Document Registry attachments)

**Executive AAR Briefing** (derived; no independently-authored fields)
- briefing_id, aar_report_ref, generated_at, generated_by, published_document_ref
- situation_summary_excerpt, improvement_action_counts_by_status{}, top_recommendations[] — all computed at generation time from the parent AAR

## States & Transitions

- **AAR Report:** `drafting` → `published` (terminal, immutable). A new retrospective on the same anchor is a fresh instance, never a reopen.
- **Improvement Action:** `open` → `in_progress` → `completed` | `cancelled` — independent of the parent AAR Report's own state; remains fully live and mutable after the parent publishes.
- **Executive AAR Briefing:** no persistent draft state — each generation directly produces its own immutable Document.

## Integrations

- **Historical CAD Log Reconstruction**: source of Timeline Reconstruction's activity-axis mechanism, reused directly and unmodified.
- **ICS Forms Engine**: published forms for the same anchor (SITREP/209, ICS-207, ICS-214) are linked as supplementary AAR source material — this doc stays an independent mechanism per explicit user direction, not a consumer of that engine.
- **ICS Role Mapping & Visual Org Chart / Multi-Incident Console**: source of the EOC Activation anchor and its `deactivated` closure trigger.
- **Incident Reporting & Management**: source of the Incident anchor and its `concluded` closure trigger.
- **Document Registry**: AAR publish and Executive Briefing generation both snapshot into immutable Documents; Improvement Action evidence attachment reuses the same upload mechanism.
- **Active Call Alerts & Timers**: overdue Improvement Action is a registered Duration Watchdog instance (dynamic threshold), Critical Event Escalation Policy the optional second-tier surface — no new alerting mechanism.
- **AI / LLM Services**: owns the new `aar_narrative` AI Context, Prompt Template, and provider/BYO-key plumbing.
- **Entity Registry Core / Party Registry / Person Registry**: Improvement Action's `proposed_owner_ref`, including inline creation.
- **Command/Action Bus**: Generate AAR Draft, Publish AAR, Generate Executive Briefing, Create/Update Improvement Action, and Attach Evidence all register as actions.
- **Improvement Plan (IP) Tracking (Module 5, not yet specified)**: forward reference — likely generalizes/aggregates this doc's per-AAR Improvement Action mechanism across multiple AARs and future drill-sourced action items into one central cross-anchor registry, per MODULES.md's own "from drills or incidents" framing. Explicitly not solved here — this doc's Improvement Action list is scoped to its own parent AAR only, no cross-AAR rollup view.
- **Exercise & Drill Planner (Module 5, not yet specified)**: forward reference only — AAR's `anchor_type` already accepts a future `exercise` value with no restructuring needed once that module exists.

## Permissions

| Action | Incident Commander / EOC Coordinator | Improvement Action owner | Supervisor | Any permitted viewer |
|---|---|---|---|---|
| Generate/publish AAR | ✅ | ❌ | ✅ (on-behalf-of) | ❌ |
| Create/edit an Improvement Action | ✅ | ❌ | ✅ | ❌ |
| Update own Improvement Action status / attach evidence | — | ✅ | ✅ (on-behalf-of) | ❌ |
| Generate Executive AAR Briefing | ✅ | ❌ | ✅ | ✅ (same as AAR view) |
| View published AAR / Executive Briefing | inherits the anchor's existing RBAC/ABAC | | | ✅ |

## Non-Functional / Constraints

- An Improvement Action's live status/owner/evidence are never frozen by its parent AAR Document's publish — only the AAR's own narrative and timeline snapshot; the underlying task keeps evolving, the same DAR-established split between a report snapshot and its live source records.
- Overdue Improvement Action alerting never blocks AAR publish, anchor closure, or any other operational action.
- Executive AAR Briefing generation is blocked (not merely discouraged) until its parent AAR has published — there is no "preview" mode over a mutable Draft.
- All narrative fields snapshot at AAR publish time — never live-updating inside a published Document, the platform's standard snapshot-immutability discipline.

## Acceptance Criteria

- [ ] An AAR can be generated against a deactivated EOC Activation or a concluded Incident, but not against either while still active/open.
- [ ] Timeline Reconstruction shows the anchor's full activity tree via the existing Historical CAD Log Reconstruction mechanism, plus links to any published SITREP/ICS-207/ICS-214 Documents for the same anchor.
- [ ] Publishing an AAR Draft creates an immutable Document via Document Registry and locks the Draft's narrative/timeline snapshot permanently.
- [ ] Creating an Improvement Action with an unregistered proposed owner prompts inline Person creation — it never accepts a free-text name.
- [ ] An Improvement Action's status and evidence remain editable after its parent AAR publishes, with no lock inherited from the parent's own immutability.
- [ ] An Improvement Action whose `target_completion_date` passes without reaching `completed` raises a Duration Watchdog instance, notifying the owner and Supervisor, without blocking anything.
- [ ] Generating an Executive AAR Briefing against an unpublished AAR is rejected.
- [ ] A generated Executive AAR Briefing's Improvement Action counts match the parent AAR's live Improvement Action records at the moment of generation, and two briefings generated at different times can legitimately show different counts.
- [ ] AI-drafted narrative content never appears in a published AAR Document until the compiler explicitly approves it.
- [ ] Attaching evidence to an Improvement Action reuses Document Registry's existing upload mechanism — no separate file-handling path is introduced.

## Open Questions

- Exact AAR/HSEEP-aligned template content (which narrative sections, whether a NIMS-209-style structured summary is expected alongside the free narrative) — a content/config design task, not committed here.
- Whether Improvement Plan (IP) Tracking should retrofit this doc's Improvement Action into a shared cross-anchor registry, or leave it AAR-local and build its own separate aggregation/rollup view on top — an open architectural question for that doc's own elicitation round, deliberately left unresolved here per the user's direction to build the full lifecycle in AAR now regardless of what the next doc does with it.
- Whether an AAR should support multiple contributors (per-Section input, mirroring SITREP's `per_section_contribution` mode) rather than single-compiler-only — not elicited; single-compiler assumed for day one.
- Exact reconciliation once a future Exercise anchor exists (Exercise & Drill Planner) — forward reference only, not solved here.
