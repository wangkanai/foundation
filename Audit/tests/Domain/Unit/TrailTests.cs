// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Wangkanai.Audit;

namespace Wangkanai.Audit.Tests;

public class TrailTests
{
   #region Basic Property Tests

   [Fact]
   public void Trail_Constructor_InitializesProperties()
   {
      // Arrange & Act
      var trail = new Trail<Guid, TestUser, string>();

      // Assert
      Assert.NotEqual(Guid.Empty, trail.Id);
      Assert.Equal(TrailType.None, trail.TrailType);
      Assert.Null(trail.UserId);
      Assert.Null(trail.User);
      Assert.Equal(DateTime.MinValue, trail.Timestamp);
      Assert.Null(trail.PrimaryKey);
      Assert.Equal(string.Empty, trail.EntityName);
      Assert.Empty(trail.ChangedColumns);
      Assert.Null(trail.OldValuesJson);
      Assert.Null(trail.NewValuesJson);
   }

   [Fact]
   public void Trail_PropertiesAssignment_WorksCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var testUser = new TestUser { Id = "user123", UserName = "testuser" };
      var timestamp = DateTime.UtcNow;
      var id = Guid.NewGuid();

      // Act
      trail.Id = id;
      trail.TrailType = TrailType.Update;
      trail.UserId = "user123";
      trail.User = testUser;
      trail.Timestamp = timestamp;
      trail.PrimaryKey = "entity123";
      trail.EntityName = "TestEntity";
      trail.ChangedColumns = new List<string> { "Name", "Email" };

