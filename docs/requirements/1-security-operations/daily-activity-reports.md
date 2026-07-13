# Daily Activity Reports (DAR)

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

A Daily Activity Report is **not a single stored record** — it's a reporting workflow built almost entirely on Master Records infrastructure already specified in Module 0.5. Three things make it up:

1. **DAR Entry** — a new, thin **Activity extension type** (per Activity Registry) for the generic narrative log note a Guard writes during a shift ("checked loading dock, no issues," "escorted vendor to Suite 200"). Tenant-configurable category + free-text narrative, optional photo attachment (`ActivityAttachmentAssociation`), optional location (`ActivityLocationAssociation`, tenant-configurable mandatory/optional). Created continuously through the shift, offline-capable, exactly like any other Activity.
2. **The report itself is a filtered timeline, not a container.** "A guard's DAR" or "a site's DAR" is a query — filtered by person and/or site and a time window — over **every** Activity in Activity Registry that falls in scope, not just DAR Entries: a shift's Incidents, Citations, Dispatches, or any other Activity extension the guard was tagged on or that occurred at their site during the window all get pulled in automatically. This is the same read-model pattern Entity Relationships & History already established for its Interaction Timeline, applied here with a shift-shaped filter.
3. **Generating a DAR snapshots that filtered view into a Document** at a specific point in time — an immutable, hash-verified artifact (per Document Registry) capturing exactly what was in scope when it was generated. Because Activities keep living their own lives after a report is generated (an Incident can still be updated by Incident Reporting long after today's DAR shipped), a saved report is explicitly a point-in-time snapshot, not a live view, and is **not guaranteed to reflect an Activity's later disposition.**

DAR entries and reports both come in two scopes, sharing the same mechanism, differing only in filter shape: a **Personal DAR** (one Guard, their own clock-in/out window) and a **Team/Shift DAR** (a Supervisor-defined time block covering every Guard rostered at a site during that window — "aggregate of all members of their shift"). Neither scope introduces a second architecture.

Since Post Schedule Builder (Module 8) doesn't exist yet, DAR owns a lightweight **Shift Window** record of its own (clock-in/out for a Guard, or a defined block for a Supervisor's team) purely to scope filtering and review — a deferred-integration point, same pattern used elsewhere (e.g., Entity Relationships & History's scheduled-shifts note), to be reconciled with real scheduling data once Module 8 is specified.

Supervisor review/sign-off is itself its own governance record (**Shift Review**), never a status field written onto the underlying Activities — an Incident pulled into today's DAR keeps its own independent lifecycle regardless of whether this shift's DAR review approves, kicks back, or excludes it.

## Actors & Roles

- **Guard** — clocks in/out (opens/closes a Personal Shift Window), creates DAR Entries throughout the shift, edits their own entries when a Supervisor kicks them back.
- **Supervisor** — opens/closes a Team/Shift Window covering their roster, reviews a Shift Review's scoped activity list entry-by-entry (approve / kick back / exclude), signs off, may generate ad hoc report snapshots.
- **Tenant Admin** — configures DAR's Settings & Preferences-registered options (entry category taxonomy, location-required toggle, report-generation mode).
- **Records Admin** — resolves any Entity Registry Core duplicate-flagging on DAR Entry Activities, same as any other Activity type.
- **Any user with report-view permission** (e.g., a Client Portal viewer, once Module 15 exists) — reads generated/signed DAR Documents. Deferred integration; not built now.

## User Stories

- As a **Guard**, I want to log notes throughout my shift, offline if needed, so I don't have to reconstruct my whole shift from memory at the end.
- As a **Guard**, I want a final review pass before I submit my shift, so I can clean up or add anything I missed before it goes to my Supervisor.
- As a **Supervisor**, I want to review my team's shift as one filtered list pulling in every logged note, incident, and dispatch — not a patchwork of separate screens — so I can sign off efficiently.
- As a **Supervisor**, I want to approve most of a Guard's entries while kicking back just the one that's unclear, without rejecting the whole shift.
- As a **Supervisor**, I want a signed-off shift to automatically produce the official report document, without a separate manual export step, if my tenant is configured that way.
- As a **Site Manager**, I want to pull an ad hoc report for a specific date range and site for a client meeting, independent of whether every shift in that range has been formally signed off yet.
- As a **Tenant Admin**, I want to decide whether my organization requires strict entry-locked sign-off with auto-generated reports, or a lighter ad hoc-only reporting mode, since different clients we serve have different oversight requirements.
- As an **Investigator**, I want an Incident that happened during a shift to show up in that shift's DAR automatically, because it's the same Activity Registry record I'm already working, not a duplicate copy.

## Functional Requirements

### DAR Entry (Activity extension)
1. **DAR Entry** registers as an Activity extension type per Activity Registry, inheriting base identity, offline-safe numbering, participant/attachment/location associations, and dedup/merge for free.
2. Fields: `category` (drawn from a tenant-configurable list — Settings & Preferences-registered, e.g., Patrol Note, Observation, Visitor Interaction, Maintenance Issue, Suspicious Activity, Equipment Check), free-text `narrative`, `entry_timestamp`.
3. Location association (`ActivityLocationAssociation`) is optional or mandatory per a tenant-configurable Settings & Preferences toggle.
4. Photo/file attachments use `ActivityAttachmentAssociation` unmodified — no bespoke attachment mechanism.
5. A Guard creates DAR Entries continuously through their shift; offline creation behaves identically to any other Activity (client UUID immediately, server display number on sync).
6. At end of shift, the Guard gets a review/edit pass over their own not-yet-submitted entries before submitting the shift for supervisor review.

### Shift Window (DAR-owned, pending Module 8 integration)
7. A **Shift Window** scopes DAR filtering and review. Two kinds: **Personal** (one Guard, `clock_in`/`clock_out`) and **Team** (a Supervisor, a site/location, a defined `started_at`/`ended_at` block, and the roster of Guards whose Personal Shift Windows fall inside it).
8. Opening a Personal Shift Window is a Guard clocking in; closing it (clocking out) is what triggers "submit for review" — the entries in scope lock (see #12) and a Shift Review record is created/opened against it.
9. A Team Shift Window is opened/closed by a Supervisor; closing it opens a Shift Review scoped to every Activity across every Personal Shift Window (and any site-scoped Activity with no personal owner, e.g., an alarm at that site) within the team block.
10. This is an explicitly interim mechanism. Once Post Schedule Builder (Module 8) exists, Shift Window should reconcile with real scheduled-shift data rather than duplicating clock logic indefinitely — flagged as a future integration, not solved here.

### Shift Review (governance record, doesn't mutate Activities)
11. Closing a Shift Window creates a **Shift Review**: a record referencing the Shift Window, its computed scope (every Activity matching the filter at close time), a status, and a reviewer.
12. On creation, every Activity in scope is snapshotted into the Shift Review's scoped list and the underlying DAR Entries become locked (read-only) for their creating Guard — corrections from this point happen through the review's kickback mechanism, not direct edits, preserving a clean audit trail.
13. A Supervisor works the scoped list **entry-by-entry**: each item gets `approved`, `kicked_back` (with a required comment), or `excluded` (removed from this review's scope without judging its content — e.g., a duplicate or out-of-scope item). Non-DAR-Entry Activities (an Incident, a Dispatch) pulled into scope can be `excluded`/`approved`/`kicked_back` for review purposes only — their own status/lifecycle is untouched, per #16.
14. `kicked_back` on a DAR Entry reopens **that specific entry** (not the whole shift) for the originating Guard to edit; once resubmitted it returns to pending review within the same Shift Review.
15. The Shift Review reaches `signed_off` once every item in scope is `approved` or `excluded` and the Supervisor executes sign-off (step-up not required — this is a routine supervisory action, not a governed high-liability one like BOLO Flag).
16. Shift Review decisions (`approved`/`kicked_back`/`excluded`) are scoped to the review itself and never write back to the underlying Activity record — an Incident's own status/lifecycle is owned entirely by its owning module (future Incident Reporting), regardless of how this shift's review judged it.

### Report generation (Document snapshot)
17. **Generate Report** produces an immutable Document: a point-in-time snapshot of a filtered Activity set (person and/or site + time window) at generation, following Document Registry's hash/integrity/versioning model.
18. Report-generation mode is a **tenant-configurable Settings & Preferences** setting, one of: (a) **auto-on-sign-off** — Shift Review sign-off automatically generates the official report, no separate action; (b) **ad hoc-only** — no automatic report; any user with permission generates a snapshot manually, any time, sign-off status notwithstanding; (c) **both** — sign-off always auto-generates the canonical official report, and users with permission may additionally pull unofficial ad hoc snapshots at any other time (e.g., a mid-shift pull for a client request), clearly marked as unofficial and distinct from the signed version.
19. A generated report is immutable once saved. It reflects the state of every included Activity **at generation time only** — it is never retroactively updated if a pulled-in Activity (e.g., an Incident) later changes, and does not need to end up 1:1 with that Activity's eventual final disposition.
20. An official (sign-off-triggered) report additionally records a pointer to the Shift Review that produced it and its final approved/excluded breakdown; an ad hoc report has no such pointer and is not implied to represent a reviewed/approved shift.

## Data Model / Fields

**DAR Entry** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- category (tenant-configurable list ref)
- narrative
- entry_timestamp
- locked (bool — true once its Shift Window closes; false again if kicked back)

**Shift Window** (DAR-owned, interim — not an Entity/Activity, no dedup/merge/participant tracking needed)
- shift_window_id, tenant_id, kind (personal, team)
- person_ref (Party, personal only), supervisor_ref (Party, team only), site_location_ref
- started_at, ended_at (nullable until clocked/closed out)
- status (open, closed)
- parent_team_shift_window_ref (nullable — links a Personal window to its enclosing Team window, if any)

**Shift Review**
- shift_review_id, tenant_id, shift_window_ref
- reviewer_ref (Party)
- status (pending, in_review, signed_off)
- scoped_items[] (activity_ref, decision: pending/approved/kicked_back/excluded, comment, decided_at)
- opened_at, signed_off_at (nullable)

**DAR Report** (Document extension — Document Registry's base fields apply: hash, version, title, etc.)
- entity_id (PK, FK → Document)
- filter_criteria (person_ref and/or site_location_ref, time window)
- included_activity_refs[] (snapshot, as of generation)
- is_official (bool)
- source_shift_review_ref (nullable — set only when auto-generated from sign-off)
- generated_by, generated_at

## States & Transitions

**Shift Window:** `open` (clocked in / team block started) → `closed` (clocked out / team block ended) — triggers Shift Review creation.

**Shift Review:** `pending` → `in_review` (Supervisor begins working the scoped list) → `signed_off` (every item approved/excluded). No reject-the-whole-review state — kickback operates per-item (#14), not on the review as a whole.

**Shift Review scoped item:** `pending` → `approved` | `excluded` | `kicked_back` → (kicked_back only) `pending` once the Guard resubmits the corrected entry.

**DAR Entry `locked`:** `false` (during open shift) → `true` (Shift Window closes) → `false` (that specific entry kicked back) → `true` (resubmitted).

**DAR Report:** created once, immutable — no further states.

## Integrations

- **Activity Registry**: DAR Entry registers as an Activity extension; report generation is a filtered read over Activity Registry's records, including extensions this doc doesn't own (Incidents, Citations, Dispatches, Alarms, etc., once specified).
- **Entity Registry Core**: DAR Entry inherits identity, dedup/merge, and display-label requirements like any Activity extension.
- **Document Registry**: DAR Report is a Document extension, inheriting hash/integrity/versioning.
- **Offline Data Sync**: DAR Entry creation during a shift follows the established offline append-only outbox pattern unmodified (Class 1 create — client UUID, locally editable until first sync).
- **Settings & Preferences**: owns the tenant-configurable entry category taxonomy, location-required toggle, and report-generation mode — DAR registers against the existing engine rather than building its own override logic.
- **Structured Logging & Audit Trails**: DAR Entry creation/edit, Shift Window open/close, every Shift Review decision, sign-off, and report generation are audit-tier events.
- **Party Registry (Person)**: Guards and Supervisors referenced via `person_ref`/`reviewer_ref`/`supervisor_ref`.
- **Location Registry**: `site_location_ref` and any DAR Entry's own `ActivityLocationAssociation`.
- **Notifications Engine**: notifies a Supervisor when a Shift Review is ready (`pending`), and a Guard when an entry is kicked back.
- **Command/Action Bus**: "Clock in/out," "Submit shift," "Approve entry," "Kick back entry," "Sign off review," and "Generate report" register as invokable actions — reachable from buttons, Command Palette, and CLI-Style Input uniformly, not bespoke per-screen buttons.
- **Post Schedule Builder (Module 8, future)**: intended eventual replacement/reconciliation point for Shift Window's interim clock-in/out and team-block logic — deferred, not built now.
- **Incident Reporting & Management (Module 1, next in queue)**: will register Incident as an Activity extension; DAR pulls those records into scope automatically the moment that module exists, with no DAR-side change required.
- **Client Portal Dashboard (Module 15, future)**: likely eventual consumer of signed DAR Report Documents — deferred, not built now.

## Permissions

| Action | Guard | Supervisor | Tenant Admin | Records Admin |
|---|---|---|---|---|
| Create/edit own DAR Entry (unlocked) | ✅ | ✅ (own entries) | ❌ | ❌ |
| Clock in/out (open/close own Personal Shift Window) | ✅ | ✅ | ❌ | ❌ |
| Open/close a Team Shift Window | ❌ | ✅ (own roster) | ✅ | ❌ |
| Review scoped items (approve/kick back/exclude) | ❌ | ✅ (own roster's review) | ✅ | ❌ |
| Sign off a Shift Review | ❌ | ✅ (own roster's review) | ✅ | ❌ |
| Generate an ad hoc report snapshot | ❌ (own shift only, if tenant allows) | ✅ | ✅ | ❌ |
| Configure DAR settings (category list, location toggle, report mode) | ❌ | ❌ | ✅ | ❌ |
| Resolve DAR Entry duplicate flags | ❌ | ❌ | ✅ | ✅ |

## Non-Functional / Constraints

- DAR Entry creation (including local editing of a not-yet-synced entry) must work fully offline, syncing per the established Offline Data Sync append-only contract, with no degraded behavior relative to any other Activity type.
- Locking a Guard's entries on Shift Window close must be atomic against any in-flight offline sync — an entry created offline just before clock-out must not slip past the lock silently.
- Report generation (snapshot creation and hashing) must be efficient enough for a full-site, multi-day ad hoc pull without unreasonable latency — a technical-spec-level concern, likely served by the same CQRS read-model projection approach used for Entity Relationships & History's timeline.
- A generated report's immutability must be enforced at the data layer, not just the UI — no code path may mutate `included_activity_refs[]` or the report's hash after generation.
- Shift Review per-item decisions must be individually audit-logged (not just the final sign-off), since "who approved which specific entry" is a real accountability question for a security operations record.
- WCAG 2.1 / Section 508 accessible entry creation, review, and sign-off flows, day one.

## Acceptance Criteria

- [ ] A Guard can clock in, add several DAR Entries (including at least one offline), and clock out, producing a closed Shift Window and a `pending` Shift Review.
- [ ] A Shift Review's scoped list correctly includes both DAR Entries and an unrelated Activity extension (e.g., a stubbed Incident) that shares the same person/site/time window.
- [ ] A Supervisor can approve some scoped items and kick back others within the same review; only the kicked-back DAR Entry reopens for edit, the rest remain locked.
- [ ] Resubmitting a kicked-back entry returns it to `pending` within the same Shift Review, not a new one.
- [ ] Sign-off is only reachable once every scoped item is `approved` or `excluded`.
- [ ] With report-generation mode set to auto-on-sign-off, signing off a Shift Review produces an immutable, official DAR Report Document referencing that review.
- [ ] With report-generation mode set to ad hoc-only, a permitted user can generate a report for an arbitrary date range/site regardless of any Shift Review's status, and it is marked unofficial.
- [ ] A DAR Report's `included_activity_refs[]` does not change even after one of those Activities (e.g., an Incident) is later updated by its owning module.
- [ ] Excluding a scoped item removes it from that Shift Review's judgment without altering the underlying Activity's own status.
- [ ] Attempting to edit a locked DAR Entry directly (outside the kickback flow) is rejected.
- [ ] DAR's tenant-configurable settings (category list, location-required toggle, report-generation mode) are read from and stored against Settings & Preferences, not a bespoke DAR settings table.

## Open Questions

- Whether/how a Team Shift Window's roster is determined (manual Supervisor selection each time vs. eventually sourced from Post Schedule Builder) — deferred until Module 8 exists.
- Exact default DAR Entry category taxonomy shipped out of the box, pending UX/content design.
- Whether an excluded scoped item should be re-includable in a later ad hoc report pull, or permanently excluded from that specific Shift Review only — leaning toward the latter (scoped to that review only) but not confirmed.
- Whether ad hoc report generation needs its own rate-limiting/permission-scoping beyond the base permission table, given it can be run repeatedly against the same data — a technical-spec-level concern if abuse patterns emerge.
- Report export formats (PDF, etc.) and delivery (download vs. emailed per Module 12's future Automated Emailed Reports) — deferred to technical spec / that future module.
- Whether DAR Entry needs its own dedup match-signal tuning beyond Activity Registry's generic same-type/overlapping-window/shared-participant defaults — likely not, given entries are narrative and low-collision, but not confirmed.
