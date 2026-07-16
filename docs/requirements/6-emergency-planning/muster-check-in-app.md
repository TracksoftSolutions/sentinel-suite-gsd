# Muster Check-in App

## Overview

Muster Check-in App (Module 6, 4/8) closes real debt Drill Compliance Logging flagged against itself: its own Occupant Exit Report is "an honest, aggregate-only interim stand-in... explicitly deferred to Module 6's future Muster Check-in App and Evacuation Roster Reconciliation" for real per-occupant roster data. This doc builds the actual check-in mechanism and, per MODULES.md's own bullet placement, a real (if honestly-scoped) missing/not-checked-in list — the deeper Access-Control-swipe reconciliation, live dashboard, and percentages stay Evacuation Roster Reconciliation's job, next in this module.

Four elicited decisions:

1. **Barcode scanning is the default, device-agnostic check-in method; NFC is an optional adaptor-gated alternative.** Per explicit user direction, a generic HID-class barcode scanner (commodity hardware the OS treats as a keyboard, no special driver) or a phone camera decoding a barcode/QR are functionally the same capture and need no dedicated integration — while NFC "would need an adaptor," consistent with the platform's established provider-adaptor discipline for anything requiring more specialized hardware/software integration. Fixed reader hardware permanently mounted at a Muster Point is explicitly not ruled out for the future, but not built now.
2. **Coverage is anyone registered as on-site, not employees-only** — per explicit user direction ("all parties registered as on site"), reusing Person Registry's existing Employee/Visitor/Contractor/Occupant extension types rather than a narrower employee-only scope.
3. **A real, honestly-scoped Expected Occupant List builds now**, per explicit user direction — sourced from what's actually trackable today (on-duty Staff via DAR Shift Window/Post, plus open Visitor Kiosk App Visits), explicitly flagged as a partial proxy rather than a claim of full coverage. The richer Access-Control-swipe-based reconciliation is Evacuation Roster Reconciliation's job.
4. **Muster Point is a lightweight designation over an existing Location, not a new Location extension** — the same "a place playing a role, not a new kind of place" reasoning ICS Role Mapping already applied to Command Post Designation, since nothing new needs to be mounted there today given decision #1.

## Actors & Roles

- **Muster Marshal / Fire Warden** — scans or manually checks in occupants at their assigned Muster Point, works the missing list for their zone, can start a Muster Session.
- **Any on-site Person** — self-checks-in via scan or a zero-friction tap.
- **EOC Coordinator / Dispatcher / Safety Coordinator** — views the live roster/missing list across every Muster Point, concludes a Muster Session, launches Missing Person Search.
- **Site / Tenant Admin** — designates Muster Points, configures the NFC Scan Adaptor if used.

## User Stories

- As a **Muster Marshal**, I want to scan an occupant's badge as they arrive at my muster point, or check them in manually if they don't have one, so we have an accurate list fast.
- As an **occupant**, I want to self-check-in with one tap in the app if I don't have a badge or a Marshal isn't scanning me.
- As an **EOC Coordinator**, I want to see who's checked in vs. still missing across every muster point in real time.
- As an **EOC Coordinator**, I want to launch a search for a specific missing person directly into CAD with their info pre-filled, not re-enter it.
- As a **Site Admin**, I want to designate our parking lot and north lawn as our two muster points ahead of time, so Marshals know where to work during a real event.
- As a **Safety Coordinator**, I want the expected roster used for a drill to be a fair, stable count, not confused with real-time badge-ins happening during the drill itself.

## Functional Requirements

### Muster Point Designation
1. **Muster Point Designation** (lightweight, local record — not a new Location extension) carries `location_ref`, `label`, `site_ref`. A site can designate multiple named Muster Points ahead of time.

### Muster Session (the anchor for one evacuation event)
2. **Muster Session** registers as its own Activity extension — the event wrapper every Muster Check-in belongs to, so a real evacuation's roster is never confused with a prior drill's. Carries the platform's now-familiar widened-anchor shape: `anchor_type` (`eoc_activation`, `call`, `incident`, `coop_activation`, `compliance_drill`, `none`) + `anchor_ref` (nullable) — the fourth mechanism to reuse this exact convention (after Checklist Run, Operational Period, ICS Role Assignment), as its own local field following the same precedent, not a shared table.
3. **A Muster Session can start with `anchor_type = none` (ad hoc)** — the priority during a real evacuation is capturing check-ins immediately, not waiting for a formal record to exist first — and be explicitly linked to an Incident afterward via **Link to Incident** (Command/Action Bus action), never automatic, mirroring the platform's launch-point discipline.
4. **At Muster Session start, an Expected Occupant List snapshot freezes**: every Person currently on-duty at that site (via DAR Shift Window / Patrol Management Post) plus every Person with an open, checked-in-not-checked-out Visit at that site (via Visitor Kiosk App) — an honest, deliberately-scoped interim proxy that doesn't catch someone who entered untracked, explicitly flagged for reconciliation once Evacuation Roster Reconciliation's real Access-Control-swipe data exists. The snapshot is frozen at session start and never dynamically recalculated as the event unfolds, so the roster stays a stable target during a live emergency.

