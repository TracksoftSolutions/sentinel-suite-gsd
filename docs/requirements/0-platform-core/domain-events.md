# Domain Events

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

Domain Events is the tenant-facing "when X happens, do Y" automation layer built on top of the low-level Command/Query/Event bus infrastructure (see [event-command-bus-architecture.md](event-command-bus-architecture.md)). It lets Tenant Admins (and the platform, via shipped baseline rules) define no-code rules that trigger on a curated, explicitly-documented subset of published domain events, optionally filtered on the event's payload, and — when matched — invoke a named action from the Command/Action Bus catalog (see [command-action-bus.md](command-action-bus.md)).

This feature owns the trigger/condition/rule-matching side of automation ("when/if"). It never performs an effect directly — all "what happens" logic is delegated to the Command/Action Bus, keeping this feature's responsibility narrow and its rules auditable as pure trigger-condition-action declarations.

## Actors & Roles

- **Tenant Admin** — authors, edits, tests (dry-run), enables/disables, and reviews firing history for tenant-scoped rules.
- **Platform Super Admin** — authors and maintains platform-baseline rules shipped out of the box; manages the curated catalog of automation-eligible events (in coordination with each feature's own spec).
- **Every platform feature/module** — explicitly designates which of its domain events are automation-eligible, and documents the payload fields available for rule conditions.
- **Command/Action Bus** — downstream feature invoked by a matched rule's action step; not something this feature calls into directly beyond issuing the invocation.

## User Stories

- As a **Tenant Admin**, I want to configure "when an Incident is created with category Theft, create a Task assigned to Investigations," without asking a developer to build it.
- As a **Tenant Admin**, I want to preview what a new rule would have done against a recent real incident before turning it on, so I don't discover a misconfiguration in production.
- As a **Tenant Admin**, I want to disable a rule that's misbehaving without losing its configuration or firing history, so I can fix and re-enable it later.
- As a **Platform Super Admin**, I want to ship a baseline rule (e.g., auto-create a Work Order from a failed Safety Inspection item) that tenants get by default and can customize or turn off.
- As a **feature developer**, I want to explicitly decide which of my feature's domain events are safe to expose as automation triggers, and what payload fields are stable enough to filter on, rather than every internal event becoming an implicit public contract.
- As a **Tenant Admin**, I want to see exactly which rules fired (or didn't, and why) for a given event, so I can debug unexpected — or missing — automated behavior.
- As a **Platform Super Admin**, I want a runaway rule cascade (rule A triggering rule B triggering rule A) to be automatically caught and disabled rather than looping indefinitely.

## Functional Requirements

### Trigger eligibility
1. Only domain events a feature has explicitly designated as **automation-eligible** (in that feature's own requirements/spec) can be used as a rule trigger; the eligible event's available payload fields for conditions are likewise explicitly documented per feature.
2. This curation prevents rules from depending on internal/incidental events whose shape might change as implementation evolves — automation-eligible events are a stable contract.

### Rule authoring
3. A rule consists of: one trigger (a specific automation-eligible event type), zero or more conditions (filters on the event's payload fields, e.g., `category = 'Theft'`, `site = 'Building A'`, `severity >= 'High'`), and one action (an invocation of a named Command/Action Bus action with mapped parameters from the event payload).
4. Tenant Admins author, edit, enable, and disable rules scoped to their tenant through a no-code trigger/condition/action builder.
5. The platform ships a set of baseline rules for common cross-module automation needs (e.g., auto-create a Work Order from a failed Safety Inspection item); tenants can enable, disable, or customize a copy of a baseline rule without affecting the platform-shipped original.

### Preview & governance
6. A rule can be run in a **dry-run/preview mode** against a sample or a recent real event, showing the condition evaluation result and what action-with-parameters it would invoke, without actually invoking the action.
7. Disabling a rule preserves its configuration and firing history, and it can be re-enabled later; deleting is a separate, explicit action.
8. Editing an existing rule creates a new version rather than silently overwriting; a rule's exact configuration at any point in time is reconstructable.
9. Rule creation, edits, enable/disable, and deletion are audit-tier events.

### Execution model
10. Rules execute as event-bus subscribers strictly after the triggering write has already committed (async, decoupled per the Event & Command Bus Architecture's delivery model) — a rule failure never rolls back or blocks the original write.
11. A rule's invoked action may itself produce a new domain event that triggers another rule (cascading); the platform enforces a maximum cascade depth and detects cycles (e.g., rule A → rule B → rule A), automatically disabling the offending rule chain and alerting Tenant/Platform Admins rather than looping indefinitely.
12. Every rule evaluation relevant to a fired trigger event is logged — which rule(s) were considered, each condition's result, which action was invoked (if any) and with what parameters, and success/failure — enabling a Tenant Admin to debug why a rule did or didn't fire.
13. A failed action invocation from a rule can be manually re-triggered by an authorized admin from the firing history, without needing to reproduce the original triggering event.

## Data Model / Fields

**Automation-Eligible Event Registration** (declared per feature)
- event_type, owning_feature
- documented_payload_fields[] (field name, type, description — the stable filterable contract)

**Rule**
- rule_id, tenant_id (null for platform-baseline), name, description
- trigger_event_type (FK → Automation-Eligible Event Registration)
- conditions[] (field, operator, value)
- action (Command/Action Bus action_id, parameter_mapping{})
- is_baseline (bool), based_on_baseline_id (nullable, if tenant-customized copy of a baseline rule)
- status (draft, active, disabled)
- version, version_history[] (version, changed_by, changed_at, diff)
- created_by, created_at

**Rule Firing Log**
- firing_id, rule_id, triggering_event_id
- conditions_evaluated[] (condition, result)
- matched (bool), action_invoked (nullable), action_result (success, failed, nullable)
- timestamp
- manually_retriggered_by (nullable), manually_retriggered_at (nullable)

**Cascade Guard State**
- chain_id, rule_sequence[] (rules fired in this cascade, in order)
- depth, status (in_progress, completed, depth_exceeded, cycle_detected)

## States & Transitions

**Rule:** `draft` (being authored/previewed, not live) → `active` (evaluates against live events) ↔ `disabled` (preserved, not evaluating, re-enablable) → `deleted` (explicit, terminal; firing history retained for audit).

**Rule Firing:** `evaluated` → `matched` (conditions passed) | `not_matched` (conditions failed, logged for debugging visibility) → (`matched` only) `action_invoked` → `succeeded` | `failed` (retriable manually per #13).

**Cascade Guard:** `in_progress` → `completed` (chain resolved normally) | `depth_exceeded` (auto-halted, alerted) | `cycle_detected` (auto-halted, offending rule(s) auto-disabled, alerted).

## Integrations

- **Event & Command Bus Architecture**: source of the underlying event stream this feature subscribes to; provides the at-least-once delivery, ordering, and dead-letter guarantees this feature builds on.
- **Command/Action Bus**: destination of every rule's action step; this feature never performs an effect itself.
- **Structured Logging & Audit Trails**: rule configuration changes and firing history are audit-tier; high-volume `not_matched` evaluations are retained in the Rule Firing Log (feature-local) rather than the platform audit store, to avoid audit-volume bloat from routine non-matches.
- **Notifications Engine**: alerts Tenant/Platform Admins on cascade depth-exceeded or cycle-detected events, and on repeated action-invocation failures.
- **Every other module**: each feature's own spec designates its automation-eligible events and documents their payload contract.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Standard User |
|---|---|---|---|
| Author/edit/enable/disable tenant rules | ✅ | ✅ (own tenant) | ❌ |
| Author/edit platform-baseline rules | ✅ | ❌ | ❌ |
| Customize a copy of a baseline rule | ✅ | ✅ (own tenant) | ❌ |
| Preview/dry-run a rule | ✅ | ✅ (own tenant) | ❌ |
| View rule firing history | ✅ | ✅ (own tenant) | ❌ (unless separately granted) |
| Manually re-trigger a failed action | ✅ | ✅ (own tenant) | ❌ |
| Designate an event as automation-eligible | ✅ (platform-wide) | ❌ | ❌ |

## Non-Functional / Constraints

- Rule evaluation must not introduce meaningful latency into the originating write's response path — it is strictly post-commit and async.
- Cascade depth limit and cycle detection are mandatory safety mechanisms, not optional hardening — an unbounded automation loop is a platform-stability risk given 211+ features' worth of potential event/action combinations.
- Dry-run/preview must never invoke a real action, including idempotent-seeming ones, to avoid surprising side effects during testing.
- Rule versioning and audit logging must satisfy the same DOE/FISMA audit-trail expectations as any other configuration change, since rules can produce real operational and compliance-relevant effects (e.g., auto-creating a task or notification).

## Acceptance Criteria

- [ ] A Tenant Admin creates a rule ("Incident Created + category=Theft → invoke create_task") and it correctly fires only for matching incidents, not for other categories.
- [ ] Dry-run mode against a real recent event shows the correct condition evaluation and intended action/parameters without actually invoking the action.
- [ ] Disabling a rule stops it from firing while preserving its configuration and history; re-enabling restores prior behavior exactly.
- [ ] Editing a rule creates a new version, and the rule's configuration at a prior point in time can be reconstructed from version history.
- [ ] A tenant can enable, customize, and independently modify a copy of a platform-baseline rule without affecting the shipped baseline or other tenants using it.
- [ ] A simulated rule cascade exceeding the configured depth limit halts automatically and alerts admins; a simulated A→B→A cycle is detected and the offending rule(s) auto-disabled.
- [ ] Firing history for a given event shows every rule considered, each condition's result, and the action outcome, sufficient to explain why a rule did or didn't fire.
- [ ] A failed action invocation can be manually re-triggered by an authorized admin from the firing history without needing to replay the original event.
- [ ] Attempting to create a rule against a non-automation-eligible event type is rejected with a clear error.

## Open Questions

- Exact default cascade depth limit — to be tuned during technical spec.
- Whether rules support multi-condition boolean logic beyond simple AND (e.g., OR groups, nested conditions) at launch, or start with AND-only and expand later — to be decided during technical spec based on early tenant feedback.
- Whether a rule can have multiple action steps (a sequence) vs. exactly one action per rule at launch — leaning toward one action per rule initially, with sequencing achievable via cascades, but to be confirmed during technical spec.
- Initial catalog of platform-baseline rules to ship at launch — to be built out incrementally as each feature (starting with Security Operations) is specified and its automation-eligible events are designated.
