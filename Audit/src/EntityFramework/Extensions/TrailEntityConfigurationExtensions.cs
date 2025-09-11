// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.EntityFramework.Extensions;

/// <summary>Extension methods for configuring audit trail entities using the preferred extension methods pattern.</summary>
public static class TrailEntityConfigurationExtensions
{
	/// <summary>Configures the audit trail entity with optimal database settings.</summary>
	/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
	/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
	/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
	/// <param name="builder">The entity type builder.</param>
	/// <returns>The configured entity type builder.</returns>
	public static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigureAuditTrail<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		return builder
			.ConfigureTableName()
			.ConfigurePrimaryKey()
			.ConfigureProperties()
			.ConfigureIndexes()
			.ConfigureRelationships();
	}

	/// <summary>Configures the table name for the audit trail entity.</summary>
	private static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigureTableName<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		builder.ToTable("AuditTrails");
		return builder;
	}

	/// <summary>Configures the primary key for the audit trail entity.</summary>
	private static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigurePrimaryKey<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		builder.HasKey(t => t.Id);
		return builder;
	}

	/// <summary>Configures the properties for the audit trail entity.</summary>
	private static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigureProperties<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		builder.Property(t => t.TrailType)
			.IsRequired()
			.HasConversion<byte>();

		builder.Property(t => t.Timestamp)
			.IsRequired()
			.HasColumnType("datetime2");

		builder.Property(t => t.EntityName)
			.IsRequired()
			.HasMaxLength(256);

		builder.Property(t => t.PrimaryKey)
			.HasMaxLength(256);

		builder.Property(t => t.OldValuesJson)
			.HasColumnType("nvarchar(max)");

		builder.Property(t => t.NewValuesJson)
			.HasColumnType("nvarchar(max)");

		// Ignore computed properties
		builder.Ignore(t => t.OldValues);
		builder.Ignore(t => t.NewValues);

		return builder;
	}

	/// <summary>Configures the indexes for optimal query performance.</summary>
	private static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigureIndexes<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		// Index for entity-based queries
		builder.HasIndex(t => new { t.EntityName, t.PrimaryKey })
			.HasDatabaseName("IX_AuditTrails_Entity");

		// Index for user-based queries
		builder.HasIndex(t => t.UserId)
			.HasDatabaseName("IX_AuditTrails_UserId");

		// Index for time-based queries
		builder.HasIndex(t => t.Timestamp)
			.HasDatabaseName("IX_AuditTrails_Timestamp");

		// Composite index for common query patterns
		builder.HasIndex(t => new { t.EntityName, t.Timestamp })
			.HasDatabaseName("IX_AuditTrails_Entity_Timestamp");

		return builder;
	}

	/// <summary>Configures the relationships for the audit trail entity.</summary>
	private static EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> ConfigureRelationships<TKey, TUserType, TUserKey>(
		this EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
		where TKey : IEquatable<TKey>, IComparable<TKey>
		where TUserType : IdentityUser<TUserKey>
		where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
	{
		// Configure optional relationship with User
		builder.HasOne(t => t.User)
			.WithMany()
			.HasForeignKey(t => t.UserId)
			.OnDelete(DeleteBehavior.SetNull);

		return builder;
	}
}