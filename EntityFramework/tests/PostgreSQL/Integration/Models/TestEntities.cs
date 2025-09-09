// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.EntityFramework.PostgreSQL.Integration.Models;

/// <summary>
/// Test entity for connection configuration testing.
/// </summary>
public class TestEntity : Entity<int>
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Test entity for JSONB testing.
/// </summary>
public class JsonEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public string Settings { get; set; } = string.Empty;
    public string UserProfile { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for array testing.
/// </summary>
public class ArrayEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int[] Scores { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public Guid[] RelatedIds { get; set; } = [];
    public decimal[] Prices { get; set; } = [];
    public DateTime[] ImportantDates { get; set; } = [];
}

/// <summary>
/// Test entity for full-text search testing.
/// </summary>
public class DocumentEntity : Entity<Guid>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string[] Categories { get; set; } = [];
    public DateTime PublishedAt { get; set; }
    public string SearchVector { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for bulk operations testing.
/// </summary>
public class BulkEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Test entity for real-time features testing.
/// </summary>
public class NotificationEntity : Entity<Guid>
{
    public string Channel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public string Sender { get; set; } = string.Empty;
    public bool IsProcessed { get; set; }
}

/// <summary>
/// Test entity for partitioning testing.
/// </summary>
public class PartitionedEntity : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Region { get; set; }
    public decimal Value { get; set; }
}

/// <summary>
/// Test entity for advanced query testing.
/// </summary>
public class OrderEntity : Entity<Guid>
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<OrderItemEntity> Items { get; set; } = [];
}

/// <summary>
/// Test entity for order items in advanced query testing.
/// </summary>
public class OrderItemEntity : Entity<Guid>
{
    public Guid OrderId { get; set; }
    public OrderEntity Order { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}