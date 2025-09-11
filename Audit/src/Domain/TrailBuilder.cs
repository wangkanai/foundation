// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

/// <summary>
/// Builder pattern implementation for creating Trail instances with complex configurations.
/// This pattern provides a fluent API for setting up audit trails while maintaining type safety
/// and reducing generic complexity.
/// </summary>
/// <typeparam name="TKey">The type of the audit trail identifier.</typeparam>
public sealed class TrailBuilder<TKey>
    where TKey : IEquatable<TKey>, IComparable<TKey>
{
    private readonly Trail<TKey> _trail;
    private IAuditConfiguration? _auditConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrailBuilder{TKey}"/> class.
    /// </summary>
    public TrailBuilder()
    {
        _trail = new Trail<TKey>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TrailBuilder{TKey}"/> class with an explicit ID.
    /// </summary>
    /// <param name="id">The explicit ID to assign to the trail.</param>
    public TrailBuilder(TKey id)
    {
        _trail = new Trail<TKey> { Id = id };
    }

    /// <summary>
    /// Configures the builder with user type information.
    /// </summary>
    /// <typeparam name="TUser">The type of the user associated with audit actions.</typeparam>
    /// <typeparam name="TUserKey">The type of the user's identifier.</typeparam>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithUserTypes<TUser, TUserKey>()
        where TUser : IdentityUser<TUserKey>
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        _auditConfiguration = AuditConfiguration<TUser, TUserKey>.Create().AsInterface();
        return this;
    }

    /// <summary>
    /// Configures the builder with an explicit audit configuration.
    /// </summary>
    /// <param name="auditConfiguration">The audit configuration to use.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithConfiguration(IAuditConfiguration auditConfiguration)
    {
        _auditConfiguration = auditConfiguration;
        return this;
    }

    /// <summary>
    /// Sets the trail type (Create, Update, Delete, etc.).
    /// </summary>
    /// <param name="trailType">The type of audit action.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithTrailType(TrailType trailType)
    {
        _trail.TrailType = trailType;
        return this;
    }

    /// <summary>
    /// Sets the entity information being audited.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="primaryKey">The primary key of the entity (optional).</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithEntity(string entityName, string? primaryKey = null)
    {
        _trail.EntityName = entityName;
        _trail.PrimaryKey = primaryKey;
        return this;
    }

    /// <summary>
    /// Sets the timestamp for the audit action.
    /// </summary>
    /// <param name="timestamp">The timestamp of the action. If null, uses current UTC time.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithTimestamp(DateTime? timestamp = null)
    {
        _trail.Timestamp = timestamp ?? DateTime.UtcNow;
        return this;
    }

    /// <summary>
    /// Sets the user information for the audit trail.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <param name="user">The user performing the action.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithUser<TUser>(TUser? user)
        where TUser : class
    {
        _trail.SetUser(user);
        return this;
    }

    /// <summary>
    /// Sets the user ID for the audit trail.
    /// </summary>
    /// <typeparam name="TUserKey">The type of the user key.</typeparam>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithUserId<TUserKey>(TUserKey? userId)
        where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
    {
        _trail.SetUserId(userId);
        return this;
    }

    /// <summary>
    /// Sets the changed values using dictionaries.
    /// </summary>
    /// <param name="oldValues">Dictionary of old values.</param>
    /// <param name="newValues">Dictionary of new values.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithValues(
        Dictionary<string, object>? oldValues = null, 
        Dictionary<string, object>? newValues = null)
    {
        if (oldValues != null)
            _trail.OldValues = oldValues;
            
        if (newValues != null)
            _trail.NewValues = newValues;
            
        return this;
    }

    /// <summary>
    /// Sets the changed values using JSON strings for optimal performance.
    /// </summary>
    /// <param name="oldValuesJson">JSON representation of old values.</param>
    /// <param name="newValuesJson">JSON representation of new values.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithValuesFromJson(string? oldValuesJson, string? newValuesJson)
    {
        _trail.SetValuesFromJson(oldValuesJson, newValuesJson);
        return this;
    }

    /// <summary>
    /// Sets the changed values using spans for high-performance scenarios.
    /// </summary>
    /// <typeparam name="T">The type of the values.</typeparam>
    /// <param name="columnNames">Span of column names.</param>
    /// <param name="oldValues">Span of old values.</param>
    /// <param name="newValues">Span of new values.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithValuesFromSpan<T>(
        ReadOnlySpan<string> columnNames, 
        ReadOnlySpan<T> oldValues, 
        ReadOnlySpan<T> newValues)
    {
        _trail.SetValuesFromSpan(columnNames, oldValues, newValues);
        return this;
    }

    /// <summary>
    /// Sets the list of changed columns.
    /// </summary>
    /// <param name="changedColumns">The list of column names that changed.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> WithChangedColumns(IEnumerable<string> changedColumns)
    {
        _trail.ChangedColumns = changedColumns.ToList();
        return this;
    }

    /// <summary>
    /// Adds a single changed column to the existing list.
    /// </summary>
    /// <param name="columnName">The name of the changed column.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public TrailBuilder<TKey> AddChangedColumn(string columnName)
    {
        _trail.ChangedColumns.Add(columnName);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured Trail instance.
    /// </summary>
    /// <returns>A fully configured Trail instance.</returns>
    public Trail<TKey> Build()
    {
        // Apply audit configuration if one was set
        if (_auditConfiguration != null)
        {
            _trail.AuditConfiguration = _auditConfiguration;
        }

        // Validate the trail before returning
        if (!_trail.ValidateUserData())
        {
            throw new InvalidOperationException(
                "The configured user data is not compatible with the audit configuration. " +
                "Ensure that user and user ID types match the configured audit types.");
        }

        return _trail;
    }

    /// <summary>
    /// Builds and returns the configured Trail instance without validation.
    /// Use this method only when you are certain about the configuration correctness.
    /// </summary>
    /// <returns>A configured Trail instance without validation.</returns>
    public Trail<TKey> BuildUnsafe()
    {
        if (_auditConfiguration != null)
        {
            _trail.AuditConfiguration = _auditConfiguration;
        }

        return _trail;
    }

    #region Static Factory Methods

    /// <summary>
    /// Creates a new TrailBuilder for string-based keys.
    /// </summary>
    /// <returns>A new TrailBuilder for string keys.</returns>
    public static TrailBuilder<string> ForStringKey()
    {
        return new TrailBuilder<string>();
    }

    /// <summary>
    /// Creates a new TrailBuilder for integer-based keys.
    /// </summary>
    /// <returns>A new TrailBuilder for integer keys.</returns>
    public static TrailBuilder<int> ForIntKey()
    {
        return new TrailBuilder<int>();
    }

    /// <summary>
    /// Creates a new TrailBuilder for GUID-based keys.
    /// </summary>
    /// <returns>A new TrailBuilder for GUID keys.</returns>
    public static TrailBuilder<Guid> ForGuidKey()
    {
        return new TrailBuilder<Guid>();
    }

    /// <summary>
    /// Creates a new TrailBuilder with explicit ID.
    /// </summary>
    /// <typeparam name="T">The key type.</typeparam>
    /// <param name="id">The explicit ID.</param>
    /// <returns>A new TrailBuilder with the specified ID.</returns>
    public static TrailBuilder<T> WithId<T>(T id)
        where T : IEquatable<T>, IComparable<T>
    {
        return new TrailBuilder<T>(id);
    }

    #endregion

    #region Preset Configurations

    /// <summary>
    /// Creates a builder preconfigured for standard ASP.NET Identity users.
    /// </summary>
    /// <returns>A TrailBuilder configured for Identity users.</returns>
    public TrailBuilder<TKey> ForIdentityUsers()
    {
        return WithUserTypes<IdentityUser, string>();
    }

    /// <summary>
    /// Creates a builder preconfigured for GUID-based Identity users.
    /// </summary>
    /// <typeparam name="TUser">The custom user type.</typeparam>
    /// <returns>A TrailBuilder configured for GUID-based users.</returns>
    public TrailBuilder<TKey> ForGuidUsers<TUser>()
        where TUser : IdentityUser<Guid>
    {
        return WithUserTypes<TUser, Guid>();
    }

    /// <summary>
    /// Creates a builder preconfigured for integer-based Identity users.
    /// </summary>
    /// <typeparam name="TUser">The custom user type.</typeparam>
    /// <returns>A TrailBuilder configured for integer-based users.</returns>
    public TrailBuilder<TKey> ForIntUsers<TUser>()
        where TUser : IdentityUser<int>
    {
        return WithUserTypes<TUser, int>();
    }

    #endregion
}

