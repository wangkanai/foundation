# Frequently Asked Questions (FAQ)

**Quick answers to common questions about the Wangkanai Domain library**

---

## Getting Started

### Q: What is the Wangkanai Domain library?

**A**: The Wangkanai Domain library is a high-performance implementation of Domain-Driven Design (DDD) patterns for .NET
applications. It provides optimized base classes for entities, value objects, and aggregate roots, along with comprehensive audit
trail capabilities and seamless Entity Framework Core integration.

**Key Features**:

- Performance-optimized Entity and ValueObject base classes
- Comprehensive audit trail system with automatic change tracking
- Generic programming with strongly-typed entity identifiers
- Clean Architecture and DDD pattern support
- EF Core integration with convention-based configuration

**References**: [Architecture Overview](architectural_analysis_report.md) | [Getting Started Guide](getting-started-guide.md)

---

### Q: How does this library differ from other DDD frameworks?

**A**: The Wangkanai Domain library focuses on performance optimization while maintaining clean DDD patterns:

**Performance Advantages**:

- **Entity Pattern**: 10% performance improvement through intelligent type caching (`Entity.cs:77-183`)
- **Value Objects**: 500-1000x performance improvement with compiled property accessors (`ValueObject.cs:151-307`)
- **Audit System**: Optimized JSON serialization with span-based operations for minimal allocations

**Architecture Benefits**:

- Strongly-typed entity identifiers with `IEquatable<T>` and `IComparable<T>` constraints
- Automatic audit trail with user attribution and change tracking
- Convention-based EF Core configuration reducing boilerplate code

**References**: [Performance Analysis](performance-analysis-report.md) | [Design Decisions](design-decisions.md)

---

## Implementation Questions

### Q: How do I create a simple entity?

**A**: Inherit from `Entity<T>` where `T` is your identifier type:

```csharp
public class User : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Entity<T> provides Id property, equality operations, and performance optimizations
}

// Usage
var user = new User 
{
    Id = Guid.NewGuid(),
    Name = "John Doe",
    Email = "john@example.com"
};

Console.WriteLine(user.IsTransient()); // False - has valid ID
```

**Performance Note**: Entity uses cached type resolution for 10% performance improvement in equality operations.

**References
**: [Entity Guide](entity-guide.md) | [API Reference](api-reference.md#entity) | [Entity.cs:19-222](../src/Domain/Entity.cs)

---

### Q: How do I implement a value object?

**A**: Inherit from `ValueObject` and implement `GetEqualityComponents()`:

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// Usage
var price1 = new Money(100m, "USD");
var price2 = new Money(100m, "USD");
Console.WriteLine(price1 == price2); // True - value equality
Console.WriteLine(price1.GetCacheKey()); // "100|'USD'" - for caching
```

**Performance Note**: ValueObject automatically uses compiled accessors for 500-1000x performance improvement when possible.

**References**: [Value Object Guide](value-object-guide.md) | [ValueObject.cs:22-308](../src/Domain/ValueObject.cs)

---

### Q: How do I enable audit trails for my entities?

**A**: Inherit from `AuditableEntity<T>` instead of `Entity<T>`:

```csharp
public class Product : AuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // Inherits Created and Updated timestamp properties
}

// EF Core context with audit support
public class ShopContext : AuditDbContext
{
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatic audit configuration through conventions
        base.OnModelCreating(modelBuilder);
    }
}
```

**Features**:

- Automatic `Created` and `Updated` timestamp management
- Full audit trail with `Audit<TKey, TUserType, TUserKey>` entity
- User attribution support with ASP.NET Core Identity integration
- Change tracking with JSON serialization optimization

**References
**: [Audit Guide](audit-guide.md) | [AuditableEntity.cs:7-37](../src/Audit/AuditableEntity.cs) | [Audit.cs:9-215](../src/Audit/Audit.cs)

---

### Q: How do I implement aggregate roots?

**A**: Use the `IAggregateRoot<T>` interface to mark your root entities:

```csharp
public class Order : Entity<Guid>, IAggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    
    public string CustomerEmail { get; private set; }
    public DateTime OrderDate { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    
    public Order(string customerEmail)
    {
        Id = Guid.NewGuid();
        CustomerEmail = customerEmail;
        OrderDate = DateTime.UtcNow;
    }
    
    public void AddItem(string productName, decimal price, int quantity)
    {
        _items.Add(new OrderItem(productName, price, quantity));
    }
    
    public decimal GetTotal() => _items.Sum(i => i.Price * i.Quantity);
}

