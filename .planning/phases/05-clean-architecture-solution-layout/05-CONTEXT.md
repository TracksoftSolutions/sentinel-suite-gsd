# Phase 5: Clean Architecture Solution Layout - Context

**Gathered:** 2026-07-16
**Status:** Ready for planning

<domain>
## Phase Boundary

**The phase was deliberately reframed during discussion.** As written, ROADMAP Phase 5 says "create five empty stub projects (`Domain.Shared`, `Domain`, `UseCases`, `Infrastructure`, `Web`) with enforced dependency direction." The user rejected scaffolding empty projects before they are needed ("if they're not necessary yet they can be added later") and instead asked for **a documented plan** — specifically **a roadmap toward a nearly-complete ABP-equivalent framework**.

**What Phase 5 now delivers:**

1. **An ABP-parity framework north-star roadmap** (a committed document) — the primary new deliverable. It inventories ABP's **runtime framework** surface and, for each capability, marks it **include-and-hand-roll / defer / exclude**, grounded in this project's FedRAMP + dependency-minimalism constraints, and maps the included pieces onto the layered project structure. This is a **multi-milestone north-star**: the current milestone stays kernel-only; the roadmap documents the full target so every future project/capability is added deliberately rather than retrofitted.

2. **The framework-vs-application naming split** that underpins the roadmap (see D-02).

3. **The minimal physical layout that already exists**, with dependency direction **compiler-enforced and proven** on the projects that exist today (see D-04, D-05), plus a root `Directory.Build.props` (D-03). **No new empty projects are created this phase** (D-06).

**Requirement:** LAYOUT-01 (reframed — see the roadmap-divergence note below).
**Depends on:** Nothing (parallel with Phases 1–4). The existing scaffold (`Framework.Domain.Shared`, `Framework.Domain`, `Framework.Domain.Shared.Tests`, `SentinelSuite.slnx`, `global.json`) is already on disk.

**ROADMAP DIVERGENCE — flag to planner (and to `/gsd-phase` for a roadmap edit):**
- Original SC#1 ("exactly five projects … `UseCases`, `Infrastructure`, `Web`") and SC#4 ("those three exist as empty stub projects") **no longer hold**. The user explicitly does not want empty stubs created now.
- Reworded intent for Phase 5's success criteria:
  1. A committed **ABP-parity framework roadmap** exists, covering the framework runtime surface (include/defer/exclude per capability), the framework-vs-app naming split, and the layered project map. All ABP **tooling** is explicitly excluded (D-08).
  2. The projects that exist (`Framework.Domain` → `Framework.Domain.Shared`) build, with dependency direction compiler-enforced.
  3. A **reverse-direction reference fails at build**, proven by a documented attempted-violation check (SC#3 survives — it is the one original criterion still directly satisfied).
  4. Central build configuration (`Directory.Build.props`) governs shared MSBuild properties.
- SC#2's *full* five-project reference chain (`UseCases→Domain`, `Infrastructure→UseCases`, `Web→UseCases/Infrastructure`) becomes **documented in the roadmap**, enforced when each project is actually created, not this phase.

</domain>

<decisions>
## Implementation Decisions

### The primary deliverable: ABP-parity framework roadmap
- **D-01:** Phase 5's headline deliverable is a **committed roadmap document toward a nearly-complete ABP-equivalent framework** — hand-rolled, no ABP/Ardalis NuGet packages (per PROJECT.md). It is a **multi-milestone north-star**, not a this-milestone build list. The current 20 kernel phases (1–20) **stay as-is**; the roadmap **anchors each existing phase to its place in the ABP map** and enumerates everything beyond the current roadmap that full framework parity implies (unit of work, data filtering/query-filter interceptors, settings, features, permissions/authorization, distributed event bus + outbox/inbox, background jobs/workers, caching abstractions, object-extending/extra-properties, the EF Core integration layer, the HttpApi/auto-controller + client-proxy layer, object mapping, `IClock`/timing, sequential GUIDs, data seeding, validation, exception handling, virtual file system, BLOB storing, emailing — the researcher's inventory determines the full list). **Scope choice locked: "North-star, keep current phases."**

