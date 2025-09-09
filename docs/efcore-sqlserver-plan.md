# EF Core SQL Server Optimization Implementation Plan

> **Document Purpose**: Comprehensive roadmap for implementing SQL Server-specific optimizations in the Wangkanai EntityFramework
> provider
>
> **Target**: Leverage enterprise SQL Server features for maximum performance and scalability
>
> **Created**: 2025-09-09 | **Status**: Planning Phase

---

## 🎯 Executive Summary

This document outlines a strategic implementation plan for SQL Server-specific optimizations in the Wangkanai EntityFramework
provider. The plan focuses on leveraging enterprise SQL Server features including columnstore indexes, memory-optimized tables,
temporal tables, and advanced query optimization capabilities through fluent API extensions.

### **Key Objectives**

- Leverage enterprise SQL Server features for maximum performance
- Enable advanced indexing strategies including columnstore and memory-optimized
- Provide temporal table support for audit and history tracking
- Optimize bulk operations using SQL Server bulk copy capabilities
- Enable Query Store integration for performance monitoring

---

## 📊 Current State Analysis

### **Existing Implementation**

```csharp
✅ ConfigurationExtensions.cs
   └── SqlValueGeneratedOnAdd<T>() - GETDATE() default value configuration

✅ Project Structure
   ├── Wangkanai.EntityFramework.SqlServer.csproj
   ├── Integration test project setup
   └── Microsoft.EntityFrameworkCore.SqlServer package reference
```

### **Gap Analysis**

- **Missing**: Columnstore index configuration
- **Missing**: Memory-optimized table support
- **Missing**: Temporal table configuration
- **Missing**: Query Store integration
- **Missing**: Bulk copy operation optimizations
- **Missing**: Advanced isolation level configurations
- **Missing**: Partition scheme support

---

## 🚀 Implementation Roadmap

### **Phase 1: Foundation Performance Extensions** (Week 1-2)

**Priority**: High | **Effort**: Low | **Impact**: High

#### **1.1 Connection Performance Extensions**

```csharp
// File: src/SqlServer/ConnectionConfigurationExtensions.cs
public static class ConnectionConfigurationExtensions
{
    /// <summary>
    /// Enables Read Committed Snapshot Isolation (RCSI) for optimistic concurrency.
    /// Reduces blocking and deadlocks in high-concurrency scenarios.
    /// </summary>
    public static DbContextOptionsBuilder<T> EnableSqlServerRCSI<T>(
        this DbContextOptionsBuilder<T> builder) where T : DbContext

    /// <summary>
    /// Configures connection resiliency with automatic retry logic.
    /// Handles transient failures in cloud and distributed environments.
    /// </summary>
    public static DbContextOptionsBuilder<T> EnableSqlServerConnectionResiliency<T>(
        this DbContextOptionsBuilder<T> builder,
        int maxRetryCount = 6,
        TimeSpan maxRetryDelay = TimeSpan.FromSeconds(30)) where T : DbContext

    /// <summary>
    /// Enables Multiple Active Result Sets (MARS) for parallel query execution.
    /// Allows multiple pending requests on a single connection.
    /// </summary>
    public static DbContextOptionsBuilder<T> EnableSqlServerMARS<T>(
        this DbContextOptionsBuilder<T> builder) where T : DbContext

    /// <summary>
    /// Configures command timeout for long-running operations.
    /// Essential for data warehouse and reporting scenarios.
    /// </summary>
    public static DbContextOptionsBuilder<T> SetSqlServerCommandTimeout<T>(
        this DbContextOptionsBuilder<T> builder, int seconds = 30) where T : DbContext

    /// <summary>
    /// Enables Query Store for performance monitoring and analysis.
    /// Tracks query performance history and execution plans.
    /// </summary>
    public static DbContextOptionsBuilder<T> EnableSqlServerQueryStore<T>(
        this DbContextOptionsBuilder<T> builder) where T : DbContext
}
```

#### **1.2 Isolation Level Extensions**

