// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Wangkanai.Audit.Configurations;

/// <summary>
/// Represents the configuration settings for the <see cref="Trail{TKey, TUserType, TUserKey}"/> entity in the context of Entity Framework Core.
/// This class defines how the <see cref="Trail{TKey, TUserType, TUserKey}"/> entity is mapped to the database schema,
/// including property configurations, relationships, and column specifications.
/// </summary>
/// <typeparam name="TKey">
/// The type of the primary key for the <see cref="Trail{TKey, TUserType, TUserKey}"/> entity.
/// </typeparam>
/// <typeparam name="TUserType">
/// The type of the user entity associated with the trail. Typically a type derived from <see cref="IdentityUser{TUserKey}"/>.
/// </typeparam>
/// <typeparam name="TUserKey">
/// The type of the primary key of the user entity associated with the trail.
/// </typeparam>
public class TrailConfiguration<TKey, TUserType, TUserKey> : IEntityTypeConfiguration<Trail<TKey, TUserType, TUserKey>>
   where TKey : IEquatable<TKey>, IComparable<TKey>
   where TUserType : IdentityUser<TUserKey>
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   /// <summary>Configures the entity type for the <see cref="Trail{TKey,TUserType,TUserKey}"/> class.</summary>
   /// <param name="builder">An object that provides a simple API for configuring an entity type.</param>
   public void Configure(EntityTypeBuilder<Trail<TKey, TUserType, TUserKey>> builder)
   {
      builder.HasKey(x => x.Id);
      builder.Property(x => x.Id);

      builder.HasIndex(x => x.EntityName);
      builder.Property(x => x.EntityName)
             .HasMaxLength(100)
             .IsRequired();

      builder.Property(x => x.PrimaryKey)
             .HasMaxLength(100);

      builder.Property(x => x.Timestamp)
             .IsRequired()
             .HasConversion(c => DateTime.SpecifyKind(c, DateTimeKind.Utc),
                            c => DateTime.SpecifyKind(c, DateTimeKind.Utc));

      builder.Property(x => x.TrailType)
             .HasConversion<string>();

      builder.Property(x => x.EntityName)
             .IsRequired();

      builder.Property(x => x.ChangedColumns)
             .HasColumnType(Configuration.Jsonb)
             .HasConversion(
                            c => JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                            c => JsonSerializer.Deserialize<List<string>>(c, (JsonSerializerOptions?)null) ?? new List<string>()
                           );

      builder.Property(x => x.OldValuesJson)
             .HasColumnName(Configuration.OldValues)
             .HasColumnType(Configuration.Jsonb);
      builder.Ignore(x => x.OldValues);

      builder.Property(x => x.NewValuesJson)
             .HasColumnName(Configuration.NewValues)
             .HasColumnType(Configuration.Jsonb);
      builder.Ignore(x => x.NewValues);

      builder.Property(x => x.UserId);
      builder.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
   }
}