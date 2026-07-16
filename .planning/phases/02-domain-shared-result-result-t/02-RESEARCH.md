# Phase 2: Domain.Shared: Result / Result<T> - Research

**Researched:** 2026-07-16
**Domain:** C# operation-result pattern design (Ardalis.Result-equivalent + CSharpFunctionalExtensions-style railway combinators, hand-rolled)
**Confidence:** HIGH (Ardalis.Result and CSharpFunctionalExtensions source mechanics — both fetched directly from GitHub this session), MEDIUM (CriticalError/Exception design guidance — grounded in a named practitioner source, not a spec), LOW (raw WebSearch-only findings, flagged inline)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Error representation**
- **D-01:** A failed `Result`/`Result<T>` carries a **list of structured errors**, not a single string or single code+message pair. Each error is a `Code` (string) + `Message` (string) pair.
- **D-02:** `Code` is a **plain string**, not a fixed enum. Freeform, dot-namespaced convention expected (e.g., `"Validation.Required"`, `"Guard.OutOfRange"`).
- **D-03:** `Message` is **required** (non-empty) on every error.
- **D-04:** `Result`/`Result<T>` expose **both** `.Errors` (full list) and `.Error` (convenience accessor for the first error).

**Result vs. exception boundary**
- **D-05:** `Result` is for expected business failures; throwing remains the path for invariant/programmer errors. Guard clauses are NOT retrofitted to return `Result` instead of throwing.
- **D-06:** Accessing `.Value` on a **failed** `Result<T>` throws `System.InvalidOperationException` (standard BCL exception, no new exception type).