```csharp
// Extend existing ConfigurationExtensions.cs
public static class ConfigurationExtensions
{
    // Existing methods...

    /// <summary>
    /// Configures snapshot isolation for read-heavy workloads.
    /// Provides consistent point-in-time view without blocking writers.
    /// </summary>
    public static EntityTypeBuilder<T> UseSqlServerSnapshotIsolation<T>(
        this EntityTypeBuilder<T> builder) where T : class

    /// <summary>
    /// Enables row versioning for optimistic concurrency control.
    /// Uses SQL Server's built-in rowversion/timestamp type.
    /// </summary>
    public static PropertyBuilder<byte[]> HasSqlServerRowVersion(
        this PropertyBuilder<byte[]> builder)

    /// <summary>
    /// Configures read uncommitted isolation for reporting queries.
    /// Reduces lock contention for non-critical read operations.
    /// </summary>
    public static EntityTypeBuilder<T> UseSqlServerNoLock<T>(
        this EntityTypeBuilder<T> builder) where T : class
}
```

**Deliverables**:

- [ ] `ConnectionConfigurationExtensions.cs` implementation
- [ ] Enhanced `ConfigurationExtensions.cs` with isolation levels
- [ ] Unit tests for all extension methods
- [ ] Performance benchmarks for RCSI vs standard isolation
- [ ] Documentation with enterprise scenario examples

---

### **Phase 2: Advanced Indexing Support** (Week 3-4)

**Priority**: High | **Effort**: Medium | **Impact**: High

#### **2.1 Columnstore Index Extensions**

```csharp
// File: src/SqlServer/ColumnstoreConfigurationExtensions.cs
public static class ColumnstoreConfigurationExtensions
{
    /// <summary>
    /// Creates clustered columnstore index for analytical workloads.
    /// Provides 10x compression and massive query performance gains.
    /// </summary>
    public static EntityTypeBuilder<T> HasSqlServerClusteredColumnstoreIndex<T>(
        this EntityTypeBuilder<T> builder) where T : class

    /// <summary>
    /// Creates non-clustered columnstore index on specific columns.
    /// Enables real-time operational analytics on transactional tables.
    /// </summary>
    public static EntityTypeBuilder<T> HasSqlServerNonClusteredColumnstoreIndex<T>(
        this EntityTypeBuilder<T> builder,
        params Expression<Func<T, object>>[] columns) where T : class

    /// <summary>
    /// Configures columnstore compression settings.
    /// Balances storage efficiency with query performance.
    /// </summary>
    public static EntityTypeBuilder<T> WithSqlServerColumnstoreCompression<T>(
        this EntityTypeBuilder<T> builder,
        CompressionType type = CompressionType.Columnstore) where T : class

    /// <summary>
    /// Optimizes batch loading for columnstore indexes.
    /// Ensures optimal rowgroup sizes for maximum compression.
    /// </summary>
    public static EntityTypeBuilder<T> OptimizeForSqlServerColumnstoreBulkLoad<T>(
        this EntityTypeBuilder<T> builder,
        int batchSize = 1048576) where T : class
}
```

#### **2.2 Advanced Index Extensions**

```csharp
// File: src/SqlServer/IndexConfigurationExtensions.cs
public static class IndexConfigurationExtensions
{
    /// <summary>
    /// Creates filtered index with WHERE clause for selective indexing.
    /// Reduces index size and maintenance overhead for sparse data.
    /// </summary>
    public static IndexBuilder<T> HasSqlServerFilteredIndex<T>(
        this IndexBuilder<T> builder,
        string filterExpression) where T : class

    /// <summary>
    /// Creates covering index with INCLUDE columns.
    /// Eliminates key lookups for frequently accessed non-key columns.
    /// </summary>
    public static IndexBuilder<T> HasSqlServerIncludedColumns<T>(
        this IndexBuilder<T> builder,
        params Expression<Func<T, object>>[] includedColumns) where T : class

    /// <summary>
    /// Configures index fill factor for page splits optimization.
    /// Balances storage efficiency with insert/update performance.
    /// </summary>
    public static IndexBuilder<T> WithSqlServerFillFactor<T>(
        this IndexBuilder<T> builder,
        int fillFactor = 80) where T : class

    /// <summary>
    /// Creates spatial index for geography/geometry columns.
    /// Optimizes location-based queries and spatial operations.
    /// </summary>
    public static PropertyBuilder<T> HasSqlServerSpatialIndex<T>(
        this PropertyBuilder<T> builder,
        SpatialIndexSettings settings = null)
}
```

