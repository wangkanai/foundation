// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL connection configuration extensions.
/// Tests argument validation for connection pooling, SSL, prepared statements, multiplexing, and timeouts.
/// </summary>
public sealed class ConnectionConfigurationExtensionsTests
{
   #region Multiplexing Tests

   [Fact]
   public void EnableNpgsqlMultiplexing_ShouldEnableMultiplexing()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.EnableNpgsqlMultiplexing();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   #endregion

   #region Connection Pool Tests

   [Fact]
   public void ConfigureNpgsqlConnectionPool_WithValidParameters_ShouldConfigurePool()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.ConfigureNpgsqlConnectionPool(5, 20);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void ConfigureNpgsqlConnectionPool_WithNegativeMinPoolSize_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.ConfigureNpgsqlConnectionPool(-1, 10);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("minPoolSize")
         .WithMessage("*Minimum pool size cannot be negative.*");
   }

   [Fact]
   public void ConfigureNpgsqlConnectionPool_WithMaxPoolSizeLessThanMin_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.ConfigureNpgsqlConnectionPool(10, 5);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("maxPoolSize")
         .WithMessage("*Maximum pool size must be greater than or equal to minimum pool size.*");
   }

   [Fact]
   public void ConfigureNpgsqlConnectionPool_WithDefaultParameters_ShouldWork()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.ConfigureNpgsqlConnectionPool();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   #endregion

   #region Prepared Statements Tests

   [Fact]
   public void EnableNpgsqlPreparedStatements_WithValidParameters_ShouldEnablePreparedStatements()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.EnableNpgsqlPreparedStatements(10);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void EnableNpgsqlPreparedStatements_WithNegativeMaxAutoPrepare_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.EnableNpgsqlPreparedStatements(-1);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("maxAutoPrepare")
         .WithMessage("*Maximum auto prepare count cannot be negative.*");
   }

   [Fact]
   public void EnableNpgsqlPreparedStatements_WithDefaultParameters_ShouldWork()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.EnableNpgsqlPreparedStatements();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   #endregion

   #region SSL Configuration Tests

   [Theory]
   [InlineData(SslMode.Disable)]
   [InlineData(SslMode.Allow)]
   [InlineData(SslMode.Prefer)]
   [InlineData(SslMode.Require)]
   public void RequireNpgsqlSSL_WithDifferentModes_ShouldConfigureSSL(SslMode sslMode)
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.RequireNpgsqlSSL(sslMode);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void RequireNpgsqlSSL_WithDefaultMode_ShouldWork()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.RequireNpgsqlSSL();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   #endregion

   #region Timeout Configuration Tests

   [Theory]
   [InlineData(5)]
   [InlineData(30)]
   [InlineData(60)]
   [InlineData(300)]
   public void SetNpgsqlStatementTimeout_WithValidTimeout_ShouldConfigureTimeout(int timeoutSeconds)
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");
      var timeout = TimeSpan.FromSeconds(timeoutSeconds);

      // Act
      var result = builder.SetNpgsqlStatementTimeout(timeout);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void SetNpgsqlStatementTimeout_WithZeroTimeout_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.SetNpgsqlStatementTimeout(TimeSpan.Zero);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("timeout")
         .WithMessage("*Statement timeout must be greater than zero.*");
   }

   [Fact]
   public void SetNpgsqlStatementTimeout_WithNegativeTimeout_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.SetNpgsqlStatementTimeout(TimeSpan.FromSeconds(-1));

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("timeout")
         .WithMessage("*Statement timeout must be greater than zero.*");
   }

   [Theory]
   [InlineData(5)]
   [InlineData(15)]
   [InlineData(30)]
   [InlineData(60)]
   public void SetNpgsqlConnectionTimeout_WithValidTimeout_ShouldConfigureTimeout(int timeoutSeconds)
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");
      var timeout = TimeSpan.FromSeconds(timeoutSeconds);

      // Act
      var result = builder.SetNpgsqlConnectionTimeout(timeout);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void SetNpgsqlConnectionTimeout_WithZeroTimeout_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.SetNpgsqlConnectionTimeout(TimeSpan.Zero);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("timeout")
         .WithMessage("*Connection timeout must be greater than zero.*");
   }

   #endregion

   #region Performance Configuration Tests

   [Fact]
   public void ConfigureNpgsqlPerformance_WithAllParameters_ShouldConfigurePerformance()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.ConfigureNpgsqlPerformance(
                                                      2,
                                                      10,
                                                      15,
                                                      TimeSpan.FromMinutes(1),
                                                      TimeSpan.FromSeconds(10),
                                                      true,
                                                      SslMode.Prefer);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void ConfigureNpgsqlPerformance_WithDefaultParameters_ShouldWork()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var result = builder.ConfigureNpgsqlPerformance();

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(builder);
   }

   [Fact]
   public void ConfigureNpgsqlPerformance_WithInvalidPoolSizes_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.ConfigureNpgsqlPerformance(10, 5);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("maxPoolSize")
         .WithMessage("*Maximum pool size must be greater than or equal to minimum pool size.*");
   }

   [Fact]
   public void ConfigureNpgsqlPerformance_WithNegativeMaxAutoPrepare_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.ConfigureNpgsqlPerformance(maxAutoPrepare: -1);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("maxAutoPrepare")
         .WithMessage("*Maximum auto prepare count cannot be negative.*");
   }

   [Fact]
   public void ConfigureNpgsqlPerformance_WithInvalidCommandTimeout_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql("Server=localhost;Database=test;User Id=test;Password=test;");

      // Act
      var act = () => builder.ConfigureNpgsqlPerformance(commandTimeout: TimeSpan.Zero);

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("commandTimeout")
         .WithMessage("*Statement timeout must be greater than zero.*");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class TestEntity
{
   public int      Id          { get; set; }
   public string   Name        { get; set; } = string.Empty;
   public DateTime CreatedAt   { get; set; }
   public string?  Description { get; set; }
}

/// <summary>
/// Test DbContext for connection configuration testing.
/// </summary>
public class TestDbContext : DbContext
{
   public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

   public DbSet<TestEntity> TestEntities => Set<TestEntity>();

   protected override void OnModelCreating(ModelBuilder modelBuilder) =>
      modelBuilder.Entity<TestEntity>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
         entity.Property(e => e.CreatedAt).IsRequired();
         entity.Property(e => e.Description).HasMaxLength(500);

         // Add index for performance testing
         entity.HasIndex(e => e.Name);
         entity.HasIndex(e => e.CreatedAt);
      });
}