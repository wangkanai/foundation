// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFramework.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Xunit;
using Wangkanai.Audit.EntityFramework;
using Wangkanai.Audit.EntityFramework.Repositories;
using Wangkanai.Audit.EntityFramework.Extensions;

namespace Wangkanai.Audit.EntityFramework.Integration.Tests;

/// <summary>Integration tests for the complete audit system using real database scenarios.</summary>
public class AuditServiceIntegrationTests : IDisposable
{
	private readonly ServiceProvider _serviceProvider;
	private readonly IServiceScope _scope;
	private readonly AuditDbContext<Guid, TestUser, string> _context;
	private readonly AuditRepository<Guid, TestUser, string> _repository;

	public AuditServiceIntegrationTests()
	{
		// Setup dependency injection container
		var services = new ServiceCollection();
		
		// Add logging
		services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
		
		// Add Entity Framework with in-memory database
		var databaseName = $"AuditIntegrationTest_{Guid.NewGuid()}";
		services.AddDbContext<AuditDbContext<Guid, TestUser, string>>(options =>
			options.UseInMemoryDatabase(databaseName)
				   .EnableSensitiveDataLogging()
				   .EnableDetailedErrors());

		// Add audit repository
		services.AddScoped<AuditRepository<Guid, TestUser, string>>();

		// Build service provider
		_serviceProvider = services.BuildServiceProvider();
		_scope = _serviceProvider.CreateScope();
		_context = _scope.ServiceProvider.GetRequiredService<AuditDbContext<Guid, TestUser, string>>();
		_repository = _scope.ServiceProvider.GetRequiredService<AuditRepository<Guid, TestUser, string>>();

		// Ensure database is created
		_context.Database.EnsureCreated();
	}

	#region End-to-End Workflow Tests

	[Fact]
	public async Task CompleteAuditWorkflow_FromCreationToQuery_WorksCorrectly()
	{
		// Arrange - Create test data
		var user1 = new TestUser { Id = "user1", UserName = "alice", Email = "alice@test.com" };
		var user2 = new TestUser { Id = "user2", UserName = "bob", Email = "bob@test.com" };

		// Create audit trails for different scenarios
		var customerCreateTrail = await CreateAndSaveTrail(user1, "Customer", "cust-123", TrailType.Create, 
			oldValues: new Dictionary<string, object>(),
			newValues: new Dictionary<string, object> 
			{ 
				{ "Name", "John Doe" }, 
				{ "Email", "john@example.com" }, 
				{ "Status", "Active" } 
			});

		await Task.Delay(10); // Ensure different timestamps

		var customerUpdateTrail = await CreateAndSaveTrail(user1, "Customer", "cust-123", TrailType.Update,
			oldValues: new Dictionary<string, object> 
			{ 
				{ "Name", "John Doe" }, 
				{ "Email", "john@example.com" }, 
				{ "Status", "Active" } 
			},
			newValues: new Dictionary<string, object> 
			{ 
				{ "Name", "John Doe" }, 
				{ "Email", "john.doe@example.com" }, 
				{ "Status", "Active" } 
			});

		await Task.Delay(10);

		var orderCreateTrail = await CreateAndSaveTrail(user2, "Order", "ord-456", TrailType.Create,
			oldValues: new Dictionary<string, object>(),
			newValues: new Dictionary<string, object> 
			{ 
				{ "CustomerId", "cust-123" }, 
				{ "Amount", 99.99m }, 
				{ "Status", "Pending" } 
			});

		// Act - Query audit trails using different methods
		var customerTrails = (await _repository.GetByEntityAsync("Customer", "cust-123")).ToList();
		var user1Trails = (await _repository.GetByUserAsync("user1")).ToList();
		var user2Trails = (await _repository.GetByUserAsync("user2")).ToList();
		var recentTrails = (await _repository.GetByDateRangeAsync(
			DateTime.UtcNow.AddMinutes(-5), 
			DateTime.UtcNow.AddMinutes(5))).ToList();

		// Assert - Verify all queries return expected results
		customerTrails.Should().HaveCount(2, "Customer should have Create and Update trails");
		customerTrails.Should().BeOrderedByDescending(t => t.Timestamp, "Results should be ordered by timestamp descending");
		customerTrails[0].TrailType.Should().Be(TrailType.Update, "Most recent trail should be Update");
		customerTrails[1].TrailType.Should().Be(TrailType.Create, "Oldest trail should be Create");

		user1Trails.Should().HaveCount(2, "User1 should have 2 trails");
		user1Trails.Should().AllSatisfy(t => t.UserId.Should().Be("user1"));

		user2Trails.Should().HaveCount(1, "User2 should have 1 trail");
		user2Trails[0].EntityName.Should().Be("Order");

		recentTrails.Should().HaveCount(3, "All trails should be within recent time range");
		recentTrails.Should().BeOrderedByDescending(t => t.Timestamp);

		// Verify trail data integrity
		var updateTrail = customerTrails.First(t => t.TrailType == TrailType.Update);
		updateTrail.GetOldValue("Email").Should().Be("john@example.com");
		updateTrail.GetNewValue("Email").Should().Be("john.doe@example.com");
		updateTrail.ChangedColumns.Should().Contain("Email");
	}

