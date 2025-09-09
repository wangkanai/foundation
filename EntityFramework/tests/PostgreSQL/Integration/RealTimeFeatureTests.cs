// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Wangkanai.EntityFramework.PostgreSQL.Integration.Infrastructure;
using Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Integration tests for PostgreSQL real-time features.
/// Tests LISTEN/NOTIFY, change notifications, and advisory locks.
/// </summary>
public sealed class RealTimeFeatureTests : PostgreSqlIntegrationTestBase
{
    public RealTimeFeatureTests(PostgreSqlTestFixture fixture, ITestOutputHelper output)
        : base(fixture, output)
    {
    }

    [Fact]
    public async Task ListenNotify_ShouldWorkCorrectly()
    {
        // Arrange
        var receivedNotifications = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Setup listener connection
        await using var listenerConnection = new NpgsqlConnection(ConnectionString);
        await listenerConnection.OpenAsync(cts.Token);

        listenerConnection.Notification += (sender, args) =>
        {
            receivedNotifications.Add(args.Payload);
        };

        await using var listenCommand = new NpgsqlCommand("LISTEN test_channel", listenerConnection);
        await listenCommand.ExecuteNonQueryAsync(cts.Token);

        // Setup notifier connection
        await using var notifierConnection = new NpgsqlConnection(ConnectionString);
        await notifierConnection.OpenAsync(cts.Token);

        // Act - Send notifications
        var messages = new[] { "Message 1", "Message 2", "Message 3" };
        
        foreach (var message in messages)
        {
            await using var notifyCommand = new NpgsqlCommand(
                "SELECT pg_notify('test_channel', @payload)", 
                notifierConnection);
            notifyCommand.Parameters.AddWithValue("payload", message);
            await notifyCommand.ExecuteNonQueryAsync(cts.Token);
        }

        // Wait for notifications
        var maxWaitTime = DateTime.UtcNow.AddSeconds(5);
        while (receivedNotifications.Count < messages.Length && DateTime.UtcNow < maxWaitTime)
        {
            await listenerConnection.WaitAsync(cts.Token);
        }

        // Assert
        receivedNotifications.Should().HaveCount(3);
        receivedNotifications.Should().BeEquivalentTo(messages);
    }

    [Fact]
    public async Task AdvisoryLocks_ShouldPreventConcurrentAccess()
    {
        // Arrange
        const long lockId = 12345;
        var results = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        // Act - Try to acquire the same advisory lock from multiple connections
        var tasks = new List<Task>();

        for (int i = 1; i <= 3; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync(cts.Token);

                // Try to acquire advisory lock
                await using var lockCommand = new NpgsqlCommand(
                    "SELECT pg_advisory_lock(@lockId)", connection);
                lockCommand.Parameters.AddWithValue("lockId", lockId);
                
                await lockCommand.ExecuteNonQueryAsync(cts.Token);
                
                lock (results)
                {
                    results.Add($"Task {taskId} acquired lock");
                }

                // Hold the lock for a short time
                await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);

                // Release the lock
                await using var unlockCommand = new NpgsqlCommand(
                    "SELECT pg_advisory_unlock(@lockId)", connection);
                unlockCommand.Parameters.AddWithValue("lockId", lockId);
                await unlockCommand.ExecuteNonQueryAsync(cts.Token);

                lock (results)
                {
                    results.Add($"Task {taskId} released lock");
                }
            }, cts.Token));
        }

        await Task.WhenAll(tasks);

        // Assert - Each task should acquire and release the lock sequentially
        results.Should().HaveCount(6); // 3 acquire + 3 release
        results.Where(r => r.Contains("acquired")).Should().HaveCount(3);
        results.Where(r => r.Contains("released")).Should().HaveCount(3);
    }

    [Fact]
    public async Task DatabaseTriggers_ShouldSendNotifications()
    {
        // Arrange
        var options = CreateDbContextOptions<RealTimeTestDbContext>();
        var receivedNotifications = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await using var context = new RealTimeTestDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Create notification trigger
        await ExecuteSqlAsync("""
            CREATE OR REPLACE FUNCTION notify_entity_changes() RETURNS TRIGGER AS $$
            BEGIN
                IF TG_OP = 'INSERT' THEN
                    PERFORM pg_notify('entity_changes', 'INSERT:' || NEW.id || ':' || NEW.message);
                    RETURN NEW;
                ELSIF TG_OP = 'UPDATE' THEN
                    PERFORM pg_notify('entity_changes', 'UPDATE:' || NEW.id || ':' || NEW.message);
                    RETURN NEW;
                ELSIF TG_OP = 'DELETE' THEN
                    PERFORM pg_notify('entity_changes', 'DELETE:' || OLD.id);
                    RETURN OLD;
                END IF;
                RETURN NULL;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER notification_entities_trigger
                AFTER INSERT OR UPDATE OR DELETE ON notification_entities
                FOR EACH ROW EXECUTE FUNCTION notify_entity_changes();
            """);

        // Setup listener
        await using var listenerConnection = new NpgsqlConnection(ConnectionString);
        await listenerConnection.OpenAsync(cts.Token);

        listenerConnection.Notification += (sender, args) =>
        {
            receivedNotifications.Add(args.Payload);
        };

        await using var listenCommand = new NpgsqlCommand("LISTEN entity_changes", listenerConnection);
        await listenCommand.ExecuteNonQueryAsync(cts.Token);

        // Act - Perform database operations
        var entity = new NotificationEntity
        {
            Channel = "test",
            Message = "Test message",
            SentAt = DateTime.UtcNow,
            Sender = "System",
            IsProcessed = false
        };

        await context.Notifications.AddAsync(entity, cts.Token);
        await context.SaveChangesAsync(cts.Token);

        // Update the entity
        entity.Message = "Updated message";
        entity.IsProcessed = true;
        await context.SaveChangesAsync(cts.Token);

        // Delete the entity
        context.Notifications.Remove(entity);
        await context.SaveChangesAsync(cts.Token);

        // Wait for notifications
        var maxWaitTime = DateTime.UtcNow.AddSeconds(5);
        while (receivedNotifications.Count < 3 && DateTime.UtcNow < maxWaitTime)
        {
            await listenerConnection.WaitAsync(cts.Token);
        }

        // Assert
        receivedNotifications.Should().HaveCount(3);
        receivedNotifications[0].Should().StartWith("INSERT:");
        receivedNotifications[1].Should().StartWith("UPDATE:");
        receivedNotifications[2].Should().StartWith("DELETE:");
    }
}

/// <summary>
/// Test DbContext for real-time features testing.
/// </summary>
public class RealTimeTestDbContext : DbContext
{
    public RealTimeTestDbContext(DbContextOptions<RealTimeTestDbContext> options) : base(options) { }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Channel).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
            entity.Property(e => e.Sender).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsProcessed).IsRequired();
        });
    }
}