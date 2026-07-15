# Pre-Incident Plans (Preplans)

## Overview

Preplans opens Module 6 (Emergency Planning): a facility's pre-authored, location- and incident-type-specific response guidance, digitized so it can both be authored once and surfaced automatically at the moment it matters. Per explicit user framing: most sites have at least a minimal preplanned response (a fire alarm procedure); some tenants (a national laboratory, this platform's own design-reference client) have extensive ones (a specific plan for a chemical release in a specific, known storage location). The core value isn't the binder — it's **surfacing**: when a matching Call/Incident comes in, the dispatcher sees the single most relevant plan for that exact type-and-place combination, can refer to it, and can launch its response procedure and notification list; the same guidance reaches responders' own devices en route, for the safest and most consistent adherence to the plan.

Seven elicited decisions — four in the initial round, three added in a follow-up round after real gaps surfaced against the drafted doc:

1. **Preplans owns dispatch-context surfacing generally, not just for this doc's own content.** It becomes the platform's mechanism for pushing building-profile/hazard/shutoff guidance to a responder before/on arrival — fulfilling `mvp.md`'s Module 7 slice ("hazard warnings surfaced as dispatch context") now, with Module 7's future HIRA/NFPA 704 doc feeding richer hazmat-specific data into this same surfacing mechanism later rather than a second one being built. Module 9's future Utility Control Tracking still owns the deep utility-operations directory; this doc only takes the part that's genuinely dispatch-relevant (see #3).
2. **Corrected framing from elicitation: a Preplan is a Document extension, not a Location extension.** The initial framing loosely called it a "Location extension," but a Preplan isn't itself a place (unlike Checkpoint/Camera Position/Alarm Zone, which genuinely are) — it's authored guidance *about* a place, structurally identical to Document Registry's own already-forward-referenced "richer Document extension... with a location association" pattern (named there for a future Post Order). Preplan (TPT, IS-A Document) inherits hash/version/authorship for free; its location tie now lives in **Preplan Scope** (#5), not a standalone association.
3. **Utility hazard mapping partially closes Location Registry's own flagged deferral.** That doc's light `utility_shutoff_pointers[]` field explicitly deferred "GIS-marked utility control points" to Facility & Zone Management's future Utility Control Tracking. This doc builds that half now (a real geo-tagged **Preplan Utility Point**, rendered as a new UOP Map pin layer) — the "detailed shut-off procedures, operational guides" half stays deferred to Module 9, unchanged.
4. **Sensitivity uses a three-tier ladder that directly reuses Person Registry's already-established emergency-aware-visibility pattern**, narrowed to a per-response trigger: no access by default; a dispatch-context summary auto-broadens to anyone actually dispatched to a matching open Call/Incident at that Location, reverting once it closes; full detail requires a dedicated permission at all times.
5. **Follow-up correction: matching is most-specific-wins across two independent hierarchies, not "surface every match and let the dispatcher filter."** Per explicit user direction, an emergency plan is fundamentally an association between an incident type and a location, **both of which are hierarchies** — a plan can be authored broadly ("Fire" at "Main Campus") or narrowly ("Grease Fire," a subtype of Fire, at "Cafeteria," nested under that same campus), and the more specific plan wins automatically, removing the need for a human to filter between overlapping matches. This retires the original design's `PreplanLocationAssociation` and flat `applicable_call_types[]`/`applicable_incident_categories[]` arrays in favor of **Preplan Scope** below, and required retrofitting Call Type Definition and Incident Category Definition with a `parent_*_ref` self-hierarchy — incident-type classification had no hierarchy at all before this doc needed one.
6. **Follow-up: a Preplan suggests an ordered list of checklists, not a single one, and the checklist↔full-procedure link is made explicit.** A plan may reasonably need several distinct checklists (e.g., a building evacuation checklist and a chemical spill response checklist), each an abbreviated, actionable version of guidance the Preplan's own full content covers in detail — this doc widens the original singular `suggested_checklist_template_ref` to an ordered `suggested_checklist_refs[]` and adds an explicit contract (not a build) that Incident Action Checklists' own Checklist Template must be able to reference back to that fuller procedure.
7. **Follow-up: Emergency Notification List, a new ordered "call these people in this order" mechanism, deliberately kept separate from EOC Activation Checklists' EOC Call-up Roster but interoperable with it.** Per explicit user direction, the two are structurally similar (both tenant-configured Person/ICS-Position contact lists) but differ in delivery semantics — EOC Call-up Roster is a simultaneous broadcast, this is a strictly ordered sequence — so they stay two mechanisms, each able to **Import Entries** from the other rather than requiring re-entry of an overlapping contact set. Automated sequential escalation (vs. a human manually working the list) is supported, but only once a real voice-call channel exists — per explicit user direction, "typically for emergencies an SMS is not sufficient" — which required retrofitting Notifications Engine with a new voice-call channel and reusing its existing Critical-tier `escalation_chain[]`/acknowledgment machinery directly rather than building a parallel automation engine.

## Actors & Roles

- **Site Admin / Facility Coordinator** — authors and maintains Preplans, Scopes, checklists, notification lists, attachments, Overlay diagrams, and Preplan Utility Points for their scope.
- **Records/Safety Admin** — holds the Preplan Viewer permission for full detail outside an active response; sets review cadence.
- **Dispatcher** — sees the single most-specific matching Preplan surface on an open Call/Incident, browses manually when nothing matches, references/launches a plan's checklists and notification list.
- **Responder (Officer/Guard)** — receives a matched Preplan's dispatch-context summary on their device once dispatched.

## User Stories

- As a **Facility Coordinator**, I want to author a Preplan for a building (or a specific room, like a chemical storage lab) capturing occupancy, hazards, shutoffs, safety structures, and staging areas, with floor plans/binder pages attached, replacing the physical binder with a maintained electronic one.
- As a **Facility Coordinator**, I want to scope a Preplan to a specific incident type and location — broadly ("Fire" campus-wide) or narrowly ("Grease Fire" in the cafeteria) — and trust that the more specific plan automatically wins when both could apply, without building filtering logic myself.
- As a **Dispatcher**, when a Call/Incident comes in, I want the single right plan for that exact type and place in front of me automatically, not a list I have to sort through.
- As a **Dispatcher**, I want to browse a Location's Preplans manually even when nothing auto-matched, for a situation the system didn't tag.
- As a **Facility Coordinator**, I want to attach several checklists to a plan (evacuation, spill response) and reorder them, since a single scenario often needs more than one.
- As a **Dispatcher**, I want a plan's emergency notification list ready to work the moment it surfaces — an ordered list of who to call, with the option to let the system place calls automatically if we have that capability.
- As a **Responder**, I want a matched Preplan's key hazards/shutoffs to reach my device without having to look them up myself mid-response.
- As a **Records Admin**, I want full Preplan detail (floor plans, hazmat storage locations) restricted to those with a need-to-know, while the safety-relevant summary still reaches any officer actually responding.
- As a **facility engineer**, I want individual utility shutoff points marked on a map so responders can find them without hunting through a document.
- As a **Records Admin**, I want a review-due reminder on each Preplan so plans don't silently go stale.

## Functional Requirements

### Preplan (Document extension)
1. **Preplan** registers as a Document extension (TPT: `entity_id` shared PK, FK → `Document.entity_id`) — inherits `document_title`/`description`/`format`/`file_ref`/`classification`/`current_version`/`version_history[]`/`hash`/`DocumentAuthorAssociation` from Document unmodified, adding: `occupancy_type`, `safety_structures[]` (fixed fire suppression, sprinkler zones, alarm panels, etc.), `staging_areas[]`, `hazard_flags[]` (an interim light tag list — see Open Questions), and `review_due_date`.
2. **PreplanAttachmentAssociation** (EntityAssociation; `entity_id_a` = the Preplan, `entity_id_b` = an attached Document) lets a Preplan reference supporting Documents (floor plan diagrams, scanned binder pages, hazmat data sheets) beyond its own primary versioned content — mirroring `ActivityAttachmentAssociation`'s established multi-document pattern, no new attachment mechanism.

### Preplan Scope (the matching key — incident type × location, both hierarchical)
3. **Preplan Scope** (child record of a Preplan) pairs exactly one location with exactly one incident-type reference: `location_ref` (FK → Location, any granularity — Campus, Building, Cafeteria) plus **exactly one** of `call_type_ref` (FK → Call Type Definition) or `incident_category_ref` (FK → Incident Category Definition), the same "exactly one set per entry" convention already used by EOC Call-up Roster's entries. **A Preplan may carry more than one Scope, and a Location may be covered by more than one Preplan** — a Building's general Fire Preplan and Lab 311's own Chemical-Release Preplan coexist rather than competing for one record.
4. **Call Type Definition and Incident Category Definition each gain a `parent_*_ref`** (retrofit, see the two module docs) — a lightweight self-hierarchy (e.g., "Grease Fire"'s parent is "Fire") distinct from, and purpose-built for, this doc's matching need. This is the incident-type equivalent of Location's existing `HierarchyAssociation` chain.

### Dispatch-context surfacing — most-specific match wins
5. When a Call or Incident is created/updated with a concrete `location_ref` and `call_type`/`category`, the platform builds two ancestor-inclusive-of-self candidate chains: the location's own chain via `HierarchyAssociation` (self, parent, grandparent, ...) and the type's own chain via `parent_call_type_ref`/`parent_category_ref` (self, parent, grandparent, ...). A Preplan Scope **matches** when both its `location_ref` and its type ref appear in the respective chain (a `call_type_ref` Scope only matches a Call; an `incident_category_ref` Scope only matches an Incident).
6. **Each matching Scope's specificity is its total hop count from the leaf on both axes combined; the lowest-total-hops Scope(s) win and their Preplan(s) alone auto-surface** — a grease fire in the cafeteria resolves straight to the cafeteria's grease-fire-specific plan, never also showing the campus's general fire plan alongside it. **A genuine tie** (multiple Scopes at the identical lowest total, e.g., one Scope more specific on location and another more specific on type) is the one case that still presents every tied candidate for a human pick — the exception, not the default; per-axis weighting is a tuning parameter, not committed here (see Open Questions).
7. A Dispatcher can **browse a Location's Preplans manually** at any time, regardless of any automatic match — the fallback for a scenario the system didn't scope.
8. The matched Preplan's dispatch-context summary (occupancy, hazard flags, utility points, staging areas — not the attached Documents or Overlay diagram) pushes to responders' devices once dispatched, via the platform's standard delivery.
9. **Reference This Preplan** (Command/Action Bus action, available on an open Call/Incident) explicitly logs which Preplan(s) were actually used during a response — a light, audit-tier record, never auto-tracked on a passive view — flagged as a plausible future data source for after-action review, not built there now.

### Checklists — ordered, plural, linked back to the full procedure
10. A Preplan carries `suggested_checklist_refs[]` (ordered, each: `sequence_number`, `checklist_template_ref`, `label`) — a plan may reasonably suggest several distinct checklists (e.g., evacuation, then spill response), each a forward-reference to a Checklist Template owned entirely by **Incident Action Checklists**, the next doc in this module. No checklist/task-assignment mechanics are specified here — the same plan/execution seam pattern used repeatedly elsewhere (Key Ring Registry → Lock Core, Access Credential Management → Clearance Profiles).
11. **Contract on the next doc**: a Checklist Template suggested by a Preplan must be able to reference back to that Preplan's own full procedure content (the Preplan itself, or a specific `PreplanAttachmentAssociation`'d Document) so an abbreviated checklist step can link out to fuller detail — specified here as a requirement Incident Action Checklists must satisfy, not built in this doc.

### Emergency Notification List
12. A Preplan carries zero or more **Preplan Notification Lists** — an ordered set of contacts (`entries[]`: `sequence_number`, `person_ref` [nullable], `ics_position_ref` [nullable], exactly one set per entry, the same shape as EOC Call-up Roster's entries) representing "call these people in this order."
13. `notification_mode` is `manual_sequential` (default) or `automated_escalation`. **`automated_escalation` is only selectable when the tenant has an eligible voice-call channel configured** *(retrofit, see Notifications Engine)* — SMS/email/push alone don't satisfy a real emergency call-down; a tenant without that channel is limited to `manual_sequential` and the option isn't offered, never silently claiming automation it can't perform.
14. **Automated escalation reuses Notifications Engine's existing Critical-tier `escalation_chain[]`/acknowledgment/timeout machinery directly, resolving the list's ordered entries into that chain** — no parallel automation engine is built here. Initiating the list in this mode creates a Critical-tier Notification Instance whose channel set includes voice call.
15. **Manual mode is a human-worked ordered checklist** — a Dispatcher (or whoever initiates it) marks each entry `attempted`/`reached`/`no_response`/`skipped` at their own pace; the system tracks status, it doesn't drive the calling.
16. **Initiate Notification List** (Command/Action Bus action, available on an open Call/Incident with a matched Preplan) creates a **Notification List Run** — `notification_instance_ref` (nullable, set only for `automated_escalation`, delegating status to Notifications Engine rather than duplicating it) or `entry_statuses[]` (maintained directly for `manual_sequential`).
17. **Import Entries**: a Preplan Notification List can be seeded from an existing EOC Call-up Roster, and vice versa *(retrofit, see EOC Activation Checklists)* — a one-time copy of `person_ref`/`ics_position_ref` rows, never a live shared list, avoiding re-entry of an overlapping contact set without merging two mechanisms with genuinely different delivery semantics (simultaneous broadcast vs. strict sequence).

### Utility control points & GIS hazard mapping
18. **Preplan Utility Point** (child record of a Preplan: `label`, `point_type` [water, gas, electric, sprinkler, other], `geo_point`, `note`) gives real geo-tagged utility control points wherever a Preplan exists for that Location — closing the "GIS-marked utility control points" half of Location Registry's own flagged deferral. Location Registry's existing free-text `utility_shutoff_pointers[]` field is untouched and remains the quick-reference for any Location with no Preplan at all.
19. Preplan Utility Points register as a new **UOP Map pin layer** ("Preplan Utility Points"), following the identical triad-pin-layer convention already used five times (Camera Positions, Alarm Zones, Lock Positions, Gate/Barrier, Intercom Positions) — off by default in both day-one presets; clicking a pin surfaces its label/note.
20. A Preplan may separately attach an annotated facilities diagram via GIS & Mapping Services' existing **Overlay Layer** (a georeferenced upload using its existing `source_format` values — no new format added, no new mapping mechanism) for a whole-diagram view; the pin layer (#19) and an Overlay diagram (#20) are independent and can both be present.

### Sensitivity & access
21. Full Preplan detail (structured fields, Scopes, checklist references, notification lists, attached Documents, Overlay diagrams) is visible only to roles holding a dedicated **Preplan Viewer** permission, layered on top of ordinary Location ABAC — the same "dedicated permission layered on ABAC" discipline already used for UOP Map Viewer and Historical Playback Viewer.
22. **The dispatch-context summary automatically broadens to any officer actually dispatched to a matching open Call/Incident at that Location, regardless of holding Preplan Viewer, for the duration of that response — reverting the moment it closes.** This directly reuses Person Registry's already-established emergency-aware-visibility broadening/reversion shape (that doc's FR #17), narrowed here to a concrete per-response trigger (an open Dispatch/Call/Incident at the matching Location) rather than assuming Person Registry's still-open, platform-wide "active emergency event" concept — flagged as a plausible future reconciliation, not resolved here (see Open Questions).
23. Viewing full Preplan content is itself an audit-tier event, the same sensitivity-logging discipline already applied to Person Registry's sensitive fields and Historical Playback Console sessions.

### Review cadence
24. A tenant-configurable **Preplan Review Reminder** reuses Active Call Alerts & Timers' existing Approaching-Deadline Reminder mechanism (lead-time, before-a-deadline — not Duration Watchdog's after-a-threshold shape) to fire ahead of a Preplan's configured `review_due_date`, so an authored-once plan doesn't silently go stale. Never blocks any operational action, per the platform's standing compliance-never-gates rule.

## Data Model / Fields

**Preplan** (TPT level: `entity_id` is the shared PK, FK → `Document.entity_id`)
- entity_id (PK, FK → Document; document_title/description/format/file_ref/classification/current_version/version_history[]/hash inherited from Document, not redefined here)
- occupancy_type (nullable)
- safety_structures[] (label, structure_type: fire_suppression/sprinkler_zone/alarm_panel/other, note)
- staging_areas[] (label, location_note)
- hazard_flags[] (tag — interim light list, see Open Questions)
- suggested_checklist_refs[] (sequence_number, checklist_template_ref, label — forward-reference, resolved by Incident Action Checklists)
- review_due_date (nullable)

*(Author, attached Documents, Scopes, and Notification Lists are association/child rows, not fields here.)*

**PreplanAttachmentAssociation** (TPT level: `association_id` shared PK, FK → `EntityAssociation.association_id`)
- association_id (PK, FK → EntityAssociation; entity_id_a = the Preplan, entity_id_b = an attached Document)
- no extra fields beyond the base EntityAssociation shape

**Preplan Scope** (owned child record of a Preplan — local, not an EntityAssociation, since a Call Type/Incident Category Definition is a lightweight Settings-registered lookup, not an Entity)
- scope_id, preplan_ref
- location_ref (FK → Location entity_id)
- call_type_ref (nullable, FK → Call Type Definition) / incident_category_ref (nullable, FK → Incident Category Definition) — exactly one set per Scope

**Preplan Utility Point** (owned child record of a Preplan — same weight as a Checklist Template's `items[]`, not its own Entity/TPT level)
- point_id, preplan_ref, label, point_type (water, gas, electric, sprinkler, other), geo_point (lat/long), note

**Preplan Notification List** (owned child record of a Preplan — local, same weight as EOC Call-up Roster)
- list_id, preplan_ref, name
- notification_mode (manual_sequential, automated_escalation — the latter only selectable when the tenant has an eligible voice-call channel)
- entries[] (entry_id, sequence_number, person_ref [nullable], ics_position_ref [nullable] — exactly one set per entry)

**Notification List Run** (local record, one per Initiate Notification List action)
- run_id, list_ref, activity_ref (the Call/Incident it was initiated from), initiated_by, initiated_at, mode_used
- notification_instance_ref (nullable, FK → Notifications Engine's Notification Instance — set only when mode_used = automated_escalation; status/ack/escalation state is read from there, never duplicated)
- entry_statuses[] (entry_ref, status: pending/attempted/reached/no_response/skipped, contacted_at, contacted_by [nullable for automated]) — authoritative for manual_sequential; a display-only mirror of Notifications Engine's own state for automated_escalation

**Preplan Reference Log** (local record, one per Reference This Preplan action)
- reference_id, preplan_ref, activity_ref (the Call/Incident referenced from), referenced_by, referenced_at

## States & Transitions

- **Preplan:** inherits Document's `active` → `tombstoned` (merged away) → `active` (merge reversed) lifecycle unmodified; independent content versioning proceeds via Document's own `version_history[]` regardless of that state.
- **PreplanAttachmentAssociation:** follows Entity Registry Core's shared EntityAssociation lifecycle (`active` → `removed`, a new `active` row on reassignment).
- **Preplan Scope, Preplan Utility Point, Preplan Notification List:** created/edited/removed directly as part of editing the owning Preplan, no independent lifecycle.
- **Notification List Run:** `in_progress` → `completed` (explicit action, mirrors Checklist Run's "completion is never gated on 100% coverage" posture) — for `automated_escalation` mode, mirrors Notifications Engine's own Notification Instance state (`created` → `dispatching` → `ack_pending` → `acknowledged`/`escalated` → `resolved`) rather than tracking parallel state.
- **Preplan Reference Log:** created once per reference action, immutable, append-only.

## Integrations

- **Document Registry**: Preplan's TPT base — hash/version/authorship mechanism; `PreplanAttachmentAssociation`'s target type.
- **Location Registry**: source of the Location(s) a Preplan Scope's `location_ref` points at and its `HierarchyAssociation` chain, walked for location-axis specificity; this doc partially closes that feature's own flagged "GIS-marked utility control points" deferral (Preplan Utility Point), leaving the "detailed shut-off procedures, operational guides" half to Facility & Zone Management's future Utility Control Tracking, unchanged.
- **Call Intake & Logging / Incident Reporting & Management** *(retrofit)*: source of the Call Type Definition / Incident Category Definition registries a Preplan Scope references, each now carrying a `parent_*_ref` self-hierarchy this doc's specificity matching walks; source of the Call/Incident records that trigger surfacing.
- **GIS & Mapping Services**: Overlay Layer for uploaded/georeferenced diagrams (existing `source_format` values reused, no retrofit needed); **Unified Operational Picture (UOP) Map**: gains a new Preplan Utility Points pin layer *(retrofit)*, off by default alongside Camera Positions/Alarm Zones/Lock Positions/Gate-Barrier/Intercom.
- **Person Registry**: source of the emergency-aware visibility pattern this doc's dispatch-context-summary broadening directly reuses; this doc's per-response trigger is flagged as a plausible future reconciliation point once Emergency Planning's Muster Check-in App defines a platform-wide "active emergency event" signal, not assumed here.
- **Notifications Engine** *(retrofit)*: source of the new voice-call channel gating `automated_escalation` mode, and of the existing Critical-tier `escalation_chain[]`/acknowledgment/timeout machinery Preplan Notification List's automated mode reuses directly rather than building a parallel engine.
- **EOC Activation Checklists** *(retrofit)*: source of the structurally similar EOC Call-up Roster; Import Entries lets either mechanism seed from the other without merging them.
- **Active Call Alerts & Timers**: source of the Approaching-Deadline Reminder mechanism reused for Preplan Review Reminder.
- **Command/Action Bus**: Browse Preplan, Reference This Preplan, Initiate Notification List, Import Entries register as actions.
- **Incident Action Checklists** (next doc, forward reference only): owns the actual runnable checklist/task-assignment mechanism `suggested_checklist_refs[]` points toward, and must satisfy the full-procedure back-reference contract (FR #11) — no mechanics specified here.
- **`mvp.md`'s Module 7 hazard-by-location slice / a future HIRA doc**: forward reference — richer hazmat/NFPA 704 data becomes a future feed into `hazard_flags[]` and dispatch-context surfacing, not built here.
- **Structured Logging & Audit Trails**: full Preplan detail views and every Reference This Preplan / Initiate Notification List action are audit-tier.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin/Facility Coordinator | Preplan Viewer (Supervisor+/hazmat-cleared) | Dispatcher | Dispatched responder |
|---|---|---|---|---|---|---|
| Author/edit a Preplan, Scopes, checklists, notification lists, attachments, Overlay diagram | ✅ | ✅ | ✅ (own scope) | ❌ (unless also granted) | ❌ | ❌ |
| View full Preplan detail (normal ops) | ✅ | ✅ | ✅ (own scope) | ✅ | ❌ (unless also granted) | ❌ (unless also granted) |
| View dispatch-context summary (active matching Call/Incident) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (broadened, reverts on close) |
| Browse/select a Preplan on an open Call/Incident; Reference This Preplan | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Initiate Notification List (manual or automated) | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ |
| Import Entries between Preplan Notification List and EOC Call-up Roster | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ | ❌ |
| Set review-due date / Review Reminder cadence | ✅ | ✅ (own tenant) | ✅ (own scope) | ❌ | ❌ | ❌ |

## Non-Functional / Constraints

- Dispatch-context summary broadening/reversion must meet Real-Time Delivery's standard safety-relevant push latency target (≤5s server-to-device) — a delayed hazard warning defeats the purpose.
- Preplan matching (Call/Incident type + Location) resolves to the single most-specific Scope by construction; it degrades gracefully to manual browse when no Scope matches, and to presenting every tied candidate only on a genuine specificity tie — never silently guesses in either case.
- `automated_escalation` mode must never be offered or silently attempted when the tenant has no eligible voice-call channel configured — degrades to `manual_sequential`, consistent with the platform's "never silently claim automation that can't actually happen" discipline (mirrors Access Credential Management's `pending_provisioning_confirmation` honesty).
- Overlay diagram uploads reuse GIS & Mapping Services' existing validation pipeline unmodified — no separate validation logic.
- Full Preplan content views are audit-tier per Structured Logging's sensitivity classification, consistent with comparably sensitive record types (Person Registry's sensitive fields, Historical Playback Console sessions).
- WCAG 2.1 / Section 508 accessible authoring flows and dispatch-surfaced summary views, day one.

## Acceptance Criteria

- [ ] A Building-level Preplan Scoped to (Fire, Main Campus) and a Room-level Preplan Scoped to (Grease Fire, Cafeteria) both exist; a Call logged as Grease Fire at the Cafeteria auto-surfaces only the Cafeteria plan, never both.
- [ ] The same Cafeteria Call, if logged instead as a generic Fire (not Grease Fire), auto-surfaces the Building's general Fire plan instead, since the Cafeteria has no Scope matching plain Fire.
- [ ] A genuine specificity tie (two Scopes with equal total hop count) presents both candidates rather than silently picking one.
- [ ] A Dispatcher can browse a Location's Preplans manually even when no automatic match occurred.
- [ ] A responder dispatched to a matching Call/Incident receives the dispatch-context summary on their device without holding Preplan Viewer; that access reverts once the Call/Incident closes.
- [ ] A user without Preplan Viewer and with no open matching Call/Incident cannot view a Preplan's full structured detail, Scopes, checklist references, notification lists, or attached Documents.
- [ ] A Preplan's `suggested_checklist_refs[]` display in the authored order, each independently labeled.
- [ ] A Preplan Notification List with no voice-call channel configured for the tenant cannot be set to `automated_escalation` — the option is unavailable, not merely rejected at save time.
- [ ] Initiating a Preplan Notification List in `automated_escalation` mode creates a Critical-tier Notification Instance whose escalation chain matches the list's ordered entries, and its acknowledgment/escalation state is visible from the resulting Notification List Run without duplication.
- [ ] Importing entries from an EOC Call-up Roster into a new Preplan Notification List correctly copies each `person_ref`/`ics_position_ref` row, and a later edit to the source roster does not retroactively change the imported list.
- [ ] A Preplan Utility Point renders as a pin on UOP Map's Preplan Utility Points layer, off by default, and its label/note display on click.
- [ ] An uploaded Overlay Layer diagram for a Preplan validates through GIS & Mapping Services' existing pipeline with no separate code path.
- [ ] A Preplan approaching its configured review-due date fires a lead-time Approaching-Deadline Reminder and never blocks any operational action.
- [ ] Reference This Preplan on an open Incident creates an immutable Preplan Reference Log row, queryable later.

## Open Questions

- Exact `hazard_flags[]` starter taxonomy — an interim light tag list; Module 7's future HIRA/NFPA 704 doc is expected to feed richer hazmat-specific data into this same surfacing mechanism (per this doc's own scope-boundary decision), not to be built here.
- **Per-axis specificity weighting** (whether a location hop and an incident-type hop count equally toward total specificity, or one axis should dominate) — a plausible technical/UX tuning parameter, not committed here; only matters on the rare cross-axis tie (one Scope more specific on location, another on type).
- Whether this doc's emergency-aware visibility trigger (an open Dispatch/Call/Incident at the matching Location) should later unify with Person Registry's still-open, platform-wide "active emergency event" concept once Muster Check-in App specifies it — flagged, not resolved.
- `suggested_checklist_refs[]`'s actual resolution/runtime behavior, and satisfying the full-procedure back-reference contract (FR #11), belong entirely to Incident Action Checklists (next doc) — this doc only reserves the pointers and states the requirement.
- The voice-call channel's actual adaptor (telephony provider, IVR script, per-tenant enablement) is Notifications Engine's own open question, not resolved here — this doc only specifies that `automated_escalation` is gated on that channel's availability.
- Whether Location Registry's existing `utility_shutoff_pointers[]` should eventually retrofit to reference Preplan Utility Point rather than staying independent free text — left alone here since a Location with no Preplan at all still needs its own light quick-reference field.
