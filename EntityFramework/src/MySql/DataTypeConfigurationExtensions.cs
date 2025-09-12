using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Defines MySQL-specific spatial data types for geographic data storage and operations.
/// </summary>
public enum MySqlSpatialType
{
    /// <summary>
    /// Single point in space with X,Y coordinates
    /// </summary>
    Point,
    
    /// <summary>
    /// Sequence of connected points forming a line
    /// </summary>
    LineString,
    
    /// <summary>
    /// Closed area defined by boundary lines
    /// </summary>
    Polygon,
    
    /// <summary>
    /// Collection of multiple points
    /// </summary>
    MultiPoint,
    
    /// <summary>
    /// Collection of multiple line strings
    /// </summary>
    MultiLineString,
    
    /// <summary>
    /// Collection of multiple polygons
    /// </summary>
    MultiPolygon,
    
    /// <summary>
    /// Collection of mixed geometry types
    /// </summary>
    GeometryCollection
}

/// <summary>
/// Defines MySQL-specific numeric data types with precision and range characteristics.
/// </summary>
public enum MySqlNumericType
{
    /// <summary>
    /// 1 byte integer (-128 to 127, or 0 to 255 if unsigned)
    /// </summary>
    TinyInt,
    
    /// <summary>
    /// 2 byte integer (-32,768 to 32,767, or 0 to 65,535 if unsigned)
    /// </summary>
    SmallInt,
    
    /// <summary>
    /// 3 byte integer (-8,388,608 to 8,388,607, or 0 to 16,777,215 if unsigned)
    /// </summary>
    MediumInt,
    
    /// <summary>
    /// 4 byte integer (-2,147,483,648 to 2,147,483,647, or 0 to 4,294,967,295 if unsigned)
    /// </summary>
    Int,
    
    /// <summary>
    /// 8 byte integer (-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807, or 0 to 18,446,744,073,709,551,615 if unsigned)
    /// </summary>
    BigInt,
    
    /// <summary>
    /// Fixed-point decimal number with configurable precision and scale
    /// </summary>
    Decimal,
    
    /// <summary>
    /// Single-precision floating-point number (4 bytes)
    /// </summary>
    Float,
    
    /// <summary>
    /// Double-precision floating-point number (8 bytes)
    /// </summary>
    Double
}

/// <summary>
/// Defines MySQL binary data storage types optimized for different data sizes and access patterns.
/// </summary>
public enum MySqlBinaryType
{
    /// <summary>
    /// Fixed-length binary data up to 255 bytes
    /// </summary>
    Binary,
    
    /// <summary>
    /// Variable-length binary data up to 65,535 bytes
    /// </summary>
    VarBinary,
    
    /// <summary>
    /// Binary large object up to 255 bytes
    /// </summary>
    TinyBlob,
    
    /// <summary>
    /// Binary large object up to 65,535 bytes (64 KB)
    /// </summary>
    Blob,
    
    /// <summary>
    /// Binary large object up to 16,777,215 bytes (16 MB)
    /// </summary>
    MediumBlob,
    
    /// <summary>
    /// Binary large object up to 4,294,967,295 bytes (4 GB)
    /// </summary>
    LongBlob
}

/// <summary>
/// Extension methods for configuring MySQL-specific data types in Entity Framework Core.
/// Provides comprehensive support for MySQL native data types including ENUM, SET, spatial, and binary types.
/// </summary>
public static class DataTypeConfigurationExtensions
{
    /// <summary>
    /// Configures the property to use MySQL ENUM type for storing constrained string values efficiently.
    /// ENUM provides better performance and storage efficiency compared to CHECK constraints.
    /// </summary>
    /// <typeparam name="T">The property type (typically string or enum)</typeparam>
    /// <param name="builder">The property builder</param>
    /// <param name="allowedValues">Array of allowed string values for the ENUM</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Configure order status as MySQL ENUM
    /// modelBuilder.Entity&lt;Order&gt;()
    ///     .Property(o =&gt; o.Status)
    ///     .HasMySqlEnum("pending", "processing", "shipped", "delivered", "cancelled");
    ///     
    /// // Equivalent MySQL SQL: 
    /// // ALTER TABLE Orders MODIFY COLUMN Status ENUM('pending','processing','shipped','delivered','cancelled');
    /// </code>
    /// </example>
    /// <remarks>
    /// Performance benefits:
    /// - 50-70% less storage than VARCHAR for repeated values
    /// - Faster queries due to internal numeric representation
    /// - Automatic value validation at database level
    /// - Index-friendly for efficient sorting and filtering
    /// 
    /// Limitations:
    /// - Maximum 65,535 distinct values per ENUM
    /// - Schema change required to add/remove values
    /// - Case-sensitive value matching
    /// </remarks>
    public static PropertyBuilder<T> HasMySqlEnum<T>(
        this PropertyBuilder<T> builder,
        params string[] allowedValues)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(allowedValues);

