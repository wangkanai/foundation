// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using Microsoft.EntityFrameworkCore;

namespace Wangkanai.EntityFramework;

/// <summary>
/// Represents an abstract database context that supports auditing capabilities.
/// This class extends the standard DbContext and implements the <see cref="IAuditDbContext"/>  interface,
/// providing the necessary functionality to integrate auditing mechanisms in entity framework operations.
/// </summary>
public abstract class AuditDbContext : DbContext, IAuditDbContext
{
   /// <summary>
   /// Gets the current database context instance.
   /// Provides access to the underlying <see cref="DbContext"/> for performing database operations and
   /// supporting audit-related functionalities in implementations of <see cref="IAuditDbContext"/>.
   /// </summary>
   public DbContext DbContext => this;
}