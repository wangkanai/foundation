// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL-specific indexing strategies.
/// Supports all PostgreSQL index types including B-tree, Hash, GIN, GiST, SP-GiST, and BRIN.
/// </summary>
public static class IndexConfigurationExtensions
{
    #region B-tree Indexes (Default)

    /// <summary>
    /// Creates a standard B-tree index on the specified property.
    /// B-tree indexes are optimal for equality and range queries on ordered data.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="unique">Whether the index should enforce uniqueness.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Property(e => e.Email)
    ///        .HasBTreeIndex("ix_user_email", unique: true);
    /// 
    /// // Generated SQL:
    /// // CREATE UNIQUE INDEX ix_user_email ON users USING btree (email);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM users WHERE email = 'john@example.com';
    /// // SELECT * FROM users WHERE email > 'a@example.com' ORDER BY email;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasBTreeIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        bool unique = false)
    {
        var indexBuilder = builder.HasIndex();
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
        if (unique)
            indexBuilder.IsUnique();
        
        return builder;
    }

    #endregion

    #region GIN Indexes (Generalized Inverted Index)

    /// <summary>
    /// Creates a GIN (Generalized Inverted Index) index on the specified property.
    /// GIN indexes are optimal for data types that contain multiple component values,
    /// such as arrays, JSONB, and full-text search vectors.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="operatorClass">Optional operator class for specialized GIN operations.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // For arrays
    /// builder.Property(e => e.Tags)
    ///        .HasGinIndex("ix_entity_tags_gin");
    /// 
    /// // For JSONB with custom operator class
    /// builder.Property(e => e.Metadata)
    ///        .HasGinIndex("ix_entity_metadata_gin", "jsonb_ops");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_tags_gin ON entities USING gin (tags);
    /// // CREATE INDEX ix_entity_metadata_gin ON entities USING gin (metadata jsonb_ops);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE tags @> ARRAY['important'];
    /// // SELECT * FROM entities WHERE metadata @> '{"status": "active"}';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasGinIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        string? operatorClass = null)
    {
        var indexBuilder = builder.HasIndex().HasMethod("gin");
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (!string.IsNullOrWhiteSpace(operatorClass))
            indexBuilder.HasAnnotation("Npgsql:IndexOperatorClass", new[] { operatorClass });
        
        return builder;
    }

    /// <summary>
    /// Creates a GIN index with specific operator classes for different PostgreSQL data types.
    /// Provides fine-grained control over GIN indexing behavior.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="operatorClass">The GIN operator class to use.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // JSONB path operations
    /// builder.Property(e => e.Document)
    ///        .HasGinIndexWithOperatorClass(GinOperatorClass.JsonbPathOps, "ix_document_paths");
    /// 
    /// // Array operations
    /// builder.Property(e => e.Categories)
    ///        .HasGinIndexWithOperatorClass(GinOperatorClass.ArrayOps, "ix_categories_gin");
    /// 
    /// // Full-text search
    /// builder.Property(e => e.SearchVector)
    ///        .HasGinIndexWithOperatorClass(GinOperatorClass.TsvectorOps, "ix_search_gin");
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasGinIndexWithOperatorClass<T>(
        this PropertyBuilder<T> builder,
        GinOperatorClass operatorClass,
        string? indexName = null)
    {
        var opClass = operatorClass switch
        {
            GinOperatorClass.JsonbOps => "jsonb_ops",
            GinOperatorClass.JsonbPathOps => "jsonb_path_ops",
            GinOperatorClass.ArrayOps => "array_ops",
            GinOperatorClass.TsvectorOps => "tsvector_ops",
            _ => throw new ArgumentException($"Unsupported operator class: {operatorClass}", nameof(operatorClass))
        };

        return builder.HasGinIndex(indexName, opClass);
    }

    #endregion

    #region GiST Indexes (Generalized Search Tree)

