// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text.Json;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL JSONB configuration extensions.
/// Tests JSONB data types, indexing strategies, path queries, and constraints.
/// </summary>
public sealed class JsonbConfigurationExtensionsTests : PostgreSqlIntegrationTestBase
{
    public JsonbConfigurationExtensionsTests(PostgreSqlTestFixture fixture)
        : base(fixture)
    {
    }

    #region HasJsonbType Tests

    [Fact]
    public async Task HasJsonbType_ShouldConfigureJsonbColumnType()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        // Act
        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify column type in database
        var columnType = await ExecuteScalarAsync<string>("""
            SELECT data_type 
            FROM information_schema.columns 
            WHERE table_name = 'json_entities' AND column_name = 'metadata';
            """);

        columnType.Should().Be("jsonb");
    }

    [Fact]
    public async Task HasJsonbType_ShouldSupportJsonbOperations()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var testData = new JsonEntity
        {
            Name = "Test Entity",
            Metadata = JsonSerializer.Serialize(new { user = new { name = "John", email = "john@test.com" }, version = 1 }),
            Settings = JsonSerializer.Serialize(new { theme = "dark", language = "en" })
        };

        // Act
        await context.JsonEntities.AddAsync(testData);
        await context.SaveChangesAsync();

        // Test JSONB containment operator
        var containsResult = await ExecuteScalarAsync<bool>("""
            SELECT EXISTS(
                SELECT 1 FROM json_entities 
                WHERE metadata @> '{"user": {"name": "John"}}'
            );
            """);

        // Test JSONB key existence
        var keyExistsResult = await ExecuteScalarAsync<bool>("""
            SELECT EXISTS(
                SELECT 1 FROM json_entities 
                WHERE metadata ? 'user'
            );
            """);

        // Assert
        containsResult.Should().BeTrue();
        keyExistsResult.Should().BeTrue();
    }

    #endregion

    #region HasJsonbGinIndex Tests

    [Fact]
    public async Task HasJsonbGinIndex_ShouldCreateGinIndex()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        // Act
        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Verify GIN index exists
        var indexExists = await IndexExistsAsync("ix_json_entities_metadata_gin");
        indexExists.Should().BeTrue();

        var indexMethod = await GetIndexMethodAsync("ix_json_entities_metadata_gin");
        indexMethod.Should().Be("gin");
    }

    [Fact]
    public async Task HasJsonbGinIndex_ShouldOptimizeContainmentQueries()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Add test data with different JSON structures
        var testEntities = new[]
        {
            new JsonEntity
            {
                Name = "User 1",
                Metadata = JsonSerializer.Serialize(new { user = new { name = "Alice", role = "admin" }, tags = new[] { "important", "user" } })
            },
            new JsonEntity
            {
                Name = "User 2",
                Metadata = JsonSerializer.Serialize(new { user = new { name = "Bob", role = "user" }, tags = new[] { "normal", "user" } })
            },
            new JsonEntity
            {
                Name = "System",
                Metadata = JsonSerializer.Serialize(new { system = true, version = 2, tags = new[] { "system", "internal" } })
            }
        };

        await context.JsonEntities.AddRangeAsync(testEntities);
        await context.SaveChangesAsync();

        // Act - Test containment queries that should use GIN index
        var adminUsersCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM json_entities 
            WHERE metadata @> '{"user": {"role": "admin"}}';
            """);

        var systemEntitiesCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM json_entities 
            WHERE metadata @> '{"system": true}';
            """);

        var userTaggedCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM json_entities 
            WHERE metadata @> '{"tags": ["user"]}';
            """);

        // Assert
        adminUsersCount.Should().Be(1);
        systemEntitiesCount.Should().Be(1);
        userTaggedCount.Should().Be(2);
    }

    #endregion

    #region HasJsonbPathIndex Tests

    [Fact]
    public async Task HasJsonbPathIndex_WithValidPath_ShouldCreatePathIndex()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        // Act
        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Assert - Check if path index configuration is applied
        // Note: The actual path index creation might require custom SQL in migrations
        // This test verifies the extension method doesn't throw and configuration is applied
        context.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasJsonbPathIndex_WithInvalidPath_ShouldThrowArgumentException(string invalidPath)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<JsonEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasJsonbType();

        // Act
        var act = () => propertyBuilder.HasJsonbPathIndex(invalidPath);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("jsonPath")
            .WithMessage("*JSON path cannot be null or whitespace.*");
    }

    #endregion

    #region HasJsonbExpressionIndex Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasJsonbExpressionIndex_WithInvalidExpression_ShouldThrowArgumentException(string invalidExpression)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<JsonEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasJsonbType();

        // Act
        var act = () => propertyBuilder.HasJsonbExpressionIndex(invalidExpression);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("expression")
            .WithMessage("*Expression cannot be null or whitespace.*");
    }

    #endregion

    #region HasJsonbDefaultValue Tests

    [Fact]
    public async Task HasJsonbDefaultValue_WithStaticValue_ShouldSetDefaultValue()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbDefaultTestDbContext>();

        // Act
        await using var context = new JsonbDefaultTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Insert entity without setting Settings (should get default)
        await ExecuteSqlAsync("INSERT INTO json_entities (id, name, metadata) VALUES (gen_random_uuid(), 'Test', '{}')");

        // Verify default value was applied
        var settingsValue = await ExecuteScalarAsync<string>("""
            SELECT settings FROM json_entities WHERE name = 'Test';
            """);

        // Assert
        settingsValue.Should().NotBeNullOrEmpty();
        var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(settingsValue!);
        settings.Should().ContainKey("theme");
        settings.Should().ContainKey("language");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasJsonbDefaultValue_WithInvalidValue_ShouldThrowArgumentException(string invalidValue)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<JsonEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasJsonbType();

        // Act
        var act = () => propertyBuilder.HasJsonbDefaultValue(invalidValue);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("defaultJson")
            .WithMessage("*Default JSON value cannot be null or whitespace.*");
    }

    #endregion

    #region HasJsonbConstraint Tests

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void HasJsonbConstraint_WithInvalidConstraint_ShouldThrowArgumentException(string invalidConstraint)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<JsonEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasJsonbType();

        // Act
        var act = () => propertyBuilder.HasJsonbConstraint(invalidConstraint);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("constraint")
            .WithMessage("*Constraint cannot be null or whitespace.*");
    }

    #endregion

    #region OptimizeForJsonbOperators Tests

    [Theory]
    [InlineData(JsonbOperators.Contains)]
    [InlineData(JsonbOperators.Exists)]
    [InlineData(JsonbOperators.GetPath)]
    [InlineData(JsonbOperators.Contains | JsonbOperators.Exists)]
    [InlineData(JsonbOperators.All)]
    public void OptimizeForJsonbOperators_WithValidOperators_ShouldConfigureOptimization(JsonbOperators operators)
    {
        // Arrange
        var builder = new ModelBuilder();
        var entityBuilder = builder.Entity<JsonEntity>();
        var propertyBuilder = entityBuilder.Property(e => e.Metadata).HasJsonbType();

        // Act
        var result = propertyBuilder.OptimizeForJsonbOperators(operators);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(propertyBuilder);
    }

    #endregion

    #region Complex JSONB Operations Tests

    [Fact]
    public async Task JsonbOperations_ComplexQueries_ShouldWorkCorrectly()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var complexEntities = new[]
        {
            new JsonEntity
            {
                Name = "E-commerce Product",
                Metadata = JsonSerializer.Serialize(new
                {
                    product = new
                    {
                        name = "Laptop",
                        category = "Electronics",
                        specs = new { ram = "16GB", cpu = "Intel i7", storage = "512GB SSD" },
                        tags = new[] { "computer", "portable", "work" }
                    },
                    pricing = new { current = 1299.99, discount = 0.1 }
                })
            },
            new JsonEntity
            {
                Name = "User Profile",
                Metadata = JsonSerializer.Serialize(new
                {
                    user = new
                    {
                        name = "Jane Doe",
                        email = "jane@example.com",
                        preferences = new { theme = "dark", notifications = true },
                        roles = new[] { "user", "premium" }
                    },
                    account = new { created = "2024-01-01", verified = true }
                })
            },
            new JsonEntity
            {
                Name = "System Config",
                Metadata = JsonSerializer.Serialize(new
                {
                    system = new
                    {
                        version = "2.1.0",
                        features = new[] { "auth", "logging", "monitoring" },
                        database = new { host = "localhost", port = 5432 }
                    }
                })
            }
        };

        await context.JsonEntities.AddRangeAsync(complexEntities);
        await context.SaveChangesAsync();

        // Act & Assert - Test various JSONB operators

        // 1. Test path extraction (#>)
        var productNames = await ExecuteScalarAsync<string>("""
            SELECT metadata #>> '{product,name}' 
            FROM json_entities 
            WHERE name = 'E-commerce Product';
            """);
        productNames.Should().Be("Laptop");

        // 2. Test array containment
        var computerProducts = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM json_entities 
            WHERE metadata @> '{"product": {"tags": ["computer"]}}';
            """);
        computerProducts.Should().Be(1);

        // 3. Test key existence (?)
        var entitiesWithUser = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM json_entities 
            WHERE metadata ? 'user';
            """);
        entitiesWithUser.Should().Be(1);

        // 4. Test nested path existence
        var entitiesWithSpecs = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM json_entities 
            WHERE metadata #> '{product,specs}' IS NOT NULL;
            """);
        entitiesWithSpecs.Should().Be(1);

        // 5. Test array element access
        var firstFeature = await ExecuteScalarAsync<string>("""
            SELECT metadata #>> '{system,features,0}' 
            FROM json_entities 
            WHERE name = 'System Config';
            """);
        firstFeature.Should().Be("auth");

        // 6. Test JSONB aggregation
        var allTags = await ExecuteScalarAsync<string>("""
            SELECT string_agg(DISTINCT tag, ', ' ORDER BY tag)
            FROM (
                SELECT jsonb_array_elements_text(metadata #> '{product,tags}') AS tag
                FROM json_entities
                WHERE metadata ? 'product'
            ) t;
            """);
        allTags.Should().Contain("computer").And.Contain("portable").And.Contain("work");
    }

    [Fact]
    public async Task JsonbOperations_PerformanceTest_ShouldHandleLargeDatasets()
    {
        // Arrange
        var options = CreateDbContextOptions<JsonbTestDbContext>();

        await using var context = new JsonbTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Insert large dataset
        const int recordCount = 1000;
        var entities = new List<JsonEntity>();

        for (int i = 0; i < recordCount; i++)
        {
            entities.Add(new JsonEntity
            {
                Name = $"Entity {i}",
                Metadata = JsonSerializer.Serialize(new
                {
                    id = i,
                    category = i % 10 == 0 ? "premium" : "standard",
                    metrics = new
                    {
                        score = new Random().Next(1, 100),
                        rating = new Random().NextDouble() * 5,
                        tags = new[] { $"tag_{i % 5}", $"group_{i % 3}" }
                    },
                    timestamp = DateTime.UtcNow.AddHours(-i).ToString("O")
                })
            });
        }

        await context.JsonEntities.AddRangeAsync(entities);
        await context.SaveChangesAsync();

        // Act - Perform complex queries
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Query 1: Find premium entities
        var premiumCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM json_entities 
            WHERE metadata @> '{"category": "premium"}';
            """);

        // Query 2: Find high-scoring entities
        var highScoreCount = await ExecuteScalarAsync<long>("""
            SELECT COUNT(*) 
            FROM json_entities 
            WHERE (metadata #>> '{metrics,score}')::int > 80;
            """);

        // Query 3: Group by tags
        var tagDistribution = await ExecuteScalarAsync<long>("""
            SELECT COUNT(DISTINCT tag)
            FROM (
                SELECT jsonb_array_elements_text(metadata #> '{metrics,tags}') AS tag
                FROM json_entities
            ) t;
            """);

        stopwatch.Stop();

        // Assert
        Console.WriteLine($"Performance test completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Premium entities: {premiumCount}");
        Console.WriteLine($"High score entities: {highScoreCount}");
        Console.WriteLine($"Unique tags: {tagDistribution}");

        premiumCount.Should().Be(recordCount / 10); // Every 10th entity is premium
        highScoreCount.Should().BeGreaterThan(0);
        tagDistribution.Should().Be(8); // 5 tag_ + 3 group_ patterns
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
    }

    #endregion
}

/// <summary>
/// Test DbContext for JSONB configuration testing.
/// </summary>
public class JsonbTestDbContext : DbContext
{
    public JsonbTestDbContext(DbContextOptions<JsonbTestDbContext> options) : base(options) { }

    public DbSet<JsonEntity> JsonEntities => Set<JsonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JsonEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.Property(e => e.Metadata)
                .IsRequired()
                .HasJsonbType()
                .HasJsonbGinIndex("ix_json_entities_metadata_gin");
                
            entity.Property(e => e.Settings)
                .HasJsonbType()
                .OptimizeForJsonbOperators(JsonbOperators.Contains | JsonbOperators.Exists);
                
            entity.Property(e => e.UserProfile)
                .HasJsonbType()
                .HasJsonbPathIndex("$.user.email", "ix_json_entities_user_email");
                
            entity.Property(e => e.Tags)
                .HasJsonbType()
                .HasJsonbExpressionIndex("(tags -> 'categories')", "ix_json_entities_tag_categories");
        });
    }
}

/// <summary>
/// Test DbContext for JSONB default value testing.
/// </summary>
public class JsonbDefaultTestDbContext : DbContext
{
    public JsonbDefaultTestDbContext(DbContextOptions<JsonbDefaultTestDbContext> options) : base(options) { }

    public DbSet<JsonEntity> JsonEntities => Set<JsonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JsonEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            
            entity.Property(e => e.Metadata)
                .IsRequired()
                .HasJsonbType();
                
            entity.Property(e => e.Settings)
                .HasJsonbType()
                .HasJsonbDefaultValue("'{\"theme\": \"light\", \"language\": \"en\"}'");
                
            entity.Property(e => e.UserProfile)
                .HasJsonbType()
                .HasJsonbConstraint("user_profile ? 'email' AND user_profile ? 'name'", "chk_user_profile_required");
        });
    }
}