# Pre-Incident Plans (Preplans)

## Overview

Preplans opens Module 6 (Emergency Planning): a facility's pre-authored, location- and incident-type-specific response guidance, digitized so it can both be authored once and surfaced automatically at the moment it matters. Per explicit user framing: most sites have at least a minimal preplanned response (a fire alarm procedure); some tenants (a national laboratory, this platform's own design-reference client) have extensive ones (a specific plan for a chemical release in a specific, known storage location). The core value isn't the binder — it's **surfacing**: when a matching Call/Incident comes in, the dispatcher sees the location- and incident-type-specific plan, can refer to it, and can launch its response procedure; the same guidance reaches responders' own devices en route, for the safest and most consistent adherence to the plan.

Four elicited decisions:

1. **Preplans owns dispatch-context surfacing generally, not just for this doc's own content.** It becomes the platform's mechanism for pushing building-profile/hazard/shutoff guidance to a responder before/on arrival — fulfilling `mvp.md`'s Module 7 slice ("hazard warnings surfaced as dispatch context") now, with Module 7's future HIRA/NFPA 704 doc feeding richer hazmat-specific data into this same surfacing mechanism later rather than a second one being built. Module 9's future Utility Control Tracking still owns the deep utility-operations directory; this doc only takes the part that's genuinely dispatch-relevant (see #3).
2. **Corrected framing from elicitation: a Preplan is a Document extension with a Location association, not a Location extension.** The initial framing loosely called it a "Location extension," but a Preplan isn't itself a place (unlike Checkpoint/Camera Position/Alarm Zone, which genuinely are) — it's authored guidance *about* a place, structurally identical to Document Registry's own already-forward-referenced "richer Document extension... with a location association" pattern (named there for a future Post Order). Preplan (TPT, IS-A Document) inherits hash/version/authorship for free and gets a `PreplanLocationAssociation` to the Location(s) it addresses — which also cleanly explains multiple Preplans per Location (a Building's general profile, and Lab 311's own chemical-specific plan, coexist rather than competing for one record).
3. **Utility hazard mapping partially closes Location Registry's own flagged deferral.** That doc's light `utility_shutoff_pointers[]` field explicitly deferred "GIS-marked utility control points" to Facility & Zone Management's future Utility Control Tracking. This doc builds that half now (a real geo-tagged **Preplan Utility Point**, rendered as a new UOP Map pin layer) — the "detailed shut-off procedures, operational guides" half stays deferred to Module 9, unchanged.
4. **Sensitivity uses a three-tier ladder that directly reuses Person Registry's already-established emergency-aware-visibility pattern**, narrowed to a per-response trigger: no access by default; a dispatch-context summary auto-broadens to anyone actually dispatched to a matching open Call/Incident at that Location, reverting once it closes; full detail requires a dedicated permission at all times.

## Actors & Roles

- **Site Admin / Facility Coordinator** — authors and maintains Preplans, attachments, Overlay diagrams, and Preplan Utility Points for their scope.
- **Records/Safety Admin** — holds the Preplan Viewer permission for full detail outside an active response; sets review cadence.
- **Dispatcher** — sees matched Preplans surface on an open Call/Incident, browses manually when nothing matches, references/launches a plan's response procedure.
- **Responder (Officer/Guard)** — receives a matched Preplan's dispatch-context summary on their device once dispatched.

## User Stories

- As a **Facility Coordinator**, I want to author a Preplan for a building (or a specific room, like a chemical storage lab) capturing occupancy, hazards, shutoffs, safety structures, and staging areas, with floor plans/binder pages attached, replacing the physical binder with a maintained electronic one.
- As a **Facility Coordinator**, I want to tag a Preplan with the incident type(s) it addresses so the right plan surfaces for the right scenario, not a generic building profile every time.
- As a **Dispatcher**, when a Call/Incident comes in at a Location with a matching Preplan, I want it in front of me automatically so I can refer to it and pass guidance to responding units.
- As a **Dispatcher**, I want to browse a Location's Preplans manually even when nothing auto-matched, for a situation the system didn't tag.
- As a **Responder**, I want a matched Preplan's key hazards/shutoffs to reach my device without having to look them up myself mid-response.
- As a **Records Admin**, I want full Preplan detail (floor plans, hazmat storage locations) restricted to those with a need-to-know, while the safety-relevant summary still reaches any officer actually responding.
- As a **facility engineer**, I want individual utility shutoff points marked on a map so responders can find them without hunting through a document.
- As a **Records Admin**, I want a review-due reminder on each Preplan so plans don't silently go stale.

## Functional Requirements

### Preplan (Document extension)
1. **Preplan** registers as a Document extension (TPT: `entity_id` shared PK, FK → `Document.entity_id`) — inherits `document_title`/`description`/`format`/`file_ref`/`classification`/`current_version`/`version_history[]`/`hash`/`DocumentAuthorAssociation` from Document unmodified, adding: `occupancy_type`, `safety_structures[]` (fixed fire suppression, sprinkler zones, alarm panels, etc.), `staging_areas[]`, `hazard_flags[]` (an interim light tag list — see Open Questions), and the tag fields in #4.
2. **PreplanLocationAssociation** (EntityAssociation; `entity_id_a` = the Preplan, `entity_id_b` = the Location it addresses) ties a Preplan to whichever granularity it actually covers — a Building's general profile, or a specific Room's hazard-specific plan — reusing Location Registry's existing hierarchy rather than requiring a Preplan at every ancestor level. **A Location may have more than one active Preplan** (a general profile plus one or more scenario-specific plans) — deliberate, not a data-quality gap.
3. **PreplanAttachmentAssociation** (EntityAssociation; `entity_id_a` = the Preplan, `entity_id_b` = an attached Document) lets a Preplan reference supporting Documents (floor plan diagrams, scanned binder pages, hazmat data sheets) beyond its own primary versioned content — mirroring `ActivityAttachmentAssociation`'s established multi-document pattern, no new attachment mechanism.
4. `applicable_call_types[]` / `applicable_incident_categories[]` (refs → the existing Call Type Definition / Incident Category Definition registries, established in Call Intake & Logging / Incident Reporting & Management) tag which scenario(s) a Preplan addresses — no new classification registry.

### Dispatch-context surfacing
5. A Call or Incident whose type/category and Location match an active Preplan's tags auto-surfaces that Preplan to the Dispatcher on the Call/Incident console.
6. **When more than one Preplan matches, every match is presented — the platform never silently picks one.** The same discipline Guard Tour established for an ambiguous checkpoint match ("never let the system silently disambiguate against a real-world decision a human actually needs to make") applies here to plan selection.
7. A Dispatcher can **browse a Location's Preplans manually** at any time, regardless of any automatic match — the fallback for a scenario the system didn't tag or catch.
8. The matched Preplan's dispatch-context summary (occupancy, hazard flags, utility points, staging areas — not the attached Documents or Overlay diagram) pushes to responders' devices once dispatched, via the platform's standard delivery.
9. **Reference This Preplan** (Command/Action Bus action, available on an open Call/Incident) explicitly logs which Preplan(s) were actually used during a response — a light, audit-tier record, never auto-tracked on a passive view — flagged as a plausible future data source for after-action review, not built there now.

### Running a matched Preplan
10. A Preplan optionally carries `suggested_checklist_template_ref` (nullable forward-reference) — the actual runnable response procedure (ordered steps, field-personnel task assignment) is owned entirely by **Incident Action Checklists**, the next doc in this module. This doc only reserves the pointer a matched Preplan can pre-suggest; no checklist/task-assignment mechanics are specified here — the same plan/execution seam pattern used repeatedly elsewhere (Key Ring Registry → Lock Core, Access Credential Management → Clearance Profiles).

### Utility control points & GIS hazard mapping
11. **Preplan Utility Point** (child record of a Preplan: `label`, `point_type` [water, gas, electric, sprinkler, other], `geo_point`, `note`) gives real geo-tagged utility control points wherever a Preplan exists for that Location — closing the "GIS-marked utility control points" half of Location Registry's own flagged deferral. Location Registry's existing free-text `utility_shutoff_pointers[]` field is untouched and remains the quick-reference for any Location with no Preplan at all.
12. Preplan Utility Points register as a new **UOP Map pin layer** ("Preplan Utility Points"), following the identical triad-pin-layer convention already used five times (Camera Positions, Alarm Zones, Lock Positions, Gate/Barrier, Intercom Positions) — off by default in both day-one presets; clicking a pin surfaces its label/note.
13. A Preplan may separately attach an annotated facilities diagram via GIS & Mapping Services' existing **Overlay Layer** (a georeferenced upload using its existing `source_format` values — no new format added, no new mapping mechanism) for a whole-diagram view; the pin layer (#12) and an Overlay diagram (#13) are independent and can both be present.

### Sensitivity & access
14. Full Preplan detail (structured fields, attached Documents, Overlay diagrams) is visible only to roles holding a dedicated **Preplan Viewer** permission, layered on top of ordinary Location ABAC — the same "dedicated permission layered on ABAC" discipline already used for UOP Map Viewer and Historical Playback Viewer.
15. **The dispatch-context summary automatically broadens to any officer actually dispatched to a matching open Call/Incident at that Location, regardless of holding Preplan Viewer, for the duration of that response — reverting the moment it closes.** This directly reuses Person Registry's already-established emergency-aware-visibility broadening/reversion shape (that doc's FR #17), narrowed here to a concrete per-response trigger (an open Dispatch/Call/Incident at the matching Location) rather than assuming Person Registry's still-open, platform-wide "active emergency event" concept — flagged as a plausible future reconciliation, not resolved here (see Open Questions).
16. Viewing full Preplan content is itself an audit-tier event, the same sensitivity-logging discipline already applied to Person Registry's sensitive fields and Historical Playback Console sessions.

### Review cadence
17. A tenant-configurable **Preplan Review Reminder** reuses Active Call Alerts & Timers' existing Approaching-Deadline Reminder mechanism (lead-time, before-a-deadline — not Duration Watchdog's after-a-threshold shape) to fire ahead of a Preplan's configured `review_due_date`, so an authored-once plan doesn't silently go stale. Never blocks any operational action, per the platform's standing compliance-never-gates rule.

