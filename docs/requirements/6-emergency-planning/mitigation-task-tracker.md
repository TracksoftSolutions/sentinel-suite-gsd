# Mitigation Task Tracker

## Overview

Mitigation Task Tracker (Module 6, 7/8) resolves the forward reference HIRA reserved but deliberately didn't build: Risk Assessment's `mitigation_task_ref`. Per HIRA's own elicited decision, this doc is a genuinely distinct mechanism from Improvement Action / IP Tracking, not a sixth `source_type` on that record — real security-capital remediation work (adding a barrier, replacing a gate) needs budget tracking and facility-technician assignment that Improvement Action's shape doesn't carry.

Four elicited decisions:

1. **Assignee is always a real, registered Person entity, inline-created when needed** — the same durable-relationship discipline as Agency Handoff Log/ICS Role Assignment, since a facility technician assigned once is very likely to be assigned again, and a real record supports dedup/accountability history even for someone with no platform login.
2. **Budget Tracking is a real per-site/fiscal-period Mitigation Budget register**, not a bare per-task cost field — tasks optionally draw against a budget, with live-computed committed/spent/remaining rollups.
3. **The "Audit Trails" MODULES.md bullet is satisfied by closing the loop back to HIRA's own Risk Assessment history, not a new mechanism** — completing a Mitigation Task offers a launch-point "Reassess Risk" action (where a lineage back to a Risk Assessment exists), and the resulting before/after score comparison in Risk Assessment's already-built history *is* the proof of safety improvement.
4. **Ad hoc creation is allowed, unrelated to any Risk Assessment** — mirroring IP Tracking's own `ad_hoc` allowance for Improvement Action, since real remediation work (e.g., "add a barrier here") is often noticed directly, not only surfaced through a formal HIRA cycle.

## Actors & Roles

- **Safety Coordinator / Facility Safety Officer** — creates Mitigation Tasks (from a Risk Assessment or ad hoc), monitors the board and registry.
- **Facility Technician** (Mitigation Task assignee) — a real Person entity, may or may not be a platform user; updates status, attaches evidence on their own assigned task.
- **Site / Tenant Admin** — configures Mitigation Budget registers.
- **Safety Director / Emergency Manager** — views the cross-site Mitigation Task Registry and budget rollups.

## Functional Requirements

### Mitigation Task (Activity extension)
1. **Mitigation Task** registers as its own Activity extension (`activity_type = mitigation_task`) — `risk_assessment_ref` (nullable, FK → Risk Assessment — the originating HIRA finding), `assigned_to_ref` (FK → Person, inline-creatable), `description`, `category` (`barrier`, `gate`, `lighting`, `structural`, `procedural`, `other`), `estimated_cost`, `actual_cost` (nullable until completion), `budget_ref` (nullable, FK → Mitigation Budget), `status` (`open`/`in_progress`/`completed`/`cancelled`), `target_completion_date`, `actual_completion_date` (nullable), `evidence_document_refs[]` — the same evidence-attachment shape Improvement Action already established (optional, never a completion gate).
2. **Creating a Mitigation Task from a Risk Assessment** (Command/Action Bus launch point, context pre-filled from the originating Threat Directory Entry/Risk Assessment) sets `risk_assessment_ref` on the task and, reciprocally, `mitigation_task_ref` on the Risk Assessment *(retrofit — [hazard-identification-risk-assessment-hira.md](hazard-identification-risk-assessment-hira.md), first real producer for a field that doc reserved but left unresolved)*.
3. **Ad hoc creation** registers as a separate Command/Action Bus action, available any time with `risk_assessment_ref = null` — no HIRA finding required, the same "real practice regularly needs this" reasoning IP Tracking's `ad_hoc` Improvement Action already established.
4. Mitigation Task registers `is_mergeable = false` — a discrete tracked task with no duplicate-identity concept beyond its own record.

