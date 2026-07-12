# Guard Tour & Checkpoint Verification

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec (see Open Questions — several defaults below are best-guess and flagged for confirmation)

## Overview

Four layers, from definition down to individual event, each building on Master Records infrastructure already specified:

1. **Checkpoint** — a scannable tour stop. Structurally a **triad**, per the user's own framing: it IS a place (extends Location Registry, inheriting geometry/hierarchy/GIS placement), it has a **physical tag** that triggers it (an Item Registry entity — NFC tag, QR sticker, barcode — associated via a single-current-value EntityAssociation so a broken/replaced tag doesn't lose the checkpoint's own identity or scan history), and it is additionally **its own thing** — a distinct registrable concept a Route references, not reducible to either half alone.
2. **Route** — an assignment plan, not a flat checkpoint list. A Route is composed of one or more **Tour Definitions**, and a single Route can span multiple places: per the user's example, "Officer A's Route 1" might require one tour of Building/Site 1, two tours of Building/Site 2 spaced at least 4 hours apart, one tour of Site 3, and one of Site 4 — each a separate Tour Definition with its own target place, checkpoint set, required occurrence count, and minimum spacing.
3. **Tour Run** — an **Activity extension** (per Activity Registry) representing one actual execution of a Tour Definition by a guard: full offline-safe identity, audit, dedup, and cross-module timeline visibility, directly serving Module 12's future Tour Completeness Logs and Officer Performance Logs without either reimplementing scan tracking.
4. **Checkpoint Scan** — an individual timed (or otherwise triggered) event within a Tour Run: which checkpoint, when, by what method, whether verified. Modeled as its own thin Activity extension, referencing its parent Tour Run and scanned Checkpoint by direct field (not an EntityAssociation — the parent/checkpoint relationship is fixed at creation and never re-parented, so it doesn't benefit from association-style history the way ownership or custody does).

Both sequence enforcement (must checkpoints be visited in order) and scan verification method (tag scan, GPS, or both) are **configurable per Tour Definition**, since a linear indoor sweep and a scattered set of exterior posts have genuinely different real-world needs — registered against Settings & Preferences where tenant-wide defaults make sense, with per-Tour-Definition override.

**Scope boundary:** this doc covers fixed Route/Checkpoint/Tour/Scan mechanics only. Broader roving or dispatch-triggered patrol assignment without fixed checkpoints is Patrol Management's territory (next in the elicitation queue) — not solved here.

## Actors & Roles

- **Guard** — executes an assigned Route, performs Checkpoint Scans (Tour Runs), views their own assignment and completion status.
- **Supervisor** — authors Routes and Tour Definitions, assigns Routes to Guards, reviews Tour Run compliance (completeness, order, spacing), configures Checkpoints (including tag replacement).
- **Tenant Admin** — sets tenant-default sequence-enforcement and scan-verification-method settings, configures baseline missed-tour alerting rules (via Domain Events).
- **Records Admin** — resolves Entity Registry Core dedup flags on Checkpoint (as a Location extension) and Tour Run (as an Activity extension), same as any other entity type.

## User Stories

- As a **Supervisor**, I want to build a Route that requires two tours of the warehouse spaced at least 4 hours apart alongside single tours of three other buildings, all under one assignment, so I don't have to manage four separate schedules for one guard.
- As a **Guard**, I want to scan a checkpoint's NFC tag and have it immediately recorded, even if I'm offline in a dead zone, so my tour isn't lost.
- As a **Supervisor**, I want to know immediately if a required tour is running late or was skipped, not discover it the next morning in a report.
- As a **Facilities Coordinator**, I want to replace a damaged checkpoint's NFC tag with a new one without losing that checkpoint's scan history or identity.
- As an **Investigator**, I want to see every checkpoint scan a specific guard performed on a specific night as part of one uniform Activity timeline, the same mechanism I'd use to review any other Activity.
- As a **Guard**, I want to know which method (tag, GPS, or both) is required at a given checkpoint before I get there, so I'm not stuck trying to scan a tag that isn't actually required.

## Functional Requirements

### Checkpoint (Location extension + associated tag Item)
1. **Checkpoint** registers as a Location extension (per Location Registry), inheriting base identity, geometry, GIS placement, and dedup/merge.
2. A Checkpoint's physical scan tag is a separate Item Registry entity, linked via a single-current-value EntityAssociation (`CheckpointTagAssociation`) — replacing a damaged/lost tag means removing the old active association row and adding a new one pointing to a newly-registered tag Item, leaving the prior tag as history, exactly like Vehicle's `ConveyanceOwnerAssociation` pattern.
3. A Checkpoint can decline to have a tag at all if its Tour Definition's verification method is GPS-only (#12).

### Route & Tour Definition
4. A **Route** is a named assignment plan owned by a Supervisor, composed of one or more **Tour Definitions**.
5. A **Tour Definition** declares: a target place (`location_ref` — any Location, typically a Building or Site-level Location, not necessarily the top of the hierarchy), the set of Checkpoints in scope at that place, `required_count` (how many times this Tour Definition must be completed over the Route's assignment period), and an optional `min_interval` (minimum time that must elapse between consecutive completions, e.g., 4 hours).
6. A Route can mix Tour Definitions targeting entirely different places under one assignment (per the warehouse-plus-three-buildings example) — there's no requirement that a Route's Tour Definitions share a location.
7. A Route is **assigned** to a Guard for an assignment period — a specific shift (optionally referencing DAR's Shift Window, when clocked-shift tracking is in play) or an explicit date/time range when no shift concept applies.

### Tour Run (Activity extension)
8. **Tour Run** registers as an Activity extension: one guard's actual execution instance against a specific Tour Definition, inheriting base Activity identity, offline-safe numbering, and dedup/merge.
9. A Tour Run's completion is computed against its Tour Definition: percentage of in-scope Checkpoints scanned, order compliance (if enforced), and — for a repeat occurrence of the same Tour Definition — spacing compliance against `min_interval` relative to the immediately preceding Tour Run for that same definition.
10. A Tour Run reaching the Activity base `concluded` status is when its final completeness/compliance is computed and locked; a still-`in_progress` Tour Run's compliance is provisional/live.

### Checkpoint Scan (Activity extension)
11. **Checkpoint Scan** registers as a thin Activity extension: `tour_run_ref` (direct field, fixed at creation), `checkpoint_ref` (direct field), `scan_method` (tag, gps, manual_override), `verified` (bool — passed its Tour Definition's required verification method), `captured_gps` (optional, for corroboration/anomaly detection even when tag scan is the required method), `scanned_by`, `scan_timestamp`.
12. Required verification method (tag-only, GPS-only, or both) is configurable per Tour Definition, defaulting to a tenant-wide Settings & Preferences value.
13. Sequence enforcement (strict order vs. any order within window) is configurable per Tour Definition, defaulting to a tenant-wide Settings & Preferences value; an out-of-sequence scan on an order-enforced Tour Definition is recorded (never discarded) but flagged as a deviation.
14. Checkpoint Scan creation works fully offline, per the platform's established offline model — a scan performed in a dead zone is recorded immediately with a client UUID and synced when connectivity returns.

### Missed/late detection (assumed default — see Open Questions)
15. A Tour Definition occurrence not completed within its expected window (derived from `required_count` spread across the assignment period, and `min_interval` where set) publishes an automation-eligible domain event, letting a Tenant Admin configure a Domain Events rule (e.g., notify the Supervisor, or auto-create a Passdown Pass-On Flag) rather than this doc hardcoding a single fixed alert behavior.

## Data Model / Fields

**Checkpoint** (Location extension, TPT level: entity_id shared PK, FK → Location.entity_id)
- entity_id (PK, FK → Location)
- *(tag association is a row, not a field — see CheckpointTagAssociation)*

**CheckpointTagAssociation** (EntityAssociation TPT subtype)
- association_id (PK, FK → EntityAssociation; entity_id_a = Checkpoint, entity_id_b = Item/tag)
- no extra fields beyond base EntityAssociation shape; single-current-value pattern (active = current tag, removed = prior tags)

**Route**
- route_id, tenant_id, name, owning_supervisor_ref
- status (active, archived)

**Tour Definition**
- tour_definition_id, tenant_id, route_ref
- location_ref (target place), checkpoint_refs[] (in-scope Checkpoints)
- sequence_enforced (bool, defaults from Settings & Preferences)
- required_verification_method (tag, gps, both — defaults from Settings & Preferences)
- required_count (per assignment period), min_interval (nullable)

**Route Assignment**
- assignment_id, tenant_id, route_ref, person_ref (Guard)
- assignment_period_start, assignment_period_end (or shift_window_ref, if shift-scoped)
- post_ref (nullable — FK → Patrol Management's Post, when this route fulfills a Fixed Post's local patrols or a Mobile Patrol Unit's checkpoint-based coverage; retrofit, see [patrol-management.md](patrol-management.md))
- status (active, completed, cancelled)

**Tour Run** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- tour_definition_ref, assignment_ref
- completeness_pct, order_compliant (bool, nullable if unordered), spacing_compliant (bool, nullable if no min_interval or first occurrence)

**Checkpoint Scan** (Activity extension, TPT level)
- entity_id (PK, FK → Activity)
- tour_run_ref, checkpoint_ref
- scan_method (tag, gps, manual_override), verified (bool)
- captured_gps (nullable), scanned_by, scan_timestamp

## States & Transitions

**Route:** `active` → `archived`.

**Route Assignment:** `active` → `completed` (assignment period ends) | `cancelled`.

**Tour Run:** follows base Activity lifecycle — `open` → `in_progress` (first scan recorded) → `concluded` (completeness/compliance locked) | `cancelled`.

**Checkpoint Scan:** created once, immutable — no further states, consistent with an event record.

**CheckpointTagAssociation:** `active` → `removed` (tag replacement), following the standard single-current-value EntityAssociation pattern.

## Integrations

- **Location Registry**: Checkpoint's base TPT level.
- **Item Registry**: source of the physical tag Item a Checkpoint's `CheckpointTagAssociation` points to.
- **Activity Registry**: Tour Run and Checkpoint Scan both register as Activity extensions.
- **Entity Registry Core**: identity, dedup/merge, and display-label requirements for Checkpoint, Tour Run, and Checkpoint Scan, same as any other Entity/Activity type.
- **Offline Data Sync**: Checkpoint Scan creation follows the established offline CRDT/client-UUID pattern unmodified.
- **Settings & Preferences**: owns tenant-default sequence-enforcement and scan-verification-method settings, with per-Tour-Definition override.
- **Domain Events / Notifications Engine**: missed/late tour detection publishes an automation-eligible event; a Tenant Admin-configured rule decides the actual notification/escalation behavior rather than a hardcoded alert path.
- **Daily Activity Reports (DAR)**: Tour Runs and Checkpoint Scans are ordinary Activities and are automatically picked up by any DAR filter matching their guard/site/time window, with no DAR-side change required.
- **Shift Passdowns & Handover Notes**: a missed/late tour is a natural candidate for a default Pass-On Rule (e.g., auto-flag a missed required Tour Definition for the next shift) — a Tenant Admin configures this via Passdown's existing Pass-On Rule mechanism, not a bespoke integration.
- **Command/Action Bus**: "Assign route," "Start tour," "Scan checkpoint," "Replace checkpoint tag" register as invokable actions across every surface.
- **GIS & Mapping Services**: Checkpoint locations render on the map via their inherited Location geometry; a live Tour Run's progress is a natural future map overlay.
- **Patrol Management**: the umbrella assignment layer this doc's Route Assignment executes underneath, via the retrofitted `post_ref` field — a Fixed Post's local patrols and an optional Mobile Patrol Unit's checkpoint coverage are both ordinary Route Assignments referencing a Post. Patrol Management owns the broader roving/non-checkpoint-based patrol concept (Mobile Patrol Unit tracking, ad hoc Patrol Requests) this doc doesn't.
- **Module 12 (Tour Completeness Logs, Officer Performance Logs) and Module 9 (Location Hierarchy Designer, future)**: future consumers of this doc's Tour Run/Checkpoint Scan data and Checkpoint's Location-extension placement, respectively.

## Permissions

| Action | Guard | Supervisor | Tenant Admin |
|---|---|---|---|
| Create/edit Checkpoint (incl. tag replacement) | ❌ | ✅ | ✅ |
| Create/edit Route & Tour Definitions | ❌ | ✅ | ✅ |
| Assign a Route to a Guard | ❌ | ✅ | ✅ |
| Perform a Checkpoint Scan (execute assigned Route) | ✅ (own assignment) | ✅ (own assignment, if also assigned) | ❌ |
| View Tour Run compliance / completeness | ✅ (own) | ✅ (own roster) | ✅ |
| Configure sequence/verification-method defaults | ❌ | ❌ | ✅ |
| Configure missed-tour Domain Events rules | ❌ | ❌ | ✅ |

## Non-Functional / Constraints

- Checkpoint Scan creation must work fully offline with no degraded behavior relative to any other Activity type, including a burst of scans recorded in sequence in a dead zone and synced together.
- Anti-spoofing corroboration (captured GPS alongside a tag scan) must not block or delay scan recording if GPS is unavailable indoors — GPS is corroborating data, never a hard requirement when tag-based verification is what's configured.
- Tour Run compliance computation (completeness/order/spacing) must be efficient enough to compute live as scans arrive, not only at conclusion, so a Supervisor can see in-progress status.
- CheckpointTagAssociation replacement must be atomic — no window where a Checkpoint has zero or two simultaneously active tags.
- WCAG 2.1 / Section 508 accessible route-building, scanning, and compliance-review flows, day one.

## Acceptance Criteria

- [ ] A Supervisor can build a Route with four Tour Definitions spanning four different Locations, one of which requires 2 occurrences at least 4 hours apart.
- [ ] Assigning that Route to a Guard produces a Route Assignment the Guard can see and begin executing.
- [ ] Scanning a Checkpoint's tag (online or offline) creates a Checkpoint Scan linked to the correct Tour Run and Checkpoint.
- [ ] A second Tour Run against the 4-hour-spaced Tour Definition, started 2 hours after the first, is correctly flagged `spacing_compliant = false`.
- [ ] An out-of-sequence scan on a sequence-enforced Tour Definition is recorded (not discarded) and flagged as a deviation.
- [ ] Replacing a Checkpoint's tag preserves the Checkpoint's own identity and full prior scan history; the old tag's association becomes `removed`, the new one `active`.
- [ ] A Checkpoint Scan performed offline gets a usable client UUID immediately and syncs correctly once connectivity returns.
- [ ] A Tour Definition's required verification method (tag/GPS/both) is enforced — a scan missing the required method is recorded but marked `verified = false`.
- [ ] A missed required Tour Definition occurrence publishes a domain event that a configured Domain Events rule can act on.
- [ ] Tour Run and Checkpoint Scan records for a given guard/night appear correctly in that guard's DAR filter view with no DAR-side special-casing.

## Open Questions

- **Exact missed/late detection window logic** (#15 is a reasonable default, not confirmed): how "expected window" is derived from `required_count` + assignment period + `min_interval` — e.g., evenly spread, or Supervisor-defined explicit time windows per occurrence — needs confirmation.
- **Checkpoint Scan as its own Activity extension vs. a lightweight child record** was the recommended option in the original question but not explicitly confirmed before this doc was drafted — flagged for review; if scan volume proves too high for full Activity-table treatment, the fallback is a lightweight child-record table on Tour Run instead.
- Whether Route Assignment should require a DAR/Shift Window link at all times (tying tour execution strictly to a clocked shift) or remain optional as drafted — current default is optional.
- Exact baseline Domain Events rule(s) shipped out of the box for missed/late tours (e.g., auto-notify Supervisor after N minutes past window) — pending content/UX design.
- Whether a guard can start an unassigned/ad hoc tour (not tied to any Route Assignment) for flexibility, or execution is always strictly tied to an assignment — not addressed here, leaning toward "assignment required" but not confirmed.
- Whether CheckpointTagAssociation's replacement action requires elevated permission/step-up (tags are a security-relevant physical asset) — currently modeled as a standard Supervisor+ action, not step-up-gated; open to revisiting.
