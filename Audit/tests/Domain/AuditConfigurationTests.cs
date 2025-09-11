// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Wangkanai.Audit;

namespace Wangkanai.Audit.Tests;

public class AuditConfigurationTests
{
    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Assert
        config.Should().NotBeNull();
        config.UserType.Should().Be(typeof(IdentityUser));
        config.UserKeyType.Should().Be(typeof(string));
    }

    [Fact]
    public void IsValidUser_WithCorrectType_ShouldReturnTrue()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var user = new IdentityUser { Id = "test", UserName = "testuser" };

        // Act
        var result = config.IsValidUser(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidUser_WithIncorrectType_ShouldReturnFalse()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var user = new CustomUser { Id = "test" };

        // Act
        var result = config.IsValidUser(user);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUser_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsValidUser(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUserKey_WithCorrectType_ShouldReturnTrue()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsValidUserKey("test-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidUserKey_WithIncorrectType_ShouldReturnFalse()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsValidUserKey(123);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidUserKey_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsValidUserKey(null);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CastUser_WithCorrectType_ShouldReturnUser()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var user = new IdentityUser { Id = "test", UserName = "testuser" };

        // Act
        var result = config.CastUser(user);

        // Assert
        result.Should().BeSameAs(user);
    }

    [Fact]
    public void CastUser_WithIncorrectType_ShouldReturnNull()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var user = new CustomUser { Id = "test" };

        // Act
        var result = config.CastUser(user);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void CastUserKey_WithCorrectType_ShouldReturnKey()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        const string key = "test-key";

        // Act
        var result = config.CastUserKey(key);

        // Assert
        result.Should().Be(key);
    }

    [Fact]
    public void CastUserKey_WithIncorrectType_ShouldReturnDefault()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.CastUserKey(123);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DefaultUserKey_ShouldReturnDefault()
    {
        // Arrange
        var stringConfig = AuditConfiguration<IdentityUser, string>.Create();
        var intConfig = AuditConfiguration<IdentityUser<int>, int>.Create();

        // Act & Assert
        stringConfig.DefaultUserKey.Should().BeNull();
        intConfig.DefaultUserKey.Should().Be(0);
    }

    [Fact]
    public void IsDefaultUserKey_WithDefaultValue_ShouldReturnTrue()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsDefaultUserKey(null);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsDefaultUserKey_WithNonDefaultValue_ShouldReturnFalse()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var result = config.IsDefaultUserKey("test");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AsInterface_ShouldReturnInterfaceImplementation()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();

        // Act
        var interfaceConfig = config.AsInterface();

        // Assert
        interfaceConfig.Should().NotBeNull();
        interfaceConfig.UserType.Should().Be(typeof(IdentityUser));
        interfaceConfig.UserKeyType.Should().Be(typeof(string));
    }

    [Fact]
    public void AsInterface_IsValidUser_ShouldWorkThroughInterface()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var interfaceConfig = config.AsInterface();
        var user = new IdentityUser { Id = "test" };

        // Act
        var result = interfaceConfig.IsValidUser(user);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AsInterface_IsValidUserKey_ShouldWorkThroughInterface()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create();
        var interfaceConfig = config.AsInterface();

        // Act
        var result = interfaceConfig.IsValidUserKey("test-key");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GuidUserConfiguration_ShouldWorkCorrectly()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser<Guid>, Guid>.Create();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var userId = Guid.NewGuid();

        // Act & Assert
        config.UserType.Should().Be(typeof(IdentityUser<Guid>));
        config.UserKeyType.Should().Be(typeof(Guid));
        config.IsValidUser(user).Should().BeTrue();
        config.IsValidUserKey(userId).Should().BeTrue();
        config.IsDefaultUserKey(Guid.Empty).Should().BeTrue();
        config.IsDefaultUserKey(userId).Should().BeFalse();
    }

    [Fact]
    public void IntUserConfiguration_ShouldWorkCorrectly()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser<int>, int>.Create();
        var user = new IdentityUser<int> { Id = 123 };
        const int userId = 456;

        // Act & Assert
        config.UserType.Should().Be(typeof(IdentityUser<int>));
        config.UserKeyType.Should().Be(typeof(int));
        config.IsValidUser(user).Should().BeTrue();
        config.IsValidUserKey(userId).Should().BeTrue();
        config.IsDefaultUserKey(0).Should().BeTrue();
        config.IsDefaultUserKey(userId).Should().BeFalse();
    }

    private class CustomUser : IdentityUser<string>
    {
    }
}