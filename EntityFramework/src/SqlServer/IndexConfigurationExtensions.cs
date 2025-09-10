using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.EntityFramework.SqlServer;

/// <summary>
/// Provides SQL Server-specific index configuration extensions for Entity Framework Core.
/// These extensions enable advanced indexing strategies that leverage SQL Server enterprise features
/// for maximum query performance and storage efficiency.
/// </summary>
public static class IndexConfigurationExtensions
{
    /// <summary>
    /// Creates a filtered index with a WHERE clause for selective indexing.
    /// Filtered indexes are ideal for sparse data patterns and can significantly reduce index size
    /// and maintenance overhead while improving query performance for matching predicates.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="builder">The index builder instance</param>
    /// <param name="filterExpression">The WHERE clause expression for the filtered index</param>
    /// <returns>The same index builder instance for method chaining</returns>
    /// <remarks>
    /// <para>
    /// Filtered indexes provide performance benefits by:
    /// - Reducing index size by excluding null values or inactive records
    /// - Lowering maintenance costs for INSERT/UPDATE/DELETE operations
    /// - Improving query performance for matching filter predicates
    /// - Reducing storage requirements and memory footprint
    /// </para>
    /// <para>
    /// Best practices:
    /// - Use for columns with high selectivity (e.g., IsActive = 1, Status = 'Active')
    /// - Avoid complex filter expressions that might confuse the query optimizer
    /// - Ensure queries match the filter predicate exactly for optimal plan selection
    /// - Consider combining with included columns for covering index benefits
    /// </para>
    /// <para>
    /// SQL Server generates T-SQL equivalent to:
    /// <code>
    /// CREATE NONCLUSTERED INDEX IX_EntityName_ColumnName 
    /// ON EntityName (ColumnName) 
    /// WHERE FilterExpression;
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure a filtered index for active users only:
    /// <code>
    /// builder.Entity&lt;User&gt;(entity =>
    /// {
    ///     entity.HasIndex(u => u.Email)
    ///           .HasSqlServerFilteredIndex("IsActive = 1")
    ///           .IsUnique();
    /// });
    /// </code>
    /// </example>
    public static IndexBuilder<T> HasSqlServerFilteredIndex<T>(
        this IndexBuilder<T> builder,
        string filterExpression) 
        where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filterExpression);
        
        return builder.HasFilter(filterExpression);
    }

    /// <summary>
    /// Creates a covering index with INCLUDE columns to eliminate key lookups.
    /// Covering indexes store non-key column data at the leaf level, allowing queries
    /// to be satisfied entirely from the index without accessing the base table.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="builder">The index builder instance</param>
    /// <param name="includedColumns">Column expressions to include in the index</param>
    /// <returns>The same index builder instance for method chaining</returns>
    /// <remarks>
    /// <para>
    /// Covering indexes provide significant performance improvements by:
    /// - Eliminating expensive key lookup operations
    /// - Reducing I/O by avoiding table access for covered queries
    /// - Improving query performance for reporting and analytical workloads
    /// - Enabling efficient execution of queries with SELECT, WHERE, and ORDER BY clauses
    /// </para>
    /// <para>
    /// Best practices:
    /// - Include frequently accessed non-key columns in SELECT clauses
    /// - Balance between query performance and index maintenance costs
    /// - Monitor index usage statistics to ensure the included columns are beneficial
    /// - Limit the number of included columns to avoid excessive index size
    /// </para>
    /// <para>
    /// SQL Server generates T-SQL equivalent to:
    /// <code>
    /// CREATE NONCLUSTERED INDEX IX_EntityName_KeyColumns
    /// ON EntityName (KeyColumn1, KeyColumn2)
    /// INCLUDE (IncludedColumn1, IncludedColumn2);
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure a covering index for efficient order queries:
    /// <code>
    /// builder.Entity&lt;Order&gt;(entity =>
    /// {
    ///     entity.HasIndex(o => new { o.CustomerId, o.OrderDate })
    ///           .HasSqlServerIncludedColumns(o => new { o.TotalAmount, o.Status, o.ShippingAddress });
    /// });
    /// </code>
    /// </example>
    public static IndexBuilder<T> HasSqlServerIncludedColumns<T>(
        this IndexBuilder<T> builder,
        params Expression<Func<T, object>>[] includedColumns)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(includedColumns);
        
        if (includedColumns.Length == 0)
            throw new ArgumentException("At least one included column must be specified.", nameof(includedColumns));

        var propertyNames = ExtractPropertyNames(includedColumns);
        return builder.IncludeProperties(propertyNames);
    }

    /// <summary>
    /// Extracts property names from lambda expressions for use with EF Core configuration methods.
    /// Handles various expression types including member access, unary expressions, and new expressions.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="expressions">Array of lambda expressions representing property access</param>
    /// <returns>Array of property names as strings</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported expression type is encountered</exception>
    private static string[] ExtractPropertyNames<T>(Expression<Func<T, object>>[] expressions)
    {
        var propertyNames = new List<string>();

        foreach (var expression in expressions)
        {
            var properties = ExtractPropertiesFromExpression(expression.Body);
            propertyNames.AddRange(properties);
        }

        return propertyNames.ToArray();
    }

    /// <summary>
    /// Recursively extracts property names from different types of expressions.
    /// </summary>
    /// <param name="expression">The expression to analyze</param>
    /// <returns>Collection of property names</returns>
    /// <exception cref="ArgumentException">Thrown when an unsupported expression type is encountered</exception>
    private static IEnumerable<string> ExtractPropertiesFromExpression(Expression expression)
    {
        return expression switch
        {
            // Direct property access: x => x.PropertyName
            MemberExpression memberExpr => new[] { memberExpr.Member.Name },
            
            // Boxing conversion: x => (object)x.PropertyName
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression memberExpr } => 
                new[] { memberExpr.Member.Name },
            
            // Anonymous object creation: x => new { x.Prop1, x.Prop2 }
            NewExpression newExpr when newExpr.Arguments.Count > 0 =>
                newExpr.Arguments.SelectMany(ExtractPropertiesFromExpression),
            
            // Member initialization: x => new { x.Prop1, SomeValue = x.Prop2 }
            MemberInitExpression initExpr =>
                initExpr.Bindings
                    .OfType<MemberAssignment>()
                    .SelectMany(binding => ExtractPropertiesFromExpression(binding.Expression)),
            
            _ => throw new ArgumentException(
                $"Unsupported expression type '{expression.NodeType}' for property extraction. " +
                $"Supported expressions include direct property access (x => x.Property), " +
                $"converted property access (x => (object)x.Property), " +
                $"and anonymous objects (x => new {{ x.Prop1, x.Prop2 }}).", 
                nameof(expression))
        };
    }

    /// <summary>
    /// Configures the index fill factor to optimize for page splits and storage efficiency.
    /// Fill factor controls the percentage of space filled on leaf-level pages during index creation,
    /// leaving room for future INSERT and UPDATE operations to minimize page splits.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="builder">The index builder instance</param>
    /// <param name="fillFactor">The fill factor percentage (1-100), default is 80</param>
    /// <returns>The same index builder instance for method chaining</returns>
    /// <remarks>
    /// <para>
    /// Fill factor optimization provides benefits for:
    /// - Reducing page splits in high-insert/update scenarios
    /// - Balancing storage efficiency with modification performance
    /// - Optimizing index maintenance operations
    /// - Minimizing index fragmentation over time
    /// </para>
    /// <para>
    /// Fill factor guidelines:
    /// - 100%: Read-only or rarely modified data (maximum storage efficiency)
    /// - 80-90%: Low to moderate modification activity (default recommendation)
    /// - 70-80%: High modification activity with frequent INSERT/UPDATE operations
    /// - 50-70%: Very high modification activity or random key patterns
    /// </para>
    /// <para>
    /// SQL Server generates T-SQL equivalent to:
    /// <code>
    /// CREATE NONCLUSTERED INDEX IX_EntityName_ColumnName
    /// ON EntityName (ColumnName)
    /// WITH (FILLFACTOR = 80);
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure fill factor for a frequently updated index:
    /// <code>
    /// builder.Entity&lt;Product&gt;(entity =>
    /// {
    ///     entity.HasIndex(p => p.LastModified)
    ///           .WithSqlServerFillFactor(70); // Leave more room for updates
    /// });
    /// </code>
    /// </example>
    public static IndexBuilder<T> WithSqlServerFillFactor<T>(
        this IndexBuilder<T> builder,
        int fillFactor = 80)
        where T : class
    {
        if (fillFactor < 1 || fillFactor > 100)
            throw new ArgumentOutOfRangeException(nameof(fillFactor), 
                "Fill factor must be between 1 and 100.");

        return builder.HasFillFactor(fillFactor);
    }

    /// <summary>
    /// Creates a spatial index for geography or geometry columns to optimize location-based queries.
    /// Spatial indexes use a hierarchical grid system to efficiently index spatial data and enable
    /// high-performance spatial operations and proximity searches.
    /// </summary>
    /// <typeparam name="T">The spatial column type (Microsoft.SqlServer.Types.SqlGeography or SqlGeometry)</typeparam>
    /// <param name="builder">The property builder for the spatial column</param>
    /// <param name="settings">Optional spatial index configuration settings</param>
    /// <returns>The same property builder instance for method chaining</returns>
    /// <remarks>
    /// <para>
    /// Spatial indexes provide significant performance improvements for:
    /// - Proximity queries (finding nearby locations)
    /// - Spatial intersection and containment operations
    /// - Geographic range and boundary queries
    /// - Location-based filtering and sorting operations
    /// </para>
    /// <para>
    /// Spatial index considerations:
    /// - Choose appropriate grid density based on data distribution
    /// - Consider bounding box settings for optimal performance
    /// - Monitor spatial index usage and adjust grid levels as needed
    /// - Use GEOGRAPHY for earth-based coordinate systems (lat/lng)
    /// - Use GEOMETRY for planar/projected coordinate systems
    /// </para>
    /// <para>
    /// SQL Server generates T-SQL equivalent to:
    /// <code>
    /// CREATE SPATIAL INDEX SX_EntityName_LocationColumn
    /// ON EntityName (LocationColumn)
    /// USING GEOGRAPHY_GRID
    /// WITH (
    ///     GRIDS = (LEVEL_1 = MEDIUM, LEVEL_2 = MEDIUM, LEVEL_3 = MEDIUM, LEVEL_4 = MEDIUM),
    ///     CELLS_PER_OBJECT = 16
    /// );
    /// </code>
    /// </para>
    /// </remarks>
    /// <example>
    /// Configure a spatial index for a location column:
    /// <code>
    /// builder.Entity&lt;Store&gt;(entity =>
    /// {
    ///     entity.Property(s => s.Location)
    ///           .HasColumnType("geography")
    ///           .HasSqlServerSpatialIndex(new SpatialIndexSettings
    ///           {
    ///               GridDensity = SpatialGridDensity.Medium,
    ///               CellsPerObject = 16
    ///           });
    /// });
    /// </code>
    /// </example>
    public static PropertyBuilder<T> HasSqlServerSpatialIndex<T>(
        this PropertyBuilder<T> builder,
        SpatialIndexSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        // Apply spatial index configuration
        var indexName = $"SX_{((IMutableEntityType)builder.Metadata.DeclaringType).GetTableName()}_{builder.Metadata.GetColumnName()}";
        
        // Configure the spatial index annotation
        builder.Metadata.SetAnnotation("SqlServer:SpatialIndex", settings ?? new SpatialIndexSettings());
        builder.Metadata.SetAnnotation("SqlServer:SpatialIndexName", indexName);
        
        return builder;
    }
}

