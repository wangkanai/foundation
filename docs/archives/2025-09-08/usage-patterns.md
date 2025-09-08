# Usage Patterns and Examples

This document provides practical usage patterns and comprehensive examples for the Wangkanai Domain library, demonstrating how to effectively implement domain-driven design patterns with performance optimization.

## Table of Contents

- [Getting Started](#getting-started)
- [Entity Patterns](#entity-patterns)
- [Value Object Patterns](#value-object-patterns)
- [Audit Trail Patterns](#audit-trail-patterns)
- [Repository Patterns](#repository-patterns)
- [Domain Events](#domain-events)
- [Result Pattern Usage](#result-pattern-usage)
- [EF Core Integration](#ef-core-integration)
- [Performance Optimization Examples](#performance-optimization-examples)
- [Advanced Scenarios](#advanced-scenarios)

## Getting Started

### Project Setup

1. **Install NuGet Packages**:
```xml
<PackageReference Include="Wangkanai.Domain" Version="9.0.0" />
<PackageReference Include="Wangkanai.Audit" Version="9.0.0" />
<PackageReference Include="Wangkanai.EntityFramework" Version="9.0.0" />
```

2. **Add Using Statements**:
```csharp
using Wangkanai.Domain;
using Wangkanai.Audit;
using Wangkanai.EntityFramework;
```

### Basic Domain Model

```csharp
// Aggregate Root
public class Order : AuditableEntity<Guid>, IAggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public Address ShippingAddress { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();

    private Order() { } // EF constructor

    public Order(string orderNumber, Address shippingAddress)
    {
        Id = Guid.NewGuid();
        OrderNumber = orderNumber;
        Status = OrderStatus.Pending;
        ShippingAddress = shippingAddress;
        TotalAmount = Money.Zero("USD");
    }

    public Result AddItem(Product product, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            return Result.Failure(OrderErrors.CannotModifyProcessedOrder);

        var item = new OrderItem(product.Id, quantity, unitPrice);
        Items.Add(item);
        RecalculateTotal();
        
        return Result.Success();
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Aggregate(Money.Zero("USD"), 
            (sum, item) => sum.Add(item.LineTotal));
    }
}

// Entity
public class OrderItem : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }
    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() { } // EF constructor

    public OrderItem(Guid productId, int quantity, Money unitPrice)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}

// Value Objects  
public class Money : ValueObject
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency) => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int factor) => new(Amount * factor, Currency);
}

public class Address : ValueObject
{
    public string Street { get; init; }
    public string City { get; init; }
    public string State { get; init; }
    public string ZipCode { get; init; }
    public string Country { get; init; }

    public Address(string street, string city, string state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }
}

public enum OrderStatus { Pending, Processing, Shipped, Delivered, Cancelled }
```

## Entity Patterns

### 1. Basic Entity Implementation

```csharp
public class Customer : Entity<Guid>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }

    private Customer() { } // EF constructor

    public Customer(string firstName, string lastName, string email)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
        LastName = lastName;
        Email = email;
    }

    public string FullName => $"{FirstName} {LastName}";

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
        
        Email = email;
    }
}
```

### 2. Entity with Different Key Types

```csharp
// Integer key
public class Category : KeyIntEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    public Category(string name, string description)
    {
        Name = name;
        Description = description;
    }
}

// String key  
public class ProductCode : KeyStringEntity
{
    public string Description { get; private set; }

    public ProductCode(string code, string description)
    {
        Id = code;
        Description = description;
    }
}

// Long key
public class SerialNumber : KeyLongEntity
{
    public DateTime IssuedDate { get; private set; }
    public bool IsActive { get; private set; }

    public SerialNumber(long number)
    {
        Id = number;
        IssuedDate = DateTime.UtcNow;
        IsActive = true;
    }
}
```

### 3. Aggregate Root with Business Rules

```csharp
public class BankAccount : AuditableEntity<Guid>, IAggregateRoot<Guid>
{
    private readonly List<Transaction> _transactions = new();

    public string AccountNumber { get; private set; }
    public decimal Balance { get; private set; }
    public AccountStatus Status { get; private set; }
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private BankAccount() { }

    public BankAccount(string accountNumber, decimal initialDeposit)
    {
        Id = Guid.NewGuid();
        AccountNumber = accountNumber;
        Balance = initialDeposit;
        Status = AccountStatus.Active;
        
        if (initialDeposit > 0)
        {
            _transactions.Add(new Transaction(TransactionType.Deposit, initialDeposit, "Initial deposit"));
        }
    }

    public Result<Transaction> Deposit(decimal amount, string description = "")
    {
        if (amount <= 0)
            return Result.Failure<Transaction>(AccountErrors.InvalidAmount);

        if (Status != AccountStatus.Active)
            return Result.Failure<Transaction>(AccountErrors.AccountNotActive);

        var transaction = new Transaction(TransactionType.Deposit, amount, description);
        _transactions.Add(transaction);
        Balance += amount;

        return Result.Success(transaction);
    }

    public Result<Transaction> Withdraw(decimal amount, string description = "")
    {
        if (amount <= 0)
            return Result.Failure<Transaction>(AccountErrors.InvalidAmount);

        if (Status != AccountStatus.Active)
            return Result.Failure<Transaction>(AccountErrors.AccountNotActive);

        if (Balance < amount)
            return Result.Failure<Transaction>(AccountErrors.InsufficientFunds);

        var transaction = new Transaction(TransactionType.Withdrawal, amount, description);
        _transactions.Add(transaction);
        Balance -= amount;

        return Result.Success(transaction);
    }

    public void CloseAccount()
    {
        if (Balance != 0)
            throw new DomainException("Cannot close account with non-zero balance");
        
        Status = AccountStatus.Closed;
    }
}

public class Transaction : Entity<Guid>
{
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; }
    public DateTime Timestamp { get; private set; }

    private Transaction() { }

    public Transaction(TransactionType type, decimal amount, string description)
    {
        Id = Guid.NewGuid();
        Type = type;
        Amount = amount;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
}

public enum TransactionType { Deposit, Withdrawal, Transfer }
public enum AccountStatus { Active, Suspended, Closed }
```

### 4. Performance Monitoring

```csharp
public class EntityPerformanceService
{
    public void MonitorEntityPerformance()
    {
        // Get performance statistics for Entity<T> type caching
        var (hits, misses, hitRatio) = Entity<Guid>.GetPerformanceStats();
        
        Console.WriteLine($"Entity Cache Performance:");
        Console.WriteLine($"  Hits: {hits}");
        Console.WriteLine($"  Misses: {misses}");
        Console.WriteLine($"  Hit Ratio: {hitRatio:P2}");

        // Clear cache if needed (testing scenarios)
        if (hitRatio < 0.8) // Less than 80% hit ratio
        {
            Entity<Guid>.ClearTypeCache();
            Console.WriteLine("Cache cleared due to low hit ratio");
        }
    }
}
```

## Value Object Patterns

### 1. Simple Value Objects

```csharp
public class PersonName : ValueObject
{
    public string First { get; init; }
    public string Last { get; init; }
    public string? Middle { get; init; }

    public PersonName(string first, string last, string? middle = null)
    {
        First = first ?? throw new ArgumentNullException(nameof(first));
        Last = last ?? throw new ArgumentNullException(nameof(last));
        Middle = middle;
    }

    public string FullName => string.IsNullOrEmpty(Middle) 
        ? $"{First} {Last}" 
        : $"{First} {Middle} {Last}";

    public static PersonName Parse(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            2 => new PersonName(parts[0], parts[1]),
            3 => new PersonName(parts[0], parts[2], parts[1]),
            _ => throw new ArgumentException("Invalid name format")
        };
    }
}

public class EmailAddress : ValueObject
{
    public string Value { get; init; }
    public string Domain => Value.Split('@')[1];
    public string LocalPart => Value.Split('@')[0];

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty");

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format");

        Value = email.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        return email.Contains('@') && email.Contains('.') && email.Length > 5;
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public static explicit operator EmailAddress(string email) => new(email);
}
```

### 2. Complex Value Objects

```csharp
public class PhoneNumber : ValueObject
{
    public string CountryCode { get; init; }
    public string AreaCode { get; init; }
    public string Number { get; init; }
    public PhoneType Type { get; init; }

    public PhoneNumber(string countryCode, string areaCode, string number, PhoneType type = PhoneType.Mobile)
    {
        CountryCode = ValidateAndFormat(countryCode, "Country code");
        AreaCode = ValidateAndFormat(areaCode, "Area code");
        Number = ValidateAndFormat(number, "Number");
        Type = type;
    }

    public string InternationalFormat => $"+{CountryCode} ({AreaCode}) {Number}";
    public string NationalFormat => $"({AreaCode}) {Number}";

    private static string ValidateAndFormat(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} cannot be empty");

        return value.Trim();
    }

    public static PhoneNumber Parse(string phoneNumber)
    {
        // Simple parsing logic - production code would be more robust
        var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
        
        return cleaned.Length switch
        {
            10 => new PhoneNumber("1", cleaned[..3], cleaned[3..]),
            11 when cleaned.StartsWith("1") => new PhoneNumber("1", cleaned[1..4], cleaned[4..]),
            _ => throw new FormatException("Invalid phone number format")
        };
    }
}

public enum PhoneType { Mobile, Home, Work, Fax }

public class Range<T> : ValueObject where T : IComparable<T>
{
    public T Start { get; init; }
    public T End { get; init; }

    public Range(T start, T end)
    {
        if (start.CompareTo(end) > 0)
            throw new ArgumentException("Start cannot be greater than end");

        Start = start;
        End = end;
    }

    public bool Contains(T value) => 
        value.CompareTo(Start) >= 0 && value.CompareTo(End) <= 0;

    public bool Overlaps(Range<T> other) =>
        Start.CompareTo(other.End) <= 0 && End.CompareTo(other.Start) >= 0;

    public override string ToString() => $"[{Start}, {End}]";
}
```

### 3. Value Objects with Collections

```csharp
public class Tags : ValueObject
{
    private readonly HashSet<string> _tags;

    public IReadOnlySet<string> Values => _tags;
    public int Count => _tags.Count;

    public Tags(IEnumerable<string> tags)
    {
        _tags = tags?.Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(t => t.Trim().ToLowerInvariant())
                    .ToHashSet() ?? new HashSet<string>();
    }

    public Tags(params string[] tags) : this((IEnumerable<string>)tags) { }

    public Tags Add(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return this;

        var newTags = new HashSet<string>(_tags) { tag.Trim().ToLowerInvariant() };
        return new Tags(newTags);
    }

    public Tags Remove(string tag)
    {
        var newTags = new HashSet<string>(_tags);
        newTags.Remove(tag?.Trim().ToLowerInvariant());
        return new Tags(newTags);
    }

    public bool Contains(string tag) => 
        _tags.Contains(tag?.Trim().ToLowerInvariant());
    
    public static implicit operator string[](Tags tags) => tags._tags.ToArray();
}
```

### 4. High-Performance Value Objects

```csharp
// Optimized for performance - simple properties only
public class Coordinates : ValueObject
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public Coordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }

    public double DistanceTo(Coordinates other)
    {
        // Haversine formula
        const double R = 6371; // Earth's radius in kilometers
        
        var lat1Rad = ToRadians(Latitude);
        var lat2Rad = ToRadians(other.Latitude);
        var deltaLatRad = ToRadians(other.Latitude - Latitude);
        var deltaLonRad = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

// Using fast accessors
public class ProductDimensions : ValueObject
{
    public decimal Length { get; init; }
    public decimal Width { get; init; }
    public decimal Height { get; init; }
    public string Unit { get; init; }

    public ProductDimensions(decimal length, decimal width, decimal height, string unit = "cm")
    {
        Length = length;
        Width = width; 
        Height = height;
        Unit = unit;
    }

    public decimal Volume => Length * Width * Height;
    
    // This will use compiled accessors for ~1000x performance improvement
    // in equality comparisons due to simple properties
}
```

## Audit Trail Patterns

### 1. Basic Audit Implementation

```csharp
public class Product : UserAuditableEntity<Guid, string>
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
    public bool IsActive { get; set; }

    // Inherits: Id, Created, Updated, CreatedBy, UpdatedBy
}

// Usage in service
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;
    private readonly ICurrentUserService _currentUser;

    public async Task<Result<Product>> UpdateProductAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            return Result.Failure<Product>(ProductErrors.NotFound);

        // Capture old values for audit
        var oldValues = new { product.Name, product.Price, product.Category };
        
        // Update product
        product.Name = request.Name;
        product.Price = request.Price;
        product.Category = request.Category;
        product.Updated = DateTime.UtcNow;
        product.UpdatedBy = _currentUser.UserId;

        await _repository.UpdateAsync(product);
        await _repository.UnitOfWork.SaveChangesAsync();

        // Create audit trail
        await CreateAuditTrailAsync(product, oldValues, new { product.Name, product.Price, product.Category });

        return Result.Success(product);
    }

    private async Task CreateAuditTrailAsync<T>(Product product, T oldValues, T newValues)
    {
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Update,
            EntityName = nameof(Product),
            PrimaryKey = product.Id.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = _currentUser.UserId,
            ChangedColumns = ["Name", "Price", "Category"]
        };

        // High-performance JSON setting
        audit.OldValuesJson = JsonSerializer.Serialize(oldValues);
        audit.NewValuesJson = JsonSerializer.Serialize(newValues);

        await _auditStore.CreateAsync(audit, CancellationToken.None);
    }
}
```

### 2. High-Performance Audit with Spans

```csharp
public class HighPerformanceAuditService
{
    public async Task AuditSmallChangesAsync<TEntity, TKey>(
        TEntity entity,
        TKey entityId,
        string userId,
        Dictionary<string, (object oldValue, object newValue)> changes)
        where TEntity : class
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        if (changes.Count <= 3) // Use span optimization for small changes
        {
            var columnNames = changes.Keys.ToArray();
            var oldValues = changes.Values.Select(v => v.oldValue).ToArray();
            var newValues = changes.Values.Select(v => v.newValue).ToArray();

            var audit = new Audit<TKey, ApplicationUser, string>
            {
                TrailType = AuditTrailType.Update,
                EntityName = typeof(TEntity).Name,
                PrimaryKey = entityId.ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };

            // Zero-allocation span operation for small changes
            audit.SetValuesFromSpan<object>(
                columnNames.AsSpan(),
                oldValues.AsSpan(),
                newValues.AsSpan()
            );

            await _auditStore.CreateAsync(audit, CancellationToken.None);
        }
        else
        {
            // Fall back to dictionary approach for larger changes
            await AuditLargeChangesAsync(entity, entityId, userId, changes);
        }
    }

    public async Task<object?> GetSpecificAuditValueAsync(Guid auditId, string columnName)
    {
        var auditResult = await _auditStore.FindByIdAsync(auditId, CancellationToken.None);
        if (!auditResult.IsSuccess || auditResult.Value == null)
            return null;

        // Efficient single-value access without full deserialization
        return auditResult.Value.GetNewValue(columnName);
    }
}
```

### 3. Soft Delete with Audit

```csharp
public class Document : SoftDeleteAuditableEntity<Guid, string>
{
    public string Title { get; set; }
    public string Content { get; set; }
    public string ContentType { get; set; }

    public void Delete(string deletedBy)
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        Deleted = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        IsDeleted = false;
        Deleted = null;
        DeletedBy = null;
    }
}

// Repository with soft delete support
public class DocumentRepository : IAsyncRepository<Document>
{
    private readonly ApplicationDbContext _context;

    public IQueryable<Document> ActiveDocuments => 
        _context.Documents.Where(d => !d.IsDeleted);

    public IQueryable<Document> DeletedDocuments => 
        _context.Documents.Where(d => d.IsDeleted);

    public async Task<Document?> GetActiveByIdAsync(Guid id)
    {
        return await ActiveDocuments.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Result> SoftDeleteAsync(Guid id, string deletedBy)
    {
        var document = await GetActiveByIdAsync(id);
        if (document == null)
            return Result.Failure(DocumentErrors.NotFound);

        document.Delete(deletedBy);
        await _context.SaveChangesAsync();
        
        return Result.Success();
    }
}
```

### 4. Custom Audit Configurations

```csharp
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

        // JSON columns for efficient storage
        builder.Property(a => a.OldValuesJson)
               .HasColumnType("nvarchar(max)");
               
        builder.Property(a => a.NewValuesJson)
               .HasColumnType("nvarchar(max)");

        // Index for common queries
        builder.HasIndex(a => new { a.EntityName, a.PrimaryKey });
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.UserId);

        // Relationship to user
        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
```

## Repository Patterns

### 1. Generic Repository Implementation

```csharp
public class Repository<T> : IAsyncRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IUnitOfWorkAsync UnitOfWork => _context;

    public virtual async Task<T> AttachAsync(T item)
    {
        var entry = _dbSet.Attach(item);
        return await Task.FromResult(entry.Entity);
    }

    public virtual async Task<T> AddAsync(T item)
    {
        var entry = await _dbSet.AddAsync(item);
        return entry.Entity;
    }

    public virtual async Task<T> UpdateAsync(T item)
    {
        _dbSet.Update(item);
        return await Task.FromResult(item);
    }

    public virtual async Task<T> DeleteAsync(T item)
    {
        _dbSet.Remove(item);
        return await Task.FromResult(item);
    }

    public virtual async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}

// Specific repository implementation
public interface IOrderRepository : IAsyncRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber);
    Task<IList<Order>> GetOrdersByCustomerAsync(Guid customerId);
    Task<IList<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
    {
        return await _dbSet
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
    }

    public async Task<IList<Order>> GetOrdersByCustomerAsync(Guid customerId)
    {
        return await _dbSet
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.Created)
            .ToListAsync();
    }

    public async Task<IList<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(o => o.Created >= startDate && o.Created <= endDate)
            .Include(o => o.Items)
            .ToListAsync();
    }
}
```

### 2. Repository with Specifications

```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int Take { get; }
    int Skip { get; }
}

public class BaseSpecification<T> : ISpecification<T>
{
    public BaseSpecification(Expression<Func<T, bool>>? criteria = null)
    {
        Criteria = criteria ?? (_ => true);
    }

    public Expression<Func<T, bool>> Criteria { get; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Take { get; private set; }
    public int Skip { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }
}

// Example specification
public class OrdersWithItemsSpecification : BaseSpecification<Order>
{
    public OrdersWithItemsSpecification(Guid customerId) 
        : base(o => o.CustomerId == customerId)
    {
        AddInclude(o => o.Items);
        ApplyOrderByDescending(o => o.Created);
    }
}

public class ActiveOrdersSpecification : BaseSpecification<Order>
{
    public ActiveOrdersSpecification() 
        : base(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Delivered)
    {
        ApplyOrderBy(o => o.Created);
    }
}

// Repository with specifications
public interface IRepositoryWithSpecifications<T> : IAsyncRepository<T> where T : class
{
    Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec);
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
}

public class RepositoryWithSpecifications<T> : Repository<T>, IRepositoryWithSpecifications<T> 
    where T : class
{
    public RepositoryWithSpecifications(ApplicationDbContext context) : base(context) { }

    public async Task<T?> GetEntityWithSpecAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).ToListAsync();
    }

    public async Task<int> CountAsync(ISpecification<T> spec)
    {
        return await ApplySpecification(spec).CountAsync();
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        var query = _dbSet.AsQueryable();

        query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy != null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        if (spec.Skip > 0)
        {
            query = query.Skip(spec.Skip);
        }

        if (spec.Take > 0)
        {
            query = query.Take(spec.Take);
        }

        return query;
    }
}
```

## Domain Events

### 1. Domain Event Implementation

```csharp
public class OrderCreatedEvent : DomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public Guid CustomerId { get; }
    public decimal TotalAmount { get; }

    public OrderCreatedEvent(Guid orderId, string orderNumber, Guid customerId, decimal totalAmount)
    {
        OrderId = orderId;
        OrderNumber = orderNumber;
        CustomerId = customerId;
        TotalAmount = totalAmount;
        TimeStamp = DateTimeOffset.UtcNow;
    }
}

public class ProductPriceChangedEvent : DomainEvent
{
    public Guid ProductId { get; }
    public decimal OldPrice { get; }
    public decimal NewPrice { get; }
    public string ChangedBy { get; }

    public ProductPriceChangedEvent(Guid productId, decimal oldPrice, decimal newPrice, string changedBy)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedBy = changedBy;
        TimeStamp = DateTimeOffset.UtcNow;
    }
}
```

### 2. Domain Event Handlers

```csharp
// Event handler interface (implementing MediatR pattern)
public interface IDomainEventHandler<in TDomainEvent> 
    where TDomainEvent : IDomainEvent
{
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

// Specific event handlers
public class OrderCreatedEventHandler : IDomainEventHandler<OrderCreatedEvent>
{
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;

    public OrderCreatedEventHandler(IEmailService emailService, IInventoryService inventoryService)
    {
        _emailService = emailService;
        _inventoryService = inventoryService;
    }

    public async Task Handle(OrderCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Send confirmation email
        await _emailService.SendOrderConfirmationAsync(domainEvent.CustomerId, domainEvent.OrderId);
        
        // Reserve inventory
        await _inventoryService.ReserveInventoryForOrderAsync(domainEvent.OrderId);
        
        // Other side effects...
    }
}

public class ProductPriceChangedEventHandler : IDomainEventHandler<ProductPriceChangedEvent>
{
    private readonly IAuditStore<Guid, ApplicationUser, string> _auditStore;
    private readonly INotificationService _notificationService;

    public async Task Handle(ProductPriceChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        // Create detailed audit trail for price changes
        var audit = new Audit<Guid, ApplicationUser, string>
        {
            TrailType = AuditTrailType.Update,
            EntityName = "Product",
            PrimaryKey = domainEvent.ProductId.ToString(),
            Timestamp = domainEvent.TimeStamp.DateTime,
            ChangedColumns = ["Price"]
        };

        audit.SetValuesFromJson(
            JsonSerializer.Serialize(new { Price = domainEvent.OldPrice }),
            JsonSerializer.Serialize(new { Price = domainEvent.NewPrice })
        );

        await _auditStore.CreateAsync(audit, cancellationToken);

        // Notify relevant parties
        await _notificationService.NotifyPriceChangeAsync(
            domainEvent.ProductId, 
            domainEvent.OldPrice, 
            domainEvent.NewPrice
        );
    }
}
```

## Result Pattern Usage

### 1. Service Layer with Result Pattern

```csharp
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;

    public async Task<Result<Order>> CreateOrderAsync(CreateOrderCommand command)
    {
        // Validate customer
        var customer = await _customerRepository.GetByIdAsync(command.CustomerId);
        if (customer == null)
            return Result.Failure<Order>(CustomerErrors.NotFound);

        // Create order
        var order = new Order(
            GenerateOrderNumber(),
            command.ShippingAddress
        );

        // Add items
        foreach (var item in command.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
                return Result.Failure<Order>(ProductErrors.NotFound);

            var addResult = order.AddItem(product, item.Quantity, item.UnitPrice);
            if (addResult.IsFailure)
                return Result.Failure<Order>(addResult.Error);
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.UnitOfWork.SaveChangesAsync();

        return Result.Success(order);
    }

    public async Task<Result> CancelOrderAsync(Guid orderId, string reason)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
            return Result.Failure(OrderErrors.NotFound);

        var cancelResult = order.Cancel(reason);
        if (cancelResult.IsFailure)
            return cancelResult;

        await _orderRepository.UpdateAsync(order);
        await _orderRepository.UnitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<Order>>> GetOrdersForCustomerAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);
        if (customer == null)
            return Result.Failure<List<Order>>(CustomerErrors.NotFound);

        var orders = await _orderRepository.GetOrdersByCustomerAsync(customerId);
        return Result.Success(orders.ToList());
    }

    // Combining multiple operations
    public async Task<Result> ProcessOrderBatchAsync(List<CreateOrderCommand> commands)
    {
        var results = new List<Result>();

        foreach (var command in commands)
        {
            var result = await CreateOrderAsync(command);
            if (result.IsFailure)
                results.Add(Result.Failure(result.Error));
            else
                results.Add(Result.Success());
        }

        // Return first failure or success if all succeeded
        return Result.FirstFailureOrSuccess(results.ToArray());
    }
}
```

### 2. Controller Integration with Result Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderCommand command)
    {
        var result = await _orderService.CreateOrderAsync(command);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetOrder), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _orderService.GetOrderAsync(id);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, CancelOrderCommand command)
    {
        var result = await _orderService.CancelOrderAsync(id, command.Reason);
        
        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId)
    {
        var result = await _orderService.GetOrdersForCustomerAsync(customerId);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }
}
```

### 3. Advanced Result Patterns

```csharp
// Result with validation
public class ValidationResult : Result
{
    public List<ValidationError> Errors { get; }

    protected ValidationResult(List<ValidationError> errors) 
        : base(false, Error.Validation)
    {
        Errors = errors;
    }

    public static ValidationResult WithErrors(params ValidationError[] errors)
        => new(errors.ToList());
}

public class ValidationError
{
    public string PropertyName { get; }
    public string ErrorMessage { get; }

    public ValidationError(string propertyName, string errorMessage)
    {
        PropertyName = propertyName;
        ErrorMessage = errorMessage;
    }
}

// Validation service
public class OrderValidationService
{
    public ValidationResult ValidateCreateOrder(CreateOrderCommand command)
    {
        var errors = new List<ValidationError>();

        if (command.CustomerId == Guid.Empty)
            errors.Add(new ValidationError(nameof(command.CustomerId), "Customer ID is required"));

        if (command.Items == null || !command.Items.Any())
            errors.Add(new ValidationError(nameof(command.Items), "At least one item is required"));

        if (command.ShippingAddress == null)
            errors.Add(new ValidationError(nameof(command.ShippingAddress), "Shipping address is required"));

        foreach (var item in command.Items ?? Enumerable.Empty<OrderItemDto>())
        {
            if (item.Quantity <= 0)
                errors.Add(new ValidationError($"Items[{command.Items!.ToList().IndexOf(item)}].Quantity", 
                    "Quantity must be greater than zero"));
        }

        return errors.Any() 
            ? ValidationResult.WithErrors(errors.ToArray())
            : ValidationResult.Success();
    }
}
```

## EF Core Integration

### 1. DbContext Configuration

```csharp
public class ApplicationDbContext : AuditDbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Audit<Guid, ApplicationUser, string>> AuditTrails { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Configure value objects
        ConfigureValueObjects(modelBuilder);
        
        // Configure audit entities
        ConfigureAuditEntities(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Money value object
        modelBuilder.Entity<Order>()
            .OwnsOne(o => o.TotalAmount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("TotalAmount");
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3);
            });

        // Address value object
        modelBuilder.Entity<Order>()
            .OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Street).HasColumnName("ShippingStreet").HasMaxLength(200);
                address.Property(a => a.City).HasColumnName("ShippingCity").HasMaxLength(100);
                address.Property(a => a.State).HasColumnName("ShippingState").HasMaxLength(50);
                address.Property(a => a.ZipCode).HasColumnName("ShippingZipCode").HasMaxLength(20);
                address.Property(a => a.Country).HasColumnName("ShippingCountry").HasMaxLength(50);
            });
    }

    private static void ConfigureAuditEntities(ModelBuilder modelBuilder)
    {
        // Configure audit timestamps with value generators
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var entity = entityType.ClrType;
            
            if (typeof(IAuditable).IsAssignableFrom(entity))
            {
                modelBuilder.Entity(entity)
                    .Property<DateTime?>("Created")
                    .HasValueGenerator<DateTimeNowGenerator>();
            }
        }
    }

    // Override SaveChanges to automatically handle audit fields
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditable>();
        
        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.Created = DateTime.UtcNow;
                    break;
                
                case EntityState.Modified:
                    entry.Entity.Updated = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### 2. Entity Configurations

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
               .HasMaxLength(20)
               .IsRequired();

        builder.Property(o => o.Status)
               .HasConversion<string>()
               .HasMaxLength(20);

        // Value object configurations
        builder.OwnsOne(o => o.TotalAmount);
        builder.OwnsOne(o => o.ShippingAddress);

        // Collection configuration
        builder.HasMany(o => o.Items)
               .WithOne()
               .HasForeignKey("OrderId")
               .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.Created);

        // Audit configuration
        builder.Property(o => o.Created)
               .HasValueGenerator<DateTimeNowGenerator>();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(p => p.Price)
               .HasPrecision(18, 2);

        // Soft delete configuration
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
```

### 3. Startup Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IAuditStore<Guid, ApplicationUser, string>, AuditStore>();

// Register domain services
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ProductService>();

var app = builder.Build();

// Ensure database is created/migrated
await app.MigrateDatabaseAsync<ApplicationDbContext>();

app.Run();
```

## Performance Optimization Examples

### 1. Entity Performance Monitoring

```csharp
public class PerformanceMonitoringService
{
    private readonly ILogger<PerformanceMonitoringService> _logger;

    public void MonitorEntityPerformance()
    {
        var stats = Entity<Guid>.GetPerformanceStats();
        
        _logger.LogInformation("Entity Performance Stats: Hits={Hits}, Misses={Misses}, HitRatio={HitRatio:P2}", 
            stats.Hits, stats.Misses, stats.HitRatio);

        if (stats.HitRatio < 0.8m) // Less than 80% hit ratio
        {
            _logger.LogWarning("Entity cache hit ratio is below 80%. Consider investigating proxy usage patterns.");
        }

        // Clear cache if memory pressure detected
        if (GC.GetTotalMemory(false) > 500_000_000) // 500MB threshold
        {
            Entity<Guid>.ClearTypeCache();
            _logger.LogInformation("Entity type cache cleared due to memory pressure");
        }
    }
}
```

### 2. Value Object Performance Testing

```csharp
public class ValueObjectPerformanceTests
{
    [Fact]
    public void SimpleValueObject_UsesCompiledAccessors()
    {
        var coord1 = new Coordinates(40.7128, -74.0060);
        var coord2 = new Coordinates(40.7128, -74.0060);
        
        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < 100_000; i++)
        {
            var isEqual = coord1.Equals(coord2);
        }
        
        stopwatch.Stop();
        
        // With compiled accessors, this should be very fast
        Assert.True(stopwatch.ElapsedMilliseconds < 10);
    }

    [Fact]
    public void ComplexValueObject_FallsBackToReflection()
    {
        var tags1 = new Tags("tag1", "tag2", "tag3");
        var tags2 = new Tags("tag1", "tag2", "tag3");
        
        // This will use reflection fallback due to complex collection property
        var isEqual = tags1.Equals(tags2);
        Assert.True(isEqual);
    }
}
```

### 3. High-Performance Audit Operations

```csharp
public class HighPerformanceAuditExample
{
    public async Task BulkAuditOperationsAsync()
    {
        var audits = new List<Audit<Guid, ApplicationUser, string>>();

        // Prepare bulk audit data
        for (int i = 0; i < 1000; i++)
        {
            var audit = new Audit<Guid, ApplicationUser, string>
            {
                TrailType = AuditTrailType.Update,
                EntityName = "Product",
                PrimaryKey = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                UserId = "user123"
            };

            // Use span operations for small changes (optimal performance)
            ReadOnlySpan<string> columns = ["Name", "Price"];
            ReadOnlySpan<object> oldValues = [$"OldName{i}", 99.99m];
            ReadOnlySpan<object> newValues = [$"NewName{i}", 149.99m];

            audit.SetValuesFromSpan(columns, oldValues, newValues);
            audits.Add(audit);
        }

        // Bulk insert
        await _context.AuditTrails.AddRangeAsync(audits);
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, object>> GetAuditValuesEfficientlyAsync(Guid auditId)
    {
        var audit = await _auditStore.FindByIdAsync(auditId, CancellationToken.None);
        if (!audit.IsSuccess || audit.Value == null)
            return new Dictionary<string, object>();

        var values = new Dictionary<string, object>();

        // Efficient single-value access (no full deserialization)
        foreach (var column in audit.Value.ChangedColumns)
        {
            var value = audit.Value.GetNewValue(column);
            if (value != null)
                values[column] = value;
        }

        return values;
    }
}
```

## Advanced Scenarios

### 1. Multi-Tenant Architecture

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}

public class TenantAwareEntity<T> : AuditableEntity<T>, ITenantEntity
    where T : IComparable<T>, IEquatable<T>
{
    public string TenantId { get; set; } = null!;
}

public class MultiTenantOrder : TenantAwareEntity<Guid>, IAggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    // ... other properties
}