        if (allowedValues.Length == 0)
        {
            throw new ArgumentException("At least one allowed value must be specified", nameof(allowedValues));
        }

        if (allowedValues.Length > 65535)
        {
            throw new ArgumentException("MySQL ENUM supports maximum 65,535 values", nameof(allowedValues));
        }

        // Validate values for SQL injection and special characters
        foreach (var value in allowedValues)
        {
            ValidateEnumValue(value);
        }

        var enumDefinition = string.Join(",", allowedValues.Select(v => $"'{v.Replace("'", "''")}'"));
        
        return builder
            .HasColumnType($"enum({enumDefinition})")
            .HasAnnotation("MySql:Enum", allowedValues)
            .HasAnnotation("MySql:EnumDefinition", enumDefinition);
    }

    /// <summary>
    /// Configures the property to use MySQL SET type for storing multiple values from a predefined list.
    /// SET allows efficient storage and querying of multiple selected values as a single column.
    /// </summary>
    /// <param name="builder">The property builder for string properties</param>
    /// <param name="allowedValues">Array of allowed string values for the SET</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Configure user permissions as MySQL SET
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .Property(u =&gt; u.Permissions)
    ///     .HasMySqlSet("read", "write", "delete", "admin", "execute");
    ///     
    /// // Stores values like: "read,write", "admin", "read,write,delete"
    /// // Equivalent MySQL SQL:
    /// // ALTER TABLE Users MODIFY COLUMN Permissions SET('read','write','delete','admin','execute');
    /// </code>
    /// </example>
    /// <remarks>
    /// SET type characteristics:
    /// - Efficient bitmap storage for up to 64 distinct values
    /// - Built-in validation prevents invalid combinations
    /// - Optimized for membership testing with FIND_IN_SET()
    /// - Values stored as comma-separated string internally
    /// 
    /// Query examples:
    /// - FIND_IN_SET('read', Permissions) - check if 'read' is selected
    /// - Permissions LIKE '%admin%' - contains admin permission
    /// - Permissions = 'read,write' - exact match
    /// </remarks>
    public static PropertyBuilder<string> HasMySqlSet(
        this PropertyBuilder<string> builder,
        params string[] allowedValues)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(allowedValues);

        if (allowedValues.Length == 0)
        {
            throw new ArgumentException("At least one allowed value must be specified", nameof(allowedValues));
        }

        if (allowedValues.Length > 64)
        {
            throw new ArgumentException("MySQL SET supports maximum 64 values", nameof(allowedValues));
        }

        // Validate values for SQL injection and special characters
        foreach (var value in allowedValues)
        {
            ValidateSetValue(value);
        }

        var setDefinition = string.Join(",", allowedValues.Select(v => $"'{v.Replace("'", "''")}'"));
        
        return builder
            .HasColumnType($"set({setDefinition})")
            .HasAnnotation("MySql:Set", allowedValues)
            .HasAnnotation("MySql:SetDefinition", setDefinition);
    }

    /// <summary>
    /// Configures the property to use MySQL spatial data types for geographic and geometric data storage.
    /// Enables efficient spatial queries and operations using MySQL's spatial extensions.
    /// </summary>
    /// <typeparam name="T">The property type (typically byte[] or custom spatial type)</typeparam>
    /// <param name="builder">The property builder</param>
    /// <param name="spatialType">The MySQL spatial data type to use</param>
    /// <param name="srid">Spatial Reference System Identifier (default 0 for Cartesian plane)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Configure location as MySQL POINT with WGS84 coordinate system
    /// modelBuilder.Entity&lt;Location&gt;()
    ///     .Property(l =&gt; l.Coordinates)
    ///     .HasMySqlSpatialType(MySqlSpatialType.Point, srid: 4326);
    ///     
    /// // Configure delivery area as POLYGON
    /// modelBuilder.Entity&lt;DeliveryZone&gt;()
    ///     .Property(d =&gt; d.Boundary)
    ///     .HasMySqlSpatialType(MySqlSpatialType.Polygon, srid: 4326);
    ///     
    /// // Equivalent MySQL SQL:
    /// // ALTER TABLE Locations ADD COLUMN Coordinates POINT SRID 4326;
    /// </code>
    /// </example>
    /// <remarks>
    /// Spatial capabilities:
    /// - Geographic distance calculations with ST_Distance()
    /// - Area and perimeter calculations
    /// - Point-in-polygon testing with ST_Contains()
    /// - Spatial indexing for fast geographic queries
    /// - Support for various coordinate systems via SRID
    /// 
    /// Common SRID values:
    /// - 0: Cartesian plane (default)
    /// - 4326: WGS84 (GPS coordinates)
    /// - 3857: Web Mercator (web mapping)
    /// 
    /// Performance: 10-100x faster than coordinate-based queries
    /// </remarks>
    public static PropertyBuilder<T> HasMySqlSpatialType<T>(
        this PropertyBuilder<T> builder,
        MySqlSpatialType spatialType,
        int srid = 0)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (srid < 0)
        {
            throw new ArgumentException("SRID must be non-negative", nameof(srid));
        }

        var columnType = spatialType.ToString().ToUpperInvariant();
        var sridClause = srid > 0 ? $" SRID {srid}" : string.Empty;
        
        return builder
            .HasColumnType($"{columnType}{sridClause}")
            .HasAnnotation("MySql:SpatialType", spatialType)
            .HasAnnotation("MySql:SRID", srid)
            .HasAnnotation("MySql:SpatialIndex", true);
    }

    /// <summary>
    /// Configures the property to use MySQL-specific numeric types with optional unsigned modifier.
    /// Provides fine-grained control over numeric storage and range optimization.
    /// </summary>
    /// <typeparam name="T">The property type (numeric types like int, long, decimal, etc.)</typeparam>
    /// <param name="builder">The property builder</param>
    /// <param name="numericType">The MySQL numeric type to use</param>
    /// <param name="unsigned">Whether to use unsigned variant (doubles positive range)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Configure user age as unsigned TINYINT (0-255)
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .Property(u =&gt; u.Age)
    ///     .HasMySqlNumericType(MySqlNumericType.TinyInt, unsigned: true);
    ///     
    /// // Configure product price as DECIMAL with precision
    /// modelBuilder.Entity&lt;Product&gt;()
    ///     .Property(p =&gt; p.Price)
    ///     .HasMySqlNumericType(MySqlNumericType.Decimal, unsigned: true)
    ///     .HasPrecision(10, 2);
    ///     
    /// // Equivalent MySQL SQL:
    /// // ALTER TABLE Users MODIFY COLUMN Age TINYINT UNSIGNED;
    /// // ALTER TABLE Products MODIFY COLUMN Price DECIMAL(10,2) UNSIGNED;
    /// </code>
    /// </example>
    /// <remarks>
    /// Numeric type benefits:
    /// - Optimized storage: TINYINT uses 1 byte vs INT's 4 bytes
    /// - Unsigned doubles positive range without negative values
    /// - Better query performance with appropriate type selection
    /// - Automatic range validation at database level
    /// 
    /// Storage size comparison:
    /// - TINYINT: 1 byte (-128 to 127, or 0 to 255 unsigned)
    /// - SMALLINT: 2 bytes (-32K to 32K, or 0 to 65K unsigned)  
    /// - MEDIUMINT: 3 bytes (-8M to 8M, or 0 to 16M unsigned)
    /// - INT: 4 bytes (-2B to 2B, or 0 to 4B unsigned)
    /// - BIGINT: 8 bytes (full 64-bit range)
    /// </remarks>
    public static PropertyBuilder<T> HasMySqlNumericType<T>(
        this PropertyBuilder<T> builder,
        MySqlNumericType numericType,
        bool unsigned = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var baseType = numericType switch
        {
            MySqlNumericType.TinyInt => "TINYINT",
            MySqlNumericType.SmallInt => "SMALLINT", 
            MySqlNumericType.MediumInt => "MEDIUMINT",
            MySqlNumericType.Int => "INT",
            MySqlNumericType.BigInt => "BIGINT",
            MySqlNumericType.Decimal => "DECIMAL",
            MySqlNumericType.Float => "FLOAT",
            MySqlNumericType.Double => "DOUBLE",
            _ => throw new ArgumentOutOfRangeException(nameof(numericType))
        };

        var unsignedClause = unsigned ? " UNSIGNED" : string.Empty;
        var columnType = $"{baseType}{unsignedClause}";

        return builder
            .HasColumnType(columnType)
            .HasAnnotation("MySql:NumericType", numericType)
            .HasAnnotation("MySql:Unsigned", unsigned);
    }

    /// <summary>
    /// Configures the property to use MySQL binary data types optimized for different data sizes and access patterns.
    /// Provides efficient storage for binary data like images, documents, and serialized objects.
    /// </summary>
    /// <param name="builder">The property builder for byte array properties</param>
    /// <param name="binaryType">The MySQL binary type to use</param>
    /// <param name="length">Maximum length for BINARY and VARBINARY types (optional)</param>
    /// <returns>The same builder instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Configure user avatar as MEDIUMBLOB (up to 16MB)
    /// modelBuilder.Entity&lt;User&gt;()
    ///     .Property(u =&gt; u.Avatar)
    ///     .HasMySqlBinaryType(MySqlBinaryType.MediumBlob);
    ///     
    /// // Configure file hash as fixed-length BINARY
    /// modelBuilder.Entity&lt;FileRecord&gt;()
    ///     .Property(f =&gt; f.Hash)
    ///     .HasMySqlBinaryType(MySqlBinaryType.Binary, length: 32);
    ///     
    /// // Equivalent MySQL SQL:
    /// // ALTER TABLE Users MODIFY COLUMN Avatar MEDIUMBLOB;
    /// // ALTER TABLE FileRecords MODIFY COLUMN Hash BINARY(32);
    /// </code>
    /// </example>
    /// <remarks>
    /// Binary type characteristics:
    /// - BINARY: Fixed-length, space-padded, up to 255 bytes
    /// - VARBINARY: Variable-length, no padding, up to 65,535 bytes
    /// - TINYBLOB: Variable-length BLOB, up to 255 bytes
    /// - BLOB: Variable-length BLOB, up to 64 KB
    /// - MEDIUMBLOB: Variable-length BLOB, up to 16 MB  
    /// - LONGBLOB: Variable-length BLOB, up to 4 GB
    /// 
    /// Performance considerations:
    /// - Smaller BLOB types have better performance for small data
    /// - BINARY is fastest for fixed-length data like hashes
    /// - Large BLOBs may impact memory usage during queries
    /// - Consider external storage for very large binary data
    /// </remarks>
    public static PropertyBuilder<byte[]> HasMySqlBinaryType(
        this PropertyBuilder<byte[]> builder,
        MySqlBinaryType binaryType,
        int? length = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var columnType = binaryType switch
        {
            MySqlBinaryType.Binary => length.HasValue ? $"BINARY({length})" : "BINARY(255)",
            MySqlBinaryType.VarBinary => length.HasValue ? $"VARBINARY({length})" : "VARBINARY(255)",
            MySqlBinaryType.TinyBlob => "TINYBLOB",
            MySqlBinaryType.Blob => "BLOB",
            MySqlBinaryType.MediumBlob => "MEDIUMBLOB",
            MySqlBinaryType.LongBlob => "LONGBLOB",
            _ => throw new ArgumentOutOfRangeException(nameof(binaryType))
        };

        // Validate length for types that support it
        if (length.HasValue)
        {
            if (binaryType == MySqlBinaryType.Binary && (length < 1 || length > 255))
            {
                throw new ArgumentException("BINARY length must be between 1 and 255", nameof(length));
            }
            if (binaryType == MySqlBinaryType.VarBinary && (length < 1 || length > 65535))
            {
                throw new ArgumentException("VARBINARY length must be between 1 and 65535", nameof(length));
            }
            if (binaryType is MySqlBinaryType.TinyBlob or MySqlBinaryType.Blob or MySqlBinaryType.MediumBlob or MySqlBinaryType.LongBlob)
            {
                throw new ArgumentException($"{binaryType} does not support explicit length specification", nameof(length));
            }
        }

        return builder
            .HasColumnType(columnType)
            .HasAnnotation("MySql:BinaryType", binaryType)
            .HasAnnotation("MySql:BinaryLength", length);
    }

    private static void ValidateEnumValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > 255)
        {
            throw new ArgumentException($"ENUM value '{value}' exceeds maximum length of 255 characters");
        }

        // Check for potentially dangerous characters
        if (value.Contains('\0'))
        {
            throw new ArgumentException($"ENUM value '{value}' contains null character which is not allowed");
        }
    }

    private static void ValidateSetValue(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length > 255)
        {
            throw new ArgumentException($"SET value '{value}' exceeds maximum length of 255 characters");
        }

        if (value.Contains(','))
        {
            throw new ArgumentException($"SET value '{value}' cannot contain comma character");
        }

        if (value.Contains('\0'))
        {
            throw new ArgumentException($"SET value '{value}' contains null character which is not allowed");
        }
    }
}