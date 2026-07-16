# Phase 2: Domain.Shared: Result / Result<T> - Pattern Map

**Mapped:** 2026-07-16
**Files analyzed:** 19 (9 production + 10 test files, per RESEARCH.md's Recommended Project Structure)
**Analogs found:** 19 / 19 (all via a single strong in-repo precedent — Phase 1's `Guards/` implementation)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `SentinelSuite.Framework.Domain.Shared/Results/ResultStatus.cs` | model (enum) | CRUD (state vocabulary) | *(no direct analog — first enum in kernel)* | none (see below) |
| `SentinelSuite.Framework.Domain.Shared/Results/Error.cs` | model | CRUD (construction/validation) | `Guards/IGuardClause.cs` + `GuardAgainstNullExtensions.cs` (invariant-enforcing constructor pattern) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/Result.cs` | model / service (sealed class + static factories) | CRUD (construction/inspection) | `Guards/Guard.cs` (sealed class, static entry point) | exact (shape precedent) |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs` | model / service | CRUD (construction/inspection + fail-fast accessor) | `Guards/Guard.cs`, `GuardAgainstNullExtensions.cs` (fail-fast throw pattern) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs` | utility (extension methods) | transform | `Guards/GuardAgainstStringExtensions.cs` (extension-method-per-concern file organization) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs` | utility (extension methods) | transform / event-driven (chaining) | `Guards/GuardAgainstNullExtensions.cs` (delegates-to-another-guard-first pattern mirrors Bind's short-circuit) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs` | utility (extension methods) | event-driven (side-effect hooks) | `Guards/GuardAgainstStringExtensions.cs` (file-per-concern extension class) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs` | utility (extension methods) | transform | `Guards/GuardAgainstStringExtensions.cs` | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs` | utility (extension methods) | transform (predicate-gated) | `Guards/GuardAgainstRangeExtensions.cs` (predicate-driven guard-and-reject shape) | role-match |
| `SentinelSuite.Framework.Domain.Shared/Results/ResultCombineExtensions.cs` | utility (extension methods, batch) | batch (aggregate N inputs) | `Guards/GuardAgainstNullExtensions.cs`'s `NullOrEmpty<T>(IEnumerable<T>)` (materialize-then-validate-a-collection shape) | partial match |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultConstructionTests.cs` | test | CRUD | `Guards/GuardAgainstNullTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultErrorTests.cs` | test | CRUD | `Guards/GuardAgainstNullTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOfTValueAccessTests.cs` | test | CRUD (fail-fast) | `Guards/GuardAgainstNullTests.cs` (throws-with-message assertions) | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultStatusFactoryTests.cs` | test | CRUD | `Guards/GuardAgainstStringTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCriticalErrorTests.cs` | test | CRUD | `Guards/GuardAgainstNullTests.cs` | exact |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/Result{Map,Bind,Ensure,Match,OnSuccessOnFailure}Tests.cs` | test | transform/event-driven | `Guards/GuardAgainstRangeTests.cs`, `GuardAgainstStringTests.cs` | role-match |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultAsyncCombinatorTests.cs` | test | transform (async) | *(no async precedent exists in Phase 1 — Guards are all sync)* | none (see below) |
| `SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs` | test | batch | `Guards/GuardAgainstNullTests.cs` (collection-guard test shape) | partial match |

## Pattern Assignments

### `SentinelSuite.Framework.Domain.Shared/Results/Result.cs` and `ResultOfT.cs` (model/service, CRUD)

**Analog:** `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs`

**Sealed class + static entry-point pattern** (`Guard.cs` lines 29–39):
```csharp
public sealed class Guard : IGuardClause
{
    private Guard()
    {
    }

    public static IGuardClause Against { get; } = new Guard();
    ...
}
```
Copy the *shape*, not the mechanism: `Result`/`Result<T>` are `sealed class` per D-16, with a `private`/`protected` constructor and **named static factory methods** (`Success()`, `NotFound(...)`, etc.) as the only construction path — exactly how `Guard` exposes only `Guard.Against` as its entry point rather than a public constructor. RESEARCH.md's Pattern 1 code example already gives the concrete factory-method bodies to use; this analog is why that shape (sealed + static-factory-only) is the house convention, not just an Ardalis.Result coincidence.

**XML-doc convention** (`Guard.cs` lines 3–28, `IGuardClause.cs` lines 3–19): every public type carries a `<summary>` plus a `<remarks>` block explaining *why* the shape is what it is (naming convention, extensibility rationale, security rationale). `Result`'s `CriticalError`'s `Exception` property needs this same remarks treatment per RESEARCH.md Pitfall 2 — document why it diverges from Ardalis.Result/CSharpFunctionalExtensions directly in the XML remarks, mirroring how `Guard.cs`'s remarks pre-empt "why doesn't this look like upstream" confusion.

**Fail-fast accessor pattern** (`GuardAgainstNullExtensions.cs` lines 28–40, the `Null<T>` guard): guard-throw-return shape —
```csharp
public static T Null<T>(this IGuardClause guardClause, [NotNull] T? input, ...) where T : class
{
    if (input is null)
    {
        throw new ArgumentNullException(Guard.SafeParamName(parameterName));
    }
    return input;
}
```
`Result<T>.Value`'s getter (D-06) should follow this same check-then-throw-else-return shape (see RESEARCH.md's own Pitfall 3 code example, which already adapts this correctly to `IsFailure ? throw : return _value!`).

**No exact analog exists for `ResultStatus.cs`** (see "No Analog Found" below) — it is the kernel's first enum type; there is no existing enum in `Domain.Shared` to pattern-match against. Follow RESEARCH.md's own `ResultStatus` code example (already vetted against `ardalis/Result`'s `ResultStatus.cs`) directly.

---

### `SentinelSuite.Framework.Domain.Shared/Results/Error.cs` (model, CRUD)

**Analog:** `Guards/GuardAgainstNullExtensions.cs` (`NullOrWhiteSpace`, lines 71–84) + `Guards/Guard.cs`

**Invariant-enforcing constructor pattern:** `Error`'s constructor must guard its own `Code`/`Message` arguments the same way every existing guard method validates its own inputs before proceeding — reuse `Guard.Against.NullOrWhiteSpace(...)` directly (RESEARCH.md's own `Error.cs` code example already does this, at lines 386–390 of RESEARCH.md). This is a direct, deliberate cross-file dependency **within the same assembly** (`Domain.Shared.Results` → `Domain.Shared.Guards`), consistent with Phase 1 code never referencing external validation.

**Fixed, non-interpolated exception-message discipline** (`GuardAgainstStringExtensions.cs` lines 74–79 remarks): "None of the three methods in this file interpolate the rejected string value into their exception messages... per the Information-Disclosure mitigation." Apply the same discipline to `Error.Message`/`Error.Code` validation failures — if `Guard.Against.NullOrWhiteSpace` is reused directly it already gets this for free (its own message is a fixed literal).

---

### `SentinelSuite.Framework.Domain.Shared/Results/Result{Map,Bind,Ensure,Match,OnSuccessOnFailure,Combine}Extensions.cs` (utility, transform/event-driven/batch)

**Analog:** `Guards/GuardAgainstStringExtensions.cs`, `Guards/GuardAgainstNullExtensions.cs`

**File-per-concern static extension class organization** (`GuardAgainstStringExtensions.cs` lines 20–97; `GuardAgainstNullExtensions.cs` lines 22–138): each guard "family" (null, string, range, numeric, input) lives in its own `public static class GuardAgainst{Concept}Extensions` file, with each method an extension on `IGuardClause`. Directly mirrors RESEARCH.md's own recommended structure (one file per combinator: `ResultMapExtensions.cs`, `ResultBindExtensions.cs`, etc.) — this repo's own Phase 1 precedent is *why* that per-concern-file split (rather than one flat `ResultExtensions.cs`) is the correct convention here, independent of what CSharpFunctionalExtensions does upstream.

**Delegates-to-a-more-fundamental-check-first pattern** (`GuardAgainstNullExtensions.cs` lines 71–76, 92–97, 122–127 — every method calls `Guard.Against.Null(input, parameterName)` before doing its own check): this is the same "short-circuit on a more fundamental failure first" shape `Bind`/`Map`/`Ensure` need (check `IsFailure` and short-circuit before invoking the continuation). Copy this defensive-delegation habit directly.

**`CallerArgumentExpression` usage** (all guard methods): not directly applicable to `Result`'s combinators (no argument-name capture needed for `Map`/`Bind`), but establishes the project convention of using compiler-supported diagnostics attributes (`[NotNull]`, `[CallerArgumentExpression]`) wherever the BCL offers one — worth checking whether `Result<T>.Value`'s throw path or `Ensure`'s predicate-failure path can similarly leverage `[CallerArgumentExpression]` for a more useful `InvalidOperationException` message.

**No direct analog for the Left/Right/Both async-overload split** (D-13, RESEARCH.md Pattern 2) — Phase 1's guard clauses are 100% synchronous; nothing in `Guards/` demonstrates a `Task<T>`-returning overload. This is a genuine gap; RESEARCH.md's own Pattern 2 code example (citing CSharpFunctionalExtensions directly) is the correct and only source of truth for this part of the implementation — flagged in "No Analog Found" below.

---

### Test files — all `SentinelSuite.Framework.Domain.Shared.Tests/Results/*.cs`

**Analog:** `SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs`

**Imports pattern** (lines 1–4):
```csharp
using SentinelSuite.Framework.Domain.Shared.Guards;
using Xunit;

namespace SentinelSuite.Framework.Domain.Shared.Tests.Guards;
```
Result tests mirror this exactly, substituting `Guards` → `Results` in both the `using` and the namespace.

**Test naming convention** (`MethodName_WhenCondition_ExpectedBehavior`, e.g. `Null_WhenReferenceTypeInputIsNull_ThrowsArgumentNullExceptionWithCapturedParameterName`, lines 19, 59): apply verbatim to Result tests, e.g. `Value_WhenResultIsFailure_ThrowsInvalidOperationException`, `Bind_WhenResultIsFailure_ShortCircuitsWithoutInvokingContinuation`.

**Arrange/Act/Assert three-line-block shape with no comments** (lines 8–16, 48–56): every test is a bare three-statement block (arrange value, act via `Guard.Against.X(...)`, assert) with zero inline comments — copy this terse style directly for Result tests (`var result = Result.Success(); var mapped = result.Map(...); Assert.True(mapped.IsSuccess);`).

**Throws-with-property-assertion pattern** (lines 22–26, 38–46):
```csharp
var ex = Assert.Throws<ArgumentNullException>(() => Guard.Against.Null(input));
Assert.Equal(nameof(input), ex.ParamName);
```
Directly reusable shape for `ResultOfTValueAccessTests` (D-06): `var ex = Assert.Throws<InvalidOperationException>(() => failedResult.Value);` then assert on `ex.Message`.

**No-leak assertion pattern** (line 66): `Assert.DoesNotContain(input, ex.Message);` — apply to `ResultCriticalErrorTests`/`ResultErrorTests` if any test constructs a `CriticalError` from a sensitive-looking exception message, per RESEARCH.md's Security Domain note about `CriticalError.Exception` leakage risk (flag but this phase's own tests are internal, not a serialization boundary — still worth a defensive test).

---

## Shared Patterns

### Sealed class + static-factory-only construction
**Source:** `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` lines 29–39
**Apply to:** `Result.cs`, `ResultOfT.cs`
```csharp
public sealed class Guard : IGuardClause
{
    private Guard() { }
    public static IGuardClause Against { get; } = new Guard();
}
```

### File-per-concern static extension class
**Source:** `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs`, `GuardAgainstNullExtensions.cs`
**Apply to:** every `Result*Extensions.cs` combinator file
- One `public static class` per concern/combinator family, not one flat class.
- Each method is an extension method (`this IGuardClause guardClause` → for Result, `this Result`/`this Result<T>`/`this Task<Result<T>>`).

### XML-doc `<summary>` + `<remarks>` with rationale
**Source:** `SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs` lines 3–28; `GuardAgainstNullExtensions.cs` lines 6–21, 60–65
**Apply to:** all new public types/methods, especially `CriticalError`'s `Exception` property (must explain the deliberate divergence from both reference libraries per RESEARCH.md Pitfall 2) and `Result<T>.Value`'s fail-fast getter (must explain divergence from Ardalis.Result's unguarded property per Pitfall 3).

### Fixed, non-interpolated failure messages (Information-Disclosure discipline)
**Source:** `SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs` lines 74–79; `GuardAgainstNullExtensions.cs` line 80
**Apply to:** `Error`'s constructor guard failures and `CriticalError`'s default fallback message (RESEARCH.md Pitfall 5) — never interpolate raw exception `.Message`/`.ToString()` output directly into a business-facing `Error.Message` without a fixed fallback.

### Test file imports/namespace/naming convention
**Source:** `SentinelSuite.Framework.Domain.Shared.Tests/Guards/GuardAgainstNullTests.cs` lines 1–26
**Apply to:** all `Results/*Tests.cs` files — `using SentinelSuite.Framework.Domain.Shared.Results;`, `namespace SentinelSuite.Framework.Domain.Shared.Tests.Results;`, `MethodName_WhenCondition_ExpectedBehavior` test naming, bare 3-statement Arrange/Act/Assert bodies, no inline comments.

## No Analog Found

Files/patterns with no close match in the existing codebase (planner should rely on RESEARCH.md's own directly-cited-from-source patterns instead):

| File / Concern | Role | Data Flow | Reason |
|---|---|---|---|
| `ResultStatus.cs` | model (enum) | CRUD (vocabulary) | Phase 1 introduced no enums in `Domain.Shared` — first one in the kernel. Use RESEARCH.md's `ResultStatus` code example (cited directly from `ardalis/Result`) verbatim per D-08's fixed list. |
| Left/Right/Both async-overload split (`*.Task.Left.cs`/`*.Task.Right.cs`/`*.Task.cs`-style file organization for each combinator) | utility (extension methods) | streaming/event-driven (async chaining) | Phase 1's `Guards/` is 100% synchronous — no async precedent exists anywhere in this codebase yet. RESEARCH.md's Pattern 2 (cited directly from `CSharpFunctionalExtensions`) is the sole source of truth for this mechanic; treat it as authoritative since no in-repo analog exists to cross-check against. |
| `ResultAsyncCombinatorTests.cs` | test | transform (async) | No async test exists anywhere in `Guards.Tests/`. Follow standard xUnit v3 `async Task`-returning `[Fact]` conventions (not present in this repo yet but standard for the framework already in use) combined with the naming/AAA-shape conventions extracted above from `GuardAgainstNullTests.cs`. |
| `ResultCombineExtensions.cs` mixed `Result`/`Result<T>[]` overload resolution | utility (batch) | batch | No batch/aggregation precedent in `Guards/` (guards operate on one input at a time, except `NullOrEmpty<T>(IEnumerable<T>)` which is only a partial shape match). Use RESEARCH.md's `ResultCombineExtensions` code example (cited from `CSharpFunctionalExtensions/Combine.cs`) directly, per Pitfall 4's separate-overloads recommendation. |

## Metadata

**Analog search scope:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/` and `SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Guards/` (only existing production code in the repo relevant to this phase — Phase 1 is the sole completed precedent; no other Domain/Application/Infrastructure code exists yet).
**Files scanned:** 6 production Guard files + 5 Guard test files (all read in full; small files, single-pass reads, no re-reads).
**Pattern extraction date:** 2026-07-16
