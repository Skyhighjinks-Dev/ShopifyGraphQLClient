using System.Text.Json.Serialization;

namespace ShopifyGraphQLClient.Models;

/// <summary>
/// Represents a bulk request containing multiple GraphQL requests
/// </summary>
public class BulkRequest
{
  /// <summary>
  /// Collection of GraphQL requests to process in bulk
  /// </summary>
  [JsonPropertyName("requests")]
  public List<GraphQLRequest> Requests { get; set; } = new();
}

/// <summary>
/// Represents the response from a bulk operation
/// </summary>
public class BulkResponse
{
  /// <summary>
  /// Collection of individual responses for each request
  /// </summary>
  [JsonPropertyName("responses")]
  public List<BulkResponseItem> Responses { get; set; } = new();

  /// <summary>
  /// Total number of requests processed
  /// </summary>
  [JsonPropertyName("totalProcessed")]
  public int TotalProcessed { get; set; }

  /// <summary>
  /// Number of successful requests
  /// </summary>
  [JsonPropertyName("successCount")]
  public int SuccessCount { get; set; }

  /// <summary>
  /// Number of failed requests
  /// </summary>
  [JsonPropertyName("failCount")]
  public int FailCount { get; set; }
}

/// <summary>
/// Represents a single response item within a bulk operation
/// </summary>
public class BulkResponseItem
{
  /// <summary>
  /// Index of the request in the original bulk request
  /// </summary>
  [JsonPropertyName("index")]
  public int Index { get; set; }

  /// <summary>
  /// Response data for this request
  /// </summary>
  [JsonPropertyName("response")]
  public GraphQLResponse Response { get; set; } = new();
}