**Deliverables**:

- [ ] `ColumnstoreConfigurationExtensions.cs` implementation
- [ ] `IndexConfigurationExtensions.cs` implementation
- [ ] Columnstore performance benchmarks (10x+ improvement targets)
- [ ] Index strategy selection guide
- [ ] Integration tests with large datasets

---

### **Phase 3: Memory-Optimized Tables & Temporal Support** (Week 5-6)

**Priority**: Medium | **Effort**: High | **Impact**: High

#### **3.1 Memory-Optimized Table Extensions**

```csharp
// File: src/SqlServer/MemoryOptimizedConfigurationExtensions.cs
public static class MemoryOptimizedConfigurationExtensions
{
    /// <summary>
    /// Configures entity as memory-optimized table for extreme performance.
    /// Provides lock-free data structures and optimistic concurrency.
    /// </summary>
    public static EntityTypeBuilder<T> IsMemoryOptimized<T>(
        this EntityTypeBuilder<T> builder,
        DurabilityType durability = DurabilityType.SchemaAndData) where T : class

    /// <summary>
    /// Creates hash index for memory-optimized tables.
    /// Optimized for point lookups with O(1) performance.
    /// </summary>
    public static PropertyBuilder<T> HasSqlServerHashIndex<T>(
        this PropertyBuilder<T> builder,
        int bucketCount = 1024)

    /// <summary>
    /// Creates natively compiled stored procedure for memory-optimized tables.
    /// Provides C-like performance for data access operations.
    /// </summary>
    public static EntityTypeBuilder<T> WithNativelyCompiledProcedures<T>(
        this EntityTypeBuilder<T> builder) where T : class

    /// <summary>
    /// Configures memory-optimized filegroup settings.
    /// Manages memory allocation and checkpoint behavior.
    /// </summary>
    public static ModelBuilder ConfigureSqlServerMemoryOptimizedFilegroup(
        this ModelBuilder builder,
        string filegroupName,
        string containerPath)
}
```

#### **3.2 Temporal Table Extensions**

```csharp
// File: src/SqlServer/TemporalConfigurationExtensions.cs
public static class TemporalConfigurationExtensions
{
    /// <summary>
    /// Configures entity as temporal table for automatic history tracking.
    /// Enables point-in-time queries and audit trail capabilities.
    /// </summary>
    public static EntityTypeBuilder<T> IsTemporal<T>(
        this EntityTypeBuilder<T> builder,
        string historyTableName = null) where T : class

    /// <summary>
    /// Configures period columns for temporal table validity.
    /// Defines system-time period for row versioning.
    /// </summary>
    public static EntityTypeBuilder<T> HasSqlServerPeriod<T>(
        this EntityTypeBuilder<T> builder,
        string startColumnName = "ValidFrom",
        string endColumnName = "ValidTo") where T : class

    /// <summary>
    /// Configures retention policy for temporal history data.
    /// Automatically purges old history based on business requirements.
    /// </summary>
    public static EntityTypeBuilder<T> WithSqlServerHistoryRetention<T>(
        this EntityTypeBuilder<T> builder,
        int retentionPeriod,
        RetentionUnit unit = RetentionUnit.Months) where T : class

    /// <summary>
    /// Enables temporal queries with FOR SYSTEM_TIME clause.
    /// Queries data as of specific point in time or time range.
    /// </summary>
    public static IQueryable<T> AsOfSystemTime<T>(
        this DbSet<T> dbSet,
        DateTime pointInTime) where T : class
}
```

**Deliverables**:

- [ ] `MemoryOptimizedConfigurationExtensions.cs` implementation
- [ ] `TemporalConfigurationExtensions.cs` implementation
- [ ] Memory-optimized performance benchmarks (100x improvement for hot data)
- [ ] Temporal table migration strategies
- [ ] Production deployment guide for In-Memory OLTP