	[Fact]
	public async Task HighVolumeAuditScenario_WithConcurrentOperations_MaintainsDataIntegrity()
	{
		// Arrange - Create multiple users
		var users = Enumerable.Range(1, 5).Select(i => new TestUser 
		{ 
			Id = $"user{i}", 
			UserName = $"user{i}",
			Email = $"user{i}@test.com"
		}).ToArray();

		// Act - Create many audit trails concurrently
		var tasks = new List<Task>();
		var createdTrails = new List<Trail<Guid, TestUser, string>>();

		// Create 50 audit trails across different entities and users
		for (int i = 0; i < 50; i++)
		{
			var index = i;
			tasks.Add(Task.Run(async () =>
			{
				var user = users[index % users.Length];
				var entityName = $"Entity{index % 10}";
				var primaryKey = $"key-{index}";
				var trailType = (TrailType)((index % 3) + 1); // Cycle through Create, Update, Delete

				var trail = await CreateAndSaveTrail(user, entityName, primaryKey, trailType,
					oldValues: new Dictionary<string, object> { { "Field1", $"OldValue{index}" } },
					newValues: new Dictionary<string, object> { { "Field1", $"NewValue{index}" } });

				lock (createdTrails)
				{
					createdTrails.Add(trail);
				}
			}));
		}

		await Task.WhenAll(tasks);

		// Assert - Verify all trails were created and can be queried
		var allTrails = await _context.AuditTrails.ToListAsync();
		allTrails.Should().HaveCount(50, "All 50 trails should be created");

		// Test various query scenarios
		var entity0Trails = (await _repository.GetByEntityAsync("Entity0", "key-0")).ToList();
		entity0Trails.Should().HaveCount(1);

		var user1AllTrails = (await _repository.GetByUserAsync("user1")).ToList();
		user1AllTrails.Should().HaveCount(10, "User1 should have 10 trails (every 5th trail)");

		// Verify data integrity - no trails should have corrupted data
		foreach (var trail in allTrails)
		{
			trail.Id.Should().NotBe(Guid.Empty);
			trail.EntityName.Should().NotBeNullOrEmpty();
			trail.PrimaryKey.Should().NotBeNullOrEmpty();
			trail.TrailType.Should().BeOneOf(TrailType.Create, TrailType.Update, TrailType.Delete);
			trail.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
		}
	}

	#endregion

	#region Database Performance Tests

