# Integration Guide

This guide demonstrates how to integrate and use the three modules of the Wangkanai Domain library together, showing cross-module
relationships, best practices, and complete implementation examples.

## Table of Contents

- [Module Overview](#module-overview)
- [Quick Start Integration](#quick-start-integration)
- [Cross-Module Relationships](#cross-module-relationships)
- [Complete Implementation Examples](#complete-implementation-examples)
- [Configuration and Setup](#configuration-and-setup)
- [Testing Strategies](#testing-strategies)
- [Migration Patterns](#migration-patterns)
- [Troubleshooting](#troubleshooting)

## Module Overview

### Integration Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
├─────────────────────────────────────────────────────────────┤
│  Controllers  │  Services   │  Commands  │  Queries        │
├─────────────────────────────────────────────────────────────┤
│                    Domain Layer                             │
│  ┌─────────────┬─────────────┬─────────────────────────────┐ │
│  │   Domain    │    Audit    │      EntityFramework       │ │
│  │   Module    │   Module    │         Module             │ │
│  │             │             │                            │ │
│  │ • Entities  │ • Auditable │ • DbContext                │ │
│  │ • Values    │   Entities  │ • Extensions               │ │
│  │ • Events    │ • Audit     │ • Value Generators         │ │
│  │ • Results   │   Store     │ • Configurations           │ │
│  └─────────────┴─────────────┴─────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                       │
│  Database  │  External APIs  │  File System  │  Caching   │
└─────────────────────────────────────────────────────────────┘
```

### Module Dependencies

| Module              | Depends On    | Provides                                      |
|---------------------|---------------|-----------------------------------------------|
| **Domain**          | None (core)   | Entities, Value Objects, Events, Results      |
| **Audit**           | Domain        | Audit trails, Auditable entities, Soft delete |
| **EntityFramework** | Domain, Audit | DbContext, Extensions, Value generators       |

## Quick Start Integration

### 1. Package Installation

```xml
<!-- Core domain functionality -->
<PackageReference Include="Wangkanai.Domain" Version="9.0.0" />

<!-- Audit capabilities (includes Domain) -->
<PackageReference Include="Wangkanai.Audit" Version="9.0.0" />

<!-- EF Core integration (includes Domain and Audit) -->
<PackageReference Include="Wangkanai.EntityFramework" Version="9.0.0" />

<!-- Additional EF Core packages -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0" />
```

### 2. Basic Domain Model Setup

```csharp
// Domain/Entities/Product.cs
using Wangkanai.Domain;
using Wangkanai.Audit;

public class Product : UserAuditableEntity<Guid, string>, IAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public ProductCategory Category { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;

    private Product() { } // EF Constructor

    public Product(string name, string description, Money price, ProductCategory category)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? string.Empty;
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    public Result UpdatePrice(Money newPrice, string updatedBy)
    {
        if (newPrice.Amount <= 0)
            return Result.Failure(ProductErrors.InvalidPrice);

        Price = newPrice;
        Updated = DateTime.UtcNow;
        UpdatedBy = updatedBy;

        return Result.Success();
    }

    public void Deactivate(string deactivatedBy)
    {
        IsActive = false;
        Updated = DateTime.UtcNow;
        UpdatedBy = deactivatedBy;
    }
}

// Domain/ValueObjects/Money.cs
public class Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required");
        
        return new Money(amount, currency);
    }
}

public class ProductCategory : ValueObject
{
    public string Name { get; init; }
    public string Code { get; init; }

    public ProductCategory(string name, string code)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }
}
```

### 3. DbContext Setup

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using Wangkanai.EntityFramework;
using Wangkanai.Audit;

public class ApplicationDbContext : AuditDbContext
{
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Audit<Guid, ApplicationUser, string>> AuditTrails { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }

    // Automatic audit timestamp management
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditableEntries = ChangeTracker.Entries<IAuditable>();
        
        foreach (var entry in auditableEntries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = DateTime.UtcNow;
                    if (entry.Entity is IUserAuditable<string> userAuditable)
                        userAuditable.CreatedBy = GetCurrentUserId();
                    break;
                
                case EntityState.Modified:
                    entry.Entity.Updated = DateTime.UtcNow;
                    if (entry.Entity is IUserAuditable<string> userAuditable2)
                        userAuditable2.UpdatedBy = GetCurrentUserId();
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private string GetCurrentUserId()
    {
        // Implementation depends on your authentication system
        return "system"; // Placeholder
    }
}
```

### 4. Entity Configuration

```csharp
// Infrastructure/Data/Configurations/ProductConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wangkanai.EntityFramework;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(p => p.Description)
               .HasMaxLength(1000);

        builder.Property(p => p.IsActive)
               .HasDefaultValue(true);

        // Value objects
        builder.OwnsOne(p => p.Price, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("Price")
                 .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3);
        });

        builder.OwnsOne(p => p.Category, category =>
        {
            category.Property(c => c.Name)
                   .HasColumnName("CategoryName")
                   .HasMaxLength(100);
            
            category.Property(c => c.Code)
                   .HasColumnName("CategoryCode")
                   .HasMaxLength(20);
        });

        // Audit properties with value generators
        builder.Property(p => p.Created)
               .HasValueGenerator<DateTimeNowGenerator>();

        // Indexes
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => new { p.IsActive, p.Created });

        // Soft delete global filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

### 5. Startup Configuration

```csharp
// Program.cs
using Microsoft.EntityFrameworkCore;
using Wangkanai.EntityFramework;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories and services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IAuditStore<Guid, ApplicationUser, string>, AuditStore>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

// Ensure database is migrated
await app.MigrateDatabaseAsync<ApplicationDbContext>();

app.Run();
```

## Cross-Module Relationships

### Domain → Audit Integration

```csharp
// Product entity using audit capabilities
public class Product : UserAuditableEntity<Guid, string>, IAggregateRoot<Guid>
{
    // Inherits from UserAuditableEntity<T, TUserKey>:
    // - Id (from Entity<T>)
    // - Created, Updated (from IAuditable)
    // - CreatedBy, UpdatedBy (from IUserAuditable<TUserKey>)
    
    public string Name { get; private set; }
    
    public void UpdateName(string newName, string updatedBy)
    {
        Name = newName;
        Updated = DateTime.UtcNow;    // From IAuditable
        UpdatedBy = updatedBy;        // From IUserAuditable<string>
    }
}
```

### Audit → EntityFramework Integration

```csharp
// Automatic audit trail creation
public class AuditTrailService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;

    public async Task<Result> UpdateProductWithAuditAsync(
        Guid productId, 
        UpdateProductRequest request,
        string userId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return Result.Failure(ProductErrors.NotFound);

        // Capture old values for audit
        var oldValues = new
        {
            Name = product.Name,
            Price = product.Price.Amount,
            Category = product.Category.Name
        };

        // Update product
        var updateResult = product.UpdatePrice(
            new Money(request.Price, request.Currency), 
            userId);
        
        if (updateResult.IsFailure)
            return updateResult;

        // Save changes (triggers automatic audit timestamp update)
        await _context.SaveChangesAsync();

        // Create detailed audit trail
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Update,
            EntityName = nameof(Product),
            PrimaryKey = product.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = userId,
            ChangedColumns = ["Price"]
        };

        // Use high-performance span operations
        ReadOnlySpan<string> columns = ["Price"];
        ReadOnlySpan<object> oldAuditValues = [oldValues.Price];
        ReadOnlySpan<object> newAuditValues = [product.Price.Amount];

        audit.SetValuesFromSpan(columns, oldAuditValues, newAuditValues);

        await _auditStore.CreateAsync(audit, CancellationToken.None);

        return Result.Success();
    }
}
```

### Domain Events with Audit Integration

```csharp
// Domain events with audit correlation
public class ProductPriceChangedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
    public string Currency { get; }
    public string ChangedBy { get; }

    public ProductPriceChangedEvent(Guid productId, decimal oldPrice, decimal newPrice, 
        string currency, string changedBy)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        Currency = currency;
        ChangedBy = changedBy;
        TimeStamp = DateTimeOffset.UtcNow;
    }
}

// Event handler that creates audit records
public class ProductPriceChangedEventHandler : IDomainEventHandler<ProductPriceChangedEvent>
{
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;

    public async Task Handle(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Update,
            EntityName = "Product",
            PrimaryKey = domainEvent.ProductId.ToString(),
            Timestamp = domainEvent.TimeStamp.DateTime,
            UserId = domainEvent.ChangedBy,
            ChangedColumns = ["Price"]
        };

        // Create audit record with event data
        audit.SetValuesFromJson(
            JsonSerializer.Serialize(new { Price = domainEvent.OldPrice, Currency = domainEvent.Currency }),
            JsonSerializer.Serialize(new { Price = domainEvent.NewPrice, Currency = domainEvent.Currency })
        );

        await _auditStore.CreateAsync(audit, cancellationToken);
    }
}
```

## Complete Implementation Examples

### E-Commerce Order Management System

```csharp
// Domain Models
public class Order : UserAuditableEntity<Guid, string>, IAggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();
    
    public string OrderNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public Address ShippingAddress { get; private set; } = null!;
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // EF Constructor

    public Order(string orderNumber, Guid customerId, Address shippingAddress, string createdBy)
    {
        Id = Guid.NewGuid();
        OrderNumber = orderNumber;
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero("USD");
        ShippingAddress = shippingAddress;
        CreatedBy = createdBy;
    }

    public Result<OrderItem> AddItem(Product product, int quantity, string addedBy)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure<OrderItem>(OrderErrors.CannotModifyProcessedOrder);

        if (quantity <= 0)
            return Result.Failure<OrderItem>(OrderErrors.InvalidQuantity);

        var item = new OrderItem(product.Id, product.Name, quantity, product.Price);
        _items.Add(item);
        
        RecalculateTotal();
        Updated = DateTime.UtcNow;
        UpdatedBy = addedBy;

        return Result.Success(item);
    }

    public Result ProcessOrder(string processedBy)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.InvalidStatusTransition);

        if (!_items.Any())
            return Result.Failure(OrderErrors.EmptyOrder);

        Status = OrderStatus.Processing;
        Updated = DateTime.UtcNow;
        UpdatedBy = processedBy;

        return Result.Success();
    }

    private void RecalculateTotal()
    {
        var total = _items.Aggregate(0m, (sum, item) => sum + (item.UnitPrice.Amount * item.Quantity));
        TotalAmount = new Money(total, "USD");
    }
}

