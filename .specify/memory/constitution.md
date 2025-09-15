# Foundation DDD Constitution

Minimal, enforceable rules that enable developers to build on the Foundation framework with correct Domain-Driven Design practices and zero unnecessary boilerplate.

## Core Principles

### 1. Ubiquitous Language First

Every module begins by defining the domain language (entities, value objects, invariants, flows) before implementation. Code, tests, documentation, and commit messages reflect the same vocabulary. No model is created without an explicit ubiquitous language agreement.

### 2. Explicit Boundaries & Aggregate Integrity

All domain changes flow through aggregate roots. Invariants are enforced inside aggregates only. Cross-aggregate access must use repositories or domain services. No leaking persistence concerns into the domain layer. Value objects are immutable and structurally comparable.

### 3. Test-Driven & Contract-Guarded

Write failing unit/spec tests for entities, value objects, and domain services before implementation. Add integration tests only for persistence behaviors and domain event publication. Red-Green-Refactor is mandatory; code merged without test coverage is rejected.

### 4. Templates Over Boilerplate

Scaffolding uses provided templates (entity, value object, repository, audit, domain event). Manual duplication of patterns is disallowed. New patterns must be codified into a template before adoption in more than one module.

### 5. Auditability & Traceable Evolution

All auditable entities implement the auditing interfaces. Domain events are the authoritative record of state transitions. No silent mutation: every state change is traceable either via domain events or audit trails.

## Platform & Architectural Constraints

Runtime: .NET 9 only; nullable enabled; file-scoped namespaces; 3-space indentation.
Keys: Entity keys implement `IEquatable<T>` and `IComparable<T>`; prefer strongly-typed IDs (wrappers) when ambiguity exists.
Layers: Domain (pure), Application (orchestration + commands/queries), Infrastructure (EF, external systems). No Infrastructure reference inside Domain.
Events: Domain events are synchronous in-process publication; external propagation handled by application/integration layer.
Persistence: EF Core mappings never introduce business rules; only shape persistence.
Guarding: Input validation performed at aggregate boundary using guard helpers; no defensive duplication downstream.
Value Objects: Immutable, validated on construction, equality via component enumeration.
Repositories: Return aggregates or value objects only—never raw data structures or anonymous types.
Auditing: Any entity with business-relevant lifecycle must opt into auditing or document why excluded.

## Development Workflow

1. Scaffold: Use template to create entity/value object/repository/event.
2. Language: Capture or update ubiquitous language notes (kept near module README or docs/architecture section).
3. Specify: Write unit tests for invariants + value object equality + domain service behavior.
4. Implement: Make tests pass with minimal code; enforce invariants internally.
5. Integrate: Add persistence mapping + repository + audit registration.
6. Wire: Register application behaviors (commands/queries) and domain event handlers.
7. Review: PR must show tests, invariant coverage, and no extraneous infrastructure leakage.
8. Document: Update docs if new concepts introduced (INDEX or architecture subpage).

## Quality Gates (Minimum)

100% test coverage for value object equality and entity invariants.
No public setters on entities except for ORM-required protected/private constructors.
All new aggregates emit at least one domain event on meaningful state transitions.
No TODO comments left unresolved in domain or application layers.
Static analysis warnings related to nullability or accessibility resolved before merge.

## Governance

This Constitution supersedes ad-hoc conventions. Non-compliant code must be refactored before feature acceptance unless a temporary exception is logged with an expiry date. Amendments require: (1) written proposal in `docs/architecture/` (2) justification + migration impact (3) approval via PR review by at least two maintainers. New recurring pattern → must become a template within two PRs or be rejected.

**Version**: 1.0.0 | **Ratified**: 2025-09-14 | **Last Amended**: 2025-09-14
