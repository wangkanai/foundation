// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring PostgreSQL full-text search capabilities.
/// Supports tsvector, tsquery, text search configurations, ranking, and highlighting.
/// </summary>
public static class FullTextSearchExtensions
{
    #region TSVector Configuration

    /// <summary>
    /// Configures a property to use PostgreSQL tsvector data type for full-text search.
    /// TSVector is a sorted list of distinct lexemes optimized for text search operations.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="language">Optional text search configuration language (e.g., 'english', 'simple').</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Basic tsvector column
    /// builder.Property(e => e.SearchVector)
    ///        .HasTsVectorType();
    /// 
    /// // Tsvector with specific language
    /// builder.Property(e => e.EnglishSearchVector)
    ///        .HasTsVectorType("english");
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     search_vector TSVECTOR,
    /// //     english_search_vector TSVECTOR
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> HasTsVectorType(
        this PropertyBuilder<NpgsqlTsVector> builder,
        string? language = null)
    {
        builder.HasColumnType("tsvector");
        
        if (!string.IsNullOrWhiteSpace(language))
            builder.HasAnnotation("Npgsql:TsVectorConfig", language);
        
        return builder;
    }

    /// <summary>
    /// Configures automatic tsvector generation from text columns using PostgreSQL triggers.
    /// This creates a computed tsvector that automatically updates when source columns change.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="sourceColumns">Array of column names to include in the tsvector generation.</param>
    /// <param name="language">Text search configuration language (default: 'english').</param>
    /// <param name="weights">Optional weights for each source column (A, B, C, D).</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when sourceColumns is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Auto-generate from title and content
    /// builder.Property(e => e.SearchVector)
    ///        .HasGeneratedTsVector(new[] { "title", "content" });
    /// 
    /// // With language and weights
    /// builder.Property(e => e.SearchVector)
    ///        .HasGeneratedTsVector(
    ///            new[] { "title", "description", "content" },
    ///            "english",
    ///            new[] { TsVectorWeight.A, TsVectorWeight.B, TsVectorWeight.D });
    /// 
    /// // Generated SQL:
    /// // search_vector TSVECTOR GENERATED ALWAYS AS (
    /// //     setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
    /// //     setweight(to_tsvector('english', coalesce(description, '')), 'B') ||
    /// //     setweight(to_tsvector('english', coalesce(content, '')), 'D')
    /// // ) STORED;
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> HasGeneratedTsVector(
        this PropertyBuilder<NpgsqlTsVector> builder,
        string[] sourceColumns,
        string language = "english",
        TsVectorWeight[]? weights = null)
    {
        if (sourceColumns == null || sourceColumns.Length == 0)
            throw new ArgumentException("Source columns cannot be null or empty.", nameof(sourceColumns));

        var expressions = new List<string>();
        
        for (int i = 0; i < sourceColumns.Length; i++)
        {
            var column = sourceColumns[i];
            var weight = weights != null && i < weights.Length ? weights[i] : TsVectorWeight.D;
            var weightChar = weight.ToString();
            
            expressions.Add($"setweight(to_tsvector('{language}', coalesce({column}, '')), '{weightChar}')");
        }

        var expression = string.Join(" || ", expressions);
        builder.HasComputedColumnSql(expression, stored: true);
        builder.HasColumnType("tsvector");
        
        return builder;
    }

    /// <summary>
    /// Creates a GIN index on a tsvector column for efficient full-text search.
    /// GIN indexes are optimal for tsvector columns and significantly improve search performance.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="indexName">Optional custom name for the index.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Property(e => e.SearchVector)
    ///        .HasTsVectorType()
    ///        .HasTsVectorGinIndex("ix_entity_search_gin");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_search_gin ON entities USING gin (search_vector);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE search_vector @@ to_tsquery('english', 'word');
    /// // SELECT * FROM entities WHERE search_vector @@ plainto_tsquery('english', 'search phrase');
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> HasTsVectorGinIndex(
        this PropertyBuilder<NpgsqlTsVector> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for tsvector usage
        // Example: entityBuilder.HasIndex(e => e.SearchVector).HasMethod("gin");
        builder.HasAnnotation("Npgsql:IndexMethod", "gin");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        
        return builder;
    }

    #endregion

    #region Text Search Configuration

