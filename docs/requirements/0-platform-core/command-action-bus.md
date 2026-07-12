# Command/Action Bus

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

The Command/Action Bus is the platform's single, unified catalog of invokable actions — the canonical "what can happen" registry that every invocation surface in the platform draws from: UI buttons, context/menu actions, drag-and-drop targets, a command palette (quick-search/invoke UI), CLI-style in-app commands, and — as a future capability — a true external CLI. Domain Events rules (see [domain-events.md](domain-events.md)) invoke actions from this same catalog as their "then" step. One action, one implementation, one governance/audit model — never a separate "automated version" and "manual button version" of the same effect.

Developers register atomic actions per feature as they build it (e.g., Personnel registers `create_task`, Notifications registers `send_notification`). Tenant Admins can compose ordered sequences of existing atomic actions into named **playbooks**, which themselves register as a single invokable action — this is where multi-step automation complexity lives, keeping both Domain Events rules and simple manual invocations uniformly "invoke one action."

**Flagged for deeper future design:** the command palette and true CLI surfaces are a significant capability in their own right (search-driven quick-invoke UX, scriptable/automatable platform access) beyond what's needed to unblock this doc. This doc scopes them as invocation surfaces of the action catalog; a dedicated design pass on the command palette/CLI experience itself is expected later and is **not** fully specified here (see Open Questions).

## Actors & Roles

- **Every platform feature/module (developer)** — registers atomic actions with typed parameter schemas and discoverability metadata as part of building that feature.
- **Tenant Admin** — composes playbooks from existing atomic actions; manages compensating-action configuration for playbook steps.
- **Any platform user** — manually invokes actions via UI buttons, menus, drag-and-drop, or command palette, always under their own RBAC/ABAC identity.
- **Domain Events** — automated invoker of actions on behalf of a matched rule, under a bounded automation service identity.
- **Platform Super Admin** — manages platform-baseline playbooks and the automation service identity model.

## User Stories

- As a **Supervisor**, I want to right-click an Incident and select "Create Work Order" from a context menu, and have it be the exact same action Domain Events could have triggered automatically.
- As a **power user**, I want to open a command palette, type "create task," and invoke it without hunting through menus.
- As a **Tenant Admin**, I want to build a "New Theft Incident" playbook that creates a task, sends a notification, and updates a KPI counter in sequence, so one automation rule (or one manual click) does all three.
- As a **Tenant Admin**, I want a failed step partway through a playbook to automatically undo the steps that already succeeded, so we never end up in a half-completed state (e.g., a task created but the notification that was supposed to accompany it silently missing, with no record of the inconsistency).
- As a **feature developer**, I want to register my action once with a typed parameter schema, and have it automatically usable by Domain Events, a UI button, and the command palette without writing three separate integrations.
- As a **Guard using a manual action**, I want the system to check my own permissions when I trigger an action, exactly as it would for any other action I take — no special-cased shortcut.
- As a **Platform Super Admin**, I want automated rule-triggered actions to run under a distinctly identifiable automation actor in the audit trail, never impersonating the user who happened to trigger the originating event.

## Functional Requirements

### Action registration
1. Each feature registers its own atomic actions as part of its implementation (not tenant-authorable at the atomic level).
2. Every registered action declares: a unique identifier, a typed parameter schema (name, type, required/optional, validation rules), and discoverability metadata — human-readable name, description, icon, category, and an **applicable-context predicate** (e.g., "only available when the current context is an Incident record") that determines where it surfaces (menus, drag-drop targets, command palette results).
3. This metadata is what allows UI surfaces to derive their available actions from the registry rather than hardcoding separate lists per surface.

### Playbooks
4. Tenant Admins can compose an ordered sequence of existing atomic actions (and/or other playbooks) into a named playbook, which registers in the catalog as a single invokable action with its own discoverability metadata.
5. Playbooks follow a **full saga pattern**: each step's action may define a compensating ("undo") action; if a later step in a playbook run fails, all prior successful steps in that run are automatically compensated (rolled back) in reverse order.
6. An action without a defined compensating action can still be used in a playbook, but a playbook containing such a step is flagged at authoring time so the Tenant Admin knows that step's effects cannot be automatically undone if a later step fails.
7. Playbook run state (which steps succeeded, which failed, whether compensation ran and succeeded) is fully visible in the run's firing log.

