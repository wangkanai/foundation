// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;
using Wangkanai.Audit.EntityFramework;

namespace Wangkanai.Audit.EntityFramework.Tests;

/// <summary>Unit tests for the AuditDbContext implementation.</summary>
public class AuditDbContextTests : IDisposable
{
	private readonly DbContextOptions<AuditDbContext<Guid, TestUser, string>> _options;

	public AuditDbContextTests()
	{
		_options = new DbContextOptionsBuilder<AuditDbContext<Guid, TestUser, string>>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithValidOptions_CreatesInstance()
	{
		// Act
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);

		// Assert
		context.Should().NotBeNull();
		context.Database.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_WithNullOptions_ThrowsException()
	{
		// Act & Assert
		Action act = () => new AuditDbContext<Guid, TestUser, string>(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	#endregion

	#region DbSet Property Tests

	[Fact]
	public void AuditTrails_Property_ReturnsValidDbSet()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);

		// Act
		var auditTrails = context.AuditTrails;

		// Assert
		auditTrails.Should().NotBeNull();
		auditTrails.Should().BeAssignableTo<DbSet<Trail<Guid, TestUser, string>>>();
	}

	[Fact]
	public void AuditTrails_CanAddAndQueryEntities()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		var user = new TestUser { Id = "user1", UserName = "testuser" };
		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Create,
			UserId = user.Id,
			User = user,
			Timestamp = DateTime.UtcNow,
			EntityName = "TestEntity",
			PrimaryKey = "123"
		};

		// Act
		context.AuditTrails.Add(trail);
		context.SaveChanges();

		var retrievedTrail = context.AuditTrails.First();

