# Improvement Plan (IP) Tracking

## Overview

IP Tracking answers the question After-Action Reports left open: what happens to Improvement Action once more than one thing can produce it. Three elicited decisions:

1. **Improvement Action's origin widens from AAR-only to a real `source_type`/`source_ref` pair** *(retrofit — see [after-action-reports.md](after-action-reports.md))* — `aar_report`, a new **`ad_hoc`** (a corrective task logged directly, no formal AAR required — real safety practice regularly needs this, and forcing a fake AAR into existence just to log one observation would be the wrong tradeoff), and a reserved-but-unreachable `drill` value forward-referencing Exercise & Drill Planner. The Improvement Action record itself — owner, target date, status, evidence, all built in AAR — is completely unchanged; only its parent-link shape widens.
2. **Action Item Registry is a pure CQRS read-model, no new storage** — a cross-source, filterable rollup over every Improvement Action platform-wide (by status, owner, site, category, source, overdue), mirroring Active Incident Queue's own "universal query over an existing base type" shape rather than duplicating Improvement Action data into a second table.
3. **Task Deadlines get a genuinely new mechanism, not a Duration Watchdog reuse** — MODULES.md's "reminders as deadlines approach" is a *lead-time* (before due date) notification, structurally different from Duration Watchdog's *elapsed-time* (after threshold) model that AAR's own Overdue Improvement Action alert already uses. Both now coexist on the same record: AAR's Duration Watchdog still fires once a deadline has passed; this doc adds a new, deliberately separate **Deadline Reminder Policy** that fires proactively at configured lead-time offsets before it.

Evidence stays exactly as AAR built it — optional, never a completion gate, per explicit user direction (the platform's "compliance tracking must never become an operational blocker" rule, already applied identically to Guard Tour's verification-degrades-gracefully posture). This doc surfaces the evidence log read-only inside the registry view; it introduces no new attachment mechanism.

## Actors & Roles

- **Safety Director / Program Manager** — primary consumer of the Action Item Registry; the role MODULES.md's "central registry" framing is really written for.
- **Supervisor** (or above) — creates ad hoc Improvement Actions outside any AAR; updates/reassigns on an owner's behalf.
- **Improvement Action owner** — unchanged from After-Action Reports: updates their own action's status, attaches evidence.
- **Site / Tenant Admin** — configures the Deadline Reminder Policy.

## User Stories

- As a **Safety Director**, I want one screen listing every open corrective action across every AAR, not a separate list per activation.
- As a **Supervisor**, I want to log a corrective action from something I noticed on a routine walk-through, without having to first manufacture an AAR to attach it to.
- As an **Improvement Action owner**, I want a heads-up a week before my task is due, not just a notification after I've already missed it.
- As a **Tenant Admin**, I want to configure how many days ahead reminders fire, and whether that differs by category.
- As a **Safety Director**, I want to filter the registry to just overdue items, or just my own site, without needing a separate report each time.

## Functional Requirements