### Muster Check-in
5. **Muster Check-in** registers as its own Activity extension: `session_ref`, `person_ref`, `muster_point_ref`, `checked_in_at`, `method` (`self_scan`, `self_tap`, `marshal_scan`, `manual_roll_call`), `scanned_by` (nullable) — one row per person per session; a correction/re-scan is a new row (the latest displays, full history retained), the same "correction is a new row" discipline used platform-wide.
6. **Barcode scanning (`self_scan`/`marshal_scan` with `scan_technology = barcode`) is the default, device-agnostic method** — a generic HID-class barcode scanner or a phone camera decoding a barcode/QR through the mobile app, functionally identical capture either way, no dedicated adaptor required.
7. **NFC scanning is an optional alternate method gated behind a new NFC Scan Adaptor registration** — per the platform's established provider-adaptor discipline, a tenant opts in only if their devices/badges actually support it; unavailable/not offered otherwise.
8. **Self-check-in (`self_tap`) reuses the platform's zero-friction-tap pattern** (Safety Check-in, Dispatch Acknowledgment) for anyone without a scannable badge or a Marshal nearby — a single tap, no typed input.
9. **Manual roll call (`manual_roll_call`) lets a Marshal mark a specific Person present by name with no scan/tap at all**, for anyone without a badge or a working phone.
10. Fixed reader hardware permanently mounted at a Muster Point is explicitly not ruled out for a future iteration but not built now — flagged in Open Questions rather than architected away.

