using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MentorBot
{
  public static class UrlEncoder
  {
    public static string Encode<TValue>(TValue o, JsonSerializerOptions? serializerOptions = null)
    {
      // Serialize the value to a JSON node
      var node = JsonSerializer.SerializeToNode(o, serializerOptions);
      // Select all key-value pairs in the JSON object and filter out any null values
      var fields = node?.AsObject()
        .Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value?.ToString()))
        .Where(kvp => kvp.Value != null);
      var query = QueryHelpers.AddQueryString("", fields!).Remove(0, 1);
      return query;
    }

    public static TValue? Decode<TValue>(string query, JsonSerializerOptions? serializerOptions = null)
    {
      JsonObject obj = new();
      foreach(var field in QueryHelpers.ParseQuery(query))
      {
        obj.Add(field.Key, field.Value.ToString());
      }
      // Deserialize the JSON object into a value of type TValue
      return JsonSerializer.Deserialize<TValue>(obj, serializerOptions);
    }
  }
}
