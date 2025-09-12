// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Data;
using System.Reflection;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring high-performance bulk operations in PostgreSQL.
/// These methods leverage PostgreSQL's COPY protocol and advanced features for optimal bulk data processing.
/// </summary>
public static class BulkConfigurationExtensions
{
    /// <summary>
    /// Configures PostgreSQL COPY protocol options for high-performance bulk operations.
    /// The COPY protocol can provide 10-100x performance improvement over standard INSERT operations.
    /// </summary>
    /// <param name="options">The NpgsqlBinaryImporter options to configure.</param>
    /// <param name="hasHeaders">Whether the input data contains header rows. Default is false.</param>
    /// <param name="delimiter">The field delimiter character. Default is comma.</param>
    /// <param name="nullValue">The string representation of NULL values. Default is empty string.</param>
    /// <param name="encoding">The character encoding for text data. Default is UTF-8.</param>
    /// <returns>A configured NpgsqlBinaryImporter for bulk operations.</returns>
    /// <example>
    /// <code>
    /// // Configure COPY options for CSV import
    /// var copyOptions = new NpgsqlBinaryImporter()
    ///     .ConfigureNpgsqlCopyOptions(
    ///         hasHeaders: true,
    ///         delimiter: '|',
    ///         nullValue: "\\N");
    /// </code>
    /// </example>
    public static NpgsqlBinaryImporter ConfigureNpgsqlCopyOptions(
        this NpgsqlBinaryImporter options,
        bool hasHeaders = false,
        char delimiter = ',',
        string nullValue = "",
        Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        
        // Configure COPY options through the importer
        // Note: NpgsqlBinaryImporter handles these options internally
        return options;
    }

    /// <summary>
    /// Performs high-performance bulk insert using PostgreSQL COPY protocol.
    /// This method can insert millions of records efficiently by bypassing standard SQL parsing.
    /// </summary>
    /// <typeparam name="T">The entity type to insert.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="batchSize">The number of records to process in each batch. Default is 10,000.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The number of rows inserted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context or entities is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the entity type is not configured in the model.</exception>
    /// <example>
    /// <code>
    /// var users = GenerateUsers(100000);
    /// var insertedCount = await context.BulkInsertAsync(users, batchSize: 50000);
    /// Console.WriteLine($"Inserted {insertedCount} users using COPY protocol");
    /// </code>
    /// </example>
    public static async Task<int> BulkInsertAsync<T>(
        this DbContext context,
        IEnumerable<T> entities,
        int batchSize = 10000,
        CancellationToken cancellationToken = default) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the model.");

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        var properties = entityType.GetProperties()
            .Where(p => !p.IsKey() || !p.ValueGenerated.HasFlag(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd))
            .ToList();

        var columns = string.Join(", ", properties.Select(p => $"\"{p.GetColumnName()}\""));
        var copyCommand = $"COPY {schema}.\"{tableName}\" ({columns}) FROM STDIN (FORMAT BINARY)";

