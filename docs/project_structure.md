# Project Structure

## Root Directory Structure

```
/
├── src/                    # Source code for the three main packages
│   ├── Domain/            # Wangkanai.Domain - Core DDD patterns
│   ├── Audit/             # Wangkanai.Audit - Auditing capabilities
│   └── EntityFramework/   # Wangkanai.EntityFramework - EF utilities
├── tests/                 # Unit tests mirroring src structure
│   ├── Domain/            # Tests for Domain package
│   ├── Audit/             # Tests for Audit package
│   └── EntityFramework/   # Tests for EntityFramework package
├── benchmark/             # Performance benchmarks
│   ├── Domain/            # Domain benchmarks
│   ├── Audit/             # Audit benchmarks
│   └── EntityFramework/   # EntityFramework benchmarks
├── .github/               # GitHub Actions CI/CD
├── Assets/                # Package assets (logo, etc.)
└── [build files]          # Solution files, build props, config
```

## Domain Module Structure (src/Domain/)

```
Domain/
├── Common/                # Common utilities
├── Events/                # Domain event implementations
├── Extensions/            # Extension methods
├── Interfaces/            # Core interfaces (IEntity, IRepository, etc.)
├── Messages/              # Message/command patterns
├── Primitives/            # Primitive value types
├── Properties/            # Assembly properties
├── Caching/               # Caching abstractions
├── Entity.cs              # Base Entity<T> class
├── ValueObject.cs         # Base ValueObject class
├── KeyXxxEntity.cs        # Specific key type entities (Int, Guid, String, etc.)
└── [other core files]     # TrailType, EntryState, etc.
```

## Key Interfaces

- **IEntity<T>**: Base entity contract with strongly-typed ID
- **IAggregateRoot**: Domain aggregate root marker
- **IRepository**: Repository pattern interface
- **IUnitOfWork**: Unit of work pattern
- **IAuditable**: Auditing contract
- **IValueObject**: Value object marker

## Configuration Files

- **Domain.slnx**: Solution file (new format)
- **Directory.Build.props**: Common MSBuild properties
- **Directory.Packages.props**: Centralized package management
- **Directory.Build.targets**: MSBuild targets and conditions
- **.editorconfig**: Code style enforcement

## Special Files

- **CLAUDE.md**: Claude Code guidance (project-specific)
- **build.ps1**: PowerShell build script
- **sign.ps1**: Package signing script
- **.repomixignore**: Repomix configuration exclusions