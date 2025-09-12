// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Extension methods for configuring JSON column support in SQLite Entity Framework Core entities.
/// Provides optimized JSON storage, indexing, and extraction capabilities specifically designed for SQLite.
/// </summary>
public static class JsonConfigurationExtensions
{
   /// <summary>
   /// Configures a property as a JSON column with optimized storage for SQLite.
   /// Applies JSON data type affinity and enables JSON1 extension functionality.
   /// </summary>
   /// <typeparam name="TEntity">The entity type being configured</typeparam>
   /// <typeparam name="TProperty">The property type to store as JSON</typeparam>
   /// <param name="propertyBuilder">The property builder instance</param>
   /// <param name="compressionEnabled">Whether to enable JSON compression for large documents</param>
   /// <returns>The property builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
   /// <remarks>
   /// This extension method optimizes JSON storage in SQLite by:
   /// - Setting appropriate column type for JSON affinity
   /// - Enabling JSON1 extension functions
   /// - Configuring optimal storage for JSON documents
   /// - Supporting compression for large JSON payloads
   /// </remarks>
   /// <example>
   /// <code>
   /// builder.Property(e => e.Settings)
   ///     .HasSqliteJsonColumn&lt;Settings&gt;(compressionEnabled: true);
   /// </code>
   /// </example>
   public static PropertyBuilder<TProperty> HasSqliteJsonColumn<TProperty>(
      this PropertyBuilder<TProperty> propertyBuilder,
      bool                            compressionEnabled = false)
   {
      ArgumentNullException.ThrowIfNull(propertyBuilder);

      return propertyBuilder
            .HasColumnType("JSON")
            .HasAnnotation("Sqlite:JsonColumn",      true)
            .HasAnnotation("Sqlite:JsonCompression", compressionEnabled)
            .HasConversion(
                           v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                           v => JsonSerializer.Deserialize<TProperty>(v, (JsonSerializerOptions?)null)!);
   }

   /// <summary>
   /// Creates a JSON path index for nested property queries in SQLite.
   /// Enables efficient querying of JSON document properties using SQLite's JSON1 extension.
   /// </summary>
   /// <typeparam name="TEntity">The entity type containing the JSON property</typeparam>
   /// <param name="entityBuilder">The entity builder instance</param>
   /// <param name="indexName">The name of the index to create</param>
   /// <param name="propertyName">The name of the JSON column property</param>
   /// <param name="jsonPath">The JSON path expression (e.g., "$.user.id", "$.settings.theme")</param>
   /// <param name="isUnique">Whether the index should enforce uniqueness</param>
   /// <returns>The entity builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
   /// <exception cref="ArgumentException">Thrown when strings are empty or whitespace</exception>
   /// <remarks>
   /// JSON path indexes significantly improve query performance for nested JSON properties.
   /// The index is created using SQLite's JSON_EXTRACT function for optimal performance.
   /// Common JSON path patterns:
   /// - "$.property" - Root level property
   /// - "$.object.property" - Nested object property
   /// - "$[0].property" - Array element property
   /// - "$.array[*].property" - All array elements
   /// </remarks>
   /// <example>
   /// <code>
   /// builder.HasSqliteJsonPath("IX_User_Email", "Settings", "$.user.email", isUnique: true);
   /// builder.HasSqliteJsonPath("IX_Config_Theme", "Configuration", "$.ui.theme");
   /// </code>
   /// </example>
   public static EntityTypeBuilder<TEntity> HasSqliteJsonPath<TEntity>(
      this EntityTypeBuilder<TEntity> entityBuilder,
      string                          indexName,
      string                          propertyName,
      string                          jsonPath,
      bool                            isUnique = false)
      where TEntity : class
   {
      ArgumentNullException.ThrowIfNull(entityBuilder);
      ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
      ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
      ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);

      var indexBuilder = entityBuilder.HasIndex($"json_extract(\"{propertyName}\", '{jsonPath}')")
                                      .HasDatabaseName(indexName)
                                      .HasAnnotation("Sqlite:JsonPath",     jsonPath)
                                      .HasAnnotation("Sqlite:JsonProperty", propertyName);

      if (isUnique)
         indexBuilder.IsUnique();

