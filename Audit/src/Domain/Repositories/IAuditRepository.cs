// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Repositories;

/// <summary>Repository interface for audit trail operations.</summary>
/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
public interface IAuditRepository<TKey, TUserType, TUserKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
   where TUserType : IdentityUser<TUserKey>
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   /// <summary>Gets audit trails for a specific entity.</summary>
   /// <param name="entityName">The name of the entity.</param>
   /// <param name="primaryKey">The primary key of the entity.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>A collection of audit trails for the specified entity.</returns>
   Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByEntityAsync(string entityName, string primaryKey, CancellationToken cancellationToken = default);

   /// <summary>Gets audit trails for a specific user.</summary>
   /// <param name="userId">The user identifier.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>A collection of audit trails for the specified user.</returns>
   Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByUserAsync(TUserKey userId, CancellationToken cancellationToken = default);

   /// <summary>Gets audit trails within a specific date range.</summary>
   /// <param name="startDate">The start date for the range.</param>
   /// <param name="endDate">The end date for the range.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>A collection of audit trails within the specified date range.</returns>
   Task<IEnumerable<Trail<TKey, TUserType, TUserKey>>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

   /// <summary>Adds a new audit trail.</summary>
   /// <param name="trail">The audit trail to add.</param>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>The added audit trail.</returns>
   Task<Trail<TKey, TUserType, TUserKey>> AddAsync(Trail<TKey, TUserType, TUserKey> trail, CancellationToken cancellationToken = default);

   /// <summary>Saves all pending changes to the data store.</summary>
   /// <param name="cancellationToken">The cancellation token.</param>
   /// <returns>The number of state entries written to the database.</returns>
   Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}