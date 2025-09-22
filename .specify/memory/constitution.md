<!-- Sync Impact Report
Version Change: 0.0.0 → 1.0.0 (MAJOR: Initial constitution establishment)

Added Sections:
- All core principles (I-VII) defined
- Architecture Standards section
- Development Workflow section
- Governance section with amendment procedures

Templates Requiring Updates:
✅ /templates/plan-template.md (to be checked)
✅ /templates/spec-template.md (to be checked)
✅ /templates/tasks-template.md (to be checked)
✅ /.claude/commands/*.md (to be checked)

Follow-up TODOs:
- RATIFICATION_DATE: To be confirmed by project owner (marked as TODO)
-->

# Wangkanai Foundation Constitution

## Core Principles

### I. Domain-Driven Design Excellence

Every architectural decision MUST align with DDD principles. Rich domain models with clear boundaries are non-negotiable.
Entities, value objects, and aggregates MUST be properly defined with strongly-typed identifiers. Domain logic belongs in the
domain layer, never in infrastructure or presentation layers. Each bounded context requires explicit definition and clear
integration patterns.

**Rationale**: DDD provides the foundational architecture that ensures long-term maintainability, clear business logic
representation, and scalability for enterprise applications.

### II. Test-First Development (NON-NEGOTIABLE)

TDD is mandatory for all feature development. The cycle MUST be: Write failing tests → Get approval → Implement → Refactor. Test
coverage MUST maintain 80% minimum as enforced by SonarQube quality gates. Unit tests mirror the source structure exactly. All
public APIs require comprehensive test coverage including edge cases.

**Rationale**: Test-first development ensures code correctness, provides living documentation, and enables confident refactoring
while maintaining system stability.

### III. Clean Architecture Layers

Strict separation of concerns across Domain, Application, and Infrastructure layers is required. Dependencies MUST flow inward (
Infrastructure → Application → Domain). The Domain layer MUST have zero external dependencies. Cross-layer communication happens
only through defined interfaces. Each layer maintains its own models without leakage.

**Rationale**: Clean architecture ensures the business logic remains independent of frameworks, databases, and external systems,
enabling flexibility and testability.

### IV. Comprehensive Audit Trail

All entity modifications MUST be automatically tracked with user, timestamp, and change details. Soft deletes are preferred over
hard deletes for data integrity. Audit records are immutable once written. Field-level change tracking captures both old and new
values in optimized JSON format. User identity tracking is mandatory for all mutations.

**Rationale**: Complete audit trails ensure regulatory compliance, enable debugging of data issues, and provide accountability for
all system changes.

### V. Performance-First Design

Every feature MUST include performance benchmarks in the benchmark/ directory. Caching strategies are required for frequently
accessed data. Database queries MUST be optimized with proper indexing. Bulk operations are preferred over individual operations
where applicable. Performance monitoring is built-in, not bolted-on.

**Rationale**: Performance directly impacts user experience and system scalability. Early performance consideration prevents
costly refactoring later.

### VI. Extension Method Preference

Code MUST strongly prefer extension methods over static utility methods for better readability and discoverability. Extension
methods create fluent, chainable APIs that are intuitive through IntelliSense. Static utilities are only acceptable when extension
methods are technically impossible.

**Rationale**: Extension methods improve code readability, enhance developer experience through better IDE support, and create
more maintainable codebases.

### VII. Documentation as Code

All Claude-generated documentation MUST be stored in the docs/ folder. Documentation is versioned alongside code. Public APIs
require XML documentation comments. Complex algorithms need explanatory comments. Documentation must be kept current with code
changes.

**Rationale**: Documentation as code ensures documentation stays synchronized with implementation, provides immediate context for
developers, and supports automated documentation generation.

## Architecture Standards

### Technology Requirements

- **.NET 9.0** minimum for all projects
- **Entity Framework Core 9.0** for database operations
- **Generic constraints** with IEquatable<T> and IComparable<T> for all entity keys
- **Nullable reference types** enabled throughout the solution
- **Implicit usings** for simplified namespace management

### Module Structure

- **Wangkanai.Foundation.Domain**: Core DDD patterns and building blocks
- **Wangkanai.Foundation.Application**: Application services and use cases
- **Wangkanai.Foundation.Infrastructure**: External system integrations
- Each module maintains clear boundaries and single responsibility
- Cross-module dependencies follow the dependency rule (inward only)

### Quality Standards

- **SonarQube** quality gate MUST pass for all code
- **Code coverage** minimum 80% enforced
- **No code smells** or security vulnerabilities allowed in production
- **Performance benchmarks** required for all critical paths

## Development Workflow

### Code Review Requirements

- All code MUST be peer-reviewed before merging
- Constitution compliance verification is mandatory
- Test coverage reports are required with each PR
- Performance impact must be assessed for significant changes
- Breaking changes require explicit documentation and migration guides

### Quality Gates

1. **Pre-commit**: Local tests must pass
2. **CI Pipeline**: Full test suite + SonarQube analysis
3. **Review**: Peer review with constitution compliance check
4. **Merge**: Only after all quality gates pass

### Versioning Policy

- Follow **Semantic Versioning** (MAJOR.MINOR.PATCH)
- **MAJOR**: Breaking API changes or significant architectural shifts
- **MINOR**: New features maintaining backward compatibility
- **PATCH**: Bug fixes and minor improvements
- All packages in the foundation maintain synchronized versions

## Governance

This constitution supersedes all other development practices and guidelines. It serves as the ultimate source of truth for
architectural decisions and development standards.

### Amendment Procedure

1. Proposed amendments MUST be documented with clear rationale
2. Impact analysis on existing code and practices is required
3. Team consensus through formal review process
4. Migration plan for existing code if breaking changes
5. Update all dependent templates and documentation
6. Version increment following semantic versioning rules

### Compliance and Enforcement

- All pull requests MUST verify constitutional compliance
- Complexity beyond constitution standards requires written justification
- Use CLAUDE.md for runtime development guidance specific to AI assistance
- Regular audits ensure ongoing compliance with all principles
- Non-compliance blocks merge to main branch

### Document Hierarchy

1. **Constitution** (this document) - Supreme governance
2. **CLAUDE.md** - AI assistant specific guidance
3. **Templates** - Implementation patterns
4. **Project Documentation** - Specific implementation details

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): Requires project owner confirmation | **Last Amended**: 2025-09-22