public class OrderItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public Money LineTotal => new(UnitPrice.Amount * Quantity, UnitPrice.Currency);

    private OrderItem() { } // EF Constructor

    public OrderItem(Guid productId, string productName, int quantity, Money unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

// Value Objects
public class Address : ValueObject
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string PostalCode { get; init; }
    public string Country { get; init; }

    public Address(string street, string city, string state, string postalCode, string country)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        City = city ?? throw new ArgumentNullException(nameof(city));
        State = state ?? throw new ArgumentNullException(nameof(state));
        PostalCode = postalCode ?? throw new ArgumentNullException(nameof(postalCode));
        Country = country ?? throw new ArgumentNullException(nameof(country));
    }
}

public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }

// Repository Implementation
public interface IOrderRepository : IAsyncRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IList<Order>> GetOrdersByCustomerAsync(Guid customerId);
    Task<IList<Order>> GetOrdersByStatusAsync(OrderStatus status);
}

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IUnitOfWorkAsync UnitOfWork => _context;

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IList<Order>> GetOrdersByCustomerAsync(Guid customerId)
    {
        return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.Created)
            .ToListAsync();
    }

    public async Task<IList<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        return await _context.Orders
            .Where(o => o.Status == status)
            .Include(o => o.Items)
            .ToListAsync();
    }

    public async Task<Order> AddAsync(Order order)
    {
        var entry = await _context.Orders.AddAsync(order);
        return entry.Entity;
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        return await Task.FromResult(order);
    }

    public async Task<Order> DeleteAsync(Order order)
    {
        _context.Orders.Remove(order);
        return await Task.FromResult(order);
    }

    public async Task<Order> AttachAsync(Order order)
    {
        _context.Orders.Attach(order);
        return await Task.FromResult(order);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}