/// <summary>
/// Configuration settings for SQL Server spatial indexes.
/// Provides fine-grained control over spatial index creation and optimization.
/// </summary>
public class SpatialIndexSettings
{
    /// <summary>
    /// Gets or sets the grid density for the spatial index levels.
    /// Higher density provides better precision but increases index size and maintenance cost.
    /// </summary>
    /// <value>The grid density setting (Low, Medium, High). Default is Medium.</value>
    public SpatialGridDensity GridDensity { get; set; } = SpatialGridDensity.Medium;

    /// <summary>
    /// Gets or sets the number of cells per object for spatial index tessellation.
    /// Higher values provide better precision for complex geometries but increase storage.
    /// </summary>
    /// <value>The cells per object count (1-8192). Default is 16.</value>
    public int CellsPerObject { get; set; } = 16;

    /// <summary>
    /// Gets or sets the coordinate system type for the spatial index.
    /// </summary>
    /// <value>The coordinate system type. Default is Geography.</value>
    public SpatialCoordinateSystem CoordinateSystem { get; set; } = SpatialCoordinateSystem.Geography;

    /// <summary>
    /// Gets or sets the bounding box for geometry spatial indexes.
    /// Only applicable for GEOMETRY coordinate systems.
    /// </summary>
    /// <value>The bounding box coordinates, or null for auto-detection.</value>
    public SpatialBoundingBox? BoundingBox { get; set; }