### Mitigation Budget
5. **Mitigation Budget** (tenant/site register) carries `site_ref` (nullable — tenant-wide or site-specific), `fiscal_period_label`, `period_start`, `period_end`, `approved_amount`, `status` (`active`/`closed`).
6. A Mitigation Task's optional `budget_ref` draws against a budget; the budget's `committed_amount` (live-computed sum of `estimated_cost` across `open`/`in_progress` tasks against it), `spent_amount` (live-computed sum of `actual_cost` across `completed` tasks against it), and `remaining_amount` (`approved_amount − committed_amount − spent_amount`) are all computed at query time, never separately stored — the same CQRS read-model discipline Action Item Registry already established, no denormalized rollup to keep in sync.
7. A Mitigation Budget going over-committed (negative `remaining_amount`) is **surfaced, never blocking** — a task can still be created/assigned against an exhausted budget; the platform's standard "escalate, don't block" rule applied to a budget constraint for the first time.

### Closing the loop (audit trail via re-assessment)
8. **Marking a Mitigation Task `completed` offers a "Reassess Risk" launch-point action** (Command/Action Bus, context pre-filled: the originating Threat Directory Entry, a note referencing the completed task) whenever `risk_assessment_ref` traces back to one — creating a new Risk Assessment on that Threat Directory Entry the normal way (HIRA's own mechanism, zero new fields). An ad hoc Mitigation Task with no `risk_assessment_ref` has no Threat Directory Entry to reassess and offers no such prompt — an honest scope limit, not a gap to patch.
9. Reassessment is never automatic and never required to mark a task `completed` — an explicit, optional next step, the same launch-point-not-obligation discipline used everywhere else (Checkpoint Scan → Patrol Finding, AI Draft Narrative's propose-then-confirm).

### Board and registry
10. **Mitigation Task registers as a `card` Queue Role** *(retrofit — Active Incident Queue)*, the same zero-new-mechanism Kanban-groupable pattern Resource Request already established — a Safety Coordinator works the board by status column in Multi-Incident Console's existing Kanban panel, dragging a card invoking the task's own already-registered status-transition action.
11. A new **`mitigation_task_registry`** panel type registers into the shared Panel Registry (after `action_item_registry`, `resource_catalog`, `alarm_monitor`, `camera`, `org_chart`, `health`, `evacuation`) — a live, filterable CQRS read-model (by status, category, assignee, site, budget, overdue) with Mitigation Budget rollups alongside, mirroring Action Item Registry's own shape and RBAC/ABAC-live-recheck discipline exactly.

## Data Model / Fields

**Mitigation Task** (Activity extension; entity_id shared PK, FK → Activity.entity_id; activity_type = mitigation_task)
- risk_assessment_ref (nullable), assigned_to_ref (FK → Person)
- description, category (barrier, gate, lighting, structural, procedural, other)
- estimated_cost, actual_cost (nullable), budget_ref (nullable, FK → Mitigation Budget)
- status (open, in_progress, completed, cancelled)
- target_completion_date, actual_completion_date (nullable)
- evidence_document_refs[]

**Mitigation Budget** (tenant/site register)
- budget_id, tenant_id, site_ref (nullable)
- fiscal_period_label, period_start, period_end
- approved_amount, status (active, closed)
- *(committed_amount, spent_amount, remaining_amount — computed, not stored)*

**Risk Assessment** *(retrofit — HIRA)*
- mitigation_task_ref (nullable) — now has a real producer (FR #2); field shape unchanged from HIRA's original spec

## States & Transitions

- **Mitigation Task:** `open` → `in_progress` → `completed`/`cancelled` — self- or on-behalf-of by the assignee or a Safety Coordinator, the platform's standard execution posture.
- **Mitigation Budget:** `active` → `closed` (fiscal period ends), plain admin lifecycle, no approval gate.

## Integrations

- **Hazard Identification & Risk Assessment (HIRA)** *(retrofit)*: source of Risk Assessment/Threat Directory Entry; resolves that doc's own reserved `mitigation_task_ref` forward reference.
- **Document Registry**: source of Mitigation Task's `evidence_document_refs[]`, unmodified.
- **Active Incident Queue (CAD Console)** *(retrofit)*: Mitigation Task registers as a `card` Queue Role.
- **Multi-Incident Console / Command Center Wallboard View** *(retrofit)*: Panel Registry gains `mitigation_task_registry` as a new cross-doc-contributed panel type.
- **Command/Action Bus**: Create Mitigation Task (from Risk Assessment), Create Ad Hoc Mitigation Task, Reassess Risk, and status-transition actions all register.
- **Entity Registry Core**: inline Person creation for a non-platform-user facility technician, the same discipline as Agency Handoff Log.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin | Safety Coordinator | Facility Technician (assignee) | Safety Director/Emergency Manager |
|---|---|---|---|---|---|---|
| Configure Mitigation Budget | ✅ | ✅ | ✅ (own scope) | ❌ | ❌ | ❌ |
| Create Mitigation Task (from Risk Assessment or ad hoc) | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| Update status / attach evidence on own task | — | — | — | ✅ (on-behalf-of) | ✅ | ❌ |
| View board / Mitigation Task Registry | ✅ | ✅ | ✅ | ✅ | ✅ (own tasks) | ✅ |
| Trigger Reassess Risk | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

## Non-Functional / Constraints

- Mitigation Budget rollups (`committed_amount`/`spent_amount`/`remaining_amount`) are always computed live, never a separately stored value that could drift from the underlying tasks.
- An over-committed budget never blocks task creation or assignment — surfaced only, the platform's standard escalate-don't-block rule.
- The Mitigation Task Registry never widens visibility beyond each task's own RBAC/ABAC, the same discipline as Action Item Registry.
- Evidence attachment stays optional, never a completion gate — consistent with every prior tracked-task mechanism (Improvement Action, Drill Component Check).

## Acceptance Criteria

- [ ] Creating a Mitigation Task from a high-risk Risk Assessment correctly sets both the task's `risk_assessment_ref` and the originating Risk Assessment's `mitigation_task_ref`.
- [ ] Creating an ad hoc Mitigation Task with no Risk Assessment succeeds, and offers no "Reassess Risk" prompt on completion.
- [ ] Assigning a Mitigation Task to a facility technician who isn't yet a registered Person inline-creates a minimal Person record.
- [ ] A Mitigation Budget's `remaining_amount` correctly reflects live committed + spent totals across every task drawing against it, with no separately stored rollup value.
- [ ] Creating a task against an already over-committed budget succeeds without any blocking confirmation beyond the platform's standard action confirmation gate.
- [ ] Completing a Risk-Assessment-originated Mitigation Task offers the Reassess Risk launch point, pre-filled with the originating Threat Directory Entry.
- [ ] The `mitigation_task_registry` panel type is selectable in both Multi-Incident Console's Console Layout and Command Center Wallboard View's Display Profile.
- [ ] Dragging a Mitigation Task card between Kanban columns in Multi-Incident Console correctly invokes its status-transition action, matching Resource Request's established behavior.

## Open Questions

- **A real overlap risk with Module 7's future Corrective Action Pipelines (Work Order Generation, Tracking Boards, Verification Logs) is flagged, not resolved here** — that doc's own framing (automated ticketing for failed safety items, tracked to technician sign-off) sounds structurally close to this doc's Mitigation Task. Deliberately not pre-merged since Module 7 isn't specified yet; worth a real reconciliation pass when that module is reached, the same posture used for Preplans' `hazard_flags[]` vs. the Module 7 Hazmat/NFPA pair.
- Mitigation Task is deliberately kept a sibling of, not merged with, Key Ring Registry/Lock Core's existing Locksmith Work Order ledger — that mechanism stays scoped to lock/key hardware specifically; not resolved further here.
- Exact Mitigation Budget approval workflow (if any tenant wants one) — not committed here; `approved_amount` is entered directly by an Admin today, no approval-gate record like Mutual Aid Agreement's optional one.
- Whether `category` should become a Tenant-Defined Subtype (Tenant-Defined Types & Custom Fields) rather than a fixed enum, if tenants need categories beyond the six listed — not committed here.
