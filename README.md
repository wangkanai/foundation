# Wangkanai Domain

## Domain: Breathtakingly Simple Domain-Driven Design for .NET

[![.NET](https://github.com/wangkanai/wangkanai/actions/workflows/dotnet.yml/badge.svg)](https://github.com/wangkanai/domain/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=wangkanai_domain&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=wangkanai_domain)

[![Open Collective](https://img.shields.io/badge/open%20collective-support%20me-3385FF.svg)](https://opencollective.com/wangkanai)
[![Patreon](https://img.shields.io/badge/patreon-support%20me-d9643a.svg)](https://www.patreon.com/wangkanai)
[![GitHub](https://img.shields.io/github/license/wangkanai/wangkanai)](https://github.com/wangkanai/wangkanai/blob/main/LICENSE)

Welcome to the **Wangkanai Domain** repository - a comprehensive collection of domain-driven design patterns, auditing
capabilities, and Entity Framework utilities for .NET applications.

## 📦 Packages

This repository produces three NuGet packages:

### Wangkanai.Domain

[![NuGet Version](https://img.shields.io/nuget/v/wangkanai.domain)](https://www.nuget.org/packages/wangkanai.domain)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.domain)](https://www.nuget.org/packages/wangkanai.domain)

Core domain-driven design patterns and building blocks for .NET applications. Provides base classes, interfaces, and utilities for
implementing clean domain models.

**Features:**

- Entity base classes with strongly-typed IDs
- Value objects with equality comparison
- Domain events and handlers
- Result patterns for error handling
- Audit trails and tracking

### Wangkanai.Audit

[![NuGet Version](https://img.shields.io/nuget/v/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.audit)](https://www.nuget.org/packages/wangkanai.audit)

Comprehensive auditing capabilities for tracking changes and maintaining data history in your applications.

**Features:**

- Automatic change tracking
- User audit trails
- Configurable audit policies
- Integration with Entity Framework
- Audit data querying and reporting

### Wangkanai.EntityFramework

[![NuGet Version](https://img.shields.io/nuget/v/wangkanai.entityframework)](https://www.nuget.org/packages/wangkanai.entityframework)
[![NuGet Pre Release](https://img.shields.io/nuget/vpre/wangkanai.entityframework)](https://www.nuget.org/packages/wangkanai.entityframework)

Entity Framework Core utilities and extensions to enhance database operations and development productivity.

**Features:**

- Database builder extensions
- Value generators for timestamps
- Convention-based configurations
- Migration utilities
- Performance optimizations

## 🚀 Quick Start

### Installation

Install the packages you need via NuGet Package Manager:

```bash
# Core domain patterns
dotnet add package Wangkanai.Domain

# Auditing capabilities
dotnet add package Wangkanai.Audit

# Entity Framework utilities
dotnet add package Wangkanai.EntityFramework
```

### Basic Usage

#### Domain Entities

```csharp
using Wangkanai.Domain;

public class Customer : Entity<int>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    public Customer(string name, string email)
    {
        Name = name;
        Email = email;
    }
}
```

#### Auditing

```csharp
using Wangkanai.Audit;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : AuditDbContext
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Configure audit tracking
        optionsBuilder.EnableAuditing();
    }
}
```

#### Entity Framework Extensions

```csharp
using Wangkanai.EntityFramework;

public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
}

public void Configure(IApplicationBuilder app)
{
    // Ensure database exists
    app.CreateDatabase<ApplicationDbContext>();
}
```

## 🏗️ Architecture

This repository follows a **clean architecture** approach with the following structure:

```
src/
├── Domain/           # Core domain patterns and entities
├── Audit/            # Auditing and change tracking
└── EntityFramework/  # EF Core utilities and extensions

tests/
├── Domain/           # Domain module tests
├── Audit/            # Audit module tests
└── EntityFramework/  # EntityFramework module tests

benchmark/
├── Domain/           # Performance benchmarks
├── Audit/            # Audit performance tests
└── EntityFramework/  # EF utilities benchmarks
```

## 🧪 Testing

Run all tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Run benchmarks:

```bash
dotnet run --project benchmark/Domain --configuration Release
dotnet run --project benchmark/Audit --configuration Release
dotnet run --project benchmark/EntityFramework --configuration Release
```

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