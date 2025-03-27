using Microsoft.AspNetCore.Mvc;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;

namespace ShopifyGraphQLClient.Controllers;

/// <summary>
/// Controller for handling bulk GraphQL operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BulkController : ControllerBase
{
  private readonly IShopifyService _shopifyService;
  private readonly ILogger<BulkController> _logger;

  public BulkController(
      IShopifyService shopifyService,
      ILogger<BulkController> logger)
  {
    _shopifyService = shopifyService;
    _logger = logger;
  }

  /// <summary>
  /// Executes multiple GraphQL requests in a single bulk operation
  /// </summary>
  /// <param name="request">The bulk request containing multiple GraphQL requests</param>
  /// <returns>A bulk response with results for each individual request</returns>
  [HttpPost]
  [ProducesResponseType(typeof(BulkResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(GraphQLResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(GraphQLResponse), StatusCodes.Status500InternalServerError)]
  public async Task<IActionResult> ExecuteBulkRequest([FromBody] BulkRequest request)
  {
    _logger.LogInformation("Received bulk request with {Count} items", request.Requests.Count);

    if (request.Requests == null || !request.Requests.Any())
    {
      _logger.LogWarning("Bad request: No requests provided in bulk operation");

      return BadRequest(new GraphQLResponse
      {
        Success = false,
        Error = "No requests provided in bulk operation"
      });
    }

    try
    {
      var response = await _shopifyService.ExecuteBulkRequestAsync(request.Requests);
      return Ok(response);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error processing bulk request");

      return StatusCode(StatusCodes.Status500InternalServerError, new GraphQLResponse
      {
        Success = false,
        Error = "An internal server error occurred during bulk processing"
      });
    }
  }
}