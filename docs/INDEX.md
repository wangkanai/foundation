# Wangkanai Foundation - Documentation Index

> **Last Updated**: 2024-06-13
> **Version**: 1.0.0
> **Architecture**: Clean Architecture Monorepo with Domain-Driven Design

## 📚 Quick Navigation

### Core Documentation

- [Project Overview](#project-overview)
- [Architecture Documentation](#architecture-documentation)
- [Module Documentation](#module-documentation)
- [Standards & Guidelines](#standards--guidelines)
- [Archives](#archives)

---

## 🎯 Project Overview

The Wangkanai Foundation is a comprehensive .NET 9.0 framework implementing Domain-Driven Design (DDD) patterns with Clean
Architecture principles. It provides foundational components for building enterprise-grade applications with robust audit
capabilities and Entity Framework Core integration.

### Key Features

- **Clean Architecture Implementation**: Separation of Domain, Application, and Infrastructure layers
- **Domain-Driven Design**: Rich domain models with clear boundaries
- **Comprehensive Audit Trail**: Automatic change tracking across all entities
- **Entity Framework Integration**: Seamless ORM support with convention-based configuration
- **Multi-Database Support**: SQL Server, PostgreSQL, MySQL providers

### Solution Structure

```
Foundation/
├── 📦 Foundation/          # Core DDD and Clean Architecture implementation
│   ├── src/
│   │   ├── Domain/         # Pure domain models and business logic
│   │   ├── Application/    # Application services and use cases
│   │   └── Infrastructure/ # Infrastructure concerns and implementations
│   ├── tests/              # Unit and integration tests
│   └── benchmarks/         # Performance benchmarks
│
├── 📦 Audit/               # Comprehensive audit trail capabilities
│   ├── src/
│   │   ├── Domain/         # Audit domain models
│   │   ├── Application/    # Audit configuration and extensions
│   │   └── Infrastructure/ # Audit infrastructure
│   ├── tests/              # Audit module tests
│   └── benchmarks/         # Audit performance benchmarks
│
└── 📦 EntityFramework/     # EF Core utilities and database providers
    ├── src/
    │   ├── EntityFramework/ # Core EF utilities
    │   ├── SqlServer/       # SQL Server provider
    │   ├── Postgres/        # PostgreSQL provider
    │   └── MySql/           # MySQL provider
    ├── tests/               # Provider-specific tests
    └── benchmarks/          # EF performance benchmarks
```

---

## 🏗️ Architecture Documentation

### Current Architecture

- **[Architecture Overview](archives/2025-09-09/ARCHITECTURE_OVERVIEW.md)** - Comprehensive system architecture
- **[Clean Architecture Evolution](archives/2025-09-09/CLEAN_ARCHITECTURE_EVOLUTION.md)** - Clean Architecture implementation
  details
- **[Final Architecture Report](archives/2025-09-09/FINAL_ARCHITECTURE_REPORT.md)** - Latest architectural state

### Design & Patterns

- **[Design Decisions](archives/2025-09-08/design-decisions.md)** - Key architectural decisions
- **[Architecture Diagrams](archives/2025-09-08/architecture-diagrams.md)** - Visual architecture representations
- **[Usage Patterns](archives/2025-09-08/usage-patterns.md)** - Common implementation patterns

### Migration & Evolution

- **[Monorepo Migration Guide](archives/2025-09-09/MONOREPO_MIGRATION_GUIDE.md)** - Monorepo structure migration
- **[Foundation Restructure Plan](archives/2025-09-09/FOUNDATION_RESTRUCTURE_PLAN.md)** - Clean Architecture restructuring
- **[Migration Guide](archives/2025-09-08/migration-guide.md)** - General migration strategies

---

## 📦 Module Documentation

### Foundation Module

**Namespace**: `Wangkanai.Foundation`

#### Domain Layer (`Wangkanai.Foundation.Domain`)

- **Entity Types**
   - `Entity<T>` - Base entity with strongly-typed ID
   - `KeyIntEntity`, `KeyLongEntity`, `KeyGuidEntity`, `KeyStringEntity`, `KeyByteEntity` - Specialized entities
   - `ValueObject` - Immutable value objects with structural equality

- **Domain Patterns**
   - `IAggregateRoot` - Aggregate root marker interface
   - `IDomainEvent` - Event-driven architecture support
   - `Result<T>` - Error handling with Result pattern
   - Domain exceptions and rule violations

#### Application Layer (`Wangkanai.Foundation.Application`)

- **Configurations**
   - `EntityConfiguration` - Base entity configuration
   - `KeyConfigurationBuilder` - Primary key configuration
   - `VersionConfigurationBuilder` - Row version configuration

- **Extensions**
   - `EntityTypeBuilderExtensions` - Fluent API extensions

#### Infrastructure Layer (`Wangkanai.Foundation.Infrastructure`)

- **Event Publishing**
   - `DomainPublisher` - Domain event publisher implementation
   - `IDomainPublisher` - Publisher interface
   - `IEventListener` - Event listener interface

### Audit Module

**Namespace**: `Wangkanai.Audit`

- **[Audit Module README](Audit/README.md)** - Complete audit module documentation
- **Core Components**
   - `Trail<TKey, TUserType, TUserKey>` - Generic audit trail entity
   - `AuditableEntity` - Base entity with audit tracking
   - `UserAuditableEntity` - User-specific audit tracking
   - `TrailType` - Audit action enumeration

### EntityFramework Module

**Namespace**: `Wangkanai.EntityFramework`

- **[EntityFramework README](EntityFramework/README.md)** - EF Core utilities documentation
- **Core Features**
   - Database provider extensions
   - Value generators for timestamps
   - Convention-based configurations
   - Multi-database support (SQL Server, PostgreSQL, MySQL)

---

## 📐 Standards & Guidelines

### Development Standards

- **[Code Style Conventions](archives/2025-09-08/code_style_conventions.md)** - Coding standards and conventions
- **[Project Structure Guide](archives/2025-09-08/project-structure-guide.md)** - Project organization guidelines
- **[Integration Guide](archives/2025-09-08/integration-guide.md)** - Integration best practices

### Quality & Performance

- **[Quality Assessment Report](archives/2025-09-08/quality_assessment_report.md)** - Code quality analysis
- **[Performance Analysis Report](archives/2025-09-08/performance-analysis-report.md)** - Performance metrics
- **[Performance Optimization Guide](archives/2025-09-08/performance-optimization-guide.md)** - Optimization strategies
- **[Security Assessment Report](archives/2025-09-08/security_assessment_report.md)** - Security analysis

### Testing & Validation

- **[Test Plan](archives/2025-09-09/test-plan.md)** - Comprehensive testing strategy
- **[Validation Testing Strategy](archives/2025-09-09/VALIDATION_TESTING_STRATEGY.md)** - Validation approaches
- **[Clean Architecture Validation](archives/2025-09-09/CLEAN_ARCHITECTURE_VALIDATION.md)** - Architecture validation

---

## 📂 Archives

### 2025-09-09 (Latest)

Recent Clean Architecture implementation and monorepo restructuring:

- [Foundation Restructure Summary](archives/2025-09-09/FOUNDATION_RESTRUCTURE_SUMMARY.md)
- [Restructure Completion Report](archives/2025-09-09/RESTRUCTURE_COMPLETION_REPORT.md)
- [Monorepo Validation Strategy](archives/2025-09-09/MONOREPO_VALIDATION_STRATEGY.md)
- [Phase Execution Scripts](archives/2025-09-09/PHASE_EXECUTION_SCRIPTS.md)
- [Rollback Contingency Plan](archives/2025-09-09/ROLLBACK_CONTINGENCY_PLAN.md)

### 2025-09-08

Initial documentation and analysis:

- [Knowledge Base Index](archives/2025-09-08/knowledge-base-index.md)
- [Architectural Analysis Report](archives/2025-09-08/architectural_analysis_report.md)
- [Tech Stack](archives/2025-09-08/tech_stack.md)
- [API Reference](archives/2025-09-08/api-reference.md)
- [FAQ](archives/2025-09-08/faq.md)
- [Glossary](archives/2025-09-08/glossary.md)
- [Troubleshooting Guide](archives/2025-09-08/troubleshooting-guide.md)

---

## 🔗 Quick Links

### Essential Files

- **[CLAUDE.md](../CLAUDE.md)** - Claude Code guidance and conventions
- **[README.md](../README.md)** - Project README
- **[Foundation.slnx](../Foundation.slnx)** - Solution file

### Module READMEs

- **[Foundation README](../Foundation/README.md)** - Foundation module details
- **[Audit README](../Audit/README.md)** - Audit module details
- **[EntityFramework README](../EntityFramework/README.md)** - EF module details

### Build & Configuration

- **[Directory.Build.props](../Directory.Build.props)** - Global build properties
- **[Directory.Packages.props](../Directory.Packages.props)** - Central package management
- **[.editorconfig](../.editorconfig)** - Code style configuration

---

## 📊 Module Cross-Reference Matrix

| Module                        | Depends On                           | Used By            | Key Interfaces                              |
|-------------------------------|--------------------------------------|--------------------|---------------------------------------------|
| **Foundation.Domain**         | None                                 | All modules        | `IEntity`, `IAggregateRoot`, `IDomainEvent` |
| **Foundation.Application**    | Domain                               | Audit, EF          | Configuration builders                      |
| **Foundation.Infrastructure** | Domain, Application                  | Host applications  | `IDomainPublisher`                          |
| **Audit.Domain**              | Foundation.Domain                    | Audit.Application  | `IAuditableEntity`, `ITrailStore`           |
| **Audit.Application**         | Audit.Domain, Foundation.Application | EF providers       | Audit configurations                        |
| **EntityFramework**           | Foundation.Application               | Database providers | Database extensions                         |

---

## 🚀 Getting Started

1. **New Developers**: Start with [Project Overview](archives/2025-09-08/project_overview.md)
2. **Architecture Deep Dive**: Review [Architecture Overview](archives/2025-09-09/ARCHITECTURE_OVERVIEW.md)
3. **Implementation**: See [Usage Patterns](archives/2025-09-08/usage-patterns.md) and module READMEs
4. **Contributing**: Follow [Code Style Conventions](archives/2025-09-08/code_style_conventions.md)

---

## 📝 Documentation Maintenance

This index is maintained as part of the continuous documentation effort. Each major architectural change or module update should
be reflected here with appropriate cross-references to detailed documentation.

**Last Review**: 2025-09-09
**Next Scheduled Review**: Upon next major architectural change

---

*Generated and maintained by Claude Code Documentation System*