### Widened origin (retrofit)
1. **Improvement Action** *(retrofit — [after-action-reports.md](after-action-reports.md))* gains `source_type` (`aar_report`, `ad_hoc`, `drill` [reserved]) and `source_ref` (nullable — the AAR Report's entity_id when `source_type = aar_report`, null for `ad_hoc`), replacing the prior direct `aar_report_ref` field. Every other field and the entire status lifecycle (`open` → `in_progress` → `completed`/`cancelled`) is unchanged. **Retrofit — now reachable**: [Exercise & Drill Planner](exercise-drill-planner.md)'s below-target Evaluation Score auto-suggests a draft Improvement Action with `source_type = drill`/`source_ref` = the originating Exercise, requiring explicit Lead Evaluator/Exercise Director confirmation before it's real — the first populated use of this reserved value, distinct from and independent of that same Exercise's own eventual `aar_report`-sourced items. **Retrofit — [Drill Compliance Logging](drill-compliance-logging.md)**: `source_type` gains a fourth value, `compliance_drill` — a failed Drill Component Check auto-suggests a draft Improvement Action the same way, `source_ref` pointing at the originating Compliance Drill Log, gated on the identical explicit-confirmation discipline.
2. **Ad hoc creation** registers as a new Command/Action Bus action, available any time, requiring no AAR or other anchor — `source_type = ad_hoc`. Carries the same mandatory fields as an AAR-originated action (recommendation, category, `proposed_owner_ref`, `target_completion_date`) plus an optional `origin_note` (free text — what prompted it) and optional `related_location_ref`, since an ad hoc observation often has a place but no formal record to anchor to.

### Action Item Registry
3. A live, filterable CQRS read-model over every Improvement Action platform-wide — by `status`, `source_type`, owner, site, category, and a computed `is_overdue` flag — with RBAC/ABAC re-checked live per record at query time (the same discipline Global Search & Data Indexing already established: an aggregation layer is never a second source of truth for access control).
4. Registers a new **`action_item_registry`** panel type into the shared Panel Registry (the catalog Multi-Incident Console and Command Center Wallboard View already promoted) — selectable in a Console Layout or Wallboard Display Profile zone, zero new panel infrastructure.
5. The registry surfaces each Improvement Action's attached evidence Documents read-only (Document Registry, unmodified) — no new upload path, no completion gate, consistent with the elicited decision to keep evidence optional.

### Deadline Reminder Policy (lead-time reminders)
6. A tenant-level, optionally category-scoped **Deadline Reminder Policy** (Settings & Preferences) carries `lead_time_offsets_days[]` (e.g., `[7, 1]`) — every `open`/`in_progress` Improvement Action with a `target_completion_date` is checked against each configured offset by the platform's existing Real-Time Delivery Server-Side Timer Service (the same restart-safe, always-on scheduling infrastructure Duration Watchdog already runs on, reused here for a lead-time check rather than an elapsed-time one). **Retrofit — [Mutual Aid Agreements Tracker](mutual-aid-agreements-tracker.md):** once a second real consumer needed this identical shape (agreement renewal reminders), this mechanism was promoted into Active Call Alerts & Timers as the shared **Approaching-Deadline Reminder** registry — this doc's `(improvement_action, target_completion_date)` instance is unchanged in behavior, just no longer locally owned.
7. Each offset fires **at most once** per Improvement Action, tracked via a `reminders_sent[]` list — crossing the 7-day mark sends one reminder; crossing the 1-day mark sends a second, independent one; neither ever repeats. Delivered through the existing Notifications Engine to the owner (and Supervisor, if the policy is configured to cc them) — no new delivery infrastructure.
8. **Approaching-deadline reminders and AAR's existing after-the-fact Overdue Duration Watchdog are two independent, coexisting mechanisms** on the same Improvement Action — one proactive (before the deadline), one reactive (after it), deliberately not merged into a single generalized "Watchdog" abstraction; a strained unification would have forced Duration Watchdog's every existing consumer (SITREP, Key Custody, EOC Logistics Hub's overdue resources) to absorb a negative-offset concept none of them need.
9. Neither reminder type ever blocks any operational action — escalate, don't block, the platform's standing rule, unchanged.

## Data Model / Fields

**Improvement Action** *(retrofit — see [after-action-reports.md](after-action-reports.md))*
- source_type (aar_report, ad_hoc, drill, compliance_drill), source_ref (nullable)
- *(all other fields — recommendation, category, proposed_owner_ref, target_completion_date, actual_completion_date, status, evidence_document_refs[] — unchanged)*
- reminders_sent[] (new — which Deadline Reminder Policy offsets have already fired for this instance)

**Deadline Reminder Policy** (Settings & Preferences registration, tenant-level, category nullable = applies to all categories)
- tenant_id, category (nullable), lead_time_offsets_days[], cc_supervisor (bool)

## States & Transitions

- **Improvement Action:** no change to its lifecycle (`open` → `in_progress` → `completed`/`cancelled`), only its parent-link shape widened per FR #1.
- **Deadline Reminder Policy:** ordinary Settings & Preferences Definition, versioned like any other tenant configuration.

## Integrations

- **After-Action Reports** *(retrofitted)*: Improvement Action's parent-link field widens from `aar_report_ref` to `source_type`/`source_ref`; every other mechanism that doc built (evidence attachment, overdue Duration Watchdog, independent post-publish lifecycle) is reused completely unmodified.
- **Real-Time Delivery & Server-Side Timers**: owns the scheduling engine both Deadline Reminder Policy's lead-time checks and AAR's Overdue Duration Watchdog run on — the same infrastructure, two independent configured behaviors.
- **Notifications Engine**: delivery channel for both approaching-deadline and overdue alerts, unmodified.
- **Active Call Alerts & Timers**: AAR's Overdue Improvement Action Duration Watchdog instance is untouched by this doc — the two reminder types are siblings, not a hierarchy.
- **Document Registry**: source of the evidence log this doc surfaces read-only.
- **Command Center Wallboard View / Multi-Incident Console**: `action_item_registry` is a seventh cross-doc Panel Registry contributor (after `health`, `org_chart`, `camera`, `alarm_monitor`, `resource_catalog`).
- **Settings & Preferences**: owns the Deadline Reminder Policy registration.
- **Command/Action Bus**: Create Ad Hoc Improvement Action, Configure Deadline Reminder Policy, and the registry's own filter/view actions all register.
- **Exercise & Drill Planner** *(retrofitted — resolved)*: `source_type = drill` is populated by a confirmed, below-target-score-triggered Improvement Action, `source_ref` pointing at the originating Exercise.
- **Drill Compliance Logging** *(retrofitted — resolved)*: `source_type = compliance_drill` is populated by a confirmed, failed-component-triggered Improvement Action, `source_ref` pointing at the originating Compliance Drill Log.

## Permissions

| Action | Site/Tenant Admin | Safety Director / Supervisor | Improvement Action owner | Any permitted viewer |
|---|---|---|---|---|
| Configure Deadline Reminder Policy | ✅ | ❌ | ❌ | ❌ |
| Create an ad hoc Improvement Action | ✅ | ✅ | ❌ | ❌ |
| Update status / attach evidence on own action | — | ✅ (on-behalf-of) | ✅ | ❌ |
| View the Action Item Registry | ✅ | ✅ | ✅ (own items) | inherits underlying record RBAC/ABAC |

## Non-Functional / Constraints

- The Action Item Registry never widens visibility beyond what each individual Improvement Action's own RBAC/ABAC already permits — an aggregation view is not a second source of access-control truth.
- Each Deadline Reminder Policy offset fires exactly once per Improvement Action instance (`reminders_sent[]`-tracked) — no repeat sends while an item sits between two configured offsets.
- Neither reminder type (approaching or overdue) blocks status transitions, evidence attachment, or anchor closure of any kind.

## Acceptance Criteria

- [ ] An Improvement Action can be created directly (`source_type = ad_hoc`) with no AAR or other anchor in existence.
- [ ] An Improvement Action originating from a published AAR retains its correct `source_ref` after the field-shape retrofit — no data loss for existing AAR-sourced records.
- [ ] The Action Item Registry, filtered to `overdue`, shows only items past their `target_completion_date` and not yet `completed`/`cancelled`, regardless of source type.
- [ ] A user with no visibility into a given Improvement Action's underlying anchor (e.g., a different site's Incident) never sees it in their own registry view.
- [ ] Configuring a Deadline Reminder Policy with offsets `[7, 1]` sends exactly two reminders per applicable Improvement Action — one at each crossing — never more.
- [ ] An Improvement Action already past due still separately triggers AAR's existing Overdue Duration Watchdog, independent of whether its lead-time reminders already fired.
- [ ] Marking an Improvement Action `completed` succeeds with zero attached evidence Documents — evidence is never a required gate.
- [ ] The `action_item_registry` panel type is selectable in both Multi-Incident Console's Console Layout and Command Center Wallboard View's Display Profile, from the same shared Panel Registry catalog.

## Open Questions

- Exact default lead-time offset presets and whether tenants need more than one preset per category — a content/config design task, not committed here.
- Whether an ad hoc Improvement Action should carry a richer originating-observation structure beyond a short free-text `origin_note` — not committed.
- Whether the registry needs bulk reassignment/export tooling for a Safety Director managing a large open list — a UX/technical-spec concern, not committed here.
- ~~Exact reconciliation once Exercise & Drill Planner exists and `source_type = drill` becomes reachable~~ — resolved; see the Integrations retrofit notes above.
