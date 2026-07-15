# ICS Forms Engine (FEMA-aligned)

## Overview

ICS Forms Engine opens Module 5 by generalizing a mechanism the platform already built twice in miniature. Situation Reports (Module 3) built Draft→Publish, recurring generation, per-Section authorship, and live auto-population for exactly one FEMA form (ICS-209). ICS Role Mapping & Visual Org Chart built a live-rendered ICS-207 org chart from ICS Role Assignment data. This doc promotes both into one platform-wide **Form Definition / Form Instance** engine covering the standard NIMS/FEMA ICS 200-series catalog (ICS-201 through ICS-215A) — per explicit user direction, generalizing rather than leaving three narrow, hand-built mechanisms to drift apart.

Three structural moves, all elicited:

1. **Operational Period becomes a first-class record** — real NIMS/FEMA practice organizes nearly every form around a defined planning cycle (e.g., 0600–1800), which the platform had no concept of. A **Full ICS** EOC Activation cycles through explicit, sequentially numbered Operational Periods; a **Limited ICS** Incident gets one implicit, continuous period auto-opened at creation and closed at incident closure, so a minor incident never needs manual period management.
2. **SITREP's machinery generalizes onto every form, not just ICS-209.** Draft→Publish (immutable Document snapshot via Document Registry), recurring/period-triggered generation (Background Job Processing), tenant-configurable authorship mode (`single_compiler`/`per_section_contribution`), and snapshot-at-publish auto-population are now properties of the generic **Form Instance**, not SITREP-specific code. ICS-209 becomes two Form Definitions (Full and Limited variants — the same "genuinely separate report types, not one form with hidden fields" instinct that shaped the original SITREP/Incident Status Report split) that consume this engine rather than owning their own copy of it.
3. **ICS-207 becomes a zero-new-fields render.** Generating an ICS-207 Form Instance queries current ICS Role Assignment rows for the anchor and renders them into the standard FEMA layout, then Document-snapshots the result exactly like any other form — no retrofit to ICS Role Assignment's own data model.

The catalog itself is a **fixed, developer-built FEMA-standard set plus tenant-defined custom forms** — a tenant can register a jurisdiction-specific or internal form as a Tenant-Defined Subtype anchored on the concrete `Form Instance` type (the existing Tenant-Defined Types & Custom Fields mechanism, no new custom-form machinery). Each Form Definition, fixed or tenant-defined, declares its own **tier applicability** (`full`, `limited`, or `both`) — most of the heavier planning artifacts (203, 204, 205, 206, 215/215A) are Full-ICS-only; lighter forms (201, 207, 213, 214) apply at both tiers.

Wherever a real platform data source exists — ICS Role Assignment, Status & State Monitors' Unit State, EOC Logistics Hub's Resource Request, Item custody/status transitions — form fields **auto-populate and freeze at publish**, the same "reporting engine captures upstream metadata" discipline SITREP's Resources Committed block established, now the default posture for the whole catalog rather than one field on one form.

## Actors & Roles

- **Incident Commander** — compiles/publishes ICS-201 (Incident Briefing); default publisher of last resort for any form with no other clear compiler.
- **Planning Section (Situation Unit, Resources Unit, Documentation Unit)** — via current ICS Role Assignment: compiles 202 (Objectives), 203 (Org Assignment List), 204 (Assignment List), 209 (Incident Status Summary), 210 (Resource Status Change), 211 (Check-In List), 215/215A (Planning Worksheet/Safety Analysis).
- **Logistics Section (Communications Unit, Medical Unit)** — compiles 205 (Communications Plan), 206 (Medical Plan).
- **Safety Officer** — compiles 208 (Safety Message/Plan), co-authors 215A.
- **Any ICS Position holder** — logs their own ICS-214 (Activity Log), authors ad hoc ICS-213 (General Message).
- **EOC Coordinator / Supervisor** — opens/closes Operational Periods (Full ICS), overrides cadence/authorship per activation, publishes on behalf of an absent Section Chief.
- **Site / Tenant Admin** — authors tenant-defined custom Form Definitions, configures Operational Period and Cadence Policy defaults.
- **Any user with permission on the Incident/EOC Activation** — views published forms.

