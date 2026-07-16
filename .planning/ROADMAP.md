# Roadmap: Sentinel Suite — Framework Kernel (Milestone 1)

## Overview

This milestone builds the custom Clean Architecture/DDD domain kernel that every one of Sentinel Suite's 26 modules and 222 planned features will sit on top of. There is no user-facing surface yet — the "user" of this milestone's output is the next milestone's developer (Claude, building concrete Master Records types). The roadmap follows the dependency chain researched for this kernel: zero-dependency primitives and the solution skeleton first, then the cross-cutting contracts `Entity` needs, then `Entity` itself as the single highest-leverage class in the milestone, then the aggregate-root/domain-event/value-object layer on top of it, then the association taxonomy and TPT abstract roots, then the capability-interface scaffold (which needs concrete roots to validate against), then the module system and Specification pattern, closing with a dedicated test-coverage gate that proves every kernel invariant is verified before the milestone is called done. Each Ardalis-branded pattern equivalent (`GuardClauses`, `Result`/`Result<T>`, `SmartEnum<T>`, `Specification<T>`) is broken out as its own dedicated phase, and every other requirement group is split to near one-requirement-per-phase granularity so each phase stays independently plannable and verifiable.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Domain.Shared: GuardClauses** - Hand-rolled guard-clause validation helpers exist with zero NuGet dependencies and full unit-test coverage
- [ ] **Phase 2: Domain.Shared: Result / Result<T>** - Hand-rolled operation-result pattern exists for expected failure paths
- [ ] **Phase 3: Domain.Shared: SmartEnum<T>** - Hand-rolled type-safe enumeration base exists
- [ ] **Phase 4: Domain.Shared: DomainException** - A dedicated domain-level exception type exists and becomes the throw type for guard-clause failures
- [ ] **Phase 5: Clean Architecture Solution Layout** - Five-project solution skeleton exists with compiler-enforced dependency direction
- [ ] **Phase 6: Cross-Cutting Contracts: Auditing** - Creation/modification auditing contracts exist as a compiled, tested shape
- [ ] **Phase 7: Cross-Cutting Contracts: Soft-Delete** - Soft-delete contract exists as a compiled, tested shape with cascade behavior documented
- [ ] **Phase 8: Cross-Cutting Contracts: Multi-Tenancy** - Multi-tenancy contract exists as a compiled, tested shape (contract only, no resolution machinery)
- [ ] **Phase 9: Entity Base Class (the Keystone)** - The abstract `Entity` base class exists with identity, equality, tenant scoping, audit fields, soft-delete, and concurrency control
- [ ] **Phase 10: Aggregate Roots & Domain Events** - Explicit aggregate-root distinction and persistence-agnostic domain-event collection convention exist on top of `Entity`
- [ ] **Phase 11: Value Object** - An immutable, structurally-equal Value Object base exists on top of `Entity`
- [ ] **Phase 12: EntityAssociation Base** - The named-kind, current-value-plus-history association base exists
- [ ] **Phase 13: TPT Abstract Roots** - The five non-instantiable TPT roots (Party, Item, Location, Activity, Document) exist
- [ ] **Phase 14: Capability Interface Scaffold** - The capability-interface pattern (composition-over-inheritance "CAN" behaviors) exists as compiled, implemented interfaces
- [ ] **Phase 15: Capability Registry & Resolver Stub** - A capability registry contract and resolver stub exist
- [ ] **Phase 16: Startup Drift-Validation Pass** - A startup validation pass fails fast on capability interface/registration drift
- [ ] **Phase 17: Extensible-Properties Pattern** - An extensible/extra-properties pattern exists, paralleling the platform's `extended_fields` concept
- [ ] **Phase 18: Module System** - A scoped-down module system with topological dependency resolution exists
- [ ] **Phase 19: Specification Pattern** - A predicate-only Specification pattern exists
- [ ] **Phase 20: Test Coverage & Quality Gate** - Every kernel invariant from Phases 1-19 has verifiable, passing xUnit v3 coverage, confirming the milestone's exit criterion