// Service Layer
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;
    private readonly ICurrentUserService _currentUser;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IAuditStore<Guid, ApplicationUser, string> auditStore,
        ICurrentUserService currentUser)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _auditStore = auditStore;
        _currentUser = currentUser;
    }

    public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Create order
        var order = new Order(
            GenerateOrderNumber(),
            command.CustomerId,
            command.ShippingAddress,
            _currentUser.UserId
        );

        // Add items
        foreach (var itemRequest in command.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId);
            if (product == null)
                return Result.Failure<Order>(ProductErrors.NotFound);

            var addResult = order.AddItem(product, itemRequest.Quantity, _currentUser.UserId);
            if (addResult.IsFailure)
                return Result.Failure<Order>(addResult.Error);
        }

        // Save order
        await _orderRepository.AddAsync(order);
        await _orderRepository.UnitOfWork.SaveChangesAsync();

        // Create audit trail for order creation
        await CreateOrderAuditAsync(order, AuditTrailType.Create);

        return Result.Success(order);
    }

    public async Task<Result> AddItemToOrderAsync(Guid orderId, AddItemToOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            return Result.Failure(OrderErrors.NotFound);

        var product = await _productRepository.GetByIdAsync(command.ProductId);
        if (product == null)
            return Result.Failure(ProductErrors.NotFound);

        // Capture old total for audit
        var oldTotal = order.TotalAmount.Amount;
        var oldItemCount = order.Items.Count;

        var addResult = order.AddItem(product, command.Quantity, _currentUser.UserId);
        if (addResult.IsFailure)
            return addResult;

        await _orderRepository.UpdateAsync(order);
        await _orderRepository.UnitOfWork.SaveChangesAsync();

        // Create detailed audit trail
        await CreateItemAddedAuditAsync(order, addResult.Value, oldTotal, oldItemCount);

        return Result.Success();
    }

    private async Task CreateOrderAuditAsync(Order order, AuditTrailType trailType)
    {
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = trailType,
            EntityName = nameof(Order),
            PrimaryKey = order.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            ChangedColumns = ["OrderNumber", "CustomerId", "Status", "TotalAmount", "ShippingAddress"]
        };

        var values = new
        {
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount.Amount,
            Currency = order.TotalAmount.Currency,
            ShippingStreet = order.ShippingAddress.Street,
            ShippingCity = order.ShippingAddress.City
        };

        if (trailType == AuditTrailType.Create)
        {
            audit.NewValuesJson = JsonSerializer.Serialize(values);
        }

        await _auditStore.CreateAsync(audit, CancellationToken.None);
    }

    private async Task CreateItemAddedAuditAsync(Order order, OrderItem addedItem, 
        decimal oldTotal, int oldItemCount)
    {
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Update,
            EntityName = nameof(Order),
            PrimaryKey = order.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            ChangedColumns = ["Items", "TotalAmount"]
        };

        // Use span operations for small changes
        ReadOnlySpan<string> columns = ["TotalAmount", "ItemCount"];
        ReadOnlySpan<object> oldValues = [oldTotal, oldItemCount];
        ReadOnlySpan<object> newValues = [order.TotalAmount.Amount, order.Items.Count];

        audit.SetValuesFromSpan(columns, oldValues, newValues);

        await _auditStore.CreateAsync(audit, CancellationToken.None);
    }

    private string GenerateOrderNumber() => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
}
```

### Entity Framework Configuration

```csharp
// Order Configuration
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(o => o.Status)
               .HasConversion<string>()
               .HasMaxLength(20);

        // Value object configurations
        builder.OwnsOne(o => o.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("TotalAmount")
                 .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3);
        });

        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(50);
            address.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(50);
        });

        // Relationships
        builder.HasMany(typeof(OrderItem), "Items")
               .WithOne()
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => new { o.Status, o.Created });

        // Audit configuration
        builder.Property(o => o.Created)
               .HasValueGenerator<DateTimeNowGenerator>();
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(oi => oi.Quantity)
               .IsRequired();

        // Value object for unit price
        builder.OwnsOne(oi => oi.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("UnitPrice")
                 .HasPrecision(18, 2);
            
            money.Property(m => m.Currency)
                 .HasColumnName("Currency")
                 .HasMaxLength(3);
        });

        // Indexes
        builder.HasIndex(oi => oi.ProductId);
    }
}

