# Wangkanai Domain - Architecture Diagrams

## Table of Contents

- [Overview](#overview)
- [Solution Structure](#solution-structure)
- [Module Dependencies](#module-dependencies)
- [Entity Inheritance Hierarchy](#entity-inheritance-hierarchy)
- [Audit Trail Flow](#audit-trail-flow)
- [Build & Test Pipeline](#build--test-pipeline)

## Overview

This document provides visual representations of the Wangkanai Domain library architecture, helping developers understand the
relationships between modules, components, and workflows.

## Solution Structure

```
📁 Wangkanai.Domain (Root)
├── 🎨 Assets/                    # Brand & Visual Assets
├── ⚙️  .solutions/               # Build Configuration
├── 📦 src/                       # Production Code
│   ├── 🏗️  Domain/              # Core DDD Module
│   ├── 📋 Audit/                 # Audit Trail Module
│   └── 🗄️  EntityFramework/     # EF Integration Module
├── 🧪 tests/                     # Quality Assurance
│   ├── 🏗️  Domain/              # Domain Tests
│   ├── 📋 Audit/                 # Audit Tests
│   └── 🗄️  EntityFramework/     # EF Tests
├── ⚡ benchmark/                 # Performance Testing
│   ├── 🏗️  Domain/              # Domain Benchmarks
│   ├── 📋 Audit/                 # Audit Benchmarks
│   └── 🗄️  EntityFramework/     # EF Benchmarks
└── 📚 docs/                      # Documentation
```

### Directory Legend

- 🎨 Assets & Branding
- ⚙️ Configuration & Build
- 🏗️ Core Domain Logic
- 📋 Audit & Tracking
- 🗄️ Data Access
- 🧪 Testing & Quality
- ⚡ Performance
- 📚 Documentation

## Module Dependencies

```
┌─────────────────────────────────────────┐
│             Consumer Applications        │
│           (Your Domain Logic)           │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│          EntityFramework Module         │
│        🗄️ Database Integration          │
│  • AuditDbContext                      │
│  • DatabaseBuilderExtensions           │
│  • Value Generators                    │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│             Audit Module                │
│         📋 Change Tracking              │
│  • Audit<TKey, TUser, TUserKey>        │
│  • AuditableEntity                     │
│  • SoftDelete Support                  │
│  • Audit Store                        │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│             Domain Module               │
│          🏗️ Core Patterns              │
│  • Entity<T>                           │
│  • ValueObject                         │
│  • Domain Events                       │
│  • Result<T>                           │
│  • Repository/UnitOfWork               │
└─────────────────────────────────────────┘
```

### Dependency Flow

- **Upward Dependencies**: Each module depends only on modules below it
- **Clean Architecture**: Domain is dependency-free, higher modules depend on lower ones
- **Modular Design**: Each module can be used independently if needed

## Entity Inheritance Hierarchy

### Domain Entities

```
                    Entity<T>
                  (Abstract Base)
                       │
         ┌─────────────┼─────────────┐
         │             │             │
   KeyGuidEntity  KeyIntEntity  KeyStringEntity
    (Guid IDs)    (Int IDs)    (String IDs)
         │             │             │
    IKeyGuidEntity IKeyIntEntity IKeyStringEntity
```

### Audit Entities

```
                 Entity<T>
                     │
              ┌──────┼──────┐
              │      │      │
    AuditableEntity  │  UserAuditableEntity
       (Basic)       │     (User-Aware)
              │      │      │
       ┌──────┴──────┼──────┴──────┐
       │             │             │
SoftDeleteAuditableEntity    UserSoftDeleteAuditableEntity
   (Soft Delete)              (User + Soft Delete)
```

### Audit Interfaces

```
             IAuditable
                │
    ┌───────────┼───────────┐
    │           │           │
ICreatedEntity IUpdatedEntity IDeletedEntity
    │           │           │
    └───────────┼───────────┘
                │
       ISoftDeleteAuditable
                │
    ┌───────────┼───────────┐
    │           │           │
IUserAuditable  │  ISoftDeletable
    │           │           │
    └───────────┼───────────┘
                │
  IUserSoftDeleteAuditable
```

## Audit Trail Flow

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Entity        │    │   DbContext     │    │   Audit Trail   │
│   Operations    │    │   SaveChanges   │    │   Generation    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ • Create        │    │ • Detect        │    │ • Audit<T>      │
│ • Update   ────►│────│   Changes  ────►│────│   Records       │
│ • Delete        │    │ • Generate      │    │ • TrailType     │
│ • Query         │    │   Audit         │    │ • Timestamps    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   AuditStore    │
                       │ • Query History │
                       │ • Filter by     │
                       │   Entity/User   │
                       │ • Time Ranges   │
                       └─────────────────┘
```

### Audit Trail Types

- **Create**: Entity creation with initial values
- **Update**: Field-level change tracking
- **Delete**: Logical or physical deletion
- **None**: No audit trail (opt-out)

### Soft Delete Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Delete    │    │  Set Flag   │    │   Query     │
│   Request   │    │ IsDeleted   │    │  Filter     │
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       ▼                   ▼                   ▼
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│ entity.     │    │ IsDeleted = │    │ Where(e =>  │
│ Delete()    │────│ true        │────│ !e.IsDeleted│
│             │    │ DeletedAt = │    │ )           │
│             │    │ DateTime.Now│    │             │
└─────────────┘    └─────────────┘    └─────────────┘
```

## Build & Test Pipeline

```
┌─────────────────┐
│   Source Code   │
│      (src/)     │
└─────────────────┘
         │
         ▼
┌─────────────────┐    ┌─────────────────┐
│   Build Step    │    │   Test Step     │
│ • Clean         │    │ • Unit Tests    │
│ • Restore  ────►│────│ • Integration   │
│ • Build         │    │ • Coverage      │
└─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│   Quality Gate  │    │   Benchmarks    │
│ • SonarQube     │    │ • Performance   │
│ • Analysis ────►│────│ • Memory Usage  │
│ • Lint Rules    │    │ • Throughput    │
└─────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌─────────────────┐
│   Package       │    │   Documentation │
│ • NuGet         │    │ • API Docs      │
│ • Sign     ────►│────│ • Guides        │
│ • Publish       │    │ • Examples      │
└─────────────────┘    └─────────────────┘
```

### Build Configuration Hierarchy

```
Directory.Build.props (Global Properties)
├── TargetFramework: net9.0
├── Nullable: enable
├── ImplicitUsings: enable
└── Package Metadata

Directory.Packages.props (Version Management)
├── Microsoft.Extensions.*: 9.0.0
├── Microsoft.EntityFrameworkCore.*: 9.0.0
├── Testing: xunit.v3, FluentAssertions
└── Benchmarking: BenchmarkDotNet

Project Files (.csproj)
├── Domain: Core dependencies
├── Audit: Domain + EF Core
├── EntityFramework: Domain + Audit
└── Tests/Benchmarks: Project references
```

## Package Relationships

```
┌─────────────────────────────────────────┐
│             NuGet Packages              │
└─────────────────────────────────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ Wangkanai.  │ │ Wangkanai.  │ │ Wangkanai.  │
│   Domain    │ │   Audit     │ │EntityFramework
└─────────────┘ └─────────────┘ └─────────────┘
         │           │           │
         └───────────┼───────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│   System    │ │    .NET     │ │   Entity    │
│Dependencies │ │ Extensions  │ │ Framework   │
└─────────────┘ └─────────────┘ └─────────────┘
```

---

## Navigation Links

- [← Project Structure Guide](project-structure-guide.md)
- [API Reference →](api-reference.md)
- [← Back to Documentation Index](README.md)

---

*Last updated: 2025-09-04*
*Document version: 1.0*