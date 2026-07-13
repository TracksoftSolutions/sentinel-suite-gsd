# Shift Passdowns & Handover Notes

**Module:** 1 Security Operations
**Status:** Draft — elicited, ready for technical spec

## Overview

Shift Passdown reuses DAR's core pattern — a filtered read over Activity Registry, snapshottable into a Document — but with a fundamentally different **inclusion rule**. A DAR pulls in *everything* matching its person/site/time filter. A Passdown pulls in only what's meant to carry forward to the next shift: Activities included **by rule** (a tenant/site-configured "always pass on" policy for certain activity types/categories, e.g. every Incident, or specific Incident categories) or **by explicit flag** (any user with permission on an Activity marking it "add to pass on"). Nothing is passed on by default just because it happened during the shift.

The mechanism doing the marking is a **Pass-On Flag** — a lightweight governed marker on an Activity, structurally the same idea as Entity Registry Core's BOLO Flag (a unary annotation, not a relationship) but scoped specifically to Activities and owned by this feature rather than promoted to the core registry, since nothing else currently needs a cross-entity-type version of it. Unlike a DAR Entry (which has no status of its own), a Pass-On Flag carries its own `open`/`resolved` lifecycle **independent of the underlying Activity's own status** — the same "governance record doesn't mutate the thing it references" discipline DAR's Shift Review already established. An open flag keeps surfacing in every subsequent shift's passdown until someone resolves it, which is what gives passdown items their task-like, carries-forward-until-resolved character.

