// Copyright (c) 2014-2025 Sarin Na Wangkanai, All Rights Reserved.

using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Identity;

namespace Wangkanai.Audit;

/// <summary>
/// Extension methods for the <see cref="Trail{TKey, TUserType, TUserKey}"/> class,
/// providing utility methods for value retrieval, setting, and JSON operations.
/// </summary>
public static class TrailExtensions
{
   /// <summary>Sets the old and new values efficiently using pre-serialized JSON strings.</summary>
   /// <param name="trail">The trail instance to modify.</param>
   /// <param name="oldValuesJson">The JSON representation of old values.</param>
   /// <param name="newValuesJson">The JSON representation of new values.</param>
   /// <remarks>
   /// This method bypasses dictionary creation and serialization, providing optimal performance for bulk audit operations.
   /// </remarks>
   public static void SetValuesFromJson<TKey, TUserType, TUserKey>
      (this Trail<TKey, TUserType, TUserKey> trail, string? oldValuesJson, string? newValuesJson)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
   {
      trail.OldValuesJson = oldValuesJson;
      trail.NewValuesJson = newValuesJson;
   }

   /// <summary>Sets audit values from ReadOnlySpan to minimize memory allocations.</summary>
   /// <typeparam name="TKey">The type of the unique identifier for the audit trail.</typeparam>
   /// <typeparam name="TUserType">The type of the user associated with the audit action.</typeparam>
   /// <typeparam name="TUserKey">The type of the user's unique identifier.</typeparam>
   /// <param name="trail">The trail instance to modify.</param>
   /// <param name="columnNames">Span of column names that changed.</param>
   /// <param name="oldValues">Span of old values corresponding to the columns.</param>
   /// <param name="newValues">Span of new values corresponding to the columns.</param>
   /// <remarks>
   /// This method provides optimal performance for high-throughput audit scenarios by
   /// using spans and avoiding dictionary allocations for small change sets.
   /// This overload uses object type for values, suitable for mixed-type audit data.
   /// </remarks>
   public static void SetValuesFromSpan<TKey, TUserType, TUserKey>
      (this Trail<TKey, TUserType, TUserKey> trail, ReadOnlySpan<string> columnNames, ReadOnlySpan<object> oldValues, ReadOnlySpan<object> newValues)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
   {
      if (columnNames.Length != oldValues.Length || columnNames.Length != newValues.Length)
         throw new ArgumentException("All spans must have the same length.");

      // For small change sets (<=3 properties), use optimized direct JSON construction
      if (columnNames.Length <= 3)
      {
         var oldJson = BuildJsonFromSpan(columnNames, oldValues);
         var newJson = BuildJsonFromSpan(columnNames, newValues);
         trail.SetValuesFromJson(oldJson, newJson);

         // Update ChangedColumns efficiently
         trail.ChangedColumns.Clear();
         for (var i = 0; i < columnNames.Length; i++)
            trail.ChangedColumns.Add(columnNames[i]);
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

         trail.OldValues = oldDict;
         trail.NewValues = newDict;

         trail.ChangedColumns.Clear();
         for (var i = 0; i < columnNames.Length; i++)
            trail.ChangedColumns.Add(columnNames[i]);
      }
   }

   /// <summary>Gets a specific old value by column name without deserializing the entire dictionary.</summary>
   /// <param name="trail">The trail instance to query.</param>
   /// <param name="columnName">The name of the column.</param>
   /// <returns>The old value if found, null otherwise.</returns>
   /// <remarks>This method provides efficient access to individual values without full deserialization overhead.</remarks>
   public static object? GetOldValue<TKey, TUserType, TUserKey>(
      this Trail<TKey, TUserType, TUserKey> trail,
      string                                columnName)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
   {
      if (string.IsNullOrEmpty(trail.OldValuesJson))
         return null;

      try
      {
         using var document = JsonDocument.Parse(trail.OldValuesJson);
         return document.RootElement.TryGetProperty(columnName, out var element)
            ? GetJsonElementValue(element)
            : null;
      }
      catch (JsonException)
      {
         return null;
      }
   }

   /// <summary>Gets a specific new value by column name without deserializing the entire dictionary.</summary>
   /// <param name="trail">The trail instance to query.</param>
   /// <param name="columnName">The name of the column.</param>
   /// <returns>The new value if found, null otherwise.</returns>
   /// <remarks>This method provides efficient access to individual values without full deserialization overhead.</remarks>
   public static object? GetNewValue<TKey, TUserType, TUserKey>
      (this Trail<TKey, TUserType, TUserKey> trail, string columnName)
      where TKey : IEquatable<TKey>, IComparable<TKey>
      where TUserType : IdentityUser<TUserKey>
      where TUserKey : IEquatable<TUserKey>, IComparable<TUserKey>
   {
      if (string.IsNullOrEmpty(trail.NewValuesJson))
         return null;

      try
      {
         using var document = JsonDocument.Parse(trail.NewValuesJson);
         return document.RootElement.TryGetProperty(columnName, out var element)
            ? GetJsonElementValue(element)
            : null;
      }
      catch (JsonException)
      {
         return null;
      }
   }

   /// <summary>Deserializes JSON values and converts JsonElement values to their appropriate CLR types.</summary>
   /// <param name="json">The JSON string to deserialize.</param>
   /// <returns>A dictionary of deserialized values.</returns>
   /// <remarks>This method is used internally by the OldValues and NewValues properties for backward compatibility.</remarks>
   internal static Dictionary<string, object> DeserializeValues(string json)
   {
      try
      {
         var result = new Dictionary<string, object>();

         using var doc = JsonDocument.Parse(json);

         foreach (var property in doc.RootElement.EnumerateObject())
            result[property.Name] = ConvertJsonElement(property.Value);

         return result;
      }
      catch (JsonException)
      {
         return new Dictionary<string, object>();
      }
   }

   /// <summary>Converts a JsonElement to the appropriate CLR type.</summary>
   /// <param name="element">The JsonElement to convert.</param>
   /// <returns>The converted CLR type value.</returns>
   private static object ConvertJsonElement(JsonElement element)
      => element.ValueKind switch
         {
            JsonValueKind.String                                             => element.GetString()!,
            JsonValueKind.Number when element.TryGetInt32(out var intValue)  => intValue,
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number                                             => element.GetDouble(),
            JsonValueKind.True                                               => true,
            JsonValueKind.False                                              => false,
            JsonValueKind.Null                                               => null!,
            _                                                                => element.ToString()
         };

   /// <summary>Builds JSON directly from spans for optimal performance with small change sets.</summary>
   /// <typeparam name="T">The type of the values being serialized.</typeparam>
   /// <param name="columnNames">Span of column names.</param>
   /// <param name="values">Span of values corresponding to the columns.</param>
   /// <returns>A JSON string representation of the column names and values.</returns>
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

   /// <summary>Extracts a value from a JsonElement efficiently.</summary>
   /// <param name="element">The JsonElement to extract a value from.</param>
   /// <returns>The extracted value in appropriate CLR type.</returns>
   private static object? GetJsonElementValue(JsonElement element)
      => element.ValueKind switch
         {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longVal) ? longVal : element.GetDouble(),
            JsonValueKind.True   => true,
            JsonValueKind.False  => false,
            JsonValueKind.Null   => null,
            _                    => element.GetRawText()
         };
}