// Multi-tenant DbContext
public class MultiTenantDbContext : AuditDbContext
{
    private readonly ITenantProvider _tenantProvider;

    public MultiTenantDbContext(DbContextOptions options, ITenantProvider tenantProvider) 
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply global query filters for multi-tenancy
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var tenantId = Expression.Constant(_tenantProvider.TenantId);
                var filter = Expression.Lambda(Expression.Equal(property, tenantId), parameter);
                
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set tenant ID for new entities
        var tenantEntities = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in tenantEntities)
        {
            entry.Entity.TenantId = _tenantProvider.TenantId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

### 2. Event Sourcing Pattern

```csharp
public abstract class EventSourcedAggregateRoot : Entity<Guid>, IAggregateRoot<Guid>
{
    private readonly List<IDomainEvent> _events = new();
    
    public IReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();
    public int Version { get; private set; }

    protected void AddEvent(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
        Version++;
    }

    public void ClearEvents()
    {
        _events.Clear();
    }

    protected void Apply(IDomainEvent domainEvent)
    {
        var method = GetType().GetMethod("Apply", new[] { domainEvent.GetType() });
        method?.Invoke(this, new object[] { domainEvent });
        Version++;
    }

    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            Apply(domainEvent);
        }
    }
}

public class EventSourcedOrder : EventSourcedAggregateRoot
{
    public string OrderNumber { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();

    private EventSourcedOrder() { } // For reconstruction from events

    public EventSourcedOrder(string orderNumber)
    {
        var orderCreated = new OrderCreatedEvent(Guid.NewGuid(), orderNumber, Guid.Empty, 0);
        AddEvent(orderCreated);
        Apply(orderCreated);
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        var itemAdded = new OrderItemAddedEvent(Id, productId, quantity, unitPrice);
        AddEvent(itemAdded);
        Apply(itemAdded);
    }

    // Event application methods
    private void Apply(OrderCreatedEvent @event)
    {
        Id = @event.OrderId;
        OrderNumber = @event.OrderNumber;
        Status = OrderStatus.Pending;
    }

    private void Apply(OrderItemAddedEvent @event)
    {
        Items.Add(new OrderItem(@event.ProductId, @event.Quantity, 
            new Money(@event.UnitPrice, "USD")));
    }
}

// Event store
public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion);
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId);
}
```

### 3. CQRS with Projections

```csharp
// Command side (write model)
public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IEventStore _eventStore;

    public async Task<Result<Guid>> Handle(CreateOrderCommand command)
    {
        var order = new EventSourcedOrder(command.OrderNumber);
        
        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        await _repository.AddAsync(order);
        await _eventStore.SaveEventsAsync(order.Id, order.Events, 0);
        
        return Result.Success(order.Id);
    }
}

// Query side (read model)
public class OrderProjection : Entity<Guid>
{
    public string OrderNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public int ItemCount { get; set; }
}

public class OrderProjectionHandler : 
    IDomainEventHandler<OrderCreatedEvent>,
    IDomainEventHandler<OrderItemAddedEvent>
{
    private readonly IOrderProjectionRepository _projectionRepository;

    public async Task Handle(OrderCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        var projection = new OrderProjection
        {
            Id = domainEvent.OrderId,
            OrderNumber = domainEvent.OrderNumber,
            Status = "Pending",
            OrderDate = domainEvent.TimeStamp.DateTime,
            TotalAmount = 0,
            ItemCount = 0
        };

        await _projectionRepository.AddAsync(projection);
        await _projectionRepository.UnitOfWork.SaveChangesAsync();
    }

    public async Task Handle(OrderItemAddedEvent domainEvent, CancellationToken cancellationToken)
    {
        var projection = await _projectionRepository.GetByIdAsync(domainEvent.OrderId);
        if (projection != null)
        {
            projection.ItemCount++;
            projection.TotalAmount += domainEvent.UnitPrice * domainEvent.Quantity;
            
            await _projectionRepository.UpdateAsync(projection);
            await _projectionRepository.UnitOfWork.SaveChangesAsync();
        }
    }
}

// Query handler
public class GetOrderQueryHandler
{
    private readonly IOrderProjectionRepository _projectionRepository;

    public async Task<Result<OrderProjection>> Handle(GetOrderQuery query)
    {
        var projection = await _projectionRepository.GetByIdAsync(query.OrderId);
        return projection != null 
            ? Result.Success(projection)
            : Result.Failure<OrderProjection>(OrderErrors.NotFound);
    }
}
```

This comprehensive usage guide demonstrates practical implementation patterns for the Wangkanai Domain library, showcasing how to leverage its performance optimizations and clean architecture principles in real-world scenarios.