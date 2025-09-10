// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class BulkConfigurationExtensionsTests
{
    #region Test Entity Classes

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
    }

    public class Inventory
    {
        public int Id { get; set; }
        public int Stock { get; set; }
        public string Location { get; set; } = string.Empty;
    }

    public class LogEntry
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    #endregion

    #region Mock Setup Helpers

    private static Mock<EntityTypeBuilder<T>> CreateMockEntityTypeBuilder<T>() where T : class
    {
        var mockEntityBuilder = new Mock<EntityTypeBuilder<T>>();
        
        mockEntityBuilder.Setup(x => x.HasAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                        .Returns(mockEntityBuilder.Object);

        return mockEntityBuilder;
    }

    #endregion

    #region OptimizeForSqliteBulkInserts Tests

    [Fact]
    public void OptimizeForSqliteBulkInserts_WithDefaultParameters_ShouldApplyDefaultOptimizations()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkInserts();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify default bulk insert settings
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsert", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsertBatchSize", 1000), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeSequentialInserts", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:SynchronousMode", "NORMAL"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:JournalMode", "WAL"), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteBulkInserts_WithCustomBatchSize_ShouldSetBatchSize()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();
        const int customBatchSize = 500;

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkInserts(customBatchSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsertBatchSize", customBatchSize), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void OptimizeForSqliteBulkInserts_WithInvalidBatchSize_ShouldThrowArgumentOutOfRangeException(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteBulkInserts(batchSize));
    }

    [Fact]
    public void OptimizeForSqliteBulkInserts_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<Product> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.OptimizeForSqliteBulkInserts());
    }

    #endregion

    #region EnableSqliteBulkTransactions Tests

    [Fact]
    public void EnableSqliteBulkTransactions_WithDefaultParameters_ShouldApplyDefaultSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteBulkTransactions();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify default transaction settings
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkTransactionIsolation", "ReadCommitted"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkCommandTimeout", 300), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableDeferredConstraints", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkTransactionMode", "IMMEDIATE"), Times.Once);
    }

    [Fact]
    public void EnableSqliteBulkTransactions_WithCustomIsolationLevel_ShouldSetIsolationLevel()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();
        const IsolationLevel isolationLevel = IsolationLevel.Serializable;

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteBulkTransactions(isolationLevel);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkTransactionIsolation", "Serializable"), Times.Once);
    }

    [Fact]
    public void EnableSqliteBulkTransactions_WithCustomTimeout_ShouldSetTimeout()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();
        const int customTimeout = 600;

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteBulkTransactions(commandTimeout: customTimeout);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkCommandTimeout", customTimeout), Times.Once);
    }

    [Fact]
    public void EnableSqliteBulkTransactions_WithNegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockEntityBuilder.Object.EnableSqliteBulkTransactions(commandTimeout: -1));
    }

    [Fact]
    public void EnableSqliteBulkTransactions_WithDeferredConstraintsDisabled_ShouldSetDeferredToFalse()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteBulkTransactions(enableDeferred: false);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableDeferredConstraints", false), Times.Once);
    }

    #endregion

    #region OptimizeForSqliteBulkUpdates Tests

    [Fact]
    public void OptimizeForSqliteBulkUpdates_WithDefaultParameters_ShouldApplyDefaultOptimizations()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Inventory>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkUpdates();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify default bulk update settings
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdate", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdateBatchSize", 500), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeRowLocking", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeUpdateIndexes", true), Times.Once);
    }

    [Fact]
    public void OptimizeForSqliteBulkUpdates_WithCustomParameters_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Inventory>();
        const bool enableRowLocking = false;
        const bool optimizeIndexes = false;
        const int batchSize = 250;

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkUpdates(
            enableRowLocking, optimizeIndexes, batchSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeRowLocking", false), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:OptimizeUpdateIndexes", false), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdateBatchSize", 250), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5001)]
    public void OptimizeForSqliteBulkUpdates_WithInvalidBatchSize_ShouldThrowArgumentOutOfRangeException(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Inventory>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockEntityBuilder.Object.OptimizeForSqliteBulkUpdates(batchSize: batchSize));
    }

    #endregion

    #region ConfigureSqliteBulkOperations Tests

    [Fact]
    public void ConfigureSqliteBulkOperations_WithDefaultParameters_ShouldApplyDefaultConfiguration()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<LogEntry>();

        // Act
        var result = mockEntityBuilder.Object.ConfigureSqliteBulkOperations();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify bulk operations configuration
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkOperationsEnabled", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableWalMode", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableBulkStatistics", true), Times.Once);
    }

    [Fact]
    public void ConfigureSqliteBulkOperations_WithWalModeDisabled_ShouldDisableWal()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<LogEntry>();

        // Act
        var result = mockEntityBuilder.Object.ConfigureSqliteBulkOperations(enableWalMode: false);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableWalMode", false), Times.Once);
    }

    [Fact]
    public void ConfigureSqliteBulkOperations_WithCustomPragmaSettings_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<LogEntry>();
        var customPragmas = new Dictionary<string, object>
        {
            ["cache_size"] = -128000, // 128MB cache
            ["temp_store"] = "MEMORY"
        };

        // Act
        var result = mockEntityBuilder.Object.ConfigureSqliteBulkOperations(
            enableWalMode: true, customPragmas);

        // Assert
        Assert.NotNull(result);
        
        // The method should merge default and custom pragma settings
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkPragmaSettings", 
            It.Is<Dictionary<string, object>>(dict => 
                dict.ContainsKey("cache_size") && 
                dict.ContainsKey("temp_store") &&
                dict.ContainsKey("synchronous"))), Times.Once);
    }

    [Fact]
    public void ConfigureSqliteBulkOperations_WithNullEntityBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        EntityTypeBuilder<LogEntry> nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.ConfigureSqliteBulkOperations());
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void BulkConfigurationMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object
            .OptimizeForSqliteBulkInserts(500)
            .EnableSqliteBulkTransactions(IsolationLevel.ReadCommitted, 600)
            .OptimizeForSqliteBulkUpdates(true, true, 300)
            .ConfigureSqliteBulkOperations();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockEntityBuilder.Object, result);
        
        // Verify all methods were called
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsert", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkTransactionIsolation", "ReadCommitted"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdate", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkOperationsEnabled", true), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CombinedBulkOptimizations_ShouldApplyAllSettings()
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();
        var customPragmas = new Dictionary<string, object>
        {
            ["journal_mode"] = "WAL",
            ["synchronous"] = "NORMAL"
        };

        // Act
        mockEntityBuilder.Object
            .OptimizeForSqliteBulkInserts(batchSize: 2000)
            .EnableSqliteBulkTransactions(
                isolationLevel: IsolationLevel.Snapshot,
                commandTimeout: 900,
                enableDeferred: false)
            .OptimizeForSqliteBulkUpdates(
                enableRowLevelLocking: true,
                optimizeIndexes: true,
                batchSize: 1000)
            .ConfigureSqliteBulkOperations(
                enableWalMode: true,
                pragmaSettings: customPragmas);

        // Assert - Verify comprehensive bulk configuration
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsertBatchSize", 2000), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkTransactionIsolation", "Snapshot"), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkCommandTimeout", 900), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableDeferredConstraints", false), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdateBatchSize", 1000), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkOperationsEnabled", true), Times.Once);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:EnableWalMode", true), Times.Once);
    }

    #endregion

    #region Parameter Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void OptimizeForSqliteBulkInserts_WithValidBatchSizes_ShouldSucceed(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Product>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkInserts(batchSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkInsertBatchSize", batchSize), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(5000)]
    public void OptimizeForSqliteBulkUpdates_WithValidBatchSizes_ShouldSucceed(int batchSize)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Inventory>();

        // Act
        var result = mockEntityBuilder.Object.OptimizeForSqliteBulkUpdates(batchSize: batchSize);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkUpdateBatchSize", batchSize), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(300)]
    [InlineData(3600)]
    public void EnableSqliteBulkTransactions_WithValidTimeouts_ShouldSucceed(int timeout)
    {
        // Arrange
        var mockEntityBuilder = CreateMockEntityTypeBuilder<Order>();

        // Act
        var result = mockEntityBuilder.Object.EnableSqliteBulkTransactions(commandTimeout: timeout);

        // Assert
        Assert.NotNull(result);
        mockEntityBuilder.Verify(x => x.HasAnnotation("Sqlite:BulkCommandTimeout", timeout), Times.Once);
    }

    #endregion
}