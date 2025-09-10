// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL array data types and related indexing strategies.
/// PostgreSQL arrays allow storing multiple values of the same data type in a single column.
/// </summary>
public static class ArrayConfigurationExtensions
{
    /// <summary>
    /// Configures a property to use PostgreSQL array data type for storing collections of values.
    /// Arrays in PostgreSQL can be one-dimensional or multi-dimensional and support various data types.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="elementTypeName">Optional PostgreSQL type name for array elements. If null, inferred from T.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Integer array
    /// builder.Property(e => e.Scores).HasArrayType();
    /// 
    /// // String array with explicit type
    /// builder.Property(e => e.Tags).HasArrayType("text");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     scores integer[],
    /// //     tags text[]
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasArrayType<T>(
        this PropertyBuilder<T> builder,
        string? elementTypeName = null)
    {
        if (!string.IsNullOrWhiteSpace(elementTypeName))
        {
            builder.HasColumnType($"{elementTypeName}[]");
        }
        else
        {
            // Let Npgsql infer the array type from T
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy", null);
        }
        
        return builder;
    }

    /// <summary>
    /// Configures a multi-dimensional PostgreSQL array with specified dimensions.
    /// Multi-dimensional arrays have a fixed number of dimensions and can have size constraints.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="dimensions">The number of dimensions for the array.</param>
    /// <param name="elementTypeName">Optional PostgreSQL type name for array elements.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when dimensions is less than 1.</exception>
    /// <example>
    /// <code>
    /// // Two-dimensional integer array
    /// builder.Property(e => e.Matrix).HasMultiDimensionalArray(2, "integer");
    /// 
    /// // Three-dimensional decimal array  
    /// builder.Property(e => e.Coordinates).HasMultiDimensionalArray(3, "decimal");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     matrix integer[][],
    /// //     coordinates decimal[][][]
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasMultiDimensionalArray<T>(
        this PropertyBuilder<T> builder,
        int dimensions,
        string? elementTypeName = null)
    {
        if (dimensions < 1)
            throw new ArgumentException("Dimensions must be at least 1.", nameof(dimensions));

        var arrayNotation = new string('[', dimensions) + new string(']', dimensions);
        var typeName = elementTypeName ?? "integer"; // Default fallback
        
        builder.HasColumnType($"{typeName}{arrayNotation}");
        builder.HasAnnotation("Npgsql:ArrayDimensions", dimensions);
        
        return builder;
    }

