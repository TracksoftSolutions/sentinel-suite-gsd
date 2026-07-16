# Phase 3: Domain.Shared: SmartEnum<T> - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-16
**Phase:** 3-Domain.Shared: SmartEnum<T>
**Areas discussed:** Value type support, Instance discovery mechanism, Lookup failure API & exception type, Comparison & enumeration surface, Equality operators, Sealed-by-default & generic constraint, ToString() override

---

## Value type support

| Option | Description | Selected |
|--------|-------------|----------|
| Both int + generic TValue | SmartEnum<TEnum> (int-backed) AND SmartEnum<TEnum, TValue> (e.g. string-backed) | ✓ |
| Int-backed only | Just SmartEnum<TEnum> with an int Value | |
| You decide | Claude picks during planning | |

**User's choice:** Both int + generic TValue
**Notes:** Matches Ardalis.SmartEnum's actual shape; continues the "build broader now" pattern from Phases 1 & 2.

---

## Instance discovery mechanism

| Option | Description | Selected |
|--------|-------------|----------|
| Reflection-based auto-discovery | Reflects over public static readonly fields, caches result | ✓ |
| Explicit manual registration | Derived type overrides a method returning all instances | |
| You decide | Claude picks during planning | |

**User's choice:** Reflection-based auto-discovery

| Option | Description | Selected |
|--------|-------------|----------|
| Lazy, cached on first lookup | Reflection runs on first FromValue/FromName/List call, cached per-type | ✓ |
| Eager, in static constructor | Reflection runs at type-load in static cctor | |
| You decide | Claude picks during planning | |

**User's choice:** Lazy, cached on first lookup
**Notes:** Matches Ardalis.SmartEnum's actual behavior; avoids static-constructor ordering risk.

---

## Lookup failure API & exception type

| Option | Description | Selected |
|--------|-------------|----------|
| Both throwing + Try-variants | FromValue/FromName throw; TryFromValue/TryFromName return bool + out | ✓ |
| Throwing only | Just FromValue/FromName | |
| You decide | Claude picks during planning | |

**User's choice:** Both throwing + Try-variants

| Option | Description | Selected |
|--------|-------------|----------|
| BCL exception now, retrofit later | Matches Phase 1/2 precedent (D-04/D-06) | |
| Dedicated exception type now | Introduce SmartEnumNotFoundException this phase | ✓ |
| You decide | Claude picks during planning | |

**User's choice:** Dedicated exception type now
**Notes:** Deliberate deviation from Phase 1/2's "BCL exception until Phase 4" precedent — flagged explicitly in CONTEXT.md as a known, accepted departure.

| Option | Description | Selected |
|--------|-------------|----------|
| SmartEnumNotFoundException | Single exception type for both FromValue-miss and FromName-miss | ✓ |
| Two distinct exception types | Separate types per miss mode | |
| You decide | Claude picks during planning | |

**User's choice:** SmartEnumNotFoundException (single type)

---

## Comparison & enumeration surface

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, IComparable<SmartEnum<TEnum>> | Sorts by underlying Value | ✓ |
| Equality only | No ordering support | |
| You decide | Claude picks during planning | |

**User's choice:** Yes, IComparable<SmartEnum<TEnum>>

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, static List property | Every derived type gets a static IReadOnlyCollection<TEnum> List | ✓ |
| No, not this phase | Skip enumeration support | |
| You decide | Claude picks during planning | |

**User's choice:** Yes, static List property

| Option | Description | Selected |
|--------|-------------|----------|
| TValue constrained to IComparable<TValue> | Both int- and TValue-backed forms sortable | ✓ |
| Only int-backed form is comparable | Generic-value form stays equality-only | |
| You decide | Claude picks during planning | |

**User's choice:** TValue constrained to IComparable<TValue>
**Notes:** Follow-up question raised because Value type support (dual forms) and IComparable intersect — resolved for consistency across both forms.

---

## Equality operators (==, !=)

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, overload == and != | Matches Ardalis.SmartEnum exactly | ✓ |
| Equals/GetHashCode only | Skip operator overloads | |
| You decide | Claude picks during planning | |

**User's choice:** Yes, overload == and !=

---

## Sealed-by-default & generic constraint

| Option | Description | Selected |
|--------|-------------|----------|
| Both: self-ref constraint + sealed convention | TEnum : SmartEnum<TEnum> enforced; sealed convention documented ahead of Phase 9 | ✓ |
| Self-ref constraint only | Enforce constraint, defer sealed convention to Phase 9 | |
| You decide | Claude picks during planning | |

**User's choice:** Both: self-ref constraint + sealed convention
**Notes:** Pre-establishes the sealed-by-default discipline that Phase 9 (Entity) formally requires — gives Phase 9 a precedent to point back to.

---

## ToString() override

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, override ToString() → Name | Matches Ardalis.SmartEnum | ✓ |
| No, default ToString() | Leave default fully-qualified type name | |
| You decide | Claude picks during planning | |

**User's choice:** Yes, override ToString() → Name

---

## Claude's Discretion

- Exact namespace (expected `Domain.Shared.SmartEnum` or `...Enumerations`, following Phase 1/2's `Domain.Shared.{Concept}` convention)
- Internal reflection-caching implementation details (e.g. ConcurrentDictionary vs Lazy<T> per type)
- File/class organization within the SmartEnum sub-namespace
- Exact wording/shape of SmartEnumNotFoundException's message and properties

## Deferred Ideas

None — discussion stayed within phase scope.