## Data Model / Fields

**Preplan** (TPT level: `entity_id` is the shared PK, FK → `Document.entity_id`)
- entity_id (PK, FK → Document; document_title/description/format/file_ref/classification/current_version/version_history[]/hash inherited from Document, not redefined here)
- occupancy_type (nullable)
- safety_structures[] (label, structure_type: fire_suppression/sprinkler_zone/alarm_panel/other, note)
- staging_areas[] (label, location_note)
- hazard_flags[] (tag — interim light list, see Open Questions)
- applicable_call_types[] (refs → Call Type Definition)
- applicable_incident_categories[] (refs → Incident Category Definition)
- suggested_checklist_template_ref (nullable, forward-reference — resolved by Incident Action Checklists)
- review_due_date (nullable)

*(Author and attached Location/Documents are association rows, not fields here.)*

**PreplanLocationAssociation** (TPT level: `association_id` shared PK, FK → `EntityAssociation.association_id`)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Preplan, entity_id_b = the Location)
- no extra fields beyond the base EntityAssociation shape

**PreplanAttachmentAssociation** (TPT level, same shape)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Preplan, entity_id_b = an attached Document)
- no extra fields beyond the base EntityAssociation shape

**Preplan Utility Point** (owned child record of a Preplan — same weight as a Checklist Template's `items[]`, not its own Entity/TPT level)
- point_id, preplan_ref, label, point_type (water, gas, electric, sprinkler, other), geo_point (lat/long), note

**Preplan Reference Log** (local record, one per Reference This Preplan action)
- reference_id, preplan_ref, activity_ref (the Call/Incident referenced from), referenced_by, referenced_at

## States & Transitions

- **Preplan:** inherits Document's `active` → `tombstoned` (merged away) → `active` (merge reversed) lifecycle unmodified; independent content versioning proceeds via Document's own `version_history[]` regardless of that state.
- **PreplanLocationAssociation, PreplanAttachmentAssociation:** follow Entity Registry Core's shared EntityAssociation lifecycle (`active` → `removed`, a new `active` row on reassignment).
- **Preplan Utility Point:** created/edited/removed directly, no independent lifecycle beyond its owning Preplan.
- **Preplan Reference Log:** created once per reference action, immutable, append-only.

## Integrations

- **Document Registry**: Preplan's TPT base — hash/version/authorship mechanism; `PreplanAttachmentAssociation`'s target type.
- **Location Registry**: source of the Location(s) a `PreplanLocationAssociation` points at; this doc partially closes that feature's own flagged "GIS-marked utility control points" deferral (Preplan Utility Point), leaving the "detailed shut-off procedures, operational guides" half to Facility & Zone Management's future Utility Control Tracking, unchanged.
- **Call Intake & Logging / Incident Reporting & Management**: source of the Call Type Definition / Incident Category Definition registries Preplan tags against for matching; source of the Call/Incident records that trigger surfacing.
- **GIS & Mapping Services**: Overlay Layer for uploaded/georeferenced diagrams (existing `source_format` values reused, no retrofit needed); **Unified Operational Picture (UOP) Map**: gains a new Preplan Utility Points pin layer *(retrofit)*, off by default alongside Camera Positions/Alarm Zones/Lock Positions/Gate-Barrier/Intercom.
- **Person Registry**: source of the emergency-aware visibility pattern this doc's dispatch-context-summary broadening directly reuses; this doc's per-response trigger is flagged as a plausible future reconciliation point once Emergency Planning's Muster Check-in App defines a platform-wide "active emergency event" signal, not assumed here.
- **Active Call Alerts & Timers**: source of the Approaching-Deadline Reminder mechanism reused for Preplan Review Reminder.
- **Command/Action Bus**: Browse Preplan, Reference This Preplan register as actions.
- **Incident Action Checklists** (next doc, forward reference only): owns the actual runnable checklist/task-assignment mechanism `suggested_checklist_template_ref` points toward — no mechanics specified here.
- **`mvp.md`'s Module 7 hazard-by-location slice / a future HIRA doc**: forward reference — richer hazmat/NFPA 704 data becomes a future feed into `hazard_flags[]` and dispatch-context surfacing, not built here.
- **Structured Logging & Audit Trails**: full Preplan detail views and every Reference This Preplan action are audit-tier.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin/Facility Coordinator | Preplan Viewer (Supervisor+/hazmat-cleared) | Dispatcher | Dispatched responder |
|---|---|---|---|---|---|---|
| Author/edit a Preplan, attachments, Overlay diagram | ✅ | ✅ | ✅ (own scope) | ❌ (unless also granted) | ❌ | ❌ |
| View full Preplan detail (normal ops) | ✅ | ✅ | ✅ (own scope) | ✅ | ❌ (unless also granted) | ❌ (unless also granted) |
| View dispatch-context summary (active matching Call/Incident) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (broadened, reverts on close) |
| Browse/select a Preplan on an open Call/Incident; Reference This Preplan | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Set review-due date / Review Reminder cadence | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ | ❌ |

## Non-Functional / Constraints

- Dispatch-context summary broadening/reversion must meet Real-Time Delivery's standard safety-relevant push latency target (≤5s server-to-device) — a delayed hazard warning defeats the purpose.
- Preplan matching (Call/Incident type + Location) degrades gracefully to manual browse when no match exists or a match is ambiguous — never silently guesses.
- Overlay diagram uploads reuse GIS & Mapping Services' existing validation pipeline unmodified — no separate validation logic.
- Full Preplan content views are audit-tier per Structured Logging's sensitivity classification, consistent with comparably sensitive record types (Person Registry's sensitive fields, Historical Playback Console sessions).
- WCAG 2.1 / Section 508 accessible authoring flows and dispatch-surfaced summary views, day one.