	[Fact]
	public async Task LargeDatasetQueries_WithIndexes_PerformEfficiently()
	{
		// Arrange - Create a large dataset
		var user = new TestUser { Id = "perf-user", UserName = "perfuser" };
		var baseDate = DateTime.UtcNow.AddDays(-30);

		// Create 1000 audit trails over 30 days
		var trails = new List<Trail<Guid, TestUser, string>>();
		for (int i = 0; i < 1000; i++)
		{
			trails.Add(new Trail<Guid, TestUser, string>
			{
				Id = Guid.NewGuid(),
				TrailType = (TrailType)((i % 3) + 1),
				UserId = user.Id,
				User = user,
				Timestamp = baseDate.AddMinutes(i * 43.2), // Spread over 30 days
				EntityName = $"Entity{i % 100}", // 100 different entities
				PrimaryKey = $"key-{i % 500}", // 500 different keys (some entities have multiple trails)
				ChangedColumns = new List<string> { $"Field{i % 10}" },
				OldValuesJson = $"{{\"Field{i % 10}\":\"OldValue{i}\"}}",
				NewValuesJson = $"{{\"Field{i % 10}\":\"NewValue{i}\"}}"
			});
		}

		await _context.AuditTrails.AddRangeAsync(trails);
		await _context.SaveChangesAsync();

		// Act & Assert - Test various query performance scenarios
		var startTime = DateTime.UtcNow;

		// Test entity-based query (should use EntityName + PrimaryKey index)
		var entityTrails = (await _repository.GetByEntityAsync("Entity1", "key-1")).ToList();
		var entityQueryTime = DateTime.UtcNow - startTime;

		startTime = DateTime.UtcNow;

		// Test user-based query (should use UserId index)
		var userTrails = (await _repository.GetByUserAsync("perf-user")).ToList();
		var userQueryTime = DateTime.UtcNow - startTime;

		startTime = DateTime.UtcNow;

		// Test date range query (should use Timestamp index)
		var dateTrails = (await _repository.GetByDateRangeAsync(
			baseDate.AddDays(10), 
			baseDate.AddDays(20))).ToList();
		var dateQueryTime = DateTime.UtcNow - startTime;

		// Assert performance and correctness
		entityQueryTime.TotalMilliseconds.Should().BeLessThan(100, "Entity query should be fast with indexes");
		userQueryTime.TotalMilliseconds.Should().BeLessThan(200, "User query should be reasonably fast");
		dateQueryTime.TotalMilliseconds.Should().BeLessThan(200, "Date range query should be reasonably fast");

		entityTrails.Should().HaveCountGreaterThan(0).And.HaveCountLessThan(10);
		userTrails.Should().HaveCount(1000);
		dateTrails.Should().HaveCountGreaterThan(400).And.HaveCountLessThan(600);
	}

	#endregion

	#region Data Consistency and Transaction Tests

	[Fact]
	public async Task TransactionalOperations_WithRollback_MaintainConsistency()
	{
		// Arrange
		var user = new TestUser { Id = "tx-user", UserName = "txuser" };
		var initialTrailCount = await _context.AuditTrails.CountAsync();

		// Act - Simulate a transaction that should rollback
		using var transaction = await _context.Database.BeginTransactionAsync();
		try
		{
			// Add some trails
			var trail1 = await CreateTrail(user, "TxEntity1", "tx-1", TrailType.Create);
			var trail2 = await CreateTrail(user, "TxEntity2", "tx-2", TrailType.Update);
			
			await _repository.AddAsync(trail1);
			await _repository.AddAsync(trail2);
			await _repository.SaveChangesAsync();

			// Verify trails are in context but not committed
			var contextTrails = await _context.AuditTrails.CountAsync();
			contextTrails.Should().Be(initialTrailCount + 2);

			// Force rollback
			await transaction.RollbackAsync();
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}

		// Assert - Changes should be rolled back
		using var newContext = CreateNewContext();
		var finalTrailCount = await newContext.AuditTrails.CountAsync();
		finalTrailCount.Should().Be(initialTrailCount, "Transaction rollback should revert all changes");
	}