## Phase Details

### Phase 1: Domain.Shared: GuardClauses

**Goal**: Hand-rolled guard-clause validation helpers exist in `Domain.Shared` with zero third-party NuGet dependencies, giving every later phase a consistent, dependency-free way to validate arguments and invariants.
**Depends on**: Nothing (first phase)
**Requirements**: PRIM-01
**Success Criteria** (what must be TRUE):

  1. `GuardClauses` static helper class compiles in `Domain.Shared` with zero third-party NuGet package references.
  2. Passing xUnit tests cover null-argument guard scenarios (both pass and throw paths).
  3. Passing xUnit tests cover empty/range/enum-membership guard scenarios (both pass and throw paths).

**Plans**: 4/6 plans executed

Plans:
**Wave 1**

- [x] 01-01-PLAN.md — Test infrastructure scaffold (MTP-native xUnit v3 project, global.json, .slnx wiring)
- [x] 01-02-PLAN.md — Guard entry point & IGuardClause extensibility anchor

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 01-03-PLAN.md — GuardAgainstNull family (Null, NullOrEmpty, NullOrWhiteSpace)
- [x] 01-04-PLAN.md — GuardAgainstRange family (OutOfRange, EnumOutOfRange)
- [ ] 01-05-PLAN.md — GuardAgainstNumeric family (Negative, NegativeOrZero, Zero, Default)
- [ ] 01-06-PLAN.md — GuardAgainstInput + String round-out (InvalidInput, StringTooShort/TooLong, InvalidFormat)

**Cross-cutting constraints:**

- D-02: every guard method in this family captures its parameter name via CallerArgumentExpression, not an explicit nameof(x) argument; D-03: every guard method returns the validated input value unchanged on success, enabling inline assignment

### Phase 2: Domain.Shared: Result / Result<T>

**Goal**: A hand-rolled operation-result pattern exists in `Domain.Shared` for expected failure paths, giving the kernel a dependency-free alternative to throwing exceptions for anticipated failures.
**Depends on**: Nothing (parallel with Phase 1)
**Requirements**: PRIM-02
**Success Criteria** (what must be TRUE):

  1. `Result` and `Result<T>` types compile in `Domain.Shared` with zero third-party NuGet package references.
  2. Passing unit tests verify success/failure state transitions (a `Result` constructed as success or failure correctly reports `IsSuccess`/`IsFailure`).
  3. Passing unit tests verify error message/code propagation from a failure `Result`.
  4. A unit test confirms a failure `Result<T>` cannot expose a `Value` (throws or fails predictably on access).

**Plans**: TBD

### Phase 3: Domain.Shared: SmartEnum<T>

**Goal**: A hand-rolled type-safe enumeration base exists in `Domain.Shared`, giving the kernel an Ardalis.SmartEnum-equivalent without the NuGet dependency.
**Depends on**: Nothing (parallel with Phase 1)
**Requirements**: PRIM-03
**Success Criteria** (what must be TRUE):

  1. `SmartEnum<T>` base class compiles in `Domain.Shared` with zero third-party NuGet package references.
  2. Two `SmartEnum<T>`-derived fixture instances with the same underlying value are equal, verified by a unit test.
  3. `FromValue`/`FromName` lookups succeed for valid inputs, verified by a unit test.
  4. `FromValue`/`FromName` lookups fail predictably (a specific, catchable exception) for invalid inputs, verified by a unit test.

**Plans**: TBD

### Phase 4: Domain.Shared: DomainException

**Goal**: A dedicated domain-level exception type exists, distinct from framework/infrastructure exceptions, and becomes the throw type for guard-clause failures — closing the loop opened in Phase 1.
**Depends on**: Phase 1
**Requirements**: PRIM-04
**Success Criteria** (what must be TRUE):

  1. `DomainException` compiles in `Domain.Shared` as a distinct type (not a bare `Exception`/`InvalidOperationException`), confirmed by a unit test that catches it specifically by type.
  2. `GuardClauses` (from Phase 1) is updated so at least one guard-failure path throws `DomainException` instead of a generic framework exception, verified by a passing unit test.
  3. `DomainException` correctly carries and round-trips a message (and error code, if applicable) when constructed and caught, verified by a unit test.

