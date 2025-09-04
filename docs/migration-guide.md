# Migration Guide

**Comprehensive migration strategies and upgrade paths for the Wangkanai Domain library**

---

## Migration Overview

### Migration Strategy Matrix

| Current State | Target State | Migration Path | Complexity | Timeline |
|---------------|--------------|----------------|------------|----------|
| Plain POCO Entities | Wangkanai Domain Entities | [Gradual Entity Migration](#from-plain-poco-entities) | Low | 1-2 weeks |
| Other DDD Frameworks | Wangkanai Domain | [Framework Migration](#from-other-ddd-frameworks) | Medium | 2-4 weeks |
| Legacy EF Models | Domain + Audit Models | [Legacy EF Migration](#from-legacy-entity-framework) | High | 4-8 weeks |
| Existing Audit Systems | Wangkanai Audit | [Audit System Migration](#audit-system-migration) | Medium | 2-3 weeks |
| .NET Framework | .NET 9 + Domain | [Framework Upgrade](#net-framework-to-net-9) | High | 6-12 weeks |

### Pre-Migration Assessment

**Compatibility Check**:
```csharp
// Run this assessment script before migration
public class MigrationAssessment
{
    public AssessmentResult AssessCurrentSystem()
    {
        return new AssessmentResult
        {
            FrameworkVersion = GetFrameworkVersion(),
            EntityCount = CountEntities(),
            ComplexityScore = CalculateComplexity(),
            AuditRequirements = AssessAuditNeeds(),
            PerformanceRequirements = AssessPerformanceNeeds(),
            RecommendedPath = RecommendMigrationPath()
        };
    }
    
    private MigrationPath RecommendMigrationPath()
    {
        // Logic to recommend best migration strategy
        // based on current system characteristics
    }
}
```

---

## From Plain POCO Entities

### Current State Assessment
```csharp
// Typical existing entity structure
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
```

### Phase 1: Foundation Setup (Week 1)

**Step 1: Install Packages**
```xml
<!-- Add to your project files -->
<PackageReference Include="Wangkanai.Domain" Version="latest" />
<PackageReference Include="Wangkanai.Audit" Version="latest" />
<PackageReference Include="Wangkanai.EntityFramework" Version="latest" />
```

**Step 2: Update DbContext**
```csharp
// Before: Plain DbContext
public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
}

// After: AuditDbContext with backward compatibility
public class ApplicationDbContext : AuditDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Apply domain conventions
        
        // Maintain existing table/column mappings
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedDate).HasColumnName("CreatedDate");
            entity.Property(e => e.ModifiedDate).HasColumnName("ModifiedDate");
        });
    }
}
```

**Step 3: Create Migration (Verify No Changes)**
```bash
# Should generate empty migration if mappings are correct
dotnet ef migrations add "AddDomainLibrary" --context ApplicationDbContext
# Review migration - should be empty or minimal
dotnet ef database update
```

### Phase 2: Entity Migration (Week 1-2)

**Step 1: Migrate Simple Entities**
```csharp
// Before: Plain POCO
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

// After: Domain Entity (backward compatible)
public class User : AuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Map existing columns to inherited properties
    protected override void OnModelCreating(EntityTypeBuilder<User> builder)
    {
        builder.Property(e => e.Created).HasColumnName("CreatedDate");
        builder.Property(e => e.Updated).HasColumnName("ModifiedDate");
    }
}
```

**Step 2: Handle Custom ID Types**
```csharp
// For entities with Guid IDs
public class Product : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}

// Configure in OnModelCreating
modelBuilder.Entity<Product>(entity =>
{
    entity.Property(e => e.Created).HasColumnName("CreatedDate");
    entity.Property(e => e.Updated).HasColumnName("ModifiedDate");
});
```

**Step 3: Update Existing Code**
```csharp
// Before: Manual timestamp management
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    var user = new User
    {
        Name = request.Name,
        Email = request.Email,
        CreatedDate = DateTime.UtcNow // Manual timestamp
    };
    
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    return user;
}

// After: Automatic timestamp management
public async Task<User> CreateUserAsync(CreateUserRequest request)
{
    var user = new User
    {
        Name = request.Name,
        Email = request.Email
        // Created timestamp set automatically
    };
    
    _context.Users.Add(user);
    await _context.SaveChangesAsync();
    return user;
}
```

### Phase 3: Value Objects Introduction (Week 2)

**Step 1: Identify Value Object Candidates**
```csharp
// Before: Primitive properties
public class Product : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
}

// After: Value objects
public class Product : AuditableEntity<Guid>
{
    public ProductName Name { get; set; } = ProductName.Empty;
    public Money Price { get; set; } = Money.Zero;
}

// Create value objects
public class ProductName : ValueObject
{
    public string Value { get; }
    
    private ProductName(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Product name cannot be empty");
        
        return new ProductName(value.Trim());
    }
    
    public static ProductName Empty => new ProductName("");
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
    
    public static implicit operator string(ProductName productName) => productName.Value;
    public static implicit operator ProductName(string value) => Create(value);
}

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }
    
    public static Money Zero => new Money(0, "USD");
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
        
        return new Money(Amount + other.Amount, Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

**Step 2: Configure EF Value Conversions**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // ProductName value conversion
    modelBuilder.Entity<Product>()
        .Property(e => e.Name)
        .HasConversion(
            productName => productName.Value,
            value => ProductName.Create(value));
    
    // Money value conversion (owned type)
    modelBuilder.Entity<Product>()
        .OwnsOne(p => p.Price, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Price");
            money.Property(m => m.Currency).HasColumnName("Currency");
        });
}
```

### Validation and Testing
```csharp
[Test]
public class MigrationValidationTests
{
    [Test]
    public async Task Should_Maintain_Data_Integrity_After_Migration()
    {
        // Arrange: Pre-migration data
        var originalUsers = await _originalContext.Users.ToListAsync();
        
        // Act: Run migration
        await _context.Database.MigrateAsync();
        
        // Assert: Verify data integrity
        var migratedUsers = await _context.Users.ToListAsync();
        
        Assert.That(migratedUsers.Count, Is.EqualTo(originalUsers.Count));
        
        foreach (var originalUser in originalUsers)
        {
            var migratedUser = migratedUsers.First(u => u.Id == originalUser.Id);
            Assert.That(migratedUser.Name, Is.EqualTo(originalUser.Name));
            Assert.That(migratedUser.Email, Is.EqualTo(originalUser.Email));
            Assert.That(migratedUser.Created, Is.EqualTo(originalUser.CreatedDate));
        }
    }
    
    [Test]
    public void Should_Improve_Performance_After_Migration()
    {
        // Arrange
        var users = GenerateTestUsers(1000);
        
        // Act & Assert: Performance comparison
        var beforeMigration = MeasurePerformance(() => CompareUsersReflection(users));
        var afterMigration = MeasurePerformance(() => CompareUsersDomain(users));
        
        // Should see performance improvement
        Assert.That(afterMigration.TotalMilliseconds, Is.LessThan(beforeMigration.TotalMilliseconds * 0.5));
    }
}
```

**References**: [FAQ](faq.md#migration-from-plain-entities) | [Getting Started Guide](getting-started-guide.md)

---

## From Other DDD Frameworks

### Common Framework Migration Scenarios

#### From Enterprise Framework
```csharp
// Before: Generic enterprise framework
public class User : EntityBase<int>, IAuditableEntity
{
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}

// After: Wangkanai Domain
public class User : UserAuditableEntity<int, User, string>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Automatic audit functionality inherited
}

// Configure column mappings
modelBuilder.Entity<User>(entity =>
{
    entity.Property(e => e.Created).HasColumnName("CreatedOn");
    entity.Property(e => e.Updated).HasColumnName("UpdatedOn");
    entity.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
    entity.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
});
```

#### From MediatR + Custom Base Classes
```csharp
// Before: MediatR-based domain events
public abstract class AggregateRoot<T> : Entity<T>
{
    private readonly List<INotification> _domainEvents = new();
    
    public IReadOnlyList<INotification> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(INotification eventItem)
    {
        _domainEvents.Add(eventItem);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

// After: Wangkanai Domain (compatible with MediatR)
public class Order : Entity<Guid>, IAggregateRoot<Guid>
{
    private readonly List<IDomainMessage> _domainEvents = new();
    
    public IReadOnlyList<IDomainMessage> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainMessage eventItem)
    {
        _domainEvents.Add(eventItem);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
    // Business logic methods
    public void CompleteOrder()
    {
        Status = OrderStatus.Completed;
        AddDomainEvent(new OrderCompletedEvent(Id, CustomerId, TotalAmount));
    }
}

// Bridge MediatR and Domain Messages
public class MediatRDomainEventService
{
    private readonly IMediator _mediator;
    
    public async Task PublishDomainEventsAsync(IAggregateRoot aggregateRoot)
    {
        var domainEvents = aggregateRoot.DomainEvents;
        aggregateRoot.ClearDomainEvents();
        
        foreach (var domainEvent in domainEvents)
        {
            // Convert to MediatR notification if needed
            if (domainEvent is INotification notification)
            {
                await _mediator.Publish(notification);
            }
            else
            {
                // Handle Wangkanai domain messages
                await HandleDomainMessage(domainEvent);
            }
        }
    }
}
```

### Migration Steps

**Phase 1: Dependency Analysis**
```csharp
// Create compatibility layer
public static class MigrationCompatibility
{
    public static void ConfigureCompatibilityServices(IServiceCollection services)
    {
        // Bridge old interfaces to new ones
        services.AddScoped<IOldAuditService, NewAuditServiceAdapter>();
        services.AddScoped<IOldEntityService, NewEntityServiceAdapter>();
        
        // Maintain existing service registrations during transition
        services.AddScoped(provider => (IOldRepository<User>)provider.GetService<IRepository<User>>());
    }
}

// Adapter pattern for gradual migration
public class NewAuditServiceAdapter : IOldAuditService
{
    private readonly IAuditService _newAuditService;
    
    public NewAuditServiceAdapter(IAuditService newAuditService)
    {
        _newAuditService = newAuditService;
    }
    
    public async Task<AuditRecord> CreateAuditRecordAsync(OldAuditInfo auditInfo)
    {
        // Convert old audit info to new format
        var newAuditInfo = ConvertAuditInfo(auditInfo);
        var result = await _newAuditService.CreateAuditAsync(newAuditInfo);
        
        // Convert result back to old format
        return ConvertAuditResult(result);
    }
}
```

**Phase 2: Gradual Entity Migration**
```csharp
// Migration strategy: One entity type at a time
public class EntityMigrationPlan
{
    public List<MigrationStep> GetMigrationSteps()
    {
        return new List<MigrationStep>
        {
            new("User", MigrationComplexity.Low, TimeSpan.FromDays(2)),
            new("Product", MigrationComplexity.Medium, TimeSpan.FromDays(3)),
            new("Order", MigrationComplexity.High, TimeSpan.FromDays(5)) // Has domain events
        };
    }
}

// Track migration progress
public class MigrationTracker
{
    public async Task<MigrationStatus> GetStatusAsync()
    {
        return new MigrationStatus
        {
            TotalEntities = GetTotalEntityCount(),
            MigratedEntities = GetMigratedEntityCount(),
            RemainingEntities = GetRemainingEntityList(),
            EstimatedCompletion = CalculateCompletion(),
            Issues = GetMigrationIssues()
        };
    }
}
```

**Phase 3: Value Object Migration**
```csharp
// Identify value object opportunities
public class ValueObjectAnalyzer
{
    public List<ValueObjectCandidate> AnalyzeEntities(Assembly assembly)
    {
        var candidates = new List<ValueObjectCandidate>();
        
        foreach (var entityType in GetEntityTypes(assembly))
        {
            var properties = entityType.GetProperties();
            
            // Look for value object patterns
            var addressProperties = properties.Where(p => 
                p.Name.Contains("Address") || 
                p.Name.Contains("Street") || 
                p.Name.Contains("City")).ToList();
            
            if (addressProperties.Count >= 3)
            {
                candidates.Add(new ValueObjectCandidate
                {
                    Name = "Address",
                    Properties = addressProperties,
                    EntityType = entityType,
                    Confidence = CalculateConfidence(addressProperties)
                });
            }
            
            // Look for money patterns
            var moneyProperties = properties.Where(p => 
                (p.Name.Contains("Price") || p.Name.Contains("Amount")) &&
                properties.Any(cp => cp.Name.Contains("Currency"))).ToList();
            
            if (moneyProperties.Count >= 2)
            {
                candidates.Add(new ValueObjectCandidate
                {
                    Name = "Money",
                    Properties = moneyProperties,
                    EntityType = entityType,
                    Confidence = CalculateConfidence(moneyProperties)
                });
            }
        }
        
        return candidates.OrderByDescending(c => c.Confidence).ToList();
    }
}
```

**References**: [Design Decisions](design-decisions.md) | [Architecture Comparison](architectural_analysis_report.md)

---

## From Legacy Entity Framework

### Legacy Database Schema Assessment
```csharp
// Analyze existing database schema
public class LegacySchemaAnalyzer
{
    public async Task<SchemaAnalysisResult> AnalyzeSchemaAsync(string connectionString)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        
        var tables = await GetTablesAsync(connection);
        var relationships = await GetRelationshipsAsync(connection);
        var indexes = await GetIndexesAsync(connection);
        
        return new SchemaAnalysisResult
        {
            Tables = tables,
            Relationships = relationships,
            Indexes = indexes,
            AuditTables = IdentifyAuditTables(tables),
            MigrationComplexity = CalculateMigrationComplexity(tables, relationships),
            RecommendedApproach = RecommendMigrationApproach()
        };
    }
    
    private List<Table> IdentifyAuditTables(List<Table> tables)
    {
        return tables.Where(t => 
            t.Name.EndsWith("_Audit") || 
            t.Name.EndsWith("History") ||
            t.Columns.Any(c => c.Name == "AuditAction" || c.Name == "ChangeType"))
            .ToList();
    }
}
```

### Database-First Migration Strategy
```csharp
// Step 1: Scaffold existing database
// dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer -o Models

// Step 2: Create wrapper entities
public class LegacyUserEntity // Generated by scaffold
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string EmailAddress { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? ModifiedTimestamp { get; set; }
}

// Step 3: Create domain entities
public class User : AuditableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Constructor for migration from legacy
    public User(LegacyUserEntity legacy)
    {
        Id = legacy.UserId;
        Name = legacy.UserName;
        Email = legacy.EmailAddress;
        Created = legacy.CreatedTimestamp;
        Updated = legacy.ModifiedTimestamp;
    }
    
    // Implicit conversion for compatibility
    public static implicit operator User(LegacyUserEntity legacy) => new(legacy);
}

// Step 4: Configure entity mappings
public class LegacyApplicationDbContext : AuditDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users"); // Existing table name
            entity.Property(e => e.Id).HasColumnName("UserId");
            entity.Property(e => e.Name).HasColumnName("UserName");
            entity.Property(e => e.Email).HasColumnName("EmailAddress");
            entity.Property(e => e.Created).HasColumnName("CreatedTimestamp");
            entity.Property(e => e.Updated).HasColumnName("ModifiedTimestamp");
        });
    }
}
```

### Legacy Audit System Integration
```csharp
// Handle existing audit tables
public class LegacyAuditMigration
{
    public async Task MigrateAuditDataAsync()
    {
        // Step 1: Analyze existing audit structure
        var existingAudits = await _legacyContext.UserAudits.ToListAsync();
        
        // Step 2: Convert to new audit format
        var newAudits = existingAudits.Select(ConvertLegacyAudit).ToList();
        
        // Step 3: Bulk insert new audit records
        await _newContext.BulkInsertAsync(newAudits);
        
        // Step 4: Verify data integrity
        await ValidateAuditMigration(existingAudits.Count, newAudits.Count);
    }
    
    private Audit<int, User, string> ConvertLegacyAudit(LegacyUserAudit legacyAudit)
    {
        return new Audit<int, User, string>
        {
            EntityName = "User",
            PrimaryKey = legacyAudit.UserId.ToString(),
            TrailType = ConvertAuditAction(legacyAudit.AuditAction),
            Timestamp = legacyAudit.AuditTimestamp,
            UserId = legacyAudit.ModifiedBy,
            OldValuesJson = ConvertLegacyValues(legacyAudit.OldValues),
            NewValuesJson = ConvertLegacyValues(legacyAudit.NewValues),
            ChangedColumns = ParseChangedColumns(legacyAudit.ChangedFields)
        };
    }
    
    private string ConvertLegacyValues(string legacyFormat)
    {
        // Convert from legacy XML or pipe-delimited format to JSON
        if (legacyFormat.StartsWith("<"))
        {
            return ConvertXmlToJson(legacyFormat);
        }
        else if (legacyFormat.Contains("|"))
        {
            return ConvertDelimitedToJson(legacyFormat);
        }
        
        return legacyFormat; // Already JSON or simple string
    }
}
```

### Data Migration Validation
```csharp
[Test]
public class DataMigrationValidationTests
{
    [Test]
    public async Task Should_Preserve_All_Entity_Data()
    {
        // Arrange
        var legacyUserCount = await _legacyContext.Users.CountAsync();
        var legacyAuditCount = await _legacyContext.UserAudits.CountAsync();
        
        // Act
        await _migrationService.MigrateAllDataAsync();
        
        // Assert
        var newUserCount = await _newContext.Users.CountAsync();
        var newAuditCount = await _newContext.Set<Audit<int, User, string>>().CountAsync();
        
        Assert.That(newUserCount, Is.EqualTo(legacyUserCount));
        Assert.That(newAuditCount, Is.GreaterThanOrEqualTo(legacyAuditCount));
        
        // Verify sample data integrity
        var sampleUsers = await _newContext.Users.Take(100).ToListAsync();
        foreach (var user in sampleUsers)
        {
            var legacyUser = await _legacyContext.Users.FindAsync(user.Id);
            AssertUserEquality(legacyUser, user);
        }
    }
    
    [Test]
    public async Task Should_Maintain_Performance_Characteristics()
    {
        // Test performance before and after migration
        var beforeTime = await MeasureQueryPerformance(_legacyContext);
        var afterTime = await MeasureQueryPerformance(_newContext);
        
        // New context should be same or better performance
        Assert.That(afterTime.TotalMilliseconds, Is.LessThanOrEqualTo(beforeTime.TotalMilliseconds * 1.1));
    }
}
```

**References**: [EF Integration Guide](ef-integration-guide.md) | [Troubleshooting Guide](troubleshooting-guide.md#migration-failures)

---

## Audit System Migration

### From Custom Audit Systems
```csharp
// Typical custom audit implementation
public class CustomAuditLog
{
    public int Id { get; set; }
    public string TableName { get; set; }
    public string Action { get; set; }
    public string RecordId { get; set; }
    public string OldData { get; set; }
    public string NewData { get; set; }
    public string UserId { get; set; }
    public DateTime Timestamp { get; set; }
}

// Migration to Wangkanai Audit
public class AuditMigrationService
{
    public async Task MigrateCustomAuditsAsync()
    {
        var customAudits = await _legacyContext.CustomAuditLogs.ToListAsync();
        
        var wangkanaiAudits = customAudits.Select(custom => new Audit<int, User, string>
        {
            EntityName = custom.TableName,
            PrimaryKey = custom.RecordId,
            TrailType = MapAuditAction(custom.Action),
            Timestamp = custom.Timestamp,
            UserId = custom.UserId,
            OldValuesJson = NormalizeJsonFormat(custom.OldData),
            NewValuesJson = NormalizeJsonFormat(custom.NewData),
            ChangedColumns = ExtractChangedColumns(custom.OldData, custom.NewData)
        }).ToList();
        
        await _context.BulkInsertAsync(wangkanaiAudits);
    }
    
    private AuditTrailType MapAuditAction(string action)
    {
        return action.ToUpper() switch
        {
            "INSERT" or "CREATE" => AuditTrailType.Create,
            "UPDATE" or "MODIFY" => AuditTrailType.Update,
            "DELETE" or "REMOVE" => AuditTrailType.Delete,
            _ => AuditTrailType.None
        };
    }
    
    private string NormalizeJsonFormat(string data)
    {
        if (string.IsNullOrEmpty(data)) return null;
        
        try
        {
            // Try to parse as JSON to validate format
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
            return JsonSerializer.Serialize(parsed); // Normalize format
        }
        catch
        {
            // Convert non-JSON formats
            return ConvertToJsonFormat(data);
        }
    }
}
```

### From Temporal Tables (SQL Server)
```csharp
// Migrate from SQL Server temporal tables
public class TemporalTableMigrationService
{
    public async Task MigrateFromTemporalTablesAsync()
    {
        // Query temporal table history
        var temporalHistory = await _context.Database
            .SqlQueryRaw<TemporalHistoryRecord>(@"
                SELECT 
                    u.UserId,
                    u.UserName,
                    u.Email,
                    u.ValidFrom,
                    u.ValidTo
                FROM Users FOR SYSTEM_TIME ALL u
                WHERE u.ValidTo < '9999-12-31 23:59:59.9999999'
                ORDER BY u.UserId, u.ValidFrom")
            .ToListAsync();
        
        // Group by entity and create audit records
        var auditRecords = temporalHistory
            .GroupBy(h => h.UserId)
            .SelectMany(group => CreateAuditRecordsFromHistory(group))
            .ToList();
        
        await _context.BulkInsertAsync(auditRecords);
    }
    
    private IEnumerable<Audit<int, User, string>> CreateAuditRecordsFromHistory(
        IGrouping<int, TemporalHistoryRecord> historyGroup)
    {
        var sortedHistory = historyGroup.OrderBy(h => h.ValidFrom).ToList();
        
        for (int i = 0; i < sortedHistory.Count - 1; i++)
        {
            var current = sortedHistory[i];
            var next = sortedHistory[i + 1];
            
            var changedColumns = CompareRecords(current, next);
            if (changedColumns.Any())
            {
                yield return new Audit<int, User, string>
                {
                    EntityName = "User",
                    PrimaryKey = current.UserId.ToString(),
                    TrailType = AuditTrailType.Update,
                    Timestamp = next.ValidFrom,
                    OldValuesJson = SerializeRecord(current),
                    NewValuesJson = SerializeRecord(next),
                    ChangedColumns = changedColumns
                };
            }
        }
    }
}
```

### Audit Performance Migration
```csharp
// Optimize audit performance during migration
public class PerformantAuditMigration
{
    public async Task MigrateLargeAuditDatasetAsync()
    {
        const int batchSize = 10000;
        var totalRecords = await GetTotalAuditRecordCount();
        var batches = (int)Math.Ceiling(totalRecords / (double)batchSize);
        
        for (int batch = 0; batch < batches; batch++)
        {
            var offset = batch * batchSize;
            
            // Process batch with span-based operations for efficiency
            await ProcessAuditBatch(offset, batchSize);
            
            // Progress reporting
            var progress = (batch + 1) / (double)batches;
            await ReportProgress(progress);
            
            // Memory cleanup between batches
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
    
    private async Task ProcessAuditBatch(int offset, int batchSize)
    {
        var legacyAudits = await GetLegacyAuditBatch(offset, batchSize);
        var convertedAudits = new List<Audit<int, User, string>>(legacyAudits.Count);
        
        // Use span-based operations for conversion
        Span<string> columnBuffer = stackalloc string[10]; // Pre-allocate for common case
        Span<object> valueBuffer = stackalloc object[10];
        
        foreach (var legacyAudit in legacyAudits)
        {
            var converted = ConvertWithSpanOperations(legacyAudit, columnBuffer, valueBuffer);
            convertedAudits.Add(converted);
        }
        
        // Bulk insert for performance
        await _context.BulkInsertAsync(convertedAudits);
    }
}
```

**References**: [Audit Guide](audit-guide.md) | [Performance Guide](performance-optimization-guide.md#audit-optimization)

---

## .NET Framework to .NET 9

### Framework Compatibility Assessment
```csharp
// Compatibility analyzer tool
public class FrameworkCompatibilityAnalyzer
{
    public async Task<CompatibilityReport> AnalyzeProjectAsync(string projectPath)
    {
        var project = await LoadProjectAsync(projectPath);
        
        return new CompatibilityReport
        {
            FrameworkVersion = GetCurrentFramework(project),
            PackageCompatibility = AnalyzePackageCompatibility(project.PackageReferences),
            CodeCompatibility = AnalyzeCodeCompatibility(project.SourceFiles),
            MigrationEstimate = EstimateMigrationEffort(),
            BlockingIssues = IdentifyBlockingIssues(),
            RecommendedUpgradePath = RecommendUpgradePath()
        };
    }
    
    private List<CompatibilityIssue> IdentifyBlockingIssues()
    {
        var issues = new List<CompatibilityIssue>();
        
        // Check for .NET Framework-specific dependencies
        if (UsesWebForms()) issues.Add(new("Web Forms not supported", Severity.Blocking));
        if (UsesWcf()) issues.Add(new("WCF requires porting", Severity.High));
        if (UsesSystemWeb()) issues.Add(new("System.Web dependencies", Severity.Medium));
        
        return issues;
    }
}
```

### Phased Migration Strategy
```csharp
// Phase 1: .NET Framework 4.8 → .NET 6/8 (preparation)
public class Phase1FrameworkMigration
{
    public async Task<MigrationResult> PrepareMigrationAsync()
    {
        // Step 1: Update to .NET Standard 2.0 compatible packages
        await UpdatePackagesToNetStandard();
        
        // Step 2: Remove .NET Framework-specific code
        await RemoveFrameworkDependencies();
        
        // Step 3: Add compatibility shims
        await AddCompatibilityShims();
        
        // Step 4: Test on .NET Framework 4.8
        return await ValidateOnFramework48();
    }
    
    private async Task UpdatePackagesToNetStandard()
    {
        var incompatiblePackages = new[]
        {
            "System.Web.Mvc",
            "System.Web.WebPages",
            "EntityFramework" // Replace with EntityFrameworkCore
        };
        
        var replacements = new Dictionary<string, string>
        {
            ["EntityFramework"] = "Microsoft.EntityFrameworkCore",
            ["System.Web.Mvc"] = "Microsoft.AspNetCore.Mvc",
            ["Newtonsoft.Json"] = "System.Text.Json" // Optional but recommended
        };
        
        foreach (var package in incompatiblePackages)
        {
            if (replacements.ContainsKey(package))
            {
                await ReplacePackage(package, replacements[package]);
            }
        }
    }
}

// Phase 2: .NET 6/8 → .NET 9 with Wangkanai Domain
public class Phase2DomainMigration
{
    public async Task<MigrationResult> MigrateToDomainLibraryAsync()
    {
        // Step 1: Install Wangkanai packages
        await InstallWangkanaiPackages();
        
        // Step 2: Migrate Entity Framework
        await MigrateEntityFramework();
        
        // Step 3: Introduce domain patterns
        await IntroduceDomainPatterns();
        
        // Step 4: Enable performance optimizations
        await EnablePerformanceOptimizations();
        
        return await ValidateMigration();
    }
}
```

### Entity Framework Core Migration
```csharp
// From Entity Framework 6.x to EF Core with Wangkanai Domain
public class EFCoreMigrationHelper
{
    public async Task MigrateFromEF6Async()
    {
        // Step 1: Generate EF Core context from existing database
        await ScaffoldExistingDatabase();
        
        // Step 2: Replace scaffolded context with AuditDbContext
        await ReplaceWithAuditDbContext();
        
        // Step 3: Migrate entities to domain patterns
        await MigrateEntitiesToDomainPatterns();
        
        // Step 4: Handle EF6-specific features
        await HandleEF6SpecificFeatures();
    }
    
    private async Task HandleEF6SpecificFeatures()
    {
        // Complex types → Owned entities
        await MigrateComplexTypes();
        
        // Stored procedures → Raw SQL or functions
        await MigrateStoredProcedures();
        
        // Custom conventions → EF Core conventions
        await MigrateConventions();
    }
    
    private async Task MigrateComplexTypes()
    {
        // EF6 Complex Type
        // public class Address
        // {
        //     public string Street { get; set; }
        //     public string City { get; set; }
        // }

        // EF Core + Domain: Value Object
        /*
        public class Address : ValueObject
        {
            public string Street { get; }
            public string City { get; }
            
            public Address(string street, string city)
            {
                Street = street;
                City = city;
            }
            
            protected override IEnumerable<object> GetEqualityComponents()
            {
                yield return Street;
                yield return City;
            }
        }
        
        // Configuration
        modelBuilder.Entity<Customer>()
            .OwnsOne(c => c.Address);
        */
    }
}
```

### Configuration Migration
```csharp
// Migrate from web.config to appsettings.json
public class ConfigurationMigrationService
{
    public async Task MigrateConfigurationAsync()
    {
        var webConfig = LoadWebConfig();
        var appSettings = ConvertToAppSettings(webConfig);
        
        // Database connections
        appSettings["ConnectionStrings:DefaultConnection"] = 
            webConfig.ConnectionStrings["DefaultConnection"].ConnectionString;
        
        // App settings
        foreach (var setting in webConfig.AppSettings.AllKeys)
        {
            appSettings[$"AppSettings:{setting}"] = webConfig.AppSettings[setting];
        }
        
        // Domain-specific configuration
        appSettings["Wangkanai:Domain:EnablePerformanceOptimization"] = "true";
        appSettings["Wangkanai:Audit:RetentionDays"] = "365";
        appSettings["Wangkanai:Audit:EnableUserAttribution"] = "true";
        
        await SaveAppSettings(appSettings);
    }
    
    // Startup configuration
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        // Wangkanai Domain services
        services.AddWangkanaiDomain(options =>
        {
            options.EnablePerformanceOptimization = true;
            options.CacheMaxSize = 1000;
        });
        
        // Audit services
        services.AddWangkanaiAudit(options =>
        {
            options.RetentionDays = 365;
            options.EnableUserAttribution = true;
        });
    }
}
```

**References**: [Integration Guide](integration-guide.md#net-core-migration) | [Performance Guide](performance-optimization-guide.md)

---

## Migration Testing Strategy

### Automated Migration Testing
```csharp
[TestFixture]
public class MigrationTestSuite
{
    [Test]
    public async Task Should_Preserve_Data_Integrity()
    {
        // Arrange: Baseline data
        var baselineData = await CaptureBaselineData();
        
        // Act: Run migration
        await _migrationService.ExecuteMigrationAsync();
        
        // Assert: Verify data integrity
        var migratedData = await CapturePostMigrationData();
        await AssertDataIntegrity(baselineData, migratedData);
    }
    
    [Test]
    public async Task Should_Maintain_Performance_SLA()
    {
        // Arrange: Performance baseline
        var baseline = await MeasurePerformanceBaseline();
        
        // Act: Run migration
        await _migrationService.ExecuteMigrationAsync();
        
        // Assert: Performance should improve or remain same
        var postMigration = await MeasurePerformancePostMigration();
        
        Assert.That(postMigration.AverageResponseTime, 
            Is.LessThanOrEqualTo(baseline.AverageResponseTime * 1.1)); // Allow 10% variance
        Assert.That(postMigration.ThroughputPerSecond,
            Is.GreaterThanOrEqualTo(baseline.ThroughputPerSecond * 0.9)); // Allow 10% variance
    }
    
    [Test]
    public async Task Should_Enable_New_Domain_Features()
    {
        // Act: Run migration
        await _migrationService.ExecuteMigrationAsync();
        
        // Assert: New features available
        var entityStats = Entity<Guid>.GetPerformanceStats();
        Assert.That(entityStats.HitRatio, Is.GreaterThan(0));
        
        var auditService = _serviceProvider.GetService<IAuditService>();
        Assert.That(auditService, Is.Not.Null);
        
        var domainEvents = _serviceProvider.GetService<IDomainHandler<TestDomainEvent>>();
        Assert.That(domainEvents, Is.Not.Null);
    }
}

// Performance comparison testing
public class PerformanceComparisonTests
{
    [Test]
    public async Task Compare_Entity_Performance_Before_After()
    {
        // Test entity operations before migration (baseline)
        var beforeMigration = await BenchmarkEntityOperations(_legacyContext);
        
        // Test entity operations after migration
        var afterMigration = await BenchmarkEntityOperations(_domainContext);
        
        // Results should show improvement
        Assert.That(afterMigration.EqualsOperationsPerSecond,
            Is.GreaterThan(beforeMigration.EqualsOperationsPerSecond));
        Assert.That(afterMigration.HashCodeOperationsPerSecond,
            Is.GreaterThan(beforeMigration.HashCodeOperationsPerSecond));
        
        _logger.LogInformation("Performance improvement: {ImprovementRatio:P2}",
            afterMigration.EqualsOperationsPerSecond / beforeMigration.EqualsOperationsPerSecond);
    }
}
```

### Rollback Strategy
```csharp
public class MigrationRollbackService
{
    public async Task<RollbackPlan> CreateRollbackPlanAsync()
    {
        return new RollbackPlan
        {
            DatabaseBackup = await CreateDatabaseBackup(),
            CodeCommit = GetCurrentCodeCommit(),
            ConfigurationBackup = await BackupConfiguration(),
            DependencyVersions = GetDependencyVersions(),
            RollbackSteps = GenerateRollbackSteps()
        };
    }
    
    public async Task ExecuteRollbackAsync(RollbackPlan plan)
    {
        try
        {
            // Step 1: Restore database
            await RestoreDatabase(plan.DatabaseBackup);
            
            // Step 2: Revert code changes
            await RevertCodeChanges(plan.CodeCommit);
            
            // Step 3: Restore configuration
            await RestoreConfiguration(plan.ConfigurationBackup);
            
            // Step 4: Downgrade dependencies
            await RestoreDependencyVersions(plan.DependencyVersions);
            
            // Step 5: Validate rollback
            await ValidateRollback();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rollback failed: {Error}", ex.Message);
            throw new RollbackFailedException("Rollback procedure failed", ex);
        }
    }
}
```

## Migration Checklist

### Pre-Migration Checklist
- [ ] **Backup Strategy**: Complete database and application backups
- [ ] **Dependency Analysis**: Identify all dependencies and their migration requirements
- [ ] **Performance Baseline**: Establish current performance metrics
- [ ] **Test Environment**: Set up isolated migration testing environment
- [ ] **Rollback Plan**: Prepare complete rollback procedures
- [ ] **Team Training**: Ensure team understands new domain patterns
- [ ] **Migration Timeline**: Realistic timeline with buffer for issues

### During Migration Checklist
- [ ] **Gradual Migration**: Migrate entities one at a time
- [ ] **Data Validation**: Verify data integrity after each step
- [ ] **Performance Monitoring**: Monitor performance throughout migration
- [ ] **Error Handling**: Log and handle migration errors appropriately
- [ ] **Progress Tracking**: Track migration progress and remaining work
- [ ] **Communication**: Keep stakeholders informed of progress

### Post-Migration Checklist
- [ ] **Functionality Testing**: Comprehensive testing of all features
- [ ] **Performance Validation**: Confirm performance improvements
- [ ] **Audit Trail Testing**: Verify audit functionality works correctly
- [ ] **Integration Testing**: Test all external integrations
- [ ] **User Acceptance**: Validate user-facing functionality
- [ ] **Documentation Update**: Update all relevant documentation
- [ ] **Team Knowledge Transfer**: Ensure team understands new patterns
- [ ] **Monitoring Setup**: Configure production monitoring

## Support and Resources

### Migration Support
- **[FAQ](faq.md#migration-support)**: Common migration questions
- **[Troubleshooting Guide](troubleshooting-guide.md)**: Migration-specific issues
- **[Integration Guide](integration-guide.md)**: Framework integration patterns
- **[Performance Guide](performance-optimization-guide.md)**: Optimization strategies

### Professional Services
- **Migration Assessment**: Professional evaluation of migration complexity
- **Migration Planning**: Detailed migration roadmap and timeline
- **Migration Execution**: Guided migration with expert support
- **Training Services**: Team training on domain patterns and best practices

---

*Last Updated: 2025-01-15 | Next Review: Q2 2025*