// Audit Configuration
public class AuditConfiguration : IEntityTypeConfiguration<Audit<Guid, ApplicationUser, string>>
{
    public void Configure(EntityTypeBuilder<Audit<Guid, ApplicationUser, string>> builder)
    {
        builder.ToTable("AuditTrails");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TrailType)
               .HasConversion<byte>();

        builder.Property(a => a.Timestamp)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.EntityName)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(a => a.PrimaryKey)
               .HasMaxLength(50);

        // JSON storage for optimal performance
        builder.Property(a => a.OldValuesJson)
               .HasColumnType("nvarchar(max)");
               
        builder.Property(a => a.NewValuesJson)
               .HasColumnType("nvarchar(max)");

        // Indexes for efficient querying
        builder.HasIndex(a => new { a.EntityName, a.PrimaryKey });
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.TrailType);

        // User relationship
        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

## Configuration and Setup

### Dependency Injection Configuration

```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Core domain services
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        
        // Repositories
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        
        // Audit services
        services.AddScoped<IAuditStore<Guid, ApplicationUser, string>, AuditStore>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        
        // Domain event handlers
        services.AddScoped<IDomainEventHandler<ProductPriceChangedEvent>, ProductPriceChangedEventHandler>();
        services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
        
        return services;
    }

    public static IServiceCollection AddDatabaseContext(this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            
            // Enable sensitive data logging in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        return services;
    }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddDomainServices();

// Add background services for performance monitoring
builder.Services.AddHostedService<EntityPerformanceMonitoringService>();
builder.Services.AddHostedService<AuditDataMaintenanceService>();

var app = builder.Build();

// Database migration
await app.MigrateDatabaseAsync<ApplicationDbContext>();

app.Run();
```

### Background Services for Maintenance