**Plans**: TBD

### Phase 5: Clean Architecture Solution Layout

**Goal**: The five-project Clean Architecture solution skeleton exists with the dependency direction compiler-enforced from day one, so no future phase has to retrofit project structure.
**Depends on**: Nothing (parallel with Phases 1-4)
**Requirements**: LAYOUT-01
**Success Criteria** (what must be TRUE):

  1. Solution contains exactly five projects — `Domain.Shared`, `Domain`, `UseCases`, `Infrastructure`, `Web` — all targeting .NET 10 with nullable and implicit usings enabled.
  2. The `ProjectReference` chain matches the intended dependency direction (`Domain` → `Domain.Shared`; `UseCases` → `Domain`; `Infrastructure` → `UseCases`; `Web` → `UseCases`/`Infrastructure`), and `dotnet build` succeeds for the whole solution.
  3. A deliberately attempted reverse-direction reference (e.g., `Domain.Shared` → `Domain`) fails at build time, confirmed by a documented attempted-violation check.
  4. `UseCases`, `Infrastructure`, and `Web` exist as empty stub projects (no domain logic) that build successfully as placeholders.

**Plans**: TBD

### Phase 6: Cross-Cutting Contracts: Auditing

**Goal**: Creation/modification auditing contracts exist as a compiled, tested shape — one of three cross-cutting contracts `Entity` depends on, decided now because `Entity` cannot be written correctly without them.
**Depends on**: Phase 1, Phase 2, Phase 3, Phase 4
**Requirements**: AUDIT-01
**Success Criteria** (what must be TRUE):

  1. `ICreationAuditable`-equivalent interface compiles with creation-actor and creation-timestamp fields, with zero EF Core/persistence-layer references.
  2. `IModificationAuditable`-equivalent interface compiles with modification-actor and modification-timestamp fields.
  3. A test fixture implementing both interfaces round-trips creation/modification actor and timestamp fields correctly in unit tests.

**Plans**: TBD

### Phase 7: Cross-Cutting Contracts: Soft-Delete

**Goal**: A soft-delete contract exists as a compiled, tested shape, with cascade behavior documented up front so `Entity` and `EntityAssociation` relationships have a settled default.
**Depends on**: Phase 1, Phase 2, Phase 3, Phase 4
**Requirements**: AUDIT-02
**Success Criteria** (what must be TRUE):

  1. `ISoftDelete`-equivalent contract compiles with a soft-delete flag/timestamp shape, with zero EF Core/persistence-layer references.
  2. Cascade-per-relationship behavior is documented (FKs default to `Restrict`) alongside the contract.
  3. A unit test verifies a fixture entity's soft-delete flag toggles as expected without physically removing state.

**Plans**: TBD

### Phase 8: Cross-Cutting Contracts: Multi-Tenancy

**Goal**: A multi-tenancy contract exists as a compiled, tested shape — contract/shape only, with no resolution machinery this milestone.
**Depends on**: Phase 1, Phase 2, Phase 3, Phase 4
**Requirements**: AUDIT-03
**Success Criteria** (what must be TRUE):

  1. `IMultiTenant`-equivalent contract compiles with a `TenantId` property and an isolation-tier enum (`shared`/`dedicated_db`/`on_prem`).
  2. A unit test verifies a fixture entity carries a non-null `TenantId` and a valid isolation-tier value.
  3. No resolution or EF Core machinery is present in this phase's code (contract/shape only), verified by the absence of any `DbContext`/EF references in `Domain.Shared`/`Domain`.

**Plans**: TBD

### Phase 9: Entity Base Class (the Keystone)