public class OrderItem : ValueObject
{
    public string ProductName { get; }
    public decimal Price { get; }
    public int Quantity { get; }
    
    public OrderItem(string productName, decimal price, int quantity)
    {
        ProductName = productName;
        Price = price;
        Quantity = quantity;
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return ProductName;
        yield return Price;
        yield return Quantity;
    }
}
```

**Best Practices**:

- Keep aggregate boundaries small and focused
- Access child entities only through the aggregate root
- Use value objects for concepts without identity
- Implement domain events for cross-aggregate communication

**References
**: [Aggregate Root Guide](aggregate-root-guide.md) | [IAggregateRoot.cs:10-19](../src/Domain/Interfaces/IAggregateRoot.cs)

---

## Performance Questions

### Q: How significant are the performance improvements?

**A**: The library provides measurable performance improvements across all core patterns:

**Entity Performance**:

- **10% improvement** in equality operations through intelligent type caching
- **Thread-safe** caching with concurrent dictionaries and atomic operations
- **Memory-efficient** with LRU eviction preventing unbounded growth

**Value Object Performance**:

- **500-1000x improvement** with compiled property accessors
- **Automatic optimization** detection with graceful fallback to reflection
- **Zero-allocation** equality comparisons for simple value objects

**Audit Performance**:

- **Span-based operations** for minimal memory allocations
- **Direct JSON construction** for change sets ≤3 properties
- **Lazy deserialization** to avoid unnecessary object creation

**Benchmark Results** (from `benchmark/Domain/ValueObjectPerformanceBenchmark.cs`):

```
Method              Mean      Error     StdDev    Ratio  Gen0   Allocated
SimpleEquals        15.2 ns   0.12 ns   0.10 ns   1.00   -      -
BulkEquals          14.8 μs   0.08 μs   0.07 μs   1.00   -      -
SimpleGetHashCode   12.1 ns   0.09 ns   0.08 ns   1.00   -      -
```

**References
**: [Performance Analysis](performance-analysis-report.md) | [Performance Guide](performance-optimization-guide.md) | [Benchmarks](../benchmark/)

---

### Q: When should I disable performance optimizations?

**A**: The library automatically handles optimization decisions, but you can influence behavior:

**Automatic Disabling**:

- Complex property types that can't be compiled (interfaces, custom IEnumerables)
- Types that fail compilation due to reflection restrictions
- Runtime compilation errors (graceful fallback to reflection)

**Manual Considerations**:

- **Testing scenarios**: Use `Entity<T>.ClearTypeCache()` to reset performance statistics
- **Memory-constrained environments**: Type cache is bounded to 1000 entries with LRU eviction
- **Debugging**: Reflection fallback provides full compatibility for debugging tools

**Code Example**:

```csharp
// Check optimization status
var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
Console.WriteLine($"Cache hit ratio: {hitRatio:P2}"); // e.g., "95.50%"

// Clear cache if needed (testing scenarios)
Entity<Guid>.ClearTypeCache();
```

**References
**: [Performance Guide](performance-optimization-guide.md#optimization-control) | [Entity.cs:162-182](../src/Domain/Entity.cs)

---

## Integration Questions

### Q: How do I integrate with ASP.NET Core?

**A**: The library works seamlessly with ASP.NET Core applications:

```csharp
// Program.cs - Service registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// DbContext with audit support
public class ApplicationDbContext : AuditDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Automatic configuration through conventions
        base.OnModelCreating(modelBuilder);
        
        // Custom configurations
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}

// Controller with domain entities
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    
    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price
        };
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        return Ok(new { Id = product.Id, product.Created, product.Updated });
    }
}
```

**Features**:

- Automatic audit trail integration with ASP.NET Core Identity
- Convention-based EF Core configuration
- Built-in support for dependency injection
- Compatible with Entity Framework migrations

**References**: [Integration Guide](integration-guide.md) | [EF Integration](ef-integration-guide.md)

---

### Q: How do I integrate with existing databases?

**A**: The library supports existing database schemas through flexible configuration:

```csharp
public class LegacyDbContext : AuditDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Map to existing table names
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("tbl_Products");
            entity.Property(e => e.Id).HasColumnName("ProductID");
            entity.Property(e => e.Created).HasColumnName("CreatedDate");
            entity.Property(e => e.Updated).HasColumnName("ModifiedDate");
        });
        
        // Disable auditing for specific entities if needed
        modelBuilder.Entity<LegacyEntity>()
            .Property(e => e.Created)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}

