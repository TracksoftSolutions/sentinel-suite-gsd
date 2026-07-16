# Evacuation Roster Reconciliation

## Overview

Evacuation Roster Reconciliation (Module 6, 5/8) closes out the muster/evacuation pair Muster Check-in App opened: that doc built the actual check-in mechanism and an honestly-scoped Expected Occupant List, explicitly deferring three things to this doc — the real Access-Control-swipe-based reconciliation, a live evacuation dashboard with muster-progress percentages, and command-post alerting on outstanding missing occupants. This doc delivers all three, plus resolves a standing cross-cutting open question flagged by both Person Registry and Pre-Incident Plans.

Four elicited decisions:

1. **Real Access-Control-swipe reconciliation is an honest interim stand-in, not built here.** Module 19 (the future upstream physical-security integration gateway) owns real door-swipe telemetry ingestion, and it isn't specified yet — Access Credential Management is explicitly access-content-agnostic and models no door/swipe events at all today. Rather than either faking swipe data or silently dropping the "reconciliation" half of this doc's own name, reconciliation is built as a **registered multi-source mechanism** (see FR #1–3) populated today by the two real sources that already exist (Muster Check-in itself, and on-duty officers' GIS Position Record), with Module 19's future swipe adaptor as a forward-referenced third registrant — the same deferred-integration posture used throughout the platform's history (DAR's Shift Window, EOC Logistics Hub's Resource Type Definition, Muster Check-in's own Expected Occupant List proxy).
2. **This doc resolves the platform-wide "active emergency event" open question** Person Registry and Pre-Incident Plans both flagged (see FR #10–13) — a new **Active Emergency Event Source Registration**, mirroring Health Signal Registration/Log Source Registration's opt-in-registry pattern, with an active Muster Session (this doc's own domain) as a concrete, motivating fourth registrant alongside Call/Incident, EOC Activation, and COOP Activation.
3. **The signal is scoped per-Location, matching Pre-Incident Plans' existing dispatch-context trigger exactly** — not a separate coarser per-Site concept. A Muster Session's own covered Location is the Site itself (a high-level ancestor Location), so under the platform's existing ancestor-or-self containment discipline, every descendant Location within that site is automatically covered while the session is active — the same breadth a per-Site design would have given, achieved by reusing one location-hierarchy mechanism instead of adding a second.
4. **Command-post alerting is a live, mid-session threshold alert** (FR #7–9), not merely a retarget of Muster Check-in's existing conclusion-time notification — targeting the Muster Session's own current Incident Commander (via ICS Role Assignment, where the anchor supports it) rather than a generic EOC/Safety Coordinator broadcast.

## Actors & Roles

- **EOC Coordinator / Dispatcher / Safety Coordinator** — views the Evacuation Dashboard, receives fallback command-post alerts when no ICS Incident Commander is assigned.
- **Incident Commander** (current ICS Role Assignment holder, Incident- or EOC-Activation-anchored sessions only) — receives the live missing-occupant alert directly.
- **Muster Marshal / Fire Warden** — unchanged from Muster Check-in App; this doc adds no new marshal-facing action.
- **Site / Tenant Admin** — registers/configures Occupancy Reconciliation Sources and Active Emergency Event Sources (platform-registered, not typically touched per-tenant, but visible for audit).

## User Stories

- As an **EOC Coordinator**, I want a live percentage of who's checked in, broken down by muster point, so I know at a glance how close we are to a full accounting.
- As an **EOC Coordinator**, I want a reconciliation log I can pull up after the fact showing exactly what data confirmed each person's status, not just a final checked-in/missing binary.
- As an **Incident Commander**, I want to be alerted directly and immediately if occupants are still unaccounted for partway through an evacuation, not just told at the very end when someone remembers to conclude the session.
- As a **Dispatcher/Safety Coordinator**, I want the same missing-occupant alert routed to me when there's no formal Incident Commander assigned yet, so an ad hoc muster (no EOC, no Incident) still reaches a real person.
- As a **Supervisor working an Incident**, I want medical-alert and emergency-contact fields for people at that location to become visible to me automatically once there's a genuine active emergency there, without a separate manual unlock step.

## Functional Requirements

### Occupancy Reconciliation Source Registration
1. **Occupancy Reconciliation Source Registration** is a new opt-in registry (mirroring Health Signal Registration / Log Source Registration's degrade-gracefully-if-unregistered pattern): `(source_type, description, resolver)` — a resolver is a named read-model callback returning, for a given Muster Session, which expected occupants that source can independently confirm as accounted-for (or not), and how.
2. **Two real day-one registrants**, both using data the platform already collects — no new capture mechanism:
   - **Muster Check-in** (self-registered, trivial case): a Person with a Muster Check-in row in the session is confirmed by their own explicit check-in.
   - **GIS Position Record**: for on-duty staff specifically, a currently-tracked Position Record outside the site's geofence during an active Muster Session is a secondary confirmation signal ("last known position clear of the site") distinct from and never a substitute for an explicit check-in — surfaced as a supporting data point on the reconciliation log, never auto-marking someone checked in.
3. **Module 19's future Access-Control swipe ingestion is a forward-referenced third registrant, not built here** — once that gateway's Signal Disposition promotion path exists, a swipe-derived "badged out after session start" signal registers the same way, with zero rework to this doc's reconciliation log or dashboard.

### Evacuation Reconciliation Log
4. **Evacuation Reconciliation Log** is a Document snapshot of one Muster Session, following the platform's established report pattern (DAR/Incident Report/Historical CAD Log Reconstruction): a filtered read — here, over the session's Expected Occupant List, Muster Check-in rows, and every registered Occupancy Reconciliation Source's output — snapshotted into an immutable Document at generation time.
5. Generation is **auto-on-conclusion by default, with ad hoc regeneration always available while the session is still active** (tenant-configurable generation mode, the same auto/ad hoc/both axis DAR established) — an ad hoc pull mid-event gives a Coordinator a documented snapshot without forcing conclusion first.
6. Each entry shows the Person, their check-in status, and — when available — which registered source(s) contributed corroborating signal, with an honest "no additional source available" state rather than implying swipe-level confirmation that doesn't exist yet.

### Command Post Missing-Occupant Alert
7. **Muster Session gains a live-computed, non-stored `all_expected_checked_in` status** (true once every Person on the frozen Expected Occupant List snapshot has a Muster Check-in row) — evaluated continuously while `status = active`, not just at conclusion.
8. A new Duration Watchdog instance registers: `(muster_session, all_expected_checked_in, false)` — a tenant-configurable duration after session start with the condition still false. This is the mechanism's first instance watching a **live-computed** field rather than a literal stored column, a small, explicitly-flagged generalization of the existing `(activity_type, watched_field, watched_value)` shape rather than a new alerting mechanism.
9. On trigger, the alert targets the session's **current Incident Commander** — resolved via ICS Role Assignment where the Muster Session's `anchor_type` is `incident` or `eoc_activation` and the anchor has a current (non-`end`-dated) IC holder — falling back to the standard EOC Coordinator/Safety Coordinator/Site Admin audience (Muster Check-in App's existing conclusion-alert recipients) when no anchor, a non-ICS anchor (`coop_activation`, `compliance_drill`, `none`), or no current IC applies. The platform's usual explicit-beats-default resolution chain, applied to alert routing.

### Active Emergency Event (resolves the platform-wide open question)
10. **Active Emergency Event Source Registration** is a new opt-in registry: `(activity_type, is_active_predicate, covered_location_resolver)` — any Activity type can register as a source of "there is an active emergency here." A source with no resolvable location contributes no coverage (an honest gap, not a platform-wide fallback) rather than being silently treated as covering everywhere.
11. **Four day-one registrants**: Call/Incident (`covered_location` = its own `ActivityLocationAssociation` target, nullable), EOC Activation *(retrofit — gains its own `ActivityLocationAssociation`, seeded from the triggering Incident's location at `Activate EOC Response` time and independently editable thereafter, since a full EOC response commonly covers a broader area than the single point an Incident began at)*, COOP Activation (no resolver registered today — an honest gap, flagged in Open Questions, since COOP doesn't tie to a single location in its current model), and **Muster Session** (`covered_location` = the Location its `site_ref` points to).
12. **A Location L is "under an Active Emergency Event" iff at least one currently-active registered source's `covered_location` is an ancestor-of-or-equal-to L** — reusing Pre-Incident Plans' exact ancestor-or-self containment check, not a new matching algorithm. Because Muster Session's `covered_location` is the site itself, every Location beneath that site counts as covered for the session's duration, giving the same practical breadth a coarser per-site design would have, without a second scoping concept.
13. **Retrofits**: Person Registry's sensitive-field visibility broadening (medical alerts, emergency contacts) now evaluates this concrete signal — for the *viewing* EOC/muster-coordinating user, resolved against the *viewed* Person's own current site (on-duty Post, open Visit, or Employee's assigned site — first match wins) — replacing its previously-unspecified "active emergency" placeholder. Pre-Incident Plans' own per-Location dispatch-context trigger is unchanged in behavior — it's now simply expressed as this registry's Call/Incident registrant rather than a bespoke local check, closing that doc's own flagged open question.

## Data Model / Fields

**Occupancy Reconciliation Source Registration** (platform registry, not tenant-configured)
- source_type, description, resolver_key

**Evacuation Reconciliation Log** (Document extension; entity_id shared PK, FK → Document.entity_id)
- muster_session_ref, generated_at, generated_by (nullable — system on auto-generation)
- entries[] (person_ref, checked_in: bool, muster_check_in_ref (nullable), corroborating_sources[] (source_type, detail))

**Muster Session** *(retrofit — Muster Check-in App)*
- gains computed (non-stored) `all_expected_checked_in`, evaluated live from `expected_occupant_snapshot[]` vs. current Muster Check-in rows for the session

**Active Emergency Event Source Registration** (platform registry, not tenant-configured)
- activity_type, is_active_predicate (field/value the source considers "active"), covered_location_resolver_key

**EOC Activation** *(retrofit)*
- gains `ActivityLocationAssociation` (nullable), seeded from the triggering Incident's location at activation, independently editable

## States & Transitions

- **Evacuation Reconciliation Log:** created once per generation (auto at conclusion, or ad hoc while active) — immutable once generated, the standard Document-snapshot discipline; a later regeneration is a new Document, never an edit to a prior snapshot.
- **Command Post Missing-Occupant Alert (Duration Watchdog instance):** `not_triggered` → `alarming` → `silenced` (auto, the moment `all_expected_checked_in` flips true, or the session concludes; or explicit Acknowledge Alarm) — identical state shape to every other Duration Watchdog instance.
- **Active Emergency Event:** not a stored record with its own lifecycle — a live, computed existence check over currently-active registered sources' status, re-evaluated at query time.

## Integrations

- **Muster Check-in App**: source of Muster Session, Muster Check-in, and the Expected Occupant List snapshot this entire doc reconciles against.
- **GIS & Mapping Services**: source of Position Record for the on-duty-staff secondary confirmation signal (FR #2).
- **Document Registry**: Evacuation Reconciliation Log's hash/version/authorship base.
- **Status & State Monitors / Active Call Alerts & Timers**: source of the generalized Duration Watchdog mechanism this doc's Command Post Missing-Occupant Alert registers a new instance of.
- **ICS Role Mapping & Visual Org Chart**: source of ICS Role Assignment, resolved to find the current Incident Commander for alert targeting (FR #9).
- **Notifications Engine / Real-Time Delivery**: delivery for the Command Post Missing-Occupant Alert and the live Evacuation Dashboard's push updates — no new delivery infrastructure.
- **Person Registry** *(retrofit)*: consumes the Active Emergency Event signal for sensitive-field visibility broadening, replacing its previously-unspecified placeholder.
- **Pre-Incident Plans (Preplans)** *(closes its own flagged open question, no behavior change)*: its existing per-Location dispatch-context trigger is now the Call/Incident registrant of Active Emergency Event Source Registration.
- **Continuity of Operations Plans (COOP)**: COOP Activation is a registered Active Emergency Event source type with no location resolver today — an honest gap (see Open Questions), not a retrofit performed here.
- **Multi-Incident Console / Command Center Wallboard View** *(retrofit)*: Panel Registry gains `evacuation` as a new cross-doc-contributed panel type (after `health`, `org_chart`, `camera`, `alarm_monitor`, `resource_catalog`), rendering the live Evacuation Dashboard (per-Muster-Point and site-wide checked-in percentages) with zero new arrangement/dock mechanism — selectable in a personal Console Layout or a Wallboard Display Profile zone exactly like every prior panel-type contributor.
- **Command/Action Bus**: Generate Reconciliation Log (ad hoc) and Acknowledge Alarm register as actions, consistent with every other invokable action platform-wide.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin | Muster Marshal | Incident Commander | EOC Coordinator/Dispatcher/Safety Coordinator |
|---|---|---|---|---|---|---|
| View Evacuation Dashboard | ✅ | ✅ | ✅ | ✅ (own point only) | ✅ | ✅ |
| Generate/view Evacuation Reconciliation Log | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Receive Command Post Missing-Occupant Alert | — | — | — | ❌ | ✅ (when assigned) | ✅ (fallback) |
| Acknowledge Alarm | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ |
| Register Occupancy Reconciliation Source / Active Emergency Event Source | ✅ (platform-level) | ❌ | ❌ | ❌ | ❌ | ❌ |
| View sensitive Person fields under an Active Emergency Event | ✅ | ✅ | ✅ (own scope) | ❌ | ✅ | ✅ |

## Non-Functional / Constraints

- Evacuation Dashboard percentage updates meet Real-Time Delivery's standard safety-relevant latency target (≤2s server-to-console) — a stale percentage during a live evacuation defeats the purpose, same discipline as Muster Check-in's own live roster.
- The GIS Position Record corroborating signal is explicitly disclosed as supporting evidence only, never sufficient alone to mark a Person checked in — surfaced, never silently substituted for an explicit check-in.
- The Evacuation Reconciliation Log must visibly disclose which sources actually contributed to each entry (never implying Access-Control-swipe confirmation before Module 19's adaptor exists) — the same honest-scope-disclosure discipline Muster Check-in App's Expected Occupant List already established.
- Command Post Missing-Occupant Alert firing, resolving, and its target resolution (IC vs. fallback) are all audit-tier events.
- Active Emergency Event resolution is a live query against currently-active registered sources — never a cached/precomputed flag that could go stale relative to a source Activity actually closing.

## Acceptance Criteria

- [ ] The Evacuation Dashboard shows a live, per-Muster-Point and site-wide checked-in percentage that updates within the standard latency target as new Muster Check-in rows are created.
- [ ] Generating an Evacuation Reconciliation Log while a session is still active succeeds and produces an immutable Document distinct from any log auto-generated later at conclusion.
- [ ] A Reconciliation Log entry for a Person confirmed only by GIS Position Record (no Muster Check-in row) is visibly distinguished from one confirmed by an explicit check-in — never displayed identically.
- [ ] A Muster Session left with `all_expected_checked_in = false` past the configured Duration Watchdog threshold fires the Command Post Missing-Occupant Alert to the session's current Incident Commander when one exists via ICS Role Assignment.
- [ ] The same alert routes to the EOC Coordinator/Safety Coordinator/Site Admin audience when the session has no ICS-compatible anchor or no current IC assigned.
- [ ] The alert auto-silences the moment `all_expected_checked_in` flips true, without requiring explicit acknowledgment.
- [ ] A Location within an active Muster Session's site resolves as "under an Active Emergency Event" via the shared registry, and a Person's medical alerts/emergency contacts become visible to an EOC/muster-coordinating viewer as a result, reverting once the session concludes.
- [ ] An open Call/Incident at a specific Location continues to broaden Preplans' dispatch-context visibility exactly as before this doc, now sourced from the shared Active Emergency Event registry rather than a bespoke check.

## Open Questions

- **COOP Activation has no Active Emergency Event location resolver today** — an honest gap, not resolved here, since COOP doesn't tie to a single Location in its current model; a future COOP retrofit (e.g., resolving to Essential Function's `alternate_site_ref` when set) is plausible but not built now.
- Exact Duration Watchdog threshold default for Command Post Missing-Occupant Alert (a content/tuning concern, not a structural one) — left to technical spec.
- Whether Module 19's future Access-Control swipe registrant, once built, should also feed the live Evacuation Dashboard's percentage (not just the Reconciliation Log) — a plausible enhancement once that source exists, not committed here.
- Whether GIS Position Record's "outside the site geofence" corroboration should extend to non-officer occupants once any future consumer-grade location-sharing capability exists — explicitly out of scope; today's GPS tracking is on-duty-officer-only per GIS & Mapping Services' own established scope.
- Whether the Active Emergency Event registry's per-Location scoping should eventually grow a coarser per-Site convenience query (for consumers that don't need location precision and find the ancestor-walk unnecessary) — not needed by any current consumer, not built now.
