// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

namespace Wangkanai.EntityFramework;

public class TestDbContext : DbContext
{
   public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

   public DbSet<TestEntity>                TestEntities                 { get; set; }
   public DbSet<TestEntityWithRowVersion>  TestEntitiesWithRowVersion   { get; set; }
   public DbSet<TestEntityWithValueObject> TestEntitiesWithValueObjects { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      // Configure entities
      modelBuilder.Entity<TestEntity>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
         entity.Property(e => e.Description).HasMaxLength(1000);
      });

      modelBuilder.Entity<TestEntityWithRowVersion>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
         entity.Property(e => e.Description).HasMaxLength(1000);
         entity.Property(e => e.RowVersion).IsRowVersion();
      });

      modelBuilder.Entity<TestEntityWithValueObject>(entity =>
      {
         entity.HasKey(e => e.Id);
         entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

         // Configure value object as owned type
         entity.OwnsOne(e => e.Address, address =>
         {
            address.Property(a => a.Street).HasMaxLength(200);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.State).HasMaxLength(50);
            address.Property(a => a.ZipCode).HasMaxLength(20);
         });
      });
   }
}