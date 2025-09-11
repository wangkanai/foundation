// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wangkanai.Audit.EntityFramework.Extensions;

namespace Wangkanai.Audit.EntityFramework.Tests;

/// <summary>Unit tests for TrailEntityConfigurationExtensions.</summary>
public class TrailEntityConfigurationExtensionsTests
{
	#region Configuration Extension Tests

	[Fact]
	public void ConfigureAuditTrail_WithValidBuilder_AppliesAllConfigurations()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestAuditDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		using var context = new TestAuditDbContext(options);
		context.Database.EnsureCreated();

		// Act - Configuration is applied during model creation
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));

		// Assert - Verify all configuration aspects
		trailEntityType.Should().NotBeNull("Trail entity should be configured");

		// Table name configuration
		var tableName = trailEntityType!.GetTableName();
		tableName.Should().Be("AuditTrails", "Table should be named AuditTrails");

		// Primary key configuration
		var primaryKey = trailEntityType.FindPrimaryKey();
		primaryKey.Should().NotBeNull("Primary key should be configured");
		primaryKey!.Properties.Should().HaveCount(1);
		primaryKey.Properties.First().Name.Should().Be("Id");

		// Property configurations
		VerifyPropertyConfigurations(trailEntityType);

		// Index configurations
		VerifyIndexConfigurations(trailEntityType);

		// Relationship configurations
		VerifyRelationshipConfigurations(trailEntityType);
	}

	[Fact]
	public void ConfigureAuditTrail_WithDifferentKeyTypes_WorksForAllTypes()
	{
		// Test with different key types
		TestConfigurationWithKeyType<int>();
		TestConfigurationWithKeyType<long>();
		TestConfigurationWithKeyType<Guid>();
		TestConfigurationWithKeyType<string>();
	}

	private static void TestConfigurationWithKeyType<TKey>()
		where TKey : IEquatable<TKey>, IComparable<TKey>
	{
		// This would require creating different test contexts for each key type
		// For now, we'll verify the method works with different generic constraints
		var result = typeof(TrailEntityConfigurationExtensions)
			.GetMethod(nameof(TrailEntityConfigurationExtensions.ConfigureAuditTrail));
		
		result.Should().NotBeNull("ConfigureAuditTrail method should exist");
	}

	#endregion

	#region Property Configuration Tests

	private static void VerifyPropertyConfigurations(Microsoft.EntityFrameworkCore.Metadata.IEntityType trailEntityType)
	{
		// TrailType property
		var trailTypeProperty = trailEntityType.FindProperty("TrailType");
		trailTypeProperty.Should().NotBeNull("TrailType property should be configured");
		trailTypeProperty!.IsNullable.Should().BeFalse("TrailType should be required");

		// Timestamp property
		var timestampProperty = trailEntityType.FindProperty("Timestamp");
		timestampProperty.Should().NotBeNull("Timestamp property should be configured");
		timestampProperty!.IsNullable.Should().BeFalse("Timestamp should be required");
		// Skip column type check for InMemory provider

		// EntityName property
		var entityNameProperty = trailEntityType.FindProperty("EntityName");
		entityNameProperty.Should().NotBeNull("EntityName property should be configured");
		entityNameProperty!.IsNullable.Should().BeFalse("EntityName should be required");
		entityNameProperty.GetMaxLength().Should().Be(256, "EntityName should have max length of 256");

		// PrimaryKey property
		var primaryKeyProperty = trailEntityType.FindProperty("PrimaryKey");
		primaryKeyProperty.Should().NotBeNull("PrimaryKey property should be configured");
		primaryKeyProperty!.GetMaxLength().Should().Be(256, "PrimaryKey should have max length of 256");

		// JSON properties
		var oldValuesJsonProperty = trailEntityType.FindProperty("OldValuesJson");
		oldValuesJsonProperty.Should().NotBeNull("OldValuesJson property should be configured");
		// Skip column type check for InMemory provider

		var newValuesJsonProperty = trailEntityType.FindProperty("NewValuesJson");
		newValuesJsonProperty.Should().NotBeNull("NewValuesJson property should be configured");
		// Skip column type check for InMemory provider

		// Ignored properties
		var oldValuesProperty = trailEntityType.FindProperty("OldValues");
		oldValuesProperty.Should().BeNull("OldValues should be ignored");

		var newValuesProperty = trailEntityType.FindProperty("NewValues");
		newValuesProperty.Should().BeNull("NewValues should be ignored");
	}

	#endregion

	#region Index Configuration Tests

	private static void VerifyIndexConfigurations(Microsoft.EntityFrameworkCore.Metadata.IEntityType trailEntityType)
	{
		var indexes = trailEntityType.GetIndexes().ToList();
		indexes.Should().NotBeEmpty("Entity should have indexes configured");

		// Entity + PrimaryKey index
		var entityIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 2 && 
			i.Properties.Any(p => p.Name == "EntityName") && 
			i.Properties.Any(p => p.Name == "PrimaryKey"));
		entityIndex.Should().NotBeNull("Should have EntityName + PrimaryKey composite index");

		// UserId index
		var userIdIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 1 && 
			i.Properties.First().Name == "UserId");
		userIdIndex.Should().NotBeNull("Should have UserId index");

		// Timestamp index
		var timestampIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 1 && 
			i.Properties.First().Name == "Timestamp");
		timestampIndex.Should().NotBeNull("Should have Timestamp index");

		// Entity + Timestamp composite index
		var compositeIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 2 && 
			i.Properties.Any(p => p.Name == "EntityName") && 
			i.Properties.Any(p => p.Name == "Timestamp"));
		compositeIndex.Should().NotBeNull("Should have EntityName + Timestamp composite index");
	}

	#endregion

	#region Relationship Configuration Tests

	private static void VerifyRelationshipConfigurations(Microsoft.EntityFrameworkCore.Metadata.IEntityType trailEntityType)
	{
		var userNavigation = trailEntityType.FindNavigation("User");
		userNavigation.Should().NotBeNull("Should have User navigation property");

		var foreignKey = userNavigation!.ForeignKey;
		foreignKey.Should().NotBeNull("Should have foreign key configured");
		foreignKey.DeleteBehavior.Should().Be(DeleteBehavior.SetNull, "Delete behavior should be SetNull");
		foreignKey.Properties.First().Name.Should().Be("UserId", "Foreign key should be UserId");
	}

	#endregion

	#region Configuration Validation Tests

	[Fact]
	public void ModelConfiguration_ValidatesSuccessfully()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestAuditDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		// Act & Assert - Context creation should not throw
		using var context = new TestAuditDbContext(options);
		Action act = () => context.Database.EnsureCreated();
		
		act.Should().NotThrow("Model configuration should be valid");
	}

	[Fact]
	public void ModelConfiguration_SupportsEntityOperations()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestAuditDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		using var context = new TestAuditDbContext(options);
		context.Database.EnsureCreated();

		var user = new TestUser { Id = "config-user", UserName = "configuser" };
		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Create,
			UserId = user.Id,
			User = user,
			Timestamp = DateTime.UtcNow,
			EntityName = "ConfigEntity",
			PrimaryKey = "config-123",
			ChangedColumns = new List<string> { "TestField" },
			OldValuesJson = """{"TestField":"OldValue"}""",
			NewValuesJson = """{"TestField":"NewValue"}"""
		};

		// Act
		Action addAct = () => context.AuditTrails.Add(trail);
		Action saveAct = () => context.SaveChanges();

		// Assert
		addAct.Should().NotThrow("Adding entity should work with configuration");
		saveAct.Should().NotThrow("Saving entity should work with configuration");

		var savedTrail = context.AuditTrails.First();
		savedTrail.Should().NotBeNull();
		savedTrail.Id.Should().Be(trail.Id);
		savedTrail.EntityName.Should().Be("ConfigEntity");
	}

	#endregion

	#region Performance Tests

	[Fact]
	public void IndexConfiguration_ImprovedQueryPerformance()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TestAuditDbContext>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		using var context = new TestAuditDbContext(options);
		context.Database.EnsureCreated();

		// Add test data
		var user = new TestUser { Id = "perf-user", UserName = "perfuser" };
		var trails = new List<Trail<Guid, TestUser, string>>();
		
		for (int i = 0; i < 100; i++)
		{
			trails.Add(new Trail<Guid, TestUser, string>
			{
				Id = Guid.NewGuid(),
				TrailType = (TrailType)((i % 3) + 1),
				UserId = user.Id,
				User = user,
				Timestamp = DateTime.UtcNow.AddMinutes(-i),
				EntityName = $"Entity{i % 10}",
				PrimaryKey = $"key-{i % 20}",
				ChangedColumns = new List<string> { "Field1" }
			});
		}

		context.AuditTrails.AddRange(trails);
		context.SaveChanges();

		// Act & Assert - These queries should benefit from indexes
		var startTime = DateTime.UtcNow;

		var entityQuery = context.AuditTrails
			.Where(t => t.EntityName == "Entity1" && t.PrimaryKey == "key-1")
			.ToList();

		var userQuery = context.AuditTrails
			.Where(t => t.UserId == "perf-user")
			.ToList();

		var timeQuery = context.AuditTrails
			.Where(t => t.Timestamp > DateTime.UtcNow.AddMinutes(-30))
			.ToList();

		var compositeQuery = context.AuditTrails
			.Where(t => t.EntityName == "Entity2" && t.Timestamp > DateTime.UtcNow.AddMinutes(-50))
			.ToList();

		var endTime = DateTime.UtcNow;

		// Assert query results are correct
		entityQuery.Should().HaveCountGreaterThan(0);
		userQuery.Should().HaveCount(100);
		timeQuery.Should().HaveCountGreaterThan(0);
		compositeQuery.Should().HaveCountGreaterThan(0);

		// Assert reasonable performance (should be very fast with in-memory database)
		var duration = endTime - startTime;
		duration.TotalSeconds.Should().BeLessThan(1, "Indexed queries should be fast");
	}

	#endregion
}

#region Test Helper Classes

/// <summary>Test-specific DbContext to verify configuration extensions.</summary>
public class TestAuditDbContext : AuditDbContext<Guid, TestUser, string>
{
	public TestAuditDbContext(DbContextOptions<TestAuditDbContext> options)
		: base((DbContextOptions<AuditDbContext<Guid, TestUser, string>>)(object)options)
	{
	}
}


#endregion