    /// <summary>
    /// Creates a GiST (Generalized Search Tree) index on the specified property.
    /// GiST indexes are optimal for geometric data types, full-text search, and range types.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="operatorClass">Optional operator class for specialized GiST operations.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // For geometric types
    /// builder.Property(e => e.Location)
    ///        .HasGistIndex("ix_entity_location_gist", "point_ops");
    /// 
    /// // For range types
    /// builder.Property(e => e.DateRange)
    ///        .HasGistIndex("ix_entity_daterange_gist", "range_ops");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_location_gist ON entities USING gist (location point_ops);
    /// // CREATE INDEX ix_entity_daterange_gist ON entities USING gist (date_range range_ops);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE location <@ circle '((0,0), 10)';
    /// // SELECT * FROM entities WHERE date_range && '[2023-01-01, 2023-12-31]';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasGistIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        string? operatorClass = null)
    {
        var indexBuilder = builder.HasIndex().HasMethod("gist");
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (!string.IsNullOrWhiteSpace(operatorClass))
            indexBuilder.HasAnnotation("Npgsql:IndexOperatorClass", new[] { operatorClass });
        
        return builder;
    }

    #endregion

    #region SP-GiST Indexes (Space-Partitioned Generalized Search Tree)

    /// <summary>
    /// Creates an SP-GiST (Space-Partitioned Generalized Search Tree) index on the specified property.
    /// SP-GiST indexes are optimal for non-balanced data structures like quadtrees, k-d trees, and radix trees.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="operatorClass">Optional operator class for specialized SP-GiST operations.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // For point data (quadtree)
    /// builder.Property(e => e.Coordinates)
    ///        .HasSpGistIndex("ix_entity_coords_spgist", "point_ops");
    /// 
    /// // For text data (suffix tree)
    /// builder.Property(e => e.TextData)
    ///        .HasSpGistIndex("ix_entity_text_spgist", "text_ops");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_coords_spgist ON entities USING spgist (coordinates point_ops);
    /// // CREATE INDEX ix_entity_text_spgist ON entities USING spgist (text_data text_ops);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE coordinates <@ box '(1,1),(10,10)';
    /// // SELECT * FROM entities WHERE text_data ~ '^prefix';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasSpGistIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        string? operatorClass = null)
    {
        var indexBuilder = builder.HasIndex().HasMethod("spgist");
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (!string.IsNullOrWhiteSpace(operatorClass))
            indexBuilder.HasAnnotation("Npgsql:IndexOperatorClass", new[] { operatorClass });
        
        return builder;
    }

    #endregion

    #region BRIN Indexes (Block Range Index)

    /// <summary>
    /// Creates a BRIN (Block Range Index) index on the specified property.
    /// BRIN indexes are optimal for large tables with naturally ordered data,
    /// providing significant space savings while maintaining reasonable performance.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="pagesPerRange">Optional number of heap pages that each BRIN summary tuple summarizes.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // For time-series data
    /// builder.Property(e => e.CreatedAt)
    ///        .HasBrinIndex("ix_entity_created_brin", pagesPerRange: 128);
    /// 
    /// // For sequential IDs
    /// builder.Property(e => e.Id)
    ///        .HasBrinIndex("ix_entity_id_brin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_created_brin ON entities USING brin (created_at) WITH (pages_per_range = 128);
    /// // CREATE INDEX ix_entity_id_brin ON entities USING brin (id);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE created_at >= '2023-01-01' AND created_at < '2023-02-01';
    /// // SELECT * FROM entities WHERE id BETWEEN 1000 AND 2000;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasBrinIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        int? pagesPerRange = null)
    {
        var indexBuilder = builder.HasIndex().HasMethod("brin");
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (pagesPerRange.HasValue)
            indexBuilder.HasAnnotation("Npgsql:IndexStorageParameter:pages_per_range", pagesPerRange.Value);
        
        return builder;
    }

    #endregion

    #region Hash Indexes

    /// <summary>
    /// Creates a Hash index on the specified property.
    /// Hash indexes are optimal for simple equality comparisons and can be faster than B-tree for equality lookups.
    /// Note: Hash indexes are not WAL-logged and cannot be used on standby servers.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // For exact-match lookups
    /// builder.Property(e => e.StatusCode)
    ///        .HasHashIndex("ix_entity_status_hash");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_status_hash ON entities USING hash (status_code);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE status_code = 'ACTIVE';
    /// // Note: Does NOT optimize range queries or ORDER BY
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasHashIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        var indexBuilder = builder.HasIndex().HasMethod("hash");
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
        
