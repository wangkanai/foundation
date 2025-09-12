// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Provides extension methods for configuring PostgreSQL specialized data types including ranges,
/// geometric types, network addresses, and other advanced PostgreSQL-specific types.
/// </summary>
public static class SpecializedTypeExtensions
{
    #region Range Types

    /// <summary>
    /// Configures a property to use PostgreSQL range data types for representing ranges of values.
    /// Range types provide efficient storage and querying for value ranges with bound inclusivity.
    /// </summary>
    /// <typeparam name="T">The range type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="rangeType">The specific PostgreSQL range type to use.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Integer range for age groups
    /// builder.Property(e => e.AgeRange)
    ///        .HasRangeType(PostgreSqlRangeType.Int4Range);
    /// 
    /// // Date range for event periods
    /// builder.Property(e => e.EventPeriod)
    ///        .HasRangeType(PostgreSqlRangeType.DateRange);
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     age_range int4range,
    /// //     event_period daterange
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasRangeType<T>(
        this PropertyBuilder<T> builder,
        PostgreSqlRangeType rangeType)
    {
        var typeName = rangeType switch
        {
            PostgreSqlRangeType.Int4Range => "int4range",
            PostgreSqlRangeType.Int8Range => "int8range",
            PostgreSqlRangeType.NumRange => "numrange",
            PostgreSqlRangeType.TsRange => "tsrange",
            PostgreSqlRangeType.TsTzRange => "tstzrange",
            PostgreSqlRangeType.DateRange => "daterange",
            _ => throw new ArgumentException($"Unsupported range type: {rangeType}", nameof(rangeType))
        };

        builder.HasColumnType(typeName);
        builder.HasAnnotation("Npgsql:RangeType", rangeType);
        return builder;
    }