### Execution & identity
8. Automated invocations (from a matched Domain Events rule) execute under a tenant-scoped **automation service identity**, whose effective permissions are capped at whatever the rule's creating admin held at rule-creation time — an automation can never do more than its author was allowed to do. This identity is distinctly identifiable in the audit trail as an automation actor, never impersonating the user who triggered the originating event.
9. Manual invocations — via UI button, menu, drag-and-drop, or command palette — always execute under the actual logged-in user's own RBAC/ABAC identity. There is no elevated/automation-level permission shortcut for manual invocation; if the user lacks permission, the action is denied exactly as any other platform action would be.
10. Every action invocation carries an idempotency key derived from the triggering event or request, so a retried/redelivered invocation (consistent with the underlying bus's at-least-once guarantee) does not double-execute.

### Confirmation gate (safety rail)
10a. Any manual invocation (any surface) of an action that either (a) has no defined compensating action, or (b) is flagged as requiring step-up authentication, must pass an explicit **confirmation gate** before executing — the action never runs directly off a single click/keystroke/submission for these cases. Each invocation surface renders its own confirmation UI appropriate to that surface (e.g., a confirm dialog for a UI button, an inline confirm chip for CLI-Style Input), but none may skip the gate.
10b. If the gated action also requires step-up authentication, the step-up challenge (per Authentication & Authorization) is presented as part of the same confirmation step; execution proceeds only after both the confirmation and the step-up challenge succeed.
10c. Automated invocations (Domain Events) are exempt from the confirmation gate by design — a rule was already explicitly configured and enabled by an admin, which stands in for interactive confirmation. Automated invocations of step-up-flagged actions are not permitted at all (there is no automated equivalent of an interactive MFA challenge); an attempt to register a rule targeting a step-up-required action is rejected at rule-authoring time.

### Undo history
10d. Every action invocation with a defined compensating action remains undoable — not on a fixed short timer alone, but for as long as it is still the **most recent invocation against its specific target aggregate/entity**. Performing another action against that same entity supersedes the prior invocation's undo eligibility (consistent with the ordering already required for playbook saga compensation). A platform-wide backstop maximum age also applies regardless, to prevent undoing a very old, likely-forgotten action long after the fact.
10e. Because undo eligibility is scoped per target entity rather than one global "most recent action of any kind," a user working across multiple entities (e.g., a Dispatcher managing several concurrent incidents) can undo the last action taken on any specific entity independently, without needing to first undo more recent actions taken on other, unrelated entities.
10f. Each invocation surface exposes undo with its own UI conventions (e.g., a fast toast-based affordance for the single most recent action, plus a longer-lived per-entity affordance for revisiting a specific entity later) built on this same underlying eligibility model — there is one undo mechanism, multiple surface presentations of it.

### Parameters across invocation surfaces
11. Parameter values are sourced differently by invocation surface but validated against the same typed schema regardless of source: Domain Events maps parameters from the triggering event's payload; manual UI/menu/drag-drop invocations collect them via a generated form or the invocation context (e.g., the record that was right-clicked); command palette/CLI invocations parse them from typed arguments.
12. A parameter may declare a **default resolver**: a lookup against another feature's data (e.g., "this site's scheduled default patrol route for the current period") that fires only when no other source supplies a value for that parameter. This lets a parameter be optional in practice — either explicitly provided, or intelligently defaulted — without being loosely typed or unvalidated; a resolved default still passes through the same validation rules as an explicitly provided value.
13. Final parameter resolution precedence, highest to lowest, applies uniformly across every invocation surface: **explicitly provided value** (typed flag, form field, event-payload mapping) → **invocation context** (e.g., a CLI active context slot or a record the action was invoked from) → **alias or playbook template value** (a fixed/passthrough value baked into an alias expansion or playbook step mapping) → **the parameter's own default resolver**. A parameter that remains unresolved after all four sources and is marked required fails validation.

## Data Model / Fields

**Registered Action**
- action_id, owning_feature, name, description, icon, category
- parameter_schema[] (name, type, required, validation_rules, default_resolver_ref (nullable))
- applicable_context_predicate
- compensating_action_id (nullable)
- is_playbook (bool)

**Parameter Default Resolver**
- resolver_ref, owning_feature, description
- lookup_logic (e.g., "site's scheduled default route for the current shift period")
- applies_to_parameter (action_id, parameter_name)

**Playbook** (a composite Registered Action)
- action_id (same as Registered Action), tenant_id
- steps[] (ordered: action_id, parameter_mapping, compensating_action_id override if any)
- created_by, created_at, version, version_history[]

**Action Invocation**
- invocation_id, action_id, idempotency_key
- invoked_by (account_id for manual, automation_identity_ref for automated)
- invocation_surface (ui_button, menu, drag_drop, command_palette, cli, domain_event_rule)
- target_aggregate_ref (the entity this invocation acted on, for undo-eligibility scoping)
- confirmation_gated (bool), confirmed_at (nullable), step_up_verified_at (nullable)
- parameters (resolved, validated), result (success, failed), timestamp
- undo_status (available, superseded, expired, undone)

**Playbook Run**
- run_id, playbook_action_id, triggering_invocation_id
- step_results[] (step, invocation_id, result, compensated: bool)
- overall_status (completed, halted_and_compensated, halted_compensation_failed)

**Automation Service Identity** (per tenant)
- identity_id, tenant_id
- effective_permission_cap (derived from rule creators' grants at creation time)

## States & Transitions

**Action Invocation:** `requested` → `authorized` (RBAC/ABAC check per invoker identity type) → (manual + gated only) `pending_confirmation` → (+ step-up if flagged) `pending_step_up` → `confirmed` → `executing` → `succeeded` | `failed`. A `pending_confirmation`/`pending_step_up` invocation that's abandoned (dialog dismissed, chip not confirmed) never executes and is discarded, not retried.

**Action Invocation (undo eligibility, succeeded invocations with a compensating action only):** `undo_available` → `undone` (compensating action invoked) | `superseded` (a newer invocation against the same target_aggregate_ref occurs) | `expired` (platform backstop age reached).

**Playbook Run:** `running` → `completed` (all steps succeeded) | `halted` (a step failed) → `compensating` → `halted_and_compensated` (rollback succeeded) | `halted_compensation_failed` (rollback itself failed — surfaced urgently to admins, since this is the one state representing genuine inconsistency).

## Integrations

- **Domain Events**: the exclusive automated invoker of this catalog; every rule's action step is a Command/Action Bus invocation.
- **Event & Command Bus Architecture**: action invocations are themselves dispatched as commands through the underlying command bus/mediator, inheriting its authorization-gating and delivery guarantees.
- **Authentication & Authorization**: source of both the manual-invocation RBAC/ABAC checks and the automation service identity's capped permission model.
- **Structured Logging & Audit Trails**: every invocation (manual or automated), playbook run, and compensation event is audit-tier given the real cross-module effects actions can produce.
- **Notifications Engine**: alerts on playbook compensation failure (the one genuinely urgent failure mode) and repeated action failures.
- **Every other module**: each feature registers its own atomic actions; this catalog is additive as features are built, not something specified once and closed.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Standard User |
|---|---|---|---|
| Register an atomic action (developer-level, via feature build) | ✅ | ❌ | ❌ |
| Compose/edit a playbook | ✅ | ✅ (own tenant) | ❌ |
| Manually invoke an action they hold permission for | ✅ | ✅ | ✅ (per own RBAC/ABAC) |
| View invocation/playbook run history | ✅ | ✅ (own tenant) | ✅ (own invocations) |
| Configure automation service identity permission cap | ✅ | ✅ (own tenant, bounded by rule creators' own grants) | ❌ |

## Non-Functional / Constraints

- Idempotency must hold under the bus's at-least-once delivery guarantee for every action, not just a subset — a double-executed `create_task` or `send_notification` is a correctness bug, not an edge case.
- Compensation (saga rollback) must be reliable enough that `halted_compensation_failed` is rare and, when it happens, is treated as an urgent operational incident, not a routine log entry.
- Discoverability metadata must be complete enough that command palette search and CLI help text can be fully auto-generated from the registry with no per-surface hardcoded duplication.
- Manual invocation authorization must go through the exact same RBAC/ABAC path as any other platform action — no bypass, ever, regardless of invocation surface (button vs palette vs future CLI).
- The confirmation gate for non-reversible/step-up-flagged actions must be enforced server-side (the server rejects execution without a recorded confirmation), never a client-only UI affordance that a malformed or malicious client request could skip.
- .NET/ASP.NET Core implementation, consistent with the platform-wide backend decision (see [_DECISIONS.md](../_DECISIONS.md)).

## Acceptance Criteria

- [ ] The same registered action, invoked via a UI button and via a Domain Events rule, produces identical results and an identical audit trail shape (differing only in invoker identity type).
- [ ] A Tenant Admin composes a 3-step playbook; running it end-to-end succeeds and all three steps' effects are visible.
- [ ] A playbook step fails partway through; all prior successful steps in that run are automatically compensated, and the run's final status reflects `halted_and_compensated`.
- [ ] An action invoked twice with the same idempotency key (simulated redelivery) executes its effect only once.
- [ ] A Domain Events-triggered action executes under a clearly distinct automation identity in the audit trail, never appearing as the user who triggered the originating event.
- [ ] A manual invocation by a user lacking the required permission is denied, identically to any other permission-denied action in the platform.
- [ ] An action's discoverability metadata correctly determines whether it appears in a context menu for a given record type (applicable-context predicate enforced).
- [ ] A playbook step without a defined compensating action is flagged as non-reversible at authoring time.
- [ ] A parameter with a default resolver, left unsupplied by every other source, correctly resolves via its lookup logic and passes the same validation as an explicit value.
- [ ] When a parameter is supplied by more than one source simultaneously (e.g., both context and an alias template value), the highest-precedence source (explicit > context > alias/playbook template > default resolver) wins, verified across at least one conflicting pair.
- [ ] A manual invocation of an action with no compensating action is blocked from executing until the confirmation gate is explicitly passed; abandoning the confirmation never executes the action.
- [ ] A manual invocation of a step-up-flagged action presents the step-up challenge as part of the confirmation step and only executes after both confirmation and step-up succeed.
- [ ] An attempt to register a Domain Events rule targeting a step-up-required action is rejected at authoring time.
- [ ] Performing a second action against the same target entity marks the first action's undo eligibility as `superseded`, even though the first action's own undo window (if a separate time-based surface convention exists) hasn't elapsed.
- [ ] A user can undo the most recent action against Entity A without first needing to undo more recent actions taken against unrelated Entity B.
- [ ] An undo attempt against an invocation past the platform backstop age is rejected, even if no superseding action occurred on that entity.

## Open Questions

- **Command palette and CLI UX design** — explicitly flagged by the user as a significant feature warranting its own deeper design discussion (search/ranking behavior, keyboard-driven workflows, scripting/argument syntax for a future true CLI, whether the CLI is local-only or remotely scriptable). This doc scopes them only as invocation surfaces of the action catalog; the experience design itself is deferred.
- Exact automation service identity permission-cap recomputation policy — if a rule creator's own grants are later reduced, does the automation identity's cap shrink retroactively, or only for rules created after the change? To be decided during technical spec.
- Whether playbooks can be nested (a playbook step invoking another playbook) — likely yes given "steps[] ... action_id" doesn't structurally forbid it, but needs the same cascade-depth/cycle-detection treatment as Domain Events; to be confirmed during technical spec.
- Drag-and-drop invocation specifics (which record-onto-record combinations map to which actions) — to be defined per-feature as drag-and-drop interactions are specced in individual feature UI work.
- Default resolvers are registered by developers per feature (mirroring atomic action registration) — exact catalog of default resolvers is built out incrementally as each feature is specified; e.g. Patrol Management would register the "site's scheduled default route" resolver referenced by CLI-Style Input's alias example.
- Exact platform backstop age for undo eligibility (item 10d) — to be set during technical spec; likely on the order of hours, not days, but needs a concrete value.
- Whether Command Palette's confirmation dialog and CLI-Style Input's confirmation chip need to look/feel identical for consistency, or can be surface-appropriate — leaning toward surface-appropriate (each doc renders its own), to be confirmed during UI/UX technical spec.
