# Phase 5: Clean Architecture Solution Layout - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-16
**Phase:** 5-Clean Architecture Solution Layout
**Areas discussed:** Project topology & naming, Create-now scope, ABP-parity roadmap scope, Inventory build method (plus the four originally-offered gray areas, subsumed by the reframing)

---

## Gray-area selection (opening)

| Option | Description | Selected |
|--------|-------------|----------|
| Stub project names | Framework infix for new stubs? | ✓ |
| Reverse-ref enforcement | Manual demo vs automated guard-test | ✓ |
| Central build config | Directory.Build.props vs per-project | ✓ |
| Stub shape & Web SDK | Empty vs marker; classlib vs Sdk.Web | ✓ |

**User's choice:** All four — plus a free-text addition: *"More fully fleshed out project structure, or plan — I didn't suggest the 4-project structure or the names."*
**Notes:** This addition reframed the phase. The user was challenging the roadmap's project count and names, not just the sub-decisions. Topology was elevated to the first discussion because names/stub-shape depend on it.

---

## Project topology

| Option | Description | Selected |
|--------|-------------|----------|
| Roadmap 5 (Ardalis) | Minimal Clean Architecture spine as written | |
| ABP-style layered | Fuller ~8-project ABP layering | |
| Framework kernel only | No app/host here; app is a separate later solution | |

**User's choice:** No menu option selected. Note: *"ultimately if they're not necessary yet [they] can be added later, but I want a plan."*
**Notes:** Signal = don't scaffold empty projects prematurely; produce a documented target-structure plan instead so future additions are deliberate.

---

## Create-now scope

| Option | Description | Selected |
|--------|-------------|----------|
| Domain pair only | Create nothing new; deliver blueprint + enforce direction on existing pair | |
| Add the reference spine | Also create UseCases + Infrastructure stubs to lock the chain | |
| Full roadmap 5 + plan | Create all five stubs and write the blueprint | |

**User's choice:** No menu option selected. Note: *"Must still have [a] broader ABP-style plan."*
**Notes:** Firm requirement on the blueprint's *content* — it must be the fuller ABP-style layered plan, not the Ardalis-minimal five. Create-now scope left to synthesis (resolved as "nothing new," D-04).

---

## Confirmation attempt (rejected → clarification)

**User's choice:** Declined to lock; asked to clarify. Clarified intent: *"a roadmap to nearly full ABP (removing things that are genuinely not needed for our build, and things like the ABP CLI, codegen, studio, etc — essentially no tooling for creating new projects/modules)."*
**Notes:** This crystallized the deliverable as an ABP-**runtime-framework** parity roadmap, hand-rolled, minus all tooling and minus product/application modules.

---

## ABP-parity roadmap scope

| Option | Description | Selected |
|--------|-------------|----------|
| North-star, keep current phases | Multi-milestone north-star; current 20 phases stay, anchored to the map | ✓ |
| North-star AND revise this milestone | Also re-open phases 2–4, 6–20 to align with ABP parity | |
| Full re-plan (new milestone) | Pause and run milestone-level planning first | |

**User's choice:** North-star, keep current phases.
**Notes:** Least disruption to in-flight kernel work; the roadmap is a north-star spanning future milestones.

---

## Inventory build method

| Option | Description | Selected |
|--------|-------------|----------|
| I research + draft, you review | Claude researches ABP's framework catalog and drafts include/defer/exclude table for review | ✓ |
| Triage live now | Triage major buckets together in discussion | |
| I'll supply the list | User hands over the cuts | |

**User's choice:** I research + draft, you review.
**Notes:** The detailed inventory is delegated to the plan-phase researcher (web-tool-equipped), driven by this CONTEXT, returned as a reviewable table.

---

## Claude's Discretion

- Per-capability include/defer/exclude cuts (delegated for research + draft, user reviews) — D-07.
- `Directory.Build.props` introduction and which properties to hoist — D-03.
- The documented attempted-violation mechanism for the reverse-reference check — D-05.
- Open questions the roadmap draft will propose: one-solution-vs-two; a repeatable direction-guard test.

## Deferred Ideas

- All ABP tooling (CLI, Suite/codegen, Studio, templates) — permanently excluded (D-08).
- ABP pre-built application modules (Identity, Account, CMS Kit, Docs, …) — product features, out of framework scope.
- Non-EF-Core persistence (MongoDB, Dapper) and UI/theme breadth (LeptonX, MVC/Blazor/Angular) — excluded/deferred pending need.
- Repeatable architecture-direction guard test — desirable future hardening, not this phase.
- Framework + app as one solution vs. two — open for the roadmap draft to propose.