## Acceptance Criteria

- [ ] Authoring a Preplan against Lab 311 (a Room-level Location) tagged `applicable_incident_categories[] = [chemical_release]`, then logging a matching Call at that exact Location, auto-surfaces the Preplan to the Dispatcher.
- [ ] A Building's general Preplan and Lab 311's chemical-specific Preplan both surface — never silently narrowed to one — when a Call at Lab 311 could plausibly match either.
- [ ] A Dispatcher can browse a Location's Preplans manually even when no automatic match occurred.
- [ ] A responder dispatched to a matching Call/Incident receives the dispatch-context summary on their device without holding Preplan Viewer; that access reverts once the Call/Incident closes.
- [ ] A user without Preplan Viewer and with no open matching Call/Incident cannot view a Preplan's full structured detail or attached Documents.
- [ ] A Preplan Utility Point renders as a pin on UOP Map's Preplan Utility Points layer, off by default, and its label/note display on click.
- [ ] An uploaded Overlay Layer diagram for a Preplan validates through GIS & Mapping Services' existing pipeline with no separate code path.
- [ ] A Preplan approaching its configured review-due date fires a lead-time Approaching-Deadline Reminder and never blocks any operational action.
- [ ] Reference This Preplan on an open Incident creates an immutable Preplan Reference Log row, queryable later.

## Open Questions

- Exact `hazard_flags[]` starter taxonomy — an interim light tag list; Module 7's future HIRA/NFPA 704 doc is expected to feed richer hazmat-specific data into this same surfacing mechanism (per this doc's own scope-boundary decision), not to be built here.
- Whether this doc's emergency-aware visibility trigger (an open Dispatch/Call/Incident at the matching Location) should later unify with Person Registry's still-open, platform-wide "active emergency event" concept once Muster Check-in App specifies it — flagged, not resolved.
- `suggested_checklist_template_ref`'s actual resolution/runtime behavior belongs entirely to Incident Action Checklists (next doc) — this doc only reserves the pointer.
- Whether ancestor-Location rollup (a Room-level Call matching only a Building-level Preplan one level up) should surface automatically or require explicit inclusion — echoing Clearance Profiles' `include_descendants`-defaults-false discipline — left open pending real hierarchy-matching UX, informed by Module 9's future Location Hierarchy Designer.
- Whether Location Registry's existing `utility_shutoff_pointers[]` should eventually retrofit to reference Preplan Utility Point rather than staying independent free text — left alone here since a Location with no Preplan at all still needs its own light quick-reference field.
