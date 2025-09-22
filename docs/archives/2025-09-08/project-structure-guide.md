# Wangkanai Domain Library - Project Structure Guide

## Table of Contents

- [Overview](#overview)
- [Directory Structure](#directory-structure)
- [Module Architecture](#module-architecture)
- [Build System](#build-system)
- [Testing & Quality Assurance](#testing--quality-assurance)
- [Development Workflow](#development-workflow)
- [Configuration Files](#configuration-files)

## Overview

The Wangkanai Domain library is a comprehensive domain-driven design (DDD) foundation built on .NET 9.0. It provides three core
modules following Clean Architecture principles with centralized package management and comprehensive testing infrastructure.

### Key Architectural Principles

- **Domain-Driven Design**: Rich domain models with clear boundaries
- **Clean Architecture**: Separation of concerns across layers
- **Generic Programming**: Strongly-typed entities with flexible key types
- **Audit Trail**: Comprehensive change tracking across all entities
- **Performance-First**: Optimized patterns with compiled accessors

## Directory Structure

```
Wangkanai.Domain/
├── Assets/                          # Brand assets and logos
│   ├── wangkanai-logo.afdesign
│   ├── wangkanai-logo.ico
│   ├── wangkanai-logo.png
│   └── wangkanai-logo.svg
├── build.ps1                        # Build automation script
├── sign.ps1                         # Code signing script
├── md-styles.css                    # Documentation styling
├── repomix.config.json              # Repository packaging config
├── SonarQube.Analysis.xml           # Static analysis config
├── LICENSE                          # Apache 2.0 license
├── README.md                        # Project documentation
├── CLAUDE.md                        # AI assistance instructions
│
├── .solutions/                      # Build & configuration
│   ├── Directory.Build.props        # Global MSBuild properties
│   ├── Directory.Build.targets      # Global MSBuild targets
│   ├── Directory.Packages.props     # Centralized package versions
│   ├── Domain.slnx                  # Solution file (new format)
│   ├── Domain.sln.DotSettings       # IDE-specific settings
│   ├── .gitattributes              # Git file handling rules
│   ├── .gitignore                  # Git ignore patterns
│   └── .editorconfig               # Code style rules
│
├── src/                            # Source code modules
│   ├── Domain/                     # Core domain module
│   ├── Audit/                      # Audit trail module
│   └── EntityFramework/            # EF Core integration
│
├── tests/                          # Unit & integration tests
│   ├── Domain/                     # Domain module tests
│   ├── Audit/                      # Audit module tests
│   └── EntityFramework/            # EF module tests
│
├── benchmark/                      # Performance benchmarks
│   ├── Domain/                     # Domain performance tests
│   ├── Audit/                      # Audit performance tests
│   └── EntityFramework/            # EF performance tests
│
└── docs/                           # Documentation
    ├── README.md                   # Documentation index
    ├── project-structure-guide.md  # This document
    ├── api-reference.md            # API documentation
    ├── architectural_analysis_report.md
    ├── performance-analysis-report.md
    └── integration-guide.md
```

### Directory Responsibilities

| Directory     | Purpose                          | Key Files                               |
|---------------|----------------------------------|-----------------------------------------|
| `Assets/`     | Brand identity and visual assets | Logo files in multiple formats          |
| `src/`        | Production source code           | Core business logic and implementations |
| `tests/`      | Quality assurance                | Unit tests mirroring src structure      |
| `benchmark/`  | Performance validation           | BenchmarkDotNet performance tests       |
| `docs/`       | Documentation                    | Architecture guides and API references  |
| `.solutions/` | Build configuration              | MSBuild props, package management       |

## Module Architecture

The solution follows a modular architecture with three distinct modules, each with clear responsibilities and boundaries:

### Core Module Hierarchy

```
┌─────────────────────────────────────┐
│            Application              │  (Consumer Code)
├─────────────────────────────────────┤
│         EntityFramework             │  (Infrastructure)
├─────────────────────────────────────┤
│             Audit                   │  (Cross-cutting)
├─────────────────────────────────────┤
│            Domain                   │  (Core)
└─────────────────────────────────────┘
```

### 1. Domain Module (`src/Domain/`)

**Purpose**: Core domain-driven design patterns and entities
**Dependencies**: Microsoft.EntityFrameworkCore, Wangkanai.System

#### Structure

```
Domain/
├── Entity.cs                        # Abstract entity base class
├── ValueObject.cs                   # Immutable value object base
├── EntryState.cs                    # Entity state tracking
├── TrackingEntry.cs                 # Change tracking utilities
├── NullCollection.cs                # Null object pattern
│
├── Caching/                         # Caching abstractions
│   ├── CacheKey.cs
│   └── ICacheKey.cs
│
├── Events/                          # Domain event system
│   ├── DomainEvent.cs               # Base domain event
│   ├── EventSuppressor.cs           # Event suppression
│   ├── GenericChangedEntry.cs       # Change notifications
│   ├── GuidDomainEvent.cs           # GUID-based events
│   └── I*.cs                        # Event interfaces
│
├── Exceptions/                      # Domain-specific exceptions
│   ├── AggregateInvariantViolationException.cs
│   ├── DomainException.cs
│   └── DomainRuleViolationException.cs
│
├── Extensions/                      # Extension methods
│   ├── EntityTypeBuilderExtensions.cs  # EF configuration
│   └── ReflectionExtensions.cs      # Reflection utilities
│
├── Interfaces/                      # Core abstractions
│   ├── IAggregateRoot.cs            # DDD aggregate root
│   ├── IEntity.cs                   # Entity contract
│   ├── IRepository.cs               # Repository pattern
│   ├── IUnitOfWork.cs              # Unit of work pattern
│   ├── IValueObject.cs             # Value object contract
│   └── Key*Entity.cs               # Typed entity interfaces
│
├── Messages/                        # Domain messaging
│   ├── ICancellableDomainHandler.cs
│   ├── IDomainHandler.cs
│   └── IDomainMessage.cs
│
├── Primitives/                      # Core primitives
│   ├── Error.cs                     # Error handling
│   ├── ErrorCodes.cs               # Standardized error codes
│   └── Result.cs                   # Result pattern
│
└── Key*Entity.cs                   # Strongly-typed entities
```

#### Key Features

- **Generic Entity System**: Type-safe entities with `IEquatable<T>` and `IComparable<T>` constraints
- **Value Objects**: Immutable objects with structural equality
- **Domain Events**: Event-driven architecture with publisher/subscriber patterns
- **Result Pattern**: Robust error handling without exceptions
- **Caching Abstractions**: Performance-optimized caching interfaces

### 2. Audit Module (`src/Audit/`)

**Purpose**: Comprehensive audit trail and change tracking
**Dependencies**: Domain module, Microsoft.EntityFrameworkCore

#### Structure

```
Audit/
├── Audit.cs                         # Generic audit entity
├── AuditTrailType.cs                # Audit action enumeration
├── AuditableEntity.cs               # Base auditable entity
├── SoftDeleteAuditableEntity.cs     # Soft delete with audit
├── UserAuditableEntity.cs           # User-aware audit
├── UserSoftDeleteAuditableEntity.cs # Combined user/soft delete
│
├── Configurations/                  # EF Core configurations
│   └── AuditConfiguration.cs
│
├── Extensions/                      # Extension methods
│   ├── AuditContextExtensions.cs    # DbContext extensions
│   ├── EntityTypeBuilderExtensions.cs  # EF configuration
│   └── SoftDeleteExtensions.cs      # Soft delete utilities
│
├── Interfaces/                      # Audit contracts
│   ├── IAuditable.cs               # Base audit interface
│   ├── ICreatedEntity.cs           # Creation tracking
│   ├── IDataEntity.cs              # Data entity marker
│   ├── IDeletedEntity.cs           # Deletion tracking
│   ├── ISoftDeletable.cs           # Soft delete capability
│   ├── ISoftDeleteAuditable.cs     # Combined soft delete/audit
│   ├── IUpdatedEntity.cs           # Update tracking
│   ├── IUserAuditable.cs           # User-aware audit
│   └── IUserSoftDeleteAuditable.cs # User soft delete audit
│
├── Reflections/                     # Reflection utilities
│   └── PrimaryKeyResolvingMap.cs    # Key resolution
│
└── Stores/                          # Audit storage
    ├── AuditStore.cs                # Audit store implementation
    ├── IAuditStore.cs              # Store interface
    └── IQueryableAuditStore.cs     # Queryable store
```

#### Key Features

- **Generic Audit Trail**: `Audit<TKey, TUserType, TUserKey>` for flexible audit records
- **Automatic Tracking**: Created, Updated, Deleted timestamps and users
- **Soft Delete**: Logical deletion with full audit trail
- **Query Support**: LINQ-compatible audit store for reporting
- **Type Safety**: Strongly-typed audit relationships

### 3. EntityFramework Module (`src/EntityFramework/`)

**Purpose**: Entity Framework Core integration and utilities
**Dependencies**: Domain and Audit modules, Microsoft.EntityFrameworkCore

#### Structure

```
EntityFramework/
├── AuditDbContext.cs                # Audit-aware DbContext
├── IAuditDbContext.cs               # DbContext interface
├── DatabaseBuilderExtensions.cs     # Database configuration
│
└── Generators/                      # Value generators
    ├── DateTimeNowGenerator.cs      # DateTime value generation
    └── DateTimeOffsetNowGenerator.cs # DateTimeOffset generation
```

#### Key Features

- **Audit Integration**: Automatic audit trail generation in DbContext
- **Convention-Based Configuration**: Automated EF Core setup
- **Value Generators**: Timestamp generation for audit fields
- **Generic DbContext**: Flexible context implementation

## Build System

The project uses a sophisticated build system with centralized package management and automated workflows.

### MSBuild Configuration

#### Directory.Build.props

```xml
<Project>
  <!-- Global Properties -->
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- Package Metadata -->
  <PropertyGroup>
    <Company>Wangkanai</Company>
    <Authors>Sarin Na Wangkanai</Authors>
    <PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
  </PropertyGroup>

  <!-- Source Linking -->
  <PropertyGroup>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
  </PropertyGroup>
</Project>
```

#### Directory.Packages.props

Centralized package version management for:

- **Microsoft.Extensions**: DI, Logging, Configuration (v9.0.0)
- **Entity Framework Core**: ORM components (v9.0.0)
- **Testing**: xUnit v3, FluentAssertions, Moq
- **Benchmarking**: BenchmarkDotNet v0.15.2
- **Wangkanai**: System and Validation libraries

### Build Scripts

#### build.ps1

```powershell
# Build automation with error handling
dotnet --version
dotnet clean   .\src\ -c Release -tl
dotnet restore .\src\
dotnet build   .\src\ -c Release -tl

dotnet clean   .\tests\ -c Release -tl
dotnet restore .\tests\
dotnet build   .\tests\ -c Release -tl
```

#### sign.ps1

Code signing automation for release packages.

### Package Structure

Each source project follows consistent structure:

- **VersionPrefix**: Semantic versioning (5.0.0)
- **Product Name**: Descriptive package names
- **PackageTags**: Searchable tags for NuGet discovery
- **Enhanced Analysis**: Static analysis enforcement

## Testing & Quality Assurance

Comprehensive testing strategy with multiple quality gates:

### Test Organization

```
tests/
├── Domain/                          # Core domain tests
│   ├── EntityTests.cs              # Entity behavior
│   ├── ValueObjectTests.cs         # Value object tests
│   ├── Primitives/ResultTests.cs   # Result pattern tests
│   ├── Extensions/                 # Extension method tests
│   ├── Configurations/             # EF configuration tests
│   └── Models/                     # Test entities
│
├── Audit/                          # Audit functionality tests
│   ├── AuditTests.cs              # Core audit tests
│   ├── SoftDeleteAuditableEntityTests.cs
│   ├── Configurations/            # Audit config tests
│   ├── Extensions/                # Audit extension tests
│   ├── Stores/                    # Store implementation tests
│   └── Models/                    # Test audit entities
│
└── EntityFramework/                # EF integration tests
    ├── DatabaseMigrationExtensions.cs
    ├── FooDbContext.cs            # Test context
    └── xunit.runner.json          # Test configuration
```

### Test Configuration

Each test project includes:

- **xunit.runner.json**: Test execution configuration
- **Mock Objects**: In-memory contexts and test doubles
- **FluentAssertions**: Readable test assertions
- **Coverage Reports**: Code coverage with coverlet.msbuild

### Benchmark Organization

```
benchmark/
├── Domain/                         # Core performance tests
│   ├── DomainBenchmark.cs         # General domain benchmarks
│   ├── ValueObjectPerformanceBenchmark.cs
│   ├── QuickPerformanceValidation.cs
│   └── Program.cs                 # Benchmark runner
│
├── Audit/                         # Audit performance tests
│   ├── AuditPerformanceBenchmark.cs
│   └── Program.cs
│
└── EntityFramework/               # EF performance tests
    ├── EntityFrameworkBenchmark.cs
    └── Program.cs
```

### Quality Tools

- **SonarQube Analysis**: Static code analysis with quality gates
- **Enhanced Analyzer Rules**: Enforced code quality standards
- **Code Coverage**: OpenCover format reporting
- **BenchmarkDotNet**: Micro-benchmarking with statistical analysis

## Development Workflow

### Recommended Development Process

1. **Environment Setup**
   ```bash
   git clone https://github.com/wangkanai/domain.git
   cd domain
   dotnet --version  # Verify .NET 9.0+
   ```

2. **Build & Test**
   ```powershell
   .\build.ps1       # Build all projects
   dotnet test       # Run all tests
   ```

3. **Benchmarking**
   ```bash
   cd benchmark/Domain
   dotnet run -c Release
   ```

4. **Code Quality**
   ```bash
   dotnet format     # Code formatting
   # SonarQube analysis via CI/CD
   ```

### IDE Configuration

- **ReSharper**: `.DotSettings` files for consistent formatting
- **EditorConfig**: Cross-IDE code style rules
- **Enhanced Analysis**: Compile-time code quality enforcement

## Configuration Files

### Key Configuration Files

| File                       | Purpose                         | Scope                 |
|----------------------------|---------------------------------|-----------------------|
| `Directory.Build.props`    | Global MSBuild properties       | All projects          |
| `Directory.Packages.props` | Centralized package versions    | Solution-wide         |
| `Domain.slnx`              | Solution structure (new format) | IDE integration       |
| `SonarQube.Analysis.xml`   | Static analysis rules           | Quality gates         |
| `repomix.config.json`      | Repository packaging            | Documentation         |
| `.editorconfig`            | Code style rules                | Cross-IDE consistency |

### Package Management Strategy

- **Central Package Management**: All versions in `Directory.Packages.props`
- **Transitive Pinning**: Prevents version conflicts
- **Semantic Versioning**: Consistent version numbering
- **Source Linking**: Debug support with GitHub integration

### Security & Compliance

- **License**: Apache 2.0 open source license
- **Security Scanning**: Enabled in repomix configuration
- **Source Linking**: Full debug symbol support
- **Package Signing**: Automated via `sign.ps1`

---

## Navigation Links

- [← Back to Documentation Index](README.md)
- [API Reference →](api-reference.md)
- [Integration Guide →](integration-guide.md)
- [Performance Analysis →](performance-analysis-report.md)
- [Architectural Analysis →](architectural_analysis_report.md)

---

*Last updated: 2025-09-04*
*Document version: 1.0*