### Missing / not-checked-in and the CAD link
11. A Person on the Expected Occupant List snapshot with no Muster Check-in row in the current session displays as **missing/not-checked-in** on the live roster, grouped by the Muster Point context available (or "unassigned" if none).
12. **Missing Person Search** (Command/Action Bus action, the platform's established launch-point pattern) creates or adds a participant to an Incident — the Muster Session's own anchored Incident if one exists, otherwise a new one — with context pre-filled (person, last-known info, site, Muster Session reference) — zero new mechanism.

## Data Model / Fields

**Muster Point Designation** (local, tenant-configured)
- designation_id, location_ref, label, site_ref

**Muster Session** (Activity extension; entity_id is the shared PK, FK → Activity)
- site_ref
- anchor_type (eoc_activation, call, incident, coop_activation, compliance_drill, none), anchor_ref (nullable)
- status (active, concluded), started_at, concluded_at (nullable)
- expected_occupant_snapshot[] (person_ref, source: on_duty_staff, active_visit)

**Muster Check-in** (Activity extension; entity_id is the shared PK, FK → Activity)
- session_ref, person_ref, muster_point_ref (nullable — a self-tap without location context leaves this unset)
- checked_in_at, method (self_scan, self_tap, marshal_scan, manual_roll_call)
- scanned_by (nullable, FK → Person), scan_technology (barcode, nfc — nullable, set only when scanned)

**NFC Scan Adaptor Registration** (tenant-configured, optional)
- adaptor_id, tenant_id, enabled, connection_config

## States & Transitions

- **Muster Point Designation:** plain create/edit/remove, no lifecycle.
- **Muster Session:** `active` → `concluded` — an explicit action by a Marshal or higher to start, by an EOC Coordinator/Safety Coordinator/Admin to conclude. Concluding is always allowed regardless of outstanding missing people (the platform's standing compliance-never-gates rule), but requires an explicit confirmation surfacing the outstanding count first.
- **Muster Check-in:** created once per interaction; a correction/re-scan is a new row for the same person, not an edit.

## Integrations

- **DAR (Shift Window) / Patrol Management (Post)**: source of the on-duty-staff half of the Expected Occupant List snapshot.
- **Visitor Kiosk App**: source of the active-Visit half of the Expected Occupant List snapshot.
- **Location Registry**: source of a Muster Point Designation's underlying Location.
- **EOC Activation Checklists / Continuity of Operations Plans / Incident Reporting & Management / Drill Compliance Logging**: valid `anchor_type` targets for a Muster Session — the fourth mechanism to reuse the platform's widened-anchor convention.
- **Command/Action Bus**: Start/Conclude Muster Session, Check In (self-scan, self-tap, marshal-scan, manual roll call), Link to Incident, Missing Person Search all register as actions.
- **Real-Time Delivery**: live roster/missing-list view meets the platform's standard safety-relevant push/update latency target.
- **Notifications Engine**: concluding a Muster Session with missing people still outstanding fires a notification to the EOC/Safety Coordinator — an existing category/tier, no new delivery mechanism.
- **Offline Data Sync**: self-tap/scan check-ins are append-only, sole-actor-execution events under the platform's existing offline three-class contract — no new offline mechanism, and genuinely important given an evacuation may coincide with a network/power disruption.
- **Drill Compliance Logging**: forward note, not a build here — a drill-anchored Muster Session's real Check-in timestamps could eventually replace Occupant Exit Report's current aggregate estimate, but "arrival at a muster point" and "exit time" aren't quite the same measurement and deserve their own consideration (see Open Questions).
- **Evacuation Roster Reconciliation** (next doc, forward reference only): owns the real Access-Control-swipe-based reconciliation, live dashboard, and muster-progress percentages — this doc's Expected Occupant List is an honest interim proxy, not the final word.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin | Muster Marshal | EOC Coordinator/Dispatcher/Safety Coordinator | Any on-site Person |
|---|---|---|---|---|---|---|
| Designate Muster Points | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ | ❌ |
| Start Muster Session | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Conclude Muster Session | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Check in (self-scan, self-tap) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (own) |
| Check in someone else (marshal-scan, manual roll call) | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| View live roster / missing list | ✅ | ✅ | ✅ | ✅ (own point) | ✅ (all points) | ❌ |
| Missing Person Search / Link to Incident | ✅ | ✅ | ✅ | ❌ | ✅ | ❌ |
| Configure NFC Scan Adaptor | ✅ | ✅ (own tenant) | ❌ | ❌ | ❌ | ❌ |

## Non-Functional / Constraints

- Check-in writes and live roster updates meet Real-Time Delivery's standard safety-relevant latency target — a delayed missing-list update defeats the purpose.
- Concluding a Muster Session with unresolved missing people is always allowed, never blocked, but requires an explicit confirmation surfacing the outstanding count — the platform's standard confirmation-gate discipline applied to a genuinely high-stakes moment, not a hard block.
- The Expected Occupant List's honest scope limitation (doesn't catch untracked entrants) must be visibly disclosed on the roster view itself, not just in documentation — never implying complete coverage it can't actually provide.
- Barcode/NFC scan capture, self-tap check-in, and marshal-performed scans are all offline-capturable (append-only, discrete-event logging) under the platform's existing offline contract.
- WCAG 2.1 / Section 508 accessible check-in flows, including manual roll call, day one.

## Acceptance Criteria

- [ ] Designating two Muster Points at a site and starting a Muster Session with `anchor_type = none` succeeds without requiring any formal Incident/Activation to exist first.
- [ ] The Expected Occupant List snapshot at session start correctly includes on-duty Staff and open Visitor Visits, and does not change as the session progresses even as new Visits open/close.
- [ ] A barcode scan (via HID scanner or phone camera) and a self-tap check-in both correctly create a Muster Check-in row with the right `method` value.
- [ ] NFC scanning is unavailable/not offered unless the tenant has configured an NFC Scan Adaptor.
- [ ] A Person on the Expected Occupant List with no Muster Check-in row displays as missing on the live roster.
- [ ] Missing Person Search launches into a new or existing Incident with person/site/session context pre-filled.
- [ ] Concluding a Muster Session with outstanding missing people succeeds after explicit confirmation, never silently blocked or silently allowed without the warning.
- [ ] Check-in via self-tap while offline syncs correctly once connectivity resumes.

## Open Questions

- Whether Occupant Exit Report (Drill Compliance Logging) should retrofit to derive real per-zone timing from a drill-anchored Muster Session's actual Check-in timestamps, replacing its current aggregate estimate — a plausible light reconciliation, not built here, since "arrival at a muster point" and "exit time" aren't quite the same measurement.
- Whether fixed reader hardware permanently mounted at a Muster Point should become a future triad-pattern instance — explicitly not ruled out, not built now.
- Exact NFC Scan Adaptor technical shape (reader/OS combinations, connection protocol) — a technical-spec-level task, not resolved here.
- Whether Checklist Run's `anchor_type` should eventually widen a further time to include `muster_session` (e.g., a "Muster Coordinator Checklist" tied to a specific session) — a plausible future generalization once real usage patterns are observed, not built now given no second concrete consumer exists yet.
- The full reconciliation against real Access-Control-swipe data (a more accurate expected-occupancy source than this doc's Staff/Visit proxy) is explicitly Evacuation Roster Reconciliation's job, next in this module.
