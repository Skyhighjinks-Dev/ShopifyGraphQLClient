using System.Text;
using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Services;

/// <summary>
/// Service that converts JSON requests to GraphQL queries
/// </summary>
public class GraphQLConverter : IGraphQLConverter
{
  private readonly ILogger<GraphQLConverter> _logger;

  public GraphQLConverter(ILogger<GraphQLConverter> logger)
  {
    _logger = logger;
  }

  /// <inheritdoc />
  public string ConvertToGraphQL(GraphQLRequest request)
  {
    _logger.LogInformation("Converting request for resource: {Resource}", request.Resource);

    if (!ValidateRequest(request))
    {
      var errors = string.Join(", ", GetValidationErrors(request));
      _logger.LogWarning("Invalid request: {Errors}", errors);
      throw new ArgumentException($"Invalid request: {errors}");
    }

    var queryBuilder = new StringBuilder();

    // Determine if this is a query or mutation
    queryBuilder.AppendLine($"{request.Operation} {{");

    // For mutations, we need to build the input differently
    if (request.Operation.Equals("mutation", StringComparison.OrdinalIgnoreCase))
    {
      BuildMutation(queryBuilder, request);
    }
    else
    {
      BuildQuery(queryBuilder, request);
    }

    queryBuilder.AppendLine("}");

    var result = queryBuilder.ToString();
    _logger.LogDebug("Generated GraphQL: {Query}", result);

    return result;
  }

  /// <inheritdoc />
  public bool ValidateRequest(GraphQLRequest request)
  {
    return GetValidationErrors(request).Count == 0;
  }

  /// <inheritdoc />
  public List<string> GetValidationErrors(GraphQLRequest request)
  {
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(request.Resource))
    {
      errors.Add("Resource cannot be empty");
    }

    if (request.Fields == null || !request.Fields.Any())
    {
      errors.Add("At least one field must be specified");
    }

    // Validate operation type
    if (!request.Operation.Equals("query", StringComparison.OrdinalIgnoreCase) &&
        !request.Operation.Equals("mutation", StringComparison.OrdinalIgnoreCase))
    {
      errors.Add("Operation must be either 'query' or 'mutation'");
    }

    // For mutations, data is required
    if (request.Operation.Equals("mutation", StringComparison.OrdinalIgnoreCase) &&
        (request.Data == null || !request.Data.Any()))
    {
      errors.Add("Data is required for mutations");
    }

    return errors;
  }

  /// <summary>
  /// Builds a GraphQL query
  /// </summary>
  private void BuildQuery(StringBuilder queryBuilder, GraphQLRequest request)
  {
    // Start with the resource
    queryBuilder.Append($"  {request.Resource}");

    // Add parameters if any
    if (request.Parameters.Any() || request.Filters.Any())
    {
      queryBuilder.Append("(");

      var allParams = new List<string>();

      // Add regular parameters
      foreach (var param in request.Parameters)
      {
        allParams.Add($"{param.Key}: {FormatParameterValue(param.Value)}");
      }

      // Add filter parameters if any
      if (request.Filters.Any())
      {
        var filters = new List<string>();
        foreach (var filter in request.Filters)
        {
          filters.Add($"{filter.Key}: {FormatParameterValue(filter.Value)}");
        }

        allParams.Add($"query: {{ {string.Join(", ", filters)} }}");
      }

      queryBuilder.Append(string.Join(", ", allParams));
      queryBuilder.Append(")");
    }

    // Add fields
    queryBuilder.AppendLine(" {");
    foreach (var field in request.Fields)
    {
      queryBuilder.AppendLine($"    {field}");
    }
    queryBuilder.AppendLine("  }");
  }

  /// <summary>
  /// Builds a GraphQL mutation
  /// </summary>
  private void BuildMutation(StringBuilder queryBuilder, GraphQLRequest request)
  {
    // Format the mutation name based on resource
    // For example, "product" becomes "productCreate" or "productUpdate"
    string mutationName = DetermineMutationName(request);

    queryBuilder.Append($"  {mutationName}(");

    // Add input parameter with data
    queryBuilder.Append("input: {");

    var inputParams = new List<string>();
    foreach (var data in request.Data!)
    {
      inputParams.Add($"{data.Key}: {FormatParameterValue(data.Value)}");
    }

    queryBuilder.Append(string.Join(", ", inputParams));
    queryBuilder.AppendLine("}) {");

    // Add response fields
    foreach (var field in request.Fields)
    {
      queryBuilder.AppendLine($"    {field}");
    }

    queryBuilder.AppendLine("  }");
  }

  /// <summary>
  /// Determines the appropriate mutation name based on the request
  /// </summary>
  private string DetermineMutationName(GraphQLRequest request)
  {
    // Look for an operation hint in the parameters
    if (request.Parameters.TryGetValue("operation", out var operation))
    {
      var op = operation.ToString()?.ToLowerInvariant();

      // Common operations: create, update, delete
      if (op == "create")
        return $"{request.Resource}Create";
      if (op == "update")
        return $"{request.Resource}Update";
      if (op == "delete")
        return $"{request.Resource}Delete";

      // If a specific operation was provided, use it
      return op!;
    }

    // Default to create if no operation specified
    return $"{request.Resource}Create";
  }

  /// <summary>
  /// Formats a parameter value for GraphQL
  /// </summary>
  private string FormatParameterValue(object value)
  {
    if (value == null)
      return "null";

    // Handle different types
    if (value is string strValue)
      return $"\"{strValue.Replace("\"", "\\\"")}\"";

    if (value is bool boolValue)
      return boolValue.ToString().ToLowerInvariant();

    if (value is Dictionary<string, object> dict)
    {
      var items = new List<string>();
      foreach (var item in dict)
      {
        items.Add($"{item.Key}: {FormatParameterValue(item.Value)}");
      }
      return $"{{ {string.Join(", ", items)} }}";
    }

    if (value is IEnumerable<object> list)
    {
      var items = new List<string>();
      foreach (var item in list)
      {
        items.Add(FormatParameterValue(item));
      }
      return $"[{string.Join(", ", items)}]";
    }

    // Default to string representation
    return value.ToString()!;
  }
}