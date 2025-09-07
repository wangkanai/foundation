// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit;

/// <summary>Defines an entity that tracks the timezone-aware timestamp of its last update.</summary>
/// <remarks>
/// This interface is typically implemented by entities that require an updated audit field, allowing tracking of changes over time with timezone precision.
/// The <see cref="Updated"/> property stores the DateTimeOffset of the most recent modification.
/// </remarks>
public interface IUpdatedEntity
{
   /// <summary>Gets or sets the DateTimeOffset when the entity was last updated, providing timezone-aware auditing.</summary>
   /// <remarks>
   /// This property allows tracking when an entity was last modified with timezone awareness.
   /// It is useful for auditing purposes, providing visibility into the update history of the implementing entity across different timezones.
   /// </remarks>
   DateTimeOffset? Updated { get; set; }
}