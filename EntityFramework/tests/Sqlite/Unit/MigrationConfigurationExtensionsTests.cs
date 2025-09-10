// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.Sqlite.Tests;

public class MigrationConfigurationExtensionsTests
{
    #region Mock Setup Helpers

    private static Mock<MigrationBuilder> CreateMockMigrationBuilder()
    {
        var mockMigrationBuilder = new Mock<MigrationBuilder>();
        
        mockMigrationBuilder.Setup(x => x.SetAnnotation(It.IsAny<string>(), It.IsAny<object>()))
                          .Returns(mockMigrationBuilder.Object);
                          
        mockMigrationBuilder.Setup(x => x.Sql(It.IsAny<string>()))
                          .Returns(mockMigrationBuilder.Object);

        return mockMigrationBuilder;
    }

    #endregion

    #region EnableSqliteIncrementalMigrations Tests

    [Fact]
    public void EnableSqliteIncrementalMigrations_WithDefaultParameters_ShouldApplyDefaultSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.EnableSqliteIncrementalMigrations();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify default incremental migration settings
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:IncrementalMigrations", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizeColumnAddition", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizeIndexCreation", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:PreserveDataDuringMigration", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:UseAlterTableWhenPossible", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationTransactionMode", "IMMEDIATE"), Times.Once);
    }

    [Fact]
    public void EnableSqliteIncrementalMigrations_WithCustomParameters_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.EnableSqliteIncrementalMigrations(
            enableColumnAddition: false,
            enableIndexOptimization: false,
            preserveData: false);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizeColumnAddition", false), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizeIndexCreation", false), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:PreserveDataDuringMigration", false), Times.Once);
    }

    [Fact]
    public void EnableSqliteIncrementalMigrations_WithNullMigrationBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationBuilder nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.EnableSqliteIncrementalMigrations());
    }

    #endregion

    #region EnableSqliteParallelMigrations Tests

    [Fact]
    public void EnableSqliteParallelMigrations_WithDefaultParameters_ShouldApplyDefaultSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.EnableSqliteParallelMigrations();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify default parallel migration settings
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:ParallelMigrations", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MaxDegreeOfParallelism", 4), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:EnableTableParallelism", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:EnableIndexParallelism", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:ParallelExecutionStrategy", "INDEPENDENT_OPERATIONS"), Times.Once);
    }

    [Fact]
    public void EnableSqliteParallelMigrations_WithCustomParameters_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const int maxParallelism = 8;
        const bool enableTableParallelism = false;
        const bool enableIndexParallelism = false;

        // Act
        var result = mockMigrationBuilder.Object.EnableSqliteParallelMigrations(
            maxParallelism, enableTableParallelism, enableIndexParallelism);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MaxDegreeOfParallelism", maxParallelism), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:EnableTableParallelism", enableTableParallelism), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:EnableIndexParallelism", enableIndexParallelism), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(17)]
    public void EnableSqliteParallelMigrations_WithInvalidMaxDegreeOfParallelism_ShouldThrowArgumentOutOfRangeException(int maxParallelism)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockMigrationBuilder.Object.EnableSqliteParallelMigrations(maxParallelism));
    }

    [Fact]
    public void EnableSqliteParallelMigrations_WithNullMigrationBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationBuilder nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.EnableSqliteParallelMigrations());
    }

    #endregion

    #region CreateSqliteMigrationCheckpoint Tests

    [Fact]
    public void CreateSqliteMigrationCheckpoint_WithDefaultParameters_ShouldCreateCheckpoint()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify checkpoint creation settings
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CreateCheckpoint", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointIncludeData", false), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointCompressionLevel", 6), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointStrategy", "SCHEMA_ONLY"), Times.Once);
        
        // Verify checkpoint name was generated (it should contain "checkpoint_")
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointName", 
            It.Is<string>(name => name.StartsWith("checkpoint_"))), Times.Once);
    }

    [Fact]
    public void CreateSqliteMigrationCheckpoint_WithCustomName_ShouldUseCustomName()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const string checkpointName = "CustomCheckpoint";

        // Act
        var result = mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint(checkpointName);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointName", checkpointName), Times.Once);
    }

    [Fact]
    public void CreateSqliteMigrationCheckpoint_WithIncludeData_ShouldSetFullBackupStrategy()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint(includeData: true);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointIncludeData", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointStrategy", "FULL_BACKUP"), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void CreateSqliteMigrationCheckpoint_WithInvalidCompressionLevel_ShouldThrowArgumentOutOfRangeException(int compressionLevel)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint(compressionLevel: compressionLevel));
    }

    [Fact]
    public void CreateSqliteMigrationCheckpoint_WithInvalidCheckpointName_ShouldThrowArgumentException()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const string invalidName = "checkpoint<>name"; // Contains invalid filename characters

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint(invalidName));
    }

    #endregion

    #region OptimizeSqliteMigrationPerformance Tests

    [Fact]
    public void OptimizeSqliteMigrationPerformance_WithDefaultParameters_ShouldApplyOptimizations()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.OptimizeSqliteMigrationPerformance();

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify performance optimization settings
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizedMigrationPerformance", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationWalMode", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationCacheSize", -65536), Times.Once); // Negative for KB
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationBusyTimeout", 30000), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationSynchronousMode", "NORMAL"), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationLockingMode", "EXCLUSIVE"), Times.Once);
    }

    [Fact]
    public void OptimizeSqliteMigrationPerformance_WithCustomParameters_ShouldApplyCustomSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const bool enableWal = false;
        const int cacheSize = 128 * 1024; // 128MB
        const int busyTimeout = 60000; // 60 seconds

        // Act
        var result = mockMigrationBuilder.Object.OptimizeSqliteMigrationPerformance(
            enableWal, cacheSize, busyTimeout);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationWalMode", false), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationCacheSize", -cacheSize), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationBusyTimeout", busyTimeout), Times.Once);
    }

    [Theory]
    [InlineData(1023)] // Below minimum
    [InlineData(0)]
    [InlineData(-1)]
    public void OptimizeSqliteMigrationPerformance_WithInvalidCacheSize_ShouldThrowArgumentOutOfRangeException(int cacheSize)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockMigrationBuilder.Object.OptimizeSqliteMigrationPerformance(cacheSize: cacheSize));
    }

    [Theory]
    [InlineData(999)] // Below minimum
    [InlineData(0)]
    [InlineData(-1)]
    public void OptimizeSqliteMigrationPerformance_WithInvalidBusyTimeout_ShouldThrowArgumentOutOfRangeException(int busyTimeout)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => 
            mockMigrationBuilder.Object.OptimizeSqliteMigrationPerformance(busyTimeout: busyTimeout));
    }

    #endregion

    #region CreateRollbackPoint Tests

    [Fact]
    public void CreateRollbackPoint_WithValidName_ShouldCreateRollbackPoint()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const string rollbackPointName = "BeforeDataMigration";

        // Act
        var result = mockMigrationBuilder.Object.CreateRollbackPoint(rollbackPointName);

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify rollback point creation
        mockMigrationBuilder.Verify(x => x.SetAnnotation($"Sqlite:RollbackPoint:{rollbackPointName}", 
            It.IsAny<DateTime>()), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation($"Sqlite:AutoRollback:{rollbackPointName}", true), Times.Once);
        
        // Verify SAVEPOINT SQL is executed
        mockMigrationBuilder.Verify(x => x.Sql($"SAVEPOINT {rollbackPointName};"), Times.Once);
    }

    [Fact]
    public void CreateRollbackPoint_WithAutoRollbackDisabled_ShouldSetAutoRollbackToFalse()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();
        const string rollbackPointName = "TestPoint";

        // Act
        var result = mockMigrationBuilder.Object.CreateRollbackPoint(rollbackPointName, autoRollbackOnFailure: false);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation($"Sqlite:AutoRollback:{rollbackPointName}", false), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRollbackPoint_WithInvalidName_ShouldThrowArgumentException(string? rollbackPointName)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            mockMigrationBuilder.Object.CreateRollbackPoint(rollbackPointName!));
    }

    [Fact]
    public void CreateRollbackPoint_WithNullMigrationBuilder_ShouldThrowArgumentNullException()
    {
        // Arrange
        MigrationBuilder nullBuilder = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            nullBuilder.CreateRollbackPoint("TestPoint"));
    }

    #endregion

    #region Method Chaining Tests

    [Fact]
    public void MigrationConfigurationMethods_ShouldSupportMethodChaining()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object
            .EnableSqliteIncrementalMigrations()
            .EnableSqliteParallelMigrations()
            .OptimizeSqliteMigrationPerformance()
            .CreateSqliteMigrationCheckpoint("TestCheckpoint")
            .CreateRollbackPoint("TestRollback");

        // Assert
        Assert.NotNull(result);
        Assert.Same(mockMigrationBuilder.Object, result);
        
        // Verify all methods were called
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:IncrementalMigrations", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:ParallelMigrations", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:OptimizedMigrationPerformance", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CreateCheckpoint", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.Sql("SAVEPOINT TestRollback;"), Times.Once);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CombinedMigrationOptimizations_ShouldApplyAllSettings()
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        mockMigrationBuilder.Object
            .EnableSqliteIncrementalMigrations(
                enableColumnAddition: true,
                enableIndexOptimization: true,
                preserveData: true)
            .EnableSqliteParallelMigrations(
                maxDegreeOfParallelism: 6,
                enableTableLevelParallelism: true,
                enableIndexParallelism: true)
            .OptimizeSqliteMigrationPerformance(
                enableWalMode: true,
                cacheSize: 256 * 1024,
                busyTimeout: 45000)
            .CreateSqliteMigrationCheckpoint(
                checkpointName: "BeforeMajorChanges",
                includeData: true,
                compressionLevel: 9)
            .CreateRollbackPoint("BeforeRiskyOperation", autoRollbackOnFailure: true);

        // Assert - Verify comprehensive migration configuration
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:IncrementalMigrations", true), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MaxDegreeOfParallelism", 6), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationCacheSize", -262144), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationBusyTimeout", 45000), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointName", "BeforeMajorChanges"), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointCompressionLevel", 9), Times.Once);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointStrategy", "FULL_BACKUP"), Times.Once);
        mockMigrationBuilder.Verify(x => x.Sql("SAVEPOINT BeforeRiskyOperation;"), Times.Once);
    }

    #endregion

    #region Parameter Validation Tests

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void EnableSqliteParallelMigrations_WithValidParallelismValues_ShouldSucceed(int maxParallelism)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.EnableSqliteParallelMigrations(maxParallelism);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MaxDegreeOfParallelism", maxParallelism), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public void CreateSqliteMigrationCheckpoint_WithValidCompressionLevels_ShouldSucceed(int compressionLevel)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.CreateSqliteMigrationCheckpoint(compressionLevel: compressionLevel);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:CheckpointCompressionLevel", compressionLevel), Times.Once);
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(65536)]
    [InlineData(262144)]
    public void OptimizeSqliteMigrationPerformance_WithValidCacheSizes_ShouldSucceed(int cacheSize)
    {
        // Arrange
        var mockMigrationBuilder = CreateMockMigrationBuilder();

        // Act
        var result = mockMigrationBuilder.Object.OptimizeSqliteMigrationPerformance(cacheSize: cacheSize);

        // Assert
        Assert.NotNull(result);
        mockMigrationBuilder.Verify(x => x.SetAnnotation("Sqlite:MigrationCacheSize", -cacheSize), Times.Once);
    }

    #endregion
}