**Status richness**
- **D-07:** Build the **full richer `ResultStatus` enum now**, not a bare success/failure boolean.
- **D-08:** `ResultStatus` includes: `Ok`, `Error`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`, `CriticalError`.
- **D-09:** Status is set via **named static factory methods per status** — `Result.Success()`, `Result.NotFound(...)`, `Result.Conflict(...)`, `Result.Invalid(errors)`, `Result.Forbidden(...)`, `Result.Unauthorized(...)`, `Result.Unavailable(...)`, `Result.CriticalError(exception)`, `Result.Error(...)` — not a single generic `Fail(status, errors)` factory.
- **D-10:** The **identical factory set applies to both `Result` and `Result<T>`**.
- **D-11 (CriticalError diagnostics):** `Result.CriticalError(exception)` **carries the original caught `Exception`** alongside its Code+Message error — distinct from a normal business-rule `Error`, which never wraps an exception.

**Combinators/chaining**
- **D-12:** Ships railway-style chaining combinators beyond the literal construction/inspection minimum: **Map**, **Bind/Then**, **OnSuccess/OnFailure**, **Match**, **Ensure**.
- **D-13:** Every combinator ships with **both sync and async (`Task<Result<T>>`) overloads** this phase.
- **D-14:** `Result<T>` supports an **implicit conversion from a bare value** (`T → Result<T>`).
- **D-15 (Combine):** `Result.Combine(r1, r2, r3, ...)` — succeeds only if all inputs succeed; on any failure, aggregates **all** errors across every failed input into one failed `Result`.

**Type shape**
- **D-16:** `Result` and `Result<T>` are **sealed classes**, not records or readonly structs.

**Naming & namespace**
- **D-17 (Claude's discretion, following established precedent):** Namespace expected to be `SentinelSuite.Framework.Domain.Shared.Results`, following Phase 1's `Domain.Shared.Guards` precedent.

### Claude's Discretion
- Exact namespace confirmation (D-17) — apply Phase 1's established `Domain.Shared.{Concept}` sub-namespace convention.
- Exact type name for the error record (e.g., `Error` vs `ResultError`) — avoid collision with any BCL/common type names.
- Internal implementation details of `Combine` (D-15) when mixing `Result` and `Result<T>` inputs.
- File/class organization within the `Results` sub-namespace (how many files, how combinators are grouped) — matching Phase 1's per-concern file split precedent.

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope. All seven discussed areas resolved as in-scope decisions, not deferred.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PRIM-02 | `Result` / `Result<T>` — hand-rolled operation-result pattern (Ardalis.Result-equivalent) for expected failure paths | Full Ardalis.Result source mechanics (ResultStatus, factory methods, Map/Bind implementation, implicit conversions) fetched and quoted directly from `github.com/ardalis/Result` this session. Full CSharpFunctionalExtensions source mechanics (Ensure, Combine, and the Left/Right/Both async-overload pattern that avoids combinatorial explosion) fetched and quoted directly from `github.com/vkhorikov/CSharpFunctionalExtensions` this session. Together these give the planner everything needed to spec a complete, correct hybrid implementation matching this phase's expanded (Ardalis-status-set + CSharpFunctionalExtensions-combinator-set) scope. Package Legitimacy Audit confirms zero NuGet packages are introduced. |
</phase_requirements>

## Summary

This phase's CONTEXT.md locks in a **hybrid design** that does not map onto a single reference library. The `ResultStatus` enum, named static factories, and Result/Result&lt;T&gt; parity (D-07 through D-11) match **Ardalis.Result**'s actual shape almost exactly — confirmed this session by fetching `Result.cs`, `Result.Void.cs`, `ResultStatus.cs`, `IResult.cs`, and `ResultExtensions.cs` directly from `github.com/ardalis/Result`. But the five railway combinators with sync+async overloads (D-12/D-13) and `Combine` (D-15) are **not** things Ardalis.Result implements at all — that library ships only `Map` and `Bind`, nothing else. `Ensure`, `OnSuccess`/`OnFailure`, `Match`, and `Combine` are the signature feature set of a *different* well-known library, **CSharpFunctionalExtensions**, whose actual source (fetched this session from `github.com/vkhorikov/CSharpFunctionalExtensions`) supplies the correct implementation pattern for all of them. The planner should treat this phase as "Ardalis.Result's status/factory surface, grafted onto CSharpFunctionalExtensions' combinator surface" — both real, well-established patterns, but from two different libraries, neither of which this project may depend on (per `PROJECT.md`).

Three concrete divergences from the reference libraries need explicit planner attention, all already flagged as intentional in CONTEXT.md but worth restating with hard evidence: (1) Ardalis.Result's `Errors` property is `IEnumerable<string>` — plain strings, not structured `Code`+`Message` pairs; only its separate `Invalid`/`ValidationError` path has a richer shape (`Identifier`, `ErrorMessage`, `ErrorCode`, `Severity`). D-01's structured-error-list design is a deliberate, well-motivated departure — closer to `ValidationError`'s shape than to `Errors`' shape, applied uniformly to every status instead of just `Invalid`. (2) Neither reference library's `CriticalError`/failure path carries an `Exception` object — Ardalis.Result's `CriticalError` takes only `params string[] errorMessages`, and a directly-relevant practitioner source (Vladimir Khorikov, author of CSharpFunctionalExtensions) explicitly argues **against** attaching exceptions to `Result` objects at all ("saving the exception... defeats the purpose of converting exceptions into `Result`"). D-11 is a deliberate, locked departure from both — implement it, but the planner should record this as a documented, discussed tradeoff, not an oversight. (3) Both reference libraries use **non-sealed reference/value types** — Ardalis.Result's `Result`/`Result<T>` are plain (unsealed) classes, and CSharpFunctionalExtensions' `Result` is a `partial struct`. D-16's "sealed class" requirement matches neither library's actual implementation; it is consistent with this project's own Phase 1 `Guard` precedent instead, which is the correct grounding to cite (not "matches Ardalis.Result's actual implementation," which is inaccurate).

The combinatorial-explosion problem (sync source × async source × sync continuation × async continuation = 4 call-site shapes per combinator) has a well-established idiomatic solution, confirmed directly from CSharpFunctionalExtensions' source layout: one base synchronous method declared on the `Result`/`Result<T>` type itself, plus three extension-method variants per combinator — conventionally suffixed/named for "Left operand async" (`Task<Result<T>>` source, sync continuation), "Right operand async" (sync source, `Task`-returning continuation), and "Both operands async" (`Task<Result<T>>` source, `Task`-returning continuation, which simply awaits the source and delegates to the Right-operand overload). This is a file-per-shape organization (`Bind.Task.Left.cs`, `Bind.Task.Right.cs`, `Bind.Task.cs`), not a single flat static class — directly informs this phase's recommended project structure.

**Primary recommendation:** Implement `ResultStatus`/status-factory surface following Ardalis.Result's actual shape verbatim (renaming to match D-01's structured-error-list instead of `IEnumerable<string>`), and implement the five combinators following CSharpFunctionalExtensions' Left/Right/Both async-overload file-per-shape pattern verbatim (adapted to the structured-error-list `Result` type). Do not attempt to unify these into "one canonical Ardalis.Result-equivalent" mental model — they are two different, well-attested patterns intentionally combined by this session's decisions.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Operation-result construction/inspection (`Result`, `Result<T>`, `ResultStatus`, structured `Error`) | Domain.Shared (shared kernel library) | — | Pure, side-effect-free data type with zero dependencies on persistence, HTTP, or tenancy — same category as Phase 1's `Guard`. Every later tier (Domain entities returning validation results, future UseCases layer returning application results) depends on this same static surface. |
| Railway-style combinators (Map/Bind/OnSuccess/OnFailure/Match/Ensure, Combine) | Domain.Shared (shared kernel library) | — | Pure static extension methods over the `Result`/`Result<T>` types above; no I/O, no tenancy — belongs in the same assembly as the types they extend. |
| Async overloads of combinators (`Task<Result<T>>`) | Domain.Shared (shared kernel library) | — | Still pure with respect to *this* layer — no actual I/O exists yet in Domain.Shared, but the async surface is deliberately front-loaded (D-13) so future async UseCases/Infrastructure code has zero-friction chaining against `Result`-returning async operations. |
| Test coverage of Result/combinator behavior | Test project (`SentinelSuite.Framework.Domain.Shared.Tests`, MTP-hosted) | — | Existing xUnit v3 test project from Phase 1; new `Results/` test subfolder mirrors the `Guards/` precedent. |

## Standard Stack

### Core
This phase introduces **zero production NuGet packages** by design (dependency-minimalism constraint, `PROJECT.md`). "Standard stack" here means the two hand-rolled patterns being reproduced and combined, not an installable package.

| Pattern reproduced | Source of truth | Purpose | Why this shape |
|---------------------|------------------|---------|-----------------|
| `ResultStatus` enum + named static factory methods, `Result`/`Result<T>` parity | `Ardalis.Result` (current: source fetched directly from `main` branch this session), MIT license [CITED: github.com/ardalis/Result/blob/main/src/Ardalis.Result/{Result.cs,Result.Void.cs,ResultStatus.cs,IResult.cs}] | Gives every one of the 26 future modules a consistent, HTTP-status-shaped vocabulary for expected business outcomes | This is the actual mechanism Ardalis.Result ships (not inference) — confirmed by direct source read |
| Map/Bind/Ensure/OnSuccess-OnFailure/Match railway combinators with Left/Right/Both async overload split | `CSharpFunctionalExtensions` (current: source fetched directly from `master` branch this session), MIT license [CITED: github.com/vkhorikov/CSharpFunctionalExtensions/tree/master/CSharpFunctionalExtensions/Result/Methods/Extensions] | Solves the 4-combination (sync/async source × sync/async continuation) overload-explosion problem via a proven file-per-shape convention | This is the actual pattern that library uses in production for exactly this problem — not a hypothetical design |
| `Result.Combine(...)` aggregation | `CSharpFunctionalExtensions` `Combine.cs` [CITED: github.com/vkhorikov/CSharpFunctionalExtensions/blob/master/CSharpFunctionalExtensions/Result/Methods/Combine.cs] | All-or-nothing combination of independent Results with full error aggregation | Confirmed actual signature shapes (`Combine(IEnumerable<Result>)`, `Combine(params Result[])`, generic `Combine<T>(params Result<T>[])`) to adapt for this project's structured-error-list shape |

### Supporting

No additional NuGet packages. This phase reuses the existing `SentinelSuite.Framework.Domain.Shared.Tests` project (xUnit v3 / MTP, `coverlet.mtp`) established in Phase 1 — no new test-project scaffolding needed, only new test files under a `Results/` subfolder.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled `Result`/`Result<T>` (Ardalis-status-shape + CSharpFunctionalExtensions-combinator-shape hybrid) | Actual `Ardalis.Result` NuGet package | Explicitly excluded by `PROJECT.md`/CLAUDE.md constraint; also doesn't ship Ensure/OnSuccess/Match/Combine at all, so it wouldn't satisfy this phase's locked scope even if the dependency were allowed |
| Hand-rolled combinators | Actual `CSharpFunctionalExtensions` NuGet package | Also excluded by dependency-minimalism constraint (not explicitly named in CLAUDE.md's excluded list, but falls under the same "every dependency lengthens FedRAMP authorization" rule as the named Ardalis packages); its `Result` is a struct, not the sealed class D-16 requires, so it would need modification anyway even if taken as a dependency |
| `IEnumerable<string> Errors` (Ardalis.Result's actual shape) | Structured `IReadOnlyList<Error>` where `Error` is a `{Code, Message}` record/class (D-01) | User's explicit, deliberate choice (D-01) — richer, machine-parseable, and forward-compatible with 26 future modules minting their own dot-namespaced codes; costs slightly more implementation surface than Ardalis.Result's plain strings |
| Non-sealed class (Ardalis.Result) or `partial struct` (CSharpFunctionalExtensions) | Sealed class (D-16) | User's explicit choice, consistent with Phase 1's `Guard` sealed-class precedent; reference-type semantics with no inheritance-extension point, matching this kernel's existing convention rather than either upstream library's actual shape |

**Installation:** None — zero packages to install in `Domain.Shared`. Existing test project already references `xunit.v3` + `coverlet.mtp` from Phase 1; no `dotnet add package` commands needed this phase.

**Version verification:** N/A — no packages installed. `Ardalis.Result` and `CSharpFunctionalExtensions` source was fetched directly from each project's GitHub `main`/`master` branch this session (not from NuGet, since neither package is being taken as a dependency — only their patterns are studied).

## Package Legitimacy Audit

> This phase installs **zero production NuGet packages** (the entire point of PRIM-02, matching PRIM-01's precedent). No new test-project packages are needed either — Phase 1 already established `xunit.v3` + `coverlet.mtp` in `SentinelSuite.Framework.Domain.Shared.Tests`, and this phase adds only new test files, not new package references.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| *(none — zero packages installed this phase)* | — | — | — | — | — | N/A |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*No packages were introduced this phase, so no legitimacy check was required. `Ardalis.Result` and `CSharpFunctionalExtensions` are studied as reference implementations only (source fetched directly from their official GitHub repositories, MIT-licensed, both long-established and widely used in the .NET ecosystem) — neither is added as a `PackageReference` anywhere in this project.*

## Architecture Patterns

### System Architecture Diagram

```
Any caller (future Domain entity constructor/method,
future UseCases validator, future guard-clause-adjacent code)
        │
        │  return Result<T>.Success(value);         (or `return value;` via D-14 implicit conversion)
        │  return Result<T>.NotFound(errors);
        │  return Result.Invalid(errors);
        ▼
