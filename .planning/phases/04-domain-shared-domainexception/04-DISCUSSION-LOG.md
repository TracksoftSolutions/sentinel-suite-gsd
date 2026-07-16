# Phase 4: Domain.Shared: DomainException - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-16
**Phase:** 4-Domain.Shared: DomainException
**Areas discussed:** Hierarchy & extensibility, Exception payload shape, Guard retrofit scope, SmartEnumNotFoundException, Test strategy, Guard error-code granularity, Equality/structural semantics, Documentation & threat model

---

## Hierarchy & extensibility

### Type shape (D-01)
| Option | Description | Selected |
|--------|-------------|----------|
| Open concrete base | Non-abstract, non-sealed; throwable AND derivable | ✓ |
| Abstract base + concrete general subtype | Abstract base, never thrown directly | |
| Single sealed type | One sealed type, Code discriminates | |

### Ship subtypes or base only (D-02)
| Option | Description | Selected |
|--------|-------------|----------|
| Base only *(Claude recommended)* | Ship DomainException alone; no reference API to copy | |
| Base + validation/rule-violation split | Two intermediates | |
| Base + fuller taxonomy | Broader named set mirroring ResultStatus | ✓ |

**User's choice:** Base + fuller taxonomy — overrode Claude's "base only" recommendation.
**Notes:** Rationale accepted as stronger than Claude's — mirroring ResultStatus means throw side and return side share one vocabulary; a failure keeps the same identity whether returned or thrown.

### Which ResultStatus cases (D-03)
| Option | Description | Selected |
|--------|-------------|----------|
| Domain-meaningful subset | Invalid, NotFound, Conflict + rule-violation | ✓ |
| Full 1:1 mirror | One subtype per failure status | |
| Mirror minus CriticalError | Everything except CriticalError | |

**Notes:** Dropped Forbidden/Unauthorized (application-layer authorization), Unavailable (infrastructure), CriticalError (circular — its job is wrapping an exception).

### Base type (D-04)
| Option | Description | Selected |
|--------|-------------|----------|
| System.Exception | Direct derivation, no misleading semantics | ✓ |
| System.InvalidOperationException | Collides with Phase 2 D-06 | |
| System.ApplicationException | Discouraged by Microsoft guidelines | |

### Subtype sealing (D-05)
| Option | Description | Selected |
|--------|-------------|----------|
| Open — all derivable | Modules derive from taxonomy leaves | ✓ |
| Sealed subtypes, open base | Modules derive only from root | |
| Mixed — open where justified | Case-by-case | |

### Naming (D-06)
| Option | Description | Selected |
|--------|-------------|----------|
| Domain-prefixed | DomainValidationException etc. — avoids BCL/ecosystem collisions | ✓ |
| Unprefixed | ValidationException etc. — collision risk under implicit usings | |
| Prefixed root, unprefixed leaves | Least predictable | |

### Validation vs rule-violation semantics (D-07)
| Option | Description | Selected |
|--------|-------------|----------|
| Shape vs. rule | "Could I decide this without loading anything else?" | ✓ |
| Severity / recoverability | Framed on caller response | |
| Origin — guard vs. entity | Rule about code location | |

### Module exception naming (D-08)
| Option | Description | Selected |
|--------|-------------|----------|
| Free names, documented base rule | Convention constrains only which base to derive from | ✓ |
| Enforce {Module}{Concept}Exception | Strict pattern, verbose | |
| Leave undocumented for now | Ad-hoc reinvention risk | |

### Marker interface (D-09)
| Option | Description | Selected |
|--------|-------------|----------|
| No — base class only | catch(DomainException) is the one mechanism | ✓ |
| Yes — marker + base | Two meanings, loses catch-by-type | |
| Marker only, no base | No catch(DomainException) anywhere | |

### Placement (D-10)
| Option | Description | Selected |
|--------|-------------|----------|
| Kernel taxonomy central, leaves local | Taxonomy in ...Exceptions; concept leaves stay with their concept | ✓ |
| All exceptions in ...Exceptions | Splits SmartEnum across two folders | |
| Each with its concept, no central folder | Abandons the {Concept} convention | |

---

## Exception payload shape

### Payload (D-11)
| Option | Description | Selected |
|--------|-------------|----------|
| Reuse Phase 2's Error type | One error vocabulary across throw and return | ✓ |
| Own Code string, no Error type | Two parallel representations that drift | |
| Message only | Retrofit cost later | |