**Goal**: The abstract `Entity` base class — the single highest-leverage type in this milestone — exists with correct identity, equality, tenant scoping, audit fields, soft-delete convention, and optimistic concurrency control, with sealed-by-default discipline applied from this class onward as the precedent for every subsequent phase.
**Depends on**: Phase 6, Phase 7, Phase 8
**Requirements**: ENT-01
**Success Criteria** (what must be TRUE):

  1. Two `Entity`-derived fixture instances of the same concrete type and same `Id` are equal; instances of different concrete types sharing the same `Id` are not equal — verified by passing unit tests.
  2. Every `Entity`-derived fixture type exposes tenant-scoping, audit (creation/modification), soft-delete, and optimistic-concurrency-stamp fields inherited from the base class, verified by unit tests instantiating a concrete fixture.
  3. `Entity` and its immediate fixture subclasses are `sealed` by default (or documented as intentionally open with a named, current second-implementation justification), enforced as a review-gate convention captured in this phase's tests/docs.
  4. A concurrency-stamp mismatch scenario, simulated in a unit test, is detectable via the `Entity` base's concurrency field without requiring any persistence/EF Core code.

**Plans**: TBD

### Phase 10: Aggregate Roots & Domain Events

**Goal**: The kernel distinguishes aggregate roots from plain entities and provides a persistence-agnostic domain-event collection convention gated on that distinction — resolving where domain events collect before any downstream type depends on the answer.
**Depends on**: Phase 9
**Requirements**: ENT-02, ENT-03
**Success Criteria** (what must be TRUE):

  1. An explicit `AggregateRoot`(-equivalent) type/marker compiles, distinct from plain `Entity`, verified by a unit test showing only aggregate-root-derived fixtures expose the domain-event collection API.
  2. `AddDomainEvent`/`ClearDomainEvents` (or equivalent) on an aggregate-root fixture correctly accumulates and clears `IDomainEvent`-marked events, verified by unit tests.
  3. The domain-event shape (`IDomainEvent`-marked) is confirmed immutable, serializable, and free of infrastructure references (outbox-compatible), verified by unit test or code-level assertion.

**Plans**: TBD

### Phase 11: Value Object

**Goal**: An immutable, structurally-equal Value Object base exists on top of `Entity`, giving the kernel a second building block alongside entities.
**Depends on**: Phase 9
**Requirements**: ENT-04
**Success Criteria** (what must be TRUE):

  1. `ValueObject` abstract base class compiles with structural-equality support.
  2. Two `ValueObject`-derived fixture instances with identical component values are equal and share the same hash code; two instances differing in any one component are not equal — verified by unit tests.
  3. `ValueObject`-derived fixture types expose no public mutators (construction-time immutability enforced), verified by a unit test or code-level assertion.

**Plans**: TBD

### Phase 12: EntityAssociation Base

**Goal**: The kernel's association taxonomy (named-kind, current-value-plus-history) exists as a base layered on `Entity` — the most bespoke, no-external-prior-art requirement in the milestone.
**Depends on**: Phase 9, Phase 10, Phase 11
**Requirements**: ASSOC-01
**Success Criteria** (what must be TRUE):

  1. `EntityAssociation` abstract base class compiles with a named-kind discriminator.
  2. Adding a new "current" association value retains the prior value in history rather than overwriting it, verified by a unit test (current-value-plus-history shape).
  3. Cascade behavior for `EntityAssociation`-to-root relationships is documented (FKs default to `Restrict`), consistent with the soft-delete contract established in Phase 7.

**Plans**: TBD

### Phase 13: TPT Abstract Roots

**Goal**: The five abstract TPT intermediate roots (Party, Item, Location, Activity, Document) exist as non-instantiable bases layered on `Entity`, ready for the next milestone's concrete Master Records types to derive from.
**Depends on**: Phase 9, Phase 10, Phase 11
**Requirements**: ASSOC-02
**Success Criteria** (what must be TRUE):

  1. `Party`, `Item`, `Location`, `Activity`, and `Document` all compile as `abstract` classes derived from `Entity`, with no direct instantiation path (compiler-enforced).
  2. At least one concrete test-fixture subclass per TPT root (five total) compiles and inherits `Entity`'s identity/audit/tenant/soft-delete behavior.
  3. Each of the five concrete test-fixture subclasses passes the equality/identity unit tests established for `Entity` in Phase 9.