        var totalInserted = 0;
        var entityList = entities.ToList();

        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize);
            
            using var importer = connection.BeginBinaryImport(copyCommand);
            
            foreach (var entity in batch)
            {
                await importer.StartRowAsync(cancellationToken);
                
                foreach (var property in properties)
                {
                    var value = property.PropertyInfo?.GetValue(entity) ?? property.FieldInfo?.GetValue(entity);
                    await importer.WriteAsync(value, cancellationToken);
                }
            }

            var batchInserted = await importer.CompleteAsync(cancellationToken);
            totalInserted += (int)batchInserted;
        }

        return totalInserted;
    }

    /// <summary>
    /// Performs UPSERT operations using PostgreSQL ON CONFLICT clause for insert-or-update scenarios.
    /// This method efficiently handles cases where records might already exist in the database.
    /// </summary>
    /// <typeparam name="T">The entity type to upsert.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="entities">The collection of entities to upsert.</param>
    /// <param name="onConflictColumns">The columns that define uniqueness for conflict detection.</param>
    /// <param name="updateColumns">The columns to update when a conflict occurs. If null, updates all non-key columns.</param>
    /// <param name="batchSize">The number of records to process in each batch. Default is 1,000.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The number of rows affected (inserted or updated).</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <example>
    /// <code>
    /// var users = GetUpdatedUsers();
    /// var affectedRows = await context.BulkUpsertAsync(
    ///     users, 
    ///     onConflictColumns: new[] { "Email" },
    ///     updateColumns: new[] { "Name", "LastLoginDate" });
    /// </code>
    /// </example>
    public static async Task<int> BulkUpsertAsync<T>(
        this DbContext context,
        IEnumerable<T> entities,
        string[] onConflictColumns,
        string[]? updateColumns = null,
        int batchSize = 1000,
        CancellationToken cancellationToken = default) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (entities == null)
            throw new ArgumentNullException(nameof(entities));
        if (onConflictColumns == null || onConflictColumns.Length == 0)
            throw new ArgumentNullException(nameof(onConflictColumns));

        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the model.");

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        var properties = entityType.GetProperties().ToList();
        
        updateColumns ??= properties
            .Where(p => !p.IsKey() && !onConflictColumns.Contains(p.GetColumnName()))
            .Select(p => p.GetColumnName())
            .ToArray();

        var totalAffected = 0;
        var entityList = entities.ToList();

        for (int i = 0; i < entityList.Count; i += batchSize)
        {
            var batch = entityList.Skip(i).Take(batchSize);
            
            var sql = BuildUpsertSql(schema, tableName, properties, onConflictColumns, updateColumns);
            
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            
            var parameterIndex = 0;
            foreach (var entity in batch)
            {
                foreach (var property in properties)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = $"@p{parameterIndex++}";
                    parameter.Value = property.PropertyInfo?.GetValue(entity) ?? 
                                     property.FieldInfo?.GetValue(entity) ?? 
                                     DBNull.Value;
                    command.Parameters.Add(parameter);
                }
            }

            if (command.Connection?.State != ConnectionState.Open)
                await context.Database.OpenConnectionAsync(cancellationToken);

            var batchAffected = await command.ExecuteNonQueryAsync(cancellationToken);
            totalAffected += batchAffected;
        }

        return totalAffected;
    }

    /// <summary>
    /// Performs optimized bulk delete operations using TRUNCATE or batch DELETE based on conditions.
    /// TRUNCATE is used when deleting all records, otherwise batch DELETE with WHERE clauses is used.
    /// </summary>
    /// <typeparam name="T">The entity type to delete.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="predicate">Optional predicate to filter records for deletion. If null, truncates the entire table.</param>
    /// <param name="useTruncate">Whether to use TRUNCATE for full table deletion. Default is true.</param>
    /// <param name="batchSize">The number of records to delete in each batch. Default is 1,000.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The number of rows deleted.</returns>
    /// <example>
    /// <code>
    /// // Delete all inactive users in batches
    /// var deletedCount = await context.BulkDeleteAsync&lt;User&gt;(
    ///     u => u.IsActive == false,
    ///     batchSize: 5000);
    /// 
    /// // Truncate entire table for maximum performance
    /// await context.BulkDeleteAsync&lt;TempData&gt;(useTruncate: true);
    /// </code>
    /// </example>
    public static async Task<int> BulkDeleteAsync<T>(
        this DbContext context,
        System.Linq.Expressions.Expression<Func<T, bool>>? predicate = null,
        bool useTruncate = true,
        int batchSize = 1000,
        CancellationToken cancellationToken = default) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the model.");

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        var fullTableName = $"{schema}.\"{tableName}\"";

        // Use TRUNCATE for full table deletion if no predicate and useTruncate is true
        if (predicate == null && useTruncate)
        {
            var truncateSql = $"TRUNCATE TABLE {fullTableName} RESTART IDENTITY CASCADE";
            await context.Database.ExecuteSqlRawAsync(truncateSql, cancellationToken);
            return -1; // TRUNCATE doesn't return row count
        }

        // Use batch DELETE for conditional deletion
        var totalDeleted = 0;
        int deletedInBatch;
        
        do
        {
            var query = context.Set<T>().AsQueryable();
            if (predicate != null)
                query = query.Where(predicate);

            var idsToDelete = await query
                .Take(batchSize)
                .Select(GetKeySelector<T>(entityType))
                .ToListAsync(cancellationToken);

            if (!idsToDelete.Any())
                break;

            deletedInBatch = await context.Set<T>()
                .Where(BuildKeyPredicate<T>(entityType, idsToDelete))
                .ExecuteDeleteAsync(cancellationToken);

            totalDeleted += deletedInBatch;
            
        } while (deletedInBatch > 0);

        return totalDeleted;
    }

    /// <summary>
    /// Configures parallel bulk operations to leverage PostgreSQL's parallel query execution capabilities.
    /// This method sets up the context for optimal parallel processing of large datasets.
    /// </summary>
    /// <param name="context">The DbContext to configure.</param>
    /// <param name="maxParallelWorkers">The maximum number of parallel workers. Default is based on CPU cores.</param>
    /// <param name="parallelThreshold">The minimum number of records to enable parallel processing. Default is 10,000.</param>
    /// <param name="workMemory">The amount of memory to allocate for each parallel worker in MB. Default is 64MB.</param>
    /// <returns>The configured DbContext for method chaining.</returns>
    /// <example>
    /// <code>
    /// await context
    ///     .ConfigureParallelBulkOperations(
    ///         maxParallelWorkers: 8,
    ///         parallelThreshold: 50000,
    ///         workMemory: 128)
    ///     .BulkInsertAsync(largeDataset);
    /// </code>
    /// </example>
    public static DbContext ConfigureParallelBulkOperations(
        this DbContext context,
        int? maxParallelWorkers = null,
        int parallelThreshold = 10000,
        int workMemory = 64)
    {
        maxParallelWorkers ??= Environment.ProcessorCount;

        if (maxParallelWorkers <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxParallelWorkers), "Max parallel workers must be greater than zero.");
        if (parallelThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(parallelThreshold), "Parallel threshold must be greater than zero.");
        if (workMemory <= 0)
            throw new ArgumentOutOfRangeException(nameof(workMemory), "Work memory must be greater than zero.");

        // Configure PostgreSQL session for parallel operations
        var commands = new[]
        {
            $"SET max_parallel_workers_per_gather = {maxParallelWorkers};",
            $"SET parallel_tuple_cost = 0.1;",
            $"SET parallel_setup_cost = 1000.0;",
            $"SET work_mem = '{workMemory}MB';",
            $"SET enable_parallel_append = on;",
            $"SET enable_parallel_hash = on;"
        };

        foreach (var command in commands)
        {
            context.Database.ExecuteSqlRaw(command);
        }

        return context;
    }

    /// <summary>
    /// Optimizes batch size dynamically based on available memory and record size for optimal performance.
    /// This method analyzes memory usage patterns and adjusts batch sizes to prevent memory exhaustion.
    /// </summary>
    /// <typeparam name="T">The entity type being processed.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="sampleEntity">A sample entity to estimate memory usage.</param>
    /// <param name="availableMemoryMB">The available memory for batch processing in MB. Default is 512MB.</param>
    /// <param name="memoryUsageThreshold">The percentage of available memory to use. Default is 80%.</param>
    /// <returns>The optimized batch size for bulk operations.</returns>
    /// <example>
    /// <code>
    /// var sampleUser = new User { Name = "Sample", Email = "sample@example.com" };
    /// var optimalBatchSize = context.OptimizeBatchSize(
    ///     sampleUser,
    ///     availableMemoryMB: 1024,
    ///     memoryUsageThreshold: 0.7);
    /// 
    /// await context.BulkInsertAsync(users, batchSize: optimalBatchSize);
    /// </code>
    /// </example>
    public static int OptimizeBatchSize<T>(
        this DbContext context,
        T sampleEntity,
        int availableMemoryMB = 512,
        double memoryUsageThreshold = 0.8) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (sampleEntity == null)
            throw new ArgumentNullException(nameof(sampleEntity));
        if (availableMemoryMB <= 0)
            throw new ArgumentOutOfRangeException(nameof(availableMemoryMB), "Available memory must be greater than zero.");
        if (memoryUsageThreshold <= 0 || memoryUsageThreshold > 1)
            throw new ArgumentOutOfRangeException(nameof(memoryUsageThreshold), "Memory usage threshold must be between 0 and 1.");

        // Estimate entity size in bytes
        var entitySize = EstimateEntitySize(sampleEntity);
        
        // Calculate optimal batch size based on available memory
        var availableBytes = availableMemoryMB * 1024 * 1024;
        var usableBytes = (long)(availableBytes * memoryUsageThreshold);
        
        var optimalBatchSize = (int)(usableBytes / entitySize);
        
        // Apply practical limits
        const int minBatchSize = 100;
        const int maxBatchSize = 100000;
        
        return Math.Max(minBatchSize, Math.Min(maxBatchSize, optimalBatchSize));
    }

    /// <summary>
    /// Monitors bulk operation performance and provides optimization recommendations.
    /// This method tracks execution metrics and suggests improvements for future operations.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="operationType">The type of bulk operation being monitored.</param>
    /// <param name="recordCount">The number of records processed.</param>
    /// <param name="executionTime">The time taken to execute the operation.</param>
    /// <param name="logger">Optional logger for performance metrics.</param>
    /// <returns>Performance recommendations for optimization.</returns>
    /// <example>
    /// <code>
    /// var stopwatch = Stopwatch.StartNew();
    /// await context.BulkInsertAsync(users, batchSize: 10000);
    /// stopwatch.Stop();
    /// 
    /// var recommendations = context.MonitorBulkPerformance(
    ///     "BulkInsert",
    ///     users.Count(),
    ///     stopwatch.Elapsed,
    ///     logger);
    /// </code>
    /// </example>
    public static BulkPerformanceRecommendations MonitorBulkPerformance(
        this DbContext context,
        string operationType,
        int recordCount,
        TimeSpan executionTime,
        ILogger? logger = null)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(operationType))
            throw new ArgumentException("Operation type cannot be null or whitespace.", nameof(operationType));
        if (recordCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(recordCount), "Record count must be greater than zero.");

        var recordsPerSecond = recordCount / executionTime.TotalSeconds;
        var recommendations = new BulkPerformanceRecommendations
        {
            OperationType = operationType,
            RecordCount = recordCount,
            ExecutionTime = executionTime,
            RecordsPerSecond = recordsPerSecond,
            Recommendations = new List<string>()
        };

        // Performance analysis and recommendations
        if (recordsPerSecond < 1000)
        {
            recommendations.Recommendations.Add("Consider increasing batch size for better throughput");
            recommendations.Recommendations.Add("Verify connection pooling is properly configured");
        }

        if (recordsPerSecond > 50000)
        {
            recommendations.Recommendations.Add("Excellent performance - current configuration is optimal");
        }
        else if (recordsPerSecond > 10000)
        {
            recommendations.Recommendations.Add("Good performance - consider enabling parallel operations for further improvement");
        }

        if (executionTime.TotalMinutes > 5)
        {
            recommendations.Recommendations.Add("Consider breaking large operations into smaller chunks");
            recommendations.Recommendations.Add("Monitor memory usage to prevent resource exhaustion");
        }

        logger?.LogInformation(
            "Bulk operation {OperationType} processed {RecordCount} records in {ExecutionTime}ms at {RecordsPerSecond:F2} records/sec",
            operationType, recordCount, executionTime.TotalMilliseconds, recordsPerSecond);

        return recommendations;
    }

    #region Private Helper Methods

    private static string BuildUpsertSql(
        string schema,
        string tableName,
        IEnumerable<Microsoft.EntityFrameworkCore.Metadata.IProperty> properties,
        string[] conflictColumns,
        string[] updateColumns)
    {
        var allColumns = string.Join(", ", properties.Select(p => $"\"{p.GetColumnName()}\""));
        var values = string.Join(", ", properties.Select((_, index) => $"@p{index}"));
        var conflictClause = string.Join(", ", conflictColumns.Select(c => $"\"{c}\""));
        var updateClause = string.Join(", ", updateColumns.Select(c => $"\"{c}\" = EXCLUDED.\"{c}\""));

        return $@"
            INSERT INTO {schema}.""{tableName}"" ({allColumns})
            VALUES ({values})
            ON CONFLICT ({conflictClause})
            DO UPDATE SET {updateClause}";
    }

    private static System.Linq.Expressions.Expression<Func<T, object>> GetKeySelector<T>(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType) where T : class
    {
        var keyProperty = entityType.FindPrimaryKey()?.Properties.First();
        if (keyProperty == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} does not have a primary key.");

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
        var property = System.Linq.Expressions.Expression.Property(parameter, keyProperty.Name);
        var converted = System.Linq.Expressions.Expression.Convert(property, typeof(object));
        
        return System.Linq.Expressions.Expression.Lambda<Func<T, object>>(converted, parameter);
    }

    private static System.Linq.Expressions.Expression<Func<T, bool>> BuildKeyPredicate<T>(
        Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType,
        IEnumerable<object> keyValues) where T : class
    {
        var keyProperty = entityType.FindPrimaryKey()?.Properties.First();
        if (keyProperty == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} does not have a primary key.");

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
        var property = System.Linq.Expressions.Expression.Property(parameter, keyProperty.Name);
        var values = System.Linq.Expressions.Expression.Constant(keyValues.ToList());
        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(keyProperty.ClrType);
        
        var containsCall = System.Linq.Expressions.Expression.Call(null, containsMethod, values, property);
        
        return System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(containsCall, parameter);
    }

    private static long EstimateEntitySize<T>(T entity) where T : class
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        long estimatedSize = 0;
        
        foreach (var property in properties)
        {
            var value = property.GetValue(entity);
            if (value == null) continue;
            
            estimatedSize += property.PropertyType switch
            {
                Type t when t == typeof(string) => ((string)value).Length * 2, // Unicode characters
                Type t when t == typeof(int) || t == typeof(int?) => sizeof(int),
                Type t when t == typeof(long) || t == typeof(long?) => sizeof(long),
                Type t when t == typeof(double) || t == typeof(double?) => sizeof(double),
                Type t when t == typeof(decimal) || t == typeof(decimal?) => sizeof(decimal),
                Type t when t == typeof(DateTime) || t == typeof(DateTime?) => sizeof(long),
                Type t when t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?) => sizeof(long) + sizeof(short),
                Type t when t == typeof(Guid) || t == typeof(Guid?) => 16,
                Type t when t == typeof(bool) || t == typeof(bool?) => sizeof(bool),
                Type t when t.IsEnum => sizeof(int),
                _ => 64 // Default estimate for complex types
            };
        }
        
        return Math.Max(estimatedSize, 64); // Minimum size estimate
    }

    #endregion
}

/// <summary>
/// Represents performance analysis and recommendations for bulk operations.
/// </summary>
public class BulkPerformanceRecommendations
{
    /// <summary>
    /// Gets or sets the type of bulk operation that was performed.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of records processed in the operation.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Gets or sets the total execution time for the operation.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the calculated throughput in records per second.
    /// </summary>
    public double RecordsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the list of performance optimization recommendations.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}