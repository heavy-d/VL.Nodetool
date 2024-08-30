// For examples, see:
// https://thegraybook.vvvv.org/reference/extending/writing-nodes.html#examples
using System;
using System.Collections.Generic;
using System.Text.Json;
using MessagePack;

namespace Main {
  public static class Utils {


    public class JobDictionaryGenerator {
      public static Dictionary<string, object> CreateJobDictionary(
          string apiUrl,
          string userId,
          string workflowId,
          string authToken,
          string jobType,
          Dictionary<string, object> parameters) {
        var jobDictionary = new Dictionary<string, object> {
          ["command"] = "run_job",
          ["data"] = new Dictionary<string, object> {
            ["type"] = "run_job_request",
            ["api_url"] = apiUrl,
            ["user_id"] = userId,
            ["workflow_id"] = workflowId,
            ["auth_token"] = authToken,
            ["job_type"] = jobType,
            ["params"] = parameters ?? new Dictionary<string, object>(),
            ["graph"] = new Dictionary<string, object> {
              ["nodes"] = new List<object>(),
              ["edges"] = new List<object>()
            }
          }
        };

        // Debug output
        Console.WriteLine("Job Dictionary: " + JsonSerializer.Serialize(jobDictionary, new JsonSerializerOptions { WriteIndented = true }));

        return jobDictionary;
      }
    }

    public static class JobDictionaryToMessagePack {
      public static byte[] ConvertAndSerialize(
          string apiUrl,
          string userId,
          string workflowId,
          string authToken,
          string jobType,
          Dictionary<string, object> parameters) {
        try {
          var jobDictionary = JobDictionaryGenerator.CreateJobDictionary(
              apiUrl, userId, workflowId, authToken, jobType, parameters);

          // Debug output
          Console.WriteLine("Job Dictionary before serialization: " +
              JsonSerializer.Serialize(jobDictionary, new JsonSerializerOptions { WriteIndented = true }));

          // Serialize to MessagePack
          var options = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
          byte[] serialized = MessagePackSerializer.Serialize(jobDictionary, options);

          Console.WriteLine($"Serialized data length: {serialized.Length} bytes");

          return serialized;
        }
        catch (Exception ex) {
          Console.WriteLine($"Error in conversion and serialization: {ex.Message}");
          return null;
        }
      }
    }
  }
}