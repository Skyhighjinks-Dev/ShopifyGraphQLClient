using System.Text.Json.Serialization;

namespace ShopifyGraphQLClient.Models;

/// <summary>
/// Represents the response from a GraphQL operation
/// </summary>
public class GraphQLResponse
{
  /// <summary>
  /// Indicates if the request was successful
  /// </summary>
  [JsonPropertyName("success")]
  public bool Success { get; set; }

  /// <summary>
  /// Response data from Shopify (null if request failed)
  /// </summary>
  [JsonPropertyName("data")]
  public object? Data { get; set; }

  /// <summary>
  /// Error message if request failed
  /// </summary>
  [JsonPropertyName("error")]
  public string? Error { get; set; }

  /// <summary>
  /// Original GraphQL query that was executed (for debugging)
  /// </summary>
  [JsonPropertyName("query")]
  public string? Query { get; set; }
}