**Notes:** Adds a Phase 2 practical dependency; ROADMAP declares Phase 1 only — flagged to planner.

### List vs single Error (D-12)
| Option | Description | Selected |
|--------|-------------|----------|
| List + .Error accessor, mirroring Result | Preserves Combine() round-trip | ✓ |
| Single Error | Breaks aggregation round-trip | |
| Single now, list later | Breaking change to a base class later | |

### Result↔exception conversion (D-13)
| Option | Description | Selected |
|--------|-------------|----------|
| ThrowIfFailure() only, as an extension | Ships the direction with a real use; no edit to Result | ✓ |
| Both directions | ToResult() overlaps CriticalError, no boundary yet | |
| Neither — defer conversion | Leaves symmetry unrealized | |

### Message composition (D-14)
| Option | Description | Selected |
|--------|-------------|----------|
| Composed from Errors | Single source of truth; nothing invisible to a plain log | ✓ |
| Explicit message + separate Errors | Two sources that drift | |
| First error's Message only | Aggregated errors vanish from default logs | |

### Ctor surface (D-15)
| Option | Description | Selected |
|--------|-------------|----------|
| At least one Error required | Compile-time invariant; (code, message) convenience ctor | ✓ |
| Allow message-only ctor | Meaningless default code proliferates | |
| Allow empty Errors | .Error becomes a null-ref trap | |

### Code convention (D-16)
| Option | Description | Selected |
|--------|-------------|----------|
| Document convention + reserve kernel prefixes | Guard.*, SmartEnum.*, Domain.* reserved | ✓ |
| Convention only, no reservation | Silent code collisions | |
| Leave to Phase 2 | Phase 2 context already locked | |

### Information disclosure (D-17)
| Option | Description | Selected |
|--------|-------------|----------|
| Documented contract, structural where possible | Kernel paths route through SafeParamName; caller strings a contract | ✓ |
| Structural sanitization in the type | No reliable way to detect "sensitive" | |
| No special handling | Drops Phase 1's NIST-relevant thread | |

### Subtype default codes (D-18)
| Option | Description | Selected |
|--------|-------------|----------|
| Subtype supplies a default, overridable | Domain.NotFound etc.; throw sites may specialize | ✓ |
| Always explicit — no defaults | Friction at every throw | |
| Fixed, not overridable | Can't distinguish missing badge from missing shift | |

### Legacy serialization ctor (D-19)
| Option | Description | Selected |
|--------|-------------|----------|
| No legacy serialization ctor | SYSLIB0051 obsolete; Error is JSON-serializable | ✓ |
| Include it for compatibility | Obsoletion warnings + RCE surface | |
| Defer to research | Obsoletion well-documented | |

---

## Guard retrofit scope

### Which paths migrate (D-20)
| Option | Description | Selected |
|--------|-------------|----------|
| Principled split | Content/format guards → DomainValidationException; argument-contract stays BCL | ✓ |
| Minimal — InvalidInput only | Line lands arbitrarily | |
| All 16 migrate | Loses ArgumentNullException semantics | |
| Opt-in exception factory | Decision scatters to 26 modules | |

### Parameter name (D-21)
| Option | Description | Selected |
|--------|-------------|----------|
| ParameterName property on DomainValidationException | Structured, on the subtype where it's meaningful | ✓ |
| Message only | Has to be parsed back out | |
| Put it in the Error record | Cross-phase edit to Phase 2's locked shape | |

### Communicating the split (D-22)
| Option | Description | Selected |
|--------|-------------|----------|
| Document the test inline | Where a guard author is already looking | ✓ |
| Document in phase docs only | Not where the decision gets made | |
| Leave it to precedent | Subtle split → divergence | |

### InvalidInput classification (D-23)
| Option | Description | Selected |
|--------|-------------|----------|
| Yes — migrate it, document the intent | One clear meaning: domain shape escape hatch | ✓ |
| No — keep it on ArgumentException | No general domain-shape guard left | |
| Both — add a domain-flavored sibling | Naming coin-flip at every call site | |

### Numeric guard BCL correction (D-24)
| Option | Description | Selected |
|--------|-------------|----------|
| Yes — fix them in this phase | Match BCL ThrowIfNegative/ThrowIfZero + adjacent OutOfRange | ✓ |
| No — out of scope, log it | Leaves a self-inconsistency | |
| Fix only where inconsistent with OutOfRange | ~Same as option 1 (Default is the only difference) | |