## User Stories

- As **Planning Section**, when a new Operational Period opens, I want the period's required forms auto-drafted with whatever live data the platform already has, so I'm not rebuilding 203/204/209 from scratch every cycle.
- As the **Incident Commander**, I want to fill out ICS-201 once near the start of the incident, not re-fill it every period.
- As the **Resources Unit**, I want ICS-203's Organization Assignment List to pull directly from current ICS Role Assignments instead of me re-transcribing who holds what position.
- As the **Communications Unit Leader**, I want to author ICS-205 manually, since the platform doesn't yet track a radio channel registry it could auto-populate from.
- As a **Section Chief** in `per_section_contribution` mode on ICS-209, I want to submit only my own section's status, same as I already can on SITREP today.
- As the **Resources Unit**, I want ICS-210 (Resource Status Change) and ICS-211 (Check-In List) to auto-draft when a tracked resource's status changes or a resource arrives, so I only have to confirm, not re-key.
- As **any ICS Position holder**, I want my ICS-214 Activity Log to auto-populate from my own logged activity for the current period, rather than manually re-listing what I already did.
- As a **Guard** at a `mandatory_limited` tenant logging a minor incident, I want the light Incident-Status-Summary and General Message forms available without any Operational Period bookkeeping.
- As a **Tenant Admin**, I want to register a jurisdiction-specific form beyond the standard FEMA set, using the same custom-field mechanism I already use elsewhere.
- As a **Supervisor**, I want to be notified if a required period form goes unpublished, without it ever blocking period close-out or EOC deactivation.

## Functional Requirements

