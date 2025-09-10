// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class ConnectionConfigurationExtensionsTests
{
    private const string ValidConnectionString = "Data Source=test.db";
    
    #region Test Context Classes
    
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }
    
    #endregion

    #region EnableSqliteWAL Tests

    [Fact]
    public void EnableSqliteWAL_Generic_WithValidConnectionString_ShouldConfigureWAL()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.EnableSqliteWAL(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
        var options = result.Options;
        Assert.NotNull(options);
    }

    [Fact]
    public void EnableSqliteWAL_Generic_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => optionsBuilder.EnableSqliteWAL(null!));
    }

    [Fact]
    public void EnableSqliteWAL_Generic_WithEmptyConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => optionsBuilder.EnableSqliteWAL(string.Empty));
    }

    [Fact]
    public void EnableSqliteWAL_NonGeneric_WithValidConnectionString_ShouldConfigureWAL()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act
        var result = optionsBuilder.EnableSqliteWAL(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
        var options = result.Options;
        Assert.NotNull(options);
    }

    [Fact]
    public void EnableSqliteWAL_NonGeneric_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => optionsBuilder.EnableSqliteWAL(null!));
    }

    #endregion

    #region SetSqliteCacheSize Tests

    [Fact]
    public void SetSqliteCacheSize_Generic_WithValidParameters_ShouldSetCacheSize()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        const int cacheSize = 32768;

        // Act
        var result = optionsBuilder.SetSqliteCacheSize(ValidConnectionString, cacheSize);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void SetSqliteCacheSize_Generic_WithDefaultCacheSize_ShouldUseDefault()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.SetSqliteCacheSize(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void SetSqliteCacheSize_Generic_WithZeroCacheSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            optionsBuilder.SetSqliteCacheSize(ValidConnectionString, 0));
    }

    [Fact]
    public void SetSqliteCacheSize_Generic_WithNegativeCacheSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            optionsBuilder.SetSqliteCacheSize(ValidConnectionString, -1));
    }

    [Fact]
    public void SetSqliteCacheSize_NonGeneric_WithValidParameters_ShouldSetCacheSize()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();
        const int cacheSize = 32768;

        // Act
        var result = optionsBuilder.SetSqliteCacheSize(ValidConnectionString, cacheSize);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    #endregion

    #region SetSqliteBusyTimeout Tests

    [Fact]
    public void SetSqliteBusyTimeout_Generic_WithValidTimeout_ShouldSetTimeout()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        const int timeout = 15000;

        // Act
        var result = optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString, timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void SetSqliteBusyTimeout_Generic_WithDefaultTimeout_ShouldUseDefault()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void SetSqliteBusyTimeout_Generic_WithNegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString, -1));
    }

    [Fact]
    public void SetSqliteBusyTimeout_NonGeneric_WithValidTimeout_ShouldSetTimeout()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();
        const int timeout = 15000;

        // Act
        var result = optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString, timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    #endregion

    #region EnableSqliteForeignKeys Tests

    [Fact]
    public void EnableSqliteForeignKeys_Generic_WithValidConnectionString_ShouldEnableForeignKeys()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.EnableSqliteForeignKeys(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void EnableSqliteForeignKeys_Generic_WithNullConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => optionsBuilder.EnableSqliteForeignKeys(null!));
    }

    [Fact]
    public void EnableSqliteForeignKeys_NonGeneric_WithValidConnectionString_ShouldEnableForeignKeys()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act
        var result = optionsBuilder.EnableSqliteForeignKeys(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    #endregion

    #region OptimizeForSqlitePerformance Tests

    [Fact]
    public void OptimizeForSqlitePerformance_Generic_WithValidParameters_ShouldApplyOptimizations()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        const int cacheSize = 32768;
        const int timeout = 15000;

        // Act
        var result = optionsBuilder.OptimizeForSqlitePerformance(ValidConnectionString, cacheSize, timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void OptimizeForSqlitePerformance_Generic_WithDefaultParameters_ShouldUseDefaults()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.OptimizeForSqlitePerformance(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void OptimizeForSqlitePerformance_Generic_WithInvalidCacheSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            optionsBuilder.OptimizeForSqlitePerformance(ValidConnectionString, 0));
    }

    [Fact]
    public void OptimizeForSqlitePerformance_Generic_WithInvalidTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            optionsBuilder.OptimizeForSqlitePerformance(ValidConnectionString, 65536, -1));
    }

    [Fact]
    public void OptimizeForSqlitePerformance_NonGeneric_WithValidParameters_ShouldApplyOptimizations()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();
        const int cacheSize = 32768;
        const int timeout = 15000;

        // Act
        var result = optionsBuilder.OptimizeForSqlitePerformance(ValidConnectionString, cacheSize, timeout);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    #endregion

    #region Connection String Building Tests

    [Theory]
    [InlineData("Data Source=test.db")]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=/path/to/database.sqlite")]
    public void ConnectionStringMethods_WithVariousValidConnectionStrings_ShouldSucceed(string connectionString)
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert - Should not throw
        var walResult = optionsBuilder.EnableSqliteWAL(connectionString);
        Assert.NotNull(walResult);

        var cacheResult = optionsBuilder.SetSqliteCacheSize(connectionString, 32768);
        Assert.NotNull(cacheResult);

        var timeoutResult = optionsBuilder.SetSqliteBusyTimeout(connectionString, 15000);
        Assert.NotNull(timeoutResult);

        var foreignKeyResult = optionsBuilder.EnableSqliteForeignKeys(connectionString);
        Assert.NotNull(foreignKeyResult);

        var optimizeResult = optionsBuilder.OptimizeForSqlitePerformance(connectionString);
        Assert.NotNull(optimizeResult);
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void SqliteConfigurationMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder
            .EnableSqliteWAL(ValidConnectionString);

        // Assert
        Assert.NotNull(result);
        Assert.Same(optionsBuilder, result);
    }

    [Fact]
    public void MultipleConfigurationMethods_ShouldWorkTogether()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert - Should not throw when called in sequence
        optionsBuilder.EnableSqliteWAL(ValidConnectionString);
        optionsBuilder.SetSqliteCacheSize(ValidConnectionString, 32768);
        optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString, 15000);
        optionsBuilder.EnableSqliteForeignKeys(ValidConnectionString);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EnableSqliteWAL_WithWhitespaceConnectionString_ShouldThrowArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => optionsBuilder.EnableSqliteWAL("   "));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(1048576)] // 1MB
    public void SetSqliteCacheSize_WithValidCacheSizes_ShouldSucceed(int cacheSize)
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.SetSqliteCacheSize(ValidConnectionString, cacheSize);

        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(30000)]
    [InlineData(60000)]
    public void SetSqliteBusyTimeout_WithValidTimeouts_ShouldSucceed(int timeout)
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        // Act
        var result = optionsBuilder.SetSqliteBusyTimeout(ValidConnectionString, timeout);

        // Assert
        Assert.NotNull(result);
    }

    #endregion
}