// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wangkanai.Audit;

/// <summary>Represents an auditable entity with properties for tracking creation and modification timestamps.</summary>
/// <typeparam name="T">
/// The type of the identifier for the entity. Must implement <see cref="IComparable{T}"/> and <see cref="IEquatable{T}"/>.
/// </typeparam>
public  class AuditableEntity<T> : Entity<T>, IAuditableEntity
   where T : IComparable<T>, IEquatable<T>
{
   public DateTime? Created { get; set; }

   public DateTime? Updated { get; set; }

   public DateTime? Deleted { get; set; }
}

/// <summary>Represents an audit trail record for tracking entity changes in the system.</summary>
/// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
/// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
/// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
public class AuditableEntity<TKey, TUserType, TUserKey> : Entity<TKey>
   where TKey : IEquatable<TKey>, IComparable<TKey>
   where TUserType : IdentityUser<TUserKey>
   where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
{
   /// <summary>Gets or sets the type of trail associated with an audit action.</summary>
   /// <remarks>
   /// The <see cref="TrailType"/> property indicates the nature of the change that occurred in an entity.
   /// It reflects whether the entity was created, updated, or deleted, or if no changes were made.
   /// </remarks>
   public AuditTrailType TrailType { get; set; }

   /// <summary>Gets or sets the unique identifier of the user associated with the audit action.</summary>
   /// <remarks>
   /// The <see cref="UserId"/> property records the primary key of the user who performed the action being tracked.
   /// This property may be null if the action was performed in a context where a user is not available or applicable.
   /// </remarks>
   public TUserKey? UserId { get; set; }

   /// <summary>Gets or sets the user associated with the audit action.</summary>
   /// <remarks>
   /// The User property represents the user who performed the action being tracked in the audit trail.
   /// It is of a generic type derived from <see cref="IdentityUser{TKey}"/> to allow flexibility in user representation.
   /// </remarks>
   public TUserType? User { get; set; }

   /// <summary>Gets or sets the timestamp for the audit trail entry.</summary>
   /// <remarks>
   /// The <see cref="Timestamp"/> property represents the date and time when the audit action occurred.
   /// It is used for tracking the moment an entity change was recorded in the system.
   /// </remarks>
   public DateTime Timestamp { get; set; }

   /// <summary>Gets or sets the primary key value of the audited entity.</summary>
   /// <remarks>
   /// The <see cref="PrimaryKey"/> property holds the unique identifier of the entity being audited.
   /// This value helps in identifying the specific entity record subject to the audit trail entry.
   /// </remarks>
   public string? PrimaryKey { get; set; }

   /// <summary>Gets or sets the name of the entity associated with the audit trail.</summary>
   /// <remarks>
   /// The <see cref="EntityName"/> property identifies the entity affected by the audit action.
   /// This property is typically used to associate the audit record with a specific entity type in the system,
   /// such as a database table or domain object.
   /// </remarks>
   public string EntityName { get; set; }

   /// <summary>Gets or sets the list of column names that were changed during an audit action.</summary>
   /// <remarks>
   /// The <see cref="ChangedColumns"/> property contains the names of the specific columns in an entity that were modified as part of the audit record.
   /// This can be used to identify and track which fields have been updated in a system, providing valuable context for changes made.
   /// </remarks>
   public List<string> ChangedColumns { get; set; } = [];

   /// <summary>Gets or sets the serialized JSON representation of old values for changed entity properties.</summary>
   /// <remarks>
   /// The <see cref="OldValuesJson"/> property contains a compact JSON string of the old values, optimized for storage and reducing boxing overhead.
   /// This approach significantly reduces memory allocation and improves serialization performance.
   /// </remarks>
   public string? OldValuesJson { get; set; }

   /// <summary>Gets or sets the serialized JSON representation of new values for changed entity properties.</summary>
   /// <remarks>
   /// The <see cref="NewValuesJson"/> property contains a compact JSON string of the new values, optimized for storage and reducing boxing overhead.
   /// This approach significantly reduces memory allocation and improves serialization performance.
   /// </remarks>
   public string? NewValuesJson { get; set; }

   /// <summary>Gets the old values as a dictionary, deserialized from JSON on demand.</summary>
   /// <remarks>
   /// This property provides backward compatibility by deserializing the JSON representation into a dictionary when accessed.
   /// Use sparingly in performance-critical code paths.
   /// </remarks>
   [JsonIgnore]
   public Dictionary<string, object> OldValues
   {
      get => string.IsNullOrEmpty(OldValuesJson)
         ? new()
         : JsonSerializer.Deserialize<Dictionary<string, object>>(OldValuesJson) ?? new Dictionary<string, object>();
      set => OldValuesJson = value.Count == 0 ? null : JsonSerializer.Serialize(value);
   }

   /// <summary>Gets the new values as a dictionary, deserialized from JSON on demand.</summary>
   /// <remarks>This property provides backward compatibility by deserializing the JSON representation into a dictionary when accessed. Use sparingly in performance-critical code paths.</remarks>
   [JsonIgnore]
   public Dictionary<string, object> NewValues
   {
      get => string.IsNullOrEmpty(NewValuesJson)
         ? new()
         : JsonSerializer.Deserialize<Dictionary<string, object>>(NewValuesJson) ?? new Dictionary<string, object>();
      set => NewValuesJson = value.Count == 0 ? null : JsonSerializer.Serialize(value);
   }

   /// <summary>Sets the old and new values efficiently using pre-serialized JSON strings.</summary>
   /// <param name="oldValuesJson">The JSON representation of old values.</param>
   /// <param name="newValuesJson">The JSON representation of new values.</param>
   /// <remarks>
   /// This method bypasses dictionary creation and serialization, providing optimal performance for bulk audit operations.
   /// </remarks>
   public void SetValuesFromJson(string? oldValuesJson, string? newValuesJson)
   {
      OldValuesJson = oldValuesJson;
      NewValuesJson = newValuesJson;
   }

   /// <summary>Sets audit values from ReadOnlySpan to minimize memory allocations.</summary>
   /// <typeparam name="T">The type of the values being audited.</typeparam>
   /// <param name="columnNames">Span of column names that changed.</param>
   /// <param name="oldValues">Span of old values corresponding to the columns.</param>
   /// <param name="newValues">Span of new values corresponding to the columns.</param>
   /// <remarks>
   /// This method provides optimal performance for high-throughput audit scenarios by
   /// using spans and avoiding dictionary allocations for small change sets.
   /// </remarks>
   public void SetValuesFromSpan<T>(ReadOnlySpan<string> columnNames, ReadOnlySpan<T> oldValues, ReadOnlySpan<T> newValues)
   {
      if (columnNames.Length != oldValues.Length || columnNames.Length != newValues.Length)
         throw new ArgumentException("All spans must have the same length.");

      // For small change sets (<=3 properties), use optimized direct JSON construction
      if (columnNames.Length <= 3)
      {
         var oldJson = BuildJsonFromSpan(columnNames, oldValues);
         var newJson = BuildJsonFromSpan(columnNames, newValues);
         SetValuesFromJson(oldJson, newJson);

         // Update ChangedColumns efficiently
         ChangedColumns.Clear();
         for (var i = 0; i < columnNames.Length; i++)
            ChangedColumns.Add(columnNames[i]);
      }
      else
      {
         // For larger change sets, fall back to dictionary approach
         var oldDict = new Dictionary<string, object>(columnNames.Length);
         var newDict = new Dictionary<string, object>(columnNames.Length);

         for (var i = 0; i < columnNames.Length; i++)
         {
            oldDict[columnNames[i]] = oldValues[i]!;
            newDict[columnNames[i]] = newValues[i]!;
         }

         OldValues = oldDict;
         NewValues = newDict;

         ChangedColumns.Clear();
         for (var i = 0; i < columnNames.Length; i++)
            ChangedColumns.Add(columnNames[i]);
      }
   }

   /// <summary>Builds JSON directly from spans for optimal performance with small change sets.</summary>
   private static string BuildJsonFromSpan<T>(ReadOnlySpan<string> columnNames, ReadOnlySpan<T> values)
   {
      if (columnNames.Length == 0)
         return "{}";

      var json = new StringBuilder(128);
      json.Append('{');

      for (var i = 0; i < columnNames.Length; i++)
      {
         if (i > 0)
            json.Append(',');
         json.Append('"').Append(columnNames[i]).Append("\":");

         var value = values[i];
         if (value is string str)
            json.Append('"').Append(str.Replace("\"", "\\\"")).Append('"');
         else if (value is null)
            json.Append("null");
         else if (value is bool b)
            json.Append(b ? "true" : "false");
         else if (value is DateTime dt)
            json.Append('"').Append(dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")).Append('"');
         else
            json.Append('"').Append(value).Append('"');
      }

      json.Append('}');
      return json.ToString();
   }

   /// <summary>Gets a specific old value by column name without deserializing the entire dictionary.</summary>
   /// <param name="columnName">The name of the column.</param>
   /// <returns>The old value if found, null otherwise.</returns>
   /// <remarks>This method provides efficient access to individual values without full deserialization overhead.</remarks>
   public object? GetOldValue(string columnName)
   {
      if (string.IsNullOrEmpty(OldValuesJson))
         return null;

      // For simple lookups, use JSON parsing instead of full deserialization
      using var document = JsonDocument.Parse(OldValuesJson);
      return document.RootElement.TryGetProperty(columnName, out var element)
         ? GetJsonElementValue(element)
         : null;
   }

   /// <summary>Gets a specific new value by column name without deserializing the entire dictionary.</summary>
   /// <param name="columnName">The name of the column.</param>
   /// <returns>The new value if found, null otherwise.</returns>
   /// <remarks>This method provides efficient access to individual values without full deserialization overhead.</remarks>
   public object? GetNewValue(string columnName)
   {
      if (string.IsNullOrEmpty(NewValuesJson))
         return null;

      // For simple lookups, use JSON parsing instead of full deserialization
      using var document = JsonDocument.Parse(NewValuesJson);
      return document.RootElement.TryGetProperty(columnName, out var element)
         ? GetJsonElementValue(element)
         : null;
   }

   /// <summary>Extracts a value from a JsonElement efficiently.</summary>
   private static object? GetJsonElementValue(JsonElement element) =>
      element.ValueKind switch
      {
         JsonValueKind.String => element.GetString(),
         JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
         JsonValueKind.True   => true,
         JsonValueKind.False  => false,
         JsonValueKind.Null   => null,
         _                    => element.GetRawText()
      };
}