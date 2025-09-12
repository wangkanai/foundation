// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

namespace Wangkanai.EntityFramework;

public class DatabaseMigrationExtensions
{
   [Fact]
   public void IsDbContextSubClass()
   {
      Assert.True(typeof(FooDbContext).IsSubclassOf(typeof(DbContext)));
      Assert.False(typeof(string).IsSubclassOf(typeof(DbContext)));
   }
}