Passdown's default scope is **site-wide, tied to the site's shift change** (reusing DAR's Team Shift Window mechanism directly — closing a Team Shift Window is what triggers passdown compilation for that site), not a 1:1 person-to-person handoff. An individual flag can optionally narrow itself to a specific **post** (a Location within the site) when it's only relevant there, but that's an exception, not the default shape.

Like DAR, generating a passdown produces an immutable Document snapshot (Document Registry's hash/version model) — but the Passdown Report additionally bakes in shift-roster context (who was on the outgoing shift, who's coming on incoming) sourced from the Team Shift Window's linked Personal Shift Windows, since "who's on shift" is core content of a real handover, not just the flagged items.

Acknowledgment is **configurable per role, and independently per passdown scope** — a tenant can require every incoming Guard to acknowledge a general site-wide passdown, while a narrower shift-level roll-up requires only the incoming Supervisor. This is a first, feature-local instance of a read/acknowledge gate; it's a strong candidate for Digital Acknowledgment Logs (Module 14, not yet specified) to later generalize, same deferred-integration posture DAR took with Shift Window and Post Schedule Builder.

## Actors & Roles

- **Guard** — flags an Activity they have permission on as "add to pass on" (with optional note and post narrowing); resolves flags they're aware of; acknowledges passdowns when their role is required to.
- **Supervisor** — same flag/resolve permissions as a Guard, plus typically the required acknowledger for shift-level passdowns; may configure site-level Pass-On Rules and Acknowledgment Policy within Settings & Preferences' delegated-authority model.
- **Tenant Admin** — sets tenant-default Pass-On Rules and Acknowledgment Policy, may lock them against site-level override.
- **Records Admin** — no distinct role here; Pass-On Flag isn't an Entity Registry Core dedup/merge participant.

## User Stories

- As a **Guard**, I want to flag an unusual observation from my shift as "pass this on," so the next shift knows to keep an eye on it even though it's not a full incident.
- As a **Tenant Admin**, I want every Incident at our sites to automatically show up in the next shift's passdown without anyone remembering to flag it manually.
- As a **Supervisor**, I want an item I flagged three shifts ago and never resolved to still show up today, so nothing quietly falls off a handover chain.
- As a **Supervisor**, I want the passdown to show me who's coming on and who's going off for this shift, not just a list of flagged items, so I know who to brief.
- As an **incoming Supervisor**, I want to be required to read and acknowledge the passdown before I'm considered on duty, while my incoming Guards see the same information without a hard gate.
- As a **Guard**, I want to mark a passdown item resolved once I've handled it, so it stops showing up for every future shift.
- As a **Site Manager**, I want most passdown items scoped to the whole site by default, but the option to flag something as relevant only to a specific post (e.g., a gate malfunction), so post-specific issues don't clutter the general briefing.

## Functional Requirements

### Pass-On Flag
1. **Pass-On Flag** is a lightweight marker referencing a single Activity (any type — DAR Entry, Incident, or any future Activity extension), feature-local to Shift Passdowns (not promoted to Entity Registry Core, unlike BOLO Flag, since no other feature currently needs a cross-entity-type version).
2. A flag is created either **manually** (any user with permission on the referenced Activity selects "add to pass on," optionally adding a short note and/or narrowing scope to a specific post/Location) or **automatically** by a matching **Pass-On Rule** (#6–7).
3. A flag's own lifecycle is `open` → `resolved`, tracked independently of the referenced Activity's own status — resolving a Pass-On Flag never changes the Activity, and the Activity concluding never auto-resolves the flag (left as an explicit open question, #Open Questions, rather than assumed).
4. An open flag appears in every passdown compilation for its site (or narrower post, if scoped) from the shift it was created until it's resolved — this is what gives passdown items their carry-forward behavior.
5. Resolving a flag requires the same permission as editing the Activity it references, plus records who resolved it and when.

### Pass-On Rules (Settings & Preferences-registered)
6. A **Pass-On Rule** declares that Activities of a given `activity_type` (and, where the type supports it, a narrower category — e.g., a specific DAR Entry category, or once Incident Reporting exists, specific Incident categories) at a given site are automatically flagged for pass-on the moment they're created (or reach a configured status).
7. Pass-On Rules register against Settings & Preferences' existing location-chain resolution (Tenant default, overridable per Site unless locked) — no bespoke rule-storage mechanism.

### Passdown scope & compilation
8. Passdown's default scope is **site-wide**, computed against the site's Team Shift Window (per DAR): closing a Team Shift Window computes the current set of open Pass-On Flags for that site (any post) plus any newly-auto-flagged Activities.
9. An individual Pass-On Flag may optionally narrow itself to a specific **post** (a Location within the site) at creation time; a post-scoped flag still surfaces in the site-wide passdown but is visually/structurally distinguished as post-specific.
10. Passdown is **not** modeled as a 1:1 person-to-person handoff — there is no "addressed to" a specific incoming individual, only a site (or post) that whoever comes on next inherits.

### Acknowledgment
11. **Passdown Acknowledgment Policy** (Settings & Preferences-registered, same location-chain resolution as Pass-On Rules) declares, per passdown scope (site-wide daily passdown vs. a specific Team Shift Window's passdown), which roles are **required** to explicitly read and acknowledge before being considered fully on duty, and which roles merely see it surfaced without a hard gate.
12. For a role configured as required on a given scope, acknowledgment is a **hard gate**: that person's Shift Window isn't considered fully started until they acknowledge every open item in scope (mirrors DAR's clock-in surfacing pattern). For roles not configured as required, the same items are surfaced prominently but don't block shift start.
13. Each acknowledgment is individually recorded (who, what was acknowledged, when) — a real accountability record, not a single shift-level checkbox.

### Report generation (Document snapshot)
14. **Generate Passdown Report** produces an immutable Document (Document Registry's hash/version model), including: every in-scope Pass-On Flag (open at generation time) with its referenced Activity's content, and shift-roster context (outgoing/incoming personnel, sourced from the Team Shift Window's linked Personal Shift Windows).
15. Report-generation mode follows the same tenant-configurable pattern DAR established (auto-on-shift-close, ad hoc-only, or both), registered separately under Passdown's own Settings & Preferences key so a tenant can set DAR and Passdown differently.
16. A generated Passdown Report is immutable and reflects flag/roster state at generation time only — resolving a flag afterward doesn't retroactively alter a report already generated.

## Data Model / Fields

**Pass-On Flag**
- flag_id, tenant_id, activity_ref (FK → Activity)
- site_location_ref, post_location_ref (nullable — narrows below site)
- status (open, resolved)
- source (manual, rule), rule_ref (nullable, FK → Pass-On Rule, set only when source = rule)
- note (optional, manual flags)
- created_by (nullable if rule-created), created_at
- resolved_by (nullable), resolved_at (nullable)

**Pass-On Rule** (Settings & Preferences registration)
- rule_id, tenant_id, site_location_ref (nullable = tenant-wide default)
- activity_type, category_filter (nullable)
- enabled (bool)

**Passdown Acknowledgment Policy** (Settings & Preferences registration)
- policy_id, tenant_id, site_location_ref (nullable = tenant-wide default)
- scope (site_daily, shift)
- required_roles[]

**Passdown Acknowledgment**
- ack_id, tenant_id, shift_window_ref (FK → Shift Window, per DAR)
- person_ref (Party), role_at_ack_time
- acknowledged_at

**Passdown Report** (Document extension — Document Registry's base fields apply: hash, version, title, etc.)
- entity_id (PK, FK → Document)
- site_location_ref, source_shift_window_ref
- included_flag_refs[] (snapshot: flag + referenced Activity content, as of generation)
- roster_snapshot[] (person_ref, role, outgoing/incoming)
- is_official
- generated_by, generated_at

## States & Transitions

**Pass-On Flag:** `open` (manual or rule-created) → `resolved` (explicit action). No automatic resolution tied to the referenced Activity's own status (see Open Questions).

**Passdown Acknowledgment:** doesn't exist → `acknowledged` (one-way, per person per shift window's passdown).

**Passdown Report:** created once, immutable — no further states, same as DAR Report.

## Integrations

- **Activity Registry**: Pass-On Flag references any Activity type; Pass-On Rules match on `activity_type`/category.
- **Daily Activity Reports (DAR)**: reuses Shift Window (specifically Team Shift Window) as the trigger for passdown compilation and the source of roster data — no separate shift concept introduced.
- **Document Registry**: Passdown Report is a Document extension, same pattern as DAR Report.
- **Settings & Preferences**: owns Pass-On Rules and Passdown Acknowledgment Policy, both registered against the existing location-chain resolution/locking engine.
- **Notifications Engine**: notifies incoming shift personnel (especially required-acknowledger roles) that a passdown is awaiting acknowledgment.
- **Structured Logging & Audit Trails**: flag creation/resolution, acknowledgment, and report generation are all audit-tier events.
- **Command/Action Bus**: "Add to pass on," "Resolve pass-on item," "Acknowledge passdown," and "Generate passdown report" register as invokable actions across every surface, consistent with the platform's action-registry discipline.
- **Digital Acknowledgment Logs (Module 14, future)**: Passdown Acknowledgment is a strong candidate for that module to later generalize into a platform-wide read/acknowledge mechanism; built feature-local now, deferred integration point, not solved here.
- **Location Registry**: `site_location_ref`/`post_location_ref` reference Locations directly.
- **Incident Reporting & Management (Module 1, upcoming)**: once specified, its Incident Activity extension becomes an immediate, natural target for a default Pass-On Rule, with no Passdown-side change required.

## Permissions

| Action | Guard | Supervisor | Tenant Admin |
|---|---|---|---|
| Create a Pass-On Flag on an Activity | ✅ (if permitted on that Activity) | ✅ (if permitted on that Activity) | ✅ |
| Resolve a Pass-On Flag | ✅ (if permitted on that Activity) | ✅ (if permitted on that Activity) | ✅ |
| Configure Pass-On Rules / Acknowledgment Policy (site level) | ❌ | ✅ (own site, per delegated authority) | ✅ |
| Configure Pass-On Rules / Acknowledgment Policy (tenant default / lock) | ❌ | ❌ | ✅ |
| Acknowledge a passdown | ✅ (if own role is in scope) | ✅ (if own role is in scope) | n/a |
| Generate an ad hoc Passdown Report | ❌ (unless tenant allows) | ✅ | ✅ |

## Non-Functional / Constraints

- Pass-On Flag creation/resolution must work offline, consistent with the platform's general offline model, syncing per the established pattern.
- A required-acknowledger's Shift Window "fully started" gate must fail closed if passdown data can't be fetched (e.g., offline with no cached passdown) — surfaced clearly rather than silently allowing the shift to proceed ungated; exact offline-gate behavior is a technical-spec-level decision.
- Passdown Report immutability enforced at the data layer, identical requirement to DAR Report.
- Acknowledgment records must be individually queryable per person per shift for accountability review, not just aggregated into a single boolean.
- WCAG 2.1 / Section 508 accessible flagging, resolution, acknowledgment, and report views, day one.

## Acceptance Criteria

- [ ] Flagging an Activity "add to pass on" creates an open Pass-On Flag that appears in the next Team Shift Window's passdown compilation for that site.
- [ ] A configured Pass-On Rule (e.g., activity_type = Incident) auto-creates a Pass-On Flag the moment a matching Activity is created, with no manual action.
- [ ] An open Pass-On Flag continues appearing in every subsequent shift's passdown until explicitly resolved, surviving at least two consecutive shift changes in a test scenario.
- [ ] Resolving a Pass-On Flag does not alter the referenced Activity's own status or fields.
- [ ] A post-scoped Pass-On Flag is visually distinguished from a site-wide item in the compiled passdown.
- [ ] With Acknowledgment Policy requiring only the Supervisor role for shift-level passdowns, an incoming Guard's shift starts without a gate while an incoming Supervisor's does not, until they acknowledge.
- [ ] Each acknowledgment is individually recorded and queryable by person and shift window.
- [ ] Generating a Passdown Report produces an immutable Document including both flagged-item content and outgoing/incoming roster data.
- [ ] A Passdown Report generated before a flag is resolved does not retroactively change after that flag is later resolved.
- [ ] Setting a site-level Pass-On Rule or Acknowledgment Policy override correctly narrows a locked tenant-level default only when the tenant default isn't locked; a locked tenant default correctly rejects the site-level override attempt.

## Open Questions

- Whether an open Pass-On Flag should auto-resolve when its referenced Activity reaches a terminal status (e.g., `concluded`) — left unresolved here; current default is fully manual resolution, independent of the Activity's lifecycle.
- Exact default Pass-On Rule set shipped out of the box (which activity types/categories are pre-configured to auto-flag) — pending UX/content design, likely at minimum "Incident."
- Whether a resolved Pass-On Flag should remain visible (struck-through/collapsed) in the current passdown view for continuity/context, or disappear entirely once resolved — a UX-level decision, not resolved here.
- Exact offline behavior when a required acknowledger has no cached passdown data available — flagged as fail-closed in principle (Non-Functional/Constraints) but precise UX deferred to technical spec.
- Whether Passdown Acknowledgment should migrate into a generalized Digital Acknowledgment Logs mechanism once Module 14 is specified, or remain feature-local permanently — deferred until that module exists.
