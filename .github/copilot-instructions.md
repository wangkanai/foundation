# GitHub Copilot Instructions

This repository contains **Wangkanai Domain** - a comprehensive collection of domain-driven design patterns, auditing capabilities, and Entity Framework utilities for .NET applications.

## Repository Overview

### Core Packages
- **Wangkanai.Domain**: Core DDD patterns with strongly-typed entities, value objects, domain events, and result patterns
- **Wangkanai.Audit**: Comprehensive auditing capabilities for tracking changes and maintaining data history
- **Wangkanai.EntityFramework**: EF Core utilities and extensions for enhanced database operations

### Architecture
- **Domain-Driven Design**: Rich domain models with clear boundaries and aggregate roots
- **Clean Architecture**: Separation of concerns across layers
- **Generic Programming**: Strongly-typed entities with flexible key types (int, Guid, string, etc.)
- **Audit Trail**: Comprehensive change tracking across all entities

## Development Guidelines

### Technology Stack
- **.NET 9.0**: Target framework
- **Entity Framework Core 9.0**: ORM support
- **Generic Constraints**: IEquatable&lt;T&gt; and IComparable&lt;T&gt; for entity keys
- **Nullable Reference Types**: Enabled throughout
- **File-scoped Namespaces**: Required (IDE0161 error level)

### Code Style Conventions

#### Formatting
- **Indentation**: 3 spaces (not tabs)
- **Line endings**: CRLF
- **Max line length**: 200 characters
- **Braces**: Next line for all constructs
- **var usage**: Preferred everywhere
- **this qualifier**: Avoided (IDE0003 error level)

#### Naming Conventions
```csharp
// Interfaces: IPascalCase
public interface IRepository<T> { }

// Private fields: _camelCase
private readonly string _connectionString;

// Private static fields: _camelCase
private static readonly ILogger _logger;

// Type parameters: TPascalCase
public class Entity<TKey> where TKey : IEquatable<TKey> { }
```

#### File Headers
All C# files must include:
```csharp
// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.
```

### Project Structure Patterns

#### Domain Module (src/Domain/)
```csharp
// Base entity with strongly-typed ID
public abstract class Entity<T> : IEntity<T> 
    where T : IEquatable<T>, IComparable<T>
{
    public T Id { get; protected set; }
    // Domain event handling, equality comparison
}

// Value objects with structural equality
public abstract class ValueObject : IValueObject
{
    // Equality based on all property values
}

// Domain events
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```

#### Audit Module (src/Audit/)
```csharp
// Auditable entities inherit from base types
public class AuditableEntity<TKey, TUserKey> : Entity<TKey>, IAuditable<TUserKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    public DateTime CreatedDate { get; set; }
    public TUserKey? CreatedBy { get; set; }
    // Automatic audit tracking
}

// Audit trail enumeration
public enum TrailType
{
    None,
    Create,
    Update,
    Delete
}
```

#### EntityFramework Module (src/EntityFramework/)
```csharp
// Database builder extensions
public static class DatabaseBuilderExtensions
{
    public static IApplicationBuilder CreateDatabase<TContext>(this IApplicationBuilder app)
        where TContext : DbContext
    {
        // Database creation logic
    }
}

// Value generators for timestamps
public class DateTimeNowGenerator : ValueGenerator<DateTime>
{
    public override DateTime Next(EntityEntry entry) => DateTime.Now;
}
```

### Common Patterns

#### Entity Creation
```csharp
// Strongly-typed entity with private setters
public class Customer : Entity<int>
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    private Customer() { } // EF Core

    public Customer(string name, string email)
    {
        Name = Guard.Against.NullOrEmpty(name, nameof(name));
        Email = Guard.Against.NullOrEmpty(email, nameof(email));
    }
}
```

#### Value Object Implementation
```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string zipCode)
    {
        Street = street;
        City = city;
        ZipCode = zipCode;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

#### Repository Pattern
```csharp
public interface IRepository<T, TKey> 
    where T : class, IEntity<TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
```

### Testing Patterns

#### Unit Test Structure
```csharp
[TestClass]
public class EntityTests
{
    [TestMethod]
    public void Constructor_WithValidData_CreatesEntity()
    {
        // Arrange
        var name = "Test Name";
        var email = "test@example.com";

        // Act
        var customer = new Customer(name, email);

        // Assert
        customer.Name.Should().Be(name);
        customer.Email.Should().Be(email);
    }
}
```

#### Test Project Organization
- Mirror the src/ structure in tests/
- Use FluentAssertions for readable assertions
- Include both unit tests and integration tests
- Test domain events and audit trails

### Build and Development

#### Solution Structure
```
Domain.slnx              # .NET 9.0 solution file
Directory.Build.props    # Common MSBuild properties
Directory.Packages.props # Centralized package management
```

#### Build Commands
```bash
# Restore packages
dotnet restore Domain.slnx

# Build solution
dotnet build Domain.slnx --no-restore -c Release

# Run tests with coverage
dotnet test Domain.slnx --configuration Release --collect:"XPlat Code Coverage"

# Run benchmarks
dotnet run --project benchmark/Domain --configuration Release
```

### Key Interfaces to Implement

#### Domain Interfaces
- **IEntity&lt;T&gt;**: For all domain entities
- **IAggregateRoot**: For aggregate root entities
- **IRepository&lt;T, TKey&gt;**: For data access
- **IUnitOfWork**: For transaction management
- **IDomainEvent**: For domain events

#### Audit Interfaces
- **IAuditable&lt;TUserKey&gt;**: For entities requiring audit trails
- **IAuditStore**: For audit data storage and querying

### Documentation Guidelines

- All documentation files (*.md) created should be saved in the `docs/` folder
- Use descriptive names for documentation files
- Follow the existing documentation structure and style
- Include code examples that compile and run

### Performance Considerations

- Use generic constraints for compile-time type safety
- Implement IEquatable&lt;T&gt; and IComparable&lt;T&gt; for entity keys
- Consider lazy loading patterns for related entities
- Use appropriate indexing strategies for audit queries
- Benchmark performance-critical operations in benchmark/ projects

### Common Gotchas

1. **Entity Keys**: Must implement IEquatable&lt;T&gt; and IComparable&lt;T&gt;
2. **Audit Trails**: Automatically captured for entities implementing IAuditable
3. **Domain Events**: Should be raised within aggregate boundaries
4. **Value Objects**: Must override equality comparison methods
5. **EF Core**: Use private parameterless constructors for entity hydration

This guidance ensures consistent, high-quality code that follows the established patterns and architectural principles of the Wangkanai Domain repository.