**Notes:** Deliberate scope addition beyond PRIM-04's literal wording. Planner trap flagged: ArgumentOutOfRangeException reverses ArgumentException's (message, paramName) order.

### Revisit: keep any BCL exception? (D-25)
| Option | Description | Selected |
|--------|-------------|----------|
| (a) Keep split, restate on defect-vs-domain-fact principle + document 7-category map | Anticipated → taxonomy; defects → BCL, allowed to fail | ✓ |
| (b) Kernel root over everything including defects | Nothing escapes the code vocabulary | |
| (c) Something else | — | |

**User's choice:** (a) — reopened the topic mid-session, then landed here.
**Notes:** User's own framing became the governing principle: *"exceptions we can anticipate are just that, not broken code which is what needs to be allowed to fail."* Retroactively validates Phase 2 D-06 (InvalidOperationException = defect, category 6). Full 7-category map to be documented as kernel exception policy.

---

## SmartEnumNotFoundException

### Classification (D-26)
| Option | Description | Selected |
|--------|-------------|----------|
| Anticipated not-found → derive from DomainNotFoundException | Category 4; keeps rich message, gains Code, catchable as DomainException | ✓ |
| It's a defect → keep it BCL-derived | Contradicts Phase 3's framing of it as a normal lookup outcome | |
| Split it — domain miss vs. misuse | Not mechanizable at throw time | |

### Sequencing (D-27)
| Option | Description | Selected |
|--------|-------------|----------|
| Reorder — Phase 4 before Phase 3 *(Claude recommended)* | Build taxonomy first, write it right once | |
| Keep order — Phase 3 ships BCL-derived, Phase 4 rebases | Explicit retrofit, exactly what Phase 3 D-05 flagged | ✓ |
| Note it now, let planner decide | Leaves a cross-phase dependency unstated | |

**User's choice:** Keep numeric order; Phase 4 rebases.
**Notes:** Phase 4's retrofit scope now covers guards AND the SmartEnum rebase (Phase 4 edits Phase 3's code). Hand-off to Phase 3 planning: ship the type from a plain base so the rebase is a one-line swap.

---

## Test strategy (D-28)
| Option | Description | Selected |
|--------|-------------|----------|
| Update in place + full new coverage | Edit the 7 affected Phase 1 tests; add DomainException + mapping + rebase coverage | ✓ |
| New tests only, leave Phase 1 tests | Old tests would fail or hide non-migration | |
| Characterization-first | Heavier than a fully-covered green-field kernel needs | |

---

## Guard error-code granularity (D-29)
| Option | Description | Selected |
|--------|-------------|----------|
| Per-guard specific codes | Guard.StringTooShort / .InvalidFormat / .InvalidInput | ✓ |
| One shared guard code | Throws away granularity the guard has | |
| Defer to planning | Code strings are expensive to change later | |

---

## Equality / structural semantics (D-30)
| Option | Description | Selected |
|--------|-------------|----------|
| Reference equality; Errors immutable & order-preserving | No override; defensively-copied read-only snapshot | ✓ |
| Reference equality, no ordering/immutability guarantee | Caller could mutate a thrown exception's errors | |
| Add value-equality | Solves a problem nothing has | |

---

## Documentation & threat model (D-31)
| Option | Description | Selected |
|--------|-------------|----------|
| Full XML docs + Phase 4 threat note | Match Phase 1-3 precedent; principle lives in code; carry T-1-02 forward | ✓ |
| Full XML docs, no separate threat note | Inconsistent security trail for a FedRAMP-track project | |
| Docs to planner's discretion | Invites a thinner result than the kernel's other primitives | |

---

## Claude's Discretion

- Exact subtype constructor overload set and file organization within `...Exceptions`.
- Exact composed-`Message` wording (multi-error case) and each XML-doc block.
- Exact `ThrowIfFailure()` overload shape for `Result` vs `Result<T>` and the mapping table's default arm.
- Precise default subtype code strings beyond the `Domain.{Case}` shape.

## Deferred Ideas

- **`ToResult()`** — reverse of `ThrowIfFailure()` (D-13). Deferred: no application/API boundary this milestone; overlaps Phase 2's `CriticalError`. Revisit when UseCases/Web lands.