// Custom ID types for legacy systems
public class LegacyProduct : AuditableEntity<int>
{
    // Works with existing INT IDENTITY columns
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

**Migration Strategies**:

1. **Gradual Migration**: Start with new entities using the library patterns
2. **Legacy Wrapper**: Wrap existing entities with audit capabilities
3. **Database First**: Use EF Core scaffolding then inherit from library base classes
4. **Custom Configurations**: Override default conventions for existing schemas

**References**: [Migration Guide](migration-guide.md) | [Integration Guide](integration-guide.md#legacy-integration)

---

## Error Handling & Troubleshooting

### Q: How do I handle domain validation and business rules?

**A**: Use the Result pattern and domain exceptions for robust error handling:

```csharp
// Result pattern for operations that might fail
public class Account : Entity<Guid>
{
    public decimal Balance { get; private set; }
    public string AccountNumber { get; private set; }
    
    public Result<decimal> Withdraw(decimal amount)
    {
        if (amount <= 0)
            return Result.Failure<decimal>(Error.Create("INVALID_AMOUNT", "Amount must be positive"));
        
        if (Balance < amount)
            return Result.Failure<decimal>(Error.Create("INSUFFICIENT_FUNDS", "Insufficient balance"));
        
        Balance -= amount;
        return Result.Success(Balance);
    }
    
    // Domain exceptions for invariant violations
    public void ChangeAccountNumber(string newNumber)
    {
        if (string.IsNullOrWhiteSpace(newNumber))
            throw new DomainRuleViolationException("Account number cannot be empty");
        
        if (newNumber.Length != 10)
            throw new DomainRuleViolationException("Account number must be 10 digits");
        
        AccountNumber = newNumber;
    }
}

// Usage in services
public class AccountService
{
    public async Task<IActionResult> ProcessWithdrawal(Guid accountId, decimal amount)
    {
        var account = await _repository.GetByIdAsync(accountId);
        if (account == null)
            return NotFound();
        
        var result = account.Withdraw(amount);
        if (result.IsFailure)
            return BadRequest(result.Error);
        
        await _repository.UpdateAsync(account);
        return Ok(new { NewBalance = result.Value });
    }
}
```

**Error Handling Patterns**:

- **Result Pattern**: For operations that might fail as part of normal business logic
- **Domain Exceptions**: For invariant violations that should never occur
- **Validation**: Use value objects to encapsulate validation rules
- **Aggregate Boundaries**: Keep validation and business rules within aggregate roots

**References
**: [Result.cs:6-105](../src/Domain/Primitives/Result.cs) | [Domain Exceptions](../src/Domain/Exceptions/) | [Design Decisions](design-decisions.md#error-handling)

---

### Q: What are common performance bottlenecks and how do I avoid them?

**A**: The library is designed to avoid common DDD performance issues:

**Common Bottlenecks & Solutions**:

1. **Entity Equality Checks**:
   - ❌ **Problem**: Reflection overhead in type checking
   - ✅ **Solution**: Built-in type caching provides 10% improvement
   - **Code**: Automatic in `Entity<T>.Equals()` method

2. **Value Object Comparisons**:
   - ❌ **Problem**: Reflection-based property access
   - ✅ **Solution**: Compiled accessors provide 500-1000x improvement
   - **Code**: Automatic in `ValueObject.GetEqualityComponentsOptimized()`

3. **Audit Trail Storage**:
   - ❌ **Problem**: Dictionary serialization overhead
   - ✅ **Solution**: Direct JSON construction for small change sets
   - **Code**: `Audit<T>.SetValuesFromSpan()` uses span-based operations

4. **Database Context Performance**:
   - ❌ **Problem**: N+1 queries and unnecessary change tracking
   - ✅ **Solution**: Use proper EF Core patterns with the library
   ```csharp
   // Efficient querying
   var products = await context.Products
       .AsNoTracking() // For read-only scenarios
       .Where(p => p.Price > 100)
       .ToListAsync();
   
   // Bulk operations
   context.Products.AddRange(newProducts);
   await context.SaveChangesAsync();
   ```

**Monitoring Performance**:

```csharp
// Check entity cache performance
var stats = Entity<Guid>.GetPerformanceStats();
_logger.LogInformation("Entity cache hit ratio: {HitRatio:P2}", stats.HitRatio);

// Monitor in production
services.AddSingleton<IHostedService, PerformanceMonitoringService>();
```

**References
**: [Performance Guide](performance-optimization-guide.md) | [Troubleshooting Guide](troubleshooting-guide.md#performance-issues)

---

### Q: How do I debug serialization issues?

**A**: The library provides debugging support for JSON serialization in audit trails:

**Common Serialization Issues**:

1. **Circular References**:
   ```csharp
   public class Order : AuditableEntity<int>
   {
       public List<OrderItem> Items { get; set; } = new();
   }
   
   public class OrderItem : Entity<int>
   {
       // ❌ Avoid circular references
       public Order Order { get; set; } = null!;
       
       // ✅ Use ID reference instead
       public int OrderId { get; set; }
   }
   ```

2. **Complex Type Serialization**:
   ```csharp
   // Configure JSON options for audit serialization
   public class CustomAudit : Audit<int, User, string>
   {
       public override void SetValuesFromJson(string? oldJson, string? newJson)
       {
           var options = new JsonSerializerOptions
           {
               ReferenceHandler = ReferenceHandler.IgnoreCycles,
               WriteIndented = false
           };
           
           // Use custom serialization if needed
           base.SetValuesFromJson(oldJson, newJson);
       }
   }
   ```

3. **Debugging Audit Values**:
   ```csharp
   // Access individual values without full deserialization
   var oldName = audit.GetOldValue("Name");
   var newName = audit.GetNewValue("Name");
   
   // Check what changed
   Console.WriteLine($"Changed columns: {string.Join(", ", audit.ChangedColumns)}");
   
   // View raw JSON for debugging
   Console.WriteLine($"Old values: {audit.OldValuesJson}");
   Console.WriteLine($"New values: {audit.NewValuesJson}");
   ```

**References
**: [Audit Guide](audit-guide.md#serialization) | [Troubleshooting Guide](troubleshooting-guide.md#serialization-issues)

---

## Advanced Usage

### Q: How do I implement domain events?

**A**: Use the domain message interfaces for event-driven architecture:

```csharp
// Define domain events
public class OrderCreatedEvent : IDomainMessage
{
    public Guid OrderId { get; }
    public string CustomerEmail { get; }
    public decimal TotalAmount { get; }
    public DateTime CreatedAt { get; }
    
    public OrderCreatedEvent(Guid orderId, string customerEmail, decimal totalAmount)
    {
        OrderId = orderId;
        CustomerEmail = customerEmail;
        TotalAmount = totalAmount;
        CreatedAt = DateTime.UtcNow;
    }
}

// Implement event handlers
public class OrderCreatedHandler : IDomainHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderCreatedHandler> _logger;
    
    public async Task Handle(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order created event for order {OrderId}", domainEvent.OrderId);
        
        await _emailService.SendOrderConfirmationAsync(
            domainEvent.CustomerEmail, 
            domainEvent.OrderId,
            domainEvent.TotalAmount);
    }
}

// Raise events from aggregates
public class Order : Entity<Guid>, IAggregateRoot<Guid>
{
    private readonly List<IDomainMessage> _domainEvents = new();
    
    public IReadOnlyList<IDomainMessage> DomainEvents => _domainEvents.AsReadOnly();
    
    public void CompleteOrder()
    {
        // Business logic
        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        
        // Raise domain event
        _domainEvents.Add(new OrderCompletedEvent(Id, CustomerEmail, GetTotal()));
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**Integration with MediatR**:

```csharp
// Extension method to publish domain events
public static async Task DispatchDomainEventsAsync(this DbContext context, IMediator mediator)
{
    var entitiesWithEvents = context.ChangeTracker.Entries<IAggregateRoot>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();
    
    var domainEvents = entitiesWithEvents
        .SelectMany(e => e.DomainEvents)
        .ToList();
    
    entitiesWithEvents.ForEach(e => e.ClearDomainEvents());
    
    foreach (var domainEvent in domainEvents)
        await mediator.Publish(domainEvent);
}
```

**References
**: [Domain Events Guide](domain-events-guide.md) | [Message Interfaces](../src/Domain/Messages/) | [Integration Guide](integration-guide.md#domain-events)

---

### Q: How do I implement custom ID types?

**A**: The library supports any ID type that implements `IEquatable<T>` and `IComparable<T>`:

```csharp
// Custom strongly-typed ID
public readonly struct ProductId : IEquatable<ProductId>, IComparable<ProductId>
{
    private readonly Guid _value;
    
    public ProductId(Guid value)
    {
        _value = value;
    }
    
    public static ProductId NewId() => new(Guid.NewGuid());
    public static ProductId Empty => new(Guid.Empty);
    
    public bool Equals(ProductId other) => _value.Equals(other._value);
    public int CompareTo(ProductId other) => _value.CompareTo(other._value);
    public override bool Equals(object? obj) => obj is ProductId other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
    public override string ToString() => _value.ToString();
    
    public static implicit operator Guid(ProductId id) => id._value;
    public static implicit operator ProductId(Guid guid) => new(guid);
}

// Use with entities
public class Product : Entity<ProductId>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // Constructor
    public Product(string name, decimal price)
    {
        Id = ProductId.NewId(); // Strongly-typed ID generation
        Name = name;
        Price = price;
    }
}

// EF Core configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Id)
            .HasConversion(
                id => (Guid)id,           // To database
                guid => (ProductId)guid); // From database
    }
}
```

**Benefits of Strongly-Typed IDs**:

- **Type Safety**: Prevents mixing up different entity IDs
- **Self-Documenting**: Clear intent in method signatures
- **Validation**: Can include validation logic in ID types
- **Performance**: Same performance as primitive types with optimizations

**References**: [Entity Guide](entity-guide.md#custom-id-types) | [Integration Guide](integration-guide.md#strongly-typed-ids)

---

## Migration & Compatibility

### Q: How do I migrate from Entity Framework without domain patterns?

**A**: Follow a gradual migration approach to minimize disruption:

**Phase 1: Add Library Dependencies**

```csharp
// Install NuGet packages
// Wangkanai.Domain
// Wangkanai.Audit  
// Wangkanai.EntityFramework

// Update existing DbContext
public class ApplicationDbContext : AuditDbContext // Change base class
{
    // Existing DbSets remain unchanged
    public DbSet<ExistingEntity> ExistingEntities { get; set; }
}
```

**Phase 2: Migrate Entities Gradually**

```csharp
// Before: Plain POCO
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

// After: Domain entity with audit support
public class Product : AuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    
    // Created and Updated properties inherited from AuditableEntity
    // Id property inherited from Entity<int>
}

// EF Core migration to rename columns if needed
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<Product>(entity =>
    {
        // Map existing columns
        entity.Property(e => e.Created).HasColumnName("CreatedDate");
        entity.Property(e => e.Updated).HasColumnName("ModifiedDate");
    });
}
```

**Phase 3: Add Value Objects and Business Logic**

```csharp
// Extract value objects from primitives
public class Product : AuditableEntity<int>
{
    // Before: primitive properties
    // public string Name { get; set; }
    // public decimal Price { get; set; }
    // public string Currency { get; set; }
    
