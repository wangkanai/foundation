// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Wangkanai.Audit;

namespace Wangkanai.Audit.Tests;

public class TrailTests
{
    private readonly AuditConfiguration<IdentityUser, string> _config;
    private readonly IAuditConfiguration _configInterface;

    public TrailTests()
    {
        _config = AuditConfiguration<IdentityUser, string>.Create();
        _configInterface = _config.AsInterface();
    }

    [Fact]
    public void Trail_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var trail = new Trail<int>();

        // Assert
        trail.Id.Should().BeGreaterThan(0);
        trail.TrailType.Should().Be(TrailType.None);
        trail.EntityName.Should().BeEmpty();
        trail.ChangedColumns.Should().BeEmpty();
        trail.Timestamp.Should().Be(default);
        trail.UserId.Should().BeNull();
        trail.User.Should().BeNull();
        trail.AuditConfiguration.Should().BeNull();
    }

    [Fact]
    public void Trail_ConstructorWithConfig_ShouldSetConfiguration()
    {
        // Arrange & Act
        var trail = new Trail<int>(_configInterface);

        // Assert
        trail.AuditConfiguration.Should().BeSameAs(_configInterface);
    }

    [Fact]
    public void Trail_GuidKey_ShouldAutoGenerateId()
    {
        // Arrange & Act
        var trail = new Trail<Guid>();

        // Assert
        trail.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SetUserId_WithValidType_ShouldSucceed()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        const string userId = "test-user-123";

        // Act
        var result = trail.SetUserId(userId);

        // Assert
        result.Should().BeTrue();
        trail.UserId.Should().Be(userId);
    }

    [Fact]
    public void SetUserId_WithIncompatibleType_ShouldFail()
    {
        // Arrange
        var guidConfig = AuditConfiguration<IdentityUser<Guid>, Guid>.Create().AsInterface();
        var trail = new Trail<int>(guidConfig);
        const string userId = "invalid-for-guid";

        // Act
        var result = trail.SetUserId(userId);

        // Assert
        result.Should().BeFalse();
        trail.UserId.Should().BeNull();
    }

    [Fact]
    public void SetUser_WithValidType_ShouldSucceed()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        var user = new IdentityUser { Id = "test-user", UserName = "testuser" };

        // Act
        var result = trail.SetUser(user);

        // Assert
        result.Should().BeTrue();
        trail.User.Should().BeSameAs(user);
    }

    [Fact]
    public void GetUserId_WithCorrectType_ShouldReturnValue()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        const string expectedUserId = "test-user-456";
        trail.SetUserId(expectedUserId);

        // Act
        var userId = trail.GetUserId<string>();

        // Assert
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void GetUserId_WithIncorrectType_ShouldReturnDefault()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        trail.SetUserId("test-user-456");

        // Act
        var userId = trail.GetUserId<int>();

        // Assert
        userId.Should().Be(0); // Default for int
    }

    [Fact]
    public void GetUser_WithCorrectType_ShouldReturnUser()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        var user = new IdentityUser { Id = "test", UserName = "testuser" };
        trail.SetUser(user);

        // Act
        var retrievedUser = trail.GetUser<IdentityUser>();

        // Assert
        retrievedUser.Should().BeSameAs(user);
    }

    [Fact]
    public void GetUser_WithIncorrectType_ShouldReturnNull()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        var user = new IdentityUser { Id = "test", UserName = "testuser" };
        trail.SetUser(user);

        // Act
        var retrievedUser = trail.GetUser<CustomUser>();

        // Assert
        retrievedUser.Should().BeNull();
    }

    [Fact]
    public void ValidateUserData_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var trail = new Trail<int>(_configInterface);
        var user = new IdentityUser { Id = "test", UserName = "testuser" };
        trail.SetUserId("test-user-id");
        trail.SetUser(user);

        // Act
        var isValid = trail.ValidateUserData();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateUserData_WithNoConfig_ShouldReturnTrue()
    {
        // Arrange
        var trail = new Trail<int>();
        trail.UserId = 12345; // Any type should be fine without config

        // Act
        var isValid = trail.ValidateUserData();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SetValuesFromJson_ShouldSetProperties()
    {
        // Arrange
        var trail = new Trail<int>();
        const string oldJson = """{"Name":"John","Age":30}""";
        const string newJson = """{"Name":"Jane","Age":31}""";

        // Act
        trail.SetValuesFromJson(oldJson, newJson);

        // Assert
        trail.OldValuesJson.Should().Be(oldJson);
        trail.NewValuesJson.Should().Be(newJson);
    }

    [Fact]
    public void OldValues_Property_ShouldDeserializeJson()
    {
        // Arrange
        var trail = new Trail<int>();
        const string json = """{"Name":"John","Age":30}""";
        trail.OldValuesJson = json;

        // Act
        var values = trail.OldValues;

        // Assert
        values.Should().ContainKey("Name").WhoseValue.Should().Be("John");
        values.Should().ContainKey("Age").WhoseValue.Should().Be(30);
    }

    [Fact]
    public void NewValues_Property_ShouldDeserializeJson()
    {
        // Arrange
        var trail = new Trail<int>();
        const string json = """{"Name":"Jane","Age":31}""";
        trail.NewValuesJson = json;

        // Act
        var values = trail.NewValues;

        // Assert
        values.Should().ContainKey("Name").WhoseValue.Should().Be("Jane");
        values.Should().ContainKey("Age").WhoseValue.Should().Be(31);
    }

    [Fact]
    public void GetOldValue_WithValidKey_ShouldReturnValue()
    {
        // Arrange
        var trail = new Trail<int>();
        const string json = """{"Name":"John","Age":30}""";
        trail.OldValuesJson = json;

        // Act
        var name = trail.GetOldValue("Name");

        // Assert
        name.Should().Be("John");
    }

    [Fact]
    public void GetNewValue_WithValidKey_ShouldReturnValue()
    {
        // Arrange
        var trail = new Trail<int>();
        const string json = """{"Name":"Jane","Age":31}""";
        trail.NewValuesJson = json;

        // Act
        var age = trail.GetNewValue("Age");

        // Assert
        age.Should().Be(31);
    }

    [Fact]
    public void SetValuesFromSpan_WithSmallChangeset_ShouldOptimize()
    {
        // Arrange
        var trail = new Trail<int>();
        ReadOnlySpan<string> columns = ["Name", "Age"];
        ReadOnlySpan<object> oldValues = ["John", 30];
        ReadOnlySpan<object> newValues = ["Jane", 31];

        // Act
        trail.SetValuesFromSpan(columns, oldValues, newValues);

        // Assert
        trail.ChangedColumns.Should().Contain(["Name", "Age"]);
        trail.GetOldValue("Name").Should().Be("John");
        trail.GetNewValue("Name").Should().Be("Jane");
        trail.GetOldValue("Age").Should().Be(30);
        trail.GetNewValue("Age").Should().Be(31);
    }

    [Fact]
    public void SetValuesFromSpan_WithLargeChangeset_ShouldUseDictionary()
    {
        // Arrange
        var trail = new Trail<int>();
        ReadOnlySpan<string> columns = ["Col1", "Col2", "Col3", "Col4", "Col5"];
        ReadOnlySpan<object> oldValues = ["Old1", "Old2", "Old3", "Old4", "Old5"];
        ReadOnlySpan<object> newValues = ["New1", "New2", "New3", "New4", "New5"];

        // Act
        trail.SetValuesFromSpan(columns, oldValues, newValues);

        // Assert
        trail.ChangedColumns.Should().HaveCount(5);
        trail.GetOldValue("Col3").Should().Be("Old3");
        trail.GetNewValue("Col3").Should().Be("New3");
    }

    [Fact]
    public void SetValuesFromSpan_WithMismatchedLengths_ShouldThrow()
    {
        // Arrange
        var trail = new Trail<int>();
        ReadOnlySpan<string> columns = ["Name", "Age"];
        ReadOnlySpan<object> oldValues = ["John"];
        ReadOnlySpan<object> newValues = ["Jane", 31];

        // Act & Assert
        var act = () => trail.SetValuesFromSpan(columns, oldValues, newValues);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OldValues_Setter_ShouldSerializeToJson()
    {
        // Arrange
        var trail = new Trail<int>();
        var values = new Dictionary<string, object> { ["Name"] = "John", ["Age"] = 30 };

        // Act
        trail.OldValues = values;

        // Assert
        trail.OldValuesJson.Should().NotBeNullOrEmpty();
        trail.OldValuesJson.Should().Contain("John");
        trail.OldValuesJson.Should().Contain("30");
    }

    [Fact]
    public void NewValues_Setter_ShouldSerializeToJson()
    {
        // Arrange
        var trail = new Trail<int>();
        var values = new Dictionary<string, object> { ["Name"] = "Jane", ["Age"] = 31 };

        // Act
        trail.NewValues = values;

        // Assert
        trail.NewValuesJson.Should().NotBeNullOrEmpty();
        trail.NewValuesJson.Should().Contain("Jane");
        trail.NewValuesJson.Should().Contain("31");
    }

    [Fact]
    public void OldValues_WithEmptyJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var trail = new Trail<int>();
        trail.OldValuesJson = string.Empty;

        // Act
        var values = trail.OldValues;

        // Assert
        values.Should().BeEmpty();
    }

    [Fact]
    public void NewValues_WithEmptyJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var trail = new Trail<int>();
        trail.NewValuesJson = string.Empty;

        // Act
        var values = trail.NewValues;

        // Assert
        values.Should().BeEmpty();
    }

    [Fact]
    public void GetOldValue_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var trail = new Trail<int>();
        trail.OldValuesJson = "invalid json";

        // Act
        var value = trail.GetOldValue("Name");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetNewValue_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var trail = new Trail<int>();
        trail.NewValuesJson = "invalid json";

        // Act
        var value = trail.GetNewValue("Name");

        // Assert
        value.Should().BeNull();
    }

    private class CustomUser : IdentityUser<string>
    {
    }
}