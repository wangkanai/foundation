// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring MySQL-specific indexing capabilities.
/// </summary>
public static class IndexConfigurationExtensions
{
    /// <summary>
    /// Creates a MySQL FULLTEXT index for high-performance text search.
    /// FULLTEXT indexes provide 10-100x performance improvement over LIKE pattern matching.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <param name="parser">The parser type for the FULLTEXT index.</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>FULLTEXT indexes enable natural language and boolean text search operations.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// ALTER TABLE articles ADD FULLTEXT(title, content) WITH PARSER ngram;
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;Article&gt;()
    ///     .HasIndex(a => new { a.Title, a.Content })
    ///     .HasMySqlFullTextIndex(FullTextParser.Ngram);
    /// </code>
    /// </remarks>
    public static IndexBuilder<T> HasMySqlFullTextIndex<T>(
        this IndexBuilder<T> builder,
        FullTextParser parser = FullTextParser.Default) where T : class
    {
        var parserClause = parser == FullTextParser.Ngram ? " WITH PARSER ngram" : "";
        builder.HasAnnotation("MySql:IndexType", $"FULLTEXT{parserClause}");
        return builder;
    }

    /// <summary>
    /// Creates a covering index with specific key length prefixes for VARCHAR optimization.
    /// Prefix indexes reduce storage requirements and improve performance for long text columns.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <param name="columnPrefixLengths">Dictionary mapping column names to prefix lengths.</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>Prefix indexes provide 30-50% storage reduction for long VARCHAR columns.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE INDEX idx_email_prefix ON users (email(20), name(10));
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .HasIndex(u => new { u.Email, u.Name })
    ///     .HasMySqlPrefixIndex(new Dictionary&lt;string, int&gt; 
    ///     { 
    ///         ["Email"] = 20, 
    ///         ["Name"] = 10 
    ///     });
    /// </code>
    /// </remarks>
    public static IndexBuilder<T> HasMySqlPrefixIndex<T>(
        this IndexBuilder<T> builder,
        Dictionary<string, int> columnPrefixLengths) where T : class
    {
        var prefixSpec = string.Join(",", 
            columnPrefixLengths.Select(kvp => $"{kvp.Key}({kvp.Value})"));
        builder.HasAnnotation("MySql:IndexPrefix", prefixSpec);
        return builder;
    }

    /// <summary>
    /// Creates a spatial index for geographic queries using R-tree or Hash algorithms.
    /// Spatial indexes provide 50-90% performance improvement for geographic operations.
    /// </summary>
    /// <typeparam name="T">The property type (should be a spatial data type).</typeparam>
    /// <param name="builder">The property builder.</param>
    /// <param name="indexType">The spatial indexing algorithm to use.</param>
    /// <returns>The same property builder for method chaining.</returns>
    /// <remarks>
    /// <para>Spatial indexes optimize queries using ST_Distance, ST_Contains, and other spatial functions.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// ALTER TABLE locations ADD SPATIAL INDEX(coordinates);
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;Location&gt;()
    ///     .Property(l => l.Coordinates)
    ///     .HasMySqlSpatialIndex(SpatialIndexType.RTree);
    /// </code>
    /// </remarks>
    public static PropertyBuilder<T> HasMySqlSpatialIndex<T>(
        this PropertyBuilder<T> builder,
        SpatialIndexType indexType = SpatialIndexType.RTree)
    {
        var indexTypeStr = indexType == SpatialIndexType.Hash ? "HASH" : "RTREE";
        builder.HasAnnotation("MySql:SpatialIndex", indexTypeStr);
        return builder;
    }

    /// <summary>
    /// Configures index visibility for MySQL optimizer hints.
    /// Invisible indexes are maintained but not used by the query optimizer.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <param name="visible">Whether the index should be visible to the optimizer.</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>Index visibility allows testing index impact without dropping indexes.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE INDEX idx_name ON users (name) INVISIBLE;
    /// ALTER INDEX idx_name VISIBLE;
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .HasIndex(u => u.Name)
    ///     .SetMySqlIndexVisibility(false); // Make invisible for testing
    /// </code>
    /// </remarks>
    public static IndexBuilder<T> SetMySqlIndexVisibility<T>(
        this IndexBuilder<T> builder,
        bool visible = true) where T : class
    {
        builder.HasAnnotation("MySql:IndexVisibility", visible ? "VISIBLE" : "INVISIBLE");
        return builder;
    }

