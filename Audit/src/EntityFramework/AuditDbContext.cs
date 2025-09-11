// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Wangkanai.Audit.EntityFramework.Extensions;

namespace Wangkanai.Audit.EntityFramework;

/// <summary>Database context for audit trail operations.</summary>
/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
public class AuditDbContext<TKey, TUserType, TUserKey> : DbContext
	where TKey : IEquatable<TKey>, IComparable<TKey>
	where TUserType : IdentityUser<TUserKey>
	where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
	/// <summary>Initializes a new instance of the <see cref="AuditDbContext{TKey, TUserType, TUserKey}"/> class.</summary>
	/// <param name="options">The options for this context.</param>
	public AuditDbContext(DbContextOptions<AuditDbContext<TKey, TUserType, TUserKey>> options)
		: base(options)
	{
	}

	/// <summary>Gets or sets the audit trails.</summary>
	public DbSet<Trail<TKey, TUserType, TUserKey>> AuditTrails => Set<Trail<TKey, TUserType, TUserKey>>();

	/// <summary>Configures the model that was discovered from the entity types exposed in <see cref="DbSet{TEntity}"/> properties on your derived context.</summary>
	/// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		ConfigureAuditTrail(modelBuilder);
	}

	/// <summary>Configures the audit trail entity using extension methods pattern.</summary>
	/// <param name="modelBuilder">The model builder.</param>
	private void ConfigureAuditTrail(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Trail<TKey, TUserType, TUserKey>>()
			.ConfigureAuditTrail();
	}
}