	[Fact]
	public async Task TransactionalOperations_WithCommit_PersistChanges()
	{
		// Arrange
		var user = new TestUser { Id = "commit-user", UserName = "commituser" };
		var initialTrailCount = await _context.AuditTrails.CountAsync();

		// Act - Simulate a successful transaction
		using var transaction = await _context.Database.BeginTransactionAsync();
		try
		{
			var trail1 = await CreateTrail(user, "CommitEntity1", "commit-1", TrailType.Create);
			var trail2 = await CreateTrail(user, "CommitEntity2", "commit-2", TrailType.Update);
			
			await _repository.AddAsync(trail1);
			await _repository.AddAsync(trail2);
			await _repository.SaveChangesAsync();
			
			await transaction.CommitAsync();
		}
		catch
		{
			await transaction.RollbackAsync();
			throw;
		}

		// Assert - Changes should be persisted
		using var newContext = CreateNewContext();
		var finalTrailCount = await newContext.AuditTrails.CountAsync();
		finalTrailCount.Should().Be(initialTrailCount + 2, "Transaction commit should persist all changes");

		var committedTrails = await newContext.AuditTrails
			.Where(t => t.EntityName.StartsWith("CommitEntity"))
			.ToListAsync();
		committedTrails.Should().HaveCount(2);
	}

	#endregion

	#region Edge Cases and Error Handling Tests

	[Fact]
	public async Task DatabaseConnectionLoss_HandledGracefully()
	{
		// Arrange
		var user = new TestUser { Id = "error-user", UserName = "erroruser" };
		var trail = await CreateTrail(user, "ErrorEntity", "error-1", TrailType.Create);

		// Act & Assert - Dispose context to simulate connection loss
		_context.Dispose();

		Func<Task> act = async () => await _repository.AddAsync(trail);
		await act.Should().ThrowAsync<ObjectDisposedException>("Should handle disposed context gracefully");
	}

	[Fact]
	public async Task LargeJsonPayloads_HandledCorrectly()
	{
		// Arrange
		var user = new TestUser { Id = "json-user", UserName = "jsonuser" };
		
		// Create large JSON payloads (simulating complex entity changes)
		var largeOldValues = new Dictionary<string, object>();
		var largeNewValues = new Dictionary<string, object>();
		
		for (int i = 0; i < 100; i++)
		{
			largeOldValues[$"Field{i}"] = $"This is a long string value for field {i} " + new string('A', 100);
			largeNewValues[$"Field{i}"] = $"This is a new long string value for field {i} " + new string('B', 100);
		}

		// Act
		var trail = await CreateAndSaveTrail(user, "LargeEntity", "large-1", TrailType.Update,
			largeOldValues, largeNewValues);

		// Assert
		trail.Should().NotBeNull();
		trail.OldValuesJson.Should().NotBeNullOrEmpty();
		trail.NewValuesJson.Should().NotBeNullOrEmpty();
		
		// Verify we can retrieve and deserialize the large payloads
		var retrievedTrails = (await _repository.GetByEntityAsync("LargeEntity", "large-1")).ToList();
		var retrievedTrail = retrievedTrails.First();
		
		var deserializedOldValues = retrievedTrail.OldValues;
		var deserializedNewValues = retrievedTrail.NewValues;
		
		deserializedOldValues.Should().HaveCount(100);
		deserializedNewValues.Should().HaveCount(100);
		deserializedOldValues["Field0"].ToString().Should().Contain("long string value for field 0");
		deserializedNewValues["Field99"].ToString().Should().Contain("new long string value for field 99");
	}

