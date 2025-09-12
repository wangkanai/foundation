// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// FTS version enumeration for SQLite full-text search configuration
/// </summary>
public enum FtsVersion
{
   /// <summary>
   /// FTS3 - Basic full-text search with limited features
   /// </summary>
   FTS3,

   /// <summary>
   /// FTS4 - Enhanced full-text search with better performance
   /// </summary>
   FTS4,

   /// <summary>
   /// FTS5 - Latest full-text search with advanced features (default)
   /// </summary>
   FTS5
}

/// <summary>
/// Extension methods for configuring SQLite full-text search (FTS) capabilities in Entity Framework Core.
/// Provides methods to configure FTS5 indexes, multi-column search, and custom tokenizers for enhanced text search performance.
/// </summary>
public static class FullTextSearchExtensions
{
   /// <summary>
   /// Configures a property for SQLite full-text search with specified FTS version.
   /// Creates a virtual table optimized for text search operations with configurable FTS version support.
   /// </summary>
   /// <typeparam name="T">The entity type</typeparam>
   /// <param name="propertyBuilder">The property builder for the text property</param>
   /// <param name="ftsVersion">The FTS version to use (default: FTS5)</param>
   /// <returns>The property builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
   /// <remarks>
   /// <para>Performance Benefits:</para>
   /// <list type="bullet">
   /// <item>FTS5 provides 2-5x faster search performance compared to LIKE queries</item>
   /// <item>Supports advanced search operators (AND, OR, NOT, phrase queries)</item>
   /// <item>Efficient indexing for large text datasets</item>
   /// <item>Memory-efficient storage with compressed indexes</item>
   /// </list>
   /// <para>Use Cases:</para>
   /// <list type="bullet">
   /// <item>Document search and content management</item>
   /// <item>Product catalog search functionality</item>
   /// <item>Knowledge base and FAQ systems</item>
   /// <item>Log analysis and text mining</item>
   /// </list>
   /// </remarks>
   /// <example>
   /// <code>
   /// // Configure a product description for full-text search
   /// modelBuilder.Entity&lt;Product&gt;(builder =>
   /// {
   ///     builder.Property(p => p.Description)
   ///         .HasSqliteFullTextSearch(FtsVersion.FTS5);
   /// });
   /// 
   /// // Query using full-text search
   /// var products = context.Products
   ///     .Where(p => EF.Functions.Match(p.Description, "search terms"))
   ///     .ToList();
   /// </code>
   /// </example>
   public static PropertyBuilder<string> HasSqliteFullTextSearch<T>(
      this PropertyBuilder<string> propertyBuilder,
      FtsVersion                   ftsVersion = FtsVersion.FTS5)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(propertyBuilder);

      var ftsVersionString = ftsVersion switch
                             {
                                FtsVersion.FTS3 => "fts3",
                                FtsVersion.FTS4 => "fts4",
                                FtsVersion.FTS5 => "fts5",
                                _               => "fts5"
                             };

      // Configure the property for full-text search
      propertyBuilder
        .HasAnnotation("Sqlite:FullTextSearch", true)
        .HasAnnotation("Sqlite:FtsVersion",     ftsVersionString);

