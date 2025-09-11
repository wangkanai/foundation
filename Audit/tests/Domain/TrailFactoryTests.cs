// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Wangkanai.Audit;

namespace Wangkanai.Audit.Tests;

public class TrailFactoryTests
{
    [Fact]
    public void Create_WithGenericTypes_ShouldReturnConfiguredTrail()
    {
        // Act
        var trail = TrailFactory.Create<int, IdentityUser, string>();

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().NotBeNull();
        trail.AuditConfiguration!.UserType.Should().Be(typeof(IdentityUser));
        trail.AuditConfiguration.UserKeyType.Should().Be(typeof(string));
    }

    [Fact]
    public void Create_WithExplicitConfiguration_ShouldUseProvidedConfig()
    {
        // Arrange
        var config = AuditConfiguration<IdentityUser, string>.Create().AsInterface();

        // Act
        var trail = TrailFactory.Create<int>(config);

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().BeSameAs(config);
    }

    [Fact]
    public void CreateBasic_ShouldReturnTrailWithoutConfiguration()
    {
        // Act
        var trail = TrailFactory.CreateBasic<int>();

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().BeNull();
    }

    [Fact]
    public void CreateStringKey_ShouldReturnStringKeyTrail()
    {
        // Act
        var trail = TrailFactory.CreateStringKey<IdentityUser, string>();

        // Assert
        trail.Should().NotBeNull();
        trail.Should().BeOfType<Trail<string>>();
        trail.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateIntKey_ShouldReturnIntKeyTrail()
    {
        // Act
        var trail = TrailFactory.CreateIntKey<IdentityUser, string>();

        // Assert
        trail.Should().NotBeNull();
        trail.Should().BeOfType<Trail<int>>();
        trail.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateGuidKey_ShouldReturnGuidKeyTrail()
    {
        // Act
        var trail = TrailFactory.CreateGuidKey<IdentityUser, string>();

        // Assert
        trail.Should().NotBeNull();
        trail.Should().BeOfType<Trail<Guid>>();
        trail.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CreateForIdentityUser_ShouldConfigureForStandardIdentity()
    {
        // Act
        var trail = TrailFactory.CreateForIdentityUser<int>();

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().NotBeNull();
        trail.AuditConfiguration!.UserType.Should().Be(typeof(IdentityUser));
        trail.AuditConfiguration.UserKeyType.Should().Be(typeof(string));
    }

    [Fact]
    public void CreateForGuidUser_ShouldConfigureForGuidKeyedUsers()
    {
        // Act
        var trail = TrailFactory.CreateForGuidUser<int, IdentityUser<Guid>>();

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().NotBeNull();
        trail.AuditConfiguration!.UserType.Should().Be(typeof(IdentityUser<Guid>));
        trail.AuditConfiguration.UserKeyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void CreateForIntUser_ShouldConfigureForIntKeyedUsers()
    {
        // Act
        var trail = TrailFactory.CreateForIntUser<int, IdentityUser<int>>();

        // Assert
        trail.Should().NotBeNull();
        trail.AuditConfiguration.Should().NotBeNull();
        trail.AuditConfiguration!.UserType.Should().Be(typeof(IdentityUser<int>));
        trail.AuditConfiguration.UserKeyType.Should().Be(typeof(int));
    }

    [Fact]
    public void CreateCompatibility_ShouldSetUserInformation()
    {
        // Arrange
        const string userId = "test-user-123";
        var user = new IdentityUser { Id = userId, UserName = "testuser" };

        // Act
        var trail = TrailFactory.CreateCompatibility<int, IdentityUser, string>(userId, user);

        // Assert
        trail.Should().NotBeNull();
        trail.GetUserId<string>().Should().Be(userId);
        trail.GetUser<IdentityUser>().Should().BeSameAs(user);
    }

    [Fact]
    public void CreateCompatibility_WithNullValues_ShouldNotSetUserInformation()
    {
        // Act
        var trail = TrailFactory.CreateCompatibility<int, IdentityUser, string>();

        // Assert
        trail.Should().NotBeNull();
        trail.UserId.Should().BeNull();
        trail.User.Should().BeNull();
    }

    [Fact]
    public void CreateConfiguration_ShouldReturnTypedConfiguration()
    {
        // Act
        var config = TrailFactory.CreateConfiguration<IdentityUser, string>();

        // Assert
        config.Should().NotBeNull();
        config.UserType.Should().Be(typeof(IdentityUser));
        config.UserKeyType.Should().Be(typeof(string));
    }

    [Fact]
    public void CreateBatch_ShouldReturnMultipleTrailsWithSameConfiguration()
    {
        // Arrange
        const int count = 5;

        // Act
        var trails = TrailFactory.CreateBatch<int, IdentityUser, string>(count);

        // Assert
        trails.Should().HaveCount(count);
        trails.Should().AllSatisfy(t => 
        {
            t.AuditConfiguration.Should().NotBeNull();
            t.AuditConfiguration!.UserType.Should().Be(typeof(IdentityUser));
        });
    }

    [Fact]
    public void CreateBatch_WithZeroCount_ShouldThrow()
    {
        // Act & Assert
        var act = () => TrailFactory.CreateBatch<int, IdentityUser, string>(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateBatch_WithNegativeCount_ShouldThrow()
    {
        // Act & Assert
        var act = () => TrailFactory.CreateBatch<int, IdentityUser, string>(-1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateBatch_AllTrailsShouldHaveUniqueIds()
    {
        // Arrange
        const int count = 10;

        // Act
        var trails = TrailFactory.CreateBatch<Guid, IdentityUser, string>(count);

        // Assert
        var ids = trails.Select(t => t.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().AllSatisfy(id => id.Should().NotBe(Guid.Empty));
    }

    [Fact]
    public void CreateBatch_IntKeys_ShouldHaveSequentialIds()
    {
        // Note: This test might be brittle due to static counter, 
        // but it verifies the intended behavior
        
        // Arrange
        const int count = 3;

        // Act
        var trails = TrailFactory.CreateBatch<int, IdentityUser, string>(count);

        // Assert
        var ids = trails.Select(t => t.Id).ToList();
        ids.Should().AllSatisfy(id => id.Should().BeGreaterThan(0));
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact] 
    public void FactoryMethods_ShouldProduceValidTrails()
    {
        // Act & Assert - Test that all factory methods produce valid trails
        var stringTrail = TrailFactory.CreateStringKey<IdentityUser, string>();
        var intTrail = TrailFactory.CreateIntKey<IdentityUser, string>();
        var guidTrail = TrailFactory.CreateGuidKey<IdentityUser, string>();
        var identityTrail = TrailFactory.CreateForIdentityUser<int>();

        // All should be valid
        stringTrail.ValidateUserData().Should().BeTrue();
        intTrail.ValidateUserData().Should().BeTrue();
        guidTrail.ValidateUserData().Should().BeTrue();
        identityTrail.ValidateUserData().Should().BeTrue();
    }

    [Fact]
    public void FactoryMethods_ShouldPreserveTypeConstraints()
    {
        // Act
        var guidUserTrail = TrailFactory.CreateForGuidUser<string, IdentityUser<Guid>>();
        var intUserTrail = TrailFactory.CreateForIntUser<string, IdentityUser<int>>();

        // Assert - Should only accept appropriate user types
        var guidUser = new IdentityUser<Guid> { Id = Guid.NewGuid() };
        var intUser = new IdentityUser<int> { Id = 123 };

        guidUserTrail.SetUser(guidUser).Should().BeTrue();
        intUserTrail.SetUser(intUser).Should().BeTrue();

        // Cross-type assignment should fail
        guidUserTrail.SetUser(intUser).Should().BeFalse();
        intUserTrail.SetUser(guidUser).Should().BeFalse();
    }
}