┌─────────────────────────────────────────────────────────────┐
│  Result / Result<T>  (sealed classes, Results sub-namespace)  │
│                                                                 │
│    ResultStatus Status   { Ok, Error, Invalid, NotFound,       │
│                             Conflict, Forbidden, Unauthorized,  │
│                             Unavailable, CriticalError }        │
│    IReadOnlyList<Error> Errors   (structured Code+Message)      │
│    Error? Error                  (first error convenience)      │
│    Exception? Exception          (set only for CriticalError)   │
│    bool IsSuccess / IsFailure                                   │
│    T Value  (Result<T> only — throws InvalidOperationException  │
│              if accessed on a failed Result<T>)                  │
│                                                                   │
│    static factories: Success/Error/Invalid/NotFound/Conflict/    │
│      Forbidden/Unauthorized/Unavailable/CriticalError            │
│      — identical set on Result and Result<T>                    │
└───────────────────┬───────────────────────────────────────────┘
                     │
                     │  chained via combinators (each with
                     │  sync + Left-async + Right-async + Both-async
                     │  overloads — see Pattern 2 below)
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Combinators (extension methods, Results sub-namespace)        │
│    Map        — transform success value, short-circuit on fail │
│    Bind/Then  — chain to another Result-returning op, flatten   │
│    OnSuccess  — side-effect on success, Result unchanged        │
│    OnFailure  — side-effect on failure, Result unchanged        │
│    Match      — collapse Result into a single value             │
│    Ensure     — turn success into failure if predicate fails    │
│    Combine    — all-or-nothing aggregate of N independent        │
│                 Results; failure = union of every failed         │
│                 input's Errors list                              │
└───────────────────┬───────────────────────────────────────────┘
                     │
                     ▼
              back to caller — `result.Match(onSuccess, onFailure)`
              or `result.IsSuccess` / `result.Errors` inspected
              directly at the boundary (e.g., future Application
              layer translating Result → HTTP response)
```

### Recommended Project Structure
```
SentinelSuite.Framework.Domain.Shared/
├── Guards/                                    # Phase 1 — unchanged
├── Results/
│   ├── ResultStatus.cs                        # enum: Ok, Error, Invalid, NotFound,
│   │                                           #   Conflict, Forbidden, Unauthorized,
│   │                                           #   Unavailable, CriticalError
│   ├── Error.cs                                # sealed record/class: Code (string), Message (string)
│   ├── Result.cs                               # sealed class: status, Errors, Error, Exception,
│   │                                           #   IsSuccess/IsFailure, all static factories
│   ├── ResultOfT.cs                            # sealed class Result<T> : mirrors Result's factory
│   │                                           #   set + Value with fail-fast access + implicit
│   │                                           #   conversion from T
│   ├── ResultMapExtensions.cs                  # Map — sync + Left/Right/Both async overloads
│   ├── ResultBindExtensions.cs                 # Bind/Then — sync + Left/Right/Both async overloads
│   ├── ResultOnSuccessOnFailureExtensions.cs   # OnSuccess/OnFailure — sync + async overloads
│   ├── ResultMatchExtensions.cs                # Match — sync + async overloads
│   ├── ResultEnsureExtensions.cs               # Ensure — sync + async overloads
│   └── ResultCombineExtensions.cs              # Result.Combine(...) static aggregation methods
└── SentinelSuite.Framework.Domain.Shared.csproj

