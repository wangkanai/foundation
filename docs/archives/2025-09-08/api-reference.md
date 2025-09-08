# Wangkanai Domain Library - API Reference

This document provides comprehensive API documentation for the Wangkanai Domain library, a performance-optimized domain-driven design (DDD) framework for .NET applications.

## Table of Contents

- [Overview](#overview)
- [Domain Module API](#domain-module-api)
- [Audit Module API](#audit-module-api)
- [EntityFramework Module API](#entityframework-module-api)
- [Performance Characteristics](#performance-characteristics)
- [Cross-Module Integration](#cross-module-integration)
- [Migration Guide](#migration-guide)

## Overview

The Wangkanai Domain library consists of three primary modules:

- **Domain Module** (`Wangkanai.Domain`): Core DDD patterns including entities, value objects, domain events, and result patterns
- **Audit Module** (`Wangkanai.Audit`): Comprehensive audit trail system with JSON-based storage and span operations  
- **EntityFramework Module** (`Wangkanai.EntityFramework`): EF Core integration with audit-aware contexts

### Key Features

- **Performance-Optimized**: Compiled property accessors, type caching, and span-based operations
- **Generic Programming**: Strongly-typed entities with flexible key types
- **Audit Trail**: Comprehensive change tracking with JSON-based serialization
- **Clean Architecture**: Clear separation of concerns across DDD layers
- **EF Core Integration**: Seamless ORM integration with convention-based configurations

---

## Domain Module API

### Core Interfaces

#### IEntity&lt;T&gt;

```csharp
public interface IEntity<T> where T : IComparable<T>, IEquatable<T>
{
    T Id { get; set; }
    bool IsTransient();
}
```

**Description**: Base contract for all domain entities with strongly-typed identifiers.

**Generic Constraints**:
- `T`: Must implement `IComparable<T>` and `IEquatable<T>`

**Methods**:
- `IsTransient()`: Returns `true` if the entity has not been persisted (ID equals default value)

#### IAggregateRoot

```csharp
public interface IAggregateRoot : IAggregateRoot<int>, IKeyIntEntity;

public interface IAggregateRoot<T> : IEntity<T> 
    where T : IComparable<T>, IEquatable<T>;
```

**Description**: Defines aggregate roots in DDD context, ensuring controlled access to aggregates.

**Usage**:
```csharp
public class Order : Entity<Guid>, IAggregateRoot<Guid>
{
    // Aggregate root implementation
}
```

#### IValueObject

```csharp
public interface IValueObject;

public interface IValueObject<T> where T : class;
```

**Description**: Marker interface for value objects - immutable objects compared by value equality.

### Core Classes

#### Entity&lt;T&gt;

```csharp
public abstract class Entity<T> : IEntity<T> 
    where T : IEquatable<T>, IComparable<T>
```

**Description**: Abstract base class for domain entities with performance-optimized type caching.

**Key Features**:
- **Type Caching**: ~10% performance improvement with EF dynamic proxy detection
- **Thread-Safe**: Concurrent caching with LRU eviction 
- **Memory Management**: Bounded cache prevents memory leaks

**Properties**:
- `Id`: The unique identifier of type `T`

**Methods**:
- `IsTransient()`: Checks if entity is not yet persisted
- `GetPerformanceStats()`: Returns cache hit/miss statistics
- `ClearTypeCache()`: Clears type cache for testing/memory management

**Performance Characteristics**:
- Cache hit ratio: >95% in typical applications
- Memory usage: Bounded to 1000 cached type mappings
- Thread safety: Lock-free concurrent operations

**Usage Example**:
```csharp
public class Product : Entity<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Usage
var product = new Product { Name = "Sample", Price = 99.99m };
if (product.IsTransient())
{
    // Entity not yet saved
}
```

#### ValueObject

```csharp
public abstract class ValueObject : IValueObject, ICacheKey, ICloneable
```

**Description**: Base class for value objects with performance-optimized equality comparisons.

**Key Features**:
- **Compiled Accessors**: 500-1000x performance improvement over reflection
- **Automatic Fallback**: Degrades gracefully for complex scenarios
- **Caching Support**: Built-in cache key generation

**Methods**:
- `GetCacheKey()`: Generates unique cache key based on object state
- `Clone()`: Creates shallow copy using `MemberwiseClone()`
- `GetProperties()`: Returns cached property information
- `GetEqualityComponents()`: Virtual method for custom equality logic

**Performance Optimizations**:
- Pre-compiled property accessors eliminate reflection overhead
- Intelligent type analysis determines optimization eligibility
- Fallback to reflection for complex property types

**Usage Example**:
```csharp
public class Address : ValueObject
{
    public string Street { get; init; }
    public string City { get; init; }
    public string ZipCode { get; init; }
}

// Usage
var address1 = new Address { Street = "123 Main St", City = "Springfield", ZipCode = "12345" };
var address2 = new Address { Street = "123 Main St", City = "Springfield", ZipCode = "12345" };

Console.WriteLine(address1.Equals(address2)); // True - value equality
Console.WriteLine(address1.GetCacheKey()); // "123 Main St|Springfield|12345"
```

### Specialized Entity Types

The library provides convenience interfaces for common key types:

```csharp
public interface IKeyIntEntity : IEntity<int>
public interface IKeyLongEntity : IEntity<long>  
public interface IKeyGuidEntity : IEntity<Guid>
public interface IKeyStringEntity : IEntity<string>
public interface IKeyByteEntity : IEntity<byte>
```

Corresponding base classes:
```csharp
public abstract class KeyIntEntity : Entity<int>, IKeyIntEntity
public abstract class KeyLongEntity : Entity<long>, IKeyLongEntity
public abstract class KeyGuidEntity : Entity<Guid>, IKeyGuidEntity
public abstract class KeyStringEntity : Entity<string>, IKeyStringEntity
public abstract class KeyByteEntity : Entity<byte>, IKeyByteEntity
```

### Repository Pattern

#### IRepository&lt;T&gt;

```csharp
public interface IRepository<in T> : IDisposable where T : class
{
    IUnitOfWork UnitOfWork { get; }
    void Attach(T item);
    void Add(T item);
    void Update(T item);
    void Delete(T item);
}
```

#### IAsyncRepository&lt;T&gt;

```csharp
public interface IAsyncRepository<T> : IAsyncDisposable where T : class
{
    IUnitOfWorkAsync UnitOfWork { get; }
    Task<T> AttachAsync(T item);
    Task<T> AddAsync(T item);
    Task<T> UpdateAsync(T item);
    Task<T> DeleteAsync(T item);
}
```

### Domain Events

#### IDomainEvent

```csharp
public interface IDomainEvent : IKeyIntEntity, IDomainEvent<int>;

public interface IDomainEvent<T> : IEntity<T>, IDomainMessage 
    where T : IComparable<T>, IEquatable<T>
{
    int Version { get; set; }
    DateTimeOffset TimeStamp { get; set; }
}
```

**Usage Example**:
```csharp
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}
```

### Result Pattern

#### Result

```csharp
public class Result
{
    public Error Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    
    public static Result Success()
    public static Result<TValue> Success<TValue>(TValue value)
    public static Result Failure(Error error)
    public static Result<TValue> Failure<TValue>(Error error)
    public static Result<TValue> Create<TValue>(TValue? value, Error error)
    public static Result FirstFailureOrSuccess(params Result[] results)
}

public class Result<T> : Result
{
    public T Value { get; }
    public static implicit operator Result<T>(T value)
}
```

**Usage Example**:
```csharp
public Result<Order> CreateOrder(OrderRequest request)
{
    if (request.Items.Count == 0)
        return Result.Failure<Order>(OrderErrors.NoItems);
        
    var order = new Order(request);
    return Result.Success(order);
}

// Usage
var result = CreateOrder(request);
if (result.IsSuccess)
{
    var order = result.Value;
    // Process order
}
else
{
    Console.WriteLine($"Error: {result.Error}");
}
```

---

## Audit Module API

### Core Audit Entity

#### Audit&lt;TKey, TUserType, TUserKey&gt;

```csharp
public class Audit<TKey, TUserType, TUserKey> : Entity<TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
    where TUserType : IdentityUser<TUserKey>
    where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
```

**Properties**:
```csharp
public AuditTrailType TrailType { get; set; }      // Create, Update, Delete, None
public TUserKey? UserId { get; set; }              // User who performed action
public TUserType? User { get; set; }               // User entity reference
public DateTime Timestamp { get; set; }            // When action occurred
public string? PrimaryKey { get; set; }            // Entity primary key
public string EntityName { get; set; }             // Entity type name
public List<string> ChangedColumns { get; set; }   // Changed property names
public string? OldValuesJson { get; set; }         // JSON of old values
public string? NewValuesJson { get; set; }         // JSON of new values
```

**Performance-Optimized Methods**:
```csharp
// High-performance JSON operations
public void SetValuesFromJson(string? oldValuesJson, string? newValuesJson)
public void SetValuesFromSpan<T>(ReadOnlySpan<string> columnNames, 
    ReadOnlySpan<T> oldValues, ReadOnlySpan<T> newValues)

// Efficient single-value access without full deserialization
public object? GetOldValue(string columnName)
public object? GetNewValue(string columnName)
```

**Backward Compatibility Properties**:
```csharp
[JsonIgnore]
public Dictionary<string, object> OldValues { get; set; }
[JsonIgnore] 
public Dictionary<string, object> NewValues { get; set; }
```

**Performance Characteristics**:
- **Memory Efficiency**: Direct JSON storage reduces memory allocations by ~60%
- **Span Operations**: Zero-allocation operations for small change sets (≤3 properties)
- **Selective Access**: Individual value retrieval without full deserialization

**Usage Example**:
```csharp
// High-performance audit creation
var audit = new Audit<Guid, ApplicationUser, string>
{
    TrailType = AuditTrailType.Update,
    EntityName = nameof(Product),
    PrimaryKey = product.Id.ToString(),
    Timestamp = DateTime.UtcNow,
    UserId = currentUser.Id
};

// Efficient span-based value setting for small changes
ReadOnlySpan<string> columns = ["Name", "Price"];
ReadOnlySpan<object> oldValues = [oldProduct.Name, oldProduct.Price];
ReadOnlySpan<object> newValues = [product.Name, product.Price];

audit.SetValuesFromSpan(columns, oldValues, newValues);
```

#### AuditTrailType

```csharp
public enum AuditTrailType : byte
{
    None = 0,
    Create = 1,
    Update = 2,
    Delete = 3
}
```

### Auditable Entity Base Classes

#### AuditableEntity&lt;T&gt;

```csharp
public abstract class AuditableEntity<T> : Entity<T>, IAuditable
    where T : IComparable<T>, IEquatable<T>
{
    public virtual bool ShouldSerializeAuditableProperties { get; }
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
    
    public virtual bool ShouldSerializeCreatedDate()
    public virtual bool ShouldSerializeUpdatedDate()
}
```

#### UserAuditableEntity&lt;T, TUserKey&gt;

```csharp
public abstract class UserAuditableEntity<T, TUserKey> : AuditableEntity<T>, IUserAuditable<TUserKey>
    where T : IComparable<T>, IEquatable<T>
    where TUserKey : IEquatable<TUserKey>
{
    public TUserKey? CreatedBy { get; set; }
    public TUserKey? UpdatedBy { get; set; }
}
```

### Audit Interfaces

```csharp
public interface IAuditable : ICreatedEntity, IUpdatedEntity;
public interface IUserAuditable<TUserKey> : IAuditable, IUserSoftDeleteAuditable<TUserKey>;
public interface ISoftDeleteAuditable : IAuditable, ISoftDeletable;
public interface IUserSoftDeleteAuditable<TUserKey> : IUserAuditable<TUserKey>, ISoftDeletable;
```

**Component Interfaces**:
```csharp
public interface ICreatedEntity 
{
    DateTime? Created { get; set; }
}

public interface IUpdatedEntity
{
    DateTime? Updated { get; set; }
}

public interface IDeletedEntity
{
    DateTime? Deleted { get; set; }
}

public interface ISoftDeletable : IDeletedEntity
{
    bool IsDeleted { get; set; }
}
```

### Audit Store

#### IAuditStore&lt;TKey, TUserType, TUserKey&gt;

```csharp
public interface IAuditStore<TKey, TUserType, TUserKey> : IDisposable
    where TKey : IEquatable<TKey>, IComparable<TKey>
    where TUserType : IdentityUser<TUserKey>  
    where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
    // CRUD Operations
    Task<Result<Audit<TKey, TUserType, TUserKey>>> CreateAsync(
        Audit<TKey, TUserType, TUserKey> audit, CancellationToken cancellationToken);
    
    Task<Result<Audit<TKey, TUserType, TUserKey>>> UpdateAsync(
        Audit<TKey, TUserType, TUserKey> auditTrail, CancellationToken cancellationToken);
    
    Task<Result<Audit<TKey, TUserType, TUserKey>>> DeleteAsync(
        Audit<TKey, TUserType, TUserKey> auditTrail, CancellationToken cancellationToken);

    // Query Operations  
    Task<Result<Audit<TKey, TUserType, TUserKey>?>> FindByIdAsync(
        TKey id, CancellationToken cancellationToken);
    
    Task<Result<Audit<TKey, TUserType, TUserKey>?>> FindByIdAsync(
        TKey id, TUserKey userId, CancellationToken cancellationToken);
    
    Task<Result<Audit<TKey, TUserType, TUserKey>?>> FindByUserIdAsync(
        TUserKey userId, CancellationToken cancellationToken);
    
    Task<Result<Audit<TKey, TUserType, TUserKey>?>> FindByUserIdAsync(
        TUserKey userId, TKey id, CancellationToken cancellationToken);
}
```

#### IQueryableAuditStore&lt;TKey, TUserType, TUserKey&gt;

```csharp
public interface IQueryableAuditStore<TKey, TUserType, TUserKey> 
    : IAuditStore<TKey, TUserType, TUserKey>
{
    IQueryable<Audit<TKey, TUserType, TUserKey>> Audits { get; }
}
```

---

## EntityFramework Module API

### Database Context

#### AuditDbContext

```csharp
public abstract class AuditDbContext : DbContext, IAuditDbContext
{
    public DbContext DbContext => this;
}
```

#### IAuditDbContext

```csharp
public interface IAuditDbContext
{
    DbContext DbContext { get; }
}
```

**Usage Example**:
```csharp
public class ApplicationDbContext : AuditDbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Audit<Guid, ApplicationUser, string>> AuditTrails { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure audit entities
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### Database Extensions

#### DatabaseBuilderExtensions

```csharp
public static class DatabaseBuilderExtensions
{
    // Synchronous database operations
    public static IApplicationBuilder CreateDatabase<T>(this IApplicationBuilder app)
        where T : DbContext
    
    public static IApplicationBuilder MigrateDatabase<T>(this IApplicationBuilder app)
        where T : DbContext
    
    // Asynchronous database operations
    public static async Task<IApplicationBuilder> CreateDatabaseAsync<T>(this IApplicationBuilder app)
        where T : DbContext
    
    public static async Task<IApplicationBuilder> MigrateDatabaseAsync<T>(this IApplicationBuilder app)
        where T : DbContext
}
```

**Usage Example**:
```csharp
// In Program.cs or Startup.cs
app.MigrateDatabase<ApplicationDbContext>();

// Or async version
await app.MigrateDatabaseAsync<ApplicationDbContext>();
```

### Value Generators

#### DateTimeNowGenerator

```csharp
public class DateTimeNowGenerator : ValueGenerator<DateTime>
{
    public override DateTime Next(EntityEntry entry) => DateTime.Now;
    public override bool GeneratesTemporaryValues => false;
}
```

#### DateTimeOffsetNowGenerator

```csharp
public class DateTimeOffsetNowGenerator : ValueGenerator<DateTimeOffset>
{
    public override DateTimeOffset Next(EntityEntry entry) => DateTimeOffset.Now;
    public override bool GeneratesTemporaryValues => false;
}
```

**Usage in Entity Configuration**:
```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.CreatedDate)
               .HasValueGenerator<DateTimeNowGenerator>();
    }
}
```

---

## Performance Characteristics

### Domain Module Performance

#### Entity&lt;T&gt; Performance

| Operation | Performance Improvement | Details |
|-----------|------------------------|---------|
| Type Resolution | ~10% faster | Cached EF proxy type mapping |
| Equality Comparison | Cache-optimized | >95% cache hit ratio |
| Memory Usage | Bounded | LRU eviction at 1000 entries |

**Benchmark Results** (typical application):
```
Cache Hit Ratio: 95.8%
Memory Usage: ~50KB for 1000 cached types  
Thread Safety: Lock-free concurrent operations
```

#### ValueObject Performance

| Operation | Performance Improvement | Details |
|-----------|------------------------|---------|
| Equality Comparison | 500-1000x faster | Compiled property accessors |
| Property Access | Zero reflection | Pre-compiled expressions |
| Fallback Mechanism | Automatic | Graceful degradation for complex types |

**Optimization Eligibility**:
- ✅ Simple properties (primitives, strings, DateTime)
- ✅ Value objects with public readable properties  
- ❌ Complex enumerables or interface properties
- ❌ Properties requiring custom serialization

### Audit Module Performance

#### JSON-Based Storage

| Metric | Improvement | Details |
|--------|-------------|---------|
| Memory Usage | ~60% reduction | Direct JSON vs Dictionary<string, object> |
| Serialization | ~40% faster | Pre-serialized storage |
| Small Changes | Zero allocation | Span-based operations (≤3 properties) |

**Performance Profiles**:
```csharp
// Small changes (≤3 properties): Span-based, zero allocation
audit.SetValuesFromSpan(columns, oldValues, newValues);

// Large changes (>3 properties): Dictionary fallback  
audit.OldValues = oldValuesDictionary;

// Single value access: No full deserialization
var oldName = audit.GetOldValue("Name");
```

### EF Core Integration Performance

- **Convention-based Configuration**: Reduces boilerplate code
- **Audit-Aware Contexts**: Minimal overhead for change tracking
- **Database Migration Extensions**: Simplified deployment workflows

---

## Cross-Module Integration

### Entity with Audit Trail

```csharp
public class Product : AuditableEntity<Guid>, IAggregateRoot<Guid>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; } // Value Object
}

public class ProductCategory : ValueObject  
{
    public string Name { get; init; }
    public string Description { get; init; }
}
```

### Domain Events with Audit

```csharp
public class ProductPriceChangedEvent : DomainEvent
{
    public Guid ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string ChangedBy { get; set; }
}
```

### EF Core Configuration

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Entity configuration
        builder.HasKey(p => p.Id);
        
        // Audit configuration
        builder.Property(p => p.Created)
               .HasValueGenerator<DateTimeNowGenerator>();
               
        // Value object configuration  
        builder.OwnsOne(p => p.Category, category =>
        {
            category.Property(c => c.Name).HasMaxLength(100);
            category.Property(c => c.Description).HasMaxLength(500);
        });
    }
}
```

### Repository Implementation

```csharp
public class ProductRepository : IAsyncRepository<Product>
{
    private readonly ApplicationDbContext _context;
    
    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public IUnitOfWorkAsync UnitOfWork => _context;
    
    public async Task<Product> AddAsync(Product product)
    {
        var entry = await _context.Products.AddAsync(product);
        return entry.Entity;
    }
    
    // Other CRUD operations...
}
```

---

## Migration Guide

### From Legacy Domain Models

#### Step 1: Update Entity Base Classes

**Before**:
```csharp
public class Product  
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}
```

**After**:
```csharp
public class Product : Entity<Guid>, IAggregateRoot<Guid>
{
    public string Name { get; set; }
    
    // Inherit: Id property, IsTransient(), equality operations
}
```

#### Step 2: Convert Value Objects

**Before**:
```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    
    public override bool Equals(object obj) 
    {
        // Manual equality implementation
    }
}
```

**After**:
```csharp
public class Address : ValueObject
{
    public string Street { get; init; }  // Immutable
    public string City { get; init; }
    
    // Inherit: Equals, GetHashCode, operators, caching
}
```

#### Step 3: Add Audit Support

**Before**:
```csharp
public class Product : Entity<Guid>
{
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
```

**After**:
```csharp  
public class Product : AuditableEntity<Guid>, IAggregateRoot<Guid>
{
    // Inherit: Created, Updated properties with serialization control
}
```

#### Step 4: Implement Result Pattern

**Before**:
```csharp
public Product CreateProduct(string name)
{
    if (string.IsNullOrEmpty(name))
        throw new ArgumentException("Name required");
        
    return new Product { Name = name };
}
```

**After**:
```csharp
public Result<Product> CreateProduct(string name)
{
    if (string.IsNullOrEmpty(name))
        return Result.Failure<Product>(ProductErrors.NameRequired);
        
    var product = new Product { Name = name };
    return Result.Success(product);
}
```

### Performance Optimization Checklist

1. **Entity Optimization**:
   - ✅ Use strongly-typed IDs with proper constraints
   - ✅ Leverage Entity<T> caching for EF proxies
   - ✅ Monitor cache hit ratios with `GetPerformanceStats()`

2. **Value Object Optimization**:
   - ✅ Keep properties simple for compiled accessor eligibility
   - ✅ Use immutable properties (`init` setters)
   - ✅ Avoid complex enumerables in equality components

3. **Audit Optimization**:
   - ✅ Use `SetValuesFromSpan()` for small change sets
   - ✅ Access individual values with `GetOldValue()`/`GetNewValue()`
   - ✅ Minimize use of Dictionary properties for performance

4. **EF Core Integration**:
   - ✅ Use convention-based configuration where possible
   - ✅ Configure value generators for audit timestamps
   - ✅ Leverage built-in database extension methods

### Breaking Changes

When migrating from previous versions, be aware of:

1. **Generic Constraints**: All entity keys must implement `IEquatable<T>` and `IComparable<T>`
2. **Value Object Changes**: Equality components now use optimized accessors by default
3. **Audit Storage**: JSON-based storage replaces Dictionary properties for performance
4. **Result Pattern**: Methods returning nullable types should use `Result<T>` pattern

### Best Practices

1. **Entity Design**:
   - Keep entities focused on business invariants
   - Use aggregate roots to control access boundaries
   - Implement domain events for cross-boundary communication

2. **Value Object Design**:
   - Make value objects immutable
   - Keep equality components simple for performance
   - Use value objects for complex concepts that don't require identity

3. **Audit Design**:
   - Use appropriate audit entity base classes
   - Configure audit storage for your performance needs
   - Implement soft delete patterns where appropriate

4. **Performance**:
   - Monitor Entity<T> cache performance in production
   - Use span-based audit operations for high-throughput scenarios
   - Profile value object equality operations under load

This API reference provides comprehensive guidance for using the Wangkanai Domain library effectively in your .NET applications. For additional examples and advanced scenarios, refer to the test projects and benchmark implementations in the repository.