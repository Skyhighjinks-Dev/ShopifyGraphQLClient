using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Services;

/// <summary>
/// Interface for converting JSON requests to GraphQL queries
/// </summary>
public interface IGraphQLConverter
{
  /// <summary>
  /// Converts a GraphQLRequest object to a GraphQL query string
  /// </summary>
  /// <param name="request">The request to convert</param>
  /// <returns>A string containing the GraphQL query</returns>
  string ConvertToGraphQL(GraphQLRequest request);

  /// <summary>
  /// Validates a GraphQLRequest object
  /// </summary>
  /// <param name="request">The request to validate</param>
  /// <returns>True if valid, false otherwise</returns>
  bool ValidateRequest(GraphQLRequest request);

  /// <summary>
  /// Gets any validation errors for a request
  /// </summary>
  /// <param name="request">The request to validate</param>
  /// <returns>A list of validation error messages</returns>
  List<string> GetValidationErrors(GraphQLRequest request);
}