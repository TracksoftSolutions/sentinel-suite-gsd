# Incident Reporting & Management

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Three layers, continuing Activity Registry's established patterns and directly reusing DAR's report-generation architecture:

1. **Incident** — the case-level Activity extension (fulfilling Activity Registry's already-anticipated `incident` activity_type): category, severity, status, location, and participants (witnesses, victims, suspects, involved vehicles, etc.) via the standard generic association mechanisms.
2. **Incident Update** — a thin Activity extension, the incremental note/entry mechanism: any authorized responder/investigator adds timestamped updates over the incident's life (initial report, follow-up, resolution note) — the same "timeline of thin Activity entries" shape as Checkpoint Scan/Patrol Finding, giving a real who-said-what-when audit trail rather than a single mutable field.
3. **Incident Report** — a Document snapshot of a filtered set of Incident Updates, directly reusing DAR's "report = filtered Activity view snapshotted to Document" architecture. Report **scope mode is tenant-configurable**: **master** (one consolidated report per Incident, including every responder's updates) or **per-responder** (one report per authoring responder, each containing only their own updates) — mirroring how multi-officer incidents are actually documented in practice, where sometimes one unified report is wanted and sometimes each officer's own account needs to stand alone.

A single **Supervisor Review** governs the whole Incident regardless of report scope mode — the same governance-record-doesn't-mutate-what-it-references pattern established by DAR's Shift Review — gating the Incident's move to fully `closed`. Report-generation mode (auto-on-sign-off / ad hoc-only / both) is tenant-configurable, the exact same three-way toggle DAR already established.

**Severity is tenant-configurable and drives escalation.** Reaching a configured severity level publishes an automation-eligible domain event, letting a Tenant Admin configure notification/escalation behavior via Domain Events — the same "trigger owned by Domain Events, effect by Command/Action Bus" split used everywhere else in the platform, not a hardcoded alert path.

**Escalation from an upstream record is a first-class, tracked link.** An Incident created via the established launch-point mechanism (from a Guard Tour Checkpoint Scan, a Patrol Finding, a DAR Entry, a Courtesy Patrol, or a Citation context) records `escalated_from_ref` — a direct field, fixed at creation, giving a clean audit trail from first observation to formal incident without forcing every Incident to have one (a self-initiated incident just leaves it null).

**Scope boundary:** this doc covers the Incident record, its update timeline, review, and reporting mechanics. AI-assisted narrative drafting is explicitly out of scope — deferred to AI-Assisted Incident Report Writing (next in the elicitation queue), which will enhance Incident Update's own narrative field, not introduce a separate one.

## Actors & Roles

