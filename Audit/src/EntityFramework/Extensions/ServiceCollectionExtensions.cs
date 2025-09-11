// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wangkanai.Audit.EntityFramework.Repositories;
using Wangkanai.Audit.Repositories;

namespace Wangkanai.Audit.EntityFramework.Extensions;

/// <summary>Extension methods for configuring audit services in dependency injection container.</summary>
public static class ServiceCollectionExtensions
{
	/// <summary>Adds audit repository services to the service collection.</summary>
	/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
	/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
	/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="configureDbContext">Optional database context configuration action.</param>
	/// <returns>The service collection for method chaining.</returns>
	public static IServiceCollection AddAuditRepository<TKey, TUserType, TUserKey>(
		this IServiceCollection services,
		Action<DbContextOptionsBuilder>? configureDbContext = null)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		// Add DbContext
		if (configureDbContext != null)
		{
			services.AddDbContext<AuditDbContext<TKey, TUserType, TUserKey>>(configureDbContext);
		}
		else
		{
			services.TryAddScoped<AuditDbContext<TKey, TUserType, TUserKey>>();
		}

		// Add Repository
		services.TryAddScoped<IAuditRepository<TKey, TUserType, TUserKey>, 
			AuditRepository<TKey, TUserType, TUserKey>>();

		return services;
	}

	/// <summary>Adds audit repository services with SQL Server configuration.</summary>
	/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
	/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
	/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="connectionString">The SQL Server connection string.</param>
	/// <returns>The service collection for method chaining.</returns>
	public static IServiceCollection AddAuditRepositorySqlServer<TKey, TUserType, TUserKey>(
		this IServiceCollection services,
		string connectionString)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		return services.AddAuditRepository<TKey, TUserType, TUserKey>(options =>
			options.UseSqlServer(connectionString));
	}

	/// <summary>Adds audit repository services with PostgreSQL configuration.</summary>
	/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
	/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
	/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="connectionString">The PostgreSQL connection string.</param>
	/// <returns>The service collection for method chaining.</returns>
	public static IServiceCollection AddAuditRepositoryPostgreSQL<TKey, TUserType, TUserKey>(
		this IServiceCollection services,
		string connectionString)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		return services.AddAuditRepository<TKey, TUserType, TUserKey>(options =>
			options.UseNpgsql(connectionString));
	}

	/// <summary>Adds audit repository services with in-memory database configuration (for testing).</summary>
	/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
	/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
	/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
	/// <param name="services">The service collection.</param>
	/// <param name="databaseName">The in-memory database name.</param>
	/// <returns>The service collection for method chaining.</returns>
	public static IServiceCollection AddAuditRepositoryInMemory<TKey, TUserType, TUserKey>(
		this IServiceCollection services,
		string databaseName = "AuditTestDb")
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		return services.AddAuditRepository<TKey, TUserType, TUserKey>(options =>
			options.UseInMemoryDatabase(databaseName));
	}
}