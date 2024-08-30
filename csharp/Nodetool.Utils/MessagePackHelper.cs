using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using MessagePack;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections;
using System.Reflection;

public static class MessagePackHelper {
  private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard.WithResolver(
      CompositeResolver.Create(
          NativeGuidResolver.Instance,
          StandardResolver.Instance
      )
  );

  public static byte[] SerializeJsonToMessagePack(string jsonString) {
    try {
      // Parse JSON to JObject
      var jobject = JObject.Parse(jsonString);

      // Convert JObject to Dictionary<object, object>
      var dict = ConvertJTokenToDictionary(jobject);

      // Transform empty objects to empty lists for specific fields
      var transformedDict = TransformDictionary(dict);

      // Serialize to MessagePack
      return MessagePackSerializer.Serialize(transformedDict, Options);
    }
    catch (Exception ex) {
      Console.WriteLine($"Error serializing JSON to MessagePack: {ex.Message}");
      return null;
    }
  }

  public static Dictionary<string, object> DeserializeMessagePackToDictionary(ArraySegment<byte> messagePackData) {
    try {
      ReadOnlyMemory<byte> readOnlyMemory = new ReadOnlyMemory<byte>(messagePackData.Array, messagePackData.Offset, messagePackData.Count);
      var deserialized = MessagePackSerializer.Deserialize<Dictionary<object, object>>(readOnlyMemory, Options);
      return ConvertToDictionaryStringKey(deserialized);
    }
    catch (Exception ex) {
      Console.WriteLine($"Error deserializing MessagePack to Dictionary: {ex.Message}");
      return null;
    }
  }

  private static object ConvertJTokenToDictionary(JToken token) {
    if (token is JObject jObject) {
      var dict = new Dictionary<object, object>();
      foreach (var property in jObject.Properties()) {
        dict[property.Name] = ConvertJTokenToDictionary(property.Value);
      }
      return dict;
    }
    else if (token is JArray jArray) {
      return jArray.Select(ConvertJTokenToDictionary).ToList();
    }
    else {
      return token.ToObject<object>();
    }
  }

  private static Dictionary<string, object> ConvertToDictionaryStringKey(Dictionary<object, object> dict) {
    var result = new Dictionary<string, object>();
    foreach (var kvp in dict) {
      var key = kvp.Key.ToString();
      if (kvp.Value is Dictionary<object, object> nestedDict) {
        result[key] = ConvertToDictionaryStringKey(nestedDict);
      }
      else if (kvp.Value is List<object> list) {
        result[key] = list.Select(item => item is Dictionary<object, object> d
            ? ConvertToDictionaryStringKey(d)
            : item).ToList();
      }
      else {
        result[key] = kvp.Value;
      }
    }
    return result;
  }

  private static object TransformDictionary(object obj) {
    if (obj is Dictionary<object, object> dict) {
      var result = new Dictionary<object, object>();
      foreach (var kvp in dict) {
        var key = kvp.Key.ToString();
        var value = TransformDictionary(kvp.Value);

        if (key == "graph" && value is Dictionary<object, object> graphDict) {
          // Transform nodes and edges to empty lists if they're empty objects
          if (graphDict.TryGetValue("nodes", out var nodes) && nodes is Dictionary<object, object> emptyNodesDict && !emptyNodesDict.Any()) {
            graphDict["nodes"] = new List<object>();
          }
          if (graphDict.TryGetValue("edges", out var edges) && edges is Dictionary<object, object> emptyEdgesDict && !emptyEdgesDict.Any()) {
            graphDict["edges"] = new List<object>();
          }
        }

        result[key] = value;
      }
      return result;
    }
    else if (obj is List<object> list) {
      return list.Select(TransformDictionary).ToList();
    }
    else {
      return obj;
    }
  }

  // JSON
  public static string DeserializeMessagePackToJson(ArraySegment<byte> messagePackData) {
    try {
      // First, deserialize MessagePack to Dictionary
      var dictionary = DeserializeMessagePackToDictionary(messagePackData);

      if (dictionary == null) {
        throw new InvalidOperationException("Deserialization to dictionary failed.");
      }

      // Then, serialize the Dictionary to JSON
      return JsonConvert.SerializeObject(dictionary, Newtonsoft.Json.Formatting.Indented);
    }
    catch (Exception ex) {
      Console.WriteLine($"Error deserializing MessagePack to JSON: {ex.Message}");
      return null;
    }
  }