---

### **Phase 4: Bulk Operations & Change Tracking** (Week 7-8)

**Priority**: Medium | **Effort**: Medium | **Impact**: High

#### **4.1 Bulk Operation Extensions**

```csharp
// File: src/SqlServer/BulkConfigurationExtensions.cs
public static class BulkConfigurationExtensions
{
    /// <summary>
    /// Configures SqlBulkCopy for high-performance bulk inserts.
    /// Achieves 100x faster inserts compared to standard EF operations.
    /// </summary>
    public static EntityTypeBuilder<T> EnableSqlServerBulkCopy<T>(
        this EntityTypeBuilder<T> builder,
        SqlBulkCopyOptions options = SqlBulkCopyOptions.Default) where T : class

    /// <summary>
    /// Optimizes entity for MERGE operations.
    /// Enables efficient upsert patterns with single round-trip.
    /// </summary>
    public static EntityTypeBuilder<T> OptimizeForSqlServerMerge<T>(
        this EntityTypeBuilder<T> builder) where T : class

    /// <summary>
    /// Configures table-valued parameters for bulk operations.
    /// Passes multiple rows as single parameter to stored procedures.
    /// </summary>
    public static EntityTypeBuilder<T> HasSqlServerTableValuedParameter<T>(
        this EntityTypeBuilder<T> builder,
        string typeName) where T : class

    /// <summary>
    /// Enables minimal logging for bulk operations.
    /// Reduces transaction log overhead for large data loads.
    /// </summary>
    public static EntityTypeBuilder<T> WithSqlServerMinimalLogging<T>(
        this EntityTypeBuilder<T> builder) where T : class
}
```

#### **4.2 Change Tracking Extensions**

```csharp
// File: src/SqlServer/ChangeTrackingConfigurationExtensions.cs
public static class ChangeTrackingConfigurationExtensions
{
    /// <summary>
    /// Enables SQL Server Change Tracking for sync scenarios.
    /// Tracks changes without storing actual data modifications.
    /// </summary>
    public static EntityTypeBuilder<T> EnableSqlServerChangeTracking<T>(
        this EntityTypeBuilder<T> builder,
        int retentionDays = 2) where T : class

    /// <summary>
    /// Configures Change Data Capture (CDC) for comprehensive auditing.
    /// Captures insert, update, and delete activity with full data.
    /// </summary>
    public static EntityTypeBuilder<T> EnableSqlServerCDC<T>(
        this EntityTypeBuilder<T> builder,
        CaptureInstance instance = null) where T : class

    /// <summary>
    /// Gets changes since last synchronization point.
    /// Returns only modified records for efficient sync operations.
    /// </summary>
    public static IQueryable<T> GetSqlServerChanges<T>(
        this DbSet<T> dbSet,
        long lastSyncVersion) where T : class

    /// <summary>
    /// Configures automatic change tracking cleanup.
    /// Maintains optimal performance by purging old tracking data.
    /// </summary>
    public static EntityTypeBuilder<T> WithSqlServerChangeTrackingCleanup<T>(
        this EntityTypeBuilder<T> builder,
        bool autoCleanup = true) where T : class
}
```

**Deliverables**:

- [ ] `BulkConfigurationExtensions.cs` implementation
- [ ] `ChangeTrackingConfigurationExtensions.cs` implementation
- [ ] Bulk operation performance benchmarks (100x improvement)
- [ ] Change tracking vs CDC comparison guide
- [ ] Synchronization pattern examples

---

### **Phase 5: Advanced Enterprise Features** (Week 9-10)

**Priority**: Low | **Effort**: High | **Impact**: Medium

#### **5.1 Partitioning Extensions**

