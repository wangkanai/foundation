# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture

### Project Structure

- **src/Domain/**: Core domain-driven design patterns and entities
- **src/Audit/**: Auditing and change tracking capabilities
- **src/EntityFramework/**: EF Core utilities and extensions
- **tests/**: Unit tests mirroring the src structure
- **benchmark/**: Performance benchmarks for each module

### Key Components

#### Domain Module (Wangkanai.Domain)

- **Entity<T>**: Abstract base class for entities with strongly-typed IDs
- **ValueObject**: Abstract base for immutable value objects with structural equality
- **Domain Events**: Event-driven architecture support with IDomainEvent and handlers
- **Result Patterns**: Error handling with Result<T> types
- **Interfaces**: IAggregateRoot, IRepository, IUnitOfWork, IAuditable

#### Audit Module (Wangkanai.Audit)

- **Audit<TKey, TUserType, TUserKey>**: Generic audit trail entity
- **AuditableEntity/UserAuditableEntity**: Base entities with automatic audit tracking
- **TrailType**: Enumeration for audit action types (Create, Update, Delete, None)
- **AuditStore**: Storage and querying of audit records

#### EntityFramework Module (Wangkanai.EntityFramework)

- **DatabaseBuilderExtensions**: Extension methods for database setup
- **Value Generators**: DateTimeNowGenerator, DateTimeOffsetNowGenerator for timestamps
- **Convention-based configurations**: Automated EF Core configuration

### Architectural Patterns

- **Domain-Driven Design**: Rich domain models with clear boundaries
- **Clean Architecture**: Separation of concerns across layers
- **Generic Programming**: Strongly-typed entities with flexible key types
- **Audit Trail**: Comprehensive change tracking across all entities
- **Entity Framework Integration**: Seamless ORM integration with DDD patterns

### Technology Stack

- **.NET 9.0**: Target framework
- **Entity Framework Core 9.0**: ORM support
- **Generic Constraints**: IEquatable<T> and IComparable<T> for all entity keys
- **Nullable Reference Types**: Enabled throughout the solution
- **Implicit Usings**: Simplified namespace management