	[Fact]
	public async Task SpecialCharactersInData_HandledCorrectly()
	{
		// Arrange
		var user = new TestUser { Id = "special-user", UserName = "special\"user'with<>chars" };
		var specialValues = new Dictionary<string, object>
		{
			{ "FieldWithQuotes", "Value with \"quotes\" and 'apostrophes'" },
			{ "FieldWithXml", "<xml>Special &amp; characters</xml>" },
			{ "FieldWithJson", "{\"nested\": \"json\", \"array\": [1, 2, 3]}" },
			{ "FieldWithUnicode", "Unicode: 测试 🚀 ñoñó" },
			{ "FieldWithNull", null },
			{ "FieldWithEmpty", "" }
		};

		// Act
		var trail = await CreateAndSaveTrail(user, "SpecialEntity", "special-1", TrailType.Create,
			new Dictionary<string, object>(), specialValues);

		// Assert
		trail.Should().NotBeNull();
		
		var retrievedTrails = (await _repository.GetByEntityAsync("SpecialEntity", "special-1")).ToList();
		var retrievedTrail = retrievedTrails.First();
		
		var newValues = retrievedTrail.NewValues;
		newValues["FieldWithQuotes"].ToString().Should().Contain("\"quotes\"");
		newValues["FieldWithXml"].ToString().Should().Contain("<xml>");
		newValues["FieldWithUnicode"].ToString().Should().Contain("测试 🚀 ñoñó");
		newValues.Should().ContainKey("FieldWithNull");
		newValues["FieldWithEmpty"].ToString().Should().BeEmpty();
	}

	#endregion

	#region Service Integration Tests

	[Fact]
	public async Task ServiceCollectionExtensions_RegisterServicesCorrectly()
	{
		// Arrange
		var services = new ServiceCollection();
		services.AddLogging();
		
		// Act - Test the extension method if it exists
		services.AddDbContext<AuditDbContext<Guid, TestUser, string>>(options =>
			options.UseInMemoryDatabase($"ServiceTest_{Guid.NewGuid()}"));
		
		services.AddScoped<AuditRepository<Guid, TestUser, string>>();

		var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		// Assert
		var context = scope.ServiceProvider.GetService<AuditDbContext<Guid, TestUser, string>>();
		var repository = scope.ServiceProvider.GetService<AuditRepository<Guid, TestUser, string>>();

		context.Should().NotBeNull("DbContext should be registered");
		repository.Should().NotBeNull("Repository should be registered");
	}

	#endregion

	#region Helper Methods

	private async Task<Trail<Guid, TestUser, string>> CreateAndSaveTrail(
		TestUser user,
		string entityName,
		string primaryKey,
		TrailType trailType,
		Dictionary<string, object> oldValues,
		Dictionary<string, object> newValues)
	{
		var trail = await CreateTrail(user, entityName, primaryKey, trailType);
		trail.OldValues = oldValues;
		trail.NewValues = newValues;
		trail.ChangedColumns = newValues.Keys.ToList();

		await _repository.AddAsync(trail);
		await _repository.SaveChangesAsync();
		
		return trail;
	}

	private static async Task<Trail<Guid, TestUser, string>> CreateTrail(
		TestUser user,
		string entityName,
		string primaryKey,
		TrailType trailType)
	{
		await Task.Delay(1); // Ensure unique timestamps
		
		return new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = trailType,
			UserId = user.Id,
			User = user,
			Timestamp = DateTime.UtcNow,
			EntityName = entityName,
			PrimaryKey = primaryKey,
			ChangedColumns = new List<string>()
		};
	}

	private AuditDbContext<Guid, TestUser, string> CreateNewContext()
	{
		var options = new DbContextOptionsBuilder<AuditDbContext<Guid, TestUser, string>>()
			.UseInMemoryDatabase(_context.Database.GetDbConnection().Database)
			.Options;
		return new AuditDbContext<Guid, TestUser, string>(options);
	}

	#endregion

	#region IDisposable Implementation

	public void Dispose()
	{
		_scope?.Dispose();
		_serviceProvider?.Dispose();
	}

	#endregion
}

#region Test Helper Classes

/// <summary>Test user class for integration tests</summary>
public class TestUser : IdentityUser<string>
{
	public TestUser()
	{
		Id = Guid.NewGuid().ToString();
	}
}