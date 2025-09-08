// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

namespace Wangkanai.Audit.Domain;

/// <summary>Represents an entity that tracks the creation date and time with timezone awareness.</summary>
/// <remarks>
/// The <see cref="ICreatedEntity"/> interface defines a contract for entities that require
/// the ability to capture and store the timezone-aware timestamp of their creation.
/// </remarks>
public interface ICreatedEntity
{
   /// <summary>Gets or sets the DateTimeOffset when the entity was created, providing timezone-aware auditing.</summary>
   /// <remarks>
   /// The <see cref="Created"/> property is used to track the creation timestamp of the entity.
   /// It is a nullable <see cref="DateTimeOffset"/> that can be set upon initialization or updated as needed.
   /// It is commonly used in scenarios that require audit or temporal tracking with timezone precision.
   /// </remarks>
   DateTimeOffset? Created { get; set; }
}