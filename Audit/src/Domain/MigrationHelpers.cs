// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

/// <summary>
/// Migration helpers to assist with transitioning from the old three-generic-parameter Trail
/// to the new simplified single-generic-parameter Trail with configuration objects.
/// </summary>
[Obsolete("This class is intended for migration purposes only and will be removed in a future version.")]
public static class MigrationHelpers
{
    /// <summary>
    /// Converts an old-style trail specification to the new configuration-based approach.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <param name="userId">The user ID from the old trail.</param>
    /// <param name="user">The user from the old trail.</param>
    /// <returns>A new Trail instance configured with the provided information.</returns>
    public static Trail<TKey> ConvertFromOldTrail<TKey, TUser, TUserKey>(
        TUserKey? userId = default, 
        TUser? user = default)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create();
        var trail = new Trail<TKey>(config.AsInterface());

        if (userId != null)
            trail.SetUserId(userId);
        
        if (user != null)
            trail.SetUser(user);

        return trail;
    }

    /// <summary>
    /// Creates a conversion function that can be used to migrate existing trail creation code.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A function that creates new trails with the specified type configuration.</returns>
    public static Func<Trail<TKey>> CreateTrailFactory<TKey, TUser, TUserKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
        return () => new Trail<TKey>(config);
    }

    /// <summary>
    /// Provides a delegate-based factory for creating trails with user information.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A function that creates trails with user information.</returns>
    public static Func<TUserKey?, TUser?, Trail<TKey>> CreateTrailWithUserFactory<TKey, TUser, TUserKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
        
        return (userId, user) =>
        {
            var trail = new Trail<TKey>(config);
            
            if (userId != null)
                trail.SetUserId(userId);
            
            if (user != null)
                trail.SetUser(user);
                
            return trail;
        };
    }

    /// <summary>
    /// Batch converts a collection of old-style trail parameters to new trail instances.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <param name="oldTrailData">Collection of tuples containing old trail user data.</param>
    /// <returns>A collection of new Trail instances.</returns>
    public static IEnumerable<Trail<TKey>> BatchConvert<TKey, TUser, TUserKey>(
        IEnumerable<(TUserKey? UserId, TUser? User)> oldTrailData)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var config = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
        
        foreach (var (userId, user) in oldTrailData)
        {
            var trail = new Trail<TKey>(config);
            
            if (userId != null)
                trail.SetUserId(userId);
            
            if (user != null)
                trail.SetUser(user);
                
            yield return trail;
        }
    }

    /// <summary>
    /// Creates a configuration-aware factory for dependency injection scenarios.
    /// </summary>
    /// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>A function that can be registered with a DI container.</returns>
    public static Func<IServiceProvider, Trail<TKey>> CreateDependencyInjectionFactory<TKey, TUser, TUserKey>()
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return serviceProvider =>
        {
            var config = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
            return new Trail<TKey>(config);
        };
    }
}

/// <summary>
/// Extension methods to assist with migration from old Trail usage patterns.
/// </summary>
[Obsolete("These extensions are intended for migration purposes only and will be removed in a future version.")]
public static class MigrationExtensions
{
    /// <summary>
    /// Converts the current trail to be compatible with old three-generic-parameter expectations.
    /// This method helps in scenarios where existing code expects the old interface.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <typeparam name="TUser">The expected user type.</typeparam>
    /// <typeparam name="TUserKey">The expected user key type.</typeparam>
    /// <param name="trail">The trail instance to convert.</param>
    /// <returns>A tuple containing the user ID and user in their expected types.</returns>
    public static (TUserKey? UserId, TUser? User) AsOldTrailData<TKey, TUser, TUserKey>(
        this Trail<TKey> trail)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : class
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        var userId = trail.GetUserId<TUserKey>();
        var user = trail.GetUser<TUser>();
        