### Operational Period
1. **Operational Period** is an Activity extension (a real start/close lifecycle with its own rollup — which forms this period expects and whether they published — earns Activity treatment under the platform's standing "the moment it has a real lifecycle, it's an Activity" rule). It anchors to either an **EOC Activation** (Full ICS) or an **Incident** (Limited ICS) via `anchor_type`/`anchor_ref` — the same widened-anchor shape ICS Role Assignment already established, so no new association mechanism is needed.
2. **Full ICS**: Operational Periods cycle explicitly and sequentially (`sequence_number`, `period_start`, `period_end`). A tenant-level **Operational Period Policy** (Settings & Preferences) sets the default period duration and whether the next period auto-opens on the prior one's end or requires an explicit close-and-open action by the EOC Coordinator/Planning Section — both overridable per-activation, the platform's standard explicit-beats-default chain.
3. **Limited ICS**: one **implicit** Operational Period (`is_implicit = true`) auto-opens at Incident creation and closes at Incident closure — no manual period management, no cycling, matching the module's established "Limited gets the lightweight default, Full gets the real mechanism" depth split (ICS Role Mapping, Situation Reports).
4. Closing a period is a marker, not a lock: it never blocks a late Form Instance for that period from still publishing (see Overdue Alerting, FR #14) — consistent with the platform's hard "never let a compliance/tracking mechanism block an operational action" rule.

### Form Definition catalog
5. A **Form Definition** declares: `form_code`, title, `tier_applicability` (`full`/`limited`/`both`), `cadence` (`one_shot_per_anchor`, `per_operational_period`, `ad_hoc_unlimited`), `has_sections` (bool, ICS-Section-structured), `default_authorship_mode`, a `default_compiler_position_ref` (resolved against the anchor's current ICS Role Assignment holder for that Position — explicit-beats-default: an override picks a specific compiler, else the mapped Position's holder, else the IC), and whether it's `required_per_period` (tenant/ICS-Adoption-Policy-overridable).
6. **Day-one FEMA catalog** (15 Form Definitions, developer-built, platform-versioned):

   | Form | Title | Tier | Cadence | Sections? | Primary auto-populate source | Default compiler |
   |---|---|---|---|---|---|---|
   | ICS-201 | Incident Briefing | both | one_shot_per_anchor | no | ICS Role Assignment (initial roster) | Incident Commander |
   | ICS-202 | Incident Objectives | full | per_operational_period | no | — (manual) | Planning Section Chief |
   | ICS-203 | Organization Assignment List | full | per_operational_period | no | ICS Role Assignment | Resources Unit |
   | ICS-204 | Assignment List | full | per_operational_period | yes | ICS Role Assignment + Resource Request | Resources Unit |
   | ICS-205 | Communications Plan | full | per_operational_period | no | — (manual; no radio-channel registry yet) | Communications Unit |
   | ICS-206 | Medical Plan | full | per_operational_period | no | — (manual) | Medical Unit |
   | ICS-207 | Organization Chart | both | per_operational_period | no | ICS Role Assignment (full render, zero new fields) | auto-generated, no compiler |
   | ICS-208 | Safety Message/Plan | full | per_operational_period | no | — (manual, AI-assist eligible) | Safety Officer |
   | ICS-209 (full) | Incident Status Summary | full | per_operational_period | yes | Unit State (Resources Committed), Update timeline | Situation Unit |
   | ICS-209-lite | Incident Status Summary (Limited) | limited | per_operational_period | no | Unit State (Resources Committed) | Incident Commander |
   | ICS-210 | Resource Status Change | full | ad_hoc_unlimited | no | Item custody/status Domain Events | Resources Unit (propose+confirm) |
   | ICS-211 | Check-In List | full | ad_hoc_unlimited | no | Resource Request fulfillment / ICS Role Assignment creation | Resources Unit (propose+confirm) |
   | ICS-213 | General Message | both | ad_hoc_unlimited | no | — (manual) | any ICS Position holder |
   | ICS-214 | Activity Log | both | ad_hoc_unlimited, windowed to period | no | Activity Registry, filtered to the holder's Position | each Position holder, own log |
   | ICS-215 / 215A | Operational Planning Worksheet / Safety Analysis | full | per_operational_period | no | — (manual) | Operations Section Chief / Safety Officer |

   Exact field-by-field content for each form is a content/config concern, not committed here (same posture SITREP already took on its own exact cadence presets).
7. **Tenant-defined custom forms** register as a **Tenant-Defined Subtype** anchored on the concrete `Form Instance` type (Tenant-Defined Types & Custom Fields, unmodified) — a tenant's custom form is a real Form Instance row with `tenant_subtype_ref` set, its fields living in `extended_fields` (`custom_`-prefixed), inheriting Form Instance's entire standard treatment (Draft→Publish, cadence, offline posture) for free. No new custom-form mechanism.

### Form Instance lifecycle (generalized from SITREP)
8. A **Form Instance** is an Activity extension, one per `(Form Definition, anchor, Operational Period)` for `per_operational_period`/`one_shot_per_anchor` cadence, or one per triggering event for `ad_hoc_unlimited` cadence. It moves `drafting` → `published` (terminal, immutable) exactly like SITREP's Draft/Incident Status Report did — a new period or trigger always produces a fresh instance; a published one is never reopened.
9. **Generation trigger** follows the Form Definition's cadence: `per_operational_period` forms auto-draft when their anchor's Operational Period opens (Background Job Processing, idempotency key `form:{form_definition_id}:{anchor_id}:{operational_period_id}`); `ad_hoc_unlimited` forms draft on their specific triggering event (a status change, a check-in, a manual "new message" action) or purely manually; `one_shot_per_anchor` forms draft once, manually, near anchor creation.
10. **Authorship mode** (`single_compiler`/`per_section_contribution`), for any `has_sections = true` Form Definition, resolves via the same tenant-default-with-per-activation-override chain SITREP established — unchanged behavior, now declared once at the engine level instead of locally to ICS-209.
11. **Publishing** snapshots a Form Instance into an immutable Document via Document Registry's hash/version model — identical, unmodified mechanism to every other report-shaped feature on the platform (DAR, Incident Report, SITREP, Historical CAD Log Reconstruction).

### Auto-population
12. Each Form Field Definition declares a `data_source`: `manual`, or `auto:<registered resolver key>` pointing at a named read-model callback owned by the module that has the data (e.g., `ics_role_assignments_by_anchor` — ICS Role Mapping; `unit_state_by_activation` — Status & State Monitors; `resource_requests_by_activation` — EOC Logistics Hub; `activity_registry_filtered_by_position` — Activity Registry). This is the same lightweight named-resolver registration discipline already used for Health Signal Registration and Reference Field Registration — a mechanically enumerable list, not a hardcoded per-form integration.
13. An `auto` field's value resolves live while the instance is `drafting` and **freezes permanently at publish** — a published form never silently drifts from what its live source shows afterward, the same snapshot-immutability discipline used everywhere else on the platform (SITREP's Resources Committed, DAR's filtered view, Historical CAD Log Reconstruction).

### ICS-209 and ICS-207 absorption
14. **ICS-209 (full) and ICS-209-lite are the direct successors of SITREP and Incident Status Report** *(retrofit — see [situation-reports.md](../3-command-center-dashboard-eoc/situation-reports.md))* — same field content, same Resources Committed auto-population, same `sitrep_narrative` AI Context, now expressed as two Form Definitions consuming this engine's generic Draft→Publish/cadence/authorship machinery rather than owning a parallel copy of it.
15. **ICS-207 is a pure render of current ICS Role Assignment data** *(retrofit — see [ics-role-mapping-visual-org-chart.md](../3-command-center-dashboard-eoc/ics-role-mapping-visual-org-chart.md))* — generating an instance queries the anchor's current Role Assignments and renders the standard FEMA org-chart layout; there is no compiler step and no independent field data. The existing live `org_chart` Panel Registry view is unaffected — this only adds the ability to Document-snapshot that same data as a formal, point-in-time ICS-207.

### Overdue / required forms
16. A `required_per_period` Form Definition with no `published` instance for its current Operational Period registers a **Duration Watchdog** instance (the same generalized `(activity_type, watched_field, watched_value)` mechanism every other overdue-alert on the platform reuses), optionally escalating via the tenant's Critical Event Escalation Policy. This never blocks Operational Period close, EOC Activation deactivation, or Incident closure — escalate, don't block, the platform's standing rule.

### AI-assisted narrative fields
17. A new **`ics_form_narrative`** AI Context (AI/LLM Services) drafts narrative-bearing manual fields (201's initial situation summary, 202's objectives narrative, 208's safety message) from live anchor data — the same "AI proposes, human confirms" discipline `sitrep_narrative` already established for ICS-209, now available to any form with a narrative field. Drafted content is `source = ai_generated` and never enters a published Document until the compiler explicitly approves it.

## Data Model / Fields

**Operational Period** (Activity extension; entity_id shared PK, FK → Activity)
- anchor_type (eoc_activation, incident), anchor_ref
- sequence_number, period_start, period_end (nullable while active)
- status (active, closed), is_implicit (bool), opened_by, closed_by (nullable)

**Operational Period Policy** (Settings & Preferences registration, tenant-level, Full-ICS only)
- tenant_id, default_period_duration_hours, auto_advance (bool)

**EOC Activation** *(retrofit — additive fields only)*
- period_duration_override_hours (nullable), period_auto_advance_override (nullable)

**Form Definition** (developer-built catalog, or tenant-defined subtype anchor)
- form_id, form_code, title, description
- tier_applicability (full, limited, both), cadence (one_shot_per_anchor, per_operational_period, ad_hoc_unlimited)
- has_sections (bool), default_authorship_mode (single_compiler, per_section_contribution)
- default_compiler_position_ref (nullable), required_per_period (bool)
- is_tenant_defined (bool)

**Form Field Definition** (child of Form Definition)
- field_id, form_definition_ref, field_key, label, field_type (text, narrative, number, table, entity_ref)
- data_source (manual, auto:<resolver_key>), section (nullable)

**Form Instance** (Activity extension; entity_id shared PK, FK → Activity; `tenant_subtype_ref` set for custom forms)
- form_definition_ref, anchor_type, anchor_ref, operational_period_ref (nullable for ad hoc instances not tied to a specific period)
- status (drafting, published), compiled_by (nullable until published), published_document_ref (nullable)
- field_values{} (per field_key: value, data_source, snapshotted_at)

**Form Section Entry** (child of Form Instance; only for `has_sections = true` Form Definitions)
- entry_id, form_instance_ref, section (command, operations, planning, logistics, finance_admin)
- field_values{} (scoped to that section), contributed_by, source (manual, ai_generated), ai_approved (bool)

**ICS Forms Cadence Policy** (Settings & Preferences registration, tenant-level)
- tenant_id, form_definition_ref, default_required_per_period (bool override)

## States & Transitions

- **Operational Period:** `active` → `closed` (Full ICS: EOC Coordinator/Planning Section action, or auto per Operational Period Policy; Limited ICS: auto-opens at Incident creation, auto-closes at Incident closure).
- **Form Instance:** `drafting` → `published` (terminal, immutable). A new period/trigger always creates a fresh instance — a published one is never reopened, identical to SITREP's own lifecycle.
- **Form Section Entry:** created once per contributor per instance, immutable once the parent instance publishes.

## Integrations

- **Situation Reports** *(retrofitted — superseded)*: SITREP and Incident Status Report become the ICS-209 (full) and ICS-209-lite Form Definitions. Their existing field shape, Resources Committed auto-population, and `sitrep_narrative` AI Context carry over unmodified as this Form Definition's own schema and resolver wiring; the Draft→Publish/cadence/authorship machinery that doc built locally is now this engine's generic mechanism.
- **ICS Role Mapping & Visual Org Chart** *(retrofitted)*: source of ICS-207's render data and every `ics_role_assignments_by_anchor` auto-populate resolver (203, 204). EOC Activation gains the period-override fields above. `is_command_staff`/Position/Section vocabulary is reused directly for Form Section Entry's `section` field and ICS-204's Division/Group-adjacent grouping.
- **Status & State Monitors**: source of the `unit_state_by_activation` resolver (209's Resources Committed, 210's status-change trigger).
- **EOC Logistics Hub**: source of the `resource_requests_by_activation` resolver (204, 210, 211).
- **Activity Registry**: source of the `activity_registry_filtered_by_position` resolver powering ICS-214's Activity Log field — the same "report = filtered Activity Registry view" instinct DAR established, expressed here as a single auto field rather than a parallel report mechanism.
- **Background Job Processing**: owns the recurring/period-triggered `form_generate_draft` job type — one instance per `(Form Definition, anchor, Operational Period)`, isolation-tier-aware placement, idempotent, the same registration discipline every prior recurring job (including SITREP's own) already used.
- **AI / LLM Services**: owns the new `ics_form_narrative` AI Context, its Prompt Template, and provider/BYO-key plumbing — this doc only declares the context and its placeholders.
- **Document Registry**: publish snapshots a Form Instance into an immutable Document, the same hash/version model every report-shaped feature uses.
- **Active Call Alerts & Timers**: overdue-required-form is a registered Duration Watchdog instance; Critical Event Escalation Policy is the optional second-tier escalation surface, no new alerting mechanism.
- **Tenant-Defined Types & Custom Fields**: owns tenant-defined Form Definitions as Tenant-Defined Subtypes anchored on `Form Instance`.
- **Settings & Preferences**: owns Operational Period Policy and ICS Forms Cadence Policy.
- **Command/Action Bus**: Generate/Edit/Publish Form Instance, Open/Close Operational Period, and per-activation cadence/authorship override all register as actions.
- **Command Center Wallboard View**: a published form is a natural `detail`-panel target; the `org_chart` panel type already renders ICS-207's live source data, unaffected by this doc's added ability to also snapshot it.
- **Structured Logging & Audit Trails**: every publish, and every Operational Period open/close, is audit-tier.

## Permissions

| Action | Site/Tenant Admin | ICS Position holder (Section Chief/Unit Lead) | EOC Coordinator/Supervisor | Any permitted viewer |
|---|---|---|---|---|
| Author a custom Form Definition | ✅ | ❌ | ❌ | ❌ |
| Configure Operational Period Policy / ICS Forms Cadence Policy | ✅ | ❌ | ❌ | ❌ |
| Open/close an Operational Period (Full ICS) | ✅ | ❌ | ✅ | ❌ |
| Contribute a Form Section Entry | — | ✅ (own section) | ✅ (on-behalf-of) | ❌ |
| Compile/publish a Form Instance | — | ✅ (mapped Position) | ✅ | ❌ |
| View a published Form Instance / Document | inherits the anchor's existing RBAC/ABAC | | | ✅ |

## Non-Functional / Constraints

- Recurring/period-triggered draft generation runs via Background Job Processing; idempotent per `(form_definition_id, anchor_id, operational_period_id)` — a re-triggered job for an already-generated period no-ops rather than duplicating a draft.
- Every `auto` field snapshots at publish time — never live-updating inside a published Document, the platform's standard snapshot-immutability discipline.
- Overdue-required-form alerting is additive notification only — it never blocks Operational Period close, EOC Activation deactivation, or Incident closure.
- Form Instance registers `is_mergeable = false` — a form occurrence has no duplicate-identity risk the way a Person or Incident does, the same explicit opt-out already declared for Checkpoint Scan and Safety Check-in.
- Offline posture follows the platform's minimal-capture-subset contract: a Form Section Entry is append-capturable offline (sole-actor, appending only the contributor's own row); Publish — a shared-state finalization touched by a compiler assembling potentially several contributors' work — is online-only or Dispatcher-relayed, consistent with "never mutate shared server-side records offline."
- Tenant-defined custom Form Definitions can never widen mergeability or bypass the Draft→Publish immutability discipline — inherited from Form Instance's own posture, unconditionally, per Tenant-Defined Types & Custom Fields' "a subtype can never widen capability beyond its anchor" rule.

## Acceptance Criteria

- [ ] Activating EOC Response and opening its first Operational Period auto-drafts every `per_operational_period`, Full-tier Form Definition, pre-populated with whatever `auto` fields have a live source.
- [ ] A Limited ICS Incident gets one implicit Operational Period at creation with no manual step, and its `limited`/`both`-tier forms are immediately available.
- [ ] Closing an Operational Period and opening the next produces a fresh set of period-scoped Form Instances; a still-unpublished draft from the prior period remains publishable afterward.
- [ ] Publishing any Form Instance creates an immutable Document via Document Registry and locks the instance permanently, regardless of later changes to the underlying anchor data.
- [ ] ICS-203's Organization Assignment List auto-populates from current ICS Role Assignments with no manual re-entry.
- [ ] ICS-207, generated for an anchor with an active org chart, matches the live `org_chart` panel's current holder set exactly, with zero independently-entered fields.
- [ ] In `per_section_contribution` mode (ICS-209 full), each Section Chief can submit only their own section; the compiler assembles and publishes the whole.
- [ ] A `required_per_period` Form Definition with no published instance for the current period raises a Duration Watchdog instance without blocking Operational Period close or EOC deactivation.
- [ ] A tenant-defined custom Form Definition, once registered, follows the identical Draft→Publish/cadence lifecycle as any FEMA-standard form with zero engine-side special-casing.
- [ ] ICS-214's Activity Log auto-populates from the holder's own Activity Registry entries, windowed to the current Operational Period.
- [ ] AI-drafted narrative content on any eligible field never appears in a published Document until the compiler explicitly approves it.
- [ ] A Form Definition with `tier_applicability = full` produces no instance and no action surface on a Limited ICS Incident — absent, not merely disabled.

## Open Questions

- Exact field-by-field content for each of the 15 day-one forms — a content/config design task, not committed here, same posture SITREP already took on its own cadence presets.
- ICS-204's real-world Division/Group granularity is finer than the platform's current Section taxonomy (command/operations/planning/logistics/finance_admin) — flagged, not resolved; day one renders 204 grouped by Section only.
- Whether ICS-205 (Communications Plan) and ICS-206 (Medical Plan) should eventually auto-populate from a future radio-channel or facility/medical-resource registry (Module 19, Module 9) — forward reference only, both stay manual-entry for now.
- Whether Operational Period auto-advance should also push a Notifications Engine event (period-change awareness for the whole EOC), beyond the overdue-form Duration Watchdog already specified — plausible, not committed.
- Whether a popular tenant-defined custom form should have a graduation path into a developer-built Form Definition — same open graduation-path question Tenant-Defined Types & Custom Fields already carries generally, not solved here.
- Exact UX for the `per_section_contribution` compiler's completeness check before publishing — a technical-spec/UX-flow decision, the same open item SITREP already flagged and left unresolved.
