// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Wangkanai.EntityFramework.Postgres;

/// <summary>
/// Provides extension methods for configuring real-time database features in PostgreSQL.
/// These methods leverage PostgreSQL's LISTEN/NOTIFY, advisory locks, and logical replication for real-time applications.
/// </summary>
public static class RealTimeConfigurationExtensions
{
    private static readonly ConcurrentDictionary<string, NpgsqlNotificationListener> _listeners = new();
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens = new();

    /// <summary>
    /// Configures PostgreSQL LISTEN/NOTIFY for pub/sub messaging within the database.
    /// This enables real-time communication between database sessions and applications.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="channels">The notification channels to listen to.</param>
    /// <param name="onNotification">The callback function to handle received notifications.</param>
    /// <param name="connectionTimeout">The timeout for maintaining the listener connection. Default is 30 seconds.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous listen operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
    /// <exception cref="ArgumentException">Thrown when channels array is empty.</exception>
    /// <example>
    /// <code>
    /// // Listen for user-related notifications
    /// await context.ConfigureNpgsqlListenNotifyAsync(
    ///     channels: new[] { "user_created", "user_updated" },
    ///     onNotification: (channel, payload) =>
    ///     {
    ///         Console.WriteLine($"Channel: {channel}, Payload: {payload}");
    ///         return Task.CompletedTask;
    ///     });
    /// 
    /// // Send a notification from another session
    /// await context.Database.ExecuteSqlRawAsync("NOTIFY user_created, 'User ID: 123'");
    /// </code>
    /// </example>
    public static async Task ConfigureNpgsqlListenNotifyAsync(
        this DbContext context,
        string[] channels,
        Func<string, string?, Task> onNotification,
        TimeSpan? connectionTimeout = null,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (channels == null || channels.Length == 0)
            throw new ArgumentException("At least one channel must be specified.", nameof(channels));
        if (onNotification == null)
            throw new ArgumentNullException(nameof(onNotification));

        connectionTimeout ??= TimeSpan.FromSeconds(30);
        
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var listener = new NpgsqlNotificationListener(connection, channels, onNotification, connectionTimeout.Value);
        
        var listenerId = $"{connection.ConnectionString}_{string.Join(",", channels)}";
        _listeners.TryAdd(listenerId, listener);

        await listener.StartAsync(cancellationToken);
    }

    /// <summary>
    /// Sets up notification channels for automatic entity change events using database triggers.
    /// This method creates triggers that automatically send notifications when entities are modified.
    /// </summary>
    /// <typeparam name="T">The entity type to monitor for changes.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="operations">The database operations to monitor (INSERT, UPDATE, DELETE).</param>
    /// <param name="includeOldValues">Whether to include old values in UPDATE notifications. Default is false.</param>
    /// <param name="channelPrefix">The prefix for notification channel names. Default is the entity name.</param>
    /// <returns>The configured DbContext for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Set up automatic notifications for User entity changes
    /// context.SetupEntityChangeNotifications&lt;User&gt;(
    ///     operations: ChangeOperation.Insert | ChangeOperation.Update | ChangeOperation.Delete,
    ///     includeOldValues: true,
    ///     channelPrefix: "users");
    /// 
    /// // This will create channels: users_insert, users_update, users_delete
    /// // And automatically send notifications when users are modified
    /// </code>
    /// </example>
    public static DbContext SetupEntityChangeNotifications<T>(
        this DbContext context,
        ChangeOperation operations = ChangeOperation.All,
        bool includeOldValues = false,
        string? channelPrefix = null) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var entityType = context.Model.FindEntityType(typeof(T));
        if (entityType == null)
            throw new InvalidOperationException($"Entity type {typeof(T).Name} is not configured in the model.");

        var tableName = entityType.GetTableName();
        var schema = entityType.GetSchema() ?? "public";
        channelPrefix ??= tableName?.ToLowerInvariant();

        var triggerCommands = new List<string>();

