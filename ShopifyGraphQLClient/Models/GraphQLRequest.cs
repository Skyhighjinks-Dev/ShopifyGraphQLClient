using System.Text.Json.Serialization;

namespace ShopifyGraphQLClient.Models;

/// <summary>
/// Represents a JSON request to be converted to a GraphQL query
/// </summary>
public class GraphQLRequest
{
  /// <summary>
  /// Type of operation (query, mutation)
  /// </summary>
  [JsonPropertyName("operation")]
  public string Operation { get; set; } = "query";

  /// <summary>
  /// GraphQL resource to query (e.g., "products", "orders")
  /// </summary>
  [JsonPropertyName("resource")]
  public string Resource { get; set; } = string.Empty;

  /// <summary>
  /// Fields to return in the response
  /// </summary>
  [JsonPropertyName("fields")]
  public List<string> Fields { get; set; } = new();

  /// <summary>
  /// Parameters for the query
  /// </summary>
  [JsonPropertyName("parameters")]
  public Dictionary<string, object> Parameters { get; set; } = new();

  /// <summary>
  /// Filter conditions
  /// </summary>
  [JsonPropertyName("filters")]
  public Dictionary<string, object> Filters { get; set; } = new();

  /// <summary>
  /// Command-specific data for mutations
  /// </summary>
  [JsonPropertyName("data")]
  public Dictionary<string, object>? Data { get; set; }
}