---
phase: 01-domain-shared-guardclauses
fixed_at: 2026-07-16T06:30:00Z
review_path: .planning/phases/01-domain-shared-guardclauses/01-REVIEW.md
iteration: 1
findings_in_scope: 7
fixed: 7
skipped: 0
status: all_fixed
---

# Phase 01: Code Review Fix Report

**Fixed at:** 2026-07-16T06:30:00Z
**Source review:** .planning/phases/01-domain-shared-guardclauses/01-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 7 (Critical: 3, Warning: 4 — fix_scope: critical_warning, Info findings excluded)
- Fixed: 7
- Skipped: 0

## Fixed Issues

### CR-01: Every guard's "never leak the rejected value" mitigation is defeated when the call site passes a literal or inline expression

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/Guard.cs`, `GuardAgainstNullExtensions.cs`, `GuardAgainstStringExtensions.cs`, `GuardAgainstNumericExtensions.cs`, `GuardAgainstRangeExtensions.cs`, `GuardAgainstInputExtensions.cs`
**Commit:** 05805b2
**Applied fix:** Added `Guard.SafeParamName(string?)` — an internal helper that returns the captured `[CallerArgumentExpression]` text only if it is a syntactically simple C# identifier (letters/digits/underscore, not digit-led), otherwise `null`. Routed every guard's `parameterName` through this sanitizer before using it in an exception's message text or `ParamName`/`paramName` constructor argument, across all five guard-extension files. Verified the existing 43 tests (which all use named locals) are unaffected, since a valid identifier passes through unchanged.

### CR-02: `EnumOutOfRange` incorrectly rejects valid `[Flags]` enum combinations

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstRangeExtensions.cs`
**Commit:** 65c1299
**Applied fix:** Detects `[Flags]` via `typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false)` and, when present, validates by OR-ing all defined member values into a mask and checking the input has no bits outside that mask (`IsValidFlagsCombination<T>`), instead of always using `Enum.IsDefined`, which only recognizes exactly-named members.

### CR-03: `EnumOutOfRange`'s thrown exception leaks the rejected value, inconsistent with every sibling guard's documented mitigation

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstRangeExtensions.cs`
**Commit:** 4bcb14c
**Applied fix:** Replaced the leaking 3-argument `InvalidEnumArgumentException(parameterName, Convert.ToInt32(input), typeof(T))` constructor (which always embeds the numeric value in `Message`) with a value-free custom message (`"The value of argument '{safeParamName}' is invalid for Enum type '{typeof(T).Name}'."`), keeping the exception type for compatibility with the existing `Assert.Throws<InvalidEnumArgumentException>` test while dropping the leaked value. Applied on top of CR-02 in the same method since both findings shared the exact same lines.

### WR-01: `InvalidInput` does not guard its own `predicate` parameter

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstInputExtensions.cs`
**Commit:** a9c951f
**Applied fix:** Added `Guard.Against.Null(predicate, nameof(predicate));` before invoking `predicate(input)`, so a null predicate now throws a clear `ArgumentNullException` naming `predicate` instead of an unhelpful `NullReferenceException`.

### WR-02: `NullOrEmpty<T>(IEnumerable<T>)` partially consumes single-pass sequences before returning them to the caller

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstNullExtensions.cs`
**Commit:** 4e8daa5
**Applied fix:** Replaced the `.Any()` emptiness check with `input as ICollection<T> ?? input.ToList()` followed by a `.Count == 0` check, and returns the materialized collection instead of the original `input` reference — so a forward-only/single-use sequence is only ever enumerated once, and the returned value is always safe to re-enumerate. Verified the existing `Assert.Same(input, result)` test for `List<int>` input still passes since `List<T>` implements `ICollection<T>` and the cast returns the same instance.

### WR-03: `OutOfRange`'s range-inversion exception has no `ParamName`

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstRangeExtensions.cs`
**Commit:** 65fa848
**Applied fix:** Added `nameof(rangeFrom)` as the `paramName` argument to the `ArgumentException` thrown when `rangeFrom > rangeTo`, matching every other exception in the guard family.

### WR-04: `InvalidFormat` does not guard its own `regexPattern` parameter

**Files modified:** `SentinelSuite/SentinelSuite.Framework.Domain.Shared/Guards/GuardAgainstStringExtensions.cs`
**Commit:** 4e790d6
**Applied fix:** Added `Guard.Against.NullOrWhiteSpace(regexPattern, nameof(regexPattern));` before calling `Regex.IsMatch`, so a null/blank pattern now surfaces a predictable, correctly-named validation failure instead of an unguarded exception referencing the wrong parameter.

## Verification

For every fix: `dotnet build` on the `SentinelSuite.Framework.Domain.Shared` project (0 warnings, 0 errors) followed by `dotnet test` on `SentinelSuite.Framework.Domain.Shared.Tests` (43/43 passed), before committing. A final full-solution `dotnet build` + `dotnet test` pass after all 7 commits also succeeded cleanly (0 warnings/errors, 43/43 tests passed).

## Skipped Issues

None — all in-scope findings were fixed.

---

_Fixed: 2026-07-16T06:30:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