**Plans**: TBD

### Phase 14: Capability Interface Scaffold

**Goal**: The capability-interface pattern (composition over inheritance for cross-cutting "CAN" behaviors) exists as a set of compiled, implemented interfaces.
**Depends on**: Phase 12, Phase 13
**Requirements**: CAP-01
**Success Criteria** (what must be TRUE):

  1. `IMergeable`, `IDisplayLabeled`, `IOfflineCapturable`, and `ICustodyTracked` interfaces compile in `Domain`.
  2. At least one TPT-root test fixture implements each of the four capability interfaces.
  3. Unit tests exercise each interface's contract against the implementing fixture(s), confirming correct behavior.

**Plans**: TBD

### Phase 15: Capability Registry & Resolver Stub

**Goal**: A capability registry contract and resolver stub exist, giving the kernel a queryable record of which types implement which capabilities (concrete backing store deferred to the Infrastructure milestone).
**Depends on**: Phase 14
**Requirements**: CAP-02
**Success Criteria** (what must be TRUE):

  1. `ICapabilityResolver` interface stub compiles in `Domain`.
  2. A registry contract (fixture-backed for this milestone) compiles alongside it.
  3. A unit test verifies a type's registered capabilities can be queried back correctly via the resolver/registry.

**Plans**: TBD

### Phase 16: Startup Drift-Validation Pass

