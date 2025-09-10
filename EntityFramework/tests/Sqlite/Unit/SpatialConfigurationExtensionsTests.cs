// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class SpatialConfigurationExtensionsTests
{
    #region Test Entity Classes

    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class DeliveryZone
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Boundary { get; set; } = string.Empty;
    }

    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class DeliveryRoute
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public class ServiceArea
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Boundary { get; set; } = string.Empty;
    }

    public class MapFeature
    {
        public int Id { get; set; }
        public string Geometry { get; set; } = string.Empty;
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<PropertyBuilder<string>> CreateMockPropertyBuilder()
    {
        var mockPropertyBuilder = new Mock<PropertyBuilder<string>>();
        
        mockPropertyBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockPropertyBuilder.Object);

        return mockPropertyBuilder;
    }

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        
        mockEntityBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                        .Returns(mockEntityBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region HasSqliteGeometry Tests

    [Fact]
    public void HasSqliteGeometry_WithValidParameters_ShouldConfigureGeometry()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string geometryType = "POINT";
        const string srid = "4326";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<Store>(geometryType, srid);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        // Verify geometry configuration
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialGeometry", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:GeometryType", "POINT"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SRID", "4326"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Dimension", 2), Times.Once); // Default dimension
    }

    [Fact]
    public void HasSqliteGeometry_WithDefaultSrid_ShouldUseDefaultSrid()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string geometryType = "POLYGON";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<DeliveryZone>(geometryType);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SRID", SpatialConfigurationExtensions.DefaultSrid), Times.Once);
    }

    [Fact]
    public void HasSqliteGeometry_WithCustomDimension_ShouldSetDimension()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string geometryType = "LINESTRING";
        const string srid = "3857";
        const int dimension = 3;

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<DeliveryRoute>(geometryType, srid, dimension);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Dimension", dimension), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasSqliteGeometry_WithInvalidGeometryType_ShouldThrowArgumentException(string? geometryType)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockPropertyBuilder.Object.HasSqliteGeometry<Store>(geometryType!));
    }

    [Theory]
    [InlineData(1)] // Below minimum
    [InlineData(5)] // Above maximum
    public void HasSqliteGeometry_WithInvalidDimension_ShouldThrowArgumentException(int dimension)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockPropertyBuilder.Object.HasSqliteGeometry<Store>("POINT", "4326", dimension));
    }

    [Fact]
    public void HasSqliteGeometry_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<string> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.HasSqliteGeometry<Store>("POINT"));
    }

    #endregion

    #region HasSqliteSpatialIndex Tests

    [Fact]
    public void HasSqliteSpatialIndex_WithDefaultName_ShouldCreateSpatialIndex()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteSpatialIndex<Restaurant>();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockPropertyBuilder.Object, result);
        
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndex", true), Times.Once);
    }

    [Fact]
    public void HasSqliteSpatialIndex_WithCustomName_ShouldSetIndexName()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        const string indexName = "IX_Restaurant_Location";

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteSpatialIndex<Restaurant>(indexName);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndex", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndexName", indexName), Times.Once);
    }

    [Fact]
    public void HasSqliteSpatialIndex_WithEmptyName_ShouldNotSetIndexName()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteSpatialIndex<Restaurant>(string.Empty);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndexName", It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void HasSqliteSpatialIndex_WithNullPropertyBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        PropertyBuilder<string> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.HasSqliteSpatialIndex<Restaurant>());
    }

    #endregion

    #region EnableSqliteSpatialFunctions Tests

    [Fact]
    public void EnableSqliteSpatialFunctions_WithDefaultParameters_ShouldEnableAllFunctions()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<DeliveryRoute>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteSpatialFunctions();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialFunctions", true), Times.Once);
        // When no specific functions are provided, should not set the EnabledSpatialFunctions annotation
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void EnableSqliteSpatialFunctions_WithSpecificFunctions_ShouldEnableOnlySpecifiedFunctions()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        string[] functions = { "Distance", "Area", "Contains", "Intersects" };

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteSpatialFunctions(functions);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialFunctions", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", functions), Times.Once);
    }

    [Fact]
    public void EnableSqliteSpatialFunctions_WithInvalidFunction_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        string[] invalidFunctions = { "Distance", "InvalidFunction", "Area" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.EnableSqliteSpatialFunctions(invalidFunctions));
    }

    [Fact]
    public void EnableSqliteSpatialFunctions_WithEmptyFunctionsArray_ShouldEnableAllFunctions()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        var emptyFunctions = Array.Empty<string>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteSpatialFunctions(emptyFunctions);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialFunctions", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void EnableSqliteSpatialFunctions_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<ServiceArea> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.EnableSqliteSpatialFunctions());
    }

    #endregion

    #region ConfigureSpatialTransformation Tests

    [Fact]
    public void ConfigureSpatialTransformation_WithValidSrids_ShouldConfigureTransformation()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<MapFeature>();
        const string sourceSrid = "4326";
        const string targetSrid = "3857";

        // Act
        var result = mockEntityBuilder.Object.ConfigureSpatialTransformation(sourceSrid, targetSrid);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialTransformation", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SourceSRID", sourceSrid), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:TargetSRID", targetSrid), Times.Once);
    }

    [Theory]
    [InlineData(null, "3857")]
    [InlineData("", "3857")]
    [InlineData("   ", "3857")]
    public void ConfigureSpatialTransformation_WithInvalidSourceSrid_ShouldThrowArgumentException(string? sourceSrid, string targetSrid)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<MapFeature>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.ConfigureSpatialTransformation(sourceSrid!, targetSrid));
    }

    [Theory]
    [InlineData("4326", null)]
    [InlineData("4326", "")]
    [InlineData("4326", "   ")]
    public void ConfigureSpatialTransformation_WithInvalidTargetSrid_ShouldThrowArgumentException(string sourceSrid, string? targetSrid)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<MapFeature>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.ConfigureSpatialTransformation(sourceSrid, targetSrid!));
    }

    [Fact]
    public void ConfigureSpatialTransformation_WithSameSrids_ShouldThrowArgumentException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<MapFeature>();
        const string sameSrid = "4326";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockEntityBuilder.Object.ConfigureSpatialTransformation(sameSrid, sameSrid));
    }

    [Fact]
    public void ConfigureSpatialTransformation_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<MapFeature> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.ConfigureSpatialTransformation("4326", "3857"));
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void SpatialConfigurationMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Restaurant>();

        // Act
        var propertyResult = mockPropertyBuilder.Object
            .HasSqliteGeometry<Restaurant>("POINT", "4326", 2)
            .HasSqliteSpatialIndex<Restaurant>("IX_Restaurant_Location");

        var entityResult = mockEntityBuilder.Object
            .EnableSqliteSpatialFunctions(new[] { "Distance", "Contains" })
            .ConfigureSpatialTransformation("4326", "3857");

        // Assert
        Assert.NotNull(propertyResult);
        Assert.Same(mockPropertyBuilder.Object, propertyResult);
        
        Assert.NotNull(entityResult);
        Assert.Same(mockEntityBuilder.Object, entityResult);
        
        // Verify all methods were called
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialGeometry", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndex", true), Times.Once);
        
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialFunctions", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialTransformation", true), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ComprehensiveSpatialConfiguration_ShouldApplyAllSettings()
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        string[] spatialFunctions = { "Distance", "Area", "Contains", "Intersects", "Buffer" };

        // Act - Configure comprehensive spatial setup
        mockPropertyBuilder.Object
            .HasSqliteGeometry<ServiceArea>("POLYGON", "4326", 2)
            .HasSqliteSpatialIndex<ServiceArea>("IX_ServiceArea_Boundary_Spatial");

        mockEntityBuilder.Object
            .EnableSqliteSpatialFunctions(spatialFunctions)
            .ConfigureSpatialTransformation("4326", "3857");

        // Assert - Verify comprehensive spatial configuration
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialGeometry", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:GeometryType", "POLYGON"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SRID", "4326"), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Dimension", 2), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndex", true), Times.Once);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialIndexName", "IX_ServiceArea_Boundary_Spatial"), Times.Once);

        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialFunctions", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", spatialFunctions), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SpatialTransformation", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SourceSRID", "4326"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:TargetSRID", "3857"), Times.Once);
    }

    #endregion

    #region Geometry Type Tests

    [Theory]
    [InlineData("POINT")]
    [InlineData("LINESTRING")]
    [InlineData("POLYGON")]
    [InlineData("MULTIPOINT")]
    [InlineData("MULTILINESTRING")]
    [InlineData("MULTIPOLYGON")]
    [InlineData("GEOMETRYCOLLECTION")]
    public void HasSqliteGeometry_WithCommonGeometryTypes_ShouldSucceed(string geometryType)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<Store>(geometryType);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:GeometryType", geometryType.ToUpperInvariant()), Times.Once);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void HasSqliteGeometry_WithValidDimensions_ShouldSucceed(int dimension)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<Store>("POINT", "4326", dimension);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:Dimension", dimension), Times.Once);
    }

    #endregion

    #region SRID Tests

    [Theory]
    [InlineData("4326")] // WGS 84
    [InlineData("3857")] // Web Mercator
    [InlineData("2154")] // RGF93 / Lambert-93
    [InlineData("32633")] // WGS 84 / UTM zone 33N
    public void HasSqliteGeometry_WithCommonSrids_ShouldSucceed(string srid)
    {
        // Arrange
        var mockPropertyBuilder = CreateMockPropertyBuilder();

        // Act
        var result = mockPropertyBuilder.Object.HasSqliteGeometry<Store>("POINT", srid);

        // Assert
        Assert.NotNull(result);
        mockPropertyBuilder.Verify(x => x.HasAnnotation("Sqlite:SRID", srid), Times.Once);
    }

    [Fact]
    public void DefaultSrid_ShouldBeWGS84()
    {
        // Assert
        Assert.Equal("4326", SpatialConfigurationExtensions.DefaultSrid);
    }

    #endregion

    #region Spatial Functions Validation Tests

    [Theory]
    [InlineData("Distance")]
    [InlineData("Area")]
    [InlineData("Length")]
    [InlineData("Buffer")]
    [InlineData("Intersects")]
    [InlineData("Contains")]
    [InlineData("Within")]
    [InlineData("Centroid")]
    [InlineData("Envelope")]
    [InlineData("Boundary")]
    [InlineData("ConvexHull")]
    [InlineData("Union")]
    [InlineData("Intersection")]
    [InlineData("Difference")]
    [InlineData("SymDifference")]
    public void EnableSqliteSpatialFunctions_WithValidFunctions_ShouldSucceed(string functionName)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        string[] functions = { functionName };

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteSpatialFunctions(functions);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", functions), Times.Once);
    }

    [Fact]
    public void EnableSqliteSpatialFunctions_WithMixedCaseFunctions_ShouldSucceed()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<ServiceArea>();
        string[] functions = { "distance", "AREA", "Contains" }; // Mixed case

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteSpatialFunctions(functions);

        // Assert - Should succeed because comparison is case-insensitive
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnabledSpatialFunctions", functions), Times.Once);
    }

    #endregion
}