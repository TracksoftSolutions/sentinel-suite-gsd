# Phase 2 — Plan Outline: Domain.Shared: Result / Result<T>

**Generated:** 2026-07-16
**Mode:** Chunked (outline-only — no PLAN.md files written yet)
**Requirement coverage:** PRIM-02 (sole phase requirement — appears in every plan below)

## Sizing Rationale

RESEARCH.md Pitfall 1 flags that D-12/D-13's "5 combinators with sync+async overloads" is not
"5 combinators x 2" but closer to 6 combinators (Map, Bind, Ensure, OnSuccess, OnFailure, Match) x
up to 4 sync/async shapes (sync/sync, Left-async, Right-async, Both-async) each — a genuinely
large surface. Per the sizing guidance for this outline, the phase is split into 6 small plans
across 3 waves instead of 1 mega-plan:

- **Wave 1** — `Result` (non-generic): `ResultStatus`, `Error`, status factories, `CriticalError`
  exception-carrying behavior (D-01–D-04, D-07–D-09, D-11).
- **Wave 2** — `Result<T>`: mirrors Wave 1's factory set, adds the fail-fast `Value` getter (D-06)
  and the one-directional `T -> Result<T>` implicit conversion (D-14). Depends on Wave 1 because
  it consumes the `Error` type Wave 1 creates.
- **Wave 3** (4 plans, run in parallel — zero file overlap, each depends only on Waves 1+2) —
  the combinator surface, split into 3 pairs by shape-similarity (Map+Bind both transform/chain
  and share the identical Left/Right/Both async pattern; Ensure+Match both collapse/gate on a
  predicate or handler pair; OnSuccess+OnFailure are pure side-effect hooks sharing one file per
  RESEARCH.md's recommended structure) plus a dedicated Combine plan (batch aggregation, D-15,
  Pitfall 4's separate-overloads strategy).

Each plan targets 2 tasks (one combinator/concern per task), well within the 2-3 task / ~50%
context budget. Async test coverage is folded into each combinator's own test file (not a single
shared `ResultAsyncCombinatorTests.cs`) specifically so the 4 Wave-3 plans have zero file overlap
and can execute in parallel.

## Outline

| Plan ID | Objective | Wave | Depends On | Requirements |
|---------|-----------|------|------------|---------------|
| 02-01 | `ResultStatus` enum (D-08) + `Error` sealed record (D-01–D-03, Guard-validated) + `Result` (non-generic) sealed class: `IsSuccess`/`IsFailure`, `.Errors`/`.Error` accessors, all 9 named static factories including `CriticalError(Exception, ...)` carrying the original exception (D-04, D-07, D-09, D-11) | 1 | — | PRIM-02 |
| 02-02 | `Result<T>` sealed class mirroring `Result`'s full factory set (D-10) + fail-fast `Value` getter throwing `InvalidOperationException` on failed access (D-06) + one-directional `T -> Result<T>` implicit conversion only (D-14, per Pitfall 3's explicit no-reverse-conversion warning) | 2 | 02-01 | PRIM-02 |
| 02-03 | `Map` and `Bind`/`Then` combinators, each with sync + Left-async + Right-async + Both-async overloads (D-12, D-13, RESEARCH.md Pattern 2 file-per-shape convention) | 3 | 02-01, 02-02 | PRIM-02 |
| 02-04 | `Ensure` (predicate-gated success-to-failure) and `Match` (collapse to single value via success/failure handlers) combinators, each with sync + async overloads (D-12, D-13) | 3 | 02-01, 02-02 | PRIM-02 |
| 02-05 | `OnSuccess` and `OnFailure` side-effect combinators (no value transform), sync + async overloads, co-located in one extensions file per RESEARCH.md's recommended structure (D-12, D-13) | 3 | 02-01, 02-02 | PRIM-02 |
| 02-06 | `Result.Combine(params Result[])` and `Result.Combine<T>(params Result<T>[])` — all-or-nothing aggregation with full error-union on any failure, separate-overloads strategy per Pitfall 4 (D-15) | 3 | 02-01, 02-02 | PRIM-02 |

**Plan count:** 6
