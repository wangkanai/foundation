// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL array configuration extensions.
/// Tests array data types, indexing strategies, constraints, and array operations.
/// </summary>
public sealed class ArrayConfigurationExtensionsTests : PostgreSqlIntegrationTestBase
{
    public ArrayConfigurationExtensionsTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    #region HasArrayType Tests

    [Fact]
    public async Task HasArrayType_WithInferredType_ShouldConfigureArrayColumn()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        // Act
        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify array column types in database
        var scoresColumnType = await ExecuteScalarAsync<string>("""
            SELECT data_type 
            FROM information_schema.columns 
            WHERE table_name = 'array_entities' AND column_name = 'scores';
            """);
        
        var tagsColumnType = await ExecuteScalarAsync<string>("""
            SELECT data_type 
            FROM information_schema.columns 
            WHERE table_name = 'array_entities' AND column_name = 'tags';
            """);

        scoresColumnType.Should().Be("ARRAY");
        tagsColumnType.Should().Be("ARRAY");
    }

    [Fact]
    public async Task HasArrayType_WithExplicitType_ShouldConfigureArrayColumn()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var testEntity = new ArrayEntity
        {
            Name = "Array Test",
            Scores = [85, 92, 78, 96, 88],
            Tags = ["important", "test", "array"],
            Prices = [19.99m, 25.50m, 12.75m]
        };

        // Act
        await context.ArrayEntities.AddAsync(testEntity);
        await context.SaveChangesAsync();

        // Assert
        var retrievedEntity = await context.ArrayEntities
            .FirstOrDefaultAsync(e => e.Name == "Array Test");

        retrievedEntity.Should().NotBeNull();
        retrievedEntity!.Scores.Should().BeEquivalentTo(new[] { 85, 92, 78, 96, 88 });
        retrievedEntity.Tags.Should().BeEquivalentTo(new[] { "important", "test", "array" });
        retrievedEntity.Prices.Should().BeEquivalentTo(new[] { 19.99m, 25.50m, 12.75m });
    }

    #endregion

    #region HasMultiDimensionalArray Tests

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void HasMultiDimensionalArray_WithValidDimensions_ShouldConfigureArray(int dimensions)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Scores);

        // Act
        var result = propertyBuilder.HasMultiDimensionalArray(dimensions, "integer");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void HasMultiDimensionalArray_WithInvalidDimensions_ShouldThrowArgumentException(int invalidDimensions)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Scores);

        // Act
        var act = () => propertyBuilder.HasMultiDimensionalArray(invalidDimensions, "integer");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("dimensions")
            .WithMessage("*Dimensions must be at least 1.*");
    }

    #endregion

    #region Array Index Tests

    [Fact]
    public async Task HasArrayGinIndex_ShouldCreateGinIndex()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        // Act
        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify GIN index exists
        var indexExists = await IndexExistsAsync("ix_array_entities_tags_gin");
        indexExists.Should().BeTrue();

        var indexMethod = await GetIndexMethodAsync("ix_array_entities_tags_gin");
        indexMethod.Should().Be("gin");
    }

    [Fact]
    public async Task HasArrayGistIndex_ShouldCreateGistIndex()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        // Act
        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify GiST index exists  
        var indexExists = await IndexExistsAsync("ix_array_entities_scores_gist");
        indexExists.Should().BeTrue();

        var indexMethod = await GetIndexMethodAsync("ix_array_entities_scores_gist");
        indexMethod.Should().Be("gist");
    }

    [Fact]
    public async Task ArrayIndexes_ShouldOptimizeArrayQueries()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Add test data
        var entities = new[]
        {
            new ArrayEntity
            {
                Name = "Student 1",
                Scores = [85, 92, 78, 96],
                Tags = ["math", "science", "excellent"],
                Prices = [10.50m, 15.75m]
            },
            new ArrayEntity
            {
                Name = "Student 2", 
                Scores = [76, 88, 82, 90],
                Tags = ["history", "science", "good"],
                Prices = [12.00m, 18.25m]
            },
            new ArrayEntity
            {
                Name = "Student 3",
                Scores = [92, 95, 89, 97],
                Tags = ["math", "physics", "excellent"],
                Prices = [14.75m, 22.50m]
            }
        };

        await context.ArrayEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act & Assert - Test array containment queries

        // Test @> (contains) operator
        var mathStudents = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE tags @> ARRAY['math'];
            """);
        mathStudents.Should().Be(2);

        // Test && (overlap) operator
        var scienceOrMath = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE tags && ARRAY['science', 'math'];
            """);
        scienceOrMath.Should().Be(3);

        // Test ANY operator
        var excellentStudents = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE 'excellent' = ANY(tags);
            """);
        excellentStudents.Should().Be(2);

        // Test numeric array queries
        var highScorers = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE scores @> ARRAY[95];
            """);
        highScorers.Should().Be(1);
    }

    #endregion

    #region Array Constraints Tests

    [Theory]
    [InlineData(5, 1)]
    [InlineData(10, 2)]
    [InlineData(3, 0)]
    public void HasArrayConstraints_WithValidParameters_ShouldConfigureConstraints(int maxLength, int minLength)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var result = propertyBuilder.HasArrayConstraints(maxLength: maxLength, minLength: minLength, allowNulls: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    [Fact]
    public void HasArrayConstraints_WithNegativeMinLength_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var act = () => propertyBuilder.HasArrayConstraints(minLength: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("minLength")
            .WithMessage("*Minimum length cannot be negative.*");
    }

    [Fact]
    public void HasArrayConstraints_WithMinGreaterThanMax_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var act = () => propertyBuilder.HasArrayConstraints(maxLength: 5, minLength: 10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("minLength")
            .WithMessage("*Minimum length cannot be greater than maximum length.*");
    }

    #endregion

    #region Array Default Value Tests

    [Theory]
    [InlineData("ARRAY['default', 'new']")]
    [InlineData("'{}'::text[]")]
    [InlineData("ARRAY[1, 2, 3]")]
    public void HasArrayDefaultValue_WithValidValue_ShouldConfigureDefault(string defaultValue)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var result = propertyBuilder.HasArrayDefaultValue(defaultValue);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasArrayDefaultValue_WithInvalidValue_ShouldThrowArgumentException(string invalidValue)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var act = () => propertyBuilder.HasArrayDefaultValue(invalidValue);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("defaultArray")
            .WithMessage("*Default array value cannot be null or whitespace.*");
    }

    #endregion

    #region Array Operators Optimization Tests

    [Theory]
    [InlineData(ArrayOperators.Contains)]
    [InlineData(ArrayOperators.Overlap)]
    [InlineData(ArrayOperators.Any)]
    [InlineData(ArrayOperators.Contains | ArrayOperators.Overlap)]
    [InlineData(ArrayOperators.Common)]
    public void OptimizeForArrayOperators_WithValidOperators_ShouldConfigureOptimization(ArrayOperators operators)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Tags).HasArrayType("text");

        // Act
        var result = propertyBuilder.OptimizeForArrayOperators(operators);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    #endregion

    #region Array Aggregation Tests

    [Theory]
    [InlineData(ArrayAggregationFunctions.ArrayAgg)]
    [InlineData(ArrayAggregationFunctions.Unnest)]
    [InlineData(ArrayAggregationFunctions.ArrayLength)]
    [InlineData(ArrayAggregationFunctions.ArrayAgg | ArrayAggregationFunctions.Unnest)]
    [InlineData(ArrayAggregationFunctions.All)]
    public void EnableArrayAggregation_WithValidFunctions_ShouldConfigureAggregation(ArrayAggregationFunctions functions)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Scores).HasArrayType("integer");

        // Act
        var result = propertyBuilder.EnableArrayAggregation(functions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    [Fact]
    public async Task ArrayAggregation_Functions_ShouldWorkCorrectly()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var entities = new[]
        {
            new ArrayEntity { Name = "Student 1", Scores = [85, 90, 78] },
            new ArrayEntity { Name = "Student 2", Scores = [92, 88, 95] },
            new ArrayEntity { Name = "Student 3", Scores = [76, 82, 89] }
        };

        await context.ArrayEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act & Assert - Test array aggregation functions

        // Test array_length
        var arrayLengths = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE array_length(scores, 1) = 3;
            """);
        arrayLengths.Should().Be(3);

        // Test unnest
        var allScores = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM (SELECT unnest(scores) FROM array_entities) t;
            """);
        allScores.Should().Be(9); // 3 entities × 3 scores each

        // Test array_agg (recreate arrays from unnested values)
        var aggregatedCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM (
                SELECT id, array_agg(score ORDER BY score) 
                FROM (
                    SELECT id, unnest(scores) as score 
                    FROM array_entities
                ) t 
                GROUP BY id
            ) agg;
            """);
        aggregatedCount.Should().Be(3);

        // Test array_position
        var positionResults = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE array_position(scores, 90) IS NOT NULL;
            """);
        positionResults.Should().Be(1);
    }

    #endregion

    #region HasTypedArray Tests

    [Theory]
    [InlineData("integer")]
    [InlineData("text")]
    [InlineData("uuid")]
    [InlineData("decimal")]
    public void HasTypedArray_WithValidType_ShouldConfigureTypedArray(string pgTypeName)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Scores);

        // Act
        var result = propertyBuilder.HasTypedArray<int>(pgTypeName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasTypedArray_WithInvalidType_ShouldThrowArgumentException(string invalidType)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<ArrayEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Scores);

        // Act
        var act = () => propertyBuilder.HasTypedArray<int>(invalidType);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("pgTypeName")
            .WithMessage("*PostgreSQL type name cannot be null or whitespace.*");
    }

    #endregion

    #region Complex Array Operations Tests

    [Fact]
    public async Task ComplexArrayOperations_ShouldWorkCorrectly()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var complexEntities = new[]
        {
            new ArrayEntity
            {
                Name = "Product A",
                Scores = [4, 5, 3, 4, 5],
                Tags = ["electronics", "popular", "new", "featured"],
                Prices = [299.99m, 249.99m, 199.99m],
                RelatedIds = [Guid.NewGuid(), Guid.NewGuid()],
                ImportantDates = [DateTime.Today, DateTime.Today.AddDays(30)]
            },
            new ArrayEntity
            {
                Name = "Product B", 
                Scores = [3, 4, 4, 3, 2],
                Tags = ["books", "education", "featured"],
                Prices = [19.99m, 24.99m],
                RelatedIds = [Guid.NewGuid()],
                ImportantDates = [DateTime.Today.AddDays(7)]
            },
            new ArrayEntity
            {
                Name = "Product C",
                Scores = [5, 5, 4, 5, 5],
                Tags = ["electronics", "premium", "featured", "bestseller"],
                Prices = [599.99m, 549.99m, 499.99m, 449.99m],
                RelatedIds = [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()],
                ImportantDates = [DateTime.Today, DateTime.Today.AddDays(15), DateTime.Today.AddDays(45)]
            }
        };

        await context.ArrayEntities.AddRangeAsync(complexEntities);
        await context.SaveChangesAsync();

        // Act & Assert - Test complex array operations

        // 1. Find products with high ratings (score 5)
        var premiumProducts = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE scores @> ARRAY[5];
            """);
        premiumProducts.Should().Be(2);

        // 2. Find featured products
        var featuredProducts = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE 'featured' = ANY(tags);
            """);
        featuredProducts.Should().Be(3);

        // 3. Find products with multiple price points
        var multiPriceProducts = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE array_length(prices, 1) > 2;
            """);
        multiPriceProducts.Should().Be(2);

        // 4. Calculate average number of tags
        var avgTagCount = await ExecuteScalarAsync<decimal>("""
            SELECT AVG(array_length(tags, 1)) 
            FROM array_entities;
            """);
        avgTagCount.Should().BeGreaterThan(3);

        // 5. Find products with overlapping categories
        var electronicsProducts = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE tags && ARRAY['electronics'];
            """);
        electronicsProducts.Should().Be(2);

        // 6. Test array concatenation and manipulation
        var concatenatedTags = await ExecuteScalarAsync<string>("""
            SELECT array_to_string(array_agg(DISTINCT tag), ', ')
            FROM (
                SELECT unnest(tags) AS tag 
                FROM array_entities
            ) t
            ORDER BY array_to_string(array_agg(DISTINCT tag), ', ');
            """);
        concatenatedTags.Should().NotBeNullOrEmpty();

        // 7. Test array slicing and element access
        var firstScores = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE scores[1] >= 4;
            """);
        firstScores.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ArrayOperations_PerformanceTest_ShouldHandleLargeArrays()
    {
        // Skip if Docker/Podman is not available
        if (!IsDockerAvailable)
        {
            Assert.True(true, "Skipping test - Docker/Podman is not available.");
            return;
        }
        
        // Arrange
        var options = CreateDbContextOptions<ArrayTestDbContext>();

        await using var context = new ArrayTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Create large dataset with various array sizes
        const int recordCount = 500;
        var entities = new List<ArrayEntity>();
        var random = new Random(42); // Fixed seed for reproducible tests

        for (int i = 0; i < recordCount; i++)
        {
            var scoreCount = random.Next(5, 20);
            var tagCount = random.Next(3, 10);
            var priceCount = random.Next(1, 5);

            entities.Add(new ArrayEntity
            {
                Name = $"Entity {i}",
                Scores = Enumerable.Range(0, scoreCount).Select(_ => random.Next(1, 100)).ToArray(),
                Tags = Enumerable.Range(0, tagCount).Select(j => $"tag_{j % 20}").Distinct().ToArray(),
                Prices = Enumerable.Range(0, priceCount).Select(_ => (decimal)(random.NextDouble() * 1000)).ToArray(),
                RelatedIds = Enumerable.Range(0, random.Next(1, 4)).Select(_ => Guid.NewGuid()).ToArray(),
                ImportantDates = [DateTime.Today.AddDays(random.Next(-30, 30))]
            });
        }

        await context.ArrayEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act - Perform performance test queries
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Query 1: Array containment
        var highScorers = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE scores @> ARRAY[90];
            """);

        // Query 2: Array overlap
        var commonTags = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM array_entities 
            WHERE tags && ARRAY['tag_1', 'tag_5', 'tag_10'];
            """);

        // Query 3: Array aggregation
        var totalElements = await ExecuteScalarAsync<long>("""
            SELECT SUM(array_length(scores, 1)) 
            FROM array_entities;
            """);

        // Query 4: Complex array analysis
        var avgArraySize = await ExecuteScalarAsync<decimal>("""
            SELECT AVG(array_length(tags, 1)) 
            FROM array_entities 
            WHERE array_length(scores, 1) > 10;
            """);

        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Array performance test completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"High scorers: {highScorers}");
        Console.WriteLine($"Common tags: {commonTags}");
        Console.WriteLine($"Total elements: {totalElements}");
        Console.WriteLine($"Average array size: {avgArraySize}");

        highScorers.Should().BeGreaterThanOrEqualTo(0);
        commonTags.Should().BeGreaterThan(0);
        totalElements.Should().BeGreaterThan(recordCount * 5); // At least 5 scores per entity
        avgArraySize.Should().BeGreaterThan(3);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Should complete within 10 seconds
    }

    #endregion
}

/// <summary>
/// Test DbContext for array configuration testing.
/// </summary>
public class ArrayTestDbContext : DbContext
{
    public ArrayTestDbContext(DbContextOptions<ArrayTestDbContext> options) : base(options) { }

    public DbSet<ArrayEntity> ArrayEntities => Set<ArrayEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArrayEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.Property(e => e.Scores)
                .HasArrayType("integer")
                .HasArrayGistIndex("ix_array_entities_scores_gist")
                .OptimizeForArrayOperators(ArrayOperators.Contains | ArrayOperators.Any)
                .EnableArrayAggregation(ArrayAggregationFunctions.ArrayAgg | ArrayAggregationFunctions.Unnest);
                
            entity.Property(e => e.Tags)
                .IsRequired()
                .HasArrayType("text")
                .HasArrayGinIndex("ix_array_entities_tags_gin")
                .HasArrayConstraints(maxLength: 20, minLength: 1, allowNulls: false)
                .OptimizeForArrayOperators(ArrayOperators.Contains | ArrayOperators.Overlap | ArrayOperators.Any);
                
            entity.Property(e => e.RelatedIds)
                .HasTypedArray<Guid>("uuid")
                .HasArrayDefaultValue("'{}'::uuid[]");
                
            entity.Property(e => e.Prices)
                .HasArrayType("decimal")
                .HasArrayConstraints(maxLength: 10, minLength: 0, allowNulls: false);
                
            entity.Property(e => e.ImportantDates)
                .HasArrayType("timestamp")
                .EnableArrayAggregation(ArrayAggregationFunctions.ArrayLength | ArrayAggregationFunctions.ArrayPosition);
        });
    }
}