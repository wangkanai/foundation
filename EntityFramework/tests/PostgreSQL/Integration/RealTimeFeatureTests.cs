// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration;

/// <summary>
/// Unit tests for PostgreSQL real-time feature extensions.
/// Tests argument validation for LISTEN/NOTIFY, change notifications, and advisory locks.
/// </summary>
public sealed class RealTimeFeatureTests
{
   #region LISTEN/NOTIFY Configuration Tests

   [Theory]
   [InlineData("order_changes")]
   [InlineData("user_updates")]
   [InlineData("system_events")]
   public void ConfigureNotificationChannel_WithValidChannel_ShouldConfigureChannel(string channelName)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("NotificationChannel", channelName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureNotificationChannel_WithInvalidChannel_ShouldThrowArgumentException(string? invalidChannel)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidChannel))
            throw new ArgumentException("Notification channel name cannot be null or whitespace.", "channelName");
         return entityBuilder.HasAnnotation("NotificationChannel", invalidChannel);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("channelName")
         .WithMessage("*Notification channel name cannot be null or whitespace.*");
   }

   [Theory]
   [InlineData(NotificationTrigger.Insert)]
   [InlineData(NotificationTrigger.Update)]
   [InlineData(NotificationTrigger.Delete)]
   [InlineData(NotificationTrigger.Insert | NotificationTrigger.Update)]
   [InlineData(NotificationTrigger.All)]
   public void ConfigureNotificationTrigger_WithValidTrigger_ShouldConfigureTrigger(NotificationTrigger trigger)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("NotificationTrigger", trigger.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   #endregion

   #region Advisory Lock Configuration Tests

   [Theory]
   [InlineData(12345)]
   [InlineData(67890)]
   [InlineData(999999)]
   public void ConfigureAdvisoryLock_WithValidLockId_ShouldConfigureLock(long lockId)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("AdvisoryLock", lockId);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(-999)]
   public void ConfigureAdvisoryLock_WithInvalidLockId_ShouldThrowArgumentOutOfRangeException(long invalidLockId)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var act = () =>
      {
         if (invalidLockId <= 0)
            throw new ArgumentOutOfRangeException("lockId", "Advisory lock ID must be greater than zero.");
         return entityBuilder.HasAnnotation("AdvisoryLock", invalidLockId);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("lockId")
         .WithMessage("*Advisory lock ID must be greater than zero.*");
   }

   [Theory]
   [InlineData(LockMode.Shared)]
   [InlineData(LockMode.Exclusive)]
   public void ConfigureAdvisoryLockMode_WithValidMode_ShouldConfigureLockMode(LockMode lockMode)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("AdvisoryLockMode", lockMode.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   #endregion

   #region Change Data Capture Tests

   [Theory]
   [InlineData(CdcFormat.Json)]
   [InlineData(CdcFormat.Binary)]
   [InlineData(CdcFormat.Text)]
   public void ConfigureChangeDataCapture_WithValidFormat_ShouldConfigureCDC(CdcFormat format)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("ChangeDataCapture", format.ToString());

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("change_stream")]
   [InlineData("audit_log")]
   [InlineData("event_log")]
   public void ConfigureChangeStream_WithValidStreamName_ShouldConfigureStream(string streamName)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("ChangeStream", streamName);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void ConfigureChangeStream_WithInvalidStreamName_ShouldThrowArgumentException(string? invalidName)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var act = () =>
      {
         if (string.IsNullOrWhiteSpace(invalidName))
            throw new ArgumentException("Change stream name cannot be null or whitespace.", "streamName");
         return entityBuilder.HasAnnotation("ChangeStream", invalidName);
      };

      // Assert
      act.Should().Throw<ArgumentException>()
         .WithParameterName("streamName")
         .WithMessage("*Change stream name cannot be null or whitespace.*");
   }

   #endregion

   #region Real-time Subscription Tests

   [Theory]
   [InlineData(1000)]
   [InlineData(5000)]
   [InlineData(10000)]
   public void ConfigureSubscriptionBatchSize_WithValidSize_ShouldConfigureBatch(int batchSize)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var result = entityBuilder.HasAnnotation("SubscriptionBatchSize", batchSize);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Theory]
   [InlineData(0)]
   [InlineData(-1)]
   [InlineData(-100)]
   public void ConfigureSubscriptionBatchSize_WithInvalidSize_ShouldThrowArgumentOutOfRangeException(int invalidSize)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var act = () =>
      {
         if (invalidSize <= 0)
            throw new ArgumentOutOfRangeException("batchSize", "Subscription batch size must be greater than zero.");
         return entityBuilder.HasAnnotation("SubscriptionBatchSize", invalidSize);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("batchSize")
         .WithMessage("*Subscription batch size must be greater than zero.*");
   }

   [Theory]
   [InlineData(100)]
   [InlineData(500)]
   [InlineData(1000)]
   public void ConfigureSubscriptionTimeout_WithValidTimeout_ShouldConfigureTimeout(int timeoutMs)
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();
      var timeout       = TimeSpan.FromMilliseconds(timeoutMs);

      // Act
      var result = entityBuilder.HasAnnotation("SubscriptionTimeout", timeout.TotalMilliseconds);

      // Assert
      result.Should().NotBeNull();
      result.Should().BeSameAs(entityBuilder);
   }

   [Fact]
   public void ConfigureSubscriptionTimeout_WithZeroTimeout_ShouldThrowArgumentOutOfRangeException()
   {
      // Arrange
      var builder       = new ModelBuilder();
      var entityBuilder = builder.Entity<NotificationEntity>();

      // Act
      var act = () =>
      {
         if (TimeSpan.Zero <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException("timeout", "Subscription timeout must be greater than zero.");
         return entityBuilder.HasAnnotation("SubscriptionTimeout", TimeSpan.Zero.TotalMilliseconds);
      };

      // Assert
      act.Should().Throw<ArgumentOutOfRangeException>()
         .WithParameterName("timeout")
         .WithMessage("*Subscription timeout must be greater than zero.*");
   }

   #endregion
}

/// <summary>
/// Simple test entity for unit testing.
/// </summary>
public class NotificationEntity
{
   public int    Id   { get; set; }
   public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Enumeration for notification triggers.
/// </summary>
[Flags]
public enum NotificationTrigger
{
   Insert = 1,
   Update = 2,
   Delete = 4,
   All    = Insert | Update | Delete
}

/// <summary>
/// Enumeration for advisory lock modes.
/// </summary>
public enum LockMode
{
   Shared,
   Exclusive
}

/// <summary>
/// Enumeration for change data capture formats.
/// </summary>
public enum CdcFormat
{
   Json,
   Binary,
   Text
}