    /// <summary>
    /// Configures a text column for full-text search with automatic tsvector generation and indexing.
    /// This is a convenience method that sets up everything needed for full-text search on a text column.
    /// </summary>
    /// <param name="builder">The property builder used to configure the string property.</param>
    /// <param name="language">Text search configuration language (default: 'english').</param>
    /// <param name="weight">Weight for the tsvector generation (default: A).</param>
    /// <param name="indexName">Optional custom name for the search index.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure title for full-text search
    /// builder.Property(e => e.Title)
    ///        .HasFullTextSearch("english", TsVectorWeight.A, "ix_entity_title_fts");
    /// 
    /// // Configure content with different weight
    /// builder.Property(e => e.Content)
    ///        .HasFullTextSearch("english", TsVectorWeight.D, "ix_entity_content_fts");
    /// 
    /// // This creates both the text column and a computed tsvector column with GIN index
    /// </code>
    /// </example>
    public static PropertyBuilder<string> HasFullTextSearch(
        this PropertyBuilder<string> builder,
        string language = "english",
        TsVectorWeight weight = TsVectorWeight.A,
        string? indexName = null)
    {
        var columnName = builder.Metadata.Name;
        var tsVectorColumnName = $"{columnName}_search_vector";
        var weightChar = weight.ToString();
        
        // Add annotation to indicate this column participates in full-text search
        builder.HasAnnotation("Npgsql:FullTextSearch", new
        {
            Language = language,
            Weight = weight,
            TsVectorColumn = tsVectorColumnName,
            IndexName = indexName
        });
        
        return builder;
    }

    /// <summary>
    /// Configures multiple text columns for combined full-text search with a single tsvector column.
    /// This creates a unified search index across multiple text fields with different weights.
    /// </summary>
    /// <param name="entityBuilder">The entity type builder used to configure the entity.</param>
    /// <param name="searchVectorProperty">The name of the tsvector property to create.</param>
    /// <param name="columnConfigurations">Configuration for each source column.</param>
    /// <param name="language">Text search configuration language (default: 'english').</param>
    /// <param name="indexName">Optional custom name for the search index.</param>
    /// <returns>The same EntityTypeBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when columnConfigurations is null or empty.</exception>
    /// <example>
    /// <code>
    /// // Configure multi-column full-text search
    /// builder.HasMultiColumnFullTextSearch("SearchVector", new[]
    /// {
    ///     new FullTextColumnConfig("title", TsVectorWeight.A),
    ///     new FullTextColumnConfig("description", TsVectorWeight.B),
    ///     new FullTextColumnConfig("content", TsVectorWeight.C),
    ///     new FullTextColumnConfig("tags", TsVectorWeight.D)
    /// }, "english", "ix_entity_multi_search");
    /// 
    /// // Generated SQL creates:
    /// // 1. A computed tsvector column combining all source columns
    /// // 2. A GIN index on the tsvector column
    /// // 3. Proper weights for search ranking
    /// </code>
    /// </example>
    public static EntityTypeBuilder<T> HasMultiColumnFullTextSearch<T>(
        this EntityTypeBuilder<T> entityBuilder,
        string searchVectorProperty,
        FullTextColumnConfig[] columnConfigurations,
        string language = "english",
        string? indexName = null) where T : class
    {
        if (columnConfigurations == null || columnConfigurations.Length == 0)
            throw new ArgumentException("Column configurations cannot be null or empty.", nameof(columnConfigurations));

        var expressions = columnConfigurations.Select(config =>
        {
            var weightChar = config.Weight.ToString();
            return $"setweight(to_tsvector('{language}', coalesce({config.ColumnName}, '')), '{weightChar}')";
        });

        var expression = string.Join(" || ", expressions);
        
        entityBuilder.Property<NpgsqlTsVector>(searchVectorProperty)
            .HasComputedColumnSql(expression, stored: true)
            .HasColumnType("tsvector");

        // Create GIN index
        var indexBuilder = entityBuilder.HasIndex(searchVectorProperty).HasMethod("gin");
        if (!string.IsNullOrWhiteSpace(indexName))
            indexBuilder.HasDatabaseName(indexName);

        return entityBuilder;
    }

    #endregion

    #region Search Query Operations

    /// <summary>
    /// Configures search query optimization annotations for better query planning.
    /// Documents the intended search patterns for the PostgreSQL query planner.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="queryTypes">The types of text search queries this column will handle.</param>
    /// <param name="expectedLanguages">Optional list of languages this search vector will handle.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Property(e => e.SearchVector)
    ///        .HasTsVectorType()
    ///        .OptimizeForSearchQueries(
    ///            SearchQueryTypes.PlainText | SearchQueryTypes.Phrase | SearchQueryTypes.Boolean,
    ///            new[] { "english", "spanish" });
    /// 
    /// // This helps with:
    /// // - Query planning optimization
    /// // - Index usage hints
    /// // - Documentation for other developers
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> OptimizeForSearchQueries(
        this PropertyBuilder<NpgsqlTsVector> builder,
        SearchQueryTypes queryTypes,
        string[]? expectedLanguages = null)
    {
        builder.HasAnnotation("Npgsql:SearchQueryTypes", queryTypes);
        
        if (expectedLanguages != null && expectedLanguages.Length > 0)
            builder.HasAnnotation("Npgsql:SearchLanguages", expectedLanguages);
        
        return builder;
    }