      return entityBuilder;
   }

   /// <summary>
   /// Creates computed columns for commonly accessed JSON properties to improve query performance.
   /// Extracts specific JSON values into dedicated columns with proper SQLite type affinity.
   /// </summary>
   /// <typeparam name="TEntity">The entity type containing the JSON property</typeparam>
   /// <typeparam name="TExtracted">The type of the extracted property</typeparam>
   /// <param name="entityBuilder">The entity builder instance</param>
   /// <param name="computedPropertyExpression">Expression selecting the computed property</param>
   /// <param name="jsonPropertyName">The name of the source JSON column</param>
   /// <param name="jsonPath">The JSON path to extract</param>
   /// <param name="sqliteType">The SQLite type affinity for the computed column</param>
   /// <returns>The entity builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
   /// <exception cref="ArgumentException">Thrown when strings are empty or whitespace</exception>
   /// <remarks>
   /// Computed columns provide significant performance benefits for frequently queried JSON properties.
   /// The extracted value is stored as a computed column with proper type affinity for optimal indexing.
   /// Supported SQLite types: TEXT, INTEGER, REAL, BLOB, NUMERIC
   /// </remarks>
   /// <example>
   /// <code>
   /// builder.HasSqliteJsonExtraction&lt;string&gt;(
   ///     e => e.UserEmail,
   ///     "Settings", 
   ///     "$.user.email", 
   ///     "TEXT");
   ///     
   /// builder.HasSqliteJsonExtraction&lt;int&gt;(
   ///     e => e.Priority,
   ///     "Configuration", 
   ///     "$.task.priority", 
   ///     "INTEGER");
   /// </code>
   /// </example>
   public static EntityTypeBuilder<TEntity> HasSqliteJsonExtraction<TEntity, TExtracted>(
      this EntityTypeBuilder<TEntity>       entityBuilder,
      Expression<Func<TEntity, TExtracted>> computedPropertyExpression,
      string                                jsonPropertyName,
      string                                jsonPath,
      string                                sqliteType = "TEXT")
      where TEntity : class
   {
      ArgumentNullException.ThrowIfNull(entityBuilder);
      ArgumentNullException.ThrowIfNull(computedPropertyExpression);
      ArgumentException.ThrowIfNullOrWhiteSpace(jsonPropertyName);
      ArgumentException.ThrowIfNullOrWhiteSpace(jsonPath);
      ArgumentException.ThrowIfNullOrWhiteSpace(sqliteType);

      var validTypes = new[] { "TEXT", "INTEGER", "REAL", "BLOB", "NUMERIC" };
      if (!validTypes.Contains(sqliteType.ToUpperInvariant()))
         throw new ArgumentException($"Invalid SQLite type: {sqliteType}. Valid types are: {string.Join(", ", validTypes)}", nameof(sqliteType));

      entityBuilder
        .Property(computedPropertyExpression)
        .HasComputedColumnSql($"json_extract(\"{jsonPropertyName}\", '{jsonPath}')")
        .HasColumnType(sqliteType)
        .HasAnnotation("Sqlite:JsonExtraction", true)
        .HasAnnotation("Sqlite:JsonPath",       jsonPath)
        .HasAnnotation("Sqlite:SourceProperty", jsonPropertyName)
        .HasAnnotation("Sqlite:ExtractedType",  sqliteType);

      return entityBuilder;
   }

   /// <summary>
   /// Creates multiple JSON path indexes for a single JSON column to optimize common query patterns.
   /// Provides a convenient way to create multiple indexes for frequently accessed nested properties.
   /// </summary>
   /// <typeparam name="TEntity">The entity type containing the JSON property</typeparam>
   /// <param name="entityBuilder">The entity builder instance</param>
   /// <param name="propertyName">The name of the JSON column property</param>
   /// <param name="pathConfigurations">Dictionary of index name to JSON path mappings</param>
   /// <returns>The entity builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
   /// <exception cref="ArgumentException">Thrown when configurations are empty</exception>
   /// <remarks>
   /// This method is useful when you have multiple frequently queried paths within the same JSON document.
   /// Each path gets its own optimized index for maximum query performance.
   /// </remarks>
   /// <example>
   /// <code>
   /// var pathConfigs = new Dictionary&lt;string, string&gt;
   /// {
   ///     { "IX_User_Email", "$.user.email" },
   ///     { "IX_User_Name", "$.user.name" },
   ///     { "IX_Settings_Theme", "$.settings.theme" }
   /// };
   /// builder.HasSqliteJsonPaths("UserData", pathConfigs);
   /// </code>
   /// </example>
   public static EntityTypeBuilder<TEntity> HasSqliteJsonPaths<TEntity>(
      this EntityTypeBuilder<TEntity> entityBuilder,
      string                          propertyName,
      Dictionary<string, string>      pathConfigurations)
      where TEntity : class
   {
      ArgumentNullException.ThrowIfNull(entityBuilder);
      ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
      ArgumentNullException.ThrowIfNull(pathConfigurations);

      if (!pathConfigurations.Any())
         throw new ArgumentException("Path configurations cannot be empty", nameof(pathConfigurations));

      foreach (var (indexName, jsonPath) in pathConfigurations)
         entityBuilder.HasSqliteJsonPath(indexName, propertyName, jsonPath);

      return entityBuilder;
   }

   /// <summary>
   /// Configures full-text search on JSON content using SQLite's FTS5 extension.
   /// Enables efficient text searching within JSON documents.
   /// </summary>
   /// <typeparam name="TEntity">The entity type containing the JSON property</typeparam>
   /// <param name="entityBuilder">The entity builder instance</param>
   /// <param name="ftsTableName">The name of the FTS virtual table</param>
   /// <param name="jsonPropertyName">The name of the JSON column property</param>
   /// <param name="searchPaths">JSON paths to include in full-text search</param>
   /// <returns>The entity builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
   /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
   /// <remarks>
   /// Full-text search on JSON properties provides powerful search capabilities across JSON content.
   /// This creates an FTS5 virtual table with triggers to maintain search index synchronization.
   /// </remarks>
   /// <example>
   /// <code>
   /// builder.HasSqliteJsonFullTextSearch(
   ///     "UserSearch", 
   ///     "Profile", 
   ///     new[] { "$.name", "$.bio", "$.skills[*]" });
   /// </code>
   /// </example>
   public static EntityTypeBuilder<TEntity> HasSqliteJsonFullTextSearch<TEntity>(
      this EntityTypeBuilder<TEntity> entityBuilder,
      string                          ftsTableName,
      string                          jsonPropertyName,
      string[]                        searchPaths)
      where TEntity : class
   {
      ArgumentNullException.ThrowIfNull(entityBuilder);
      ArgumentException.ThrowIfNullOrWhiteSpace(ftsTableName);
      ArgumentException.ThrowIfNullOrWhiteSpace(jsonPropertyName);
      ArgumentNullException.ThrowIfNull(searchPaths);

      if (!searchPaths.Any())
         throw new ArgumentException("Search paths cannot be empty", nameof(searchPaths));

      return entityBuilder
            .HasAnnotation("Sqlite:FtsTable",        ftsTableName)
            .HasAnnotation("Sqlite:FtsJsonProperty", jsonPropertyName)
            .HasAnnotation("Sqlite:FtsSearchPaths",  searchPaths);
   }
}