/// <summary>
/// Extension methods for TrailBuilder to provide additional functionality.
/// </summary>
public static class TrailBuilderExtensions
{
    /// <summary>
    /// Creates a trail for entity creation with common defaults.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entityName">The name of the created entity.</param>
    /// <param name="primaryKey">The primary key of the created entity.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static TrailBuilder<TKey> ForCreation<TKey>(
        this TrailBuilder<TKey> builder, 
        string entityName, 
        string? primaryKey = null)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return builder
            .WithTrailType(TrailType.Create)
            .WithEntity(entityName, primaryKey)
            .WithTimestamp();
    }

    /// <summary>
    /// Creates a trail for entity update with common defaults.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entityName">The name of the updated entity.</param>
    /// <param name="primaryKey">The primary key of the updated entity.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static TrailBuilder<TKey> ForUpdate<TKey>(
        this TrailBuilder<TKey> builder, 
        string entityName, 
        string? primaryKey = null)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return builder
            .WithTrailType(TrailType.Update)
            .WithEntity(entityName, primaryKey)
            .WithTimestamp();
    }

    /// <summary>
    /// Creates a trail for entity deletion with common defaults.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entityName">The name of the deleted entity.</param>
    /// <param name="primaryKey">The primary key of the deleted entity.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static TrailBuilder<TKey> ForDeletion<TKey>(
        this TrailBuilder<TKey> builder, 
        string entityName, 
        string? primaryKey = null)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return builder
            .WithTrailType(TrailType.Delete)
            .WithEntity(entityName, primaryKey)
            .WithTimestamp();
    }

    /// <summary>
    /// Creates a trail for entity archival with common defaults.
    /// </summary>
    /// <typeparam name="TKey">The trail key type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="entityName">The name of the archived entity.</param>
    /// <param name="primaryKey">The primary key of the archived entity.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public static TrailBuilder<TKey> ForArchival<TKey>(
        this TrailBuilder<TKey> builder, 
        string entityName, 
        string? primaryKey = null)
        where TKey : IEquatable<TKey>, IComparable<TKey>
    {
        return builder
            .WithTrailType(TrailType.Archive)
            .WithEntity(entityName, primaryKey)
            .WithTimestamp();
    }
}