```csharp
// Services/EntityPerformanceMonitoringService.cs
public class EntityPerformanceMonitoringService : BackgroundService
{
    private readonly ILogger<EntityPerformanceMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            MonitorPerformance();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private void MonitorPerformance()
    {
        var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
        
        _logger.LogInformation("Entity Performance: Hits={Hits}, Misses={Misses}, HitRatio={HitRatio:P2}",
            hits, misses, hitRatio);

        if (hitRatio < 0.8m && hits + misses > 1000)
        {
            _logger.LogWarning("Entity cache hit ratio {HitRatio:P2} is below 80% threshold", hitRatio);
        }

        // Memory monitoring
        var memoryUsage = GC.GetTotalMemory(false);
        if (memoryUsage > 800_000_000) // 800MB warning threshold
        {
            _logger.LogWarning("High memory usage detected: {MemoryUsage:N0} bytes", memoryUsage);
            
            if (memoryUsage > 1_000_000_000) // 1GB critical threshold
            {
                _logger.LogCritical("Critical memory usage, clearing entity cache");
                Entity<Guid>.ClearTypeCache();
                GC.Collect(2, GCCollectionMode.Forced, true);
            }
        }
    }
}

// Services/AuditDataMaintenanceService.cs
public class AuditDataMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AuditDataMaintenanceService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PerformMaintenanceAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(6), stoppingToken); // Run every 6 hours
        }
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Archive old audit records (older than 1 year)
            var cutoffDate = DateTime.UtcNow.AddYears(-1);
            var oldAudits = await context.AuditTrails
                .Where(a => a.Timestamp < cutoffDate)
                .CountAsync(cancellationToken);

            if (oldAudits > 0)
            {
                _logger.LogInformation("Found {Count} old audit records for archival", oldAudits);
                
                // In production, implement proper archival strategy
                // This is a simple example - consider using batch operations
                var auditsToArchive = await context.AuditTrails
                    .Where(a => a.Timestamp < cutoffDate)
                    .Take(1000) // Process in batches
                    .ToListAsync(cancellationToken);

                // Archive logic would go here
                // For now, just log the operation
                _logger.LogInformation("Would archive {Count} audit records", auditsToArchive.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audit maintenance");
        }
    }
}
```

## Testing Strategies

### Unit Testing Domain Logic

```csharp
// Tests/Domain/OrderTests.cs
public class OrderTests
{
    [Fact]
    public void CreateOrder_WithValidData_ShouldSucceed()
    {
        // Arrange
        var orderNumber = "ORD-20241201-TEST001";
        var customerId = Guid.NewGuid();
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        var createdBy = "test-user";

        // Act
        var order = new Order(orderNumber, customerId, address, createdBy);

        // Assert
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(orderNumber, order.OrderNumber);
        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0, order.TotalAmount.Amount);
        Assert.Equal("USD", order.TotalAmount.Currency);
        Assert.Equal(createdBy, order.CreatedBy);
    }

    [Fact]
    public void AddItem_ToValidOrder_ShouldUpdateTotal()
    {
        // Arrange
        var order = CreateTestOrder();
        var product = CreateTestProduct("Test Product", 99.99m);
        var quantity = 2;

        // Act
        var result = order.AddItem(product, quantity, "test-user");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(order.Items);
        Assert.Equal(199.98m, order.TotalAmount.Amount);
    }

    [Fact]
    public void AddItem_ToProcessedOrder_ShouldFail()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ProcessOrder("test-user");
        var product = CreateTestProduct("Test Product", 99.99m);

        // Act
        var result = order.AddItem(product, 1, "test-user");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CannotModifyProcessedOrder, result.Error);
    }

    private Order CreateTestOrder()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        return new Order("TEST-001", Guid.NewGuid(), address, "test-user");
    }

    private Product CreateTestProduct(string name, decimal price)
    {
        var money = new Money(price, "USD");
        var category = new ProductCategory("Test Category", "TEST");
        return new Product(name, "Test Description", money, category);
    }
}
```

### Integration Testing with Database