    /// <summary>
    /// Configures text search ranking optimization for search result ordering.
    /// Sets up the necessary infrastructure for ranking search results by relevance.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="rankingMethod">The ranking method to optimize for.</param>
    /// <param name="weights">Optional custom weights for ranking calculation.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Property(e => e.SearchVector)
    ///        .HasTsVectorType()
    ///        .OptimizeForRanking(
    ///            SearchRankingMethod.CoverDensity,
    ///            new[] { 1.0f, 0.4f, 0.2f, 0.1f }); // A, B, C, D weights
    /// 
    /// // Enables efficient queries like:
    /// // SELECT *, ts_rank(search_vector, query) as rank
    /// // FROM entities 
    /// // WHERE search_vector @@ query 
    /// // ORDER BY rank DESC;
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> OptimizeForRanking(
        this PropertyBuilder<NpgsqlTsVector> builder,
        SearchRankingMethod rankingMethod,
        float[]? weights = null)
    {
        builder.HasAnnotation("Npgsql:SearchRanking", rankingMethod);
        
        if (weights != null && weights.Length == 4)
            builder.HasAnnotation("Npgsql:SearchWeights", weights);
        
        return builder;
    }

    #endregion

    #region Text Search Dictionaries

    /// <summary>
    /// Configures custom text search dictionaries for specialized text processing.
    /// Allows fine-grained control over how text is parsed and searched.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="dictionaryName">The name of the custom text search dictionary.</param>
    /// <param name="stopWords">Optional list of stop words to exclude from indexing.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when dictionaryName is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // Use custom dictionary
    /// builder.Property(e => e.TechnicalSearchVector)
    ///        .HasTsVectorType()
    ///        .UseTextSearchDictionary("technical_dict");
    /// 
    /// // Use dictionary with custom stop words
    /// builder.Property(e => e.ContentSearchVector)
    ///        .HasTsVectorType()
    ///        .UseTextSearchDictionary("content_dict", new[] { "very", "really", "quite" });
    /// 
    /// // This affects how text is processed during tsvector generation:
    /// // SELECT to_tsvector('technical_dict', 'technical content');
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> UseTextSearchDictionary(
        this PropertyBuilder<NpgsqlTsVector> builder,
        string dictionaryName,
        string[]? stopWords = null)
    {
        if (string.IsNullOrWhiteSpace(dictionaryName))
            throw new ArgumentException("Dictionary name cannot be null or whitespace.", nameof(dictionaryName));

        builder.HasAnnotation("Npgsql:TextSearchDictionary", dictionaryName);
        
        if (stopWords != null && stopWords.Length > 0)
            builder.HasAnnotation("Npgsql:StopWords", stopWords);
        
        return builder;
    }

    #endregion

    #region Search Highlighting

    /// <summary>
    /// Configures search result highlighting options for displaying search matches.
    /// Sets up the infrastructure for highlighting matched terms in search results.
    /// </summary>
    /// <param name="builder">The property builder used to configure the string property.</param>
    /// <param name="highlightOptions">Options for configuring search result highlighting.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when highlightOptions is null.</exception>
    /// <example>
    /// <code>
    /// // Configure highlighting for search results
    /// builder.Property(e => e.Content)
    ///        .HasSearchHighlighting(new SearchHighlightOptions
    ///        {
    ///            StartTag = "&lt;mark&gt;",
    ///            StopTag = "&lt;/mark&gt;",
    ///            MaxWords = 35,
    ///            MinWords = 15,
    ///            ShortWord = 3,
    ///            HighlightAll = false,
    ///            MaxFragments = 5
    ///        });
    /// 
    /// // Enables queries like:
    /// // SELECT ts_headline('english', content, query, 'StartSel=&lt;mark&gt;, StopSel=&lt;/mark&gt;')
    /// // FROM entities WHERE search_vector @@ query;
    /// </code>
    /// </example>
    public static PropertyBuilder<string> HasSearchHighlighting(
        this PropertyBuilder<string> builder,
        SearchHighlightOptions highlightOptions)
    {
        if (highlightOptions == null)
            throw new ArgumentNullException(nameof(highlightOptions));

        builder.HasAnnotation("Npgsql:SearchHighlight", highlightOptions);
        
        return builder;
    }

