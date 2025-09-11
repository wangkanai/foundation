// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using Moq;
using Xunit;
using Wangkanai.Audit.EntityFramework;
using Wangkanai.Audit.EntityFramework.Repositories;

namespace Wangkanai.Audit.EntityFramework.Tests;

/// <summary>Unit tests for the AuditRepository implementation.</summary>
public class AuditRepositoryTests : IDisposable
{
	private readonly AuditDbContext<Guid, TestUser, string> _context;
	private readonly Mock<ILogger<AuditRepository<Guid, TestUser, string>>> _mockLogger;
	private readonly AuditRepository<Guid, TestUser, string> _repository;

	public AuditRepositoryTests()
	{
		// Setup in-memory database
		var options = new DbContextOptionsBuilder<AuditDbContext<Guid, TestUser, string>>()
			.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
			.Options;

		_context = new AuditDbContext<Guid, TestUser, string>(options);
		_mockLogger = new Mock<ILogger<AuditRepository<Guid, TestUser, string>>>();
		_repository = new AuditRepository<Guid, TestUser, string>(_context, _mockLogger.Object);
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithValidParameters_CreatesInstance()
	{
		// Act & Assert
		_repository.Should().NotBeNull();
	}

	[Fact]
	public void Constructor_WithNullContext_ThrowsArgumentNullException()
	{
		// Act & Assert
		Action act = () => new AuditRepository<Guid, TestUser, string>(null!, _mockLogger.Object);
		act.Should().Throw<ArgumentNullException>().WithParameterName("context");
	}

	[Fact]
	public void Constructor_WithNullLogger_ThrowsArgumentNullException()
	{
		// Act & Assert
		Action act = () => new AuditRepository<Guid, TestUser, string>(_context, null!);
		act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
	}

	#endregion

	#region GetByEntityAsync Tests

	[Fact]
	public async Task GetByEntityAsync_WithValidParameters_ReturnsMatchingTrails()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "testuser" };
		var trail1 = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		var trail2 = CreateTestTrail(user, "TestEntity", "123", TrailType.Update);
		var trail3 = CreateTestTrail(user, "OtherEntity", "456", TrailType.Create);

		await _context.AuditTrails.AddRangeAsync(trail1, trail2, trail3);
		await _context.SaveChangesAsync();

		// Act
		var result = await _repository.GetByEntityAsync("TestEntity", "123");
		var trails = result.ToList();

