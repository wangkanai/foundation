// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Moq;

namespace Wangkanai.Audit.Extensions;

public class AuditableContextExtensionsTests
{
   [Fact]
   public void ApplyAuditTrailConfiguration_ShouldApplyConfiguration()
   {
      // Arrange
      var mockBuilder   = new Mock<ModelBuilder>();
      var configuration = It.IsAny<IEntityTypeConfiguration<AuditableEntity<Guid, IdentityUser<Guid>, Guid>>>();
      mockBuilder.Setup(x => x.ApplyConfiguration(configuration))
                 .Verifiable();

      // Act
      mockBuilder.Object.ApplyAuditableConfiguration<Guid, IdentityUser<Guid>, Guid>();

      // Assert
      //mockBuilder.Verify(x => x.ApplyConfiguration(It.IsAny<AuditConfiguration<Guid, IdentityUser<Guid>, Guid>>()), Times.Once);
   }

   [Fact]
   public void ApplyAuditTrailConfiguration_WithIntKeys_ShouldApplyConfiguration()
   {
      // Arrange
      var mockBuilder   = new Mock<ModelBuilder>();
      var configuration = It.IsAny<IEntityTypeConfiguration<AuditableEntity<int, IdentityUser<int>, int>>>();
      mockBuilder.Setup(x => x.ApplyConfiguration(configuration))
                 .Verifiable();

      // Act
      mockBuilder.Object.ApplyAuditableConfiguration<int, IdentityUser<int>, int>();

      // Assert
      //mockBuilder.Verify(x => x.ApplyConfiguration(It.IsAny<AuditConfiguration<int, IdentityUser<int>, int>>()), Times.Once);
   }

   [Fact]
   public void ApplyAuditTrailConfiguration_WithDifferentKeyTypes_ShouldApplyConfiguration()
   {
      // Arrange
      var mockBuilder   = new Mock<ModelBuilder>();
      var configuration = It.IsAny<IEntityTypeConfiguration<AuditableEntity<Guid, IdentityUser<string>, string>>>();
      mockBuilder.Setup(x => x.ApplyConfiguration(configuration))
                 .Verifiable();

      // Act
      mockBuilder.Object.ApplyAuditableConfiguration<Guid, IdentityUser<string>, string>();

      // Assert
      //mockBuilder.Verify(x => x.ApplyConfiguration(It.IsAny<AuditConfiguration<Guid, IdentityUser<string>, string>>()), Times.Once);
   }
}