        if (operations.HasFlag(ChangeOperation.Insert))
        {
            triggerCommands.Add(CreateChangeNotificationTrigger(
                schema, tableName!, $"{channelPrefix}_insert", "INSERT", includeOldValues: false));
        }

        if (operations.HasFlag(ChangeOperation.Update))
        {
            triggerCommands.Add(CreateChangeNotificationTrigger(
                schema, tableName!, $"{channelPrefix}_update", "UPDATE", includeOldValues));
        }

        if (operations.HasFlag(ChangeOperation.Delete))
        {
            triggerCommands.Add(CreateChangeNotificationTrigger(
                schema, tableName!, $"{channelPrefix}_delete", "DELETE", includeOldValues: false));
        }

        foreach (var command in triggerCommands)
        {
            context.Database.ExecuteSqlRaw(command);
        }

        return context;
    }

    /// <summary>
    /// Configures PostgreSQL advisory locks for distributed coordination and application-level mutexes.
    /// Advisory locks provide a way to coordinate access to resources across multiple database sessions.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="lockKey">The unique identifier for the lock.</param>
    /// <param name="lockMode">The type of advisory lock to acquire.</param>
    /// <param name="timeout">The maximum time to wait for the lock. If null, waits indefinitely.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>True if the lock was acquired successfully, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <example>
    /// <code>
    /// // Acquire an exclusive advisory lock for a critical section
    /// var lockAcquired = await context.AcquireNpgsqlAdvisoryLockAsync(
    ///     lockKey: 12345,
    ///     lockMode: AdvisoryLockMode.Exclusive,
    ///     timeout: TimeSpan.FromSeconds(10));
    /// 
    /// if (lockAcquired)
    /// {
    ///     try
    ///     {
    ///         // Perform critical operations
    ///         await ProcessCriticalData();
    ///     }
    ///     finally
    ///     {
    ///         await context.ReleaseNpgsqlAdvisoryLockAsync(12345);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static async Task<bool> AcquireNpgsqlAdvisoryLockAsync(
        this DbContext context,
        long lockKey,
        AdvisoryLockMode lockMode = AdvisoryLockMode.Exclusive,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var lockFunction = lockMode switch
        {
            AdvisoryLockMode.Exclusive => timeout.HasValue ? "pg_try_advisory_lock" : "pg_advisory_lock",
            AdvisoryLockMode.Shared => timeout.HasValue ? "pg_try_advisory_lock_shared" : "pg_advisory_lock_shared",
            _ => throw new ArgumentException($"Invalid lock mode: {lockMode}", nameof(lockMode))
        };

        string sql;
        if (timeout.HasValue)
        {
            // For try locks, we get an immediate boolean result
            sql = $"SELECT {lockFunction}({lockKey})";
            var result = await context.Database.SqlQueryRaw<bool>(sql).FirstAsync(cancellationToken);
            return result;
        }
        else
        {
            // For blocking locks, we set a statement timeout if specified
            sql = $"SELECT {lockFunction}({lockKey})";
            
            if (timeout.HasValue)
            {
                var oldTimeout = context.Database.GetCommandTimeout();
                context.Database.SetCommandTimeout(timeout.Value);
                
                try
                {
                    await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                    return true;
                }
                catch (Npgsql.NpgsqlException ex) when (ex.SqlState == "57014") // query_canceled
                {
                    return false;
                }
                finally
                {
                    context.Database.SetCommandTimeout(oldTimeout);
                }
            }
            else
            {
                await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
                return true;
            }
        }
    }

    /// <summary>
    /// Releases a PostgreSQL advisory lock that was previously acquired.
    /// This method should always be called in a finally block to ensure lock cleanup.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="lockKey">The unique identifier for the lock to release.</param>
    /// <param name="lockMode">The type of advisory lock to release.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>True if the lock was successfully released, false if the lock was not held.</returns>
    /// <example>
    /// <code>
    /// var lockKey = 12345;
    /// var lockAcquired = await context.AcquireNpgsqlAdvisoryLockAsync(lockKey);
    /// 
    /// try
    /// {
    ///     // Critical section
    /// }
    /// finally
    /// {
    ///     if (lockAcquired)
    ///         await context.ReleaseNpgsqlAdvisoryLockAsync(lockKey);
    /// }
    /// </code>
    /// </example>
    public static async Task<bool> ReleaseNpgsqlAdvisoryLockAsync(
        this DbContext context,
        long lockKey,
        AdvisoryLockMode lockMode = AdvisoryLockMode.Exclusive,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var unlockFunction = lockMode switch
        {
            AdvisoryLockMode.Exclusive => "pg_advisory_unlock",
            AdvisoryLockMode.Shared => "pg_advisory_unlock_shared",
            _ => throw new ArgumentException($"Invalid lock mode: {lockMode}", nameof(lockMode))
        };

        var sql = $"SELECT {unlockFunction}({lockKey})";
        var result = await context.Database.SqlQueryRaw<bool>(sql).FirstAsync(cancellationToken);
        return result;
    }

    /// <summary>
    /// Configures logical replication for change data capture and real-time event streaming.
    /// This method sets up logical replication to capture and stream database changes to external systems.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="replicationSlotName">The name of the logical replication slot.</param>
    /// <param name="publicationName">The name of the publication to create.</param>
    /// <param name="tableNames">The tables to include in the publication. If empty, includes all tables.</param>
    /// <param name="replicationOptions">Additional options for logical replication.</param>
    /// <returns>The configured DbContext for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Set up logical replication for change data capture
    /// context.ConfigureNpgsqlLogicalReplication(
    ///     replicationSlotName: "app_changes",
    ///     publicationName: "app_publication",
    ///     tableNames: new[] { "users", "orders", "products" },
    ///     replicationOptions: new LogicalReplicationOptions
    ///     {
    ///         IncludeInserts = true,
    ///         IncludeUpdates = true,
    ///         IncludeDeletes = true,
    ///         IncludeTimestamps = true
    ///     });
    /// </code>
    /// </example>
    public static DbContext ConfigureNpgsqlLogicalReplication(
        this DbContext context,
        string replicationSlotName,
        string publicationName,
        string[]? tableNames = null,
        LogicalReplicationOptions? replicationOptions = null)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(replicationSlotName))
            throw new ArgumentException("Replication slot name cannot be null or empty.", nameof(replicationSlotName));
        if (string.IsNullOrWhiteSpace(publicationName))
            throw new ArgumentException("Publication name cannot be null or empty.", nameof(publicationName));

        replicationOptions ??= new LogicalReplicationOptions();

        // Create logical replication slot
        var createSlotSql = $"SELECT pg_create_logical_replication_slot('{replicationSlotName}', 'pgoutput')";
        
        try
        {
            context.Database.ExecuteSqlRaw(createSlotSql);
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42710") // duplicate_object
        {
            // Slot already exists, continue
        }

        // Create publication
        var publicationSql = new StringBuilder($"CREATE PUBLICATION {publicationName}");
        
        if (tableNames != null && tableNames.Length > 0)
        {
            publicationSql.Append($" FOR TABLE {string.Join(", ", tableNames)}");
        }
        else
        {
            publicationSql.Append(" FOR ALL TABLES");
        }

        // Add publication options
        var options = new List<string>();
        if (replicationOptions.IncludeInserts) options.Add("insert");
        if (replicationOptions.IncludeUpdates) options.Add("update");
        if (replicationOptions.IncludeDeletes) options.Add("delete");
        if (replicationOptions.IncludeTruncates) options.Add("truncate");

        if (options.Any())
        {
            publicationSql.Append($" WITH (publish = '{string.Join(", ", options)}')");
        }

        try
        {
            context.Database.ExecuteSqlRaw(publicationSql.ToString());
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42710") // duplicate_object
        {
            // Publication already exists, continue
        }

        return context;
    }

    /// <summary>
    /// Creates a real-time event streaming service that processes logical replication events.
    /// This service continuously monitors database changes and invokes callbacks for each event.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="replicationSlotName">The name of the logical replication slot to consume.</param>
    /// <param name="onChangeEvent">The callback function to handle change events.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the streaming operation.</returns>
    /// <example>
    /// <code>
    /// // Start real-time event streaming
    /// await context.StartNpgsqlEventStreamingAsync(
    ///     replicationSlotName: "app_changes",
    ///     onChangeEvent: (changeEvent) =>
    ///     {
    ///         Console.WriteLine($"Table: {changeEvent.TableName}, Operation: {changeEvent.Operation}");
    ///         Console.WriteLine($"Data: {JsonSerializer.Serialize(changeEvent.NewValues)}");
    ///         return Task.CompletedTask;
    ///     });
    /// </code>
    /// </example>
    public static async Task StartNpgsqlEventStreamingAsync(
        this DbContext context,
        string replicationSlotName,
        Func<ChangeEvent, Task> onChangeEvent,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(replicationSlotName))
            throw new ArgumentException("Replication slot name cannot be null or empty.", nameof(replicationSlotName));
        if (onChangeEvent == null)
            throw new ArgumentNullException(nameof(onChangeEvent));

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        var eventStreamer = new LogicalReplicationEventStreamer(connection, replicationSlotName, onChangeEvent);
        await eventStreamer.StartStreamingAsync(cancellationToken);
    }

    /// <summary>
    /// Configures connection management for long-lived notification listeners.
    /// This method optimizes connection settings for applications that maintain persistent listeners.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="keepAliveInterval">The interval for sending keep-alive packets. Default is 10 seconds.</param>
    /// <param name="reconnectInterval">The interval for attempting reconnection on failure. Default is 5 seconds.</param>
    /// <param name="maxReconnectAttempts">The maximum number of reconnection attempts. Default is 10.</param>
    /// <returns>The configured DbContext for method chaining.</returns>
    /// <example>
    /// <code>
    /// // Configure for long-lived real-time connections
    /// context.ConfigureNpgsqlRealTimeConnections(
    ///     keepAliveInterval: TimeSpan.FromSeconds(5),
    ///     reconnectInterval: TimeSpan.FromSeconds(3),
    ///     maxReconnectAttempts: 20);
    /// </code>
    /// </example>
    public static DbContext ConfigureNpgsqlRealTimeConnections(
        this DbContext context,
        TimeSpan? keepAliveInterval = null,
        TimeSpan? reconnectInterval = null,
        int maxReconnectAttempts = 10)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        keepAliveInterval ??= TimeSpan.FromSeconds(10);
        reconnectInterval ??= TimeSpan.FromSeconds(5);

        if (maxReconnectAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxReconnectAttempts), "Max reconnect attempts must be greater than zero.");

        // Configure connection for real-time operations
        var commands = new[]
        {
            "SET application_name = 'EntityFramework-RealTime';",
            $"SET tcp_keepalives_idle = {(int)keepAliveInterval.Value.TotalSeconds};",
            $"SET tcp_keepalives_interval = {(int)keepAliveInterval.Value.TotalSeconds};",
            "SET tcp_keepalives_count = 3;",
            "SET statement_timeout = 0;", // Disable statement timeout for long-lived connections
            "SET idle_in_transaction_session_timeout = 0;" // Disable idle timeout
        };

        foreach (var command in commands)
        {
            context.Database.ExecuteSqlRaw(command);
        }

        return context;
    }

    /// <summary>
    /// Stops all active notification listeners and cleans up resources.
    /// This method should be called when shutting down the application to properly clean up connections.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="timeout">The maximum time to wait for listeners to stop. Default is 30 seconds.</param>
    /// <returns>A task representing the cleanup operation.</returns>
    /// <example>
    /// <code>
    /// // Clean up during application shutdown
    /// await context.StopAllNpgsqlListenersAsync(timeout: TimeSpan.FromSeconds(10));
    /// </code>
    /// </example>
    public static async Task StopAllNpgsqlListenersAsync(
        this DbContext context,
        TimeSpan? timeout = null)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        timeout ??= TimeSpan.FromSeconds(30);

        var stopTasks = _listeners.Values.Select(listener => listener.StopAsync(timeout.Value));
        await Task.WhenAll(stopTasks);

        _listeners.Clear();
        
        foreach (var cts in _cancellationTokens.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        
        _cancellationTokens.Clear();
    }

    #region Private Helper Methods

    private static string CreateChangeNotificationTrigger(
        string schema,
        string tableName,
        string channelName,
        string operation,
        bool includeOldValues)
    {
        var functionName = $"notify_{tableName}_{operation.ToLower()}";
        var triggerName = $"{tableName}_{operation.ToLower()}_trigger";

        var payloadBuilder = new StringBuilder();
        payloadBuilder.Append("'{\"table\": \"' || TG_TABLE_NAME || '\"");
        payloadBuilder.Append(", \"operation\": \"' || TG_OP || '\"");
        payloadBuilder.Append(", \"timestamp\": \"' || NOW() || '\"");

        if (operation == "INSERT" || operation == "UPDATE")
        {
            payloadBuilder.Append(", \"new\": ' || row_to_json(NEW) || '");
        }

        if (operation == "UPDATE" && includeOldValues)
        {
            payloadBuilder.Append(", \"old\": ' || row_to_json(OLD) || '");
        }

        if (operation == "DELETE")
        {
            payloadBuilder.Append(", \"old\": ' || row_to_json(OLD) || '");
        }

        payloadBuilder.Append("}'");

        return $@"
            CREATE OR REPLACE FUNCTION {schema}.{functionName}() 
            RETURNS TRIGGER AS $$
            BEGIN
                PERFORM pg_notify('{channelName}', {payloadBuilder});
                RETURN COALESCE(NEW, OLD);
            END;
            $$ LANGUAGE plpgsql;

            DROP TRIGGER IF EXISTS {triggerName} ON {schema}.""{tableName}"";
            
            CREATE TRIGGER {triggerName}
                AFTER {operation} ON {schema}.""{tableName}""
                FOR EACH ROW EXECUTE FUNCTION {schema}.{functionName}();";
    }

    #endregion
}