        return builder;
    }

    #endregion

    #region Partial Indexes

    /// <summary>
    /// Creates a partial index with a WHERE clause condition.
    /// Partial indexes index only rows that satisfy the specified condition,
    /// reducing index size and maintenance overhead while maintaining performance for filtered queries.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="whereClause">The PostgreSQL WHERE clause condition for the partial index.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="indexMethod">Optional index method (btree, gin, gist, etc.). Defaults to btree.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when whereClause is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Index only active users
    /// builder.Property(e => e.Email)
    ///        .HasPartialIndex("status = 'ACTIVE'", "ix_active_user_email");
    /// 
    /// // Index only non-null values
    /// builder.Property(e => e.OptionalField)
    ///        .HasPartialIndex("optional_field IS NOT NULL", "ix_optional_field_notnull");
    /// 
    /// // Partial GIN index for JSONB
    /// builder.Property(e => e.Metadata)
    ///        .HasPartialIndex("metadata IS NOT NULL", "ix_metadata_gin", "gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_active_user_email ON users (email) WHERE status = 'ACTIVE';
    /// // CREATE INDEX ix_optional_field_notnull ON entities (optional_field) WHERE optional_field IS NOT NULL;
    /// // CREATE INDEX ix_metadata_gin ON entities USING gin (metadata) WHERE metadata IS NOT NULL;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasPartialIndex<T>(
        this PropertyBuilder<T> builder,
        string whereClause,
        string? indexName = null,
        string indexMethod = "btree")
    {
        if (string.IsNullOrWhiteSpace(whereClause))
            throw new ArgumentException("WHERE clause cannot be null or whitespace.", nameof(whereClause));

        var indexBuilder = builder.HasIndex();
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (indexMethod != "btree")
            indexBuilder.HasMethod(indexMethod);
            
        indexBuilder.HasFilter(whereClause);
        
        return builder;
    }

    #endregion

    #region Covering Indexes (INCLUDE columns)

    /// <summary>
    /// Creates a covering index that includes additional columns in the index for faster query execution.
    /// Covering indexes include non-key columns at the leaf level, enabling index-only scans.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="includeColumns">Array of column names to include in the index.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="unique">Whether the index should enforce uniqueness on the key columns.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when includeColumns is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Covering index for user lookups
    /// builder.Property(e => e.UserId)
    ///        .HasCoveringIndex(new[] { "first_name", "last_name", "email" }, "ix_user_covering");
    /// 
    /// // Unique covering index
    /// builder.Property(e => e.Username)
    ///        .HasCoveringIndex(new[] { "created_at", "last_login" }, "ix_username_covering", unique: true);
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_user_covering ON users (user_id) INCLUDE (first_name, last_name, email);
    /// // CREATE UNIQUE INDEX ix_username_covering ON users (username) INCLUDE (created_at, last_login);
    /// 
    /// // Enables index-only scans for queries like:
    /// // SELECT first_name, last_name, email FROM users WHERE user_id = 123;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasCoveringIndex<T>(
        this PropertyBuilder<T> builder,
        string[] includeColumns,
        string? indexName = null,
        bool unique = false)
    {
        if (includeColumns == null || includeColumns.Length == 0)
            throw new ArgumentException("Include columns cannot be null or empty.", nameof(includeColumns));

        var indexBuilder = builder.HasIndex();
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (unique)
            indexBuilder.IsUnique();
            
        indexBuilder.IncludeProperties(includeColumns);
        
        return builder;
    }

    #endregion

    #region Expression Indexes

    /// <summary>
    /// Creates an expression index using a custom PostgreSQL expression.
    /// Expression indexes allow indexing on computed values or function results.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="expression">The PostgreSQL expression to index.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="indexMethod">Optional index method (btree, gin, gist, etc.). Defaults to btree.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when expression is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Index on lowercase email for case-insensitive searches
    /// builder.Property(e => e.Email)
    ///        .HasExpressionIndex("lower(email)", "ix_email_lower");
    /// 
    /// // Index on extracted JSON field
    /// builder.Property(e => e.Metadata)
    ///        .HasExpressionIndex("(metadata->>'category')", "ix_metadata_category");
    /// 
    /// // GIN index on array elements
    /// builder.Property(e => e.Tags)
    ///        .HasExpressionIndex("unnest(tags)", "ix_tags_elements", "gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_email_lower ON users (lower(email));
    /// // CREATE INDEX ix_metadata_category ON entities ((metadata->>'category'));
    /// // CREATE INDEX ix_tags_elements ON entities USING gin (unnest(tags));
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM users WHERE lower(email) = 'john@example.com';
    /// // SELECT * FROM entities WHERE metadata->>'category' = 'important';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasExpressionIndex<T>(
        this PropertyBuilder<T> builder,
        string expression,
        string? indexName = null,
        string indexMethod = "btree")
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be null or whitespace.", nameof(expression));

        var indexBuilder = builder.HasIndex();
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (indexMethod != "btree")
            indexBuilder.HasMethod(indexMethod);
            
        // Store the expression for potential use in migrations
        indexBuilder.HasAnnotation("Npgsql:IndexExpression", expression);
        
        return builder;
    }

    #endregion

    #region Concurrent Index Creation

    /// <summary>
    /// Configures an index to be created concurrently, which allows table access during index creation.
    /// Concurrent creation takes longer but doesn't lock the table for reads and writes.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="indexMethod">Optional index method (btree, gin, gist, etc.). Defaults to btree.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Create index without locking the table
    /// builder.Property(e => e.LargeDatasetField)
    ///        .HasConcurrentIndex("ix_large_dataset_concurrent");
    /// 
    /// // Concurrent GIN index for JSONB
    /// builder.Property(e => e.Metadata)
    ///        .HasConcurrentIndex("ix_metadata_concurrent", "gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX CONCURRENTLY ix_large_dataset_concurrent ON large_table (large_dataset_field);
    /// // CREATE INDEX CONCURRENTLY ix_metadata_concurrent ON entities USING gin (metadata);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasConcurrentIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null,
        string indexMethod = "btree")
    {
        var indexBuilder = builder.HasIndex();
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (indexMethod != "btree")
            indexBuilder.HasMethod(indexMethod);
            
        indexBuilder.HasAnnotation("Npgsql:CreatedConcurrently", true);
        
        return builder;
    }

    #endregion

    #region Index Storage Parameters

    /// <summary>
    /// Configures storage parameters for the index to optimize performance and space usage.
    /// Storage parameters control various aspects of index behavior and maintenance.
    /// </summary>
    /// <typeparam name="T">The type of the property being indexed.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="parameters">Dictionary of storage parameters and their values.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <param name="indexMethod">Optional index method (btree, gin, gist, etc.). Defaults to btree.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Configure B-tree index fill factor
    /// builder.Property(e => e.UserId)
    ///        .HasIndexWithStorageParameters(
    ///            new Dictionary<string, object> { ["fillfactor"] = 90 },
    ///            "ix_user_id_optimized");
    /// 
    /// // Configure GIN index work memory
    /// builder.Property(e => e.SearchVector)
    ///        .HasIndexWithStorageParameters(
    ///            new Dictionary<string, object> { ["fastupdate"] = "off", ["gin_pending_list_limit"] = 4096 },
    ///            "ix_search_vector_gin", "gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_user_id_optimized ON entities (user_id) WITH (fillfactor = 90);
    /// // CREATE INDEX ix_search_vector_gin ON entities USING gin (search_vector) 
    /// //   WITH (fastupdate = off, gin_pending_list_limit = 4096);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasIndexWithStorageParameters<T>(
        this PropertyBuilder<T> builder,
        Dictionary<string, object> parameters,
        string? indexName = null,
        string indexMethod = "btree")
    {
        if (parameters == null || parameters.Count == 0)
            throw new ArgumentException("Storage parameters cannot be null or empty.", nameof(parameters));

        var indexBuilder = builder.HasIndex();
        
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);
            
        if (indexMethod != "btree")
            indexBuilder.HasMethod(indexMethod);

        foreach (var (key, value) in parameters)
        {
            indexBuilder.HasAnnotation($"Npgsql:IndexStorageParameter:{key}", value);
        }
        
        return builder;
    }

    #endregion
}

/// <summary>
/// Represents the available GIN operator classes for specialized indexing behavior.
/// </summary>
public enum GinOperatorClass
{
    /// <summary>Standard JSONB operations supporting all JSONB operators.</summary>
    JsonbOps,
    
    /// <summary>Path-specific JSONB operations optimized for containment queries.</summary>
    JsonbPathOps,
    
    /// <summary>Array operations for PostgreSQL arrays.</summary>
    ArrayOps,
    
    /// <summary>Full-text search vector operations.</summary>
    TsvectorOps
}