        return (userId, user);
    }

    /// <summary>
    /// Sets user information using the old three-generic-parameter pattern.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <typeparam name="TUser">The user type.</typeparam>
    /// <typeparam name="TUserKey">The user key type.</typeparam>
    /// <param name="trail">The trail instance.</param>
    /// <param name="userId">The user ID to set.</param>
    /// <param name="user">The user to set.</param>
    /// <returns>The trail instance for fluent chaining.</returns>
    public static Trail<TKey> SetOldStyleUser<TKey, TUser, TUserKey>(
        this Trail<TKey> trail, 
        TUserKey? userId, 
        TUser? user)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : class
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        if (userId != null)
            trail.SetUserId(userId);
        
        if (user != null)
            trail.SetUser(user);
            
        return trail;
    }

    /// <summary>
    /// Validates that the trail's configuration is compatible with the expected old-style types.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <typeparam name="TUser">The expected user type.</typeparam>
    /// <typeparam name="TUserKey">The expected user key type.</typeparam>
    /// <param name="trail">The trail instance to validate.</param>
    /// <returns>True if the trail is compatible with the expected types; otherwise, false.</returns>
    public static bool IsCompatibleWithOldTypes<TKey, TUser, TUserKey>(
        this Trail<TKey> trail)
        where TKey : IEquatable<TKey>, IComparable<TKey>
        where TUser : class
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        if (trail.AuditConfiguration == null)
            return true; // No configuration means no constraints
        
        var config = trail.AuditConfiguration;
        
        // Check if the configuration types match the expected types
        return config.UserType == typeof(TUser) && config.UserKeyType == typeof(TUserKey);
    }
}

/// <summary>
/// Utility class for type compatibility checks during migration.
/// </summary>
[Obsolete("This class is intended for migration purposes only and will be removed in a future version.")]
public static class TypeCompatibilityChecker
{
    /// <summary>
    /// Verifies that the new trail configuration is compatible with expected old-style usage.
    /// </summary>
    /// <typeparam name="TUser">The expected user type.</typeparam>
    /// <typeparam name="TUserKey">The expected user key type.</typeparam>
    /// <param name="auditConfiguration">The audit configuration to check.</param>
    /// <returns>True if compatible; otherwise, false.</returns>
    public static bool IsConfigurationCompatible<TUser, TUserKey>(IAuditConfiguration auditConfiguration)
        where TUser : class
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        return auditConfiguration.UserType == typeof(TUser) && 
               auditConfiguration.UserKeyType == typeof(TUserKey);
    }

    /// <summary>
    /// Creates a compatibility report for migration planning.
    /// </summary>
    /// <param name="auditConfiguration">The configuration to analyze.</param>
    /// <param name="expectedUserType">The expected user type.</param>
    /// <param name="expectedUserKeyType">The expected user key type.</param>
    /// <returns>A detailed compatibility report.</returns>
    public static CompatibilityReport CreateCompatibilityReport(
        IAuditConfiguration auditConfiguration, 
        Type expectedUserType, 
        Type expectedUserKeyType)
    {
        return new CompatibilityReport
        {
            IsUserTypeCompatible = auditConfiguration.UserType == expectedUserType,
            IsUserKeyTypeCompatible = auditConfiguration.UserKeyType == expectedUserKeyType,
            ActualUserType = auditConfiguration.UserType,
            ActualUserKeyType = auditConfiguration.UserKeyType,
            ExpectedUserType = expectedUserType,
            ExpectedUserKeyType = expectedUserKeyType
        };
    }
}

/// <summary>
/// Represents a compatibility report for migration analysis.
/// </summary>
[Obsolete("This class is intended for migration purposes only and will be removed in a future version.")]
public sealed record CompatibilityReport
{
    /// <summary>Gets or sets whether the user type is compatible.</summary>
    public bool IsUserTypeCompatible { get; init; }
    
    /// <summary>Gets or sets whether the user key type is compatible.</summary>
    public bool IsUserKeyTypeCompatible { get; init; }
    
    /// <summary>Gets or sets the actual user type in the configuration.</summary>
    public Type ActualUserType { get; init; } = typeof(object);
    
    /// <summary>Gets or sets the actual user key type in the configuration.</summary>
    public Type ActualUserKeyType { get; init; } = typeof(object);
    
    /// <summary>Gets or sets the expected user type.</summary>
    public Type ExpectedUserType { get; init; } = typeof(object);
    
    /// <summary>Gets or sets the expected user key type.</summary>
    public Type ExpectedUserKeyType { get; init; } = typeof(object);
    
    /// <summary>Gets whether the configuration is fully compatible.</summary>
    public bool IsFullyCompatible => IsUserTypeCompatible && IsUserKeyTypeCompatible;
    
    /// <summary>Gets a human-readable summary of the compatibility status.</summary>
    public string Summary
    {
        get
        {
            if (IsFullyCompatible)
                return "Configuration is fully compatible with expected types.";
            
            var issues = new List<string>();
            
            if (!IsUserTypeCompatible)
                issues.Add($"User type mismatch: expected {ExpectedUserType.Name}, got {ActualUserType.Name}");
                
            if (!IsUserKeyTypeCompatible)
                issues.Add($"User key type mismatch: expected {ExpectedUserKeyType.Name}, got {ActualUserKeyType.Name}");
                
            return $"Compatibility issues: {string.Join(", ", issues)}";
        }
    }
}