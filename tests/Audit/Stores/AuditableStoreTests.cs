// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Moq;

namespace Wangkanai.Audit.Stores;

public class AuditableStoreTests
{
   private DbContextOptions<TestAuditDbContext> CreateNewContextOptions()
   {
      return new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseInMemoryDatabase($"AuditStoreTest_{Guid.NewGuid()}")
            .Options;
   }

   [Fact]
   public void Constructor_WithNullContext_ThrowsException()
   {
      // Arrange & Act & Assert
      Assert.Throws<ArgumentNullException>(() => new AuditableStore<TestAuditDbContext, Guid, IdentityUser<Guid>, Guid>(null!));
   }


   [Fact]
   public async ValueTask CreateAsync_WithConcurrencyException_ReturnsError()
   {
      // Arrange
      var mockContext = new Mock<TestAuditDbContext>(CreateNewContextOptions());
      mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Throws(new DbUpdateConcurrencyException());

      var store = new AuditableStore<TestAuditDbContext, Guid, IdentityUser<Guid>, Guid>(mockContext.Object);
      var audit = new AuditableEntity<Guid, IdentityUser<Guid>, Guid>();

      // Act
      var result = await store.CreateAsync(audit, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Equal(ErrorCodes.Concurrency, result.Error?.Code);
   }

   [Fact]
   public async ValueTask UpdateAsync_WithConcurrencyException_ReturnsError()
   {
      // Arrange
      var mockContext = new Mock<TestAuditDbContext>(CreateNewContextOptions());

      mockContext.Setup(c => c.Update(It.IsAny<AuditableEntity<Guid, IdentityUser<Guid>, Guid>>()));
      mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Throws(new DbUpdateConcurrencyException());

      var store = new AuditableStore<TestAuditDbContext, Guid, IdentityUser<Guid>, Guid>(mockContext.Object);
      var audit = new AuditableEntity<Guid, IdentityUser<Guid>, Guid> { EntityName = "Test" };

      // Act
      var result = await store.UpdateAsync(audit, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Equal(ErrorCodes.Concurrency, result.Error?.Code);
   }

   [Fact]
   public async ValueTask DeleteAsync_WithConcurrencyException_ReturnsError()
   {
      // Arrange
      var mockContext = new Mock<TestAuditDbContext>(CreateNewContextOptions());
      mockContext.Setup(c => c.Remove(It.IsAny<AuditableEntity<Guid, IdentityUser<Guid>, Guid>>()));
      mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Throws(new DbUpdateConcurrencyException());

      var store = new AuditableStore<TestAuditDbContext, Guid, IdentityUser<Guid>, Guid>(mockContext.Object);
      var audit = new AuditableEntity<Guid, IdentityUser<Guid>, Guid>();

      // Act
      var result = await store.DeleteAsync(audit, CancellationToken.None);

      // Assert
      Assert.False(result.IsSuccess);
      Assert.Equal(ErrorCodes.Concurrency, result.Error?.Code);
   }

   public class TestAuditDbContext(DbContextOptions<TestAuditDbContext> options) : DbContext(options)
   {
      public DbSet<AuditableEntity<Guid, IdentityUser<Guid>, Guid>> Audits { get; set; }
      public DbSet<IdentityUser<Guid>>                    Users  { get; set; }

      protected override void OnModelCreating(ModelBuilder builder)
      {
         base.OnModelCreating(builder);
         builder.ApplyAuditableConfiguration<Guid, IdentityUser<Guid>, Guid>();
      }
   }
}