    // After: value objects and business logic
    public ProductName Name { get; private set; }
    public Money Price { get; private set; }
    
    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new DomainRuleViolationException("Price must be positive");
        
        Price = newPrice;
    }
}

public class ProductName : ValueObject
{
    public string Value { get; }
    
    public ProductName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Product name is required");
        
        Value = value.Trim();
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public static implicit operator string(ProductName name) => name.Value;
    public static implicit operator ProductName(string value) => new(value);
}
```

**References**: [Migration Guide](migration-guide.md) | [Integration Guide](integration-guide.md#migration-strategies)

---

### Q: Is the library compatible with .NET Standard and older frameworks?

**A**: The library targets .NET 9.0 but follows compatibility best practices:

**Current Support**:

- ✅ **.NET 9.0**: Full support with all performance optimizations
- ⚠️ **.NET 8.0**: Most features compatible (requires testing)
- ❌ **.NET Standard 2.1**: Not directly supported due to performance optimizations using .NET 9 features

**Performance Features by Framework**:

- **Entity Caching**: Uses `ConcurrentDictionary` and `Interlocked` (available in .NET Standard 2.1+)
- **Value Object Compilation**: Uses `Expression.Compile()` (available in .NET Standard 2.1+)
- **Span-based Operations**: Uses `ReadOnlySpan<T>` (requires .NET Core 2.1+)

**Migration Path for Older Frameworks**:

1. **Upgrade to .NET 8**: Recommended path for most applications
2. **Custom Implementation**: Adapt patterns without performance optimizations
3. **Gradual Migration**: Use library patterns as guidance for manual implementation

**References**: [Migration Guide](migration-guide.md#framework-compatibility) | [Tech Stack](tech_stack.md)

---

## Community & Support

### Q: Where can I find examples and sample applications?

**A**: Examples are available in multiple locations:

**In-Repository Examples**:

- **[Benchmark Tests](../benchmark/)**: Performance testing with realistic scenarios
- **[Unit Tests](../tests/)**: Usage patterns and edge cases
- **Code Snippets**: Throughout documentation with file references

**Usage Patterns Documentation**:

- **[Basic Patterns](usage-patterns.md#basic-patterns)**: Getting started examples
- **[Advanced Scenarios](usage-patterns.md#advanced-scenarios)**: Complex domain modeling
- **[Integration Examples](integration-guide.md#examples)**: ASP.NET Core and EF Core integration

**Sample Applications** (create separate repository):

- E-commerce domain with orders, products, and customers
- Financial domain with accounts, transactions, and audit trails
- Content management with articles, authors, and publishing workflow

**References
**: [Usage Patterns](usage-patterns.md) | [Integration Guide](integration-guide.md) | [Getting Started](getting-started-guide.md)

---

### Q: How do I contribute to the library?

**A**: Contributions are welcome following the project standards:

**Development Setup**:

```bash
# Clone repository
git clone https://github.com/wangkanai/domain.git
cd domain

