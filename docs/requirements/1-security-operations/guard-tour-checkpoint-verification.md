# Guard Tour & Checkpoint Verification

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec (see Open Questions — several defaults below are best-guess and flagged for confirmation)

## Overview

Two parallel hierarchies, one-to-one, confirmed with the user after a big-picture review of the first draft caught several real gaps (see the corrections called out below):

| Plan (definition, versioned, not an Activity) | Execution (Activity extension, one per real occurrence) |
|---|---|
| **Route** — the overall plan/build | **Route Assignment** — one instance of that Route being assigned to a Guard and carried through to completion |
| **Tour Definition** — one place + required count + spacing within a Route | **Patrol** — one actual run against a Tour Definition |
| **Checkpoint** — one scannable stop, reusable across Tour Definitions | **Checkpoint Scan** — one actual scan of a Checkpoint |

**Checkpoint** — a scannable tour stop. Structurally a **triad**: it IS a place (extends Location Registry), it has a **physical tag** that triggers it (an Item Registry entity, associated via a single-current-value EntityAssociation so a broken/replaced tag doesn't lose the checkpoint's own identity or scan history), and it is additionally **its own thing**. Critically, **a Checkpoint doesn't belong to any one Tour Definition** — it can be referenced by several (even across different Routes), so anything that's a property of the physical checkpoint itself (required verification method, inspection instructions) lives on the Checkpoint, not on whichever tour happens to be visiting it.

**Route** is an assignment plan, not a flat checkpoint list — composed of one or more **Tour Definitions**, each with its own target place, checkpoint set, required occurrence count, and minimum spacing (e.g., 2 tours of one building at least 4 hours apart, alongside single tours of three other buildings). Both **which Tour Definitions within a Route must happen in order** and **which Checkpoints within a Tour Definition must be scanned in order** are independently configurable, defaulting to **unordered** — a Route might care about tour sequence but not checkpoint sequence within each tour, or neither, or both.

