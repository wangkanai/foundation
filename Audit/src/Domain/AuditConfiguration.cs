// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>
/// Configuration class that encapsulates user-related type information for audit operations.
/// This pattern replaces multiple generic parameters with a single configuration object,
/// reducing generic type complexity while maintaining type safety.
/// </summary>
/// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
public sealed class AuditConfiguration<TUser, TUserKey>
   where TUser : IdentityUser<TUserKey>
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   /// <summary>Gets the type of the user entity.</summary>
   public Type UserType => typeof(TUser);

   /// <summary>Gets the type of the user key.</summary>
   public Type UserKeyType => typeof(TUserKey);

   /// <summary>
   /// Creates a new instance of the audit configuration.
   /// </summary>
   public static AuditConfiguration<TUser, TUserKey> Create() => new();

   /// <summary>
   /// Validates that the provided user is of the correct type.
   /// </summary>
   /// <param name="user">The user to validate.</param>
   /// <returns>True if the user is valid; otherwise, false.</returns>
   public bool IsValidUser(object? user) => user is TUser;

   /// <summary>
   /// Validates that the provided user key is of the correct type.
   /// </summary>
   /// <param name="userKey">The user key to validate.</param>
   /// <returns>True if the user key is valid; otherwise, false.</returns>
   public bool IsValidUserKey(object? userKey) => userKey is TUserKey;

   /// <summary>
   /// Safely casts a user object to the configured user type.
   /// </summary>
   /// <param name="user">The user object to cast.</param>
   /// <returns>The user cast to the correct type, or null if casting fails.</returns>
   public TUser? CastUser(object? user) => user as TUser;

   /// <summary>
   /// Safely casts a user key object to the configured user key type.
   /// </summary>
   /// <param name="userKey">The user key object to cast.</param>
   /// <returns>The user key cast to the correct type, or default if casting fails.</returns>
   public TUserKey? CastUserKey(object? userKey) => userKey is TUserKey key ? key : default;

   /// <summary>
   /// Gets the default value for the user key type.
   /// </summary>
   public TUserKey DefaultUserKey => default!;

   /// <summary>
   /// Determines whether the user key equals its default value.
   /// </summary>
   /// <param name="userKey">The user key to check.</param>
   /// <returns>True if the user key is the default value; otherwise, false.</returns>
   public bool IsDefaultUserKey(TUserKey? userKey) => EqualityComparer<TUserKey>.Default.Equals(userKey, DefaultUserKey);
}

/// <summary>
/// Non-generic interface for audit configuration to enable type-agnostic operations.
/// </summary>
public interface IAuditConfiguration
{
   /// <summary>Gets the type of the user entity.</summary>
   Type UserType { get; }

   /// <summary>Gets the type of the user key.</summary>
   Type UserKeyType { get; }

   /// <summary>
   /// Validates that the provided user is of the correct type.
   /// </summary>
   /// <param name="user">The user to validate.</param>
   /// <returns>True if the user is valid; otherwise, false.</returns>
   bool IsValidUser(object? user);

   /// <summary>
   /// Validates that the provided user key is of the correct type.
   /// </summary>
   /// <param name="userKey">The user key to validate.</param>
   /// <returns>True if the user key is valid; otherwise, false.</returns>
   bool IsValidUserKey(object? userKey);
}

/// <summary>
/// Extension to make AuditConfiguration implement the non-generic interface.
/// </summary>
public static class AuditConfigurationExtensions
{
   /// <summary>
   /// Converts a typed audit configuration to the non-generic interface.
   /// </summary>
   /// <typeparam name="TUser">The user type.</typeparam>
   /// <typeparam name="TUserKey">The user key type.</typeparam>
   /// <param name="config">The configuration to convert.</param>
   /// <returns>The configuration as a non-generic interface.</returns>
   public static IAuditConfiguration AsInterface<TUser, TUserKey>(
      this AuditConfiguration<TUser, TUserKey> config)
      where TUser : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
      => new AuditConfigurationAdapter<TUser, TUserKey>(config);
}

/// <summary>
/// Adapter to implement the non-generic interface for audit configuration.
/// </summary>
internal sealed class AuditConfigurationAdapter<TUser, TUserKey>(AuditConfiguration<TUser, TUserKey> config)
   : IAuditConfiguration
   where TUser : IdentityUser<TUserKey>
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   public Type UserType    => config.UserType;
   public Type UserKeyType => config.UserKeyType;

   public bool IsValidUser(object?    user)    => config.IsValidUser(user);
   public bool IsValidUserKey(object? userKey) => config.IsValidUserKey(userKey);
}