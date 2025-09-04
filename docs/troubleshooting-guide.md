# Troubleshooting Guide

**Comprehensive problem resolution guide for the Wangkanai Domain library**

---

## Quick Problem Resolution

### Emergency Checklist
1. **Performance Issues**: [Performance Problems](#performance-issues) → Check cache hit ratios and optimization status
2. **Serialization Errors**: [Serialization Issues](#serialization-issues) → Verify JSON format and circular references
3. **Entity Framework**: [EF Integration Issues](#entity-framework-integration-issues) → Check context configuration and migrations
4. **Compilation Failures**: [Optimization Issues](#optimization-issues) → Review fallback mechanisms and error logs
5. **Audit Problems**: [Audit Trail Issues](#audit-trail-issues) → Validate audit configuration and user context

### Diagnostic Tools
```csharp
// Performance diagnostics
var entityStats = Entity<Guid>.GetPerformanceStats();
Console.WriteLine($"Entity cache hit ratio: {entityStats.HitRatio:P2}");

// Value object optimization status
var voStats = ValueObjectDiagnostics.GetOptimizationStats();
Console.WriteLine($"Value object optimizations: {voStats.OptimizedTypes}/{voStats.TotalTypes}");

// Clear caches for testing
Entity<Guid>.ClearTypeCache();
```

---

## Performance Issues

### Problem: Poor Entity Equality Performance

**Symptoms**:
- Slow entity comparisons and lookups
- High CPU usage during entity operations
- Low cache hit ratios (<90%)

**Root Causes & Solutions**:

1. **Type Cache Not Warming Up**
   ```csharp
   // Check cache statistics
   var stats = Entity<Guid>.GetPerformanceStats();
   if (stats.HitRatio < 0.9 && stats.Hits + stats.Misses > 1000)
   {
       // Cache should be warmed up by now - investigate
       // Check for type diversity or proxy issues
   }
   ```

2. **EF Core Proxy Issues**
   ```csharp
   // Disable proxy creation if causing issues
   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
       optionsBuilder.UseLazyLoadingProxies(false); // Disable if problematic
       optionsBuilder.UseChangeTrackingProxies(false);
   }
   ```

3. **Memory Pressure Causing Evictions**
   ```csharp
   // Monitor cache size and evictions
   private static void MonitorCachePerformance()
   {
       var timer = new Timer(_ =>
       {
           var stats = Entity<Guid>.GetPerformanceStats();
           if (stats.Misses > previousMisses * 1.1) // 10% increase
           {
               _logger.LogWarning("Cache miss rate increasing - possible memory pressure");
           }
       }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
   }
   ```

**Resolution Steps**:
1. Monitor cache hit ratios in production
2. Disable EF proxy creation if necessary
3. Increase cache size limit if memory allows
4. Review entity usage patterns for excessive type diversity

**References**: [Entity.cs:162-182](../src/Domain/Entity.cs) | [Performance Guide](performance-optimization-guide.md#entity-performance)

---

### Problem: Value Object Performance Degradation

**Symptoms**:
- Slow value object equality comparisons
- High memory allocation during value object operations
- Compilation errors in logs

**Root Causes & Solutions**:

1. **Optimization Disabled for Types**
   ```csharp
   // Check optimization status
   public static bool IsOptimizationEnabled(Type type)
   {
       return _optimizationEnabled.GetOrAdd(type, _ => true);
   }
   
   // Re-enable optimization if safe
   public static void ResetOptimization(Type type)
   {
       _optimizationEnabled.TryUpdate(type, true, false);
   }
   ```

2. **Complex Property Types**
   ```csharp
   // Types that can't be optimized
   public class ComplexValueObject : ValueObject
   {
       // ❌ Interface properties prevent optimization
       public IEnumerable<string> Items { get; }
       
       // ✅ Concrete types work better
       public List<string> Items { get; }
       
       // ❌ Custom interfaces
       public ICustomInterface Custom { get; }
       
       // ✅ Simple properties optimize well
       public string Name { get; }
       public int Count { get; }
   }
   ```

3. **Security Policy Restrictions**
   ```csharp
   // Handle compilation failures gracefully
   try
   {
       var accessor = GetOrCreateCompiledAccessor(type);
       return accessor(this);
   }
   catch (SecurityException ex)
   {
       _logger.LogWarning("Compilation disabled by security policy: {Error}", ex.Message);
       DisableOptimization(type);
       return GetEqualityComponentsReflection();
   }
   ```

**Resolution Steps**:
1. Review property types in slow value objects
2. Check application security policies
3. Monitor optimization success rates
4. Consider manual implementation for performance-critical types

**Performance Testing**:
```csharp
[Test]
public void ValueObjectPerformanceTest()
{
    var vo1 = new TestValueObject("test", 42, DateTime.Now);
    var vo2 = new TestValueObject("test", 42, DateTime.Now);
    
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < 100000; i++)
    {
        _ = vo1.Equals(vo2);
    }
    sw.Stop();
    
    // Should be under 50ms for 100K operations
    Assert.That(sw.ElapsedMilliseconds, Is.LessThan(50));
}
```

**References**: [ValueObject.cs:151-307](../src/Domain/ValueObject.cs) | [Performance Benchmarks](../benchmark/Domain/ValueObjectPerformanceBenchmark.cs)

---

### Problem: High Memory Usage

**Symptoms**:
- Increasing memory usage over time
- Frequent garbage collection
- OutOfMemoryException in long-running applications

**Root Causes & Solutions**:

1. **Unbounded Cache Growth**
   ```csharp
   // Monitor cache sizes
   private static void MonitorMemoryUsage()
   {
       var process = Process.GetCurrentProcess();
       var workingSet = process.WorkingSet64;
       
       // Check if memory growth correlates with cache usage
       var entityCacheSize = GetEntityCacheSize();
       var voAccessorCount = GetCompiledAccessorCount();
       
       _logger.LogInformation("Memory: {WorkingSet}MB, Entity Cache: {EntityCache}, VO Accessors: {VOAccessors}", 
           workingSet / 1024 / 1024, entityCacheSize, voAccessorCount);
   }
   ```

2. **Compiled Accessor Leaks**
   ```csharp
   // Implement cache cleanup for long-running applications
   public static void CleanupOptimizationCaches()
   {
       // Clear optimization caches periodically
       Entity<Guid>.ClearTypeCache();
       ValueObjectOptimizations.ClearAccessorCache();
       
       // Force garbage collection after cleanup
       GC.Collect();
       GC.WaitForPendingFinalizers();
       GC.Collect();
   }
   ```

3. **Audit Trail Accumulation**
   ```csharp
   // Implement audit cleanup strategy
   public async Task CleanupOldAudits()
   {
       var cutoffDate = DateTime.UtcNow.AddDays(-90);
       
       // Batch delete old audit records
       await _context.Database.ExecuteSqlRawAsync(
           "DELETE FROM Audits WHERE Timestamp < {0}", cutoffDate);
   }
   ```

**Resolution Steps**:
1. Implement memory monitoring and alerting
2. Set up periodic cache cleanup for long-running applications
3. Implement audit record cleanup policies
4. Profile memory usage patterns in production

**References**: [Entity.cs:176-182](../src/Domain/Entity.cs) | [Performance Guide](performance-optimization-guide.md#memory-management)

---

## Serialization Issues

### Problem: JSON Serialization Errors in Audit Trails

**Symptoms**:
- JsonException during audit trail creation
- Incomplete or corrupted audit data
- Circular reference errors

**Root Causes & Solutions**:

1. **Circular References in Entity Relationships**
   ```csharp
   // ❌ Problematic circular reference
   public class Order : AuditableEntity<int>
   {
       public List<OrderItem> Items { get; set; } = new();
   }
   
   public class OrderItem : Entity<int>
   {
       public Order Order { get; set; } = null!; // Circular reference
   }
   
   // ✅ Solution: Use ID references
   public class OrderItem : Entity<int>
   {
       public int OrderId { get; set; } // Reference by ID only
   }
   ```

2. **Complex Type Serialization**
   ```csharp
   // Configure JSON options for audit serialization
   services.Configure<JsonOptions>(options =>
   {
       options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
       options.SerializerOptions.MaxDepth = 32;
       options.SerializerOptions.WriteIndented = false; // Compact audit storage
   });
   
   // Custom audit serialization
   public class CustomAuditService
   {
       private static readonly JsonSerializerOptions _auditOptions = new()
       {
           ReferenceHandler = ReferenceHandler.IgnoreCycles,
           DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
       };
       
       public string SerializeAuditValue(object value)
       {
           return JsonSerializer.Serialize(value, _auditOptions);
       }
   }
   ```

3. **Large Object Serialization**
   ```csharp
   // Implement size limits for audit values
   public void SetAuditValues(Dictionary<string, object> oldValues, Dictionary<string, object> newValues)
   {
       const int maxSize = 64 * 1024; // 64KB limit
       
       var oldJson = JsonSerializer.Serialize(oldValues);
       if (oldJson.Length > maxSize)
       {
           // Truncate or summarize large values
           oldJson = TruncateAuditData(oldValues, maxSize);
       }
       
       OldValuesJson = oldJson;
       // ... similar for new values
   }
   ```

**Resolution Steps**:
1. Review entity relationships for circular references
2. Configure JSON serialization options appropriately
3. Implement size limits for audit data
4. Add error handling for serialization failures

**Testing Serialization**:
```csharp
[Test]
public void AuditSerializationTest()
{
    var audit = new Audit<int, User, string>
    {
        EntityName = "Product",
        PrimaryKey = "123",
        TrailType = AuditTrailType.Update
    };
    
    var changes = new Dictionary<string, object>
    {
        ["Name"] = "Test Product",
        ["Price"] = 99.99m,
        ["LastModified"] = DateTime.UtcNow
    };
    
    // Should not throw exceptions
    Assert.DoesNotThrow(() => audit.OldValues = changes);
    Assert.DoesNotThrow(() => audit.NewValues = changes);
    
    // Verify data integrity
    Assert.That(audit.GetOldValue("Name"), Is.EqualTo("Test Product"));
}
```

**References**: [Audit.cs:52-215](../src/Audit/Audit.cs) | [FAQ](faq.md#debugging-serialization-issues)

---

### Problem: Value Object Serialization Issues

**Symptoms**:
- Value objects don't serialize properly to JSON
- Missing properties in serialized output
- Deserialization creates different objects

**Root Causes & Solutions**:

1. **Constructor Parameter Mismatch**
   ```csharp
   // ❌ Problem: Property names don't match constructor parameters
   public class Money : ValueObject
   {
       public decimal Amount { get; }
       public string CurrencyCode { get; } // Different name
       
       public Money(decimal amount, string currency) // Parameter name mismatch
       {
           Amount = amount;
           CurrencyCode = currency;
       }
   }
   
   // ✅ Solution: Match names or use JsonPropertyName
   public class Money : ValueObject
   {
       public decimal Amount { get; }
       
       [JsonPropertyName("currency")]
       public string CurrencyCode { get; }
       
       public Money(decimal amount, [JsonPropertyName("currency")] string currencyCode)
       {
           Amount = amount;
           CurrencyCode = currencyCode;
       }
   }
   ```

2. **Missing Parameterless Constructor**
   ```csharp
   // ✅ Solution: Custom JSON converter for value objects
   public class ValueObjectJsonConverter<T> : JsonConverter<T> where T : ValueObject
   {
       public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
       {
           using var document = JsonDocument.ParseValue(ref reader);
           var properties = GetValueObjectProperties(typeToConvert);
           var constructorArgs = ExtractConstructorArguments(document, properties);
           
           return (T)Activator.CreateInstance(typeToConvert, constructorArgs);
       }
       
       public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
       {
           var properties = value.GetProperties();
           writer.WriteStartObject();
           
           foreach (var property in properties)
           {
               var propertyValue = property.GetValue(value);
               writer.WritePropertyName(property.Name);
               JsonSerializer.Serialize(writer, propertyValue, options);
           }
           
           writer.WriteEndObject();
       }
   }
   ```

3. **Complex Value Object Serialization**
   ```csharp
   // Configure custom converters globally
   services.Configure<JsonOptions>(options =>
   {
       options.SerializerOptions.Converters.Add(new MoneyJsonConverter());
       options.SerializerOptions.Converters.Add(new AddressJsonConverter());
   });
   
   // Or use attributes for specific types
   [JsonConverter(typeof(MoneyJsonConverter))]
   public class Money : ValueObject
   {
       // Implementation
   }
   ```

**Resolution Steps**:
1. Ensure constructor parameters match property names
2. Implement custom JSON converters for complex value objects
3. Test serialization round-trips
4. Configure converters globally or per-type as needed

**References**: [ValueObject.cs:22-308](../src/Domain/ValueObject.cs) | [Integration Guide](integration-guide.md#json-serialization)

---

## Entity Framework Integration Issues

### Problem: Migration Failures

**Symptoms**:
- Migration generation fails
- Database schema doesn't match entity definitions
- Constraint violation errors during SaveChanges

**Root Causes & Solutions**:

1. **Missing Base Class Configuration**
   ```csharp
   // ❌ Problem: Not calling base.OnModelCreating
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       // Missing base call - audit configuration won't be applied
       modelBuilder.Entity<User>().HasIndex(u => u.Email);
   }
   
   // ✅ Solution: Always call base configuration
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder); // Essential for audit configuration
       
       // Custom configurations
       modelBuilder.Entity<User>().HasIndex(u => u.Email);
   }
   ```

2. **Generic ID Type Configuration**
   ```csharp
   // Configure custom ID types properly
   public class ProductConfiguration : IEntityTypeConfiguration<Product>
   {
       public void Configure(EntityTypeBuilder<Product> builder)
       {
           // ❌ Problem: Not configuring ID conversion
           // builder.Property(p => p.Id); // Uses default mapping
           
           // ✅ Solution: Explicit ID conversion
           builder.Property(p => p.Id)
               .HasConversion(
                   id => id.Value,        // To database
                   value => new ProductId(value) // From database
               );
       }
   }
   ```

3. **Audit Table Configuration Issues**
   ```csharp
   // Ensure audit tables are properly configured
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       
       // Explicit audit table configuration if needed
       modelBuilder.Entity<Audit<int, User, string>>(entity =>
       {
           entity.ToTable("AuditTrails");
           entity.Property(e => e.OldValuesJson).HasColumnType("nvarchar(max)");
           entity.Property(e => e.NewValuesJson).HasColumnType("nvarchar(max)");
           
           // Index for query performance
           entity.HasIndex(e => new { e.EntityName, e.PrimaryKey });
           entity.HasIndex(e => e.Timestamp);
       });
   }
   ```

**Resolution Steps**:
1. Always call `base.OnModelCreating()` in derived contexts
2. Configure custom ID type conversions explicitly
3. Review generated migrations before applying
4. Test database operations thoroughly

**Migration Testing**:
```csharp
[Test]
public async Task MigrationIntegrityTest()
{
    using var context = new TestDbContext();
    await context.Database.EnsureDeletedAsync();
    await context.Database.MigrateAsync();
    
    // Test entity creation
    var user = new User { Name = "Test", Email = "test@example.com" };
    context.Users.Add(user);
    await context.SaveChangesAsync();
    
    // Verify audit trail creation
    var audit = await context.Set<Audit<int, User, string>>()
        .FirstOrDefaultAsync(a => a.EntityName == "User");
    
    Assert.That(audit, Is.Not.Null);
    Assert.That(audit.TrailType, Is.EqualTo(AuditTrailType.Create));
}
```

**References**: [AuditDbContext.cs:12-20](../src/EntityFramework/AuditDbContext.cs) | [Integration Guide](integration-guide.md#ef-core-configuration)

---

### Problem: Audit Trails Not Being Created

**Symptoms**:
- Audit records not appearing in database
- SaveChanges completes but no audit entries
- Missing Created/Updated timestamps

**Root Causes & Solutions**:

1. **Context Not Inheriting from AuditDbContext**
   ```csharp
   // ❌ Problem: Using plain DbContext
   public class ApplicationDbContext : DbContext
   {
       // Audit functionality won't work
   }
   
   // ✅ Solution: Inherit from AuditDbContext
   public class ApplicationDbContext : AuditDbContext
   {
       // Audit functionality automatically enabled
   }
   ```

2. **Missing Audit Entity Configuration**
   ```csharp
   // Ensure audit entity is included in context
   public class ApplicationDbContext : AuditDbContext
   {
       public DbSet<User> Users { get; set; }
       
       // ✅ Add audit DbSet if needed
       public DbSet<Audit<int, User, string>> UserAudits { get; set; }
   }
   ```

3. **Audit Context Not Properly Configured**
   ```csharp
   // Configure audit context with user information
   public class AuditService
   {
       private readonly ApplicationDbContext _context;
       private readonly IHttpContextAccessor _httpContextAccessor;
       
       public async Task<int> SaveChangesAsync()
       {
           // Set user context for audit trails
           var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
           
           // Configure audit entries before saving
           foreach (var entry in _context.ChangeTracker.Entries<IAuditable>())
           {
               if (entry.State == EntityState.Added)
               {
                   entry.Entity.Created = DateTime.UtcNow;
               }
               else if (entry.State == EntityState.Modified)
               {
                   entry.Entity.Updated = DateTime.UtcNow;
               }
           }
           
           return await _context.SaveChangesAsync();
       }
   }
   ```

**Resolution Steps**:
1. Verify context inherits from AuditDbContext
2. Check audit entity configuration in OnModelCreating
3. Implement user context for audit attribution
4. Test audit creation with unit tests

**Debugging Audit Creation**:
```csharp
[Test]
public async Task AuditCreationDebugging()
{
    using var context = new TestDbContext();
    
    var user = new User { Name = "Test User", Email = "test@test.com" };
    context.Users.Add(user);
    
    // Check change tracker before save
    var entries = context.ChangeTracker.Entries().ToList();
    Console.WriteLine($"Tracked entries: {entries.Count}");
    
    foreach (var entry in entries)
    {
        Console.WriteLine($"Entity: {entry.Entity.GetType().Name}, State: {entry.State}");
    }
    
    await context.SaveChangesAsync();
    
    // Verify audit creation
    var audits = await context.Set<Audit<int, User, string>>().ToListAsync();
    Console.WriteLine($"Audit records created: {audits.Count}");
}
```

**References**: [AuditDbContext.cs:12-20](../src/EntityFramework/AuditDbContext.cs) | [FAQ](faq.md#audit-trails)

---

## Optimization Issues

### Problem: Compilation Failures

**Symptoms**:
- Value object operations slower than expected
- Compilation error messages in logs
- Fallback to reflection for all types

**Root Causes & Solutions**:

1. **Security Policy Restrictions**
   ```csharp
   // Check for security policy issues
   try
   {
       var testExpression = Expression.Constant(42);
       var testLambda = Expression.Lambda(testExpression).Compile();
       // If this fails, compilation is blocked
   }
   catch (SecurityException ex)
   {
       _logger.LogWarning("Expression compilation blocked by security policy: {Error}", ex.Message);
       // Implement policy configuration or accept reflection fallback
   }
   ```

2. **Complex Property Types**
   ```csharp
   // Identify problematic property types
   private static bool ShouldSkipOptimization(Type propertyType)
   {
       // Current implementation
       return propertyType.IsInterface &&
              propertyType != typeof(string) &&
              typeof(IEnumerable).IsAssignableFrom(propertyType);
   }
   
   // Extended diagnostic version
   private static bool DiagnosePropertyType(Type propertyType)
   {
       var reasons = new List<string>();
       
       if (propertyType.IsInterface && propertyType != typeof(string))
           reasons.Add("Interface type");
       
       if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
           reasons.Add("Enumerable type");
       
       if (propertyType.IsGenericType && !propertyType.IsGenericTypeDefinition)
           reasons.Add("Generic type");
       
       if (reasons.Any())
       {
           _logger.LogDebug("Skipping optimization for {Type}: {Reasons}", 
               propertyType.Name, string.Join(", ", reasons));
           return true;
       }
       
       return false;
   }
   ```

3. **Memory or Resource Constraints**
   ```csharp
   // Handle resource constraints during compilation
   private static Func<object, object?[]> SafeCompileAccessor(Type type)
   {
       try
       {
           return BuildCompiledAccessor(type);
       }
       catch (OutOfMemoryException)
       {
           _logger.LogWarning("Insufficient memory for compiling accessor for {Type}", type.Name);
           DisableOptimization(type);
           throw;
       }
       catch (InvalidOperationException ex) when (ex.Message.Contains("Complex properties"))
       {
           _logger.LogDebug("Complex properties detected in {Type}: {Error}", type.Name, ex.Message);
           DisableOptimization(type);
           throw;
       }
   }
   ```

**Resolution Steps**:
1. Review application security policies for expression compilation
2. Analyze property types in slow value objects
3. Monitor compilation success rates
4. Consider manual optimization for critical types

**Compilation Diagnostics**:
```csharp
public static class OptimizationDiagnostics
{
    public static Dictionary<string, object> GetDiagnosticInfo()
    {
        return new Dictionary<string, object>
        {
            ["OptimizedTypes"] = _compiledAccessors.Count,
            ["DisabledTypes"] = _optimizationEnabled.Count(kvp => !kvp.Value),
            ["TotalTypes"] = _optimizationEnabled.Count,
            ["MemoryUsage"] = GC.GetTotalMemory(false),
            ["CompilationErrors"] = _compilationErrors.Count
        };
    }
}
```

**References**: [ValueObject.cs:185-262](../src/Domain/ValueObject.cs) | [Design Decisions](design-decisions.md#adr-005-intelligent-optimization-fallbacks)

---

## Audit Trail Issues

### Problem: Missing User Attribution in Audits

**Symptoms**:
- Audit records have null UserId
- Cannot track who made changes
- User information not populated

**Root Causes & Solutions**:

1. **Missing HTTP Context Configuration**
   ```csharp
   // Configure HTTP context accessor
   services.AddHttpContextAccessor();
   
   // Custom audit service with user context
   public class AuditService : IAuditService
   {
       private readonly IHttpContextAccessor _httpContextAccessor;
       private readonly IUserService _userService;
       
       public string GetCurrentUserId()
       {
           var httpContext = _httpContextAccessor.HttpContext;
           return httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       }
       
       public async Task<User> GetCurrentUserAsync()
       {
           var userId = GetCurrentUserId();
           return userId != null 
               ? await _userService.GetByIdAsync(userId)
               : null;
       }
   }
   ```

2. **Audit Context Override**
   ```csharp
   public class ApplicationDbContext : AuditDbContext
   {
       private readonly IAuditService _auditService;
       
       public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
       {
           // Set user information before saving
           var currentUser = await _auditService.GetCurrentUserAsync();
           
           foreach (var entry in ChangeTracker.Entries<IAuditable>())
           {
               if (entry.State == EntityState.Added)
               {
                   entry.Entity.Created = DateTime.UtcNow;
                   if (entry.Entity is IUserAuditable userAuditable)
                   {
                       userAuditable.CreatedBy = currentUser?.Id;
                   }
               }
               // Similar for Updated
           }
           
           return await base.SaveChangesAsync(cancellationToken);
       }
   }
   ```

3. **Background Task Scenarios**
   ```csharp
   // Handle audit attribution in background tasks
   public class BackgroundAuditService
   {
       public async Task ProcessInBackgroundContext(string systemUserId)
       {
           using var scope = _serviceProvider.CreateScope();
           var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
           
           // Set system user context
           context.SetAuditContext(systemUserId, "SYSTEM");
           
           // Perform operations
           await ProcessDataAsync(context);
       }
   }
   
   // Extension for manual audit context
   public static class DbContextExtensions
   {
       public static void SetAuditContext(this DbContext context, string userId, string source)
       {
           context.ChangeTracker.StateChanged += (sender, args) =>
           {
               if (args.Entry.Entity is IAuditable auditable)
               {
                   auditable.AuditUserId = userId;
                   auditable.AuditSource = source;
               }
           };
       }
   }
   ```

**Resolution Steps**:
1. Configure HTTP context accessor in dependency injection
2. Implement audit service to get current user context
3. Override SaveChanges methods to set user information
4. Handle background task scenarios appropriately

**Testing User Attribution**:
```csharp
[Test]
public async Task AuditUserAttributionTest()
{
    // Mock HTTP context with user
    var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
        new Claim(ClaimTypes.Name, "Test User")
    }));
    
    var httpContext = new Mock<HttpContext>();
    httpContext.Setup(x => x.User).Returns(user);
    
    var httpContextAccessor = new Mock<IHttpContextAccessor>();
    httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
    
    // Test audit creation with user context
    using var context = new TestDbContext(httpContextAccessor.Object);
    
    var entity = new TestEntity { Name = "Test" };
    context.TestEntities.Add(entity);
    await context.SaveChangesAsync();
    
    var audit = await context.Audits.FirstAsync();
    Assert.That(audit.UserId, Is.EqualTo("test-user-id"));
}
```

**References**: [Audit.cs:19-26](../src/Audit/Audit.cs) | [Integration Guide](integration-guide.md#user-attribution)

---

### Problem: Large Audit Records Causing Performance Issues

**Symptoms**:
- Slow SaveChanges operations
- Large database sizes
- Memory issues with audit queries

**Root Causes & Solutions**:

1. **Unlimited Audit Data Size**
   ```csharp
   // Implement audit data size limits
   public class SizeLimitedAudit : Audit<int, User, string>
   {
       private const int MaxValueSize = 64 * 1024; // 64KB
       
       public override void SetValues(Dictionary<string, object> oldValues, Dictionary<string, object> newValues)
       {
           // Truncate large values
           var truncatedOld = TruncateValues(oldValues);
           var truncatedNew = TruncateValues(newValues);
           
           base.SetValues(truncatedOld, truncatedNew);
       }
       
       private Dictionary<string, object> TruncateValues(Dictionary<string, object> values)
       {
           var result = new Dictionary<string, object>();
           
           foreach (var kvp in values)
           {
               var valueStr = kvp.Value?.ToString() ?? "";
               if (valueStr.Length > 1000) // Individual value limit
               {
                   result[kvp.Key] = valueStr.Substring(0, 997) + "...";
               }
               else
               {
                   result[kvp.Key] = kvp.Value;
               }
           }
           
           return result;
       }
   }
   ```

2. **Selective Audit Fields**
   ```csharp
   // Implement field-level audit configuration
   public class SelectiveAuditableEntity<T> : AuditableEntity<T>
       where T : IComparable<T>, IEquatable<T>
   {
       [AuditIgnore]
       public byte[] LargeData { get; set; } = Array.Empty<byte>();
       
       [AuditSummary] // Custom attribute for summary-only auditing
       public string LargeText { get; set; } = string.Empty;
   }
   
   // Custom audit processing
   public class CustomAuditProcessor
   {
       public AuditEntry ProcessEntity(EntityEntry entry)
       {
           var auditableProperties = entry.Properties
               .Where(p => !p.Metadata.ClrType.GetProperty(p.Metadata.Name)
                   ?.GetCustomAttribute<AuditIgnoreAttribute>() != null)
               .ToList();
           
           // Process only auditable properties
           // ...
       }
   }
   ```

3. **Audit Archival Strategy**
   ```csharp
   // Implement audit archival
   public class AuditArchivalService
   {
       public async Task ArchiveOldAudits(TimeSpan retention)
       {
           var cutoffDate = DateTime.UtcNow.Subtract(retention);
           
           // Move old audits to archive table
           await _context.Database.ExecuteSqlRawAsync(@"
               INSERT INTO AuditArchive 
               SELECT * FROM Audits 
               WHERE Timestamp < {0}", cutoffDate);
           
           // Delete archived records
           await _context.Database.ExecuteSqlRawAsync(
               "DELETE FROM Audits WHERE Timestamp < {0}", cutoffDate);
       }
   }
   
   // Background service for regular archival
   public class AuditArchivalBackgroundService : BackgroundService
   {
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               await _archivalService.ArchiveOldAudits(TimeSpan.FromDays(90));
               await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
           }
       }
   }
   ```

**Resolution Steps**:
1. Implement size limits for audit data
2. Configure selective auditing for large entities
3. Set up audit archival and cleanup processes
4. Monitor audit table sizes and query performance

**Performance Monitoring**:
```csharp
public class AuditPerformanceMonitor
{
    public async Task<AuditMetrics> GetMetricsAsync()
    {
        return new AuditMetrics
        {
            TotalAuditRecords = await _context.Audits.CountAsync(),
            AverageRecordSize = await _context.Audits
                .Select(a => (a.OldValuesJson ?? "").Length + (a.NewValuesJson ?? "").Length)
                .AverageAsync(),
            LargeRecords = await _context.Audits
                .Where(a => (a.OldValuesJson ?? "").Length + (a.NewValuesJson ?? "").Length > 64000)
                .CountAsync(),
            OldestRecord = await _context.Audits.MinAsync(a => a.Timestamp)
        };
    }
}
```

**References**: [Audit.cs:85-215](../src/Audit/Audit.cs) | [Performance Guide](performance-optimization-guide.md#audit-optimization)

---

## Configuration Issues

### Problem: Dependency Injection Configuration Errors

**Symptoms**:
- Services not resolving correctly
- Null reference exceptions in contexts
- Configuration not being applied

**Root Causes & Solutions**:

1. **Missing Service Registrations**
   ```csharp
   // Complete DI configuration
   public void ConfigureServices(IServiceCollection services)
   {
       // Essential services
       services.AddHttpContextAccessor();
       services.AddDbContext<ApplicationDbContext>(options =>
           options.UseSqlServer(connectionString));
       
       // Audit services
       services.AddScoped<IAuditService, AuditService>();
       services.AddScoped<IUserContextService, UserContextService>();
       
       // Background services
       services.AddHostedService<AuditArchivalBackgroundService>();
       
       // Performance monitoring
       services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
   }
   ```

2. **Scoped Service Issues**
   ```csharp
   // ❌ Problem: Using scoped service in singleton
   public class SingletonService
   {
       private readonly ApplicationDbContext _context; // Wrong scope
       
       public SingletonService(ApplicationDbContext context)
       {
           _context = context; // Will cause issues
       }
   }
   
   // ✅ Solution: Use service locator or factory
   public class SingletonService
   {
       private readonly IServiceProvider _serviceProvider;
       
       public SingletonService(IServiceProvider serviceProvider)
       {
           _serviceProvider = serviceProvider;
       }
       
       public async Task DoWorkAsync()
       {
           using var scope = _serviceProvider.CreateScope();
           var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
           // Use context safely
       }
   }
   ```

3. **Configuration Validation**
   ```csharp
   // Validate configuration at startup
   public class ConfigurationValidator
   {
       public static void ValidateConfiguration(IServiceProvider services)
       {
           using var scope = services.CreateScope();
           
           // Test database connection
           try
           {
               var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
               var canConnect = context.Database.CanConnect();
               if (!canConnect)
                   throw new InvalidOperationException("Database connection failed");
           }
           catch (Exception ex)
           {
               throw new InvalidOperationException("Database configuration invalid", ex);
           }
           
           // Test audit service
           try
           {
               var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
               var userId = auditService.GetCurrentUserId(); // Should not throw
           }
           catch (Exception ex)
           {
               throw new InvalidOperationException("Audit service configuration invalid", ex);
           }
       }
   }
   
   // Call during startup
   public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
   {
       ConfigurationValidator.ValidateConfiguration(app.ApplicationServices);
       // ... rest of configuration
   }
   ```

**Resolution Steps**:
1. Review and complete DI service registrations
2. Ensure correct service lifetimes (singleton, scoped, transient)
3. Implement configuration validation at startup
4. Test dependency resolution in integration tests

**References**: [Integration Guide](integration-guide.md#dependency-injection) | [FAQ](faq.md#aspnet-core-integration)

---

## Testing and Debugging

### Problem: Unit Testing Domain Entities

**Symptoms**:
- Difficulty testing entity behavior
- Mocking issues with value objects
- Performance tests failing

**Root Causes & Solutions**:

1. **Entity Testing Patterns**
   ```csharp
   [Test]
   public void Entity_Equality_Should_Work_Correctly()
   {
       // Arrange
       var id = Guid.NewGuid();
       var entity1 = new User { Id = id, Name = "Test User" };
       var entity2 = new User { Id = id, Name = "Different Name" };
       
       // Act & Assert
       Assert.That(entity1, Is.EqualTo(entity2)); // Same ID = equal
       Assert.That(entity1.GetHashCode(), Is.EqualTo(entity2.GetHashCode()));
   }
   
   [Test]
   public void Entity_Should_Handle_Transient_State()
   {
       // Arrange
       var entity = new User { Name = "Test User" }; // No ID set
       
       // Act & Assert
       Assert.That(entity.IsTransient(), Is.True);
       
       entity.Id = Guid.NewGuid();
       Assert.That(entity.IsTransient(), Is.False);
   }
   ```

2. **Value Object Testing**
   ```csharp
   [Test]
   public void ValueObject_Equality_Should_Be_Value_Based()
   {
       // Arrange
       var money1 = new Money(100m, "USD");
       var money2 = new Money(100m, "USD");
       var money3 = new Money(200m, "USD");
       
       // Act & Assert
       Assert.That(money1, Is.EqualTo(money2)); // Same values = equal
       Assert.That(money1, Is.Not.EqualTo(money3)); // Different values = not equal
       
       // Hash codes should be equal for equal objects
       Assert.That(money1.GetHashCode(), Is.EqualTo(money2.GetHashCode()));
   }
   
   [Test]
   public void ValueObject_Should_Support_Immutability()
   {
       // Arrange
       var originalMoney = new Money(100m, "USD");
       
       // Act
       var newMoney = originalMoney.Add(new Money(50m, "USD"));
       
       // Assert
       Assert.That(originalMoney.Amount, Is.EqualTo(100m)); // Original unchanged
       Assert.That(newMoney.Amount, Is.EqualTo(150m)); // New instance created
   }
   ```

3. **Performance Testing**
   ```csharp
   [Test]
   [Category("Performance")]
   public void Entity_Equality_Performance_Should_Be_Optimized()
   {
       // Arrange
       var entities = Enumerable.Range(0, 10000)
           .Select(i => new User { Id = Guid.NewGuid(), Name = $"User{i}" })
           .ToList();
       
       Entity<Guid>.ClearTypeCache(); // Start with cold cache
       
       // Act
       var sw = Stopwatch.StartNew();
       for (int i = 0; i < 10000; i++)
       {
           _ = entities[i].Equals(entities[(i + 1) % entities.Count]);
       }
       sw.Stop();
       
       // Assert
       Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100)); // Should be fast
       
       var stats = Entity<Guid>.GetPerformanceStats();
       Assert.That(stats.HitRatio, Is.GreaterThan(0.5)); // Cache should be effective
   }
   ```

**Testing Infrastructure**:
```csharp
// Base test class for domain testing
public abstract class DomainTestBase
{
    [SetUp]
    public virtual void SetUp()
    {
        // Clear caches for consistent test results
        Entity<Guid>.ClearTypeCache();
        Entity<int>.ClearTypeCache();
        Entity<string>.ClearTypeCache();
    }
    
    protected T CreateEntity<T>(object id) where T : class, new()
    {
        var entity = new T();
        var idProperty = typeof(T).GetProperty("Id");
        idProperty?.SetValue(entity, id);
        return entity;
    }
    
    protected void AssertPerformanceImprovement<T>(Action<T> operation, T input, double expectedImprovementRatio = 2.0)
    {
        // Cold run (first time)
        var sw1 = Stopwatch.StartNew();
        operation(input);
        sw1.Stop();
        
        // Warm run (should be faster due to caching)
        var sw2 = Stopwatch.StartNew();
        operation(input);
        sw2.Stop();
        
        // Warm run should be significantly faster
        Assert.That(sw1.ElapsedTicks / (double)sw2.ElapsedTicks, Is.GreaterThan(expectedImprovementRatio));
    }
}
```

**References**: [Unit Tests](../tests/) | [Performance Benchmarks](../benchmark/) | [FAQ](faq.md#unit-testing)

---

## Performance Monitoring

### Problem: Production Performance Monitoring

**Symptoms**:
- Need visibility into performance optimizations in production
- Want to detect performance degradations
- Require metrics for optimization effectiveness

**Solutions**:

1. **Built-in Performance Metrics**
   ```csharp
   public class DomainPerformanceMetrics
   {
       public static void RegisterMetrics(IServiceCollection services)
       {
           services.AddSingleton<IHostedService, PerformanceMonitoringService>();
       }
   }
   
   public class PerformanceMonitoringService : BackgroundService
   {
       private readonly ILogger<PerformanceMonitoringService> _logger;
       
       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           while (!stoppingToken.IsCancellationRequested)
           {
               LogPerformanceMetrics();
               await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
           }
       }
       
       private void LogPerformanceMetrics()
       {
           // Entity performance
           var entityStats = Entity<Guid>.GetPerformanceStats();
           _logger.LogInformation("Entity Cache - Hits: {Hits}, Misses: {Misses}, Hit Ratio: {HitRatio:P2}", 
               entityStats.Hits, entityStats.Misses, entityStats.HitRatio);
           
           // Value object performance
           var voStats = GetValueObjectStats();
           _logger.LogInformation("Value Object Optimizations - Enabled: {Enabled}, Total: {Total}", 
               voStats.OptimizedTypes, voStats.TotalTypes);
           
           // Memory usage
           var memoryBefore = GC.GetTotalMemory(false);
           GC.Collect();
           var memoryAfter = GC.GetTotalMemory(true);
           _logger.LogInformation("Memory - Before GC: {Before}KB, After GC: {After}KB", 
               memoryBefore / 1024, memoryAfter / 1024);
       }
   }
   ```

2. **Custom Performance Counters**
   ```csharp
   public class DomainPerformanceCounters
   {
       private static readonly Counter<long> EntityComparisons = 
           Meter.CreateCounter<long>("domain.entity.comparisons");
           
       private static readonly Histogram<double> ValueObjectOperations = 
           Meter.CreateHistogram<double>("domain.valueobject.operations");
           
       private static readonly Gauge<int> CacheSize = 
           Meter.CreateGauge<int>("domain.cache.size");
       
       public static void RecordEntityComparison(bool cacheHit)
       {
           EntityComparisons.Add(1, new("cache_hit", cacheHit.ToString()));
       }
       
       public static void RecordValueObjectOperation(double durationMs, bool optimized)
       {
           ValueObjectOperations.Record(durationMs, new("optimized", optimized.ToString()));
       }
   }
   ```

3. **Application Insights Integration**
   ```csharp
   public class ApplicationInsightsPerformanceTracker
   {
       private readonly TelemetryClient _telemetryClient;
       
       public void TrackDomainPerformance()
       {
           var stats = Entity<Guid>.GetPerformanceStats();
           
           _telemetryClient.TrackMetric("Domain.Entity.CacheHitRatio", stats.HitRatio);
           _telemetryClient.TrackMetric("Domain.Entity.CacheHits", stats.Hits);
           _telemetryClient.TrackMetric("Domain.Entity.CacheMisses", stats.Misses);
           
           // Custom properties for correlation
           var properties = new Dictionary<string, string>
           {
               ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
               ["MachineName"] = Environment.MachineName
           };
           
           _telemetryClient.TrackEvent("DomainPerformanceSnapshot", properties);
       }
   }
   ```

**Alerting Configuration**:
```csharp
public class PerformanceAlerting
{
    private readonly ILogger _logger;
    private readonly INotificationService _notifications;
    
    public async Task CheckPerformanceThresholds()
    {
        var entityStats = Entity<Guid>.GetPerformanceStats();
        
        // Alert on low cache hit ratios
        if (entityStats.HitRatio < 0.85 && entityStats.Hits + entityStats.Misses > 10000)
        {
            await _notifications.SendAlert(
                "Low Entity Cache Hit Ratio",
                $"Hit ratio: {entityStats.HitRatio:P2} (threshold: 85%)",
                AlertLevel.Warning);
        }
        
        // Alert on high memory usage
        var memoryUsage = GC.GetTotalMemory(false);
        if (memoryUsage > 500 * 1024 * 1024) // 500MB
        {
            await _notifications.SendAlert(
                "High Domain Library Memory Usage",
                $"Memory usage: {memoryUsage / 1024 / 1024}MB",
                AlertLevel.Warning);
        }
    }
}
```

**References**: [Performance Guide](performance-optimization-guide.md) | [Design Decisions](design-decisions.md#adr-001-performance-first-architecture)

---

## Quick Reference

### Diagnostic Commands
```csharp
// Entity performance
var stats = Entity<Guid>.GetPerformanceStats();
Console.WriteLine($"Hit ratio: {stats.HitRatio:P2}");

// Clear caches
Entity<Guid>.ClearTypeCache();

// Check optimization status
var optimized = IsOptimizationEnabled(typeof(MyValueObject));
```

### Common Error Patterns
| Error Pattern | Likely Cause | Quick Fix |
|---------------|--------------|-----------|
| Low cache hit ratio | Type diversity, proxy issues | Monitor type usage, disable problematic proxies |
| Compilation failures | Security policies, complex types | Review property types, accept fallback |
| Missing audit records | Wrong base class, missing configuration | Inherit from AuditDbContext |
| Serialization errors | Circular references, complex objects | Simplify object graphs, configure JSON options |

### Support Resources
- **[FAQ](faq.md)**: Common questions and solutions
- **[Performance Guide](performance-optimization-guide.md)**: Optimization strategies
- **[Integration Guide](integration-guide.md)**: Framework integration patterns
- **[API Reference](api-reference.md)**: Complete API documentation

---

*Last Updated: 2025-01-15 | Next Review: Q2 2025*