- **Guard/Responder** — creates an Incident (self-initiated or escalated), adds Incident Updates, tags participants.
- **Investigator** — adds Incident Updates, manages participant tagging (witness/suspect/victim roles), may reference the incident from a future Case File.
- **Supervisor** — performs the Supervisor Review, signs off, may generate ad hoc report snapshots.
- **Tenant Admin** — configures Category Definitions, Severity Definitions, report scope mode, and report-generation mode via Settings & Preferences.
- **Records Admin** — resolves Entity Registry Core dedup flags on Incident (directly feeding Dispatch/CAD's future Incident Merging feature, per Activity Registry).

## User Stories

- As a **Guard**, I want to escalate a Patrol Finding I logged into a full Incident without re-entering the location or basic details.
- As an **Investigator**, I want to add a follow-up update to an incident three days later and have it clearly timestamped and attributed, distinct from the original report.
- As a **Supervisor**, I want to review and sign off on an incident before it's considered closed, the same way I already review a shift's DAR.
- As a **Tenant Admin**, I want incidents involving multiple responding officers to each produce their own standalone report, since that's how our jurisdiction requires documentation — while another of our sites is fine with one consolidated report.
- As a **Tenant Admin**, I want a Critical-severity incident to immediately notify the on-duty Supervisor and EOC contact, without a guard needing to remember to call anyone.
- As an **Investigator**, I want every incident a specific suspect was tagged in to show up on their profile's timeline automatically, using the same association mechanism as everywhere else.

## Functional Requirements

### Incident (Activity extension)
1. **Incident** registers as an Activity extension, fulfilling Activity Registry's already-anticipated `incident` activity_type: inherits base identity, offline-safe numbering, standard dedup/merge (directly powering Dispatch/CAD's future Incident Merging feature), participant/attachment/location associations, and display-label requirements.
2. `category` references a tenant-configurable Category Definition (e.g., Theft, Assault, Trespass, Medical, Fire, Property Damage, Suspicious Activity — same tenant-configurable-taxonomy pattern established throughout Module 1).
3. `severity` references a tenant-configurable Severity Definition (e.g., Low, Medium, High, Critical).
4. Participants use Activity Registry's standard `ActivityParticipantAssociation` with an Incident-specific role vocabulary: witness, victim, suspect, reporting_officer, responding_unit, involved_vehicle, seized_weapon. The Guard/Investigator who creates the Incident is automatically tagged `reporting_officer`. *(Retrofit, by ICS Role Mapping & Visual Org Chart)* At a tenant whose ICS Adoption Policy is `mandatory_limited`, this default and the `responding_unit` role are both superseded for officer/unit participants: the creator is automatically given an ICS Role Assignment for Incident Commander instead of a plain `reporting_officer` tag, and associating any additional officer/unit requires an explicit ICS Position selection rather than the plain `responding_unit` default. Non-officer roles (witness, victim, suspect, involved_vehicle, seized_weapon) are unaffected either way.
5. `escalated_from_ref` (nullable, direct field, fixed at creation) records the upstream record (a Patrol Finding, DAR Entry, Checkpoint Scan, Courtesy Patrol, or Citation) an Incident was escalated from, when created via the platform's established launch-point mechanism.
6. Evidence/photos attach via the standard `ActivityAttachmentAssociation` to Document; full evidentiary chain-of-custody is explicitly deferred to Investigation Management's future Digital/Physical Evidence Tracking, not built here.

### Incident Update (thin Activity extension — the timeline)
7. **Incident Update** registers as its own thin Activity extension: `origin_incident_ref` (direct field, fixed at creation, same non-EntityAssociation reasoning as Checkpoint Scan/Patrol Finding's own parent links), `author_ref`, `narrative`, `update_timestamp`.
8. Any user with permission on the Incident can add an Incident Update at any time during its open/in_progress life — the update timeline is the incident's real audit trail of who reported what and when, never a single overwritten field.
9. Incident Updates are immutable once created (consistent with an event/log-entry record) — a correction is a new Incident Update, not an edit to a prior one.

### Incident Report (Document snapshot, reusing DAR's report architecture)
10. **Generate Incident Report** snapshots a filtered set of an Incident's Updates (plus the Incident's own core fields) into an immutable Document, following exactly the pattern DAR's report generation already established (Document Registry's hash/version model, point-in-time, never retroactively updated).
11. Report **scope mode** is a tenant-configurable Settings & Preferences setting: **master** (one Document per Incident, including every author's updates) or **per-responder** (one Document per authoring responder, each containing only that author's own updates) — a tenant may need either depending on jurisdictional/organizational documentation requirements.
12. Report-generation **mode** (auto-on-sign-off, ad hoc-only, or both) is tenant-configurable, reusing DAR's exact three-way toggle pattern, registered separately under Incident's own Settings & Preferences key.