```csharp
// Tests/Integration/OrderServiceTests.cs
public class OrderServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ApplicationDbContext _context;
    private readonly OrderService _orderService;

    public OrderServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _context = CreateDbContext();
        _orderService = CreateOrderService();
    }

    [Fact]
    public async Task CreateOrder_WithValidCommand_ShouldPersistToDatabase()
    {
        // Arrange
        var product = await CreateAndSaveTestProductAsync();
        var command = new CreateOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            ShippingAddress = new Address("123 Main St", "Springfield", "IL", "62701", "US"),
            Items = new[]
            {
                new CreateOrderItemCommand
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        
        var savedOrder = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == result.Value.Id);
        
        Assert.NotNull(savedOrder);
        Assert.Equal(command.CustomerId, savedOrder.CustomerId);
        Assert.Single(savedOrder.Items);
        Assert.Equal(2, savedOrder.Items.First().Quantity);

        // Verify audit trail was created
        var auditTrail = await _context.AuditTrails
            .FirstOrDefaultAsync(a => a.EntityName == nameof(Order) && 
                                    a.PrimaryKey == savedOrder.Id.ToString());
        
        Assert.NotNull(auditTrail);
        Assert.Equal(AuditTrailType.Create, auditTrail.TrailType);
    }

    [Fact]
    public async Task CreateOrder_PerformanceTest_ShouldCompleteQuickly()
    {
        // Arrange
        var product = await CreateAndSaveTestProductAsync();
        var commands = Enumerable.Range(1, 100)
            .Select(i => new CreateOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new Address($"{i} Main St", "Springfield", "IL", "62701", "US"),
                Items = new[]
                {
                    new CreateOrderItemCommand { ProductId = product.Id, Quantity = 1 }
                }
            })
            .ToList();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = commands.Select(cmd => _orderService.CreateOrderAsync(cmd));
        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete in under 5 seconds
        
        // Verify entity performance
        var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
        Assert.True(hitRatio > 0.8); // Should have good cache performance
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new ApplicationDbContext(options);
    }

    private OrderService CreateOrderService()
    {
        var orderRepository = new OrderRepository(_context);
        var productRepository = new ProductRepository(_context);
        var auditStore = new AuditStore(_context);
        var currentUserService = new TestCurrentUserService();
        
        return new OrderService(orderRepository, productRepository, auditStore, currentUserService);
    }

    private async Task<Product> CreateAndSaveTestProductAsync()
    {
        var money = new Money(99.99m, "USD");
        var category = new ProductCategory("Electronics", "ELEC");
        var product = new Product("Test Product", "Description", money, category);
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        
        return product;
    }
}
```

### Performance Testing

```csharp
// Tests/Performance/DomainPerformanceTests.cs
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class DomainPerformanceBenchmarks
{
    private Order _order;
    private Product _product;
    private Money _money1;
    private Money _money2;

    [GlobalSetup]
    public void Setup()
    {
        var address = new Address("123 Main St", "Springfield", "IL", "62701", "US");
        _order = new Order("TEST-001", Guid.NewGuid(), address, "test-user");
        
        var category = new ProductCategory("Electronics", "ELEC");
        _money1 = new Money(99.99m, "USD");
        _product = new Product("Test Product", "Description", _money1, category);
        
        _money2 = new Money(99.99m, "USD");
    }

    [Benchmark(Description = "Entity equality comparison")]
    public bool EntityEquality()
    {
        return _order.Equals(_order);
    }

    [Benchmark(Description = "ValueObject equality (Money) - should use compiled accessors")]
    public bool ValueObjectEquality()
    {
        return _money1.Equals(_money2);
    }

    [Benchmark(Description = "Order.AddItem operation")]
    public Result<OrderItem> AddItemToOrder()
    {
        var newOrder = new Order("TEST-002", Guid.NewGuid(), 
            new Address("456 Oak St", "Springfield", "IL", "62701", "US"), "test-user");
        
        return newOrder.AddItem(_product, 1, "test-user");
    }

    [Benchmark(Description = "Audit span operation")]
    public void AuditSpanOperation()
    {
        var audit = new Audit<Guid, ApplicationUser, string>();
        ReadOnlySpan<string> columns = ["Name", "Price"];
        ReadOnlySpan<object> oldValues = ["Old Product", 50.00m];
        ReadOnlySpan<object> newValues = ["New Product", 99.99m];
        
        audit.SetValuesFromSpan(columns, oldValues, newValues);
    }
}
```

## Migration Patterns

### From Legacy Systems