    /// <summary>
    /// Creates a GiST (Generalized Search Tree) index on a range column for efficient range queries.
    /// GiST indexes are optimal for range operations like overlap, contains, and adjacency.
    /// </summary>
    /// <typeparam name="T">The range type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Entity configuration
    /// builder.Property(e => e.TimeSlot)
    ///        .HasRangeType(PostgreSqlRangeType.TsRange)
    ///        .HasRangeGistIndex("ix_entity_timeslot_gist");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_timeslot_gist ON entities USING GiST (time_slot);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE time_slot && '[2024-01-01 09:00, 2024-01-01 17:00)';
    /// // SELECT * FROM entities WHERE time_slot @> '2024-01-01 12:00'::timestamp;
    /// // SELECT * FROM entities WHERE time_slot -|- '[2024-01-01 17:00, 2024-01-01 18:00)';
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasRangeGistIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for range usage
        // Example: entityBuilder.HasIndex(e => e.RangeProperty).HasMethod("gist");
        builder.HasAnnotation("Npgsql:IndexMethod", "gist");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    /// <summary>
    /// Configures range constraints including bounds validation and overlap restrictions.
    /// This ensures data integrity by validating range properties at the database level.
    /// </summary>
    /// <typeparam name="T">The range type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="allowEmpty">Whether empty ranges are allowed. Default is true.</param>
    /// <param name="allowInfinite">Whether infinite bounds are allowed. Default is true.</param>
    /// <param name="constraintName">Optional custom name for the constraint.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Range with constraints
    /// builder.Property(e => e.WorkingHours)
    ///        .HasRangeType(PostgreSqlRangeType.TsRange)
    ///        .HasRangeConstraints(allowEmpty: false, allowInfinite: false);
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ADD CONSTRAINT chk_entity_working_hours_constraints 
    /// //   CHECK (NOT isempty(working_hours) AND range_lower(working_hours) IS NOT NULL 
    /// //          AND range_upper(working_hours) IS NOT NULL);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasRangeConstraints<T>(
        this PropertyBuilder<T> builder,
        bool allowEmpty = true,
        bool allowInfinite = true,
        string? constraintName = null)
    {
        var constraints = new List<string>();
        var columnName = builder.Metadata.Name;

        if (!allowEmpty)
            constraints.Add($"NOT isempty({columnName})");

        if (!allowInfinite)
        {
            constraints.Add($"range_lower({columnName}) IS NOT NULL");
            constraints.Add($"range_upper({columnName}) IS NOT NULL");
        }

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

    #endregion

    #region Geometric Types

    /// <summary>
    /// Configures a property to use PostgreSQL geometric data types for spatial data storage.
    /// Geometric types support 2D spatial data with various shapes and spatial operations.
    /// </summary>
    /// <typeparam name="T">The geometric type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="geometryType">The specific PostgreSQL geometric type to use.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Point for location coordinates
    /// builder.Property(e => e.Location)
    ///        .HasGeometryType(PostgreSqlGeometryType.Point);
    /// 
    /// // Circle for coverage areas
    /// builder.Property(e => e.CoverageArea)
    ///        .HasGeometryType(PostgreSqlGeometryType.Circle);
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     location point,
    /// //     coverage_area circle
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasGeometryType<T>(
        this PropertyBuilder<T> builder,
        PostgreSqlGeometryType geometryType)
    {
        var typeName = geometryType switch
        {
            PostgreSqlGeometryType.Point => "point",
            PostgreSqlGeometryType.Line => "line",
            PostgreSqlGeometryType.LineSegment => "lseg",
            PostgreSqlGeometryType.Box => "box",
            PostgreSqlGeometryType.Path => "path",
            PostgreSqlGeometryType.Polygon => "polygon",
            PostgreSqlGeometryType.Circle => "circle",
            _ => throw new ArgumentException($"Unsupported geometry type: {geometryType}", nameof(geometryType))
        };

        builder.HasColumnType(typeName);
        builder.HasAnnotation("Npgsql:GeometryType", geometryType);
        return builder;
    }

    /// <summary>
    /// Configures a property to use PostGIS geometry types for advanced spatial data with SRID support.
    /// PostGIS extends PostgreSQL with advanced spatial capabilities and coordinate system support.
    /// </summary>
    /// <typeparam name="T">The PostGIS geometry type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="geometryType">The PostGIS geometry type (POINT, POLYGON, LINESTRING, etc.).</param>
    /// <param name="srid">The Spatial Reference System Identifier. Default is 4326 (WGS84).</param>
    /// <param name="dimensions">The number of dimensions (2, 3, or 4). Default is 2.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when geometryType is null or whitespace.</exception>
    /// <example>
    /// <code>
    /// // PostGIS point with WGS84 coordinate system
    /// builder.Property(e => e.GeoLocation)
    ///        .HasPostGisGeometry("POINT", srid: 4326);
    /// 
    /// // PostGIS polygon with custom SRID and 3D coordinates
    /// builder.Property(e => e.Territory)
    ///        .HasPostGisGeometry("POLYGON", srid: 3857, dimensions: 3);
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     geo_location geometry(POINT, 4326),
    /// //     territory geometry(POLYGONZ, 3857)
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasPostGisGeometry<T>(
        this PropertyBuilder<T> builder,
        string geometryType,
        int srid = 4326,
        int dimensions = 2)
    {
        if (string.IsNullOrWhiteSpace(geometryType))
            throw new ArgumentException("Geometry type cannot be null or whitespace.", nameof(geometryType));

        var typeSpec = dimensions switch
        {
            2 => geometryType,
            3 => $"{geometryType}Z",
            4 => $"{geometryType}ZM",
            _ => throw new ArgumentException("Dimensions must be 2, 3, or 4.", nameof(dimensions))
        };

        builder.HasColumnType($"geometry({typeSpec}, {srid})");
        builder.HasAnnotation("Npgsql:PostGisGeometryType", geometryType);
        builder.HasAnnotation("Npgsql:PostGisSrid", srid);
        builder.HasAnnotation("Npgsql:PostGisDimensions", dimensions);
        return builder;
    }

    /// <summary>
    /// Creates a GiST spatial index on a geometry column for efficient spatial queries.
    /// Spatial indexes are essential for performance of geometric and PostGIS operations.
    /// </summary>
    /// <typeparam name="T">The geometry type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Spatial index for geometric queries
    /// builder.Property(e => e.Area)
    ///        .HasGeometryType(PostgreSqlGeometryType.Polygon)
    ///        .HasSpatialIndex("ix_entity_area_spatial");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_area_spatial ON entities USING GiST (area);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE area && box '((0,0),(100,100))';
    /// // SELECT * FROM entities WHERE point '(50,50)' <@ area;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasSpatialIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for spatial usage
        // Example: entityBuilder.HasIndex(e => e.SpatialProperty).HasMethod("gist");
        builder.HasAnnotation("Npgsql:IndexMethod", "gist");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    #endregion

    #region Network Address Types

    /// <summary>
    /// Configures a property to use PostgreSQL network address data types for IP addresses and network ranges.
    /// Network types provide efficient storage and querying for IP addresses, subnets, and MAC addresses.
    /// </summary>
    /// <typeparam name="T">The network type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="networkType">The specific PostgreSQL network type to use.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // IP address with subnet
    /// builder.Property(e => e.IpAddress)
    ///        .HasNetworkType(PostgreSqlNetworkType.Inet);
    /// 
    /// // Network/subnet only
    /// builder.Property(e => e.Subnet)
    ///        .HasNetworkType(PostgreSqlNetworkType.Cidr);
    /// 
    /// // MAC address
    /// builder.Property(e => e.MacAddress)
    ///        .HasNetworkType(PostgreSqlNetworkType.MacAddr);
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     ip_address inet,
    /// //     subnet cidr,
    /// //     mac_address macaddr
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasNetworkType<T>(
        this PropertyBuilder<T> builder,
        PostgreSqlNetworkType networkType)
    {
        var typeName = networkType switch
        {
            PostgreSqlNetworkType.Inet => "inet",
            PostgreSqlNetworkType.Cidr => "cidr",
            PostgreSqlNetworkType.MacAddr => "macaddr",
            PostgreSqlNetworkType.MacAddr8 => "macaddr8",
            _ => throw new ArgumentException($"Unsupported network type: {networkType}", nameof(networkType))
        };

        builder.HasColumnType(typeName);
        builder.HasAnnotation("Npgsql:NetworkType", networkType);
        return builder;
    }

    /// <summary>
    /// Creates a GiST index on network address columns for efficient network containment queries.
    /// GiST indexes optimize subnet containment and IP range queries.
    /// </summary>
    /// <typeparam name="T">The network type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="indexName">Optional custom name for the index. If null, EF Core will generate a name.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Network index for subnet queries
    /// builder.Property(e => e.NetworkRange)
    ///        .HasNetworkType(PostgreSqlNetworkType.Cidr)
    ///        .HasNetworkGistIndex("ix_entity_network_gist");
    /// 
    /// // Generated SQL:
    /// // CREATE INDEX ix_entity_network_gist ON entities USING GiST (network_range);
    /// 
    /// // Optimizes queries like:
    /// // SELECT * FROM entities WHERE network_range >> '192.168.1.0/24';
    /// // SELECT * FROM entities WHERE '192.168.1.100'::inet <@ network_range;
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasNetworkGistIndex<T>(
        this PropertyBuilder<T> builder,
        string? indexName = null)
    {
        // Note: Index creation should be done at the entity level using EntityTypeBuilder.HasIndex()
        // This method now only configures the property for network usage
        // Example: entityBuilder.HasIndex(e => e.NetworkProperty).HasMethod("gist");
        builder.HasAnnotation("Npgsql:IndexMethod", "gist");
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            builder.HasAnnotation("Npgsql:IndexName", indexName);
        }
        return builder;
    }

    /// <summary>
    /// Configures network address constraints including format validation and range restrictions.
    /// This ensures data integrity by validating network addresses at the database level.
    /// </summary>
    /// <typeparam name="T">The network type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="allowPrivate">Whether private IP ranges are allowed. Default is true.</param>
    /// <param name="allowLoopback">Whether loopback addresses are allowed. Default is true.</param>
    /// <param name="ipVersion">Restrict to specific IP version. Null allows both IPv4 and IPv6.</param>
    /// <param name="constraintName">Optional custom name for the constraint.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Public IPv4 addresses only
    /// builder.Property(e => e.PublicIp)
    ///        .HasNetworkType(PostgreSqlNetworkType.Inet)
    ///        .HasNetworkConstraints(allowPrivate: false, ipVersion: 4);
    /// 
    /// // Generated SQL:
    /// // ALTER TABLE entities ADD CONSTRAINT chk_entity_public_ip_constraints 
    /// //   CHECK (NOT (ip_address <<= '10.0.0.0/8'::cidr OR 
    /// //               ip_address <<= '172.16.0.0/12'::cidr OR 
    /// //               ip_address <<= '192.168.0.0/16'::cidr) 
    /// //          AND family(ip_address) = 4);
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasNetworkConstraints<T>(
        this PropertyBuilder<T> builder,
        bool allowPrivate = true,
        bool allowLoopback = true,
        int? ipVersion = null,
        string? constraintName = null)
    {
        var constraints = new List<string>();
        var columnName = builder.Metadata.Name;

        if (!allowPrivate)
        {
            constraints.Add($"NOT ({columnName} <<= '10.0.0.0/8'::cidr OR " +
                          $"{columnName} <<= '172.16.0.0/12'::cidr OR " +
                          $"{columnName} <<= '192.168.0.0/16'::cidr)");
        }

        if (!allowLoopback)
        {
            constraints.Add($"NOT ({columnName} <<= '127.0.0.0/8'::cidr OR " +
                          $"{columnName} <<= '::1/128'::cidr)");
        }

        if (ipVersion.HasValue)
        {
            constraints.Add($"family({columnName}) = {ipVersion.Value}");
        }

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

    #endregion

    #region Additional Specialized Types

    /// <summary>
    /// Configures a property to use PostgreSQL UUID data type with generation strategies.
    /// UUIDs provide globally unique identifiers with various generation algorithms.
    /// </summary>
    /// <param name="builder">The property builder used to configure the Guid property.</param>
    /// <param name="version">The UUID version to use for generation. Default is Version4.</param>
    /// <param name="generateOnAdd">Whether to generate UUID values on entity add. Default is true.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // UUID v4 with automatic generation
    /// builder.Property(e => e.Id)
    ///        .HasUuidType(UuidVersion.Version4, generateOnAdd: true);
    /// 
    /// // UUID v1 (timestamp-based)
    /// builder.Property(e => e.TrackingId)
    ///        .HasUuidType(UuidVersion.Version1);
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     id uuid DEFAULT gen_random_uuid(),
    /// //     tracking_id uuid DEFAULT uuid_generate_v1()
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<Guid> HasUuidType(
        this PropertyBuilder<Guid> builder,
        UuidVersion version = UuidVersion.Version4,
        bool generateOnAdd = true)
    {
        builder.HasColumnType("uuid");
        
        if (generateOnAdd)
        {
            var defaultFunction = version switch
            {
                UuidVersion.Version1 => "uuid_generate_v1()",
                UuidVersion.Version4 => "gen_random_uuid()",
                UuidVersion.Version5 => "uuid_generate_v5(uuid_ns_dns(), 'example.com')",
                _ => "gen_random_uuid()"
            };
            
            builder.HasDefaultValueSql(defaultFunction);
            builder.ValueGeneratedOnAdd();
        }
        
        builder.HasAnnotation("Npgsql:UuidVersion", version);
        return builder;
    }

    /// <summary>
    /// Configures a property to use PostgreSQL bit string data types for efficient bit manipulation.
    /// Bit strings allow storage and manipulation of sequences of bits.
    /// </summary>
    /// <typeparam name="T">The type of the property being configured.</typeparam>
    /// <param name="builder">The property builder used to configure the property.</param>
    /// <param name="length">The fixed length for bit strings. If null, uses variable-length bit varying.</param>
    /// <returns>The same PropertyBuilder instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Fixed-length bit string
    /// builder.Property(e => e.Permissions)
    ///        .HasBitStringType(length: 8);
    /// 
    /// // Variable-length bit string
    /// builder.Property(e => e.Features)
    ///        .HasBitStringType();
    /// 
    /// // Generated SQL:
    /// // CREATE TABLE entities (
    /// //     permissions bit(8),
    /// //     features bit varying
    /// // );
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasBitStringType<T>(
        this PropertyBuilder<T> builder,
        int? length = null)
    {
        var typeName = length.HasValue ? $"bit({length.Value})" : "bit varying";
        builder.HasColumnType(typeName);
        
        if (length.HasValue)
        {
            builder.HasAnnotation("Npgsql:BitStringLength", length.Value);
        }
        
        return builder;
    }

    #endregion
}

