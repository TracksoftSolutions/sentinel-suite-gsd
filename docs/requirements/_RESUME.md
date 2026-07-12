# Resume Prompt — paste this into a new session

We're mid-way through a structured requirements-elicitation exercise for Sentinel Suite. Before doing anything else:

1. Read `docs/requirements/_INDEX.md` — the ordered, checkbox-tracked list of every feature (from `docs/MODULES.md`, plus several features added during elicitation). `[x]` = doc written, `[ ]` = not started yet. Find the first unchecked item — that's where we continue.
2. Read `docs/requirements/_DECISIONS.md` in full — it's the accumulated log of every cross-cutting architectural decision made so far. This is not optional context; several decisions (see below) constrain how every future feature doc must be written. Do not contradict or re-litigate anything in it without flagging the conflict to the user first.
3. Skim 2-3 of the already-written docs in `docs/requirements/0.5-master-records/` (e.g., `entity-registry-core.md` and `person-registry.md`) to internalize the template and level of depth expected: Overview, Actors & Roles, User Stories, Functional Requirements, Data Model/Fields, States & Transitions, Integrations, Permissions, Non-Functional/Constraints, Acceptance Criteria, Open Questions.

## Where things stand

- **Module 0 (Platform Core): complete** — 12 features, including four added during elicitation (Domain Events, Command/Action Bus, Command Palette, CLI-Style Input) beyond the original 8.
- **Module 0.5 (Master Records): complete** — 12 features on a from-scratch Table-Per-Type entity/association architecture (Entity Registry Core, Party, Person, Organization, Item, Vehicle/Conveyance, Location, Activity, Document, Entity Relationships & History), replacing the original 5-feature MODULES.md list.
- **25 of 217 features done overall.** Next up: **Module 1, Security Operations**, starting with **Daily Activity Reports (DAR)**.

## Process to follow (unchanged from the rest of the session)

- One feature at a time. Use `AskUserQuestion` in rounds — keep asking until nothing about the feature is ambiguous. Adaptive depth: simple registry-style features get 1-2 rounds, complex workflow/lifecycle features get more.
- Write the doc to `docs/requirements/<module-slug>/<feature-slug>.md` using the template above.
- Mark the feature `[x]` in `_INDEX.md` immediately after writing its doc.
- If a feature reveals a cross-cutting decision (a new shared mechanism, a naming collision, a pattern that should generalize), log it in `_DECISIONS.md` — that file is what keeps 200+ features consistent with each other across sessions.
- When the user pushes back on an architectural choice, engage with the actual technical tradeoff — don't just defer or just agree. This session's biggest wins (NIEM grounding, TPT inheritance, the Entity/EntityAssociation split) came from genuine back-and-forth, not from taking the first design and running with it.

## Load-bearing decisions from `_DECISIONS.md` you must not contradict

- **.NET/ASP.NET Core backend** (FedRAMP/FIPS alignment) — GraphQL for internal frontend, REST for external integrations.
- **RBAC baseline + ABAC overlay** authorization model; every sensitive action gated by a **confirmation gate** (Command/Action Bus) and **step-up auth** (Authentication & Authorization) where flagged.
- **NIEM Core grounding discipline**: before inventing fields for any new entity-like concept, check the real NIEM Core type (`nc:PersonType`, `nc:ItemType`, `nc:ActivityType`, `nc:DocumentType`, `nc:EntityType`/Party, etc.) rather than guessing. This has repeatedly produced better field models than starting from scratch.
- **Entity/EntityAssociation TPT architecture** (Module 0.5): any future module that needs its own "thing with an ID that gets tracked/deduped/cross-referenced" should very likely register as an extension of one of the five base types (Party, Item, Location, Activity, Document) rather than inventing a parallel identity system. Relationships between things should very likely be a named `EntityAssociation` subtype, not a plain foreign-key field, when the relationship benefits from independent audit/history (ownership, custody, employment, etc. all do; a purely descriptive intrinsic field usually doesn't).
- **Every Entity has a display label** (`display_label_strategy`, template or computed) — a universal Entity Registry Core requirement, not per-feature.
- **Settings & Preferences** is a shared hierarchical config engine (location chain + identity chain, lockable per level) — any future tenant/site/user-configurable setting should register against it, not implement its own override logic.
- **Command system** (Command/Action Bus, Domain Events, Command Palette, CLI-Style Input) is an intentional platform differentiator — any new feature that exposes user-facing actions should register them as Command/Action Bus actions, not build bespoke buttons with no palette/CLI/automation reach.

## A note on working style

This session ran long and context got heavy, which is why we're handing off. Keep responses tight, avoid re-deriving things already settled in `_DECISIONS.md`, and don't be afraid to push back if something in this resume doc looks wrong once you're looking at the live repo state — verify before trusting, same as always.