```csharp
// Migration/LegacyDataMigrationService.cs
public class LegacyDataMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;

    public async Task MigrateLegacyOrdersAsync(IEnumerable<LegacyOrderDto> legacyOrders)
    {
        var migrationUserId = "system-migration";
        var migrationBatch = Guid.NewGuid();

        foreach (var legacyOrder in legacyOrders)
        {
            try
            {
                // Create new domain order
                var address = new Address(
                    legacyOrder.ShippingStreet ?? string.Empty,
                    legacyOrder.ShippingCity ?? string.Empty,
                    legacyOrder.ShippingState ?? string.Empty,
                    legacyOrder.ShippingPostalCode ?? string.Empty,
                    legacyOrder.ShippingCountry ?? "US"
                );

                var order = new Order(
                    legacyOrder.OrderNumber,
                    legacyOrder.CustomerId,
                    address,
                    migrationUserId
                );

                // Preserve original timestamps
                order.Created = legacyOrder.OrderDate;
                order.Updated = legacyOrder.LastModified;

                // Migrate items
                foreach (var legacyItem in legacyOrder.Items)
                {
                    var money = new Money(legacyItem.UnitPrice, legacyOrder.Currency ?? "USD");
                    var orderItem = new OrderItem(
                        legacyItem.ProductId,
                        legacyItem.ProductName,
                        legacyItem.Quantity,
                        money
                    );
                    
                    // Use reflection to add item directly (bypass business rules)
                    var itemsField = typeof(Order).GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance);
                    var items = (List<OrderItem>)itemsField!.GetValue(order)!;
                    items.Add(orderItem);
                }

                // Set final status based on legacy status
                var statusField = typeof(Order).GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
                var newStatus = MapLegacyStatus(legacyOrder.Status);
                statusField!.SetValue(order, newStatus);

                await _context.Orders.AddAsync(order);

                // Create migration audit record
                await CreateMigrationAuditAsync(order, legacyOrder, migrationBatch);
            }
            catch (Exception ex)
            {
                // Log error but continue with other orders
                Console.WriteLine($"Failed to migrate order {legacyOrder.OrderNumber}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
    }

    private OrderStatus MapLegacyStatus(string legacyStatus)
    {
        return legacyStatus?.ToLower() switch
        {
            "new" or "created" => OrderStatus.Pending,
            "processing" or "in_progress" => OrderStatus.Processing,
            "shipped" or "dispatched" => OrderStatus.Shipped,
            "delivered" or "completed" => OrderStatus.Delivered,
            "cancelled" or "canceled" => OrderStatus.Cancelled,
            _ => OrderStatus.Pending
        };
    }

    private async Task CreateMigrationAuditAsync(Order order, LegacyOrderDto legacyOrder, Guid migrationBatch)
    {
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Create,
            EntityName = nameof(Order),
            PrimaryKey = order.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = "system-migration"
        };

        // Record migration metadata
        var migrationData = new
        {
            MigrationBatch = migrationBatch,
            LegacyOrderId = legacyOrder.LegacyId,
            OriginalOrderDate = legacyOrder.OrderDate,
            MigrationDate = DateTime.UtcNow,
            LegacyStatus = legacyOrder.Status,
            NewStatus = order.Status.ToString()
        };

        audit.NewValuesJson = JsonSerializer.Serialize(migrationData);
        await _auditStore.CreateAsync(audit, CancellationToken.None);
    }
}

// DTOs for legacy data
public class LegacyOrderDto
{
    public int LegacyId { get; set; }
    public string OrderNumber { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public DateTime LastModified { get; set; }
    public string? Currency { get; set; }
    
    // Shipping address fields
    public string? ShippingStreet { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    
    public List<LegacyOrderItemDto> Items { get; set; } = new();
}

public class LegacyOrderItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Poor Entity Cache Performance

**Issue**: Entity cache hit ratio below 80%

**Diagnosis**:

```csharp
public void DiagnoseEntityCacheIssues()
{
    var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
    
    if (hitRatio < 0.8m)
    {
        Console.WriteLine($"Cache Performance Issues Detected:");
        Console.WriteLine($"  Hit Ratio: {hitRatio:P2} (Target: >80%)");
        Console.WriteLine($"  Total Operations: {hits + misses}");
        
        Console.WriteLine("Potential Causes:");
        Console.WriteLine("  • High diversity of EF proxy types");
        Console.WriteLine("  • Frequent cache evictions due to memory pressure");
        Console.WriteLine("  • Mixed inheritance hierarchies");
    }
}
```

**Solutions**:

1. Increase cache size if memory allows
2. Review entity inheritance patterns
3. Monitor memory usage and GC pressure

#### 2. Value Object Performance Issues

**Issue**: Slow value object equality comparisons

**Symptoms**:

```csharp
// Check if value object is using compiled accessors
public void CheckValueObjectOptimization<T>() where T : ValueObject, new()
{
    var valueObject = new T();
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    
    foreach (var prop in properties)
    {
        var isOptimized = IsPropertyOptimized(prop.PropertyType);
        Console.WriteLine($"{prop.Name}: {(isOptimized ? "✅ Optimized" : "⚠️ Reflection")}");
    }
}