### Naming — framework vs. application (the split the user flagged)
- **D-02:** Resolve the naming mismatch the ABP way — **two layers, two naming conventions:**
  - `SentinelSuite.Framework.*` = the **reusable framework kernel** (this milestone's work). The equivalent of `Volo.Abp.*`. Existing `Framework.Domain.Shared` / `Framework.Domain` already follow this.
  - `SentinelSuite.*` (no `Framework` infix) = the **application built on the framework** (future milestones). The equivalent of `Acme.BookStore.*` in an ABP-generated app.
  - This is why `UseCases`/`Web` felt wrong under a `Framework` prefix — those are *application* layers and must not wear the framework name. **The exact app-side project names are finalized in the roadmap draft** (D-01), validated against ABP's own layer naming.

### Physical layout this phase (minimal, nothing premature)
- **D-03:** Introduce a **root `Directory.Build.props`** now (there is none today) to centralize `TargetFramework` (`net10.0`), `Nullable` (`enable`), `ImplicitUsings` (`enable`), and a home for future shared analyzer/lang-version settings. Rationale: the roadmap anticipates many projects; one place keeps them consistent and prevents per-`.csproj` drift. Existing `.csproj` files get their now-redundant per-project properties trimmed to inherit. **Watch:** the test project's MTP-specific properties (`OutputType=Exe`, `UseMicrosoftTestingPlatformRunner`, `IsPackable=false`, etc.) stay project-local — do not hoist test-only settings into the shared props.
- **D-04:** **Create no new projects this phase.** `Framework.Domain.Shared` + `Framework.Domain` (+ `Framework.Domain.Shared.Tests`) already exist and *are* the kernel so far. App-layer and additional framework projects are added when a milestone actually needs them, per the roadmap. **Create-now scope locked: "nothing new."**
- **D-05:** **Compiler-enforce and prove dependency direction on the existing pair.** `Framework.Domain → Framework.Domain.Shared` is the allowed direction (already wired). Demonstrate that the **reverse** (`Framework.Domain.Shared → Framework.Domain`) **fails the build**, captured as a **documented attempted-violation check** (e.g., a short markdown note with the failing build output, or a commented-out reverse `ProjectReference` with an explanatory comment). This satisfies original SC#3 and establishes the enforcement convention the roadmap mandates for every future project.
- **D-06:** The **stub shape and per-project conventions** for future projects (truly empty vs. namespace-anchor marker; `Web` as `Microsoft.NET.Sdk.Web` vs. classlib; where test projects sit) are **documented in the roadmap as conventions**, not decided/created this phase. They apply when each project lands.

### Tooling — hard exclusions
- **D-08:** The roadmap targets ABP's **runtime framework only**. **Explicitly excluded (never reproduced):** ABP CLI, ABP Suite (code generation / entity generation), ABP Studio, and all project/module **templates & scaffolding tooling**. Sentinel Suite creates new projects and modules **by hand**. The roadmap must state this exclusion up front so no future phase tries to build a code generator.

### Claude's / researcher's discretion (delegated by the user: "I research + draft, you review")
- **D-07:** The **per-capability include / defer / exclude cuts** are delegated to the research+draft step for the user's review. Guiding principles the draft must apply:
  - **Exclude all tooling** (D-08).
  - **Exclude ABP's pre-built *application* modules** (Identity, Account, CMS Kit, Docs, etc.) — those are *product features*, not framework, and belong to later product milestones if at all, not the framework roadmap.
  - **Exclude/defer heavy UI & infra breadth not needed:** bundled UI themes (LeptonX), MVC/Blazor/Angular UI packages, multiple ORM integrations (target **EF Core only**; MongoDB/Dapper excluded), and provider-specific distributed-bus adapters until a real need exists.
  - **Include-and-hand-roll only where reproducing the pattern is minimal-to-moderate effort** (PROJECT.md's dependency-minimalism test); take a real dependency only where in-house reproduction is genuinely nontrivial, and call that out explicitly per item.
  - Each row: capability → decision (include-hand-roll / defer / exclude) → target layer/project → rough milestone → dependency-minimalism note.
- **Open questions for the roadmap draft to propose (user reviews):**
  1. **One solution vs. two** — do framework + application live in one `.slnx` (project references) or does the framework become a separately-versioned package set the app consumes? (ABP-on-ABP is separate; Sentinel *building* its framework could go either way.)
  2. Whether a lightweight **repeatable** architecture-direction guard (a tiny zero-dependency test asserting reference direction) should replace the one-time documented check as the tree grows — desirable for FedRAMP-auditability, but not required this phase.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level constraints and requirements
- `.planning/PROJECT.md` — dependency-minimalism constraint, the **no-ABP/no-Ardalis-package** rule (borrow patterns, never packages), .NET 10 / nullable+implicit-usings baseline, FedRAMP/NIST 800-53 posture. The "ABP-inspired" framing throughout is the north-star roadmap's whole basis. Also §"Context" — the existing scaffold state.
- `.planning/REQUIREMENTS.md` — LAYOUT-01 (this phase's requirement, now reframed); the reference table maps LAYOUT-01 → Phase 5.
- `.planning/ROADMAP.md` §"Phase 5: Clean Architecture Solution Layout" — the original success criteria. **Read alongside this CONTEXT's ROADMAP DIVERGENCE note** — SC#1/#4 are superseded; the planner should also trigger a `/gsd-phase` edit to reword Phase 5's roadmap entry.

### Architecture and pattern guidance
- `docs/architecture-guidance.md` — the inherit/interface/compose three-way rule and registry-is-authoritative rule. Governs class shape, **not** project topology (confirmed: it is silent on solution layout), but the roadmap's capability placement (capabilities as interfaces + registry, provider adaptors, composed services) must stay consistent with it. Note its EF Core practical notes and the "if the base-framework decision lands on ABP, extend rather than duplicate" line — directly relevant to the parity roadmap.
- `.claude/CLAUDE.md` §"Hand-Roll vs. Depend" and §"Explicitly excluded" — the concrete hand-roll-vs-depend mechanics for the nine ABP/Ardalis patterns, and the banned package list. The roadmap's include/hand-roll rows should reuse this analysis.

### Platform requirements corpus (referenced by PROJECT.md — the roadmap researcher should skim for framework-shaping decisions)
- `docs/pdd.md` (Project Design Document), `docs/mvp.md` (MVP scope ledger), `docs/MODULES.md` (26-module / 222-feature catalog), `docs/requirements/_DECISIONS.md` (200+ cross-cutting architectural decisions). These define *what the platform is* and constrain which ABP framework capabilities are genuinely needed (e.g., tiered multi-tenancy shared/dedicated_db/on_prem, air-gapped deployments, provider-adaptor requirements).

### Prior-phase precedent (naming/namespace convention this phase's layout must stay consistent with)
- `.planning/phases/01-domain-shared-guardclauses/01-CONTEXT.md`, `.../02-…/02-CONTEXT.md`, `.../03-…/03-CONTEXT.md`, `.../04-…/04-CONTEXT.md` — establish the `SentinelSuite.Framework.Domain.Shared.{Concept}` namespace convention (`.Guards`, `.Results`, `.SmartEnum`, `.Exceptions`) and the MTP-native xUnit v3 test project shape the roadmap's app-layer test-project conventions should mirror.

### Existing code/scaffold to read before planning
- `SentinelSuite/SentinelSuite.slnx` — current solution (three projects wired).
- `SentinelSuite/SentinelSuite.Framework.Domain/SentinelSuite.Framework.Domain.csproj` — has the `ProjectReference` to `Domain.Shared`; the direction to preserve/enforce (D-05).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` — the kernel-root project; per-project props to migrate into `Directory.Build.props` (D-03).
- `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/…Tests.csproj` — MTP-specific props that must stay project-local (D-03 watch-note).
- `SentinelSuite/global.json` — pins the MTP test runner.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- The **existing three-project scaffold** is the kernel-so-far and needs no new siblings this phase (D-04). `Framework.Domain` → `Framework.Domain.Shared` is already the correct, enforceable dependency edge (D-05).
- The **Phase 1–4 namespace convention** (`SentinelSuite.Framework.Domain.Shared.{Concept}`) is the established pattern the roadmap's framework-side naming continues; the app side introduces the un-prefixed `SentinelSuite.*` convention (D-02).

### Established Patterns
- **`SentinelSuite.Framework.*` = framework/kernel** already in use for the two real projects — this CONTEXT formalizes it as the framework-vs-app split (D-02).
- **Zero third-party NuGet in framework projects**, MTP-native xUnit v3 in the test project — the roadmap's "include-and-hand-roll" bias inherits directly from this.

### Integration Points
- No new integration points created this phase. The roadmap defines the *future* seams (framework kernel ← app core ← application ← infrastructure/API/host) as documentation; the only live, enforced seam remains `Framework.Domain → Framework.Domain.Shared`.
- `Directory.Build.props` (D-03) becomes the shared MSBuild integration point every current and future project inherits from.

</code_context>

<specifics>
## Specific Ideas

- The user's driving intent, verbatim: **"a roadmap to nearly full ABP (removing things that are genuinely not needed for our build, and things like the ABP CLI, codegen, studio, etc — essentially no tooling for creating new projects/modules)."** Every framing decision here serves that: reproduce ABP's *runtime framework*, hand-rolled and dependency-minimal, minus all tooling and minus product/application modules.
- The user twice rejected creating empty stub projects prematurely — the guiding rule is **"add later when needed, but document the plan now."** The roadmap document *is* the plan; physical creation stays minimal.
- ABP's own **framework-vs-app separation** (`Volo.Abp.*` framework packages vs `Acme.BookStore.*` generated app) is the conceptual template for D-02 and for the whole roadmap's structure — the researcher should mirror ABP's layer decomposition as the inventory's spine, then subtract.
- The user delegated the detailed include/defer/exclude cuts to a **research-and-draft step for their review** — the draft should come back as a reviewable table, not a finished decree.

</specifics>

<deferred>
## Deferred Ideas

- **All ABP tooling** — CLI, Suite/codegen, Studio, project & module templates. Not "deferred to a later phase"; **permanently excluded** (D-08). Recorded here so no future phase reopens it.
- **ABP pre-built application modules** (Identity, Account, Tenant Management UI, CMS Kit, Docs, etc.) — product features, not framework; out of the framework roadmap's scope (belong to product milestones if ever, via the platform's own requirements corpus).
- **Non-EF-Core persistence integrations** (MongoDB, Dapper) and **UI framework/theme breadth** (LeptonX, MVC/Blazor/Angular packages) — excluded/deferred pending genuine need (D-07).
- **A repeatable architecture-direction guard test** (vs. the one-time documented check) — noted as a desirable future hardening for FedRAMP-auditability; not built this phase (D-07 open question 2).
- **Whether framework + app become one solution or two** — open question for the roadmap draft to propose (D-07 open question 1); not decided now.

</deferred>

---

*Phase: 5-Clean Architecture Solution Layout*
*Context gathered: 2026-07-16*
