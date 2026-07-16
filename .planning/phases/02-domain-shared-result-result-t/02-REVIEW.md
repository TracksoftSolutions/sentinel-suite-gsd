---
phase: 02-domain-shared-result-result-t
reviewed: 2026-07-16T00:00:00Z
depth: standard
files_reviewed: 19
files_reviewed_list:
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultBindTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCombineTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultConstructionTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultCriticalErrorTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultEnsureTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultErrorTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMapTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultMatchTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOfTValueAccessTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultOnSuccessOnFailureTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared.Tests/Results/ResultStatusFactoryTests.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Error.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs
  - SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultStatus.cs
findings:
  critical: 3
  warning: 3
  info: 1
  total: 7
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-07-16T00:00:00Z
**Depth:** standard
**Files Reviewed:** 19
**Status:** issues_found

## Summary

Reviewed the `Result`/`Result<T>` kernel (statuses, factories, `Error`, and the `Bind`/`Map`/`Ensure`/`Match`/`OnSuccess`/`OnFailure`/`Combine` combinators) plus its full test suite. The combinator sync/async overload matrices are internally consistent and the documented short-circuit/simplification behaviors match their tests. However, three of the type's own documented invariants are not actually enforced at runtime:

1. `CriticalError`'s "never pass a potentially-empty string into `Error`'s constructor" guarantee (explicitly cited as mitigating pitfall T-2-02 in the XML docs) uses `string.IsNullOrEmpty` instead of `string.IsNullOrWhiteSpace`, so a whitespace-only exception message crashes `CriticalError` itself with a masking `ArgumentException` — reproduced below.
2. `Error`'s constructor guarantees a non-null, non-empty `Message` (D-03: "guaranteeing anything catching a failure ... always has a displayable string"), but none of `Result`/`Result<T>`'s failure factories (`Failure`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`) or `Ensure`'s `error` parameter validate that the supplied `Error` argument(s) are non-null before storing them in `Errors`, so a null `Error` argument produces a "failed" `Result` whose `.Error!.Message` throws an unguarded `NullReferenceException` — reproduced below.
3. `Result<T>.Success`/the implicit `T -> Result<T>` conversion never validate that a reference-type `value` is non-null, so a null value silently produces `IsSuccess == true` with `Value == null` — the exact "silently present a default/uninitialized value as if legitimately produced" failure mode that D-06's own doc comments say this type was built to prevent (they only guard the *failure* read path, not the *success* write path). Reproduced below.

All three were confirmed against a standalone repro compiled with the .NET 10 SDK (see fix snippets). Remaining findings are consistency/robustness gaps (missing null-guards on delegate/task parameters across combinators, `Combine`'s element-level null check, and duplicated `Combine`/`Combine<T>` bodies).

## Critical Issues

### CR-01: `CriticalError` crashes instead of falling back when the exception's `Message` is whitespace-only

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs:151-153`
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs:169-171`

**Issue:** Both `Result.CriticalError` and `Result<T>.CriticalError` derive the fallback `Error` message as:

```csharp
var resolvedError = error ?? new Results.Error(
    "CriticalError",
    string.IsNullOrEmpty(exception.Message) ? "An unexpected error occurred." : exception.Message);
```

`string.IsNullOrEmpty` only rejects `null`/`""`. If the caught exception's `Message` is whitespace-only (e.g. `"   "` — a real possibility for hand-rolled or third-party exception types that fail to trim), the ternary passes `"   "` straight into `Error`'s constructor, which calls `Guard.Against.NullOrWhiteSpace(message)` and throws `ArgumentException("Required input was empty or whitespace.")`. This is precisely the "confusing secondary exception masking the real `CriticalError` call site" scenario the method's own XML docs cite as pitfall T-2-02 — the mitigation is implemented with the wrong null-check, so the pitfall it claims to avoid still occurs. Confirmed via a standalone repro: a custom `Exception` overriding `Message => "   "` passed through the equivalent of this exact ternary throws `ArgumentException: Required input was empty or whitespace. (Parameter 'message')` instead of returning a `Result`.

**Fix:**
```csharp
var resolvedError = error ?? new Results.Error(
    "CriticalError",
    string.IsNullOrWhiteSpace(exception.Message) ? "An unexpected error occurred." : exception.Message);
```
Apply identically in both `Result.cs` and `ResultOfT.cs`.

### CR-02: Failure factories (and `Ensure`) accept a null `Error`, silently breaking the "every failure has a displayable message" guarantee

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs:105-123` (`Failure`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`)
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs:123-141` (same set)
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs:48-56, 74-83, 103-111, 129-138` (`error` parameter)

**Issue:** `Error.cs`'s own doc comment states: "`Message` is required and non-empty (D-03), guaranteeing anything catching a failure (logs, UI, a future API response) always has a displayable string." That guarantee is only enforced inside `Error`'s constructor — it is never re-checked when a `Result`/`Result<T>` factory receives an `Error` (or array of them). `Failure(params Results.Error[] errors)` (and every sibling factory) stores whatever it is given verbatim: `new(ResultStatus.Error, errors)`. A call such as `Result.Failure(null!)` (or any code path that ends up passing a null `Error` reference — e.g. a lookup dictionary miss upstream) compiles and produces a `Result` with `Errors.Count == 1` where `Errors[0]` is `null`. Any downstream consumer following the class's own documented convenience-accessor pattern, `result.Error!.Message`, throws an unguarded `NullReferenceException` instead of the clean, informative `ArgumentException`/`ArgumentNullException` this codebase otherwise favors everywhere else (see `Guard.Against.*`). The same gap exists in `ResultEnsureExtensions.Ensure(...)`, whose `error` parameter is passed straight to `Result.Invalid(error)`/`Result<T>.Invalid(error)` without a null check. Confirmed via a standalone repro mirroring this exact shape: `Failure(null!)` produces a one-element `Errors` array containing `null`, and reading `.Error!.Message` throws `NullReferenceException`.

Given this kernel is the DDD foundation for all 26 planned modules (per `PROJECT.md`), an unguarded null `Error` silently breaking every "catch a failure and log/display it" call site is a correctness risk that should be caught at construction, not at the first unlucky read site months later.

**Fix:** Guard each factory's `errors` parameter (and `Ensure`'s `error` parameter) before storing, e.g.:
```csharp
public static Result Failure(params Results.Error[] errors)
{
    Guard.Against.NullOrEmpty(errors); // rejects null array
    if (errors.Any(e => e is null))
    {
        throw new ArgumentException("Required input must not contain a null Error.", nameof(errors));
    }

    return new Result(ResultStatus.Error, errors);
}
```
(or a shared private helper reused by every status factory in both `Result.cs` and `ResultOfT.cs`, plus the `error` parameter in `ResultEnsureExtensions.cs`).

### CR-03: `Result<T>.Success`/implicit conversion accept a null value for reference-type `T`, defeating the fail-fast guarantee `Value` is documented to provide

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs:113-114, 181`

**Issue:** `Result<T>`'s XML docs (D-06) draw an explicit contrast with Ardalis.Result: "a failed `Result<T>` can never silently present an uninitialized value as if it were a legitimately-produced result." That guarantee is only checked on the *read* side (`Value`'s getter throws on `IsFailure`). It is not checked on the *write* side: `Success(T value)` and the implicit `operator Result<T>(T value)` never validate `value`. For any reference-type `T`, code such as:
```csharp
string? maybeNull = LookupSomething(); // returns null
Result<string> r = maybeNull!;         // implicit conversion — compiles, only a nullable-reference *warning*
```
produces `r.IsSuccess == true` and `r.Value == null` — i.e. exactly the "silently present a default/uninitialized value as legitimately produced" failure mode the type's own docs say it prevents, just moved from the failure path to the success path. Confirmed via a standalone repro: assigning a null `string` through an equivalent `Success`/implicit-operator pair yields `IsSuccess=True, Value is null=True` for both a direct assignment and a method returning the converted value. Because `Value`'s getter uses the null-forgiving `_value!`, no compiler warning survives at the consumption site either — callers that trust `IsSuccess` before touching `Value` (exactly the pattern this type is meant to encourage) get an unexpected null with no signal.

**Fix:** Guard `value` for reference types inside `Success` (works safely for any unconstrained `T` via the `is null` pattern, value types simply never match it):
```csharp
public static Result<T> Success(T value)
{
    if (value is null)
    {
        throw new ArgumentNullException(nameof(value));
    }

    return new(ResultStatus.Ok, value, NoErrors);
}
```
The implicit operator delegates to `Success`, so it inherits the guard automatically.

## Warnings

### WR-01: `Combine`/`Combine<T>` validate the array itself but not its elements

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs:186-200, 228-242`

**Issue:** `Guard.Against.Null(results)` only rejects a null `results` array reference. A caller passing a null element within the array (e.g. `Result.Combine(a, b, null!)`) reaches `results.Where(result => result.IsFailure)`, which throws an unguarded `NullReferenceException` on the null element rather than a clear `ArgumentException`.

**Fix:** After the existing null-array guard, reject null elements explicitly:
```csharp
if (results.Any(r => r is null))
{
    throw new ArgumentException("Required input must not contain a null Result.", nameof(results));
}
```
Apply to both `Combine(Result[])` and `Combine<T>(Result<T>[])`.

### WR-02: `Bind`/`Map`/`Ensure`/`Match` never guard their delegate parameters, unlike `OnSuccess`/`OnFailure`

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs` (all `func` parameters)
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs` (all `mapper`/`valueFactory` parameters)
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs` (all `predicate` parameters)
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs` (all `onSuccess`/`onFailure` parameters)

**Issue:** `ResultOnSuccessOnFailureExtensions.cs`'s class docs state: "Every overload validates its action/func delegate via `Guard.Against`'s `Null` guard before inspecting `Result`/`Result{T}` state (T-2-08)." That guard is applied consistently in that file, but none of the sibling combinator files (`Bind`, `Map`, `Ensure`, `Match`) apply the same guard to their delegates. Calling e.g. `result.Bind((Func<int, Result<int>>)null!)` on a successful `result` throws a bare `NullReferenceException` from inside the extension method body instead of a clear `ArgumentNullException` naming the parameter — an inconsistent developer experience for what should be the same class of caller error across every combinator in this namespace.

**Fix:** Add `Guard.Against.Null(func)` / `Guard.Against.Null(mapper)` / `Guard.Against.Null(predicate)` / `Guard.Against.Null(onSuccess); Guard.Against.Null(onFailure);` at the top of each sync entry-point overload (the async overloads that delegate to the sync ones inherit the guard for free), mirroring `ResultOnSuccessOnFailureExtensions.cs`.

### WR-03: Async overloads never guard against a null `Task<Result>`/`Task<Result<T>>` receiver

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs:62, 87, 111, 138`
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs:67, 92, 113, 139`
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs:62, 89, 117, 145`
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs:59, 80, 99, 120`
**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs:79, 108, 137, 166, 196, 225, 254, 283`

**Issue:** Every `Left-async`/`Both-async` overload (e.g. `this Task<Result<TIn>> resultTask`) awaits its `resultTask` argument with no prior null check. Since these are extension methods, `((Task<Result>)null!).Bind(...)` compiles and throws a raw `NullReferenceException` at the `await` rather than a clear `ArgumentNullException`. This is a minor, low-probability caller error (a null `Task` is unusual), but it is inconsistent with this codebase's otherwise pervasive guard-clause discipline, and would produce a confusing stack trace for whoever hits it first.

**Fix:** Either accept this as a known, low-priority gap, or add `Guard.Against.Null(resultTask)` as the first line of each Left-/Both-async overload across all five files for consistency.

## Info

### IN-01: `Combine` and `Combine<T>` are near-duplicate implementations

**File:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs:186-200` and `228-242`

**Issue:** `Combine(Result[] results)` and `Combine<T>(Result<T>[] results)` are structurally identical (guard, filter failed, early-return success, flatten errors into `Failure(...)`), differing only in element type. The class's own remarks already explain why `Combine<T>` can't be expressed as a generic-over-`Result`-shape method, but the two method *bodies* could still share a private helper (e.g. `private static Result CombineCore(IEnumerable<Result> results, IEnumerable<IReadOnlyList<Results.Error>> errorLists)` or simpler, a shared local function taking `Func<IEnumerable<IReadOnlyList<Results.Error>>>` produced by each caller) to avoid maintaining two copies of the same aggregation logic.

**Fix:** Extract the shared "is-any-failed / flatten-errors-or-succeed" logic into a small private helper reused by both overloads.

---

_Reviewed: 2026-07-16T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