private bool IsPropertyOptimized(Type propertyType)
{
    return !propertyType.IsInterface ||
           propertyType == typeof(string) ||
           !typeof(IEnumerable).IsAssignableFrom(propertyType);
}
```

**Solutions**:

1. Simplify value object properties
2. Avoid complex collections in equality components
3. Override `GetEqualityComponents()` for complex scenarios

#### 3. Audit Performance Problems

**Issue**: High memory usage or slow audit operations

**Diagnosis**:

```csharp
public void DiagnoseAuditPerformance()
{
    // Check audit size distribution
    var auditSizes = _context.AuditTrails
        .Take(1000)
        .ToList()
        .Select(a => (a.OldValuesJson?.Length ?? 0) + (a.NewValuesJson?.Length ?? 0))
        .ToList();
    
    if (auditSizes.Any())
    {
        var avgSize = auditSizes.Average();
        var maxSize = auditSizes.Max();
        
        Console.WriteLine($"Audit Size Analysis:");
        Console.WriteLine($"  Average Size: {avgSize:F0} bytes");
        Console.WriteLine($"  Maximum Size: {maxSize} bytes");
        
        if (maxSize > 10000)
        {
            Console.WriteLine("⚠️ Large audit entries detected");
            Console.WriteLine("Consider reducing tracked properties or implementing archival");
        }
    }
}
```

**Solutions**:

1. Use span operations for small changes
2. Implement audit data archiving
3. Reduce tracked properties for large entities
4. Use selective auditing patterns

### Debugging Tools

```csharp
// Debug/PerformanceProfiler.cs
public class DomainPerformanceProfiler
{
    public void ProfileDomainOperations()
    {
        Console.WriteLine("=== Domain Performance Profile ===");
        
        // Entity performance
        ProfileEntityPerformance();
        
        // Value object performance
        ProfileValueObjectPerformance();
        
        // Audit performance
        ProfileAuditPerformance();
        
        // Memory usage
        ProfileMemoryUsage();
    }

    private void ProfileEntityPerformance()
    {
        var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
        
        Console.WriteLine("\n--- Entity Performance ---");
        Console.WriteLine($"Cache Hits: {hits:N0}");
        Console.WriteLine($"Cache Misses: {misses:N0}");
        Console.WriteLine($"Hit Ratio: {hitRatio:P2}");
        Console.WriteLine($"Status: {(hitRatio > 0.8 ? "✅ Good" : "⚠️ Needs attention")}");
    }

    private void ProfileValueObjectPerformance()
    {
        Console.WriteLine("\n--- Value Object Performance ---");
        
        // Test simple value object
        var money1 = new Money(100m, "USD");
        var money2 = new Money(100m, "USD");
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 100000; i++)
        {
            money1.Equals(money2);
        }
        stopwatch.Stop();
        
        var nsPerOp = (double)stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency / 100000;
        Console.WriteLine($"Money Equality: {nsPerOp:F1} ns per operation");
        Console.WriteLine($"Status: {(nsPerOp < 10 ? "✅ Optimized" : "⚠️ Using reflection")}");
    }

    private void ProfileAuditPerformance()
    {
        Console.WriteLine("\n--- Audit Performance ---");
        
        // Test span operation
        var audit = new Audit<Guid, ApplicationUser, string>();
        ReadOnlySpan<string> columns = ["Name", "Price"];
        ReadOnlySpan<object> oldValues = ["Old", 50m];
        ReadOnlySpan<object> newValues = ["New", 100m];
        
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10000; i++)
        {
            audit.SetValuesFromSpan(columns, oldValues, newValues);
        }
        stopwatch.Stop();
        
        var nsPerOp = (double)stopwatch.ElapsedTicks * 1000000000 / Stopwatch.Frequency / 10000;
        Console.WriteLine($"Span Operation: {nsPerOp:F1} ns per operation");
        Console.WriteLine($"Status: {(nsPerOp < 1000 ? "✅ Good" : "⚠️ Slow")}");
    }

    private void ProfileMemoryUsage()
    {
        Console.WriteLine("\n--- Memory Usage ---");
        
        var memoryBefore = GC.GetTotalMemory(true);
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);
        
        // Perform operations
        for (int i = 0; i < 1000; i++)
        {
            var order = new Order($"TEST-{i}", Guid.NewGuid(), 
                new Address("123 Main", "City", "State", "12345", "US"), "test");
            var money = new Money(99.99m, "USD");
        }
        
        var memoryAfter = GC.GetTotalMemory(false);
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);
        var gen2After = GC.CollectionCount(2);
        
        Console.WriteLine($"Memory Usage: {memoryAfter - memoryBefore:N0} bytes");
        Console.WriteLine($"Gen 0 GCs: {gen0After - gen0Before}");
        Console.WriteLine($"Gen 1 GCs: {gen1After - gen1Before}");
        Console.WriteLine($"Gen 2 GCs: {gen2After - gen2Before}");
    }
}
```

This comprehensive integration guide provides everything needed to successfully implement and integrate all three modules of the
Wangkanai Domain library in production applications. The examples demonstrate real-world scenarios while maintaining performance
and following DDD principles.