      // Assert
      Assert.Equal(id, trail.Id);
      Assert.Equal(TrailType.Update, trail.TrailType);
      Assert.Equal("user123", trail.UserId);
      Assert.Equal(testUser, trail.User);
      Assert.Equal(timestamp, trail.Timestamp);
      Assert.Equal("entity123", trail.PrimaryKey);
      Assert.Equal("TestEntity", trail.EntityName);
      Assert.Equal(2, trail.ChangedColumns.Count);
      Assert.Contains("Name", trail.ChangedColumns);
      Assert.Contains("Email", trail.ChangedColumns);
   }

   #endregion

   #region JSON Serialization/Deserialization Tests

   [Fact]
   public void OldValues_SetAndGet_SerializesCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var oldValues = new Dictionary<string, object>
      {
         { "Name", "John Doe" },
         { "Age", 30 },
         { "IsActive", true }
      };

      // Act
      trail.OldValues = oldValues;
      var retrievedValues = trail.OldValues;

      // Assert
      Assert.NotNull(trail.OldValuesJson);
      Assert.Equal("John Doe", retrievedValues["Name"].ToString());
      Assert.Equal(30, Convert.ToInt32(retrievedValues["Age"]));
      Assert.Equal(true, Convert.ToBoolean(retrievedValues["IsActive"]));
   }

   [Fact]
   public void NewValues_SetAndGet_SerializesCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var newValues = new Dictionary<string, object>
      {
         { "Name", "Jane Smith" },
         { "Age", 25 },
         { "IsActive", false }
      };

      // Act
      trail.NewValues = newValues;
      var retrievedValues = trail.NewValues;

      // Assert
      Assert.NotNull(trail.NewValuesJson);
      Assert.Equal("Jane Smith", retrievedValues["Name"].ToString());
      Assert.Equal(25, Convert.ToInt32(retrievedValues["Age"]));
      Assert.Equal(false, Convert.ToBoolean(retrievedValues["IsActive"]));
   }

   [Fact]
   public void Values_EmptyDictionary_SetsJsonToNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();

      // Act
      trail.OldValues = new Dictionary<string, object>();
      trail.NewValues = new Dictionary<string, object>();

      // Assert
      Assert.Null(trail.OldValuesJson);
      Assert.Null(trail.NewValuesJson);
   }

   [Fact]
   public void Values_NullOrEmptyJson_ReturnsEmptyDictionary()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();

      // Act & Assert - Null JSON
      Assert.Empty(trail.OldValues);
      Assert.Empty(trail.NewValues);

      // Act & Assert - Empty JSON
      trail.OldValuesJson = string.Empty;
      trail.NewValuesJson = string.Empty;
      Assert.Empty(trail.OldValues);
      Assert.Empty(trail.NewValues);
   }

   [Fact]
   public void SetValuesFromJson_DirectJsonAssignment_BypassesSerialization()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var oldJson = """{"Name":"John","Age":30}""";
      var newJson = """{"Name":"Jane","Age":25}""";

      // Act
      trail.SetValuesFromJson(oldJson, newJson);

      // Assert
      Assert.Equal(oldJson, trail.OldValuesJson);
      Assert.Equal(newJson, trail.NewValuesJson);
   }

   [Fact]
   public void SetValuesFromJson_NullValues_HandlesCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();

      // Act
      trail.SetValuesFromJson(null, null);

      // Assert
      Assert.Null(trail.OldValuesJson);
      Assert.Null(trail.NewValuesJson);
      Assert.Empty(trail.OldValues);
      Assert.Empty(trail.NewValues);
   }

   #endregion

   #region Span Operations Tests

   [Fact]
   public void SetValuesFromSpan_SmallChangeSet_UsesOptimizedPath()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "Name", "Age", "IsActive" };
      var oldValues = new object[] { "John", 30, true };
      var newValues = new object[] { "Jane", 25, false };

      // Act
      trail.SetValuesFromSpan(columnNames.AsSpan(), oldValues.AsSpan(), newValues.AsSpan());

      // Assert
      Assert.NotNull(trail.OldValuesJson);
      Assert.NotNull(trail.NewValuesJson);
      Assert.Equal(3, trail.ChangedColumns.Count);
      Assert.Contains("Name", trail.ChangedColumns);
      Assert.Contains("Age", trail.ChangedColumns);
      Assert.Contains("IsActive", trail.ChangedColumns);

      // Verify JSON structure is correct
      Assert.Contains("\"Name\":", trail.OldValuesJson);
      Assert.Contains("\"Age\":", trail.OldValuesJson);
      Assert.Contains("\"IsActive\":", trail.OldValuesJson);
   }

   [Fact]
   public void SetValuesFromSpan_LargeChangeSet_UsesDictionaryApproach()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "Col1", "Col2", "Col3", "Col4", "Col5"};
      var oldValues = new object[] { "Val1", "Val2", "Val3", "Val4", "Val5"};
      var newValues = new object[] { "New1", "New2", "New3", "New4", "New5"};

      // Act
      trail.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Assert
      Assert.NotNull(trail.OldValuesJson);
      Assert.NotNull(trail.NewValuesJson);
      Assert.Equal(5, trail.ChangedColumns.Count);

      // Should use dictionary serialization path
      var oldDict = trail.OldValues;
      var newDict = trail.NewValues;
      Assert.Equal("Val1", oldDict["Col1"].ToString());
      Assert.Equal("New1", newDict["Col1"].ToString());
   }

   [Fact]
   public void SetValuesFromSpan_MismatchedLengths_ThrowsException()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "Name", "Age"};
      var oldValues = new object[] { "John"};  // Different length
      var newValues = new object[] { "Jane", "25"};

      // Act & Assert
      var exception = Assert.Throws<ArgumentException>(() =>
         trail.SetValuesFromSpan(columnNames, oldValues, newValues));
      Assert.Contains("All spans must have the same length", exception.Message);
   }

   [Fact]
   public void SetValuesFromSpan_EmptySpans_HandlesCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { };
      var oldValues = new object[] { };
      var newValues = new object[] { };

      // Act
      trail.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Assert
      Assert.Empty(trail.ChangedColumns);
      // For empty spans, JSON should be set to "{}" 
      Assert.Equal("{}", trail.OldValuesJson);
      Assert.Equal("{}", trail.NewValuesJson);
   }

   #endregion

   #region Performance-Optimized Value Access Tests

   [Fact]
   public void GetOldValue_ExistingColumn_ReturnsCorrectValue()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = """{"Name":"John","Age":30,"IsActive":true}""";

      // Act
      var nameValue = trail.GetOldValue("Name");
      var ageValue = trail.GetOldValue("Age");
      var isActiveValue = trail.GetOldValue("IsActive");

      // Assert
      Assert.Equal("John", nameValue?.ToString());
      Assert.Equal(30, Convert.ToInt64(ageValue));
      Assert.True(Convert.ToBoolean(isActiveValue));
   }

   [Fact]
   public void GetNewValue_ExistingColumn_ReturnsCorrectValue()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.NewValuesJson = """{"Name":"Jane","Age":25,"IsActive":false}""";

      // Act
      var nameValue = trail.GetNewValue("Name");
      var ageValue = trail.GetNewValue("Age");
      var isActiveValue = trail.GetNewValue("IsActive");

      // Assert
      Assert.Equal("Jane", nameValue?.ToString());
      Assert.Equal(25, Convert.ToInt64(ageValue));
      Assert.False(Convert.ToBoolean(isActiveValue));
   }

   [Fact]
   public void GetOldValue_NonExistentColumn_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = """{"Name":"John"}""";

      // Act
      var value = trail.GetOldValue("NonExistent");

      // Assert
      Assert.Null(value);
   }

   [Fact]
   public void GetNewValue_NonExistentColumn_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.NewValuesJson = """{"Name":"Jane"}""";

      // Act
      var value = trail.GetNewValue("NonExistent");

      // Assert
      Assert.Null(value);
   }

   [Fact]
   public void GetOldValue_NullOrEmptyJson_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();

      // Act & Assert - Null JSON
      Assert.Null(trail.GetOldValue("Name"));

      // Act & Assert - Empty JSON
      trail.OldValuesJson = string.Empty;
      Assert.Null(trail.GetOldValue("Name"));
   }

   [Fact]
   public void GetNewValue_NullOrEmptyJson_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();

      // Act & Assert - Null JSON
      Assert.Null(trail.GetNewValue("Name"));

      // Act & Assert - Empty JSON
      trail.NewValuesJson = string.Empty;
      Assert.Null(trail.GetNewValue("Name"));
   }

   [Fact]
   public void GetValue_WithNullJsonValue_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = """{"Name":null,"Age":25}""";

      // Act
      var nameValue = trail.GetOldValue("Name");
      var ageValue = trail.GetOldValue("Age");

      // Assert
      Assert.Null(nameValue);
      Assert.Equal(25, Convert.ToInt64(ageValue));
   }

   [Fact]
   public void GetValue_WithDifferentJsonTypes_ReturnsCorrectTypes()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = """
         {
            "StringValue": "text",
            "IntValue": 42,
            "DoubleValue": 3.14,
            "BoolValue": true,
            "NullValue": null
         }
         """;

      // Act & Assert
      Assert.Equal("text", trail.GetOldValue("StringValue")?.ToString());
      Assert.Equal(42, Convert.ToInt64(trail.GetOldValue("IntValue")));
      Assert.Equal(3.14, Convert.ToDouble(trail.GetOldValue("DoubleValue")));
      Assert.True(Convert.ToBoolean(trail.GetOldValue("BoolValue")));
      Assert.Null(trail.GetOldValue("NullValue"));
   }

   #endregion

   #region JSON Building Optimization Tests

   [Fact]
   public void BuildJsonFromSpan_StringValues_EscapesQuotes()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "Name", "Description"};
      ReadOnlySpan<string> values = ["John \"Johnny\" Doe", "A \"test\" description"];

      // Act
      trail.SetValuesFromSpan(columnNames, values.Cast<object>().ToArray().AsSpan(), values.Cast<object>().ToArray().AsSpan());

      // Assert
      Assert.Contains("John \\\"Johnny\\\" Doe", trail.OldValuesJson);
      Assert.Contains("A \\\"test\\\" description", trail.NewValuesJson);
   }

   [Fact]
   public void BuildJsonFromSpan_BooleanValues_FormatsCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "IsActive", "IsDeleted"};
      ReadOnlySpan<bool> values = [true, false];

      // Act
      trail.SetValuesFromSpan(columnNames, values.Cast<object>().ToArray().AsSpan(), values.Cast<object>().ToArray().AsSpan());

      // Assert
      Assert.Contains("\"IsActive\":true", trail.OldValuesJson);
      Assert.Contains("\"IsDeleted\":false", trail.NewValuesJson);
   }

   [Fact]
   public void BuildJsonFromSpan_DateTimeValues_FormatsWithIso8601()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var dateTime = new DateTime(2023, 12, 25, 10, 30, 45, 123, DateTimeKind.Utc);
      var columnNames = new string[] { "CreatedAt"};
      ReadOnlySpan<DateTime> values = [dateTime];

      // Act
      trail.SetValuesFromSpan(columnNames, values.Cast<object>().ToArray().AsSpan(), values.Cast<object>().ToArray().AsSpan());

      // Assert
      Assert.Contains("\"CreatedAt\":\"2023-12-25T10:30:45.123Z\"", trail.OldValuesJson);
   }

   [Fact]
   public void BuildJsonFromSpan_NullValues_HandlesCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var columnNames = new string[] { "Name", "Description"};
      ReadOnlySpan<object?> values = [null, "Valid Description"];

      // Act
      trail.SetValuesFromSpan(columnNames, values.ToArray().AsSpan(), values.ToArray().AsSpan());

      // Assert
      Assert.Contains("\"Name\":null", trail.OldValuesJson);
      Assert.Contains("\"Description\":\"Valid Description\"", trail.NewValuesJson);
   }

   #endregion

   #region Error Handling and Edge Cases

   [Fact]
   public void Trail_WithInvalidJson_HandlesGracefully()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = "invalid json {";

      // Act & Assert - Should not throw, should return empty dictionary
      var oldValues = trail.OldValues;
      Assert.Empty(oldValues);
   }

   [Fact]
   public void GetValue_WithInvalidJson_ReturnsNull()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      trail.OldValuesJson = "invalid json {";

      // Act
      var value = trail.GetOldValue("Name");

      // Assert
      Assert.Null(value);
   }

   [Fact]
   public void Trail_LargeJsonValues_HandlesEfficiently()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var largeText = new string('A', 10000);
      var values = new Dictionary<string, object>
      {
         { "LargeText", largeText },
         { "NormalText", "Small" }
      };

      // Act
      trail.OldValues = values;
      var retrievedValues = trail.OldValues;

      // Assert
      Assert.Equal(largeText, retrievedValues["LargeText"].ToString());
      Assert.Equal("Small", retrievedValues["NormalText"].ToString());
   }

   [Fact]
   public void Trail_ConcurrentAccess_ThreadSafe()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      var tasks = new List<Task>();

      // Act - Simulate concurrent access
      for (int i = 0; i < 10; i++)
      {
         int iteration = i;
         tasks.Add(Task.Run(() =>
         {
            trail.OldValues = new Dictionary<string, object> 
            { 
               { $"Key{iteration}", $"Value{iteration}" } 
            };
            var retrieved = trail.GetOldValue($"Key{iteration}");
         }));
      }

      // Assert - Should not throw
      Assert.All(tasks, task => task.Wait(TimeSpan.FromSeconds(5)));
   }

   #endregion

   #region Integration Tests

   [Fact]
   public void Trail_CompleteWorkflow_WorksCorrectly()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>
      {
         Id = Guid.NewGuid(),
         TrailType = TrailType.Update,
         UserId = "user123",
         User = new TestUser { Id = "user123", UserName = "testuser" },
         Timestamp = DateTime.UtcNow,
         PrimaryKey = "entity456",
         EntityName = "Customer"
      };

      // Simulate change tracking
      ReadOnlySpan<string> changedColumns = ["Name", "Email", "LastModified"];
      var oldValues = new object[] { "John Doe", "john@old.com", DateTime.UtcNow.AddDays(-1)};
      var newValues = new object[] { "Jane Doe", "jane@new.com", DateTime.UtcNow};

      // Act
      trail.SetValuesFromSpan(changedColumns, oldValues, newValues);

      // Assert - Verify all properties are set correctly
      Assert.Equal(TrailType.Update, trail.TrailType);
      Assert.Equal("user123", trail.UserId);
      Assert.Equal("Customer", trail.EntityName);
      Assert.Equal(3, trail.ChangedColumns.Count);

      // Verify value access methods work
      Assert.Equal("John Doe", trail.GetOldValue("Name")?.ToString());
      Assert.Equal("jane@new.com", trail.GetNewValue("Email")?.ToString());

      // Verify dictionary access works
      var oldDict = trail.OldValues;
      var newDict = trail.NewValues;
      Assert.Equal("John Doe", oldDict["Name"].ToString());
      Assert.Equal("Jane Doe", newDict["Name"].ToString());
   }

   [Fact]
   public void Trail_PerformanceOptimizations_WorkTogether()
   {
      // Arrange
      var trail = new Trail<Guid, TestUser, string>();
      
      // Test small change set (optimized path)
      ReadOnlySpan<string> smallColumns = ["Name", "Email"];
      ReadOnlySpan<object> smallOld = ["John", "john@test.com"];
      ReadOnlySpan<object> smallNew = ["Jane", "jane@test.com"];

      // Act
      trail.SetValuesFromSpan(smallColumns, smallOld, smallNew);

      // Assert - Verify optimized path was used and efficient access works
      Assert.NotNull(trail.OldValuesJson);
      Assert.Equal("John", trail.GetOldValue("Name")?.ToString());
      Assert.Equal("jane@test.com", trail.GetNewValue("Email")?.ToString());

      // Verify change to larger set uses dictionary approach
      ReadOnlySpan<string> largeColumns = ["Col1", "Col2", "Col3", "Col4", "Col5"];
      ReadOnlySpan<object> largeOld = ["V1", "V2", "V3", "V4", "V5"];
      ReadOnlySpan<object> largeNew = ["N1", "N2", "N3", "N4", "N5"];

      trail.SetValuesFromSpan(largeColumns, largeOld, largeNew);

      Assert.Equal(5, trail.ChangedColumns.Count);
      Assert.Equal("V1", trail.GetOldValue("Col1")?.ToString());
      Assert.Equal("N1", trail.GetNewValue("Col1")?.ToString());
   }

   #endregion
}

#region Test Helper Classes

/// <summary>Test user class for Trail tests</summary>
public class TestUser : IdentityUser<string>
{
   public TestUser()
   {
      Id = Guid.NewGuid().ToString();
   }
}

#endregion