SentinelSuite.Framework.Domain.Shared.Tests/
├── Guards/                                    # Phase 1 — unchanged
├── Results/
│   ├── ResultConstructionTests.cs              # success/failure state transitions (Success criterion 2)
│   ├── ResultErrorTests.cs                     # error code/message propagation (Success criterion 3)
│   ├── ResultOfTValueAccessTests.cs            # failed Result<T>.Value throws (Success criterion 4)
│   ├── ResultStatusFactoryTests.cs             # every named factory sets the right ResultStatus,
│   │                                           #   identical behavior on Result and Result<T>
│   ├── ResultCriticalErrorTests.cs             # CriticalError carries the original Exception
│   ├── ResultMapTests.cs / ResultBindTests.cs / ResultEnsureTests.cs / ResultMatchTests.cs /
│   │   ResultOnSuccessOnFailureTests.cs        # sync-path combinator behavior, short-circuit on failure
│   ├── ResultAsyncCombinatorTests.cs           # async overload coverage (Task<Result<T>> source and/or
│   │                                           #   Task-returning continuation, at least one case per combinator)
│   └── ResultCombineTests.cs                   # all-success passthrough, aggregated-errors-on-any-failure,
│                                               #   mixed Result/Result<T> inputs
└── SentinelSuite.Framework.Domain.Shared.Tests.csproj
```

### Pattern 1: `ResultStatus` + identical named static factories on `Result` and `Result<T>` (D-07 through D-10)
**What:** A single `ResultStatus` enum; every named factory (`Success`, `Error`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`, `CriticalError`) exists on **both** `Result` and `Result<T>`, constructing an instance with the corresponding `Status` and populating `Errors` (this project's structured list, not Ardalis's plain strings).
**When to use:** Every construction call site.
**Example:**
```csharp
// Source: shape confirmed against ardalis/Result Result.cs, Result.Void.cs, ResultStatus.cs
// [CITED: github.com/ardalis/Result] — adapted here to this project's structured Error list (D-01)
// instead of Ardalis's IEnumerable<string> Errors, and to a sealed class instead of Ardalis's
// non-sealed class (D-16 — a deliberate departure, see Summary).
namespace SentinelSuite.Framework.Domain.Shared.Results;

public sealed class Result
{
    private static readonly IReadOnlyList<Error> NoErrors = Array.Empty<Error>();

    protected Result(ResultStatus status, IReadOnlyList<Error> errors, Exception? exception = null)
    {
        Status = status;
        Errors = errors;
        Exception = exception;
    }

    public ResultStatus Status { get; }
    public IReadOnlyList<Error> Errors { get; }
    public Error? Error => Errors.Count > 0 ? Errors[0] : null;
    public Exception? Exception { get; }
    public bool IsSuccess => Status == ResultStatus.Ok;
    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(ResultStatus.Ok, NoErrors);
    public static Result Error(params Error[] errors) => new(ResultStatus.Error, errors);
    public static Result Invalid(params Error[] errors) => new(ResultStatus.Invalid, errors);
    public static Result NotFound(params Error[] errors) => new(ResultStatus.NotFound, errors);
    public static Result Conflict(params Error[] errors) => new(ResultStatus.Conflict, errors);
    public static Result Forbidden(params Error[] errors) => new(ResultStatus.Forbidden, errors);
    public static Result Unauthorized(params Error[] errors) => new(ResultStatus.Unauthorized, errors);
    public static Result Unavailable(params Error[] errors) => new(ResultStatus.Unavailable, errors);

    public static Result CriticalError(Exception exception, Error? error = null) =>
        new(ResultStatus.CriticalError,
            new[] { error ?? new Error("CriticalError", exception.Message) },
            exception);
}
```
*(`Result<T>` mirrors this exactly, adding a `Value` property that throws `InvalidOperationException` when `IsFailure`, plus the `T → Result<T>` implicit conversion — see Pattern 3 and Pitfall 3.)*

### Pattern 2: Left/Right/Both async-overload split for every combinator (D-12, D-13)
**What:** Instead of one method per combinator, each combinator gets a **synchronous base method** on `Result`/`Result<T>` plus three extension-method variants covering every sync/async combination — confirmed directly from CSharpFunctionalExtensions' actual file layout (`Bind.cs`, `Bind.Task.Left.cs`, `Bind.Task.Right.cs`, `Bind.Task.cs`).
**When to use:** Every combinator in D-12 (Map, Bind, OnSuccess/OnFailure, Match, Ensure).
**Example:**
```csharp
// Source: pattern confirmed against vkhorikov/CSharpFunctionalExtensions
// Bind.cs, Bind.Task.Left.cs, Bind.Task.Right.cs [CITED: github.com/vkhorikov/CSharpFunctionalExtensions]
namespace SentinelSuite.Framework.Domain.Shared.Results;

public static class ResultBindExtensions
{
    // 1. Sync source, sync continuation — the base case
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> func) =>
        result.IsFailure ? Result<TOut>.Error(result.Errors.ToArray()) : func(result.Value);

    // 2. Async source ("Left operand async"), sync continuation
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Result<TOut>> func) =>
        (await resultTask.ConfigureAwait(false)).Bind(func);

    // 3. Sync source, async continuation ("Right operand async")
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func) =>
        result.IsFailure ? Result<TOut>.Error(result.Errors.ToArray()) : await func(result.Value).ConfigureAwait(false);

    // 4. Async source, async continuation ("Both operands async")
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> func) =>
        await (await resultTask.ConfigureAwait(false)).Bind(func).ConfigureAwait(false);
}
```
*(`Map`, `Ensure`, `OnSuccess`/`OnFailure`, and `Match` each need this same four-shape treatment. This is roughly 4× the method count per combinator, not a single method — plan file sizes and task-splitting accordingly; see Pitfall 1.)*

