// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

public class AuditTests
{
   [Fact]
   public void Audit_ChangedColumns_ShouldBeInitialized()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>();

      // Act & Assert
      Assert.NotNull(audit.ChangedColumns);
      Assert.Empty(audit.ChangedColumns);
   }

   [Fact]
   public void Audit_OldValues_ShouldBeInitialized()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>();

      // Act & Assert
      Assert.NotNull(audit.OldValues);
      Assert.Empty(audit.OldValues);
   }

   [Fact]
   public void Audit_NewValues_ShouldBeInitialized()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>();

      // Act & Assert
      Assert.NotNull(audit.NewValues);
      Assert.Empty(audit.NewValues);
   }

   [Fact]
   public void Audit_SetTrailType_ShouldRetainValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     TrailType = AuditTrailType.Create
                  };

      // Act & Assert
      Assert.Equal(AuditTrailType.Create, audit.TrailType);
   }

   [Fact]
   public void Audit_SetUserId_ShouldRetainValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     UserId = 1
                  };

      // Act & Assert
      Assert.Equal(1, audit.UserId);
   }

   [Fact]
   public void Audit_SetUser_ShouldRetainValue()
   {
      // Arrange
      var user = new IdentityUser<int> { Id = 1, UserName = "TestUser" };
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     User = user
                  };

      // Act & Assert
      Assert.NotNull(audit.User);
      Assert.Equal("TestUser", audit.User.UserName);
   }

   [Fact]
   public void Audit_SetTimestamp_ShouldRetainValue()
   {
      // Arrange
      var timestamp = DateTime.UtcNow;
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     Timestamp = timestamp
                  };

      // Act & Assert
      Assert.Equal(timestamp, audit.Timestamp);
   }

   [Fact]
   public void Audit_SetPrimaryKey_ShouldRetainValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     PrimaryKey = "123"
                  };

      // Act & Assert
      Assert.Equal("123", audit.PrimaryKey);
   }

   [Fact]
   public void Audit_SetEntityName_ShouldRetainValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     EntityName = "TestEntity"
                  };

      // Act & Assert
      Assert.Equal("TestEntity", audit.EntityName);
   }

   [Fact]
   public void Audit_SetChangedColumns_ShouldRetainValues()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     ChangedColumns = ["Column1", "Column2"]
                  };

      // Act & Assert
      Assert.NotEmpty(audit.ChangedColumns);
      Assert.Contains("Column1", audit.ChangedColumns);
      Assert.Contains("Column2", audit.ChangedColumns);
      Assert.Equal(2, audit.ChangedColumns.Count);
   }

   [Fact]
   public void Audit_SetOldValues_ShouldRetainValues()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     OldValues = new() { { "Column1", "OldValue1" } }
                  };

      // Act & Assert
      Assert.NotEmpty(audit.OldValues);
      var actualValue = audit.OldValues["Column1"];
      Assert.Equal("OldValue1", actualValue?.ToString()); // Convert to string for comparison
      Assert.Single(audit.OldValues);
      Assert.NotNull(audit.OldValuesJson);
   }

   [Fact]
   public void Audit_SetNewValues_ShouldRetainValues()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     NewValues = new() { { "Column1", "NewValue1" } }
                  };

      // Act & Assert
      Assert.NotEmpty(audit.NewValues);
      var actualValue = audit.NewValues["Column1"];
      Assert.Equal("NewValue1", actualValue?.ToString()); // Convert to string for comparison
      Assert.Single(audit.NewValues);
      Assert.NotNull(audit.NewValuesJson);
   }

   [Fact]
   public void Audit_SetValuesFromJson_ShouldStoreJsonDirectly()
   {
      // Arrange
      var audit   = new Audit<int, IdentityUser<int>, int>();
      var oldJson = "{\"Column1\":\"OldValue1\",\"Column2\":42}";
      var newJson = "{\"Column1\":\"NewValue1\",\"Column2\":84}";

      // Act
      audit.SetValuesFromJson(oldJson, newJson);

      // Assert
      Assert.Equal(oldJson,     audit.OldValuesJson);
      Assert.Equal(newJson,     audit.NewValuesJson);
      Assert.Equal("OldValue1", audit.GetOldValue("Column1"));
      Assert.Equal("NewValue1", audit.GetNewValue("Column1"));
   }

   [Fact]
   public void Audit_SetValuesFromSpan_SmallChangeSet_ShouldOptimize()
   {
      // Arrange
      var                  audit       = new Audit<int, IdentityUser<int>, int>();
      ReadOnlySpan<string> columnNames = ["Column1", "Column2"];
      ReadOnlySpan<object> oldValues   = ["OldValue1", 42];
      ReadOnlySpan<object> newValues   = ["NewValue1", 84];

      // Act
      audit.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Assert
      Assert.NotNull(audit.OldValuesJson);
      Assert.NotNull(audit.NewValuesJson);
      Assert.Equal("OldValue1", audit.GetOldValue("Column1"));
      Assert.Equal("NewValue1", audit.GetNewValue("Column1"));
      Assert.Equal(2,           audit.ChangedColumns.Count);
      Assert.Contains("Column1", audit.ChangedColumns);
      Assert.Contains("Column2", audit.ChangedColumns);
   }

   [Fact]
   public void Audit_SetValuesFromSpan_LargeChangeSet_ShouldUseDictionary()
   {
      // Arrange
      var                  audit       = new Audit<int, IdentityUser<int>, int>();
      ReadOnlySpan<string> columnNames = ["Col1", "Col2", "Col3", "Col4", "Col5"];
      ReadOnlySpan<object> oldValues   = ["Old1", "Old2", "Old3", "Old4", "Old5"];
      ReadOnlySpan<object> newValues   = ["New1", "New2", "New3", "New4", "New5"];

      // Act
      audit.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Assert
      Assert.NotNull(audit.OldValuesJson);
      Assert.NotNull(audit.NewValuesJson);
      Assert.Equal("Old1", audit.GetOldValue("Col1"));
      Assert.Equal("New1", audit.GetNewValue("Col1"));
      Assert.Equal(5,      audit.ChangedColumns.Count);
   }

   [Fact]
   public void Audit_GetOldValue_WithoutFullDeserialization_ShouldReturnCorrectValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson("{\"Column1\":\"TestValue\",\"Column2\":123}", null);

      // Act
      var value1      = audit.GetOldValue("Column1");
      var value2      = audit.GetOldValue("Column2");
      var nonExistent = audit.GetOldValue("NonExistent");

      // Assert
      Assert.Equal("TestValue", value1);
      Assert.Equal(123.0,       Convert.ToDouble(value2)); // JSON numbers are deserialized as double
      Assert.Null(nonExistent);
   }

   [Fact]
   public void Audit_GetNewValue_WithoutFullDeserialization_ShouldReturnCorrectValue()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>();
      audit.SetValuesFromJson(null, "{\"Column1\":\"TestValue\",\"Column2\":true}");

      // Act
      var value1      = audit.GetNewValue("Column1");
      var value2      = audit.GetNewValue("Column2");
      var nonExistent = audit.GetNewValue("NonExistent");

      // Assert
      Assert.Equal("TestValue", value1);
      Assert.True((bool)value2!);
      Assert.Null(nonExistent);
   }

   [Fact]
   public void Audit_EmptyValues_ShouldOptimizeStorage()
   {
      // Arrange
      var audit = new Audit<int, IdentityUser<int>, int>
                  {
                     OldValues = new(),
                     NewValues = new()
                  };

      // Act & Assert
      Assert.Null(audit.OldValuesJson);
      Assert.Null(audit.NewValuesJson);
      Assert.Empty(audit.OldValues);
      Assert.Empty(audit.NewValues);
   }

   [Fact]
   public void Audit_SetValuesFromSpan_MismatchedLengths_ShouldThrowException()
   {
      // Arrange
      var audit       = new Audit<int, IdentityUser<int>, int>();
      var columnNames = new[] { "Column1", "Column2" };
      var oldValues   = new object[] { "OldValue1" };
      var newValues   = new object[] { "NewValue1", "NewValue2" };

      // Act & Assert
      Assert.Throws<ArgumentException>(() =>
                                          audit.SetValuesFromSpan<object>(columnNames.AsSpan(), oldValues.AsSpan(), newValues.AsSpan()));
   }

   [Fact]
   public void Audit_JsonSerialization_ShouldHandleComplexTypes()
   {
      // Arrange
      var                  audit       = new Audit<int, IdentityUser<int>, int>();
      var                  dateTime    = DateTime.Parse("2025-01-15T10:30:00Z");
      ReadOnlySpan<string> columnNames = ["StringCol", "DateCol", "BoolCol", "NullCol"];
      ReadOnlySpan<object> oldValues   = ["TestString", dateTime, true, null!];
      ReadOnlySpan<object> newValues   = ["NewString", dateTime.AddDays(1), false, "NotNull"];

      // Act
      audit.SetValuesFromSpan(columnNames, oldValues, newValues);

      // Assert
      Assert.Equal("TestString", audit.GetOldValue("StringCol"));
      Assert.Equal("NewString",  audit.GetNewValue("StringCol"));
      Assert.True((bool)audit.GetOldValue("BoolCol")!);
      Assert.False((bool)audit.GetNewValue("BoolCol")!);
      Assert.Null(audit.GetOldValue("NullCol"));
      Assert.Equal("NotNull", audit.GetNewValue("NullCol"));
   }
}