### Supervisor Review (governance record, doesn't mutate the Incident)
13. A **Supervisor Review** gates an Incident's move to `closed` — reviewer, status, comments — following exactly the same "governance record references but never mutates what it judges" discipline DAR's Shift Review established, appropriate given an incident's liability/investigation significance.
14. One Supervisor Review governs the whole Incident regardless of report scope mode — reviewing the full update timeline once, not once per responder, keeping the review gate simple even when reporting output is split per-responder.
15. Sign-off is only reachable once the Supervisor has reviewed the current update timeline; the Incident can still receive new Incident Updates after sign-off (e.g., new information surfaces), which reopens the review (mirrors DAR's kickback-reopens-editing posture, applied here as new-information-reopens-review). ICS Role Mapping & Visual Org Chart's mandatory-adoption posture does not add a sign-off precondition here — see the retrofit at FR #4: compliance is guaranteed structurally at the point an officer is associated with the Incident, not checked at closure.

### Severity-driven escalation
16. Reaching a configured severity level (e.g., an Incident created or updated to `severity = Critical`) publishes an automation-eligible domain event, letting a Tenant Admin configure the actual notification/escalation behavior via Domain Events — this doc never hardcodes a single alert path.

## Data Model / Fields

**Incident** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- category (ref → Category Definition), severity (ref → Severity Definition)
- escalated_from_ref (nullable, direct field, fixed at creation)
- *(participants, attachments, location are rows, not fields — standard Activity Registry associations)*

**Incident Category Definition** / **Incident Severity Definition** (Settings & Preferences registrations)
- category_id / severity_id, tenant_id, name, enabled, sort_order (severity only, for escalation-threshold comparison)

**Incident Update** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- origin_incident_ref (direct field, fixed at creation)
- author_ref, narrative, update_timestamp
- source (typed, voice, canned_phrase, ai_generated — retrofit, see [ai-assisted-incident-report-writing.md](ai-assisted-incident-report-writing.md)), source_draft_ref (nullable, set only when source = ai_generated)

**Incident Report** (Document extension — Document Registry's base fields apply)
- entity_id (PK, FK → Document)
- source_incident_ref, scope_mode (master, per_responder), responder_ref (nullable, set only in per_responder mode)
- included_update_refs[] (snapshot, as of generation)
- is_official, source_review_ref (nullable, set only when auto-generated from sign-off)
- generated_by, generated_at

**Supervisor Review**
- review_id, tenant_id, incident_ref
- reviewer_ref, status (pending, in_review, signed_off, reopened)
- comments
- opened_at, signed_off_at (nullable), reopened_at (nullable)

## States & Transitions

**Incident:** follows base Activity lifecycle — `open` → `in_progress` → `concluded` (Supervisor Review signed off) → `closed` | `cancelled`. A new Incident Update after `concluded` reopens to `in_progress` and reopens the Supervisor Review.

**Incident Update:** created once, immutable.

**Incident Report:** created once, immutable, same as DAR Report.

**Supervisor Review:** `pending` → `in_review` → `signed_off` → (new Incident Update arrives) → `reopened` → `in_review` (cycle repeats as needed).

## Integrations

- **Activity Registry**: Incident and Incident Update both register as Activity extensions; Incident fulfills the `incident` activity_type already anticipated there. Standard dedup/merge directly powers Dispatch/CAD's future Incident Merging feature.
- **Entity Registry Core**: identity, dedup/merge, display-label, and BOLO Flag eligibility (a tagged suspect Person can be BOLO-flagged via the existing generic mechanism, no new integration needed) for Incident and Incident Update.
- **Document Registry**: Incident Report is a Document extension, inheriting hash/integrity/versioning, same pattern as DAR Report.
- **Daily Activity Reports (DAR)**: Incidents and Incident Updates are ordinary Activities, automatically picked up by any DAR filter matching guard/site/time window — the very integration DAR's own doc already anticipated.
- **Shift Passdowns & Handover Notes**: an open/unresolved Incident is a natural default Pass-On Rule candidate, same mechanism as elsewhere.
- **Guard Tour & Checkpoint Verification, Courtesy Patrol, Tickets/Citations**: all already-established launch points for creating an Incident with `escalated_from_ref` set and location pre-filled via Command/Action Bus context-seeding — no changes needed on those docs' side.
- **Settings & Preferences**: owns Category/Severity Definitions, report scope mode, and report-generation mode.
- **Domain Events / Notifications Engine**: severity-threshold escalation publishes an automation-eligible event; a Tenant Admin-configured rule decides actual notification/escalation behavior.
- **Structured Logging & Audit Trails**: Incident creation, every Incident Update, Supervisor Review decisions/sign-off/reopen, and report generation are all audit-tier events.
- **AI-Assisted Incident Report Writing (next in queue)**: will enhance how an Incident Update's `narrative` gets drafted (voice/AI-assisted composition) — not built here, this doc only owns the record/workflow the AI-assist feature writes into.
- **Investigation Management (future — Case Files, Digital/Physical Evidence Tracking)**: an Incident is the natural seed record for a future Case File; full evidentiary chain-of-custody is deferred to that module.
- **Dispatch/CAD (future)**: consumes Incident's merge mechanism directly for its own Incident Merging feature, per Activity Registry's existing forward reference.
- **Command Center — ICS Role Mapping & Visual Org Chart** *(retrofit)*: at a `mandatory_limited` tenant, officer/unit participant tagging (FR #4) resolves to that doc's ICS Role Assignment instead of the plain `reporting_officer`/`responding_unit` roles — Supervisor Review itself is untouched.

## Permissions

| Action | Guard/Responder | Investigator | Supervisor | Tenant Admin |
|---|---|---|---|---|
| Create an Incident (self-initiated or escalated) | ✅ | ✅ | ✅ | ❌ |
| Add an Incident Update | ✅ (own/assigned incidents) | ✅ | ✅ | ❌ |
| Tag/edit participants | ✅ (own/assigned incidents) | ✅ | ✅ | ❌ |
| Perform Supervisor Review / sign off | ❌ | ❌ | ✅ | ✅ |
| Generate an ad hoc Incident Report | ❌ | ✅ | ✅ | ✅ |
| Configure Category/Severity Definitions, scope mode, report mode | ❌ | ❌ | ❌ | ✅ |
| Resolve Incident dedup flags / confirm-reject merges | ❌ | ❌ (unless granted) | ❌ (unless granted) | ✅ |

## Non-Functional / Constraints

- Incident and Incident Update creation must work fully offline, per the platform's established offline model — a multi-responder incident with several guards each adding updates from different dead zones must sync and reconcile correctly.
- Incident Report generation (both scope modes) must be efficient enough for a busy multi-update incident without unreasonable latency, likely served by the same CQRS read-model projection approach used elsewhere.
- A generated Incident Report's immutability must be enforced at the data layer — no code path may mutate `included_update_refs[]` after generation, identical requirement to DAR Report.
- Severity-threshold domain event evaluation must fire promptly (near-real-time) given its role in life-safety-relevant escalation — a technical-spec-level latency target.
- WCAG 2.1 / Section 508 accessible incident creation, update timeline, review, and report-generation flows, day one.

## Acceptance Criteria

- [ ] Escalating a Patrol Finding into an Incident correctly sets `escalated_from_ref` and pre-fills location, without requiring re-entry.
- [ ] Multiple responders can each add Incident Updates to the same Incident; each update is immutably attributed and timestamped.
- [ ] A suspect Person tagged on an Incident can be BOLO-flagged using the existing generic Entity Registry Core mechanism, no Incident-side special-casing.
- [ ] With report scope mode set to `master`, generating a report produces one Document including every responder's updates.
- [ ] With report scope mode set to `per_responder`, generating reports produces one Document per authoring responder, each containing only their own updates.
- [ ] An Incident cannot reach `closed` without a signed-off Supervisor Review.
- [ ] Adding a new Incident Update to a `concluded` Incident with a signed-off review correctly reopens both the Incident (`in_progress`) and the Supervisor Review (`reopened`).
- [ ] Creating/updating an Incident to a configured escalation severity publishes a domain event a configured Domain Events rule can act on.
- [ ] Two Incidents with overlapping time window, location, and shared participant are flagged as a potential duplicate and never auto-merge, using the standard Activity Registry mechanism.
- [ ] Incident and Incident Update records for a given guard/night appear correctly in that guard's DAR filter view with no DAR-side special-casing.

## Open Questions

- Exact default Incident Category and Severity taxonomies shipped out of the box — pending UX/content design.
- Exact escalation domain-event payload/threshold-comparison logic (e.g., does severity need a numeric `sort_order` for ">= High" style rule conditions) — a technical-spec-level decision; `sort_order` is sketched in the data model but not fully specified.
- Whether per-responder report mode needs its own, separate per-responder sign-off (rather than one Supervisor Review governing the whole incident) for jurisdictions requiring each officer's report to be individually attested — current default is one shared review; flagged as a possible gap.
- Whether an Incident's participant role vocabulary needs to be tenant-configurable (like category) or stays a fixed platform list — current default is fixed, mirroring Activity Registry's own "role vocabulary may be a controlled list" allowance without committing to tenant-editability.
- Full Case File hand-off mechanics once Investigation Management is specified — deferred entirely, not solved here.