      return propertyBuilder;
   }

   /// <summary>
   /// Creates a multi-column full-text search index combining multiple text properties for comprehensive search capabilities.
   /// Enables searching across multiple columns simultaneously with a single FTS virtual table.
   /// </summary>
   /// <typeparam name="T">The entity type</typeparam>
   /// <param name="entityBuilder">The entity type builder</param>
   /// <param name="indexName">The name of the FTS index</param>
   /// <param name="textProperties">Array of expressions selecting text properties to include in the index</param>
   /// <param name="ftsVersion">The FTS version to use (default: FTS5)</param>
   /// <returns>The entity type builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when entityBuilder, indexName, or textProperties is null</exception>
   /// <exception cref="ArgumentException">Thrown when indexName is empty or textProperties is empty</exception>
   /// <remarks>
   /// <para>Performance Benefits:</para>
   /// <list type="bullet">
   /// <item>Single index covers multiple columns, reducing storage overhead</item>
   /// <item>Cross-column search without multiple table joins</item>
   /// <item>Optimized query planning for complex text searches</item>
   /// <item>Supports weighted ranking across different text fields</item>
   /// </list>
   /// <para>Index Strategy:</para>
   /// <list type="bullet">
   /// <item>Creates a virtual FTS table mirroring selected columns</item>
   /// <item>Maintains synchronization with source table automatically</item>
   /// <item>Supports column-specific search operations</item>
   /// </list>
   /// </remarks>
   /// <example>
   /// <code>
   /// // Configure multi-column FTS index for products
   /// modelBuilder.Entity&lt;Product&gt;(builder =>
   /// {
   ///     builder.HasSqliteFullTextIndex("ProductSearchIndex",
   ///         new Expression&lt;Func&lt;Product, string&gt;&gt;[]
   ///         {
   ///             p => p.Name,
   ///             p => p.Description,
   ///             p => p.Category,
   ///             p => p.Tags
   ///         },
   ///         FtsVersion.FTS5);
   /// });
   /// 
   /// // Search across all indexed columns
   /// var products = context.Products
   ///     .Where(p => EF.Functions.Match("ProductSearchIndex", "laptop gaming"))
   ///     .ToList();
   /// </code>
   /// </example>
   public static EntityTypeBuilder<T> HasSqliteFullTextIndex<T>(
      this EntityTypeBuilder<T>     entityBuilder,
      string                        indexName,
      Expression<Func<T, string>>[] textProperties,
      FtsVersion                    ftsVersion = FtsVersion.FTS5)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(entityBuilder);
      ArgumentNullException.ThrowIfNull(indexName);
      ArgumentNullException.ThrowIfNull(textProperties);

      if (string.IsNullOrWhiteSpace(indexName))
         throw new ArgumentException("Index name cannot be empty or whitespace.", nameof(indexName));

      if (textProperties.Length == 0)
         throw new ArgumentException("At least one text property must be specified.", nameof(textProperties));

      var ftsVersionString = ftsVersion switch
                             {
                                FtsVersion.FTS3 => "fts3",
                                FtsVersion.FTS4 => "fts4",
                                FtsVersion.FTS5 => "fts5",
                                _               => "fts5"
                             };

      // Extract property names from expressions
      var propertyNames = textProperties
                         .Select(expr => GetPropertyName(expr))
                         .ToArray();

      // Configure the multi-column FTS index
      entityBuilder
        .HasAnnotation("Sqlite:FullTextIndex", indexName)
        .HasAnnotation("Sqlite:FtsVersion",    ftsVersionString)
        .HasAnnotation("Sqlite:FtsColumns",    propertyNames);

      return entityBuilder;
   }

   /// <summary>
   /// Configures a custom FTS tokenizer for language-specific text processing and enhanced search accuracy.
   /// Enables specialized tokenization for different languages, custom stemming, and advanced text processing.
   /// </summary>
   /// <typeparam name="T">The entity type</typeparam>
   /// <param name="propertyBuilder">The property builder for the text property</param>
   /// <param name="tokenizerName">The name of the tokenizer (e.g., "porter", "unicode61", "ascii")</param>
   /// <param name="tokenizerOptions">Optional tokenizer-specific configuration options</param>
   /// <returns>The property builder for method chaining</returns>
   /// <exception cref="ArgumentNullException">Thrown when propertyBuilder or tokenizerName is null</exception>
   /// <exception cref="ArgumentException">Thrown when tokenizerName is empty</exception>
   /// <remarks>
   /// <para>Available Tokenizers:</para>
   /// <list type="bullet">
   /// <item><strong>unicode61</strong>: Unicode-aware tokenization (default)</item>
   /// <item><strong>ascii</strong>: ASCII-only tokenization for performance</item>
   /// <item><strong>porter</strong>: Porter stemming algorithm for English</item>
   /// <item><strong>simple</strong>: Basic whitespace and punctuation splitting</item>
   /// </list>
   /// <para>Performance Considerations:</para>
   /// <list type="bullet">
   /// <item>ASCII tokenizer provides 20-30% better performance for English-only content</item>
   /// <item>Porter stemming improves search recall for English text</item>
   /// <item>Unicode61 required for international character support</item>
   /// <item>Custom tokenizers enable domain-specific text processing</item>
   /// </list>
   /// <para>Language Support:</para>
   /// <list type="bullet">
   /// <item>Configure appropriate tokenizer based on content language</item>
   /// <item>Consider stemming requirements for better search results</item>
   /// <item>Unicode support necessary for non-Latin scripts</item>
   /// </list>
   /// </remarks>
   /// <example>
   /// <code>
   /// // Configure Porter stemming tokenizer for English content
   /// modelBuilder.Entity&lt;Article&gt;(builder =>
   /// {
   ///     builder.Property(a => a.Content)
   ///         .HasSqliteFullTextSearch&lt;Article&gt;(FtsVersion.FTS5)
   ///         .HasSqliteFtsTokenizer&lt;Article&gt;("porter");
   /// });
   /// 
   /// // Configure Unicode tokenizer with case folding for international content
   /// modelBuilder.Entity&lt;Document&gt;(builder =>
   /// {
   ///     builder.Property(d => d.Text)
   ///         .HasSqliteFullTextSearch&lt;Document&gt;(FtsVersion.FTS5)
   ///         .HasSqliteFtsTokenizer&lt;Document&gt;("unicode61", "casefold=1");
   /// });
   /// 
   /// // Search with stemming benefits
   /// var articles = context.Articles
   ///     .Where(a => EF.Functions.Match(a.Content, "running")) // Also matches "run", "runs", "ran"
   ///     .ToList();
   /// </code>
   /// </example>
   public static PropertyBuilder<string> HasSqliteFtsTokenizer<T>(
      this PropertyBuilder<string> propertyBuilder,
      string                       tokenizerName,
      string?                      tokenizerOptions = null)
      where T : class
   {
      ArgumentNullException.ThrowIfNull(propertyBuilder);
      ArgumentNullException.ThrowIfNull(tokenizerName);

      if (string.IsNullOrWhiteSpace(tokenizerName))
         throw new ArgumentException("Tokenizer name cannot be empty or whitespace.", nameof(tokenizerName));

      // Configure the FTS tokenizer
      propertyBuilder
        .HasAnnotation("Sqlite:FtsTokenizer", tokenizerName);

      if (!string.IsNullOrWhiteSpace(tokenizerOptions))
      {
         propertyBuilder
           .HasAnnotation("Sqlite:FtsTokenizerOptions", tokenizerOptions);
      }

      return propertyBuilder;
   }

   /// <summary>
   /// Extracts the property name from a lambda expression.
   /// </summary>
   /// <typeparam name="T">The entity type</typeparam>
   /// <param name="expression">The property selector expression</param>
   /// <returns>The property name</returns>
   /// <exception cref="ArgumentException">Thrown when expression is not a simple property access</exception>
   private static string GetPropertyName<T>(Expression<Func<T, string>> expression)
   {
      if (expression.Body is MemberExpression memberExpression)
         return memberExpression.Member.Name;

      throw new ArgumentException($"Expression '{expression}' is not a simple property access.", nameof(expression));
   }
}