		// Assert
		retrievedTrail.Should().NotBeNull();
		retrievedTrail.Id.Should().Be(trail.Id);
		retrievedTrail.EntityName.Should().Be("TestEntity");
		retrievedTrail.PrimaryKey.Should().Be("123");
		retrievedTrail.TrailType.Should().Be(TrailType.Create);
	}

	#endregion

	#region Model Configuration Tests

	[Fact]
	public void OnModelCreating_ConfiguresAuditTrailCorrectly()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act - Get the model to trigger OnModelCreating
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));

		// Assert
		trailEntityType.Should().NotBeNull("Trail entity should be configured");
	}

	[Fact]
	public void ModelConfiguration_HasCorrectTableName()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));
		var tableName = trailEntityType?.GetTableName();

		// Assert
		tableName.Should().Be("AuditTrails");
	}

	[Fact]
	public void ModelConfiguration_HasCorrectPrimaryKey()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));
		var primaryKey = trailEntityType?.FindPrimaryKey();

		// Assert
		primaryKey.Should().NotBeNull();
		primaryKey!.Properties.Should().HaveCount(1);
		primaryKey.Properties.First().Name.Should().Be("Id");
	}

	[Fact]
	public void ModelConfiguration_HasCorrectPropertyConfigurations()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));

		var trailTypeProperty = trailEntityType?.FindProperty("TrailType");
		var timestampProperty = trailEntityType?.FindProperty("Timestamp");
		var entityNameProperty = trailEntityType?.FindProperty("EntityName");
		var primaryKeyProperty = trailEntityType?.FindProperty("PrimaryKey");
		var oldValuesJsonProperty = trailEntityType?.FindProperty("OldValuesJson");
		var newValuesJsonProperty = trailEntityType?.FindProperty("NewValuesJson");

		// Assert property configurations
		trailTypeProperty.Should().NotBeNull();
		trailTypeProperty!.IsNullable.Should().BeFalse();
		trailTypeProperty.GetColumnType().Should().Contain("byte", "TrailType should be stored as byte");

		timestampProperty.Should().NotBeNull();
		timestampProperty!.IsNullable.Should().BeFalse();
		timestampProperty.GetColumnType().Should().Be("datetime2");

		entityNameProperty.Should().NotBeNull();
		entityNameProperty!.IsNullable.Should().BeFalse();
		entityNameProperty.GetMaxLength().Should().Be(256);

		primaryKeyProperty.Should().NotBeNull();
		primaryKeyProperty!.GetMaxLength().Should().Be(256);

		oldValuesJsonProperty.Should().NotBeNull();
		oldValuesJsonProperty!.GetColumnType().Should().Be("nvarchar(max)");

		newValuesJsonProperty.Should().NotBeNull();
		newValuesJsonProperty!.GetColumnType().Should().Be("nvarchar(max)");
	}

	[Fact]
	public void ModelConfiguration_IgnoresComputedProperties()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));

		var oldValuesProperty = trailEntityType?.FindProperty("OldValues");
		var newValuesProperty = trailEntityType?.FindProperty("NewValues");

		// Assert - These properties should be ignored
		oldValuesProperty.Should().BeNull("OldValues should be ignored");
		newValuesProperty.Should().BeNull("NewValues should be ignored");
	}

	[Fact]
	public void ModelConfiguration_HasCorrectIndexes()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));
		var indexes = trailEntityType?.GetIndexes().ToList();

		// Assert
		indexes.Should().NotBeNull();
		indexes!.Should().HaveCountGreaterThanOrEqualTo(4, "Should have at least 4 indexes defined");

		// Check for specific indexes
		var entityIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 2 && 
			i.Properties.Any(p => p.Name == "EntityName") && 
			i.Properties.Any(p => p.Name == "PrimaryKey"));
		entityIndex.Should().NotBeNull("Should have EntityName + PrimaryKey index");

		var userIdIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 1 && 
			i.Properties.First().Name == "UserId");
		userIdIndex.Should().NotBeNull("Should have UserId index");

		var timestampIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 1 && 
			i.Properties.First().Name == "Timestamp");
		timestampIndex.Should().NotBeNull("Should have Timestamp index");

		var compositeIndex = indexes.FirstOrDefault(i => 
			i.Properties.Count == 2 && 
			i.Properties.Any(p => p.Name == "EntityName") && 
			i.Properties.Any(p => p.Name == "Timestamp"));
		compositeIndex.Should().NotBeNull("Should have EntityName + Timestamp composite index");
	}

	[Fact]
	public void ModelConfiguration_HasCorrectRelationshipConfiguration()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		// Act
		var model = context.Model;
		var trailEntityType = model.FindEntityType(typeof(Trail<Guid, TestUser, string>));
		var userNavigation = trailEntityType?.FindNavigation("User");

		// Assert
		userNavigation.Should().NotBeNull("Should have User navigation property");
		
		var foreignKey = userNavigation?.ForeignKey;
		foreignKey.Should().NotBeNull();
		foreignKey!.DeleteBehavior.Should().Be(DeleteBehavior.SetNull, "Delete behavior should be SetNull");
		foreignKey.Properties.First().Name.Should().Be("UserId");
	}

	#endregion

	#region Database Operations Tests

	[Fact]
	public void Database_EnsureCreated_CreatesSchemaSuccessfully()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);

		// Act
		var created = context.Database.EnsureCreated();

		// Assert
		created.Should().BeTrue("Database should be created successfully");
		context.AuditTrails.Should().NotBeNull();
	}

	[Fact]
	public void Database_CanSaveAndRetrieveComplexTrail()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var user = new TestUser { Id = "complex-user", UserName = "complexuser", Email = "complex@test.com" };
		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Update,
			UserId = user.Id,
			User = user,
			Timestamp = DateTime.UtcNow,
			EntityName = "ComplexEntity",
			PrimaryKey = "complex-123",
			ChangedColumns = new List<string> { "Name", "Email", "Status" },
			OldValuesJson = """{"Name":"Old Name","Email":"old@test.com","Status":"Active"}""",
			NewValuesJson = """{"Name":"New Name","Email":"new@test.com","Status":"Inactive"}"""
		};

		// Act
		context.AuditTrails.Add(trail);
		context.SaveChanges();

		// Clear context to ensure we're reading from database
		context.Entry(trail).State = EntityState.Detached;
		
		var retrievedTrail = context.AuditTrails
			.Include(t => t.User)
			.First(t => t.Id == trail.Id);

		// Assert
		retrievedTrail.Should().NotBeNull();
		retrievedTrail.Id.Should().Be(trail.Id);
		retrievedTrail.TrailType.Should().Be(TrailType.Update);
		retrievedTrail.UserId.Should().Be("complex-user");
		retrievedTrail.EntityName.Should().Be("ComplexEntity");
		retrievedTrail.PrimaryKey.Should().Be("complex-123");
		retrievedTrail.ChangedColumns.Should().BeEquivalentTo(new[] { "Name", "Email", "Status" });
		retrievedTrail.OldValuesJson.Should().Be("""{"Name":"Old Name","Email":"old@test.com","Status":"Active"}""");
		retrievedTrail.NewValuesJson.Should().Be("""{"Name":"New Name","Email":"new@test.com","Status":"Inactive"}""");

		// Test computed properties work correctly
		var oldValues = retrievedTrail.OldValues;
		var newValues = retrievedTrail.NewValues;
		
		oldValues.Should().NotBeEmpty();
		oldValues["Name"].ToString().Should().Be("Old Name");
		newValues["Email"].ToString().Should().Be("new@test.com");
	}

	[Fact]
	public void Database_HandlesNullUserIdCorrectly()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Create,
			UserId = null,
			User = null,
			Timestamp = DateTime.UtcNow,
			EntityName = "SystemEntity",
			PrimaryKey = "system-123"
		};

		// Act & Assert
		Action act = () =>
		{
			context.AuditTrails.Add(trail);
			context.SaveChanges();
		};

		act.Should().NotThrow("Should allow null UserId and User");

		var retrievedTrail = context.AuditTrails.First(t => t.Id == trail.Id);
		retrievedTrail.UserId.Should().BeNull();
		retrievedTrail.User.Should().BeNull();
	}

	[Fact]
	public void Database_HandlesLongJsonValuesCorrectly()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var longText = new string('A', 5000); // 5KB text
		var longJson = $@"{{
				""LongField"": ""{longText}"",
				""NormalField"": ""normal value"",
				""AnotherLongField"": ""{new string('B', 3000)}""
			}}";

		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Update,
			Timestamp = DateTime.UtcNow,
			EntityName = "LargeEntity",
			PrimaryKey = "large-123",
			OldValuesJson = longJson,
			NewValuesJson = longJson
		};

		// Act & Assert
		Action act = () =>
		{
			context.AuditTrails.Add(trail);
			context.SaveChanges();
		};

		act.Should().NotThrow("Should handle large JSON values");

		var retrievedTrail = context.AuditTrails.First(t => t.Id == trail.Id);
		retrievedTrail.OldValuesJson.Should().Be(longJson);
		retrievedTrail.NewValuesJson.Should().Be(longJson);
	}

	#endregion

	#region Concurrency and Performance Tests

	[Fact]
	public void Context_MultipleInstances_CanOperateIndependently()
	{
		// Arrange
		var dbName = Guid.NewGuid().ToString();
		var options1 = new DbContextOptionsBuilder<AuditDbContext<Guid, TestUser, string>>()
			.UseInMemoryDatabase(databaseName: dbName)
			.Options;
		var options2 = new DbContextOptionsBuilder<AuditDbContext<Guid, TestUser, string>>()
			.UseInMemoryDatabase(databaseName: dbName)
			.Options;

		// Act & Assert
		using (var context1 = new AuditDbContext<Guid, TestUser, string>(options1))
		using (var context2 = new AuditDbContext<Guid, TestUser, string>(options2))
		{
			context1.Database.EnsureCreated();

			var trail1 = new Trail<Guid, TestUser, string>
			{
				Id = Guid.NewGuid(),
				TrailType = TrailType.Create,
				Timestamp = DateTime.UtcNow,
				EntityName = "Entity1",
				PrimaryKey = "1"
			};

			var trail2 = new Trail<Guid, TestUser, string>
			{
				Id = Guid.NewGuid(),
				TrailType = TrailType.Update,
				Timestamp = DateTime.UtcNow,
				EntityName = "Entity2",
				PrimaryKey = "2"
			};

			context1.AuditTrails.Add(trail1);
			context2.AuditTrails.Add(trail2);

			context1.SaveChanges();
			context2.SaveChanges();

			// Both contexts should see both records
			context1.AuditTrails.Should().HaveCount(2);
			context2.AuditTrails.Should().HaveCount(2);
		}
	}

	[Fact]
	public void Context_BulkOperations_PerformEfficiently()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var user = new TestUser { Id = "bulk-user", UserName = "bulkuser" };
		var trails = new List<Trail<Guid, TestUser, string>>();

		// Generate 100 audit trails
		for (int i = 0; i < 100; i++)
		{
			trails.Add(new Trail<Guid, TestUser, string>
			{
				Id = Guid.NewGuid(),
				TrailType = (TrailType)(i % 3 + 1), // Cycle through Create, Update, Delete
				UserId = user.Id,
				User = user,
				Timestamp = DateTime.UtcNow.AddMinutes(-i),
				EntityName = $"Entity{i % 10}",
				PrimaryKey = $"key-{i}"
			});
		}

		// Act
		var startTime = DateTime.UtcNow;
		context.AuditTrails.AddRange(trails);
		context.SaveChanges();
		var endTime = DateTime.UtcNow;

		// Assert
		var duration = endTime - startTime;
		duration.TotalSeconds.Should().BeLessThan(5, "Bulk insert should complete within 5 seconds");

		context.AuditTrails.Should().HaveCount(100);
		
		// Test query performance
		var queryStart = DateTime.UtcNow;
		var entityTrails = context.AuditTrails
			.Where(t => t.EntityName == "Entity1")
			.OrderByDescending(t => t.Timestamp)
			.ToList();
		var queryEnd = DateTime.UtcNow;

		var queryDuration = queryEnd - queryStart;
		queryDuration.TotalSeconds.Should().BeLessThan(1, "Query should complete within 1 second");
		entityTrails.Should().HaveCount(10); // Should have 10 trails for Entity1
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public void Context_InvalidModelState_HandlesGracefully()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var trail = new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = TrailType.Create,
			Timestamp = DateTime.UtcNow,
			EntityName = null!, // This should cause validation error
			PrimaryKey = "123"
		};

		// Act & Assert
		context.AuditTrails.Add(trail);
		
		Action act = () => context.SaveChanges();
		act.Should().Throw<DbUpdateException>("Should throw for null required field");
	}

	[Fact]
	public void Context_DuplicatePrimaryKeys_HandlesCorrectly()
	{
		// Arrange
		using var context = new AuditDbContext<Guid, TestUser, string>(_options);
		context.Database.EnsureCreated();

		var id = Guid.NewGuid();
		var trail1 = new Trail<Guid, TestUser, string>
		{
			Id = id,
			TrailType = TrailType.Create,
			Timestamp = DateTime.UtcNow,
			EntityName = "Entity1",
			PrimaryKey = "123"
		};

		var trail2 = new Trail<Guid, TestUser, string>
		{
			Id = id, // Same ID
			TrailType = TrailType.Update,
			Timestamp = DateTime.UtcNow,
			EntityName = "Entity2",
			PrimaryKey = "456"
		};

		// Act & Assert
		context.AuditTrails.Add(trail1);
		context.SaveChanges();

		context.AuditTrails.Add(trail2);
		Action act = () => context.SaveChanges();
		
		act.Should().Throw<InvalidOperationException>("Should prevent duplicate primary keys");
	}

	#endregion

	#region IDisposable Implementation

	public void Dispose()
	{
		// Cleanup is handled by using statements in tests
	}

	#endregion
}