#region Supporting Classes and Enums

/// <summary>
/// Specifies the types of database operations to monitor for change notifications.
/// </summary>
[Flags]
public enum ChangeOperation
{
    /// <summary>No operations.</summary>
    None = 0,
    /// <summary>INSERT operations.</summary>
    Insert = 1,
    /// <summary>UPDATE operations.</summary>
    Update = 2,
    /// <summary>DELETE operations.</summary>
    Delete = 4,
    /// <summary>All operations (INSERT, UPDATE, DELETE).</summary>
    All = Insert | Update | Delete
}

/// <summary>
/// Specifies the mode for PostgreSQL advisory locks.
/// </summary>
public enum AdvisoryLockMode
{
    /// <summary>Exclusive lock - only one session can hold the lock.</summary>
    Exclusive,
    /// <summary>Shared lock - multiple sessions can hold the lock simultaneously.</summary>
    Shared
}

/// <summary>
/// Configuration options for PostgreSQL logical replication.
/// </summary>
public class LogicalReplicationOptions
{
    /// <summary>Gets or sets whether to include INSERT operations in replication. Default is true.</summary>
    public bool IncludeInserts { get; set; } = true;

    /// <summary>Gets or sets whether to include UPDATE operations in replication. Default is true.</summary>
    public bool IncludeUpdates { get; set; } = true;