```csharp
// File: src/SqlServer/PartitionConfigurationExtensions.cs
public static class PartitionConfigurationExtensions
{
    /// <summary>
    /// Configures table partitioning for large-scale data management.
    /// Enables parallel query execution and maintenance operations.
    /// </summary>
    public static EntityTypeBuilder<T> HasSqlServerPartitionScheme<T>(
        this EntityTypeBuilder<T> builder,
        string partitionScheme,
        Expression<Func<T, object>> partitionKey) where T : class

    /// <summary>
    /// Creates partition function for range-based partitioning.
    /// Distributes data across filegroups based on key ranges.
    /// </summary>
    public static ModelBuilder CreateSqlServerPartitionFunction(
        this ModelBuilder builder,
        string functionName,
        SqlDbType dataType,
        params object[] boundaryValues)

    /// <summary>
    /// Enables partition switching for fast data archival.
    /// Moves entire partitions between tables instantaneously.
    /// </summary>
    public static void SwitchSqlServerPartition<T>(
        this DbContext context,
        int partitionNumber,
        string targetTable) where T : class

    /// <summary>
    /// Configures sliding window partitioning for time-series data.
    /// Automatically manages partition creation and removal.
    /// </summary>
    public static EntityTypeBuilder<T> WithSqlServerSlidingWindow<T>(
        this EntityTypeBuilder<T> builder,
        int windowSizeDays = 30) where T : class
}
```

#### **5.2 Service Broker & Query Hints Extensions**

```csharp
// File: src/SqlServer/AdvancedConfigurationExtensions.cs
public static class AdvancedConfigurationExtensions
{
    /// <summary>
    /// Enables Service Broker for asynchronous messaging.
    /// Provides reliable message queuing within database.
    /// </summary>
    public static DbContextOptionsBuilder<T> EnableSqlServerServiceBroker<T>(
        this DbContextOptionsBuilder<T> builder) where T : DbContext

    /// <summary>
    /// Applies query hints for execution plan control.
    /// Forces specific join types, index usage, or parallelism.
    /// </summary>
    public static IQueryable<T> WithSqlServerHint<T>(
        this IQueryable<T> query,
        string hint) where T : class

    /// <summary>
    /// Configures Resource Governor for workload management.
    /// Controls CPU, memory, and I/O resources per workload.
    /// </summary>
    public static DbContextOptionsBuilder<T> UseSqlServerResourceGovernor<T>(
        this DbContextOptionsBuilder<T> builder,
        string resourcePoolName) where T : DbContext

    /// <summary>
    /// Enables Stretch Database for hybrid cloud storage.
    /// Transparently archives cold data to Azure.
    /// </summary>
    public static EntityTypeBuilder<T> EnableSqlServerStretchDatabase<T>(
        this EntityTypeBuilder<T> builder,
        Func<T, bool> filterPredicate) where T : class
}
```

**Deliverables**:

- [ ] `PartitionConfigurationExtensions.cs` implementation
- [ ] `AdvancedConfigurationExtensions.cs` implementation
- [ ] Partitioning strategy guide for various scenarios
- [ ] Service Broker messaging patterns
- [ ] Enterprise feature adoption roadmap

---

## 📈 Performance Impact Matrix

| Extension Category      | Read Performance | Write Performance | Storage Efficiency | Enterprise Scale |
|-------------------------|------------------|-------------------|--------------------|------------------|
| **Connection Config**   | ++++             | ++++              | ++                 | +++++            |
| **Columnstore Indexes** | +++++            | +++               | +++++              | +++++            |
| **Memory-Optimized**    | +++++            | +++++             | +                  | ++++             |
| **Temporal Tables**     | +++              | ++                | ++                 | +++++            |
| **Bulk Operations**     | ++               | +++++             | +++                | +++++            |
| **Partitioning**        | ++++             | ++++              | ++++               | +++++            |

**Legend**: + (Minor) | ++ (Moderate) | +++ (Significant) | ++++ (Major) | +++++ (Exceptional)

---

## 🧪 Testing Strategy

### **Unit Testing Framework**

```csharp
// Test structure following project patterns
EntityFramework/tests/SqlServer/Unit/
├── ConnectionConfigurationExtensionsTests.cs
├── ColumnstoreConfigurationExtensionsTests.cs
├── MemoryOptimizedConfigurationExtensionsTests.cs
├── TemporalConfigurationExtensionsTests.cs
├── BulkConfigurationExtensionsTests.cs
└── PerformanceTests/
    ├── ColumnstoreBenchmarks.cs
    ├── MemoryOptimizedBenchmarks.cs
    └── BulkOperationBenchmarks.cs
```