**Starting a Patrol is an explicit, trackable moment, not an inference.** The system needs to know when "Unit 1 starts a patrol of Building 37" — either **explicitly** (the guard starts it themselves, or a Supervisor/Dispatcher starts it on the guard's behalf after a radio call — reusing the same manual/radio-driven update posture already established for Mobile Patrol Unit location in Patrol Management) or **implicitly** (the first scan of a Checkpoint that belongs to exactly one currently-due, not-yet-started Tour Definition auto-starts a Patrol for it). Because a Checkpoint can belong to more than one Tour Definition, an implicit start is only safe when it's unambiguous — if a scanned Checkpoint could belong to more than one currently-eligible Tour Definition, the system prompts the guard to choose rather than guessing. Once a Patrol is active, each subsequent scan against that Tour Definition's checkpoint set "crosses it off" a live checklist.

**Missed points are missed points.** If an expected Tour Definition occurrence's window closes with no Patrol ever started, the system creates that occurrence's Patrol directly in a `missed` status — a real, queryable row, not a silent gap — and a Guard or Supervisor can attach a note explaining why, but the note provides context only; it never changes `missed` back to something else.

**Verification is advisory, never a hard gate.** A Checkpoint declares its own required/preferred verification method (tag, GPS, both, or none), but a guard can always mark a checkpoint complete via manual override — recorded as unverified — if the tag is missing, damaged, or otherwise unreadable. The tour must be completable even when the hardware isn't cooperating.

**Route and Tour Definition are version-controlled.** An edit creates a new version rather than mutating the live plan in place, mirroring the versioning discipline already established for Settings & Preferences (prior value, changed by, changed at). A Route Assignment and every Patrol underneath it pin the specific plan version in effect when they started, so a Supervisor changing a Route's requirements never retroactively alters what's expected of work already in progress or completed.

**Checkpoint Scan generally doesn't need Entity Registry Core's full dedup/merge machinery.** A scan event has no "duplicate identity" concept the way a Person or Incident does — the only real duplicate-detection need is a narrow, write-time debounce catching an accidental back-to-back double-read of the same checkpoint (a bad NFC read, a double-tap), not a human-reviewed potential-duplicate workflow.

A scan is also a **launch point** for follow-on records with location pre-seeded from the checkpoint: a lightweight built-in **Patrol Finding** for minor observations (an unlocked door, a leak), or any richer Activity type (e.g., a future Incident) via ordinary Command/Action Bus context-seeding.

**Naming disambiguation:** this doc's **Patrol** (the Activity representing one tour run) is a different concept from Patrol Management's **Mobile Patrol Unit** or **Patrol Request** — same root word, different layer: Patrol Management's Post is *who's assigned and where*; this doc's Patrol is *one completed run of a tour*.

**Scope boundary:** this doc covers fixed Route/Checkpoint/Patrol/Scan mechanics only. Broader roving or dispatch-triggered patrol assignment without fixed checkpoints is Patrol Management's territory — not solved here.

## Actors & Roles

- **Guard** — executes an assigned Route, starts/completes Patrols, performs Checkpoint Scans, adds resolution notes to their own missed Patrols, views their own assignment and completion status.
- **Supervisor** — authors Routes and Tour Definitions (creating new versions on edit), assigns Routes to Guards, starts a Patrol on a guard's behalf (e.g., after a radio call), reviews Patrol compliance, adds resolution notes to missed Patrols, configures Checkpoints (including tag replacement and required verification method).
- **Tenant Admin** — sets tenant-default sequence-enforcement and scan-verification-method settings, configures baseline missed-tour alerting rules (via Domain Events).
- **Records Admin** — resolves Entity Registry Core dedup flags on Checkpoint (as a Location extension) and Route Assignment/Patrol (as Activity extensions) — Checkpoint Scan is explicitly excluded from standard dedup (see Overview).

## User Stories

- As a **Supervisor**, I want to build a Route that requires two tours of the warehouse spaced at least 4 hours apart alongside single tours of three other buildings, all under one assignment, so I don't have to manage four separate schedules for one guard.
- As a **Dispatcher/Supervisor**, I want to start a Patrol on behalf of a unit who radioed in that they're beginning their building sweep, so the system tracks it even when the guard can't operate their device hands-free.
- As a **Guard**, I want a checkpoint with a broken NFC tag to never block me from completing my tour — I should be able to mark it done manually.
- As a **Supervisor**, I want to see exactly which required tours were missed, with my or the guard's note explaining why, without the missed record disappearing or being softened into something else.
- As a **Route author**, I want to change a Route's requirements going forward without silently altering a guard's already-in-progress assignment.
- As a **Facilities Coordinator**, I want to replace a damaged checkpoint's NFC tag with a new one without losing that checkpoint's scan history or identity.
- As an **Investigator**, I want to see every checkpoint scan a specific guard performed on a specific night as part of one uniform Activity timeline, the same mechanism I'd use to review any other Activity.

## Functional Requirements

### Checkpoint (Location extension + associated tag Item)
1. **Checkpoint** registers as a Location extension (per Location Registry), inheriting base identity, geometry, GIS placement, and dedup/merge.
2. A Checkpoint's physical scan tag is a separate Item Registry entity, linked via a single-current-value EntityAssociation (`CheckpointTagAssociation`) — replacing a damaged/lost tag means removing the old active association row and adding a new one, leaving the prior tag as history, exactly like Vehicle's `ConveyanceOwnerAssociation` pattern.
3. A Checkpoint declares its own `required_verification_method` (tag, gps, both, or none — defaulting to a tenant-wide Settings & Preferences value), since a Checkpoint can be reused across multiple Tour Definitions and even multiple Routes — verification requirements are a property of the physical checkpoint's own setup, never of whichever tour happens to be visiting it.
4. Verification is **never a hard gate**: if a Checkpoint's tag is missing, damaged, or otherwise unreadable, the officer can still mark that checkpoint complete via manual override — recorded as `verified = false`, `scan_method = manual_override` — rather than being blocked from completing their tour.

### Route & Tour Definition (the plan — versioned, not an Activity)
5. A **Route** is a named plan owned by a Supervisor, composed of one or more **Tour Definitions**.
6. A **Tour Definition** declares: a target place (`location_ref`), the set of Checkpoints in scope (`checkpoint_refs[]` — may overlap with other Tour Definitions' sets), `required_count` (occurrences required over the assignment period), and an optional `min_interval` (minimum spacing between consecutive occurrences).
7. A Checkpoint has no owning tour — the same Checkpoint can appear in more than one Tour Definition's `checkpoint_refs[]`, and a Tour Definition's Checkpoints can be shared across Routes.
8. Two independently configurable ordering settings, both defaulting to unordered: `tour_sequence_enforced` (on Route — whether its Tour Definitions must be started in `order_index` sequence) and `sequence_enforced` (on Tour Definition, unchanged from the original draft — whether its Checkpoints must be scanned in `checkpoint_refs[]` order).
9. Route and Tour Definition are **version-controlled** — an edit creates a new version (prior value, changed by, changed at), mirroring the versioning discipline already established for Settings & Preferences and CLI aliases, rather than mutating the live plan in place. A Route Assignment (#11) and each Patrol (#14) pin the specific version in effect when they started; later plan edits never retroactively change what's expected of work already in progress or completed.

### Route Assignment (Activity extension)
10. **Route Assignment** registers as an Activity extension: one instance of a Route being assigned to a Guard and carried through to completion, inheriting base Activity identity, offline-safe numbering, and dedup/merge.
11. A Route Assignment covers an assignment period (a shift, optionally referencing DAR's Shift Window, or an explicit date/time range) and pins `route_version_ref` at creation (#9); it rolls up completion across every Patrol underneath it.
12. A Route Assignment reaches `concluded` once every Tour Definition under its pinned Route version has met its `required_count` for the assignment period (or the period ends — see Open Questions), locking its rolled-up completeness.

### Patrol (Activity extension) — starting, crossing off, and missing
13. **Patrol** registers as an Activity extension: one guard's actual execution instance against a specific Tour Definition (and its pinned `tour_definition_version_ref`, #9), inheriting base Activity identity, offline-safe numbering, and dedup/merge.
14. A Patrol is started one of two ways: **explicitly** (`start_method = explicit_self` or `explicit_dispatched`) — the guard, or a Supervisor/Dispatcher starting it on the guard's behalf (e.g., after a radio call), selects which Tour Definition is beginning; or **implicitly** (`start_method = implicit_first_scan`) — the first scan of a Checkpoint belonging to exactly one currently-due, not-yet-started Tour Definition under the guard's active Route Assignment. If a scanned Checkpoint could belong to more than one currently-eligible Tour Definition with no active Patrol, the system prompts the guard to choose rather than guessing.
15. Once a Patrol is active, each subsequent Checkpoint Scan against its Tour Definition's checkpoint set marks that checkpoint complete on a live checklist, computed against: percentage of in-scope Checkpoints scanned, order compliance (if enforced), and — for a repeat occurrence — spacing compliance against `min_interval` relative to the immediately preceding Patrol for that same Tour Definition.
16. A Patrol reaching `concluded` is when its final completeness/compliance is locked; a still-`in_progress` Patrol's compliance is provisional/live and rolls up into its parent Route Assignment's own completeness (#11).
17. If an expected Tour Definition occurrence's window closes with no Patrol ever started, the system creates that occurrence's Patrol directly in a `missed` status — a real, queryable row, giving Tour Completeness Logs and Officer Performance Logs one row per expected occurrence regardless of outcome, not a silent gap.
18. A `missed` Patrol accepts a `resolution_note` from the Guard or a Supervisor — free-text context on why it was missed. The note never changes the `missed` status itself; a miss stays a miss.
18a. **Dispatch interruption (retrofit, per Unit Dispatch & Proximity Routing's stacking/preemption model):** when an in-progress Patrol's guard has a Dispatch become active (assignment, or explicit queue advance), the Patrol auto-transitions to an **`interrupted`** status with `interrupted_by_dispatch_ref` set — a real, queryable row explaining any resulting completeness gap, never a silent one. An `interrupted` Patrol resumes to `in_progress` when the guard scans its next in-scope Checkpoint or explicitly resumes it (self or on-behalf-of); if its Tour Definition occurrence window closes while still `interrupted`, the Patrol concludes with its partial completeness as-is — missed points are missed points, but Tour Completeness Logs and Officer Performance Logs can attribute the gap to the referenced Dispatch rather than to the guard. Repeated interrupt/resume cycles within one Patrol append to `interruption_history[]` rather than overwriting the reference.

### Checkpoint Scan (Activity extension)
19. **Checkpoint Scan** registers as a thin Activity extension: `patrol_ref` (direct field, fixed at creation), `checkpoint_ref` (direct field), `scan_method` (tag, gps, manual_override), `verified` (bool), `captured_gps` (optional corroboration), `scanned_by`, `scan_timestamp`.
20. A second scan of the **same Checkpoint within the same Patrol**, within a short debounce window of the immediately preceding scan of that Checkpoint, is treated as a duplicate device read and collapsed rather than creating a second Checkpoint Scan row. This is a narrow, write-time debounce — **not** Entity Registry Core's full dedup/merge/human-review workflow, since a routine scan has no "duplicate identity" concept the way a Person or Incident does; Checkpoint Scan does not participate in standard Activity dedup matching.
21. Checkpoint Scan creation works fully offline — a scan performed in a dead zone is recorded immediately with a client UUID and synced when connectivity returns.

### Missed/late notification
22. In addition to the `missed` Patrol record itself (#17), a Tour Definition occurrence not completed within its expected window publishes an automation-eligible domain event, letting a Tenant Admin configure a Domain Events rule (e.g., notify the Supervisor, or auto-create a Passdown Pass-On Flag) rather than this doc hardcoding a single fixed alert behavior.

### Inspection instructions & scan-triggered record creation
23. A Checkpoint carries `inspection_instructions` (free text) — displayed automatically the moment a guard scans it.
24. **Patrol Finding** registers as its own thin Activity extension, created directly from a Checkpoint Scan: `origin_scan_ref` (direct field), `category` (tenant-configurable list, e.g. Unlocked Door, Leak, Damage, Suspicious Item, Maintenance Issue — same pattern as DAR Entry), and `narrative`. Its `ActivityLocationAssociation` is auto-seeded from the originating Checkpoint's own Location.
25. A Patrol Finding follows the base Activity lifecycle (`open` → `concluded`, i.e. resolved) independently of its originating Scan or Patrol — resolving a finding never alters the scan or Patrol it came from.
26. A Checkpoint Scan is also a **generic launch point** for creating any other, richer Activity type via Command/Action Bus context-seeding: invoking "create [Activity Type]" from a scan screen passes the checkpoint's Location as explicit context.

## Data Model / Fields

**Checkpoint** (Location extension, TPT level: entity_id shared PK, FK → Location.entity_id)
- entity_id (PK, FK → Location)
- inspection_instructions (free text)
- required_verification_method (tag, gps, both, none — defaults from Settings & Preferences)
- *(tag association is a row, not a field — see CheckpointTagAssociation)*

**CheckpointTagAssociation** (EntityAssociation TPT subtype)
- association_id (PK, FK → EntityAssociation; entity_id_a = Checkpoint, entity_id_b = Item/tag)
- no extra fields beyond base EntityAssociation shape; single-current-value pattern (active = current tag, removed = prior tags)

**Route** (versioned — prior value, changed_by, changed_at retained per edit)
- route_id, tenant_id, name, owning_supervisor_ref
- tour_sequence_enforced (bool, default false)
- version, status (active, archived)

**Tour Definition** (versioned, same discipline as Route)
- tour_definition_id, tenant_id, route_ref
- location_ref (target place), checkpoint_refs[] (in-scope Checkpoints — may overlap with other Tour Definitions)
- order_index (nullable — meaningful only when parent Route's `tour_sequence_enforced = true`)
- sequence_enforced (bool, defaults from Settings & Preferences)
- required_count (per assignment period), min_interval (nullable)
- version

**Route Assignment** (Activity extension, TPT level: entity_id shared PK, FK → Activity.entity_id)
- entity_id (PK, FK → Activity)
- route_ref, route_version_ref (pinned at creation)
- person_ref (Guard)
- assignment_period_start, assignment_period_end (or shift_window_ref, if shift-scoped)
- post_ref (nullable — FK → Patrol Management's Post; retrofit, see [patrol-management.md](patrol-management.md))
- completeness_pct (rollup across every Patrol underneath it)

**Patrol** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- tour_definition_ref, tour_definition_version_ref (pinned at creation)
- route_assignment_ref (direct field, fixed at creation)
- start_method (explicit_self, explicit_dispatched, implicit_first_scan)
- completeness_pct, order_compliant (bool, nullable if unordered), spacing_compliant (bool, nullable if no min_interval or first occurrence)
- resolution_note (nullable — set by Guard or Supervisor when status = missed)
- interrupted_by_dispatch_ref (nullable — the Dispatch that most recently interrupted this Patrol, per #18a)
- interruption_history[] (dispatch_ref, interrupted_at, resumed_at nullable — one entry per interrupt/resume cycle)

**Checkpoint Scan** (Activity extension, TPT level — excluded from standard Activity dedup matching, see #20)
- entity_id (PK, FK → Activity)
- patrol_ref, checkpoint_ref
- scan_method (tag, gps, manual_override), verified (bool)
- captured_gps (nullable), scanned_by, scan_timestamp

**Patrol Finding** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- origin_scan_ref (direct field, fixed at creation)
- category (tenant-configurable list ref), narrative
- *(location is a row, not a field — ActivityLocationAssociation, auto-seeded from the originating Checkpoint)*

## States & Transitions

**Route / Tour Definition:** `active` → `archived`; an edit while active creates a new version without changing active/archived status.

**Route Assignment:** follows base Activity lifecycle — `open` (assigned, not yet started) → `in_progress` (first Patrol begins) → `concluded` (every Tour Definition under its pinned Route version meets `required_count`, or the assignment period ends) | `cancelled`.

**Patrol:** follows base Activity lifecycle with extension-level status nuance (explicitly allowed by Activity Registry) — `open` → `in_progress` (first scan recorded) ⇄ **`interrupted`** (auto on the guard's Dispatch becoming active, #18a; back to `in_progress` on next in-scope scan or explicit resume) → `concluded` (completeness/compliance locked — reachable from `interrupted` too, at window close, with partial completeness attributed to the referenced Dispatch) | `cancelled` | **`missed`** (auto-created directly in this status if the expected window closes with no Patrol ever started — never transitions elsewhere except a Guard/Supervisor attaching a `resolution_note`, #18).

**Checkpoint Scan:** created once, immutable — no further states, consistent with an event record. A near-duplicate read within the debounce window doesn't create a new row at all (#20), rather than creating and later reconciling one.

**Patrol Finding:** follows base Activity lifecycle — `open` → `concluded` (resolved) | `cancelled` — independent of its originating Scan or Patrol's own state.

**CheckpointTagAssociation:** `active` → `removed` (tag replacement), following the standard single-current-value EntityAssociation pattern.

## Integrations

- **Location Registry**: Checkpoint's base TPT level.
- **Item Registry**: source of the physical tag Item a Checkpoint's `CheckpointTagAssociation` points to.
- **Activity Registry**: Route Assignment, Patrol, Checkpoint Scan, and Patrol Finding all register as Activity extensions — Route and Tour Definition are the only non-Activity (versioned plan) layers. Patrol's `missed` status uses Activity Registry's explicit allowance for extensions to layer richer status nuance on the base lifecycle.
- **Entity Registry Core**: identity, dedup/merge, and display-label requirements for Checkpoint, Route Assignment, Patrol, and Patrol Finding, same as any other Entity/Activity type — **Checkpoint Scan is a deliberate exception**, excluded from standard dedup matching in favor of a narrow write-time debounce (#20); a candidate precedent for Activity Registry to eventually document a general "high-volume event-type Activities may opt out of dedup" allowance, not resolved here.
- **Offline Data Sync**: Checkpoint Scan and Patrol Finding creation both follow the established offline append-only outbox/client-UUID pattern unmodified; a Patrol's own start/complete transitions are offline-appendable as Class 2 events since the assigned officer is the sole actor on their own Patrol.
- **Settings & Preferences**: owns tenant-default sequence-enforcement and scan-verification-method settings; Route/Tour Definition's versioning discipline mirrors (but isn't literally stored by) the pattern this feature established.
- **Domain Events / Notifications Engine**: missed/late tour detection publishes an automation-eligible event (#22) alongside the `missed` Patrol record itself (#17); a Tenant Admin-configured rule decides notification/escalation behavior.
- **Unit Dispatch & Proximity Routing (Module 2) — retrofit**: a Dispatch becoming active for a guard with an in-progress Patrol drives the Patrol's `interrupted` transition (#18a); the Patrol's completeness gap is attributable to the referenced Dispatch in reporting rather than reading as an unexplained skip.
- **Daily Activity Reports (DAR)**: Route Assignments, Patrols (including `missed` ones), Checkpoint Scans, and Patrol Findings are all ordinary Activities and are automatically picked up by any DAR filter matching their guard/site/time window.
- **Shift Passdowns & Handover Notes**: a `missed` Patrol, and a still-open Patrol Finding, are both natural candidates for a default Pass-On Rule.
- **Command/Action Bus**: "Assign route," "Start patrol" (self or on behalf of a unit), "Scan checkpoint," "Replace checkpoint tag," "Create patrol finding," and "Create [Activity Type] from checkpoint scan" register as invokable actions across every surface.
- **GIS & Mapping Services**: Checkpoint locations render on the map via their inherited Location geometry; a live Patrol's progress is a natural future map overlay.
- **Patrol Management**: the umbrella assignment layer this doc's Route Assignment executes underneath, via the retrofitted `post_ref` field. Its "on-behalf-of radio check" pattern for Mobile Patrol Unit location is directly reused here for Supervisor/Dispatcher-initiated Patrol starts (#14).
- **Module 12 (Tour Completeness Logs, Officer Performance Logs) and Module 9 (Location Hierarchy Designer, future)**: future consumers of this doc's Route Assignment/Patrol/Checkpoint Scan data — `missed` Patrols with real rows (#17) give both a clean, complete dataset to query.

## Permissions

| Action | Guard | Supervisor | Tenant Admin |
|---|---|---|---|
| Create/edit Checkpoint (incl. tag replacement, verification method) | ❌ | ✅ | ✅ |
| Create/edit Route & Tour Definitions (creates a new version) | ❌ | ✅ | ✅ |
| Assign a Route to a Guard | ❌ | ✅ | ✅ |
| Start a Patrol (self, or on behalf of a unit e.g. radioed) | ✅ (own) | ✅ | ❌ |
| Perform a Checkpoint Scan (execute assigned Route) | ✅ (own assignment) | ✅ (own assignment, if also assigned) | ❌ |
| Add a resolution note to a missed Patrol | ✅ (own) | ✅ | ❌ |
| Create a Patrol Finding from a scan | ✅ (own scan) | ✅ | ❌ |
| Resolve a Patrol Finding | ✅ (own, if permitted) | ✅ | ✅ |
| Author/edit Checkpoint inspection instructions | ❌ | ✅ | ✅ |
| View Route Assignment / Patrol compliance / completeness | ✅ (own) | ✅ (own roster) | ✅ |
| Configure sequence/verification-method defaults | ❌ | ❌ | ✅ |
| Configure missed-tour Domain Events rules | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Checkpoint Scan creation must work fully offline with no degraded behavior relative to any other Activity type, including a burst of scans recorded in sequence in a dead zone and synced together.
- The duplicate-scan debounce window must be short enough to only catch genuine accidental double-reads (e.g., a few seconds), never collapsing two legitimate scans of nearby checkpoints in quick succession — exact duration is a technical-spec/field-testing question (see Open Questions).
- `missed` Patrol creation must run reliably server-side on a schedule/computation independent of any specific guard's device coming back online — a missed occurrence must surface even if the guard never opens the app again that day.
- Version pinning (`route_version_ref`, `tour_definition_version_ref`) must be enforced so a Route Assignment/Patrol's effective requirements never silently change after creation, regardless of later plan edits.
- Patrol compliance computation (completeness/order/spacing) must be efficient enough to compute live as scans arrive, not only at conclusion, so a Supervisor can see in-progress status.
- CheckpointTagAssociation replacement must be atomic — no window where a Checkpoint has zero or two simultaneously active tags.
- A Checkpoint's `inspection_instructions` and `required_verification_method` must be cached client-side for offline availability.
- WCAG 2.1 / Section 508 accessible route-building, patrol-starting, scanning, compliance-review, and finding-creation flows, day one.

## Acceptance Criteria

- [ ] A Supervisor can build a Route with four Tour Definitions spanning four different Locations, one of which requires 2 occurrences at least 4 hours apart.
- [ ] A single Checkpoint referenced by two different Tour Definitions appears correctly in both without the Checkpoint record being duplicated.
- [ ] Assigning that Route to a Guard produces an `open` Route Assignment Activity pinned to the current Route version.
- [ ] A Supervisor starting a Patrol on a guard's behalf (e.g., after a radio call) correctly creates it with `start_method = explicit_dispatched`.
- [ ] Scanning the first checkpoint of a due, unambiguous Tour Definition with no active Patrol auto-starts one (`start_method = implicit_first_scan`).
- [ ] Scanning a checkpoint that matches two currently-eligible, not-yet-started Tour Definitions prompts the guard to choose rather than guessing.
- [ ] A checkpoint with a required tag scan but a missing/broken tag still allows the officer to mark it complete via manual override, recorded as `verified = false`.
- [ ] Editing a Tour Definition's `required_count` does not change the requirements of an already-in-progress Route Assignment/Patrol pinned to the prior version.
- [ ] An expected Tour Definition occurrence whose window closes with no Patrol started auto-creates a `missed` Patrol record.
- [ ] A Guard or Supervisor can attach a `resolution_note` to a missed Patrol without changing its `missed` status.
- [ ] A Dispatch becoming active for a guard mid-Patrol auto-transitions the Patrol to `interrupted` with `interrupted_by_dispatch_ref` set; the guard's next in-scope Checkpoint Scan (or an explicit resume) returns it to `in_progress` with the cycle recorded in `interruption_history[]`.
- [ ] A Patrol whose occurrence window closes while `interrupted` concludes with partial completeness, and its gap is attributable to the referenced Dispatch in completeness reporting.
- [ ] Two scans of the same checkpoint 2 seconds apart within the same Patrol collapse into a single Checkpoint Scan row; two scans of the same checkpoint on separate required occurrences (hours apart) do not.
- [ ] A second Patrol against the 4-hour-spaced Tour Definition, started 2 hours after the first, is correctly flagged `spacing_compliant = false`.
- [ ] Replacing a Checkpoint's tag preserves the Checkpoint's own identity and full prior scan history.
- [ ] A Checkpoint Scan performed offline gets a usable client UUID immediately and syncs correctly once connectivity returns.
- [ ] Route Assignment, Patrol (including `missed`), Checkpoint Scan, and Patrol Finding records for a given guard/night appear correctly in that guard's DAR filter view with no DAR-side special-casing.
- [ ] Launching "create Incident" (stubbed) from a Checkpoint Scan correctly pre-fills the checkpoint's Location as context, overridable by the user.

## Open Questions

- Exact duplicate-scan debounce window duration (a few seconds vs. a longer threshold) — pending technical spec / field testing.
- Exact versioning storage mechanics for Route/Tour Definition (full snapshot copy per version vs. delta/event-sourced versioning) — technical-spec-level, likely informed by whichever the platform's general versioning implementation (Settings & Preferences, CLI aliases) already settles on.
- UX for the ambiguity prompt when a scanned Checkpoint matches more than one currently-eligible Tour Definition — not designed here.
- Whether Route-level `tour_sequence_enforced` needs its own minimum-gap concept between different Tour Definitions (distinct from a single Tour Definition's own `min_interval` between its repeat occurrences) — not addressed, leaning toward "not needed" but unconfirmed.
- Exact Route Assignment completion policy when the assignment period ends with some Tour Definitions still short of `required_count` — current default assumes auto-conclude at period end (producing `missed` Patrols for the gaps), not confirmed.
- Exact baseline Domain Events rule(s) shipped out of the box for missed/late tours — pending content/UX design.
- Whether CheckpointTagAssociation's replacement action requires elevated permission/step-up — currently modeled as a standard Supervisor+ action, not step-up-gated.
- Whether `inspection_instructions` should support structured checklists rather than free text only.
- Exact default Patrol Finding category taxonomy shipped out of the box.
- Whether Patrol Finding needs its own severity field (distinct from category) to drive urgency in Domain Events rules.
- Whether Activity Registry should formally document a "high-volume event-type Activity may opt out of dedup" allowance, given Checkpoint Scan's precedent here — flagged for that doc's future attention, not resolved in this doc.