  // XML
  public static string ConvertJsonToXml(string json) {
    try {
      XNode node = JsonConvert.DeserializeXNode(json, "root");
      return node.ToString();
    }
    catch (Exception ex) {
      Console.WriteLine($"Error converting JSON to XML: {ex.Message}");
      Console.WriteLine($"Stack Trace: {ex.StackTrace}");
      return null;
    }
  }

  public static string DeserializeMessagePackToXml(ArraySegment<byte> messagePackData) {
    string json = DeserializeMessagePackToJson(messagePackData);
    if (json == null) {
      return null;
    }
    return ConvertJsonToXml(json);
  }

  // Inspect

  public static void InspectObject(object obj) {
    if (obj == null) {
      Console.WriteLine("Object is null");
      return;
    }

    Type type = obj.GetType();
    Console.WriteLine($"Object Type: {type.FullName}");

    if (obj is IDictionary dictionary) {
      Console.WriteLine("Object is a dictionary. Contents:");
      foreach (DictionaryEntry entry in dictionary) {
        Console.WriteLine($"  Key ({entry.Key.GetType().Name}): {entry.Key}");
        Console.WriteLine($"  Value ({entry.Value.GetType().Name}): {entry.Value}");
      }
    }
    else if (obj is IEnumerable enumerable) {
      Console.WriteLine("Object is enumerable. Contents:");
      foreach (var item in enumerable) {
        Console.WriteLine($"  Item ({item.GetType().Name}): {item}");
      }
    }
    else {
      Console.WriteLine("Properties:");
      foreach (PropertyInfo prop in type.GetProperties()) {
        try {
          var value = prop.GetValue(obj);
          Console.WriteLine($"  {prop.Name} ({prop.PropertyType.Name}): {value}");
        }
        catch (Exception ex) {
          Console.WriteLine($"  {prop.Name}: Error accessing value - {ex.Message}");
        }
      }
    }

    Console.WriteLine("Methods:");
    foreach (MethodInfo method in type.GetMethods()) {
      Console.WriteLine($"  {method.Name}");
    }
  }

  public static void InspectNestedDictionary(Dictionary<object, object> outerDict) {
    if (outerDict.TryGetValue("image_output_2024-08-01", out object innerDictObj)) {
      if (innerDictObj is Dictionary<object, object> innerDict) {
        Console.WriteLine("Contents of inner dictionary:");
        foreach (var kvp in innerDict) {
          Console.WriteLine($"  Key ({kvp.Key.GetType().Name}): {kvp.Key}");
          Console.WriteLine($"  Value ({kvp.Value.GetType().Name}): {kvp.Value}");

          // If the value is complex, you might want to inspect it further
          if (kvp.Value is Dictionary<object, object> || kvp.Value is IEnumerable<object>) {
            Console.WriteLine("  Complex value detected. You may want to inspect this further.");
          }

          Console.WriteLine(); // Empty line for readability
        }
      }
      else {
        Console.WriteLine($"Inner object is not a Dictionary<object, object>. Actual type: {innerDictObj.GetType().FullName}");
      }
    }
    else {
      Console.WriteLine("Key 'image_output_2024-08-01' not found in the outer dictionary.");
    }
  }
  public static void SaveBase64ToFile(string base64String, string outputPath) {
    try {
      // Convert base64 to byte array
      byte[] fileData = Convert.FromBase64String(base64String);

      // Save the byte array as a file
      File.WriteAllBytes(outputPath, fileData);

      Console.WriteLine($"File successfully saved to: {outputPath}");
    }
    catch (FormatException) {
      Console.WriteLine("Error: The input is not a valid base64 string.");
    }
    catch (IOException ex) {
      Console.WriteLine($"Error saving file: {ex.Message}");
    }
    catch (Exception ex) {
      Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    }
  }

  public static byte[] Base64ToByteArray(string base64String) {
    try {
      return Convert.FromBase64String(base64String);
    }
    catch (FormatException) {
      Console.WriteLine("Error: The input is not a valid base64 string.");
      return null;
    }
    catch (Exception ex) {
      Console.WriteLine($"An unexpected error occurred: {ex.Message}");
      return null;
    }
  }
}