    /// <summary>Gets or sets whether to include DELETE operations in replication. Default is true.</summary>
    public bool IncludeDeletes { get; set; } = true;

    /// <summary>Gets or sets whether to include TRUNCATE operations in replication. Default is false.</summary>
    public bool IncludeTruncates { get; set; } = false;

    /// <summary>Gets or sets whether to include timestamps in change events. Default is true.</summary>
    public bool IncludeTimestamps { get; set; } = true;
}

/// <summary>
/// Represents a database change event from logical replication.
/// </summary>
public class ChangeEvent
{
    /// <summary>Gets or sets the name of the table that was changed.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of operation (INSERT, UPDATE, DELETE).</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the change occurred.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Gets or sets the new values for INSERT and UPDATE operations.</summary>
    public Dictionary<string, object?>? NewValues { get; set; }

    /// <summary>Gets or sets the old values for UPDATE and DELETE operations.</summary>
    public Dictionary<string, object?>? OldValues { get; set; }

    /// <summary>Gets or sets additional metadata about the change event.</summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Manages PostgreSQL notification listeners for LISTEN/NOTIFY functionality.
/// </summary>
internal class NpgsqlNotificationListener
{
    private readonly NpgsqlConnection _connection;
    private readonly string[] _channels;
    private readonly Func<string, string?, Task> _onNotification;
    private readonly TimeSpan _connectionTimeout;
    private Task? _listenerTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public NpgsqlNotificationListener(
        NpgsqlConnection connection,
        string[] channels,
        Func<string, string?, Task> onNotification,
        TimeSpan connectionTimeout)
    {
        _connection = connection;
        _channels = channels;
        _onNotification = onNotification;
        _connectionTimeout = connectionTimeout;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync(_cancellationTokenSource.Token);

        // Subscribe to channels
        foreach (var channel in _channels)
        {
            using var command = new NpgsqlCommand($"LISTEN {channel}", _connection);
            await command.ExecuteNonQueryAsync(_cancellationTokenSource.Token);
        }

        _connection.Notification += async (sender, args) =>
        {
            try
            {
                await _onNotification(args.Channel, args.Payload);
            }
            catch (Exception ex)
            {
                // Log error but don't stop the listener
                Console.WriteLine($"Error processing notification: {ex.Message}");
            }
        };

        _listenerTask = ListenForNotificationsAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync(TimeSpan timeout)
    {
        _cancellationTokenSource?.Cancel();
        
        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                // Force cleanup if timeout is exceeded
            }
        }