### Pattern 3: `T → Result<T>` implicit conversion (D-14)
**What:** `public static implicit operator Result<T>(T value) => new Result<T>(value);` lets a method with return type `Result<T>` `return value;` directly.
**When to use:** Every success-path return in future consumer code.
**Example:**
```csharp
// Source: confirmed verbatim against ardalis/Result Result.cs [CITED: github.com/ardalis/Result]
public static implicit operator Result<T>(T value) => new(value);
```
**Gotcha (from direct source inspection, not present in this phase's locked scope but worth flagging):** Ardalis.Result's actual `Result<T>` also implements the *reverse* conversion, `implicit operator T(Result<T> result) => result.Value`, which silently unwraps even a **failed** `Result<T>` (`Value` would be `default(T)` or throw, depending on implementation) with no compiler-visible signal that the unwrap could be lossy. D-14 only asks for the `T → Result<T>` direction — do **not** add the reverse conversion, since it would bypass D-06's fail-fast `InvalidOperationException` on failed-`Value` access entirely (the whole point of D-06). Flag this explicitly in the plan so no one "completes the pair" by analogy to Ardalis's actual shape.

### Anti-Patterns to Avoid
- **One flat static class with 5 methods for 5 combinators, sync-only.** Fails D-13 outright (async coverage required) and does not scale — CSharpFunctionalExtensions' own repo abandoned this shape in favor of file-per-sync/async-variant specifically because a flat class becomes unmaintainable once every combinator needs 4 overloads.
- **Reusing Ardalis.Result's plain-string `Errors` shape ("it's what the reference library does").** D-01 is a locked, deliberate departure — don't revert to strings under the banner of "matching upstream."
- **Silently making `Result`/`Result<T>` unsealed "for extensibility," reasoning from Ardalis.Result's actual (unsealed) shape.** D-16 is locked; sealed is correct per this project's own Phase 1 precedent, not per upstream.
- **Adding the reverse `Result<T> → T` implicit conversion "to match Ardalis.Result exactly."** See Pattern 3's gotcha — this would silently defeat D-06.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Overload explosion for sync/async combinator chaining | A naive cross-product of every combinator × every sync/async combination hand-invented from scratch | The Left/Right/Both extension-class convention (Pattern 2), copied structurally from CSharpFunctionalExtensions' actual, battle-tested file layout | This is a solved problem with a well-known idiomatic shape; reinventing it from first principles risks missing edge cases (e.g., `ConfigureAwait(false)` placement, which overload should own the failure short-circuit check) that the reference implementation already handles correctly |
| Structured error aggregation logic for `Combine` | Ad hoc string-concatenation or first-error-wins logic | The `Combine` shape in Architecture Patterns / Code Examples below (adapted from CSharpFunctionalExtensions' `Combine.cs`, which already handles the "which Results failed" filter-then-aggregate logic correctly) | Off-by-one aggregation bugs (e.g., silently dropping the second failure's errors) are exactly the kind of subtle defect a hand-rolled-from-scratch implementation risks; the reference shape (filter failures, project their errors, flatten) is already correct |

**Key insight:** Unlike Phase 1 (where hand-rolling was the entire point and the shape was simple), this phase's combinator surface is genuinely tricky to get right from scratch — the reference libraries exist specifically because naive implementations of "async Result chaining" accumulate subtle bugs (double-await, missing `ConfigureAwait`, wrong short-circuit ordering). Reproduce the *pattern*, not the package — but reproduce it faithfully.

## Runtime State Inventory

> N/A — this is a greenfield addition to `Domain.Shared` (new types in a new sub-namespace). No rename/refactor/migration involved. Skipping per this section's own scope note.

## Common Pitfalls

### Pitfall 1: Underestimating the method count for "5 combinators with sync+async overloads"
**What goes wrong:** D-13 sounds like "5 combinators × 2 (sync/async) = 10 methods." The actual reference-library shape is 5 combinators × up to 4 shapes (sync/sync, async-source/sync-continuation, sync-source/async-continuation, async/async) × (Result-returning + Result&lt;T&gt;-returning + T→Result&lt;T&gt; variants) = a much larger surface — CSharpFunctionalExtensions' `Bind` alone spans 8+ files.
**Why it happens:** "Async overload" is usually mentally modeled as one extra method, not a 2×2 combinatorial expansion across source-sync/async and continuation-sync/async.
**How to avoid:** Budget this phase's task breakdown assuming each combinator is its own multi-file unit of work (mirroring the Recommended Project Structure above — one file per combinator, each internally covering all 4 shapes), not a single quick method. Consider whether `Result` (non-generic) needs the full 4-shape treatment for every combinator, or whether some combinators (e.g., `OnFailure`, which never touches a value) can be simplified — flag as an Open Question below for planning to size explicitly.
**Warning signs:** A plan that allocates the same effort to "add Ensure" as it did to "add EnumOutOfRange guard" in Phase 1 — the complexity is not comparable.

### Pitfall 2: `CriticalError` carrying an `Exception` contradicts both reference libraries and a named practitioner's explicit guidance
**What goes wrong:** Neither Ardalis.Result nor CSharpFunctionalExtensions attaches an `Exception` object to a failure/Result type. Vladimir Khorikov (CSharpFunctionalExtensions' author) has written directly on this exact question and argues against it: "Saving the exception or its stack trace to `Result` defeats the purpose of converting exceptions into `Result`" — his position is that if you need the stack trace, you should be throwing, not converting to `Result` [CITED: khorikov.org/posts/2022-06-27-stack-trace-in-result].
**Why it happens:** D-11 is a deliberate, discussed, locked departure from both reference shapes and from this specific practitioner guidance — the user explicitly wants `CriticalError` to be the one status that preserves a caught exception for logging at a boundary.
**How to avoid:** Implement it (D-11 is locked, not a suggestion) — but as a top-level nullable `Exception? Exception { get; }` property on `Result`, populated **only** by the `CriticalError` factory (every other factory leaves it `null`), not embedded inside the structured `Error` entry itself (an `Error.Code`/`Error.Message` pair should stay serializable/loggable without dragging exception internals through every code path that touches `.Errors`). Document in the type's XML remarks *why* this diverges from both reference libraries — a future contributor or code reviewer familiar with Ardalis.Result/CSharpFunctionalExtensions may otherwise flag this as an accidental deviation rather than a deliberate one.
**Warning signs:** A future PR "simplifying" `Result` by removing the `Exception` property because "Ardalis.Result doesn't have this" — the XML-doc rationale is the guard against that regression.

### Pitfall 3: `Result<T>.Value` on a failed instance — three different failure behaviors exist in the wild, only one is D-06-compliant
**What goes wrong:** Ardalis.Result's actual `Value` property is a plain auto-property with no guard at all (`public T Value { get; init; }`) — accessing it on a failed `Result<T>` silently returns `default(T)`, not a thrown exception. This is the opposite of D-06's requirement.
**Why it happens:** Ardalis.Result optimizes for a scenario (ASP.NET Core action-filter mapping) where the caller checks `Status`/`IsSuccess` before ever touching `Value`; it doesn't defend against a caller skipping that check.
**How to avoid:** This project's `Result<T>.Value` getter must explicitly check `IsFailure` and `throw new InvalidOperationException(...)` before returning the backing field — do not copy Ardalis.Result's auto-property shape verbatim; this is exactly the kind of subtle divergence direct source inspection catches that "matches Ardalis.Result" documentation-level claims (like this phase's D-06 comment) can miss if not checked against the actual code.
**Warning signs:** A unit test asserting `Assert.Throws<InvalidOperationException>(() => failedResult.Value)` failing because `Value` silently returned `default(T)` instead — this is Success Criterion 4 from the roadmap, so it will be directly tested; get the getter shape right the first time.

### Pitfall 4: `Result.Combine` mixing `Result` and `Result<T>` inputs needs an explicit overload strategy (left to planning per CONTEXT.md, but the shape exists in the reference library)
**What goes wrong:** A naive `Combine(params Result[] results)` signature can't accept `Result<T>` instances directly unless `Result<T>` either inherits from `Result` (Ardalis.Result's actual approach — `Result : Result<Result>`, an unusual self-referential-generic inheritance shape) or there's an implicit/explicit conversion path.
**Why it happens:** C#'s type system doesn't let you `params`-collect a mix of `Result` and `Result<T>` (for varying `T`) into one array without a common base type or conversion.
**How to avoid:** CSharpFunctionalExtensions' actual approach (confirmed from source) is simpler and better-suited here: provide **separate overloads** — `Combine(params Result[] results)` and `Combine<T>(params Result<T>[] results)` — rather than Ardalis.Result's inheritance trick (`Result : Result<Result>`), which is a genuinely unusual C# shape not worth reproducing just for `Combine`'s sake. Both overloads project each input's `.Errors` list, flatten, and construct one aggregate failed `Result` — this matches D-15's "aggregates all errors across every failed input" requirement without needing `Result`/`Result<T>` to share an inheritance relationship. Confirm this overload strategy explicitly during planning (CONTEXT.md already flags this as Claude's Discretion).
**Warning signs:** A design that tries to make `Result<T>` inherit from `Result` (or vice versa) "to make Combine easier" — resist; two separate, simple overloads are less surprising and don't complicate D-16's sealed-class requirement (a sealed class can't be a non-sealed base of anything, and D-16 requires sealing both).

### Pitfall 5: `[CriticalError]`'s `Error` entry must still satisfy D-03 (`Message` required, non-empty)
**What goes wrong:** It's tempting to construct the `CriticalError` factory's `Error` entry directly from `exception.Message`, which can be `null` or empty for some exception types (e.g., a bare `new Exception()` with no message argument has a framework-default message, but custom exception subclasses can override `Message` to return an empty string in edge cases).
**Why it happens:** Exception messages are not guaranteed non-empty by the BCL the way this phase's own `Error.Message` invariant (D-03) requires.
**How to avoid:** The `CriticalError(Exception exception, Error? error = null)` factory should default to a **fixed, non-empty fallback message** (e.g., `"An unexpected error occurred."`) when no explicit `Error` is supplied and `exception.Message` is null/empty — never pass a potentially-empty string straight through into a field that D-03 declares required.
**Warning signs:** A unit test constructing `Result.CriticalError(new SomeCustomException())` where `SomeCustomException.Message` returns `""` — if this test isn't in the plan already, add it; it's the concrete edge case D-03 exists to prevent.

## Code Examples

### `ResultStatus` enum
```csharp
// Source: shape confirmed against ardalis/Result ResultStatus.cs [CITED: github.com/ardalis/Result]
// This project's set matches D-08 exactly (Ardalis.Result's actual enum additionally has
// Created and NoContent, both HTTP-201/204-flavored concepts with no meaning yet in a
// persistence-agnostic Domain.Shared kernel — correctly excluded per D-08's explicit list).
namespace SentinelSuite.Framework.Domain.Shared.Results;

public enum ResultStatus
{
    Ok,
    Error,
    Invalid,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized,
    Unavailable,
    CriticalError
}
```

### Structured `Error` type (D-01 through D-04)
```csharp
// Source: shape informed by ardalis/Result ValidationError.cs's Identifier/ErrorMessage/ErrorCode
// fields [CITED: github.com/ardalis/Result] — adapted to D-01's uniform Code+Message pair applied
// to every status, not just Invalid.
namespace SentinelSuite.Framework.Domain.Shared.Results;

public sealed class Error
{
    public Error(string code, string message)
    {
        Code = Guard.Against.NullOrWhiteSpace(code);      // reuse Phase 1's Guard — D-03/D-02
        Message = Guard.Against.NullOrWhiteSpace(message); // D-03: Message required, non-empty
    }

    public string Code { get; }
    public string Message { get; }
}
```
*(Reusing Phase 1's `Guard.Against.NullOrWhiteSpace` here is a natural cross-phase integration point — Phase 1 is already complete and its guards are available. Confirm during planning whether `Domain.Shared.Results` should take this internal dependency on `Domain.Shared.Guards`, both being the same assembly — this should be uncontroversial since both live in `Domain.Shared` already, but call it out explicitly as a design choice.)*

### `Result<T>.Value` fail-fast getter (D-06 — see Pitfall 3)
```csharp
// Source: pattern is this project's own design — deliberately diverges from
// ardalis/Result's Result.cs actual `public T Value { get; init; }` (no guard) [CITED: github.com/ardalis/Result]
namespace SentinelSuite.Framework.Domain.Shared.Results;

public sealed class Result<T>
{
    private readonly T? _value;

    // ... constructors/factories mirroring Result (Pattern 1) ...

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                $"Cannot access {nameof(Value)} on a failed Result (Status: {Status}).");

    public static implicit operator Result<T>(T value) => Success(value); // D-14 — one direction ONLY, see Pattern 3 gotcha
}
```

### `Result.Combine` (D-15 — see Pitfall 4)
```csharp
// Source: aggregation strategy confirmed against vkhorikov/CSharpFunctionalExtensions Combine.cs
// [CITED: github.com/vkhorikov/CSharpFunctionalExtensions] — adapted to this project's
// structured Error list instead of string-message concatenation.
namespace SentinelSuite.Framework.Domain.Shared.Results;

public static class ResultCombineExtensions
{
    public static Result Combine(params Result[] results)
    {
        var failed = results.Where(r => r.IsFailure).ToList();
        return failed.Count == 0
            ? Result.Success()
            : Result.Error(failed.SelectMany(r => r.Errors).ToArray());
    }

    public static Result Combine<T>(params Result<T>[] results)
    {
        var failed = results.Where(r => r.IsFailure).ToList();
        return failed.Count == 0
            ? Result.Success()
            : Result.Error(failed.SelectMany(r => r.Errors).ToArray());
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| Ad hoc "one method per async shape" Result-chaining libraries | Left/Right/Both async-operand extension-class convention (CSharpFunctionalExtensions) | Established convention in this library for several major versions; no recent breaking change found this session | Directly informs this phase's file/method organization (Pattern 2, Recommended Project Structure) |
| Exceptions as the only failure-signaling mechanism | Result/Either-style explicit failure types for *expected* business failures, exceptions reserved for *unexpected* ones | Long-standing functional-programming-influenced .NET convention (Ardalis.Result, CSharpFunctionalExtensions, LanguageExt, FluentResults all ship variants) | Matches D-05's Result-vs-exception boundary exactly; this phase is adopting an already-mainstream .NET pattern, not inventing one |

**Deprecated/outdated:**
- None identified specific to this phase's scope — both reference libraries' source was fetched from their current default branch this session, no legacy-vs-current API split found for the specific mechanics studied (ResultStatus, factories, Map/Bind/Ensure/Combine).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `Ardalis.Result` and `CSharpFunctionalExtensions` source was fetched via `raw.githubusercontent.com`/`api.github.com` WebFetch this session rather than a first-party doc-lookup tool (no Context7/MCP doc tool available); the `gsd-tools classify-confidence` seam tags `webfetch`/`websearch` provenance as LOW regardless of the fact that the actual content fetched is a direct, verbatim, primary-source code read (not a summary or search-result synthesis) | Standard Stack, Architecture Patterns, Code Examples | Low — the fetched content is the literal source file text from each project's official GitHub repository (`github.com/ardalis/Result`, `github.com/vkhorikov/CSharpFunctionalExtensions`), which is as close to ground truth as this domain gets short of running the actual library; the LOW tag reflects tooling-provenance classification, not epistemic uncertainty about the code shown |
| A2 | Vladimir Khorikov's blog post (khorikov.org) is treated as a directly-relevant practitioner source for Pitfall 2's "don't attach exceptions to Result" guidance, since he is CSharpFunctionalExtensions' author — but this is still a personal blog opinion, not a spec or a cross-checked industry consensus | Common Pitfalls, Pitfall 2 | Low — D-11 is a locked user decision that overrides this guidance anyway; the citation exists to make the tradeoff visible for the planner/future reviewers, not to argue against the locked decision |
| A3 | `Result.Combine`'s overload strategy (separate `Combine(params Result[])` / `Combine<T>(params Result<T>[])` rather than Ardalis.Result's `Result : Result<Result>` inheritance trick) is this research's recommendation, not something independently confirmed against a third source — CONTEXT.md explicitly leaves this to planning discretion | Common Pitfalls, Pitfall 4; Code Examples | Low — both overload shapes are simple, standard C# `params` overloading with no exotic type-system interaction; if planning chooses a different strategy (e.g., a shared non-generic base after all), the sealed-class requirement (D-16) would need re-examination, since D-16 requires *both* `Result` and `Result<T>` to be sealed, which is incompatible with an inheritance relationship between them |

**If this table is empty:** N/A — see entries above.

## Open Questions

1. **Should every combinator (Map/Bind/OnSuccess/OnFailure/Match/Ensure) get the full 4-shape (sync/async × source/continuation) treatment, or can some be safely narrowed?**
   - What we know: CSharpFunctionalExtensions implements all 4 shapes for `Bind`, `Map`, `Ensure`, and several others uniformly. `OnFailure` conceptually never needs the value (it only fires when `IsFailure`), so its async-continuation shapes could arguably be simpler than `Bind`'s.
   - What's unclear: Whether narrowing `OnFailure`'s overload set (relative to `Bind`'s) would create an inconsistent, surprising API surface across the five combinators, versus the effort savings of not building every shape for every combinator.
   - Recommendation: Default to full 4-shape parity across all five combinators for API consistency (matches D-13's "every combinator... both sync and async" wording literally) unless the planner determines during task-sizing (Pitfall 1) that this meaningfully blows the phase's budget — in which case, deliberately narrow only `OnSuccess`/`OnFailure` (the two combinators that don't need a `TOut` type parameter) and document the narrowing explicitly as a scoped decision, not an oversight.

2. **Does `Error` need value equality (records) or is reference-type class equality sufficient?**
   - What we know: D-16 locks `Result`/`Result<T>` as sealed classes; CONTEXT.md's Claude's-Discretion section explicitly leaves the error type's exact shape/name to planning, but doesn't address equality semantics.
   - What's unclear: Whether future test code or consumer code will want `new Error("X", "Y") == new Error("X", "Y")` to be `true` (useful for `Assert.Contains(expectedError, result.Errors)`-style test assertions) — a plain `sealed class` (Code Examples above) would need `Equals`/`GetHashCode` overrides or an `IEquatable<Error>` implementation to support this; a `sealed record` gets it for free but wasn't explicitly locked by D-16 (which only addresses `Result`/`Result<T>`, not the error type).
   - Recommendation: Make `Error` a `sealed record` (not `record class` — needs no distinction here since it has no positional members required) rather than a plain sealed class — value-equality is almost certainly wanted for the test-assertion use case described in this phase's own Success Criteria (verifying error code/message propagation), and D-16 only constrains `Result`/`Result<T>`'s shape, not `Error`'s. Confirm during planning; this is a small, low-risk deviation from the plain-class Code Example above shown for illustration.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|--------------|-----------|---------|----------|
| .NET 10 SDK | Compiling `Domain.Shared` and the existing test project | ✓ | 10.0.201 (confirmed via `dotnet --list-sdks`) | — |
| `SentinelSuite.Framework.Domain.Shared.Tests` project (xUnit v3 / MTP, `coverlet.mtp`) | Test coverage for this phase | ✓ (already exists from Phase 1, confirmed via file listing) | — | — |
| `global.json` (routes `dotnet test` through MTP) | Running the full test suite | ✓ (already exists from Phase 1) | — | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none — this phase's environment needs are a strict subset of what Phase 1 already established.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit v3 (`xunit.v3` 3.2.2) on Microsoft.Testing.Platform — established in Phase 1, unchanged |
| Config file | `SentinelSuite/global.json` (exists from Phase 1) |
| Quick run command | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter Results` |
| Full suite command | `dotnet test` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|--------------------|---------------|
| PRIM-02 | `Result`/`Result<T>` compile with zero third-party `PackageReference` entries | build/static check | `dotnet build SentinelSuite.Framework.Domain.Shared/SentinelSuite.Framework.Domain.Shared.csproj` + manual `.csproj` inspection | ❌ Wave 0 |
| PRIM-02 | Success/failure state transitions (`IsSuccess`/`IsFailure` correct per constructed status) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultConstructionTests` | ❌ Wave 0 |
| PRIM-02 | Error code/message propagation from a failure `Result` (`.Errors`, `.Error` convenience accessor) | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultErrorTests` | ❌ Wave 0 |
| PRIM-02 | Failed `Result<T>.Value` throws `InvalidOperationException` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultOfTValueAccessTests` | ❌ Wave 0 |
| PRIM-02 | All named `ResultStatus` factories exist identically on `Result` and `Result<T>` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultStatusFactoryTests` | ❌ Wave 0 |
| PRIM-02 | `CriticalError` carries the original `Exception` | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultCriticalErrorTests` | ❌ Wave 0 |
| PRIM-02 | Map/Bind/OnSuccess/OnFailure/Match/Ensure — sync short-circuit-on-failure behavior | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter "ResultMapTests|ResultBindTests|ResultEnsureTests|ResultMatchTests|ResultOnSuccessOnFailureTests"` | ❌ Wave 0 |
| PRIM-02 | Async overloads (`Task<Result<T>>`) for each combinator | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultAsyncCombinatorTests` | ❌ Wave 0 |
| PRIM-02 | `Result.Combine` — all-success passthrough, aggregated errors on any failure, mixed `Result`/`Result<T>` inputs | unit | `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter ResultCombineTests` | ❌ Wave 0 |

### Sampling Rate
- **Per task commit:** `dotnet run --project SentinelSuite.Framework.Domain.Shared.Tests --filter Results`
- **Per wave merge:** `dotnet test` (full suite, includes Phase 1's `Guards` tests unaffected)
- **Phase gate:** Full suite green before `/gsd-verify-work`

### Wave 0 Gaps
- [ ] `SentinelSuite.Framework.Domain.Shared/Results/` — new sub-namespace, does not exist
- [ ] `SentinelSuite.Framework.Domain.Shared.Tests/Results/` — new test subfolder, does not exist
- [ ] Framework install: none — reuses Phase 1's existing test project unchanged

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | No | This phase has no authentication surface |
| V3 Session Management | No | No session concept in a pure data/combinator type |
| V4 Access Control | No | No access-control surface; `Forbidden`/`Unauthorized` are vocabulary statuses this phase defines, not an access-control *mechanism* — enforcement is a future phase's concern |
| V5 Input Validation | Partial | `Error`'s constructor enforces D-03 (non-empty `Message`) via Phase 1's `Guard.Against.NullOrWhiteSpace` — this is invariant validation on the `Result` type's own construction, not a general input-validation surface for external data |
| V6 Cryptography | No | No cryptographic material handled |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| `CriticalError`'s `Exception` property leaking sensitive internal details (connection strings, file paths, stack frames) if a `Result` is ever serialized directly to an external response (future API layer) | Information Disclosure | Out of scope for *this* Domain.Shared-only phase (no serialization/HTTP boundary exists yet), but the `Exception` property's existence is a landmine for whichever future phase adds an API/Application layer that maps `Result` → HTTP response. Document in the `Result`/`CriticalError` XML remarks that `Exception` must never be included in any external-facing serialization of a `Result`, and that consuming code must strip it before crossing a trust boundary — flag this explicitly for the future API-layer phase's own security research rather than silently deferring it. |
| Structured `Error.Code`/`Error.Message` echoing raw exception or internal-state text back to a caller in a way that leaks implementation details (mirrors Phase 1's guard-clause Information Disclosure pitfall, same threat class) | Information Disclosure | Same discipline as Phase 1's Guard exceptions: `Error.Message` should be a human-readable, safe-to-display string; do not interpolate raw exception `Message`/`ToString()` output directly into a business-facing `Error.Message` without review — the `CriticalError` factory's default fallback message (Pitfall 5) is one place this specifically matters. |

## Sources

### Primary (HIGH confidence — direct source-code fetch from the official repository)
- `github.com/ardalis/Result` — `Result.cs` (contains `Result<T>`), `Result.Void.cs` (contains non-generic `Result : Result<Result>`), `ResultStatus.cs`, `IResult.cs`, `ResultExtensions.cs`, `ValidationError.cs` — fetched and quoted directly this session
- `github.com/vkhorikov/CSharpFunctionalExtensions` — `Result/Methods/Combine.cs`, `Result/Methods/Extensions/Ensure.cs`, `Result/Methods/Extensions/Bind.Task.Left.cs`, `Result/Methods/Extensions/Bind.Task.Right.cs`, directory listings of `Result/Methods/` and `Result/Methods/Extensions/` — fetched and quoted directly this session
- `dotnet --list-sdks` run directly against the local environment
- Direct `Read` of `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs`, `IGuardClause.cs`, and the existing `SentinelSuite.Framework.Domain.Shared.Tests` directory listing (this repo, Phase 1 precedent)

### Secondary (MEDIUM confidence — named practitioner source, not official spec/docs)
- `khorikov.org/posts/2022-06-27-stack-trace-in-result` — Vladimir Khorikov (author of CSharpFunctionalExtensions) on why exceptions/stack traces typically should not be attached to `Result` objects; directly relevant to D-11's CriticalError design, cited as a documented tradeoff, not a blocker (D-11 is locked)

### Tertiary (LOW confidence — WebSearch synthesis, used only to locate primary sources)
- General WebSearch result summaries used to identify candidate repository/file paths before direct fetch (not relied on for factual claims beyond pointing to primary sources)

## Metadata

**Confidence breakdown:**
- Standard stack (Ardalis.Result status/factory mechanics): HIGH — confirmed via direct source-code fetch from the official `ardalis/Result` repository this session
- Standard stack (CSharpFunctionalExtensions combinator/async-overload mechanics): HIGH — confirmed via direct source-code fetch from the official `vkhorikov/CSharpFunctionalExtensions` repository this session
- Architecture (hybrid design combining both patterns, adapted for structured errors + sealed classes): MEDIUM — the individual source patterns are HIGH confidence; the specific hybrid combination is this research's synthesis, not something either upstream library does, so treat the *combination* as reasoned design work grounded in verified primitives, not itself a verified fact
- Pitfalls: HIGH for Pitfalls 1, 3, 4 (each grounded in direct source-code comparison); MEDIUM for Pitfall 2 (grounded in a named practitioner's own blog post, not a spec) and Pitfall 5 (grounded in general BCL knowledge about `Exception.Message` nullability, not independently fetched this session — flagged as such)

**Research date:** 2026-07-16
**Valid until:** 30 days — both reference libraries (Ardalis.Result, CSharpFunctionalExtensions) are stable, slow-moving, mature MIT-licensed projects; re-verify only if this phase is delayed past that window or either project's source has since restructured its file layout
