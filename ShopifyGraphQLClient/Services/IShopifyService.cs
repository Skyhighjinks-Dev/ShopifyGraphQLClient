using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Services;

/// <summary>
/// Interface for Shopify GraphQL API operations
/// </summary>
public interface IShopifyService
{
  /// <summary>
  /// Executes a GraphQL request against the Shopify API
  /// </summary>
  /// <param name="request">The request to execute</param>
  /// <returns>A response containing the results or error information</returns>
  Task<GraphQLResponse> ExecuteRequestAsync(GraphQLRequest request);

  /// <summary>
  /// Executes multiple GraphQL requests in bulk
  /// </summary>
  /// <param name="requests">Collection of requests to process</param>
  /// <returns>A bulk response containing results for each request</returns>
  Task<BulkResponse> ExecuteBulkRequestAsync(IEnumerable<GraphQLRequest> requests);
}