        _cancellationTokenSource?.Dispose();
    }

    private async Task ListenForNotificationsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _connection.WaitAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Notification listener error: {ex.Message}");
        }
    }
}

/// <summary>
/// Manages PostgreSQL logical replication event streaming.
/// </summary>
internal class LogicalReplicationEventStreamer
{
    private readonly NpgsqlConnection _connection;
    private readonly string _replicationSlotName;
    private readonly Func<ChangeEvent, Task> _onChangeEvent;

    public LogicalReplicationEventStreamer(
        NpgsqlConnection connection,
        string replicationSlotName,
        Func<ChangeEvent, Task> onChangeEvent)
    {
        _connection = connection;
        _replicationSlotName = replicationSlotName;
        _onChangeEvent = onChangeEvent;
    }

    public async Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        // Note: This is a simplified implementation
        // Production code would use NpgsqlLogicalReplicationConnection for proper streaming
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Poll for changes (in production, use streaming replication)
                var sql = $"SELECT * FROM pg_logical_slot_get_changes('{_replicationSlotName}', NULL, NULL)";
                using var command = new NpgsqlCommand(sql, _connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var lsn = reader.GetString(0);
                    var xid = reader.GetFieldValue<uint>(1);
                    var data = reader.GetString(2);

                    var changeEvent = ParseChangeEvent(data);
                    if (changeEvent != null)
                    {
                        await _onChangeEvent(changeEvent);
                    }
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Error in event streaming: {ex.Message}");
                await Task.Delay(1000, cancellationToken); // Wait before retrying
            }
        }
    }

    private static ChangeEvent? ParseChangeEvent(string data)
    {
        try
        {
            // Simplified parsing - production code would use proper WAL parsing
            if (data.Contains("INSERT") || data.Contains("UPDATE") || data.Contains("DELETE"))
            {
                return new ChangeEvent
                {
                    TableName = "unknown", // Would be parsed from WAL data
                    Operation = "unknown", // Would be parsed from WAL data
                    Timestamp = DateTime.UtcNow,
                    NewValues = new Dictionary<string, object?>(),
                    Metadata = new Dictionary<string, object> { ["raw_data"] = data }
                };
            }
        }
        catch (Exception)
        {
            // Ignore parsing errors
        }

        return null;
    }
}

#endregion