### **Integration Testing**

```csharp
EntityFramework/tests/SqlServer/Integration/
├── EnterpriseScenarioTests.cs
├── HighConcurrencyTests.cs
├── TemporalQueryTests.cs
└── PartitioningTests.cs
```

### **Performance Benchmarking**

- BenchmarkDotNet for quantitative measurements
- Columnstore: 10x compression, 100x analytical query improvement
- Memory-Optimized: 30x OLTP performance improvement
- Bulk Operations: 100x insert performance improvement
- Query Store integration for production monitoring

---

## 🔧 Implementation Guidelines

### **Code Style Consistency**

- Follow existing `SqlValueGeneratedOnAdd<T>()` pattern
- Use expression-bodied syntax where appropriate
- Comprehensive XML documentation with SQL Server specifics
- Include T-SQL examples in documentation

### **SQL Server Version Compatibility**

- **Minimum**: SQL Server 2016 (for columnstore, temporal tables)
- **Recommended**: SQL Server 2019+ (for all features)
- **Azure SQL Database**: Full compatibility with managed instance
- **Feature detection**: Runtime capability checking

### **Enterprise Considerations**

- Always-On Availability Groups compatibility
- Replication support verification
- Security context and permissions documentation
- Licensing implications for enterprise features

---

## 🚀 Deployment Strategy

### **Release Phases**

1. **Alpha Release**: Foundation + Columnstore (Phases 1-2)
2. **Beta Release**: Add Memory-Optimized + Temporal (Phases 1-3)
3. **Stable Release**: Complete enterprise feature set (All phases)

### **Version Compatibility**

- **SQL Server**: 2016+ (2019+ recommended)
- **.NET Version**: .NET 9.0+
- **EF Core Version**: 9.0.0+
- **Azure SQL**: Full compatibility

### **Migration Path**

- Gradual adoption strategy for existing applications
- Feature flag system for enterprise features
- Backward compatibility with standard EF operations
- Performance baseline establishment guide

---

## 📚 Documentation Deliverables

### **Developer Documentation**

- [ ] **API Reference**: Complete XML documentation
- [ ] **Enterprise Patterns**: Best practices for large-scale applications
- [ ] **Performance Tuning**: Query Store analysis guide
- [ ] **Migration Guide**: From standard to optimized configurations

### **Architecture Documentation**

- [ ] **Feature Matrix**: SQL Server edition requirements
- [ ] **Scaling Patterns**: Partitioning and sharding strategies
- [ ] **High Availability**: Integration with Always-On features
- [ ] **Security Guide**: Row-level security and encryption

---

## 🎯 Success Metrics

### **Performance Targets**

- **Analytical Queries**: 10-100x improvement with columnstore
- **OLTP Operations**: 30x improvement with memory-optimized tables
- **Bulk Operations**: 100x improvement with SqlBulkCopy
- **Concurrent Users**: Support for 10,000+ concurrent connections

### **Enterprise Adoption**

- **Feature Coverage**: 100% of major SQL Server enterprise features
- **Azure Integration**: Seamless Azure SQL Database support
- **Monitoring**: Query Store integration for all optimizations
- **Compliance**: Support for regulatory requirements (temporal tables)

### **Quality Metrics**

- **Test Coverage**: >95% code coverage
- **Performance Regression**: Automated detection via benchmarks
- **Documentation**: Enterprise scenario examples for all features
- **Production Validation**: Testing with 1TB+ databases

---

## 🤝 Contribution Guidelines

### **Enterprise Testing Requirements**

- Test with SQL Server Developer Edition (free)
- Include Azure SQL Database compatibility tests
- Benchmark against various SQL Server editions
- Validate with production-scale datasets

### **Review Process**

- Enterprise feature expertise review
- Performance impact verification with Query Store
- Security review for elevated permission requirements
- Azure SQL Database compatibility validation

---

**Next Steps**: Begin with Phase 1 foundation extensions for immediate RCSI and connection resiliency benefits, then progressively
adopt enterprise features based on specific workload requirements.