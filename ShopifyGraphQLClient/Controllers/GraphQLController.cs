using Microsoft.AspNetCore.Mvc;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;

namespace ShopifyGraphQLClient.Controllers;

/// <summary>
/// Controller for handling individual GraphQL requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GraphQLController : ControllerBase
{
  private readonly IShopifyService _shopifyService;
  private readonly ILogger<GraphQLController> _logger;

  public GraphQLController(
      IShopifyService shopifyService,
      ILogger<GraphQLController> logger)
  {
    _shopifyService = shopifyService;
    _logger = logger;
  }

  /// <summary>
  /// Executes a GraphQL request
  /// </summary>
  /// <param name="request">The request to execute</param>
  /// <returns>A response containing the results or error information</returns>
  [HttpPost]
  [ProducesResponseType(typeof(GraphQLResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(GraphQLResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(GraphQLResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> ExecuteRequest([FromBody] GraphQLRequest request)
  {
    _logger.LogInformation("Received request for resource: {Resource}", request.Resource);

    try
    {
      var response = await _shopifyService.ExecuteRequestAsync(request);

      if (response.Success)
      {
        return Ok(response);
      }
      else
      {
        // Still return 200 for valid API calls with GraphQL errors
        // The success flag will indicate the failure
        return Ok(response);
      }
    }
    catch (ArgumentException ex)
    {
      _logger.LogWarning("Bad request: {Message}", ex.Message);

      return BadRequest(new GraphQLResponse
      {
        Success = false,
        Error = ex.Message
      });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing request");

      return StatusCode(StatusCodes.Status500InternalServerError, new GraphQLResponse
      {
        Success = false,
        Error = "An internal server error occurred"
      });
    }
  }

  /// <summary>
  /// Gets documentation about the available resources and operations
  /// </summary>
  [HttpGet("docs")]
  [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
  public IActionResult GetDocumentation()
  {
    var docs = new
    {
      Resources = new[]
        {
                new
                {
                    Name = "products",
                    Description = "Shopify products",
                    Operations = new[]
                    {
                        new { Name = "query", Description = "Retrieves products" },
                        new { Name = "create", Description = "Creates a new product" },
                        new { Name = "update", Description = "Updates an existing product" },
                        new { Name = "delete", Description = "Deletes a product" }
                    },
                    CommonFields = new[]
                    {
                        "id", "title", "description", "handle", "productType", "vendor", "status"
                    }
                },
                new
                {
                    Name = "orders",
                    Description = "Shopify orders",
                    Operations = new[]
                    {
                        new { Name = "query", Description = "Retrieves orders" },
                        new { Name = "update", Description = "Updates an existing order" },
                        new { Name = "cancel", Description = "Cancels an order" }
                    },
                    CommonFields = new[]
                    {
                        "id", "name", "email", "createdAt", "totalPrice", "displayFinancialStatus", "displayFulfillmentStatus"
                    }
                },
                new
                {
                    Name = "customers",
                    Description = "Shopify customers",
                    Operations = new[]
                    {
                        new { Name = "query", Description = "Retrieves customers" },
                        new { Name = "create", Description = "Creates a new customer" },
                        new { Name = "update", Description = "Updates an existing customer" },
                        new { Name = "delete", Description = "Deletes a customer" }
                    },
                    CommonFields = new[]
                    {
                        "id", "firstName", "lastName", "email", "phone", "ordersCount", "totalSpent"
                    }
                },
                new
                {
                    Name = "collections",
                    Description = "Shopify collections",
                    Operations = new[]
                    {
                        new { Name = "query", Description = "Retrieves collections" },
                        new { Name = "create", Description = "Creates a new collection" },
                        new { Name = "update", Description = "Updates an existing collection" },
                        new { Name = "delete", Description = "Deletes a collection" }
                    },
                    CommonFields = new[]
                    {
                        "id", "title", "handle", "description", "productsCount"
                    }
                }
            },
      RequestFormat = new
      {
        Description = "Format of JSON request to be sent to the API",
        Example = new GraphQLRequest
        {
          Operation = "query",
          Resource = "products",
          Fields = new List<string> { "id", "title", "description" },
          Parameters = new Dictionary<string, object>
                    {
                        { "first", 5 }
                    },
          Filters = new Dictionary<string, object>
                    {
                        { "title", "Shirt" }
                    }
        }
      },
      ForClaudeAi = "This API allows you to interact with a Shopify store using JSON payloads that are converted to GraphQL queries. Follow the documentation carefully when constructing your requests. Each request should include an operation type, resource, fields to return, and any necessary parameters or filters. For mutations, include a data object with the appropriate fields. All requests will return a success or fail status along with any relevant data or error messages."
    };

    return Ok(docs);
  }
}