**Goal**: A startup validation pass exists that fails fast on interface/registration drift, so the capability system's compile-time interfaces and runtime registry can never silently disagree.
**Depends on**: Phase 14, Phase 15
**Requirements**: CAP-03
**Success Criteria** (what must be TRUE):

  1. A startup validation pass exists that walks registered types and their implemented capability interfaces, comparing them against the registry from Phase 15.
  2. A deliberately-introduced mismatch (a fixture type implementing a capability interface but missing from the registry) is detected and fails fast, verified by a unit test.
  3. A deliberately-introduced mismatch in the other direction (a fixture registered for a capability it doesn't implement) is also detected and fails fast, verified by a unit test.
  4. The "registration wins at runtime" discipline is documented alongside the validation pass.

**Plans**: TBD

### Phase 17: Extensible-Properties Pattern

**Goal**: An extensible/extra-properties pattern exists, paralleling the platform's existing `extended_fields` concept, so downstream types can carry schema-less data without kernel changes.
**Depends on**: Phase 12, Phase 13
**Requirements**: CAP-04
**Success Criteria** (what must be TRUE):

  1. `IHasExtraProperties`-equivalent contract compiles in `Domain`.
  2. A fixture entity implementing the contract can store and retrieve arbitrary key/value extra properties, verified by a unit test.
  3. Storing a new, previously-undeclared key requires no schema/type changes to the fixture entity (schema-less extensibility), verified by test or code-level assertion.

**Plans**: TBD

### Phase 18: Module System

**Goal**: A deliberately scoped-down module system (manifest, dependency declaration, topological resolution, diamond-dependency handling) exists, ready for the next milestone's concrete modules to build on — intentionally excluded from DI bootstrapping this milestone.
**Depends on**: Phase 12, Phase 13
**Requirements**: MOD-01
**Success Criteria** (what must be TRUE):

  1. `IFrameworkModule`(-equivalent) marker interface and a `[DependsOn]`-equivalent declaration mechanism compile in `Domain`.
  2. A unit test registers three or more fixture modules and asserts the resolved initialization order respects every declared dependency (topological resolution).
  3. A diamond-dependency fixture scenario (a module depended on by two others, which are both depended on by a common root) resolves without double-initializing the shared module, verified by a passing unit test.
  4. No DI bootstrapper or reflection-based auto-registration exists in the module-system code, consistent with this milestone's deliberately scoped-down module system.

**Plans**: TBD

### Phase 19: Specification Pattern

**Goal**: A predicate-only Specification pattern exists, ready for the next milestone's concrete queries to build on — intentionally excluded from EF query composition this milestone.
**Depends on**: Phase 12, Phase 13
**Requirements**: QUERY-01
**Success Criteria** (what must be TRUE):

  1. `Specification<T>`/`ISpecification<T>` compiles with an `IsSatisfiedBy(T)` method only (no EF query-composition surface).
  2. A concrete specification fixture correctly matches and rejects TPT-root test fixtures, verified by unit tests.
  3. No EF Core or persistence-layer references exist anywhere in the Specification pattern code, confirmed by inspecting the `Domain` project's references.

**Plans**: TBD

### Phase 20: Test Coverage & Quality Gate

**Goal**: Every kernel invariant introduced across Phases 1-19 has verifiable, passing xUnit v3 test coverage, run via Microsoft.Testing.Platform, confirming the milestone's exit criterion — the next milestone can build real domain types on top of this kernel without fighting it.
**Depends on**: Phase 1 through Phase 19 (all preceding phases)
**Requirements**: TEST-01
**Success Criteria** (what must be TRUE):

  1. `dotnet test` (Microsoft.Testing.Platform host) runs the full xUnit v3 test suite across all kernel invariants — primitives, contracts, `Entity`, aggregate roots/domain events, Value Object, `EntityAssociation`/TPT roots, capability system, module system, Specification — with zero failures.
  2. Every sealed base class in the taxonomy (`Entity`, `AggregateRoot`, `ValueObject`, `EntityAssociation`, and each TPT root) has at least one dedicated equality/invariant test, confirmed by a coverage-to-class traceability pass.
  3. The startup drift-detection test (Phase 16) and the diamond-dependency module-resolution test (Phase 18) are both present and passing in the consolidated suite, not deferred or skipped.
  4. No test project references a disallowed dependency (FluentAssertions 8+, MediatR, any `Ardalis.*` package, any `Volo.Abp.*` package), verified by inspecting the test project(s)' package references.

**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12 → 13 → 14 → 15 → 16 → 17 → 18 → 19 → 20
(Phases 1-3 and Phase 5 have no interdependencies and could run in parallel; Phases 6-8 similarly share the same prerequisite and could run in parallel. Default execution follows numeric order for a single implementer.)

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Domain.Shared: GuardClauses | 4/6 | In Progress|  |
| 2. Domain.Shared: Result / Result<T> | 0/TBD | Not started | - |
| 3. Domain.Shared: SmartEnum<T> | 0/TBD | Not started | - |
| 4. Domain.Shared: DomainException | 0/TBD | Not started | - |
| 5. Clean Architecture Solution Layout | 0/TBD | Not started | - |
| 6. Cross-Cutting Contracts: Auditing | 0/TBD | Not started | - |
| 7. Cross-Cutting Contracts: Soft-Delete | 0/TBD | Not started | - |
| 8. Cross-Cutting Contracts: Multi-Tenancy | 0/TBD | Not started | - |
| 9. Entity Base Class (the Keystone) | 0/TBD | Not started | - |
| 10. Aggregate Roots & Domain Events | 0/TBD | Not started | - |
| 11. Value Object | 0/TBD | Not started | - |
| 12. EntityAssociation Base | 0/TBD | Not started | - |
| 13. TPT Abstract Roots | 0/TBD | Not started | - |
| 14. Capability Interface Scaffold | 0/TBD | Not started | - |
| 15. Capability Registry & Resolver Stub | 0/TBD | Not started | - |
| 16. Startup Drift-Validation Pass | 0/TBD | Not started | - |
| 17. Extensible-Properties Pattern | 0/TBD | Not started | - |
| 18. Module System | 0/TBD | Not started | - |
| 19. Specification Pattern | 0/TBD | Not started | - |
| 20. Test Coverage & Quality Gate | 0/TBD | Not started | - |
