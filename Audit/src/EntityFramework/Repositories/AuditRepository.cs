// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wangkanai.Audit.Repositories;

namespace Wangkanai.Audit.EntityFramework.Repositories;

/// <summary>Entity Framework implementation of the audit repository.</summary>
/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
public class AuditRepository<TKey, TUserType, TUserKey> : IAuditRepository<TKey, TUserType, TUserKey>
	where TKey : IEquatable<TKey>, IComparable<TKey>
	where TUserType : IdentityUser<TUserKey>
	where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
	private readonly AuditDbContext<TKey, TUserType, TUserKey> _context;
	private readonly ILogger<AuditRepository<TKey, TUserType, TUserKey>> _logger;

	/// <summary>Initializes a new instance of the <see cref="AuditRepository{TKey, TUserType, TUserKey}"/> class.</summary>
	/// <param name="context">The database context.</param>
	/// <param name="logger">The logger.</param>
	public AuditRepository(
		AuditDbContext<TKey, TUserType, TUserKey> context,
		ILogger<AuditRepository<TKey, TUserType, TUserKey>> logger)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc />
	public async Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByEntityAsync(
		string entityName, 
		string primaryKey, 
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(entityName))
			throw new ArgumentException("Entity name cannot be null or empty.", nameof(entityName));
		
		if (string.IsNullOrEmpty(primaryKey))
			throw new ArgumentException("Primary key cannot be null or empty.", nameof(primaryKey));

		_logger.LogDebug("Retrieving audit trails for entity: {EntityName}, PrimaryKey: {PrimaryKey}", 
			entityName, primaryKey);

		try
		{
			var trails = await _context.AuditTrails
				.Where(t => t.EntityName == entityName && t.PrimaryKey == primaryKey)
				.Include(t => t.User)
				.OrderByDescending(t => t.Timestamp)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			_logger.LogDebug("Retrieved {Count} audit trails for entity: {EntityName}, PrimaryKey: {PrimaryKey}", 
				trails.Count, entityName, primaryKey);

			return trails;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving audit trails for entity: {EntityName}, PrimaryKey: {PrimaryKey}", 
				entityName, primaryKey);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByUserAsync(
		TUserKey userId, 
		CancellationToken cancellationToken = default)
	{
		if (userId == null)
			throw new ArgumentNullException(nameof(userId));

		_logger.LogDebug("Retrieving audit trails for user: {UserId}", userId);

		try
		{
			var trails = await _context.AuditTrails
				.Where(t => t.UserId != null && t.UserId.Equals(userId))
				.Include(t => t.User)
				.OrderByDescending(t => t.Timestamp)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			_logger.LogDebug("Retrieved {Count} audit trails for user: {UserId}", 
				trails.Count, userId);

			return trails;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving audit trails for user: {UserId}", userId);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByDateRangeAsync(
		DateTime startDate, 
		DateTime endDate, 
		CancellationToken cancellationToken = default)
	{
		if (startDate >= endDate)
			throw new ArgumentException("Start date must be earlier than end date.");

		_logger.LogDebug("Retrieving audit trails for date range: {StartDate} to {EndDate}", 
			startDate, endDate);

		try
		{
			var trails = await _context.AuditTrails
				.Where(t => t.Timestamp >= startDate && t.Timestamp <= endDate)
				.Include(t => t.User)
				.OrderByDescending(t => t.Timestamp)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			_logger.LogDebug("Retrieved {Count} audit trails for date range: {StartDate} to {EndDate}", 
				trails.Count, startDate, endDate);

			return trails;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving audit trails for date range: {StartDate} to {EndDate}", 
				startDate, endDate);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<Trail<TKey, TUserType, TUserKey>> AddAsync(
		Trail<TKey, TUserType, TUserKey> trail, 
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(trail);

		_logger.LogDebug("Adding audit trail for entity: {EntityName}, PrimaryKey: {PrimaryKey}, TrailType: {TrailType}", 
			trail.EntityName, trail.PrimaryKey, trail.TrailType);

		try
		{
			var entry = await _context.AuditTrails.AddAsync(trail, cancellationToken);
			
			_logger.LogDebug("Successfully added audit trail with ID: {TrailId}", trail.Id);
			
			return entry.Entity;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error adding audit trail for entity: {EntityName}, PrimaryKey: {PrimaryKey}", 
				trail.EntityName, trail.PrimaryKey);
			throw;
		}
	}

	/// <inheritdoc />
	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		_logger.LogDebug("Saving audit trail changes to database");

		try
		{
			var result = await _context.SaveChangesAsync(cancellationToken);
			
			_logger.LogDebug("Successfully saved {Count} audit trail changes to database", result);
			
			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error saving audit trail changes to database");
			throw;
		}
	}
}