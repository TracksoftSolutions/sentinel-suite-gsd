# CLI-Style Input

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

CLI-Style Input is an in-app, typed-command-syntax bar for power users who prefer typing structured commands over navigating menus — **not** a standalone external CLI tool. It runs entirely inside the already-authenticated web/mobile session (no separate credentialed binary), using a predictable `verb noun --flag value` syntax that mirrors each action's Command/Action Bus parameter schema, with live autocomplete, session command history, tenant/site/user-configurable parameterized aliases, and **chained commands with implicit context threading** — a single input line can run multiple commands separated by `;`, where later commands automatically operate against entities created/touched by earlier ones, without repeating identifiers. It is a distinct, separately-triggered component from the Command Palette (see [command-palette.md](command-palette.md)) — fuzzy search/navigation lives in the palette; typed structured commands live here.

For example: `create-incident fire --location B-105 --priority 1; assign-unit U-01 --role IncCom` creates a fire incident, then assigns unit U-01 to it as Incident Commander — the second command never has to name the incident explicitly, because creating it made it the active incident context for the rest of the chain.

A true external, installable/scriptable CLI remains a separate, deferred future possibility (noted in [command-action-bus.md](command-action-bus.md)'s open questions) and is explicitly **not** what this doc specifies.

Beyond the core syntax, this doc also specifies four capabilities intended to make this a standout, differentiating feature rather than merely a functional power-user tool: an explicitly-toggled **AI-assist mode** that translates loose natural language into a proposed structured command for confirm/edit (never blended into strict-mode parsing, so it can never misinterpret a power user's precise alias or syntax); **proactive alias suggestion**, where the system notices a repeated command pattern and offers to save it as an alias; **alias sharing and promotion**, letting a personal alias grow into a team library entry and, at an admin's discretion, a site/tenant default; and a **per-entity undo history** that lets a Dispatcher juggling several concurrent incidents undo the last action on any one of them independently, rather than being stuck behind a single global "most recent action" undo slot.

**Explicitly considered and rejected:** using physical-world triggers (e.g., an NFC/QR checkpoint scan) to seed CLI active context. Checkpoint scanning happens on a guard's phone in the field, which isn't a natural context for a typed command-syntax tool — this doc's context-seeding mechanisms (chained commands, screen-ambient) remain desktop-power-user-oriented.

## Actors & Roles

- **Power user (any role)** — opens the CLI-style input via its own keyboard shortcut, types commands, uses/creates aliases.
- **Tenant Admin** — defines tenant-wide and site-wide alias defaults.
- **Site Admin** — defines site-scoped aliases (narrower than tenant, overridable by user).
- **Any user** — defines personal aliases, which always win over site/tenant aliases of the same name.
- **Every platform feature/module** — contributes its registered actions' typed parameter schemas, which drive command syntax validation and autocomplete.
- **Team** (an existing organizational grouping, e.g., a squad/shift roster) — scope for shared alias libraries, sitting between personal and site-level aliases for sharing (not resolution) purposes.

## User Stories

- As a **power-user Supervisor**, I want to type `create task --title "Fix hallway light" --assignee jdoe --site BuildingA` and have it execute immediately, faster than clicking through a form.
- As a **user**, I want autocomplete to suggest valid flags and values as I type, so I don't need to memorize exact syntax.
- As a **user**, I want my command history available via up-arrow within my session, so I can quickly repeat or tweak a recent command.
- As a **Tenant Admin**, I want to define an org-wide alias `ct` for `create task --assignee me`, so every user in our tenant gets the shortcut by default.
- As a **user**, I want to define my own personal alias that overrides the tenant default if I want different behavior, and have my version always win for me.
- As a **user who fat-fingered a command**, I want an Undo option on the result toast for a few seconds after execution, so a miskeyed action doesn't require a support ticket to fix.
- As a **user**, I want to see a scrolling transcript of the commands I've run this session and their results, so I have a running record without re-running anything.
- As a **Dispatcher**, I want to type `create-incident fire --location B-105 --priority 1; assign-unit U-01 --role IncCom` as one line and have the unit assigned to the incident I just created, without having to know or type its new incident number.
- As a **user**, I want the incident I just created to stay "active" after I hit Enter, so my next command (typed separately, not chained) still applies to it without re-specifying it.
- As a **user**, I want to explicitly clear my active context with a quick double-Enter when I'm done working on a record, so stale context doesn't silently apply to my next unrelated command.
- As a **user** viewing an incident's page, I want opening the CLI input to automatically start with that incident as context, so I don't have to re-select it just because I want to type a quick command.
- As a **user**, I want to see which contexts (incident, unit, etc.) are currently active as small chips near the input, so I always know what my next command will implicitly apply to.
- As a **Supervisor at ESIF**, I want an alias `esif <unit>` that expands to creating tonight's patrol and assigning a unit to it, so `esif U-01` does in one line what would otherwise take a full chained command with an explicit route flag.
- As a **Supervisor**, I want the patrol's route to resolve automatically from our site's scheduled default when I don't specify one, so my alias doesn't need to hardcode a route that might change between schedule periods.
- As a **power user**, I want to type `esif U-01 --priority 2` and have the extra flag apply to the right step of the expanded chain, so I can tweak an alias's behavior without defining a whole new alias.
- As a **user unsure of exact syntax**, I want to toggle into AI-assist mode and type "assign U-01 to the fire at B-105 as incident commander," get shown the equivalent structured command, and confirm it before it runs.
- As a **power user**, I want AI-assist mode to be something I explicitly opt into per entry, so it never silently reinterprets my precise alias or structured command when I'm not asking it to.
- As a **user**, I want an optional voice input inside AI-assist mode for the rare moment my hands are free but typing isn't convenient, without it being a primary workflow I'm pushed toward.
- As a **user**, I want the system to notice I've typed the same 3-command sequence four times this week and offer to save it as an alias, so I don't have to think to build the alias myself.
- As a **user**, I want to share a useful personal alias to my team's library so others can adopt it, without it silently changing anyone else's behavior until they choose to.
- As a **Site Admin**, I want to promote a popular team alias to our site's default set, so every guard at our site gets it automatically going forward.
- As a **user about to run a non-reversible command** (e.g., destroying a piece of evidence), I want an explicit confirmation step before it executes, not just a 5-second Undo window after the fact, since some things genuinely can't be undone.
- As a **user running a command that also requires step-up authentication mid-chain**, I want the chain to pause for my MFA challenge at exactly that point and continue automatically once I pass it, rather than having to restart the whole chain.
- As a **Dispatcher managing six active incidents**, I want to undo my last miskeyed action on Incident 3 specifically, without needing to first undo more recent actions I correctly took on Incidents 4, 5, and 6.

## Functional Requirements

### Syntax & execution
1. Commands follow a structured `verb noun --flag value [--flag value ...]` syntax, directly mirroring the invoked action's typed Command/Action Bus parameter schema — no natural-language parsing.
2. Live autocomplete suggests the next valid token as the user types: first the available action (matching registered action names/aliases applicable to context and permission), then that action's flags, then valid values where enumerable (e.g., site names, usernames), sourced from the same discoverability metadata and parameter schema defined in Command/Action Bus.
3. Submitted commands are validated against the action's parameter schema before execution; validation errors are shown inline without executing.
4. Commands execute under the invoking user's own RBAC/ABAC identity, per the Command/Action Bus's manual-invocation model — identical authorization path to any other manual invocation (palette, button, menu).

### History & aliases
5. Command history within the current session is recallable via up/down arrow navigation.
6. Users, Site Admins, and Tenant Admins can each define **parameterized aliases** — a short name that expands to one or more chained commands with argument passthrough, not just a fixed macro. An alias's expansion template can itself contain a full `;`-separated chain (e.g., `esif <unit>` expanding to `create-patrol Esif --route esif-night; assign-unit <unit>`), and can define **multiple distinct named placeholders** mapped to different positions across that chain, not just a single trailing argument.
6a. Any flags typed after an alias invocation's own defined placeholders are consumed are appended to the **last command** in the alias's expanded chain (e.g., `esif U-01 --priority 2` appends `--priority 2` to the `assign-unit` step) — letting a power user extend or override an alias ad hoc without authoring a new alias for every variation.
6b. An alias template value is not the only way a parameter can go unspecified and still resolve: per the Command/Action Bus parameter resolution model, final precedence across all sources is **explicit flag > active CLI context > alias/playbook template value > the action's own default resolver**. This is what lets `esif U-01` omit a route entirely — the alias doesn't hardcode `--route`, and `assign-patrol`'s route parameter resolves via its registered default resolver (e.g., "this site's scheduled default route for the current shift period") since no explicit flag, context, or alias value supplied one.
7. Alias scope hierarchy: tenant-level (broadest) → site-level → user-level (narrowest). On a name conflict, the narrowest defined scope wins for a given user — a user's own alias always overrides a site or tenant alias of the same name, and a site alias overrides a tenant alias for users at that site.
8. The platform ships sensible default aliases for common actions, keeping the system opinionated out of the box while remaining fully overridable — the goal is to get out of a power user's way, not force a particular workflow.

### Chaining & context
5a. Multiple commands can be typed on one line separated by `;`; they execute in order within a single submission.
5b. The platform maintains **type-aware active context**: a separate "current entity" slot per entity type (current incident, current unit, current person, etc.), not a single most-recent-wins slot — touching a unit doesn't bump an active incident out of context.
5c. A command that creates or resolves an entity (e.g., `create-incident`) sets that entity as the active context for its type; a subsequent command in the same chain (or a later, separately-typed command) that accepts a parameter of that type and doesn't specify it explicitly uses the active context automatically.
5d. Context is also **ambiently seeded** from the current screen: opening CLI-style input while viewing a specific record's page (e.g., an incident detail page) automatically starts that record as the active context for its type, without any command having been typed.
5e. An explicit flag in a command always overrides the active context for that parameter, even if it conflicts with it (e.g., `--incident INC-9999` wins over whatever incident is currently active) — typing something explicit is always a deliberate, unambiguous override.
5f. Pressing **Enter once** executes the current input's command(s) and leaves active context as whatever it now is (updated per 5c) — ready for the next command to use implicitly. Pressing **Enter twice in rapid succession** (a double-Enter gesture) executes the input and then clears all active context slots in the same motion.
5g. Active context slots are shown as small, individually removable chips near the input — each can be cleared independently (without waiting for or requiring the double-Enter clear-all gesture), and the chips are the authoritative visual indicator of what the next command will implicitly apply to.
5h. If a command partway through a `;`-chain fails, execution halts — remaining commands in that chain do not run. Already-succeeded commands earlier in the chain are **not** automatically rolled back (unlike a formally pre-authored Command/Action Bus playbook's full saga behavior); each succeeded step remains individually undoable via its own Undo affordance if its action has a compensating action. Ad hoc chains are intentionally lighter-weight than authored playbooks.

### Output & feedback
9. Executed commands append to a persistent, scrollable **session transcript** (command + result), not a single ephemeral view — the user can scroll back through everything they've run this session.
10. Each result also surfaces as an inline ephemeral confirmation and, per the platform's Notifications Engine toast conventions, a toast notification.
11. For actions with a defined compensating action, the toast includes a fast **Undo** button active for a short window (on the order of 5 seconds) as a convenience affordance for the single most recent action. This sits on top of the underlying per-entity **Undo history** mechanism defined in Command/Action Bus (§ Undo history): each active context chip (per 5g) additionally exposes "undo last action on this entity," valid for as long as that invocation remains the most recent one against that specific entity (not just the 5-second window), letting a user revisit and undo the last action on any entity they're still working with — e.g., a Dispatcher juggling several incidents can undo Incident 3's last action without touching Incidents 4–6. Actions without a compensating action show no Undo option anywhere, consistent with the non-reversible flag established in Command/Action Bus.
12. Commands whose natural result is "go look at the thing" (e.g., a create-type command) auto-navigate the user to the resulting record after execution, in addition to the transcript/toast feedback.

### Safety rail: confirmation gate
12a. Per Command/Action Bus's confirmation gate, a command whose action has no compensating action, or is flagged for step-up authentication, does not execute directly on submission. Instead, an inline **confirmation chip** ("Confirm: destroy-evidence EV-2291?") appears in place of/alongside the result, requiring an explicit click or Enter-on-the-chip before it runs. Typing the command alone is never sufficient for these.
12b. If the gated command also requires step-up authentication, the standard step-up MFA challenge is presented inline as part of confirming the chip.
12c. When a step-up-gated command appears mid-`;`-chain, the chain **pauses** at that exact point once earlier commands have executed, shows the confirmation chip and step-up challenge, and — on success — automatically resumes and executes the remaining commands in the chain. On cancel or step-up failure, the chain halts at that point per the existing no-rollback failure behavior (5h) — already-succeeded earlier steps stand.

### AI-assist mode
12d. A dedicated, explicit trigger (e.g., a leading `?` character) switches a given input entry into **AI-assist mode**; without it, input is always parsed strictly as structured syntax/aliases, exactly as specified above — AI-assist mode never silently activates or reinterprets a normally-typed command.
12e. In AI-assist mode, free-form natural language is translated into a proposed structured command (with resolved parameters, drawing on the same active-context and default-resolver model as normal commands) and displayed for the user to review, edit, and explicitly confirm — it is never auto-executed straight from the translation, regardless of the gate/step-up status of the underlying action.
12f. AI-assist mode supports an optional voice-input affordance (microphone toggle) as an additional way to fill the natural-language entry, reusing the `voice_transcription` AI context established by AI-Assisted Incident Report Writing and resolved through AI/LLM Services. This is a minor, off-by-default convenience — not a primary field-work workflow, since hands-free scenarios in the field are typically already served by radio.
12g. Tenant Admins can disable AI-assist mode entirely for their tenant, consistent with the platform's general approach of letting each tenant configure capabilities to their own security/compliance posture.

### Proactive alias suggestion
12h. The platform tracks each user's recent command sequences (within and across sessions) and, on detecting a repeated multi-command pattern above a defined threshold, surfaces an inline, dismissible suggestion offering to save it as a named personal alias with the varying parts identified as placeholders.
12i. Accepting the suggestion opens the standard personal alias creation flow pre-filled with the detected pattern; dismissing it does not ask again for that exact pattern (a "don't suggest this again" implicit mute), though a materially different repeated pattern can still trigger a new suggestion.

### Alias sharing & promotion
12j. A user can **share** a personal alias to their Team's alias library — a browsable, opt-in list visible to team members, who may individually adopt (copy into their own personal aliases) a shared entry. Sharing never changes another user's behavior automatically; it only makes the alias discoverable.
12k. A Site Admin or Tenant Admin can separately **promote** a shared alias — their own or one browsed from a team library — into a site- or tenant-scope alias, which per the existing scope/precedence rules (item 7) then applies by default to everyone in that scope (unless they've defined their own narrower override).

## Data Model / Fields

**Alias**
- alias_id, scope (tenant, site, user), scope_ref (tenant_id, site_id, or account_id)
- name, expansion_template (a full `;`-chainable command string with one or more named `<param>` placeholders mapped to any position across the chain)
- extra_args_target (which command in the expansion chain receives flags typed beyond the alias's own placeholders — defaults to the last command)
- created_by, created_at
- is_platform_default (bool)
- extended_fields (JSONB, nullable — registers Alias as a carrier for [Tenant-Defined Types & Custom Fields](tenant-defined-types-custom-fields.md); governance/validation owned by that feature, not here)

**Command Execution** (session-local, feeds into the standard Action Invocation audit record from Command/Action Bus)
- transcript_entry_id, session_id, chain_id (groups commands submitted together on one line), chain_position
- raw_input, resolved_action_id, resolved_parameters, context_params_used[] (which parameters were filled from active context vs typed explicitly)
- was_ai_assisted (bool), ai_translation_source_text (nullable, the original natural-language/voice input if AI-assisted)
- confirmation_gated (bool), confirmed_at (nullable) — mirrors Command/Action Bus's Action Invocation fields
- result, invocation_ref (FK → Command/Action Bus Action Invocation, source of undo_status/target_aggregate_ref)
- timestamp

**Active Context Slot** (session-local, client + server session state)
- session_id, entity_type, entity_ref
- source (ambient_from_screen, set_by_command), set_at
- last_invocation_ref (the most recent Action Invocation against this entity, for the chip's "undo last action on this entity" affordance)

**Alias Suggestion** (system-detected, per user)
- suggestion_id, account_id, detected_pattern (command sequence with identified placeholders), occurrence_count
- status (suggested, accepted, dismissed)

**Alias Share**
- share_id, source_alias_id, team_ref, shared_by, shared_at
- adopted_by[] (account_id, adopted_at) — users who've individually copied it into their own personal aliases

**Alias Promotion**
- promotion_id, source_alias_id (personal or team-shared), promoted_to_scope (site, tenant), promoted_scope_ref
- promoted_by, promoted_at

## States & Transitions

**Command Execution:** `typed` (or `ai_translated` if AI-assist mode) → `validating` (context params resolved per 5c, explicit flags applied per 5e) → `valid` | `invalid` (inline error, not executed) → (`valid`, gated only) `pending_confirmation` → (+ step-up if flagged) `pending_step_up` → `confirmed` → (`valid`/`confirmed` only) `executing` → `succeeded` (undo eligibility per Command/Action Bus's Undo history model; active context slots updated) | `failed` (chain halts here per 5h, no rollback of prior chain steps). An abandoned `pending_confirmation`/`pending_step_up` command never executes.

**AI-assist translation:** `listening` (if voice) | `typing` → `translating` → `proposed` (shown for review) → `confirmed` (proceeds into standard Command Execution flow above) | `edited` (user adjusts before confirming) | `discarded`.

**Alias resolution:** `input_parsed` → `checking_user_scope` → `checking_site_scope` (if no user match) → `checking_tenant_scope` (if no site match) → `resolved` | `unresolved` (treated as a literal action name attempt).

**Active Context Slot:** `unset` → `ambient` (seeded from current screen) | `explicit` (set by a command's result) → `cleared` (individual chip dismissal or double-Enter clear-all).

**Alias Suggestion:** `tracking` (occurrence count incrementing) → `suggested` (threshold reached) → `accepted` (alias created) | `dismissed` (muted for that exact pattern).

**Alias Share/Promotion:** `personal` → `shared` (visible in team library, others can `adopted`) → (independently) `promoted` (site/tenant scope registration created by an admin, per Command/Action Bus's alias/action registration model).

## Integrations

- **Command/Action Bus**: source of every invokable action, its parameter schema (including default resolvers), its compensating action, and the confirmation gate and per-entity Undo history mechanisms this feature's chips/toasts render; this feature is purely an alternate invocation/authoring surface over that catalog. The four-tier parameter resolution precedence (explicit > context > alias/playbook template > default resolver) is defined once in Command/Action Bus and applies here without modification.
- **Authentication & Authorization**: RBAC/ABAC enforcement identical to any other manual invocation, and the step-up MFA challenge presented inline during gated confirmation.
- **Notifications Engine**: toast delivery conventions for command results and the fast Undo affordance.
- **Command Palette**: sibling surface, distinct trigger, no shared UI state — a user may use either or both.
- **Settings & Preferences**: personal alias management, AI-assist mode preference, and (where enabled) voice input toggle surface alongside other user preference settings.
- **AI-Assisted Incident Report Writing** (Security Operations): establishes the `voice_transcription` AI context reused by AI-assist mode's optional voice input, rather than this feature building its own.
- **AI/LLM Services**: actual owner of the provider abstraction (SaaS-pooled or BYO API key), Prompt Templates, and Custom Instructions that both the `voice_transcription` and this feature's own `cli_assist_translation` AI contexts resolve against — AI-assist mode declares its context and placeholders, never talks to a provider or assembles a prompt directly.
- **Tenant-Defined Types & Custom Fields**: Alias registers as one of the first two non-entity carriers proving that feature's mechanism generalizes beyond Entity Registry Core.

## Non-Goals

- A checkpoint scan (NFC/QR) setting CLI active context — considered and explicitly rejected; mobile field scanning isn't a natural CLI-Style Input context. See Overview.
- Voice as a primary field-work input method — it exists only as a minor, off-by-default convenience inside AI-assist mode, not a designed-for workflow, since radio already serves most hands-free field scenarios.
- AI-assist mode ever auto-executing a translated command without explicit user confirmation, or activating implicitly within strict-mode input.

## Permissions

| Action | Platform Super Admin | Tenant Admin | Site Admin | Standard User |
|---|---|---|---|---|
| Define tenant-level default aliases | ✅ | ✅ (own tenant) | ❌ | ❌ |
| Define site-level aliases | ✅ | ✅ | ✅ (own site) | ❌ |
| Define personal aliases | ✅ | ✅ | ✅ | ✅ |
| Execute a command they hold permission for | ✅ | ✅ | ✅ | ✅ (per own RBAC/ABAC) |
| Invoke Undo (fast window or per-entity) | ✅ | ✅ | ✅ | ✅ (own execution) |
| Share a personal alias to a team library | ✅ | ✅ | ✅ | ✅ (own aliases) |
| Adopt a shared team alias | ✅ | ✅ | ✅ | ✅ |
| Promote an alias to site/tenant scope | ✅ | ✅ (own tenant) | ✅ (own site) | ❌ |
| Disable AI-assist mode tenant-wide | ✅ | ✅ (own tenant) | ❌ | ❌ |
| Use AI-assist mode / voice input (where enabled) | ✅ | ✅ | ✅ | ✅ |

## Non-Functional / Constraints

- Autocomplete must respond fast enough to feel like typing, not like waiting for a network round-trip — locally cached action/parameter metadata, not a live query per keystroke.
- Alias resolution order (user → site → tenant) must be deterministic and fast, evaluated client-side against a synced alias set where feasible.
- The fast Undo window must be short and clearly time-bound in the UI (visible countdown); the per-entity Undo history is longer-lived but still clearly scoped to "the last action on this specific entity," not an unbounded revert — both map to a real compensating-action invocation, never a client-side-only visual revert.
- The confirmation gate (12a) must be enforced server-side per Command/Action Bus's non-functional requirement — the chip is a UI affordance for a check the server performs regardless.
- AI-assist mode's translation step must never be able to bypass the confirmation gate or step-up requirement of the action it resolves to — translation only proposes; the same execution path (including gating) applies as if the user had typed the structured command directly.
- Voice transcription (where used) must run through the same `voice_transcription` AI context (AI/LLM Services) as AI-Assisted Incident Report Writing, not a separately built/maintained speech-to-text integration.
- Must degrade gracefully offline (mobile): locally queued/offline-capable actions (per Offline Data Sync) remain invocable via CLI-style input using the same offline queuing mechanics as any other invocation surface; the transcript reflects pending-sync state clearly.
- Active context slots must resolve entirely client-side against already-available data (the just-created/just-touched entity, or the ambient screen record) — never a server round-trip just to figure out what "the current incident" refers to, keeping chained commands fast.
- Double-Enter detection needs a sensible, non-frustrating timing window (fast enough to be deliberate, not so fast it's unreliable for typical typing speed) — a specific threshold is a technical-spec/UX-testing decision, not fixed here.
- WCAG 2.1 / Section 508 accessible: fully keyboard-operable by definition (it's a text input), with accessible autocomplete suggestion navigation and result announcements.

## Acceptance Criteria

- [ ] Typing a valid structured command and submitting it executes the correct action with correctly parsed parameters.
- [ ] Autocomplete correctly suggests valid next tokens (actions, then flags, then enumerable values) as the user types.
- [ ] An invalid command (bad flag, missing required parameter) shows an inline validation error and does not execute.
- [ ] A user's personal alias overrides a site alias of the same name; a site alias overrides a tenant alias, for users in that site.
- [ ] A parameterized alias correctly splices a typed argument into its expansion template before execution.
- [ ] The session transcript retains all commands and results run in the current session, scrollable, without clearing on each new command.
- [ ] A command whose action has a compensating action shows an Undo button on its result toast; invoking Undo within the window correctly reverses the effect via the compensating action.
- [ ] An action without a compensating action shows no Undo option.
- [ ] A create-type command auto-navigates to the resulting record after execution.
- [ ] A command executed via CLI-style input by a user lacking the required permission is denied identically to the same action attempted via a UI button.
- [ ] `create-incident fire --location B-105 --priority 1; assign-unit U-01 --role IncCom` creates the incident and correctly assigns the unit to that specific newly-created incident without the incident being named explicitly in the second command.
- [ ] After a single Enter submission, the resulting entity remains active context; a subsequent, separately-typed command that omits that parameter still correctly applies to it.
- [ ] A rapid double-Enter both executes the pending input and clears all active context slots afterward.
- [ ] Opening CLI-style input while viewing a specific record's page starts with that record as active context for its type, visible as a chip, without any command having been typed.
- [ ] An explicit flag in a command overrides active context for that parameter even when they conflict, with no error or ambiguity.
- [ ] Touching a unit via a command does not clear an already-active incident context slot — both remain independently active and visible as separate chips.
- [ ] A chain where the second of three commands fails halts before the third runs, while the first command's effect remains in place (not rolled back) and is individually undoable via its own Undo affordance.
- [ ] Dismissing a single context chip clears only that entity type's context, leaving other active context slots untouched.
- [ ] An alias with a multi-command expansion template and one placeholder (e.g., `esif <unit>` → `create-patrol Esif --route esif-night; assign-unit <unit>`) correctly expands and executes both commands with the typed argument spliced into the right position.
- [ ] An alias expansion that omits a parameter with a registered default resolver (e.g., no `--route` in the template) correctly resolves that parameter via its default resolver at execution time.
- [ ] Typing an alias invocation with an extra flag beyond its defined placeholders (e.g., `esif U-01 --priority 2`) correctly appends that flag to the alias's designated extra-args target command.
- [ ] An alias with multiple distinct named placeholders correctly maps each typed argument to its corresponding position across the expanded chain.
- [ ] Typing a normal structured command never triggers AI translation, even if it superficially resembles natural language.
- [ ] Prefixing input with the AI-assist trigger translates natural language into a proposed structured command that requires explicit confirmation before executing; editing the proposal before confirming changes what actually runs.
- [ ] A Tenant Admin disabling AI-assist mode removes the trigger/toggle for all users in that tenant.
- [ ] Voice input inside AI-assist mode correctly transcribes speech into the natural-language field without executing anything until the standard AI-mode confirm step.
- [ ] Running the same 3-command sequence a defined number of times triggers an inline alias-save suggestion; dismissing it does not re-suggest that exact pattern again, while accepting it opens a pre-filled alias creation flow.
- [ ] Sharing a personal alias to a team library makes it visible/adoptable to teammates without changing their behavior until they individually adopt it.
- [ ] A Site Admin promoting a team-shared alias to site scope causes it to apply by default to site users who haven't defined their own narrower override.
- [ ] Submitting a command with no compensating action shows a confirmation chip and does not execute until it's explicitly confirmed; abandoning the chip leaves the action un-run.
- [ ] Submitting a step-up-flagged command mid-chain pauses the chain after prior steps complete, presents the step-up challenge inline, and resumes the remaining chain automatically on success.
- [ ] Canceling a step-up challenge mid-chain halts the chain at that point without rolling back already-succeeded earlier steps.
- [ ] With active context chips for Incident 3 and Incident 5 both present, undoing "last action on Incident 3" via its chip succeeds and does not affect Incident 5's most recent action or its own undo eligibility.
- [ ] Performing a new action on Incident 3 after undoing supersedes further undo eligibility for the now-undone prior action (it can't be "re-undone").

## Open Questions

- Exact default keyboard shortcut for opening CLI-style input, distinct from the Command Palette's shortcut — to be finalized during UI/UX technical spec.
- Whether the session transcript persists across page reloads within a session or resets — leaning toward session-scoped-only (resets on reload) at launch, to be confirmed during technical spec.
- Full list of platform-shipped default aliases — to be built out incrementally as each feature's actions are specified.
- Whether a true external, scriptable CLI is ever built as a separate future capability remains an open, deferred question (see [command-action-bus.md](command-action-bus.md)) — not part of this doc's scope.
- Exact double-Enter timing threshold — to be tuned during UX testing in technical spec.
- Whether active context slots persist across a page navigation that isn't itself a context-seeding record view (e.g., navigating to a settings page) — leaning toward "persist until explicitly cleared or superseded, navigation alone doesn't clear it" but to be confirmed during technical spec.
- Maximum practical chain length / whether extremely long `;`-chains need a distinct confirmation step before executing — to be decided during technical spec.
- Whether context chips are visible/interactive on mobile's smaller viewport in the same form, or need a condensed mobile-specific treatment — to be resolved during UI/UX technical spec.
- ~~Which AI provider/model powers AI-assist translation~~ — resolved structurally: [ai-llm-services.md](ai-llm-services.md) provides multi-provider abstraction (Anthropic, OpenAI, Gemini, Azure OpenAI) with SaaS-pooled or BYO-key modes. Whether a DOE/air-gapped tenant's needs are met by a BYO key pointed at a private/self-hosted-compatible endpoint, or require a genuinely self-hostable model outside this doc's four named providers, remains open — none of the four are self-hostable in the platform's usual sense.
- Exact alias-suggestion repetition threshold (occurrence count / time window) before a suggestion surfaces — to be tuned during technical spec/UX testing.
- Whether a promoted alias can later be un-promoted (reverted to team/personal scope) and what happens to users who'd adopted it in the meantime — to be decided during technical spec.
- Exact confirmation-chip and step-up-pause visual treatment mid-chain (e.g., how remaining un-run commands are visually indicated as pending) — to be resolved during UI/UX technical spec.
