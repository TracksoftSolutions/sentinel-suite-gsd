---
phase: 02-domain-shared-result-result-t
fixed_at: 2026-07-16T21:45:57Z
review_path: .planning/phases/02-domain-shared-result-result-t/02-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 6
skipped: 0
status: all_fixed
---

# Phase 02: Code Review Fix Report

**Fixed at:** 2026-07-16T21:45:57Z
**Source review:** .planning/phases/02-domain-shared-result-result-t/02-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (Critical: 3, Warning: 3 — `fix_scope: critical_warning`, so IN-01 was excluded)
- Fixed: 6
- Skipped: 0

All fixes were verified by a full `dotnet build` (0 warnings, 0 errors) and a full `dotnet run` test-suite pass (198/198 tests passed) after each individual fix, in addition to the standard Tier 1 re-read verification.

## Fixed Issues

### CR-01: `CriticalError` crashes instead of falling back when the exception's `Message` is whitespace-only

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs`
**Commit:** `87954d6`
**Applied fix:** Changed `string.IsNullOrEmpty(exception.Message)` to `string.IsNullOrWhiteSpace(exception.Message)` in both `Result.CriticalError` and `Result<T>.CriticalError`'s fallback-message ternary, exactly as suggested in the review. A whitespace-only exception message now correctly falls back to the fixed `"An unexpected error occurred."` literal instead of being passed into `Error`'s constructor and triggering a masking `ArgumentException`.

### CR-02: Failure factories (and `Ensure`) accept a null `Error`, silently breaking the "every failure has a displayable message" guarantee

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs`
**Commit:** `d7eca99`
**Applied fix:** Adapted the review's suggested per-factory inline guard into a single shared private `GuardErrors(Results.Error[] errors)` helper on both `Result` and `Result<T>` (rather than duplicating the same `Guard.Against.NullOrEmpty` + null-element check seven times per class), and routed every status factory (`Failure`, `Invalid`, `NotFound`, `Conflict`, `Forbidden`, `Unauthorized`, `Unavailable`) through it. Confirmed via the existing test suite that no factory call site in this codebase passes zero `Error` arguments, so tightening the array guard to `NullOrEmpty` (rejecting both null and zero-length) does not break any existing behavior. Also added `Guard.Against.Null(error)` to all four sync/Right-async entry-point overloads of `ResultEnsureExtensions.Ensure` (Left-async and Both-async inherit the guard by delegation).

### CR-03: `Result<T>.Success`/implicit conversion accept a null value for reference-type `T`, defeating the fail-fast guarantee `Value` is documented to provide

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOfT.cs`
**Commit:** `196c188`
**Applied fix:** Converted `Success(T value)` from an expression-bodied member to a block body that throws `ArgumentNullException(nameof(value))` when `value is null`, exactly as the review's fix snippet suggested. The implicit `T -> Result<T>` operator delegates to `Success` and inherits the guard automatically, with no separate change needed there.

### WR-01: `Combine`/`Combine<T>` validate the array itself but not its elements

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/Result.cs`
**Commit:** `e3d4bc8`
**Applied fix:** Added the review's suggested `results.Any(r => r is null)` check (throwing `ArgumentException`) immediately after the existing `Guard.Against.Null(results)` call in both `Combine(Result[])` and `Combine<T>(Result<T>[])`.

### WR-02: `Bind`/`Map`/`Ensure`/`Match` never guard their delegate parameters, unlike `OnSuccess`/`OnFailure`

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs`
**Commit:** `54839b5`
**Applied fix:** Added `Guard.Against.Null(func)` / `Guard.Against.Null(mapper)` / `Guard.Against.Null(valueFactory)` / `Guard.Against.Null(predicate)` / `Guard.Against.Null(onSuccess); Guard.Against.Null(onFailure);` as the first statement(s) of each sync and Right-async entry-point overload across all four combinator files (converting a few expression-bodied members to block bodies to accommodate the guard statement), mirroring `ResultOnSuccessOnFailureExtensions.cs`'s existing pattern. Left-async and Both-async overloads inherit the guard for free by delegating to the sync/Right-async entry points.

### WR-03: Async overloads never guard against a null `Task<Result>`/`Task<Result<T>>` receiver

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultBindExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMapExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultEnsureExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultMatchExtensions.cs`, `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Results/ResultOnSuccessOnFailureExtensions.cs`
**Commit:** `54f9e77`
**Applied fix:** Added `Guard.Against.Null(resultTask)` as the first statement of every Left-async/Both-async overload across all five files (24 call sites total), as the review suggested. One adaptation beyond the literal suggestion: `Guard.Against.Null<T>` is generic and returns `T`; since `T` is inferred as `Task<Result<TIn>>`/`Task<Result>` at these call sites, an unused return value of static type `Task<...>` inside an `async` method body triggers the compiler's CS4014 ("this call is not awaited") warning even though the call is fully synchronous. Discarded the return value explicitly (`_ = Guard.Against.Null(resultTask);`) at all 24 sites to keep the build warning-free while preserving the guard's throw-on-null behavior.

## Skipped Issues

None — all 6 in-scope findings were fixed.

Note: IN-01 (`Combine`/`Combine<T>` near-duplicate implementations) was intentionally not addressed — it is an Info-level finding and `fix_scope` for this run was `critical_warning`.

---

_Fixed: 2026-07-16T21:45:57Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