    /// <summary>
    /// Validates the spatial index settings for consistency and SQL Server compatibility.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when settings are invalid or incompatible.</exception>
    public void Validate()
    {
        if (CellsPerObject < 1 || CellsPerObject > 8192)
            throw new InvalidOperationException("CellsPerObject must be between 1 and 8192.");

        if (CoordinateSystem == SpatialCoordinateSystem.Geometry && BoundingBox == null)
            throw new InvalidOperationException("BoundingBox is required for Geometry coordinate system.");

        if (CoordinateSystem == SpatialCoordinateSystem.Geography && BoundingBox != null)
            throw new InvalidOperationException("BoundingBox should not be specified for Geography coordinate system.");
    }
}

/// <summary>
/// Defines the grid density levels for spatial index tessellation.
/// Higher density levels provide better precision but increase storage and maintenance overhead.
/// </summary>
public enum SpatialGridDensity
{
    /// <summary>Low grid density - best for sparse spatial data with large objects.</summary>
    Low,
    /// <summary>Medium grid density - balanced performance for most scenarios.</summary>
    Medium,
    /// <summary>High grid density - best for dense spatial data with small, precise objects.</summary>
    High
}

/// <summary>
/// Defines the coordinate system types supported by SQL Server spatial indexes.
/// </summary>
public enum SpatialCoordinateSystem
{
    /// <summary>Geography coordinate system for earth-based lat/lng data (EPSG:4326).</summary>
    Geography,
    /// <summary>Geometry coordinate system for planar/projected coordinate data.</summary>
    Geometry
}