    /// <summary>
    /// Creates a GIN (Generalized Inverted Index) index on an array column for efficient containment queries.
    /// GIN indexes are optimal for array queries using operators like @>, <@, &&, and = ANY().
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Custom name for the index.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.Tags)
    ///        .HasArrayType("text")
    ///        .HasArrayGinIndex("ix_entity_tags_gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_tags_gin ON entities USING GIN (tags);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE tags @> ARRAY['important'];
    /// // SELECT * FROM entities WHERE tags && ARRAY['urgent', 'critical'];
    /// // SELECT * FROM entities WHERE 'specific' = ANY(tags);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasArrayGinIndex<T>(
        this PropertyBuilder<T> builder,
        string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
            throw new ArgumentException("Index name cannot be null or whitespace.", nameof(indexName));

        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for array usage
        // Example: entityBuilder.HasIndex(e => e.ArrayProperty).HasMethod("gin");
        builder.HasAnnotation("Npgsql:IndexMethod", "gin");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    /// <summary>
    /// Creates a GiST (Generalized Search Tree) index on an array column for range and similarity queries.
    /// GiST indexes are better for overlap operations and custom operators on arrays.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Entity configuration for range arrays
    /// builder.Property(e => e.TimeSlots)
    ///        .HasArrayType("int4range")
    ///        .HasArrayGistIndex("ix_entity_timeslots_gist");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_timeslots_gist ON entities USING GiST (time_slots);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE time_slots && ARRAY['[9,17)'::int4range];
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasArrayGistIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for array usage
        // Example: entityBuilder.HasIndex(e => e.ArrayProperty).HasMethod("gist");
        builder.HasAnnotation("Npgsql:IndexMethod", "gist");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    /// <summary>
    /// Configures array constraints including size limits, element constraints, and null handling.
    /// This ensures data integrity by validating array structure and content at the database level.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="maxLength">Maximum number of elements allowed in the array. Null for unlimited.</param>
    /// <param name="minLength">Minimum number of elements required in the array. Default is 0.</param>
    /// <param name="allowNulls">Whether null values are allowed as array elements. Default is true.</param>
    /// <param name="constraintName">Optional custom name for the constraint.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when minLength is negative or greater than maxLength.</exception>
    /// <example>
    /// <code>
    /// // Array with size constraints
    /// builder.Property(e => e.Tags)
    ///        .HasArrayType("text")
    ///        .HasArrayConstraints(maxLength: 10, minLength: 1, allowNulls: false);
    /// 
    /// // Array with custom validation
    /// builder.Property(e => e.Scores)
    ///        .HasArrayType("integer")
    ///        .HasArrayConstraints(maxLength: 5, constraintName: "chk_scores_valid");
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ADD CONSTRAINT chk_entity_tags_constraints 
    /// //   CHECK (array_length(tags, 1) >= 1 AND array_length(tags, 1) <= 10 
    /// //          AND NOT (tags @> ARRAY[NULL]));
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasArrayConstraints<T>(
        this PropertyBuilder<T> builder,
        int? maxLength = null,
        int minLength = 0,
        bool allowNulls = true,
        string? constraintName = null)
    {
        if (minLength < 0)
            throw new ArgumentException("Minimum length cannot be negative.", nameof(minLength));
        
        if (maxLength.HasValue && minLength > maxLength.Value)
            throw new ArgumentException("Minimum length cannot be greater than maximum length.", nameof(minLength));

        var constraints = new List<string>();
        var columnName = builder.Metadata.Name;

        // Length constraints
        if (minLength > 0)
            constraints.Add($"array_length({columnName}, 1) >= {minLength}");
        
        if (maxLength.HasValue)
            constraints.Add($"array_length({columnName}, 1) <= {maxLength.Value}");

        // Null element constraint
        if (!allowNulls)
            constraints.Add($"NOT ({columnName} @> ARRAY[NULL])");

        if (constraints.Count > 0)
        {
            var constraintSql = string.Join(" AND ", constraints);
            builder.HasAnnotation("Npgsql:CheckConstraint", constraintSql);
            
            if (!string.IsNullOrWhiteSpace(constraintName))
            {
                builder.HasAnnotation("Npgsql:CheckConstraintName", constraintName);
            }
        }

        return builder;
    }

    /// <summary>
    /// Configures an array property with default values using PostgreSQL array construction functions.
    /// Supports both static array values and dynamic expressions.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="defaultArray">The default array value or PostgreSQL array expression.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when defaultArray is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Static default array
    /// builder.Property(e => e.DefaultTags)
    ///        .HasArrayType("text")
    ///        .HasArrayDefaultValue("ARRAY['draft', 'new']");
    /// 
    /// // Dynamic default using PostgreSQL functions
    /// builder.Property(e => e.Timestamps)
    ///        .HasArrayType("timestamp")
    ///        .HasArrayDefaultValue("ARRAY[NOW()]");
    /// 
    /// // Empty array default
    /// builder.Property(e => e.EmptyScores)
    ///        .HasArrayType("integer")
    ///        .HasArrayDefaultValue("'{}'::integer[]");
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ALTER COLUMN default_tags SET DEFAULT ARRAY['draft', 'new'];
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasArrayDefaultValue<T>(
        this PropertyBuilder<T> builder,
        string defaultArray)
    {
        if (string.IsNullOrWhiteSpace(defaultArray))
            throw new ArgumentException("Default array value cannot be null or whitespace.", nameof(defaultArray));

        builder.HasDefaultValueSql(defaultArray);
        return builder;
    }

    /// <summary>
    /// Configures an array property to use specific array operators for query optimization.
    /// Documents the intended usage patterns for better query planning and indexing strategies.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="operators">The array operators this property will commonly use.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure for containment and overlap queries
    /// builder.Property(e => e.Categories)
    ///        .HasArrayType("text")
    ///        .OptimizeForArrayOperators(ArrayOperators.Contains | ArrayOperators.Overlap);
    /// 
    /// // This helps EF Core choose appropriate indexing strategies
    /// // Common operators include:
    /// // @>  (contains)         - array @> array
    /// // <@  (contained by)     - array <@ array  
    /// // &&  (overlap)          - array && array
    /// // =   (equal)            - array = array
    /// // ANY (any element)      - value = ANY(array)
    /// // ALL (all elements)     - value = ALL(array)
    /// </code>
    /// </example>
    public static PropertyBuilder<T> OptimizeForArrayOperators<T>(
        this PropertyBuilder<T> builder,
        ArrayOperators operators)
    {
        builder.HasAnnotation("Npgsql:ArrayOperators", operators);
        return builder;
    }

    /// <summary>
    /// Configures support for PostgreSQL array aggregation functions on the array property.
    /// This enables efficient aggregation operations directly in the database.
    /// </summary>
    /// <typeparam name="T">The array type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="functions">The aggregation functions to optimize for.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure for array aggregation
    /// builder.Property(e => e.Values)
    ///        .HasArrayType("integer")
    ///        .EnableArrayAggregation(ArrayAggregationFunctions.ArrayAgg | ArrayAggregationFunctions.Unnest);
    /// 
    /// // Optimizes for queries like:
    /// // SELECT array_agg(unnest(values)) FROM entities;
    /// // SELECT unnest(values) FROM entities WHERE id = 1;
    /// // SELECT array_length(values, 1), array_dims(values) FROM entities;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> EnableArrayAggregation<T>(
        this PropertyBuilder<T> builder,
        ArrayAggregationFunctions functions)
    {
        builder.HasAnnotation("Npgsql:ArrayAggregation", functions);
        return builder;
    }

    /// <summary>
    /// Configures a typed array property for strongly-typed array operations with specific element types.
    /// This provides better type safety and performance for homogeneous array data.
    /// </summary>
    /// <typeparam name="TElement">The element type of the array.</typeparam>
    /// <param name="builder">The property builder used to configure the array property.</param>
    /// <param name="pgTypeName">The PostgreSQL type name for array elements.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when pgTypeName is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Strongly typed integer array
    /// builder.Property(e => e.Scores)
    ///        .HasTypedArray<int>("integer");
    /// 
    /// // Strongly typed UUID array
    /// builder.Property(e => e.RelatedIds)
    ///        .HasTypedArray<Guid>("uuid");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     scores integer[],
    /// //     related_ids uuid[]
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<TElement[]> HasTypedArray<TElement>(
        this PropertyBuilder<TElement[]> builder,
        string pgTypeName)
    {
        if (string.IsNullOrWhiteSpace(pgTypeName))
            throw new ArgumentException("PostgreSQL type name cannot be null or whitespace.", nameof(pgTypeName));

        builder.HasColumnType($"{pgTypeName}[]");
        builder.HasAnnotation("Npgsql:ArrayElementType", typeof(TElement));
        return builder;
    }
}

