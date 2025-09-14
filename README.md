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

- **Entity Base Classes**: Strongly-typed entities with generic ID support
- **Value Objects**: Immutable value objects with structural equality
- **Aggregate Roots**: Support for aggregate boundaries and consistency rules
- **Domain Events**: Event-driven architecture with domain event handlers
- **Repositories**: Generic repository interfaces and patterns
- **Specifications**: Query specification pattern implementation
- **Result Patterns**: Railway-oriented programming for error handling
- **Domain Services**: Encapsulation of domain logic
- **Unit of Work**: Transaction boundary management

### Wangkanai.Audit

- Domain [![NuGet Version](https://img.shields.io/nuget/v/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)
  [![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)
- Application
- Infrastructure

Comprehensive auditing module for maintaining complete audit trails and change history in your domain models. Seamlessly
integrates with the Foundation module to provide automatic tracking capabilities.

**Features:**

- **Automatic Change Tracking**: Intercept and record all entity modifications
- **User Audit Trails**: Track who made changes and when
- **Audit Entities**: Pre-built audit entity base classes
- **Flexible Configuration**: Configurable audit policies and filters
- **Audit Context**: Capture additional contextual information
- **Query Support**: Rich querying capabilities for audit data
- **Compliance Ready**: Meet regulatory and compliance requirements
- **Soft Delete Support**: Track deletions without data loss
- **Field-Level Tracking**: Monitor changes at property level

### Wangkanai.EntityFramework

[![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework)](https://www.nuget.org/packages/wangkanai.entityframework)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework)](https://www.nuget.org/packages/wangkanai.entityframework)

- Postgres
- SqlServer
- MySql
- Sqlite

Entity Framework Core integration module that bridges DDD patterns with EF Core's powerful ORM capabilities. Provides utilities
and extensions for seamless database operations while maintaining domain model purity.

**Features:**

- **DbContext Extensions**: Enhanced context configuration and setup
- **Repository Implementation**: EF Core-based repository patterns
- **Value Converters**: Custom converters for value objects
- **Shadow Properties**: Automatic timestamp and audit field management
- **Migration Helpers**: Simplified database migration utilities
- **Query Extensions**: LINQ extensions for common query patterns
- **Performance Optimizations**: Query optimization and caching strategies
- **Convention Configurations**: Automatic entity configuration based on conventions
- **Seed Data Support**: Fluent API for database seeding
- **Multi-tenancy Support**: Built-in tenant isolation patterns

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