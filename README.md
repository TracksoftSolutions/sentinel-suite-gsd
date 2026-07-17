# Sentinel Suite

Sentinel Suite is a unified platform for security operations, dispatch, emergency management, safety, and compliance — serving both in-house facility security departments and contract security companies, from a single guard on shift up to DOE GOCO-operated National Laboratories.

## What This Is

**This repository currently holds Milestone 1 — the domain kernel, not a customer-facing feature.** It's the custom Clean Architecture/DDD domain kernel — the Entity/EntityAssociation TPT foundation, capability-interface conventions, and tenant/audit/domain-event plumbing — that every one of the platform's 26 modules and 222 planned features will be built on top of.

**Core value:** Every one of Sentinel Suite's 222 planned features depends on getting the Entity/EntityAssociation taxonomy, multi-tenancy, and auditing conventions right once, in a dependency-minimal, FedRAMP-friendly kernel — get this foundation wrong and every module built on it inherits the mistake.

The kernel is built with deliberate dependency minimalism: patterns commonly pulled in via ABP Framework or the Ardalis package family (`Ardalis.Specification`, `Ardalis.Result`, `Ardalis.GuardClauses`, `Ardalis.SmartEnum`) are hand-rolled in-house rather than taken as NuGet dependencies, since every added dependency lengthens the FedRAMP authorization process this platform is on a track toward.

## Current Status

Milestone 1 (v1.0), currently on **Phase 03 — Domain.Shared: SmartEnum&lt;T&gt;**.

Validated so far:

- **GuardClauses** (Ardalis-style, hand-rolled) — Phase 1
- **Result / Result&lt;T&gt;** (Ardalis-style, hand-rolled) — Phase 2

See [.planning/STATE.md](.planning/STATE.md) for live progress and [.planning/PROJECT.md](.planning/PROJECT.md) for the full requirements ledger (Validated / Active / Out of Scope).

## Repository Layout

```
SentinelSuite/
├── SentinelSuite.slnx
├── global.json
├── src/
│   └── Framework/
│       ├── SentinelSuite.Framework.Domain/
│       └── SentinelSuite.Framework.Domain.Shared/
└── test/
    └── Framework/
        └── SentinelSuite.Framework.Domain.Shared.Tests/
```

Projects are grouped by area under `src/Framework/` and `test/Framework/` — `Framework` is the first area (the domain kernel itself). This split is intentional groundwork for the 26-module platform this kernel underpins; a flat, single-directory layout does not scale to that size.

- `SentinelSuite.Framework.Domain.Shared` — dependency-free primitives shared across the framework: `GuardClauses`, `Result`/`Result<T>`, and (in progress) `SmartEnum<T>`.
- `SentinelSuite.Framework.Domain` — the Domain project proper; will host `Entity`/`AggregateRoot`, `EntityAssociation`, capability interfaces, auditing/soft-delete, multi-tenancy plumbing, and domain events as later phases land.
- `SentinelSuite.Framework.Domain.Shared.Tests` — xUnit v3 tests (via `Microsoft.Testing.Platform`, per `SentinelSuite/global.json`) covering `Domain.Shared`.

Build and test from `SentinelSuite/`:

```bash
cd SentinelSuite
dotnet build
dotnet test
```

## Documentation

**Platform requirements and architecture** live under [`docs/`](docs/):

- [`docs/architecture-guidance.md`](docs/architecture-guidance.md) — the inheritance/interface/composition rules (inherit what a thing IS, interface what a thing CAN, compose what a thing USES) this kernel must follow
- [`docs/MODULES.md`](docs/MODULES.md) — the 26-module / 222-feature platform catalog
- [`docs/pdd.md`](docs/pdd.md) — Project Design Document
- [`docs/mvp.md`](docs/mvp.md) — living MVP scope ledger
- [`docs/requirements/`](docs/requirements/) — per-module requirement specs, including `_DECISIONS.md`, the cross-cutting architectural decision log

**GSD-driven project and phase tracking** lives under [`.planning/`](.planning/):

- [`.planning/PROJECT.md`](.planning/PROJECT.md) — current milestone scope, requirements (Validated/Active/Out of Scope), constraints, and key decisions
- [`.planning/ROADMAP.md`](.planning/ROADMAP.md) — phase-by-phase build plan for this milestone
- [`.planning/STATE.md`](.planning/STATE.md) — current phase/plan position and accumulated context
