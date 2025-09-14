# Wangkanai Domain-Driven-Design

## A Comprehensive Domain-Driven Design Framework for .NET

[![.NET](https://github.com/wangkanai/wangkanai/actions/workflows/dotnet.yml/badge.svg)](https://github.com/wangkanai/domain/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=wangkanai_foundation&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=wangkanai_foundation)

[![Open Collective](https://img.shields.io/badge/open%20collective-support%20me-3385FF.svg)](https://opencollective.com/wangkanai)
[![Patreon](https://img.shields.io/badge/patreon-support%20me-d9643a.svg)](https://www.patreon.com/wangkanai)
[![GitHub](https://img.shields.io/github/license/wangkanai/wangkanai)](https://github.com/wangkanai/wangkanai/blob/main/LICENSE)

Welcome to the **Wangkanai Domain-Driven-Design Foundation** repository - a robust framework providing essential building blocks
for implementing Domain-Driven Design (DDD) patterns in .NET applications. This foundation library offers core domain modeling
capabilities, comprehensive auditing features, and Entity Framework Core integrations to accelerate enterprise application
development.

## 📦 Modules

This repository produces three NuGet packages:

### Wangkanai.Foundation

| Package                             | Release                                                                                                                                                    | Pre-Release                                                                                                                                                       |
|-------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Wangkanai.Foundation.Domain         | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.foundation.domain)](https://www.nuget.org/packages/wangkanai.foundation.domain)                 | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.foundation.domain)](https://www.nuget.org/packages/wangkanai.foundation.domain)                 |
| Wangkanai.Foundation.Application    | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.foundation.application)](https://www.nuget.org/packages/wangkanai.foundation.application)       | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.foundation.application)](https://www.nuget.org/packages/wangkanai.foundation.application)       |
| Wangkanai.Foundation.Infrastructure | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.foundation.infrastructure)](https://www.nuget.org/packages/wangkanai.foundation.infrastructure) | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.foundation.infrastructure)](https://www.nuget.org/packages/wangkanai.foundation.infrastructure) |

Core domain-driven design patterns and fundamental building blocks for implementing DDD in .NET applications. This module provides
the essential abstractions and base implementations for creating rich domain models.

**Features:**

- **Entity Base Classes**: Strongly-typed entities with generic ID support, optimized EF proxy handling, and performance caching
- **Value Objects**: Immutable value objects with structural equality, cache key generation, and optimized equality comparison
- **Aggregate Roots**: Support for aggregate boundaries with typed ID constraints (Int, Guid, String, Long, Byte)
- **Domain Events**: Event-driven architecture with versioning, timestamps, and event suppression capabilities
- **Repository Pattern**: Generic sync/async repository interfaces with Unit of Work pattern support
- **Result Pattern**: Railway-oriented programming for error handling with typed success/failure results
- **Domain Exceptions**: Structured exception hierarchy for domain-specific errors
- **Cache Key Management**: Advanced cache key generation with type-based and composite key support
- **Change Tracking**: Entity state tracking with entry state management (Unchanged, Modified, Added, Deleted)
- **Performance Optimizations**: Built-in performance monitoring and type caching for improved runtime efficiency

### Wangkanai.Audit

| Package                             | Release                                                                                                                                                    | Pre-Release                                                                                                                                                       |
|-------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Wangkanai.Audit.Domain              | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)                                         | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)                                         |
| Wangkanai.Audit.Application         | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.audit.application)](https://www.nuget.org/packages/wangkanai.audit.application)                 | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.audit.application)](https://www.nuget.org/packages/wangkanai.audit.application)                 |
| Wangkanai.Audit.Infrastructure      | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.audit.infrastructure)](https://www.nuget.org/packages/wangkanai.audit.infrastructure)           | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.audit.infrastructure)](https://www.nuget.org/packages/wangkanai.audit.infrastructure)           |

Comprehensive auditing module for maintaining complete audit trails and change history in your domain models. Seamlessly
integrates with the Foundation module to provide automatic tracking capabilities.

**Features:**

- **Audit Trail Entity**: Comprehensive trail tracking with user, timestamp, and change details
- **Auditable Entity Base Classes**: Pre-built entities with Created, Updated, Deleted timestamps
- **User Auditable Entities**: Track CreatedBy, UpdatedBy, DeletedBy user information
- **Trail Type Enumeration**: Categorize changes as Create, Update, Delete, or None
- **JSON Value Storage**: Efficient storage of old/new values in optimized JSON format
- **Field-Level Change Tracking**: Monitor and record specific column modifications
- **Trail Store Repository**: Queryable store for managing and retrieving audit records
- **Entity Configuration Builders**: Fluent API for configuring audit behavior
- **Soft Delete Support**: Track deletions with timestamp and user information
- **Row Version Support**: Optimistic concurrency control with version tracking

### Wangkanai.EntityFramework

| Package                                | Release                                                                                                                                                          | Pre-Release                                                                                                                                                             |
|---------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Wangkanai.EntityFramework.Postgres    | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework.postgres)](https://www.nuget.org/packages/wangkanai.entityframework.postgres)         | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework.postgres)](https://www.nuget.org/packages/wangkanai.entityframework.postgres)         |
| Wangkanai.EntityFramework.SqlServer   | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework.sqlserver)](https://www.nuget.org/packages/wangkanai.entityframework.sqlserver)       | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework.sqlserver)](https://www.nuget.org/packages/wangkanai.entityframework.sqlserver)       |
| Wangkanai.EntityFramework.MySql       | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework.mysql)](https://www.nuget.org/packages/wangkanai.entityframework.mysql)               | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework.mysql)](https://www.nuget.org/packages/wangkanai.entityframework.mysql)               |
| Wangkanai.EntityFramework.Sqlite      | [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework.sqlite)](https://www.nuget.org/packages/wangkanai.entityframework.sqlite)             | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework.sqlite)](https://www.nuget.org/packages/wangkanai.entityframework.sqlite)             |

Entity Framework Core integration module that bridges DDD patterns with EF Core's powerful ORM capabilities. Provides utilities
and extensions for seamless database operations while maintaining domain model purity.

**Features:**

- **Database Management Extensions**: CreateDatabase, MigrateDatabase with async support
- **Value Generators**: DateTime and DateTimeOffset generators for automatic timestamps
- **Database Provider Support**: Specialized extensions for Postgres, SqlServer, MySql, Sqlite
- **PostgreSQL Features**: JSONB support, array types, full-text search, partitioning, real-time notifications
- **SQL Server Features**: Temporal tables, columnstore indexes, memory-optimized tables, change tracking
- **MySQL Features**: JSON support, replication configuration, partitioning, query optimization
- **SQLite Features**: JSON1 extension, full-text search, spatial data, version management
- **Bulk Operations**: Efficient bulk insert/update/delete across all providers
- **Index Optimization**: Provider-specific index configurations and strategies
- **Connection Management**: Advanced connection pooling and configuration options

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## 📋 Requirements

- .NET 9.0 or later
- Entity Framework Core 9.0 or later (for EF-related packages)

## 📄 License

This project is licensed under the [Apache License 2.0](LICENSE) - see the LICENSE file for details.

## 💝 Support

If this project has helped you, please consider:

- ⭐ Starring the repository
- 🐛 [Reporting bugs or requesting features](https://github.com/wangkanai/domain/issues)
- 💰 [Supporting via Open Collective](https://opencollective.com/wangkanai)
- 💰 [Supporting via Patreon](https://www.patreon.com/wangkanai)

## 📚 Documentation

For detailed documentation, examples, and API references, visit our [Documentation](https://wangkanai.github.io/domain).

---

**Built with ❤️ by [Sarin Na Wangkanai](https://github.com/wangkanai) and the community.**