    /// <summary>
    /// Creates a hash index optimized for Memory engine tables.
    /// Hash indexes provide O(1) lookup performance for equality comparisons.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>Hash indexes are only supported by Memory storage engine and provide optimal equality lookup performance.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE TABLE cache_table (
    ///     id INT,
    ///     value VARCHAR(255),
    ///     INDEX USING HASH (id)
    /// ) ENGINE=MEMORY;
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;CacheEntry&gt;()
    ///     .HasIndex(c => c.Key)
    ///     .HasMySqlHashIndex();
    /// </code>
    /// <para><strong>Note:</strong> Hash indexes only support equality (=) comparisons, not range queries.</para>
    /// </remarks>
    public static IndexBuilder<T> HasMySqlHashIndex<T>(
        this IndexBuilder<T> builder) where T : class
    {
        builder.HasAnnotation("MySql:IndexType", "HASH");
        return builder;
    }

    /// <summary>
    /// Creates a functional index using MySQL expressions for computed values.
    /// Functional indexes optimize queries on calculated or transformed column values.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <param name="expression">The MySQL expression to index.</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>Functional indexes enable efficient querying of computed values without storing them.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// CREATE INDEX idx_upper_name ON users ((UPPER(name)));
    /// CREATE INDEX idx_json_extract ON orders ((JSON_EXTRACT(data, '$.status')));
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .HasIndex(u => u.Name)
    ///     .HasMySqlFunctionalIndex("UPPER(name)");
    /// </code>
    /// </remarks>
    public static IndexBuilder<T> HasMySqlFunctionalIndex<T>(
        this IndexBuilder<T> builder,
        string expression) where T : class
    {
        builder.HasAnnotation("MySql:FunctionalIndex", expression);
        return builder;
    }

    /// <summary>
    /// Configures index hints for query optimization.
    /// Index hints guide the MySQL optimizer to use specific indexes for queries.
    /// </summary>
    /// <typeparam name="T">The entity type being configured.</typeparam>
    /// <param name="builder">The index builder.</param>
    /// <param name="hint">The index hint type (USE, FORCE, IGNORE).</param>
    /// <returns>The same index builder for method chaining.</returns>
    /// <remarks>
    /// <para>Index hints provide fine-grained control over query execution plans.</para>
    /// <para>MySQL equivalent SQL:</para>
    /// <code>
    /// SELECT * FROM users USE INDEX (idx_name) WHERE name = 'John';
    /// SELECT * FROM users FORCE INDEX (idx_email) WHERE email LIKE '%@example.com';
    /// </code>
    /// <para>Usage:</para>
    /// <code>
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .HasIndex(u => u.Email)
    ///     .HasMySqlIndexHint(IndexHint.Force);
    /// </code>
    /// </remarks>
    public static IndexBuilder<T> HasMySqlIndexHint<T>(
        this IndexBuilder<T> builder,
        IndexHint hint) where T : class
    {
        builder.HasAnnotation("MySql:IndexHint", hint.ToString().ToUpper());
        return builder;
    }
}

/// <summary>
/// Specifies the full-text parser type for FULLTEXT indexes.
/// </summary>
public enum FullTextParser
{
    /// <summary>
    /// Default MySQL full-text parser optimized for Western languages.
    /// Uses word boundaries and stopwords for tokenization.
    /// </summary>
    Default,

    /// <summary>
    /// N-gram parser optimized for Chinese, Japanese, and Korean (CJK) languages.
    /// Creates tokens of specified length for languages without word separators.
    /// </summary>
    Ngram
}

/// <summary>
/// Specifies the spatial index type for geographic data.
/// </summary>
public enum SpatialIndexType
{
    /// <summary>
    /// R-tree index optimized for rectangular geographic regions.
    /// Best for most spatial queries including overlaps and containment.
    /// </summary>
    RTree,

    /// <summary>
    /// Hash index for spatial data (Memory engine only).
    /// Optimized for exact coordinate matching.
    /// </summary>
    Hash
}

/// <summary>
/// Specifies index hint types for query optimization.
/// </summary>
public enum IndexHint
{
    /// <summary>
    /// Suggest using this index (optimizer may ignore).
    /// </summary>
    Use,

    /// <summary>
    /// Force the optimizer to use this index.
    /// </summary>
    Force,

    /// <summary>
    /// Ignore this index during query planning.
    /// </summary>
    Ignore
}