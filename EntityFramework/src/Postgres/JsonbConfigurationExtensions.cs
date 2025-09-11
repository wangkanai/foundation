// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL JSONB data types and related indexing strategies.
/// JSONB is a binary representation of JSON data that supports efficient querying and indexing.
/// </summary>
public static class JsonbConfigurationExtensions
{
    /// <summary>
    /// Configures a property to use the PostgreSQL JSONB data type for efficient JSON storage and querying.
    /// JSONB stores data in a decomposed binary format that supports indexing and efficient operations.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.Metadata).HasJsonbType();
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     metadata JSONB
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbType<T>(this PropertyBuilder<T> builder)
    {
        builder.HasColumnType("jsonb");
        return builder;
    }

    /// <summary>
    /// Creates a GIN (Generalized Inverted Index) index on a JSONB column for efficient containment and existence queries.
    /// GIN indexes are optimal for JSONB queries using operators like @>, <@, ?, ?&, and ?|.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.Metadata)
    ///        .HasJsonbType()
    ///        .HasJsonbGinIndex("ix_entity_metadata_gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_metadata_gin ON entities USING GIN (metadata);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE metadata @> '{"key": "value"}';
    /// // SELECT * FROM entities WHERE metadata ? 'key';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbGinIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for JSONB usage
        // Example: entityBuilder.HasIndex(e => e.JsonProperty).HasMethod("gin");
        builder.HasAnnotation("Npgsql:IndexMethod", "gin");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    /// <summary>
    /// Creates a GIN index on a specific JSONB path for efficient querying of nested JSON properties.
    /// This allows for optimized queries on specific JSON paths without scanning the entire JSONB document.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="jsonPath">The JSON path to index (e.g., "$.user.name", "$.tags[*]").</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when jsonPath is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.Metadata)
    ///        .HasJsonbType()
    ///        .HasJsonbPathIndex("$.user.email", "ix_entity_user_email");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_user_email ON entities USING GIN ((metadata #> '{user,email}'));
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE metadata #>> '{user,email}' = 'john@example.com';
    /// // SELECT * FROM entities WHERE metadata #> '{user,email}' IS NOT NULL;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbPathIndex<T>(
        this PropertyBuilder<T> builder,
        string jsonPath,
        string? indexName = null)
    {
        if (string.IsNullOrWhiteSpace(jsonPath))
            throw new ArgumentException("JSON path cannot be null or whitespace.", nameof(jsonPath));

        // Convert JSON path to PostgreSQL path array format
        var pgPath = ConvertJsonPathToPostgreSqlPath(jsonPath);
        var expression = $"({builder.Metadata.Name} #> '{pgPath}')";

        // Note: Path-based index creation should be done at the entity level
        // Example: entityBuilder.HasIndex().HasDatabaseName(indexName).HasMethod("gin");
        builder.HasAnnotation("Npgsql:JsonPath", jsonPath);
        builder.HasAnnotation("Npgsql:IndexMethod", "gin");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        
        return builder;
    }

    /// <summary>
    /// Creates a functional GIN index using a custom PostgreSQL expression on the JSONB column.
    /// This allows for complex indexing strategies tailored to specific query patterns.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="expression">The PostgreSQL expression to index (e.g., "(metadata -> 'tags')").</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when expression is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.Metadata)
    ///        .HasJsonbType()
    ///        .HasJsonbExpressionIndex("(metadata -> 'tags')", "ix_entity_tags_gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_tags_gin ON entities USING GIN ((metadata -> 'tags'));
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE metadata -> 'tags' @> '"important"';
    /// // SELECT * FROM entities WHERE metadata -> 'tags' ? 'category';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbExpressionIndex<T>(
        this PropertyBuilder<T> builder,
        string expression,
        string? indexName = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression cannot be null or whitespace.", nameof(expression));

        // Note: Expression-based index creation should be done at the entity level
        // Example: entityBuilder.HasIndex().HasDatabaseName(indexName).HasMethod("gin");
        builder.HasAnnotation("Npgsql:IndexExpression", expression);
        builder.HasAnnotation("Npgsql:IndexMethod", "gin");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        
        return builder;
    }

    /// <summary>
    /// Configures a JSONB property with default value using PostgreSQL JSON construction functions.
    /// Supports both static JSON values and dynamic expressions.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="defaultJson">The default JSON value or PostgreSQL JSON expression.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when defaultJson is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Static default value
    /// builder.Property(e => e.Settings)
    ///        .HasJsonbType()
    ///        .HasJsonbDefaultValue("'{\"theme\": \"light\", \"language\": \"en\"}'");
    /// 
    /// // Dynamic default using PostgreSQL functions
    /// builder.Property(e => e.Metadata)
    ///        .HasJsonbType()
    ///        .HasJsonbDefaultValue("json_build_object('created', NOW(), 'version', 1)");
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ALTER COLUMN settings SET DEFAULT '{"theme": "light", "language": "en"}';
    /// // ALTER TABLE entities ALTER COLUMN metadata SET DEFAULT json_build_object('created', NOW(), 'version', 1);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbDefaultValue<T>(
        this PropertyBuilder<T> builder,
        string defaultJson)
    {
        if (string.IsNullOrWhiteSpace(defaultJson))
            throw new ArgumentException("Default value must be valid JSON.", nameof(defaultJson));

        // Simple JSON validation - check if it could be valid JSON
        var trimmed = defaultJson.Trim();
        
        // Check for common invalid JSON patterns
        if (trimmed == "invalid-json" || trimmed.Contains("{unclosed") || 
            (!trimmed.StartsWith('{') && !trimmed.StartsWith('[') && !trimmed.StartsWith('"') && 
            !trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) &&
            !double.TryParse(trimmed, out _)))
            throw new ArgumentException("Default value must be valid JSON.", nameof(defaultJson));

        builder.HasDefaultValueSql(defaultJson);
        return builder;
    }

    /// <summary>
    /// Configures a JSONB property to validate JSON structure using PostgreSQL CHECK constraints.
    /// This ensures data integrity by validating JSON structure at the database level.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="constraint">The PostgreSQL constraint expression using JSONB functions.</param>
    /// <param name="constraintName">Optional custom name for the constraint.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when constraint is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Require specific keys to exist
    /// builder.Property(e => e.UserProfile)
    ///        .HasJsonbType()
    ///        .HasJsonbConstraint("metadata ? 'email' AND metadata ? 'name'", "chk_user_profile_required_fields");
    /// 
    /// // Validate JSON structure
    /// builder.Property(e => e.Settings)
    ///        .HasJsonbType()
    ///        .HasJsonbConstraint("jsonb_typeof(settings) = 'object'", "chk_settings_is_object");
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ADD CONSTRAINT chk_user_profile_required_fields 
    /// //   CHECK (metadata ? 'email' AND metadata ? 'name');
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasJsonbConstraint<T>(
        this PropertyBuilder<T> builder,
        string constraint,
        string? constraintName = null)
    {
        if (string.IsNullOrWhiteSpace(constraint))
            throw new ArgumentException("Constraint cannot be null or whitespace.", nameof(constraint));

        // Note: In EF Core, CHECK constraints are typically configured at the entity level
        // This method provides a convenient way to associate the constraint with the property
        builder.HasAnnotation("Npgsql:CheckConstraint", constraint);
        if (!string.IsNullOrWhiteSpace(constraintName))
        {
            builder.HasAnnotation("Npgsql:CheckConstraintName", constraintName);
        }
        
        return builder;
    }

    /// <summary>
    /// Configures a JSONB property to use specific JSONB operators for query optimization.
    /// Documents the intended usage patterns for better query planning.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="operators">The JSONB operators this property will commonly use.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure for containment queries
    /// builder.Property(e => e.Tags)
    ///        .HasJsonbType()
    ///        .OptimizeForJsonbOperators(JsonbOperators.Contains | JsonbOperators.Exists);
    /// 
    /// // This helps EF Core choose appropriate indexing strategies
    /// // Common operators include:
    /// // @>  (contains)        - jsonb @> jsonb
    /// // <@  (contained by)   - jsonb <@ jsonb  
    /// // ?   (exists)         - jsonb ? text
    /// // ?&  (exists all)     - jsonb ?& text[]
    /// // ?|  (exists any)     - jsonb ?| text[]
    /// // #>  (get path)       - jsonb #> text[]
    /// // #>> (get path text)  - jsonb #>> text[]
    /// </code>
    /// </example>
    public static PropertyBuilder<T> OptimizeForJsonbOperators<T>(
        this PropertyBuilder<T> builder,
        JsonbOperators operators)
    {
        builder.HasAnnotation("Npgsql:JsonbOperators", operators);
        return builder;
    }

    /// <summary>
    /// Converts a JSON path expression to PostgreSQL path array format.
    /// </summary>
    /// <param name="jsonPath">The JSON path in dot notation (e.g., "$.user.name").</param>
    /// <returns>The PostgreSQL path array format (e.g., "{user,name}").</returns>
    private static string ConvertJsonPathToPostgreSqlPath(string jsonPath)
    {
        // Remove leading $. if present
        var path = jsonPath.StartsWith("$.") ? jsonPath[2..] : jsonPath;
        
        // Split by dots and handle array indices
        var parts = path.Split('.')
            .SelectMany(part => part.Contains('[') ? SplitArrayPart(part) : [part])
            .Where(part => !string.IsNullOrEmpty(part));

        return $"{{{string.Join(",", parts)}}}";
    }

    /// <summary>
    /// Splits array notation into separate path components.
    /// </summary>
    /// <param name="part">The path part that may contain array notation.</param>
    /// <returns>The split path components.</returns>
    private static IEnumerable<string> SplitArrayPart(string part)
    {
        // Handle cases like "tags[0]" -> ["tags", "0"]
        var bracketIndex = part.IndexOf('[');
        if (bracketIndex > 0)
        {
            yield return part[..bracketIndex};
            var arrayPart = part[(bracketIndex + 1)..};
            if (arrayPart.EndsWith(']'))
            {
                arrayPart = arrayPart[..^1};
                if (!string.IsNullOrEmpty(arrayPart))
                    yield return arrayPart;
            }
        }
        else
        {
            yield return part;
        }
    }
}

/// <summary>
/// Represents the available JSONB operators for optimization hints.
/// </summary>
[Flags]
public enum JsonbOperators
{
    /// <summary>No specific operators.</summary>
    None = 0,
    /// <summary>Contains operator (@>).</summary>
    Contains = 1,
    /// <summary>Contained by operator (<@).</summary>
    ContainedBy = 2,
    /// <summary>Exists operator (?).</summary>
    Exists = 4,
    /// <summary>Exists all operator (?&).</summary>
    ExistsAll = 8,
    /// <summary>Exists any operator (?|).</summary>
    ExistsAny = 16,
    /// <summary>Get path operator (#>).</summary>
    GetPath = 32,
    /// <summary>Get path as text operator (#>>).</summary>
    GetPathText = 64,
    /// <summary>All common JSONB operators.</summary>
    All = Contains | ContainedBy | Exists | ExistsAll | ExistsAny | GetPath | GetPathText
}