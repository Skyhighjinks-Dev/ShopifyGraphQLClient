using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Services;

/// <summary>
/// Service for interacting with the Shopify GraphQL API
/// </summary>
public class ShopifyService : IShopifyService
{
  private readonly HttpClient _httpClient;
  private readonly IGraphQLConverter _converter;
  private readonly ILogger<ShopifyService> _logger;
  private readonly ShopifySettings _settings;

  public ShopifyService(
      HttpClient httpClient,
      IGraphQLConverter converter,
      IOptions<ShopifySettings> options,
      ILogger<ShopifyService> logger)
  {
    _httpClient = httpClient;
    _converter = converter;
    _logger = logger;
    _settings = options.Value;

    // Configure HttpClient defaults
    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    _httpClient.DefaultRequestHeaders.Add("X-Shopify-Access-Token", _settings.AccessToken);

    // Set base address if not already set and if endpoint is relative
    if (_httpClient.BaseAddress == null && !Uri.IsWellFormedUriString(_settings.GraphQLEndpoint, UriKind.Absolute))
    {
      // Use StoreUrl as base if GraphQLEndpoint is relative
      _httpClient.BaseAddress = new Uri(_settings.StoreUrl);
      _logger.LogInformation("Setting base address to: {BaseAddress}", _httpClient.BaseAddress);
    }
  }

  /// <inheritdoc />
  public async Task<GraphQLResponse> ExecuteRequestAsync(GraphQLRequest request)
  {
    try
    {
      _logger.LogInformation("Executing request for resource: {Resource}", request.Resource);

      // Validate the request
      if (!_converter.ValidateRequest(request))
      {
        var errors = string.Join(", ", _converter.GetValidationErrors(request));
        _logger.LogWarning("Invalid request: {Errors}", errors);

        return new GraphQLResponse
        {
          Success = false,
          Error = $"Invalid request: {errors}"
        };
      }

      // Convert the request to GraphQL
      string graphQLQuery = _converter.ConvertToGraphQL(request);

      // Create the request payload
      var payload = new { query = graphQLQuery };
      var jsonContent = JsonSerializer.Serialize(payload);
      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      // Ensure we have an absolute URI for the endpoint
      Uri endpointUri;
      if (Uri.IsWellFormedUriString(_settings.GraphQLEndpoint, UriKind.Absolute))
      {
        endpointUri = new Uri(_settings.GraphQLEndpoint);
      }
      else if (_httpClient.BaseAddress != null)
      {
        endpointUri = new Uri(_httpClient.BaseAddress, _settings.GraphQLEndpoint);
      }
      else
      {
        return new GraphQLResponse
        {
          Success = false,
          Error = "Invalid GraphQLEndpoint configuration: endpoint must be absolute or BaseAddress must be set."
        };
      }

      // Execute the request
      var response = await _httpClient.PostAsync(endpointUri, content);
      var responseBody = await response.Content.ReadAsStringAsync();

      // Handle the response
      if (response.IsSuccessStatusCode)
      {
        // Parse the JSON response
        var jsonResponse = JsonDocument.Parse(responseBody);

        // Check for GraphQL errors
        if (jsonResponse.RootElement.TryGetProperty("errors", out var errors))
        {
          string errorMessage = errors.ToString();
          _logger.LogWarning("GraphQL error: {Error}", errorMessage);

          return new GraphQLResponse
          {
            Success = false,
            Error = errorMessage,
            Query = graphQLQuery
          };
        }

        // Extract the data
        object? data = null;
        if (jsonResponse.RootElement.TryGetProperty("data", out var dataElement))
        {
          data = JsonSerializer.Deserialize<object>(dataElement.ToString());
        }

        return new GraphQLResponse
        {
          Success = true,
          Data = data,
          Query = graphQLQuery
        };
      }
      else
      {
        _logger.LogWarning("HTTP error: {StatusCode} - {ResponseBody}",
            response.StatusCode, responseBody);

        return new GraphQLResponse
        {
          Success = false,
          Error = $"HTTP error: {response.StatusCode} - {responseBody}",
          Query = graphQLQuery
        };
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error executing GraphQL request");

      return new GraphQLResponse
      {
        Success = false,
        Error = $"Error: {ex.Message}"
      };
    }
  }

  /// <inheritdoc />
  public async Task<BulkResponse> ExecuteBulkRequestAsync(IEnumerable<GraphQLRequest> requests)
  {
    _logger.LogInformation("Executing bulk request with {Count} items", requests.Count());

    var response = new BulkResponse();
    var requestList = requests.ToList();

    for (int i = 0; i < requestList.Count; i++)
    {
      var request = requestList[i];
      var result = await ExecuteRequestAsync(request);

      response.Responses.Add(new BulkResponseItem
      {
        Index = i,
        Response = result
      });

      // Update counters
      response.TotalProcessed++;
      if (result.Success)
        response.SuccessCount++;
      else
        response.FailCount++;
    }

    _logger.LogInformation("Bulk request completed: {Success} succeeded, {Failed} failed",
        response.SuccessCount, response.FailCount);

    return response;
  }


  public class ShopifySettings
  {
    public string StoreUrl { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string GraphQLEndpoint { get; set; } = string.Empty;
  }
}