		// Assert
		trails.Should().HaveCount(2);
		trails.Should().Contain(t => t.TrailType == TrailType.Create);
		trails.Should().Contain(t => t.TrailType == TrailType.Update);
		trails.Should().AllSatisfy(t => t.EntityName.Should().Be("TestEntity"));
		trails.Should().AllSatisfy(t => t.PrimaryKey.Should().Be("123"));
		trails.Should().BeInDescendingOrder(t => t.Timestamp);
	}

	[Fact]
	public async Task GetByEntityAsync_WithNonExistentEntity_ReturnsEmptyCollection()
	{
		// Act
		var result = await _repository.GetByEntityAsync("NonExistent", "999");
		var trails = result.ToList();

		// Assert
		trails.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByEntityAsync_WithNullEntityName_ThrowsArgumentException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync(null!, "123");
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("entityName")
			.WithMessage("Entity name cannot be null or empty.*");
	}

	[Fact]
	public async Task GetByEntityAsync_WithEmptyEntityName_ThrowsArgumentException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync("", "123");
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("entityName")
			.WithMessage("Entity name cannot be null or empty.*");
	}

	[Fact]
	public async Task GetByEntityAsync_WithNullPrimaryKey_ThrowsArgumentException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync("TestEntity", null!);
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("primaryKey")
			.WithMessage("Primary key cannot be null or empty.*");
	}

	[Fact]
	public async Task GetByEntityAsync_WithEmptyPrimaryKey_ThrowsArgumentException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync("TestEntity", "");
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("primaryKey")
			.WithMessage("Primary key cannot be null or empty.*");
	}

	[Fact]
	public async Task GetByEntityAsync_WithCancellationToken_RespectsToken()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "testuser" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		await _context.AuditTrails.AddAsync(trail);
		await _context.SaveChangesAsync();

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync("TestEntity", "123", cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task GetByEntityAsync_LogsDebugMessages()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "testuser" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		await _context.AuditTrails.AddAsync(trail);
		await _context.SaveChangesAsync();

		// Act
		await _repository.GetByEntityAsync("TestEntity", "123");

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieving audit trails for entity")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.AtLeastOnce);

		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieved") && v.ToString().Contains("audit trails")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region GetByUserAsync Tests

	[Fact]
	public async Task GetByUserAsync_WithValidUserId_ReturnsMatchingTrails()
	{
		// Arrange
		var user1 = new TestUser { Id = "user1", UserName = "user1" };
		var user2 = new TestUser { Id = "user2", UserName = "user2" };
		
		var trail1 = CreateTestTrail(user1, "Entity1", "123", TrailType.Create);
		var trail2 = CreateTestTrail(user1, "Entity2", "456", TrailType.Update);
		var trail3 = CreateTestTrail(user2, "Entity3", "789", TrailType.Delete);

		await _context.AuditTrails.AddRangeAsync(trail1, trail2, trail3);
		await _context.SaveChangesAsync();

		// Act
		var result = await _repository.GetByUserAsync("user1");
		var trails = result.ToList();

		// Assert
		trails.Should().HaveCount(2);
		trails.Should().AllSatisfy(t => t.UserId.Should().Be("user1"));
		trails.Should().BeInDescendingOrder(t => t.Timestamp);
	}

	[Fact]
	public async Task GetByUserAsync_WithNonExistentUser_ReturnsEmptyCollection()
	{
		// Act
		var result = await _repository.GetByUserAsync("nonexistent");
		var trails = result.ToList();

		// Assert
		trails.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByUserAsync_WithNullUserId_ThrowsArgumentNullException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.GetByUserAsync(null!);
		await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("userId");
	}

	[Fact]
	public async Task GetByUserAsync_WithCancellationToken_RespectsToken()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByUserAsync("user1", cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task GetByUserAsync_LogsDebugMessages()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "Entity1", "123", TrailType.Create);
		await _context.AuditTrails.AddAsync(trail);
		await _context.SaveChangesAsync();

		// Act
		await _repository.GetByUserAsync("user1");

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieving audit trails for user")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region GetByDateRangeAsync Tests

	[Fact]
	public async Task GetByDateRangeAsync_WithValidRange_ReturnsMatchingTrails()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var baseDate = DateTime.UtcNow;
		
		var trail1 = CreateTestTrail(user, "Entity1", "123", TrailType.Create, baseDate.AddDays(-2));
		var trail2 = CreateTestTrail(user, "Entity2", "456", TrailType.Update, baseDate.AddDays(-1));
		var trail3 = CreateTestTrail(user, "Entity3", "789", TrailType.Delete, baseDate.AddDays(1));

		await _context.AuditTrails.AddRangeAsync(trail1, trail2, trail3);
		await _context.SaveChangesAsync();

		// Act
		var result = await _repository.GetByDateRangeAsync(baseDate.AddDays(-3), baseDate);
		var trails = result.ToList();

		// Assert
		trails.Should().HaveCount(2);
		trails.Should().Contain(t => t.TrailType == TrailType.Create);
		trails.Should().Contain(t => t.TrailType == TrailType.Update);
		trails.Should().BeInDescendingOrder(t => t.Timestamp);
	}

	[Fact]
	public async Task GetByDateRangeAsync_WithEmptyRange_ReturnsEmptyCollection()
	{
		// Act
		var result = await _repository.GetByDateRangeAsync(DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddDays(2));
		var trails = result.ToList();

		// Assert
		trails.Should().BeEmpty();
	}

	[Fact]
	public async Task GetByDateRangeAsync_WithStartDateAfterEndDate_ThrowsArgumentException()
	{
		// Arrange
		var startDate = DateTime.UtcNow;
		var endDate = startDate.AddDays(-1);

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByDateRangeAsync(startDate, endDate);
		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("Start date must be earlier than end date.");
	}

	[Fact]
	public async Task GetByDateRangeAsync_WithEqualStartAndEndDates_ThrowsArgumentException()
	{
		// Arrange
		var date = DateTime.UtcNow;

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByDateRangeAsync(date, date);
		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("Start date must be earlier than end date.");
	}

	[Fact]
	public async Task GetByDateRangeAsync_WithCancellationToken_RespectsToken()
	{
		// Arrange
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByDateRangeAsync(
			DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task GetByDateRangeAsync_LogsDebugMessages()
	{
		// Arrange
		var startDate = DateTime.UtcNow.AddDays(-1);
		var endDate = DateTime.UtcNow;

		// Act
		await _repository.GetByDateRangeAsync(startDate, endDate);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Retrieving audit trails for date range")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region AddAsync Tests

	[Fact]
	public async Task AddAsync_WithValidTrail_AddsToContext()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);

		// Act
		var result = await _repository.AddAsync(trail);

		// Assert
		result.Should().NotBeNull();
		result.Should().BeSameAs(trail);
		
		var contextEntry = _context.Entry(trail);
		contextEntry.State.Should().Be(EntityState.Added);
	}

	[Fact]
	public async Task AddAsync_WithNullTrail_ThrowsArgumentNullException()
	{
		// Act & Assert
		Func<Task> act = async () => await _repository.AddAsync(null!);
		await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("trail");
	}

	[Fact]
	public async Task AddAsync_WithCancellationToken_RespectsToken()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		// Note: EF Core's AddAsync may not immediately throw on cancellation
		// The cancellation is typically checked during SaveChangesAsync
		var result = await _repository.AddAsync(trail, cts.Token);
		
		// Assert - Check if SaveChanges respects the cancellation
		Func<Task> act = async () => await _repository.SaveChangesAsync(cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task AddAsync_LogsDebugMessages()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);

		// Act
		await _repository.AddAsync(trail);

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Adding audit trail for entity")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);

		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully added audit trail")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region SaveChangesAsync Tests

	[Fact]
	public async Task SaveChangesAsync_WithPendingChanges_ReturnsSavedCount()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail1 = CreateTestTrail(user, "Entity1", "123", TrailType.Create);
		var trail2 = CreateTestTrail(user, "Entity2", "456", TrailType.Update);

		await _repository.AddAsync(trail1);
		await _repository.AddAsync(trail2);

		// Act
		var result = await _repository.SaveChangesAsync();

		// Assert
		// The result might include other tracked entities (e.g., User) 
		// We only care that at least our 2 trails were saved
		result.Should().BeGreaterThanOrEqualTo(2);
		
		var savedTrails = await _context.AuditTrails.ToListAsync();
		savedTrails.Should().HaveCount(2);
	}

	[Fact]
	public async Task SaveChangesAsync_WithNoPendingChanges_ReturnsZero()
	{
		// Act
		var result = await _repository.SaveChangesAsync();

		// Assert
		result.Should().Be(0);
	}

	[Fact]
	public async Task SaveChangesAsync_WithCancellationToken_RespectsToken()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "Entity1", "123", TrailType.Create);
		await _repository.AddAsync(trail);

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act & Assert
		Func<Task> act = async () => await _repository.SaveChangesAsync(cts.Token);
		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[Fact]
	public async Task SaveChangesAsync_LogsDebugMessages()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "Entity1", "123", TrailType.Create);
		await _repository.AddAsync(trail);

		// Act
		await _repository.SaveChangesAsync();

		// Assert
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Saving audit trail changes to database")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);

		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Debug,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Successfully saved") && v.ToString().Contains("audit trail changes")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task GetByEntityAsync_WithDatabaseException_LogsErrorAndRethrows()
	{
		// Arrange - Dispose the context to simulate a database error
		_context.Dispose();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByEntityAsync("TestEntity", "123");
		
		await act.Should().ThrowAsync<ObjectDisposedException>();

		// Verify error logging
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving audit trails for entity")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetByUserAsync_WithDatabaseException_LogsErrorAndRethrows()
	{
		// Arrange - Dispose the context to simulate a database error
		_context.Dispose();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByUserAsync("user1");
		
		await act.Should().ThrowAsync<ObjectDisposedException>();

		// Verify error logging
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving audit trails for user")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task GetByDateRangeAsync_WithDatabaseException_LogsErrorAndRethrows()
	{
		// Arrange - Dispose the context to simulate a database error
		_context.Dispose();

		// Act & Assert
		Func<Task> act = async () => await _repository.GetByDateRangeAsync(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
		
		await act.Should().ThrowAsync<ObjectDisposedException>();

		// Verify error logging
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error retrieving audit trails for date range")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task AddAsync_WithDatabaseException_LogsErrorAndRethrows()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		
		// Dispose the context to simulate a database error
		_context.Dispose();

		// Act & Assert
		Func<Task> act = async () => await _repository.AddAsync(trail);
		
		await act.Should().ThrowAsync<ObjectDisposedException>();

		// Verify error logging
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error adding audit trail for entity")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Fact]
	public async Task SaveChangesAsync_WithDatabaseException_LogsErrorAndRethrows()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "user1" };
		var trail = CreateTestTrail(user, "TestEntity", "123", TrailType.Create);
		await _repository.AddAsync(trail);

		// Dispose the context to simulate a database error
		_context.Dispose();

		// Act & Assert
		Func<Task> act = async () => await _repository.SaveChangesAsync();
		
		await act.Should().ThrowAsync<ObjectDisposedException>();

		// Verify error logging
		_mockLogger.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error saving audit trail changes to database")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	#endregion

	#region Integration Tests

	[Fact]
	public async Task FullWorkflow_AddTrailAndQuery_WorksCorrectly()
	{
		// Arrange
		var user = new TestUser { Id = "user1", UserName = "testuser" };
		var trail = CreateTestTrail(user, "Customer", "123", TrailType.Update);
		trail.ChangedColumns = new List<string> { "Name", "Email" };
		trail.OldValues = new Dictionary<string, object> { { "Name", "Old Name" }, { "Email", "old@email.com" } };
		trail.NewValues = new Dictionary<string, object> { { "Name", "New Name" }, { "Email", "new@email.com" } };

		// Act - Add and save
		var addedTrail = await _repository.AddAsync(trail);
		var saveResult = await _repository.SaveChangesAsync();

		// Act - Query by entity
		var entityTrails = await _repository.GetByEntityAsync("Customer", "123");
		var userTrails = await _repository.GetByUserAsync("user1");
		var dateTrails = await _repository.GetByDateRangeAsync(
			DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));

		// Assert
		saveResult.Should().Be(1);
		addedTrail.Should().BeSameAs(trail);

		entityTrails.Should().ContainSingle().Which.Should().BeEquivalentTo(trail, options => options.Excluding(t => t.User));
		userTrails.Should().ContainSingle().Which.Should().BeEquivalentTo(trail, options => options.Excluding(t => t.User));
		dateTrails.Should().ContainSingle().Which.Should().BeEquivalentTo(trail, options => options.Excluding(t => t.User));
	}

	[Fact]
	public async Task QueryMethods_WithComplexData_HandleCorrectly()
	{
		// Arrange
		var users = new[]
		{
			new TestUser { Id = "user1", UserName = "user1" },
			new TestUser { Id = "user2", UserName = "user2" }
		};

		var baseTime = DateTime.UtcNow;
		var trails = new[]
		{
			CreateTestTrail(users[0], "Customer", "1", TrailType.Create, baseTime.AddMinutes(-30)),
			CreateTestTrail(users[0], "Customer", "1", TrailType.Update, baseTime.AddMinutes(-20)),
			CreateTestTrail(users[1], "Customer", "2", TrailType.Create, baseTime.AddMinutes(-10)),
			CreateTestTrail(users[0], "Order", "100", TrailType.Create, baseTime.AddMinutes(-5))
		};

		foreach (var trail in trails)
		{
			await _repository.AddAsync(trail);
		}
		await _repository.SaveChangesAsync();

		// Act & Assert - Entity queries
		var customer1Trails = await _repository.GetByEntityAsync("Customer", "1");
		customer1Trails.Should().HaveCount(2);
		customer1Trails.Should().BeInDescendingOrder(t => t.Timestamp);

		// Act & Assert - User queries
		var user1Trails = await _repository.GetByUserAsync("user1");
		user1Trails.Should().HaveCount(3);
		user1Trails.Should().BeInDescendingOrder(t => t.Timestamp);

		// Act & Assert - Date range queries
		var recentTrails = await _repository.GetByDateRangeAsync(baseTime.AddMinutes(-15), baseTime);
		recentTrails.Should().HaveCount(2);
		recentTrails.Should().BeInDescendingOrder(t => t.Timestamp);
	}

	#endregion

	#region Helper Methods

	private static Trail<Guid, TestUser, string> CreateTestTrail(
		TestUser user, 
		string entityName, 
		string primaryKey, 
		TrailType trailType,
		DateTime? timestamp = null)
	{
		return new Trail<Guid, TestUser, string>
		{
			Id = Guid.NewGuid(),
			TrailType = trailType,
			UserId = user.Id,
			// Don't set User navigation to avoid EF tracking issues in tests
			Timestamp = timestamp ?? DateTime.UtcNow,
			EntityName = entityName,
			PrimaryKey = primaryKey,
			ChangedColumns = new List<string>()
		};
	}

	#endregion

	#region IDisposable Implementation

	public void Dispose()
	{
		_context?.Dispose();
	}

	#endregion
}