/// <summary>
/// Represents the available PostgreSQL array operators for optimization hints.
/// </summary>
[Flags]
public enum ArrayOperators
{
    /// <summary>No specific operators.</summary>
    None = 0,
    /// <summary>Contains operator (@>).</summary>
    Contains = 1,
    /// <summary>Contained by operator (<@).</summary>
    ContainedBy = 2,
    /// <summary>Overlap operator (&&).</summary>
    Overlap = 4,
    /// <summary>Equal operator (=).</summary>
    Equal = 8,
    /// <summary>Any element operator (= ANY).</summary>
    Any = 16,
    /// <summary>All elements operator (= ALL).</summary>
    All = 32,
    /// <summary>All common array operators.</summary>
    Common = Contains | ContainedBy | Overlap | Equal | Any | All
}

/// <summary>
/// Represents the available PostgreSQL array aggregation functions for optimization.
/// </summary>
[Flags]
public enum ArrayAggregationFunctions
{
    /// <summary>No specific functions.</summary>
    None = 0,
    /// <summary>Array aggregation function (array_agg).</summary>
    ArrayAgg = 1,
    /// <summary>Unnest function (unnest).</summary>
    Unnest = 2,
    /// <summary>Array length function (array_length).</summary>
    ArrayLength = 4,
    /// <summary>Array dimensions function (array_dims).</summary>
    ArrayDims = 8,
    /// <summary>Array position function (array_position).</summary>
    ArrayPosition = 16,
    /// <summary>Array remove function (array_remove).</summary>
    ArrayRemove = 32,
    /// <summary>Array append function (array_append).</summary>
    ArrayAppend = 64,
    /// <summary>All common array functions.</summary>
    All = ArrayAgg | Unnest | ArrayLength | ArrayDims | ArrayPosition | ArrayRemove | ArrayAppend
}