# Restore packages
dotnet restore

# Run tests
dotnet test

# Run benchmarks
cd benchmark/Domain
dotnet run -c Release
```

**Contribution Guidelines**:

- **Code Quality**: Follow existing patterns and performance considerations
- **Testing**: Add unit tests for new features with benchmark tests for performance-critical code
- **Documentation**: Update relevant documentation files and cross-references
- **Compatibility**: Maintain backward compatibility unless major version change

**Areas for Contribution**:

- **Performance Optimizations**: Additional caching strategies or compilation techniques
- **EF Core Features**: New value generators, conventions, or configurations
- **Domain Patterns**: Additional DDD patterns or architectural components
- **Documentation**: Examples, tutorials, or improved explanations

**References**: [Design Decisions](design-decisions.md) | [Quality Assessment](quality_assessment_report.md)

---

## Quick Reference

### Common Use Cases

| Scenario       | Pattern              | Implementation                         | Performance                  |
|----------------|----------------------|----------------------------------------|------------------------------|
| Simple Entity  | `Entity<T>`          | `class User : Entity<Guid>`            | 10% improvement              |
| Value Object   | `ValueObject`        | Override `GetEqualityComponents()`     | 500-1000x improvement        |
| Audit Trail    | `AuditableEntity<T>` | `class Product : AuditableEntity<int>` | Optimized JSON serialization |
| Business Rules | Result Pattern       | `Result<T> Withdraw(decimal amount)`   | Zero allocation for success  |
| Domain Events  | `IDomainMessage`     | Implement handler interface            | Event-driven architecture    |

### File Quick Reference

| Pattern       | Source File                                                             | Key Features                               |
|---------------|-------------------------------------------------------------------------|--------------------------------------------|
| Entity        | [`Entity.cs:19-222`](../src/Domain/Entity.cs)                           | Type caching, equality optimization        |
| ValueObject   | [`ValueObject.cs:22-308`](../src/Domain/ValueObject.cs)                 | Compiled accessors, automatic optimization |
| Audit         | [`Audit.cs:9-215`](../src/Audit/Audit.cs)                               | Span operations, JSON optimization         |
| Result        | [`Result.cs:6-105`](../src/Domain/Primitives/Result.cs)                 | Railway-oriented programming               |
| AggregateRoot | [`IAggregateRoot.cs:10-19`](../src/Domain/Interfaces/IAggregateRoot.cs) | Domain boundary marker                     |

**Related Documentation
**: [Knowledge Base Index](knowledge-base-index.md) | [API Reference](api-reference.md) | [Troubleshooting Guide](troubleshooting-guide.md)

---

*Last Updated: 2025-01-15 | Next Review: Q2 2025*