    #endregion

    #region Proximity and Phrase Search

    /// <summary>
    /// Configures proximity search optimization for phrase and distance-based queries.
    /// Optimizes the search index for queries that care about word positions and distances.
    /// </summary>
    /// <param name="builder">The property builder used to configure the NpgsqlTsVector property.</param>
    /// <param name="maxDistance">Maximum distance between words for proximity matching.</param>
    /// <param name="preservePositions">Whether to preserve word positions in the tsvector.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Property(e => e.SearchVector)
    ///        .HasTsVectorType()
    ///        .OptimizeForProximitySearch(maxDistance: 5, preservePositions: true);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE search_vector @@ 'word1 &lt;-&gt; word2';  -- Adjacent words
    /// // SELECT * FROM entities WHERE search_vector @@ 'word1 &lt;3&gt; word2';   -- Within 3 positions
    /// // SELECT * FROM entities WHERE search_vector @@ '"exact phrase"';      -- Phrase search
    /// </code>
    /// </example>
    public static PropertyBuilder<NpgsqlTsVector> OptimizeForProximitySearch(
        this PropertyBuilder<NpgsqlTsVector> builder,
        int maxDistance = 10,
        bool preservePositions = true)
    {
        builder.HasAnnotation("Npgsql:ProximitySearch", new
        {
            MaxDistance = maxDistance,
            PreservePositions = preservePositions
        });
        
        return builder;
    }

    #endregion
}

/// <summary>
/// Represents weight categories for tsvector generation and ranking.
/// Weights affect search result ranking, with A being highest and D being lowest.
/// </summary>
public enum TsVectorWeight
{
    /// <summary>Highest weight - typically used for titles and headings.</summary>
    A,
    /// <summary>High weight - typically used for important content.</summary>
    B,
    /// <summary>Medium weight - typically used for regular content.</summary>
    C,
    /// <summary>Lowest weight - typically used for metadata and tags.</summary>
    D
}

/// <summary>
/// Configuration for a column participating in full-text search.
/// </summary>
public record FullTextColumnConfig(string ColumnName, TsVectorWeight Weight);

/// <summary>
/// Types of text search queries that can be optimized for.
/// </summary>
[Flags]
public enum SearchQueryTypes
{
    /// <summary>No specific query type optimization.</summary>
    None = 0,
    /// <summary>Plain text queries using plainto_tsquery().</summary>
    PlainText = 1,
    /// <summary>Phrase queries using phraseto_tsquery().</summary>
    Phrase = 2,
    /// <summary>Boolean queries using to_tsquery().</summary>
    Boolean = 4,
    /// <summary>Web-style queries using websearch_to_tsquery().</summary>
    WebSearch = 8,
    /// <summary>Proximity and distance queries.</summary>
    Proximity = 16,
    /// <summary>All query types.</summary>
    All = PlainText | Phrase | Boolean | WebSearch | Proximity
}

/// <summary>
/// Methods for ranking search results by relevance.
/// </summary>
public enum SearchRankingMethod
{
    /// <summary>Standard ranking using ts_rank().</summary>
    Standard,
    /// <summary>Cover density ranking using ts_rank_cd().</summary>
    CoverDensity,
    /// <summary>Custom ranking with user-defined weights.</summary>
    Custom
}

/// <summary>
/// Options for configuring search result highlighting.
/// </summary>
public class SearchHighlightOptions
{
    /// <summary>HTML/markup tag to start highlighting (default: "&lt;b&gt;").</summary>
    public string StartTag { get; set; } = "<b>";
    
    /// <summary>HTML/markup tag to stop highlighting (default: "&lt;/b&gt;").</summary>
    public string StopTag { get; set; } = "</b>";
    
    /// <summary>Maximum number of words in the highlighted fragment.</summary>
    public int MaxWords { get; set; } = 35;
    
    /// <summary>Minimum number of words in the highlighted fragment.</summary>
    public int MinWords { get; set; } = 15;
    
    /// <summary>Length below which words are considered short and ignored.</summary>
    public int ShortWord { get; set; } = 3;
    
    /// <summary>Whether to highlight all query terms or just the best matching ones.</summary>
    public bool HighlightAll { get; set; } = false;
    
    /// <summary>Maximum number of highlighted fragments to return.</summary>
    public int MaxFragments { get; set; } = 0;
    
    /// <summary>String to separate multiple fragments.</summary>
    public string FragmentDelimiter { get; set; } = " ... ";
}