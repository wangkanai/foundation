using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.IO.Compression;

namespace Wangkanai.EntityFramework.Sqlite;

/// <summary>
/// Compression levels for BLOB optimization in SQLite.
/// Provides different trade-offs between compression ratio and performance.
/// </summary>
public enum CompressionLevel
{
    /// <summary>
    /// No compression - fastest performance, largest storage
    /// </summary>
    None = 0,

    /// <summary>
    /// Fastest compression with minimal CPU overhead
    /// </summary>
    Fastest = 1,

    /// <summary>
    /// Balanced compression ratio and performance (default)
    /// </summary>
    Optimal = 2,

    /// <summary>
    /// Smallest size with higher CPU overhead
    /// </summary>
    SmallestSize = 3
}

/// <summary>
/// Extension methods for configuring data type optimizations in SQLite Entity Framework Core entities.
/// Provides type affinity configurations and storage optimizations specifically designed for SQLite.
/// </summary>
public static class DataTypeConfigurationExtensions
{
    /// <summary>
    /// Forces INTEGER affinity for optimal numeric performance in SQLite.
    /// Ensures efficient storage and indexing for integer-based properties.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be numeric)</typeparam>
    /// <param name="propertyBuilder">The property builder instance</param>
    /// <param name="enableAutoIncrement">Whether to enable SQLite AUTOINCREMENT for primary keys</param>
    /// <returns>The property builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
    /// <remarks>
    /// INTEGER affinity in SQLite provides:
    /// - Optimal storage efficiency for whole numbers
    /// - Fast arithmetic operations
    /// - Efficient indexing and sorting
    /// - Automatic type coercion from compatible types
    /// 
    /// Supported types: byte, short, int, long, and their nullable variants
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Property(e => e.Id)
    ///     .HasSqliteIntegerAffinity(enableAutoIncrement: true);
    ///     
    /// builder.Property(e => e.Quantity)
    ///     .HasSqliteIntegerAffinity();
    /// </code>
    /// </example>
    public static PropertyBuilder<TProperty> HasSqliteIntegerAffinity<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        bool enableAutoIncrement = false)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!IsIntegerType(underlyingType))
        {
            throw new ArgumentException($"INTEGER affinity can only be applied to integer types. Type {propertyType.Name} is not supported.", nameof(propertyBuilder));
        }

        var builder = propertyBuilder
            .HasColumnType("INTEGER")
            .HasAnnotation("Sqlite:TypeAffinity", "INTEGER")
            .HasAnnotation("Sqlite:OptimizedInteger", true);

        if (enableAutoIncrement)
        {
            builder.HasAnnotation("Sqlite:Autoincrement", true);
        }

        return builder;
    }

    /// <summary>
    /// Configures REAL affinity for decimal precision in SQLite.
    /// Optimizes storage and operations for floating-point numbers.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be floating-point)</typeparam>
    /// <param name="propertyBuilder">The property builder instance</param>
    /// <param name="precision">The total number of significant digits</param>
    /// <param name="scale">The number of digits after the decimal point</param>
    /// <returns>The property builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when precision or scale are invalid</exception>
    /// <remarks>
    /// REAL affinity in SQLite provides:
    /// - IEEE 754 floating-point storage
    /// - Optimal performance for mathematical operations
    /// - Efficient sorting and comparison
    /// - Automatic type coercion from numeric types
    /// 
    /// Supported types: float, double, decimal, and their nullable variants
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Property(e => e.Price)
    ///     .HasSqliteRealAffinity(precision: 10, scale: 2);
    ///     
    /// builder.Property(e => e.Percentage)
    ///     .HasSqliteRealAffinity(precision: 5, scale: 4);
    /// </code>
    /// </example>
    public static PropertyBuilder<TProperty> HasSqliteRealAffinity<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        int precision = 18,
        int scale = 2)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        if (precision < 1 || precision > 38)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be between 1 and 38");
        }

        if (scale < 0 || scale > precision)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be between 0 and precision");
        }

        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!IsRealType(underlyingType))
        {
            throw new ArgumentException($"REAL affinity can only be applied to floating-point types. Type {propertyType.Name} is not supported.", nameof(propertyBuilder));
        }

        return propertyBuilder
            .HasColumnType("REAL")
            .HasPrecision(precision, scale)
            .HasAnnotation("Sqlite:TypeAffinity", "REAL")
            .HasAnnotation("Sqlite:OptimizedReal", true)
            .HasAnnotation("Sqlite:Precision", precision)
            .HasAnnotation("Sqlite:Scale", scale);
    }

    /// <summary>
    /// Optimizes BLOB storage with compression and chunking for large binary data.
    /// Provides efficient storage and retrieval of binary content in SQLite.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be byte array or binary)</typeparam>
    /// <param name="propertyBuilder">The property builder instance</param>
    /// <param name="compressionLevel">The compression level to apply</param>
    /// <param name="chunkSize">The size in bytes for chunking large BLOBs (0 = no chunking)</param>
    /// <param name="enableDeduplication">Whether to enable content-based deduplication</param>
    /// <returns>The property builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when chunkSize is negative</exception>
    /// <remarks>
    /// BLOB optimization in SQLite provides:
    /// - Configurable compression to reduce storage size
    /// - Chunking for large binary data (>1MB recommended)
    /// - Content deduplication to avoid storing identical content
    /// - Optimized I/O operations for binary data
    /// 
    /// Compression trade-offs:
    /// - None: No CPU overhead, maximum storage
    /// - Fastest: ~10% CPU, ~30% size reduction
    /// - Optimal: ~20% CPU, ~50% size reduction (recommended)
    /// - SmallestSize: ~40% CPU, ~70% size reduction
    /// </remarks>
    /// <example>
    /// <code>
    /// // Image storage with optimal compression
    /// builder.Property(e => e.ImageData)
    ///     .HasSqliteBlobOptimization(
    ///         CompressionLevel.Optimal, 
    ///         chunkSize: 1024 * 1024); // 1MB chunks
    ///         
    /// // Document storage with deduplication
    /// builder.Property(e => e.DocumentContent)
    ///     .HasSqliteBlobOptimization(
    ///         CompressionLevel.SmallestSize,
    ///         enableDeduplication: true);
    /// </code>
    /// </example>
    public static PropertyBuilder<TProperty> HasSqliteBlobOptimization<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        CompressionLevel compressionLevel = CompressionLevel.Optimal,
        int chunkSize = 0,
        bool enableDeduplication = false)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        if (chunkSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size cannot be negative");
        }

        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!IsBinaryType(underlyingType))
        {
            throw new ArgumentException($"BLOB optimization can only be applied to binary types. Type {propertyType.Name} is not supported.", nameof(propertyBuilder));
        }

        var builder = propertyBuilder
            .HasColumnType("BLOB")
            .HasAnnotation("Sqlite:TypeAffinity", "BLOB")
            .HasAnnotation("Sqlite:OptimizedBlob", true)
            .HasAnnotation("Sqlite:CompressionLevel", compressionLevel);

        if (chunkSize > 0)
        {
            builder.HasAnnotation("Sqlite:ChunkSize", chunkSize);
        }

        if (enableDeduplication)
        {
            builder.HasAnnotation("Sqlite:Deduplication", true);
        }

        return builder;
    }

    /// <summary>
    /// Forces TEXT affinity for string optimization in SQLite.
    /// Configures collation and encoding for optimal text storage and searching.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be string)</typeparam>
    /// <param name="propertyBuilder">The property builder instance</param>
    /// <param name="collation">The SQLite collation to use (BINARY, NOCASE, RTRIM)</param>
    /// <param name="enableFullTextSearch">Whether to enable FTS5 full-text search</param>
    /// <param name="maxLength">Maximum length constraint (0 = unlimited)</param>
    /// <returns>The property builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
    /// <exception cref="ArgumentException">Thrown when collation is invalid</exception>
    /// <remarks>
    /// TEXT affinity optimization provides:
    /// - Optimized string storage and comparison
    /// - Configurable collation for sorting and searching
    /// - Optional full-text search integration
    /// - Length constraints for validation
    /// 
    /// Collation options:
    /// - BINARY: Byte-by-byte comparison (fastest, case-sensitive)
    /// - NOCASE: Case-insensitive comparison (Unicode-aware)
    /// - RTRIM: Right-trim whitespace before comparison
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Property(e => e.Name)
    ///     .HasSqliteTextAffinity("NOCASE", maxLength: 100);
    ///     
    /// builder.Property(e => e.SearchableContent)
    ///     .HasSqliteTextAffinity("NOCASE", enableFullTextSearch: true);
    /// </code>
    /// </example>
    public static PropertyBuilder<TProperty> HasSqliteTextAffinity<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        string collation = "BINARY",
        bool enableFullTextSearch = false,
        int maxLength = 0)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);
        ArgumentException.ThrowIfNullOrWhiteSpace(collation);

        var validCollations = new[] { "BINARY", "NOCASE", "RTRIM" };
        if (!validCollations.Contains(collation.ToUpperInvariant()))
        {
            throw new ArgumentException($"Invalid collation: {collation}. Valid options are: {string.Join(", ", validCollations)}", nameof(collation));
        }

        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (underlyingType != typeof(string))
        {
            throw new ArgumentException($"TEXT affinity can only be applied to string properties. Type {propertyType.Name} is not supported.", nameof(propertyBuilder));
        }

        var builder = propertyBuilder
            .HasColumnType("TEXT")
            .UseCollation(collation.ToUpperInvariant())
            .HasAnnotation("Sqlite:TypeAffinity", "TEXT")
            .HasAnnotation("Sqlite:OptimizedText", true)
            .HasAnnotation("Sqlite:Collation", collation.ToUpperInvariant());

        if (maxLength > 0)
        {
            builder.HasMaxLength(maxLength);
        }

        if (enableFullTextSearch)
        {
            builder.HasAnnotation("Sqlite:FullTextSearch", true);
        }

        return builder;
    }

    /// <summary>
    /// Configures NUMERIC affinity for precise decimal calculations.
    /// Optimizes storage for exact numeric values with fixed precision.
    /// </summary>
    /// <typeparam name="TProperty">The property type (must be decimal or numeric)</typeparam>
    /// <param name="propertyBuilder">The property builder instance</param>
    /// <param name="precision">The total number of digits</param>
    /// <param name="scale">The number of digits after decimal point</param>
    /// <param name="enableCurrencyMode">Whether to optimize for currency calculations</param>
    /// <returns>The property builder for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when propertyBuilder is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when precision or scale are invalid</exception>
    /// <remarks>
    /// NUMERIC affinity provides exact decimal arithmetic without floating-point precision issues.
    /// Ideal for financial calculations, measurements, and other precision-critical applications.
    /// 
    /// Currency mode optimizations:
    /// - Uses fixed-point arithmetic
    /// - Optimized rounding behavior
    /// - Enhanced precision for monetary calculations
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Property(e => e.Balance)
    ///     .HasSqliteNumericAffinity(precision: 19, scale: 4, enableCurrencyMode: true);
    ///     
    /// builder.Property(e => e.Measurement)
    ///     .HasSqliteNumericAffinity(precision: 10, scale: 6);
    /// </code>
    /// </example>
    public static PropertyBuilder<TProperty> HasSqliteNumericAffinity<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        int precision = 18,
        int scale = 2,
        bool enableCurrencyMode = false)
    {
        ArgumentNullException.ThrowIfNull(propertyBuilder);

        if (precision < 1 || precision > 38)
        {
            throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be between 1 and 38");
        }

        if (scale < 0 || scale > precision)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be between 0 and precision");
        }

        var propertyType = typeof(TProperty);
        var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (!IsNumericType(underlyingType))
        {
            throw new ArgumentException($"NUMERIC affinity can only be applied to decimal types. Type {propertyType.Name} is not supported.", nameof(propertyBuilder));
        }

        var builder = propertyBuilder
            .HasColumnType("NUMERIC")
            .HasPrecision(precision, scale)
            .HasAnnotation("Sqlite:TypeAffinity", "NUMERIC")
            .HasAnnotation("Sqlite:OptimizedNumeric", true)
            .HasAnnotation("Sqlite:Precision", precision)
            .HasAnnotation("Sqlite:Scale", scale);

        if (enableCurrencyMode)
        {
            builder.HasAnnotation("Sqlite:CurrencyMode", true);
        }

        return builder;
    }

    #region Private Helper Methods

    private static bool IsIntegerType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong);

    private static bool IsRealType(Type type) =>
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    private static bool IsBinaryType(Type type) =>
        type == typeof(byte[]) || type == typeof(Memory<byte>) || type == typeof(ReadOnlyMemory<byte>);

    private static bool IsNumericType(Type type) =>
        type == typeof(decimal);

    #endregion
}