/// <summary>
/// Defines the bounding box coordinates for geometry spatial indexes.
/// Used to optimize spatial index performance by defining the data extents.
/// </summary>
public class SpatialBoundingBox
{
    /// <summary>Gets or sets the minimum X coordinate (longitude/easting).</summary>
    public double MinX { get; set; }

    /// <summary>Gets or sets the minimum Y coordinate (latitude/northing).</summary>
    public double MinY { get; set; }

    /// <summary>Gets or sets the maximum X coordinate (longitude/easting).</summary>
    public double MaxX { get; set; }

    /// <summary>Gets or sets the maximum Y coordinate (latitude/northing).</summary>
    public double MaxY { get; set; }

    /// <summary>
    /// Validates the bounding box coordinates for logical consistency.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when coordinates are invalid.</exception>
    public void Validate()
    {
        if (MinX >= MaxX)
            throw new InvalidOperationException("MinX must be less than MaxX.");

        if (MinY >= MaxY)
            throw new InvalidOperationException("MinY must be less than MaxY.");
    }

    /// <summary>
    /// Returns a string representation of the bounding box coordinates.
    /// </summary>
    /// <returns>A formatted string containing the bounding box coordinates.</returns>
    public override string ToString()
    {
        return $"({MinX}, {MinY}, {MaxX}, {MaxY})";
    }
}