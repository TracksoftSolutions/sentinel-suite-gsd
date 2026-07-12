# Command Palette

**Module:** 0. Platform Core
**Status:** Draft — elicited, ready for technical spec

## Overview

The Command Palette is a keyboard-triggered, fuzzy-search quick-invoke surface available to any user: search and invoke Command/Action Bus actions, navigate directly to records/pages, search across data content itself, revisit recent/frequent items, and toggle certain personal settings — all from one search box, without leaving the current screen. It is a distinct component from the CLI-Style Input (see [cli-style-input.md](cli-style-input.md)), which serves power users who prefer typed command syntax; the palette is the fuzzy-search, point-and-select surface for everyone.

The palette does not introduce new capabilities of its own — it is a discovery and invocation layer over the Command/Action Bus catalog, Master Records/module data, and platform navigation, all filtered through the invoking user's own RBAC/ABAC identity exactly as any other manual invocation.

## Actors & Roles

- **Any platform user** — opens the palette via keyboard shortcut, searches, and invokes/navigates.
- **Every platform feature/module** — contributes its registered actions (via Command/Action Bus discoverability metadata) and its searchable record types to the palette's index.
- **Tenant Admin** — no special palette-specific admin role; palette content is entirely derived from existing RBAC/ABAC grants and Command/Action Bus registrations.

## User Stories

- As a **Supervisor**, I want to hit a keyboard shortcut and type "create task" to invoke it immediately, without navigating through menus.
- As a **Dispatcher**, I want to type an incident number into the palette and jump straight to that incident's record.
- As a **Guard**, I want to type a partial license plate and have the matching vehicle record surface, so I don't need to know which module owns vehicle data.
- As a **user**, I want my recently viewed incidents to appear at the top when I open the palette with no search text yet, so I can quickly return to what I was just working on.
- As a **user**, I want to toggle my notification quiet hours directly from the palette, without navigating to a settings page.
- As a **Guard with limited permissions**, I want the palette to only show me actions and records I'm actually allowed to see/do, so it never becomes a way to discover things I can't access.

## Functional Requirements

### Invocation & search
1. The palette opens via a global keyboard shortcut from anywhere in the web/mobile app, and closes on selection, explicit dismiss (Esc), or losing focus.
2. Search is fuzzy-matched across four result categories in one unified result list, ranked by relevance and recency: **actions** (from the Command/Action Bus catalog), **navigation targets** (pages/screens), **data records** (universal search), and **recent/frequent items**.
3. With no search text entered, the palette defaults to showing recent/frequent items for the current user.

### Actions
4. Only actions whose applicable-context predicate matches the palette's current invocation context (e.g., a record open on screen) and whose RBAC/ABAC check the current user would pass are shown — never actions the user cannot actually perform.
5. Selecting an action follows the same parameter-collection behavior as any manual Command/Action Bus invocation: required parameters not resolvable from context are collected via a generated inline form before execution.
6. Palette-invoked actions execute under the invoking user's own RBAC/ABAC identity, per the Command/Action Bus's manual-invocation model — no special-cased permission path.
6a. An action with no compensating action, or flagged for step-up authentication, passes through Command/Action Bus's confirmation gate before executing — rendered here as a confirmation dialog (with the step-up challenge inline where required), consistent with the gate's server-side enforcement regardless of surface. A palette-invoked action with a compensating action is undoable via the same per-entity Undo history mechanism the CLI-Style Input surface uses, surfaced here as a toast affordance.

### Navigation & universal search
7. Typing a recognizable identifier (record number, name, plate, etc.) surfaces direct matches from any module's searchable data, filtered to the user's data-scope grants.
8. Selecting a navigation or data result routes the user directly to that page/record.

### Recent, frequent, and settings
9. The palette tracks each user's recent and frequent actions/navigations/records to prioritize palette ranking and populate the no-search-text default view.
10. A defined set of personal settings (e.g., notification quiet hours, theme) are toggleable inline from the palette without navigating to a settings page.

## Data Model / Fields

**Palette Index Entry** (derived, not separately authored)
- entry_id, entry_type (action, navigation, record, setting)
- source_ref (action_id, route, record_ref, or setting_key)
- searchable_text, applicable_context_predicate (for actions)

**User Palette Activity**
- account_id, entry_id, last_used_at, use_count

## States & Transitions

**Palette:** `closed` → `open-default` (recent/frequent shown) → `open-searching` (results updating live per keystroke) → `result-selected` (action param collection, navigation, or setting toggle) → `closed`.

## Integrations

- **Command/Action Bus**: source of invokable actions and their discoverability metadata/applicable-context predicates.
- **Authentication & Authorization**: RBAC/ABAC filtering of every result category.
- **Master Records / every module's searchable data**: source of universal search results, respecting each module's own data-scope rules.
- **Settings & Preferences**: source of the toggleable personal settings surfaced inline.
- **CLI-Style Input**: a separate, complementary surface; the palette does not include typed command syntax.

## Permissions

Palette results are entirely derivative of existing permissions — there is no separate permission model for the palette itself. What a user sees in the palette is exactly what their RBAC/ABAC grants would allow them to see/do elsewhere in the platform.

## Non-Functional / Constraints

- Palette search must feel instantaneous (sub-100ms perceived latency for local/cached indices) even though results span actions, navigation, and live data queries.
- Must respect the same tenant-isolation and data-scope boundaries as every other query path — the palette is not a shortcut around authorization.
- WCAG 2.1 / Section 508 accessible: full keyboard operability (open, navigate results, select) with no mouse dependency, screen-reader-friendly result announcements.
- Must degrade gracefully offline (mobile): recent/frequent items and cached navigation remain available; live universal search against server data is unavailable offline and is clearly indicated as such, consistent with the platform's offline-capable model.

## Acceptance Criteria

- [ ] Opening the palette with no text shows the current user's recent/frequent items.
- [ ] Typing an action name surfaces and can invoke that action, but only if it's applicable to the current context and the user holds the required permission.
- [ ] Typing a record identifier (e.g., incident number, plate) surfaces the correct matching record, scoped to the user's data-scope grants.
- [ ] A user without permission for a given action never sees it in palette results, even if they know its name and type it exactly.
- [ ] Toggling a personal setting (e.g., quiet hours) from the palette takes effect identically to toggling it from the settings page.
- [ ] The palette is fully operable via keyboard alone, verified with a screen reader.
- [ ] Offline on mobile, recent/frequent items remain accessible while live universal search is clearly shown as unavailable.

## Open Questions

- Exact keyboard shortcut and whether it's customizable per user — to be finalized during UI/UX technical spec.
- Whether universal search results are ranked by a relevance algorithm beyond simple recency/frequency (e.g., a proper search-relevance score) — deferred to technical spec.
- Full list of settings toggleable inline from the palette at launch — to be finalized alongside the Settings & Preferences feature doc.