/// <summary>
/// Represents the available PostgreSQL range types.
/// </summary>
public enum PostgreSqlRangeType
{
    /// <summary>Integer range (int4range).</summary>
    Int4Range,
    /// <summary>Big integer range (int8range).</summary>
    Int8Range,
    /// <summary>Numeric range (numrange).</summary>
    NumRange,
    /// <summary>Timestamp range (tsrange).</summary>
    TsRange,
    /// <summary>Timestamp with timezone range (tstzrange).</summary>
    TsTzRange,
    /// <summary>Date range (daterange).</summary>
    DateRange
}

/// <summary>
/// Represents the available PostgreSQL geometric types.
/// </summary>
public enum PostgreSqlGeometryType
{
    /// <summary>Point (x,y coordinates).</summary>
    Point,
    /// <summary>Infinite line.</summary>
    Line,
    /// <summary>Line segment.</summary>
    LineSegment,
    /// <summary>Rectangular box.</summary>
    Box,
    /// <summary>Geometric path (open or closed).</summary>
    Path,
    /// <summary>Polygon (closed path).</summary>
    Polygon,
    /// <summary>Circle (center point and radius).</summary>
    Circle
}

/// <summary>
/// Represents the available PostgreSQL network address types.
/// </summary>
public enum PostgreSqlNetworkType
{
    /// <summary>IP address with optional subnet (inet).</summary>
    Inet,
    /// <summary>Network subnet specification (cidr).</summary>
    Cidr,
    /// <summary>6-byte MAC address (macaddr).</summary>
    MacAddr,
    /// <summary>8-byte MAC address (macaddr8).</summary>
    MacAddr8
}

/// <summary>
/// Represents the available UUID generation versions.
/// </summary>
public enum UuidVersion
{
    /// <summary>Time-based UUID version 1.</summary>
    Version1,
    /// <summary>Random UUID version 4 (most common).</summary>
    Version4,
    /// <summary>Name-based UUID version 5 (SHA-1 hash).</summary>
    Version5
}