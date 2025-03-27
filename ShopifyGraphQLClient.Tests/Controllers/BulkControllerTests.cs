using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ShopifyGraphQLClient.Controllers;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;

namespace ShopifyGraphQLClient.Tests.Controllers;

public class BulkControllerTests
{
  private readonly Mock<IShopifyService> _shopifyServiceMock;
  private readonly Mock<ILogger<BulkController>> _loggerMock;
  private readonly BulkController _controller;

  public BulkControllerTests()
  {
    _shopifyServiceMock = new Mock<IShopifyService>();
    _loggerMock = new Mock<ILogger<BulkController>>();
    _controller = new BulkController(_shopifyServiceMock.Object, _loggerMock.Object);
  }

  [Fact]
  public async Task ExecuteBulkRequest_ValidRequests_ReturnsOkResult()
  {
    // Arrange
    var bulkRequest = new BulkRequest
    {
      Requests = new List<GraphQLRequest>
            {
                new()
                {
                    Operation = "query",
                    Resource = "products",
                    Fields = new List<string> { "id", "title" }
                },
                new()
                {
                    Operation = "query",
                    Resource = "orders",
                    Fields = new List<string> { "id", "name" }
                }
            }
    };

    var expectedResponse = new BulkResponse
    {
      TotalProcessed = 2,
      SuccessCount = 2,
      FailCount = 0,
      Responses = new List<BulkResponseItem>
            {
                new()
                {
                    Index = 0,
                    Response = new GraphQLResponse
                    {
                        Success = true,
                        Data = new { products = new[] { new { id = "1", title = "Product 1" } } },
                        Query = "query { products { id title } }"
                    }
                },
                new()
                {
                    Index = 1,
                    Response = new GraphQLResponse
                    {
                        Success = true,
                        Data = new { orders = new[] { new { id = "101", name = "Order 101" } } },
                        Query = "query { orders { id name } }"
                    }
                }
            }
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteBulkRequestAsync(bulkRequest.Requests))
        .ReturnsAsync(expectedResponse);

    // Act
    var result = await _controller.ExecuteBulkRequest(bulkRequest);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<BulkResponse>(okResult.Value);

    Assert.Equal(2, response.TotalProcessed);
    Assert.Equal(2, response.SuccessCount);
    Assert.Equal(0, response.FailCount);
    Assert.Equal(2, response.Responses.Count);
    Assert.True(response.Responses[0].Response.Success);
    Assert.True(response.Responses[1].Response.Success);
  }

  [Fact]
  public async Task ExecuteBulkRequest_EmptyRequests_ReturnsBadRequest()
  {
    // Arrange
    var bulkRequest = new BulkRequest
    {
      Requests = new List<GraphQLRequest>()
    };

    // Act
    var result = await _controller.ExecuteBulkRequest(bulkRequest);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<GraphQLResponse>(badRequestResult.Value);

    Assert.False(response.Success);
    Assert.Null(response.Data);
    Assert.Equal("No requests provided in bulk operation", response.Error);
  }

  [Fact]
  public async Task ExecuteBulkRequest_MixedResults_ReturnsOkWithMixedResponses()
  {
    // Arrange
    var bulkRequest = new BulkRequest
    {
      Requests = new List<GraphQLRequest>
            {
                new()
                {
                    Operation = "query",
                    Resource = "products",
                    Fields = new List<string> { "id", "title" }
                },
                new()
                {
                    Operation = "query",
                    Resource = "", // Invalid
                    Fields = new List<string> { "id", "name" }
                }
            }
    };

    var expectedResponse = new BulkResponse
    {
      TotalProcessed = 2,
      SuccessCount = 1,
      FailCount = 1,
      Responses = new List<BulkResponseItem>
            {
                new()
                {
                    Index = 0,
                    Response = new GraphQLResponse
                    {
                        Success = true,
                        Data = new { products = new[] { new { id = "1", title = "Product 1" } } },
                        Query = "query { products { id title } }"
                    }
                },
                new()
                {
                    Index = 1,
                    Response = new GraphQLResponse
                    {
                        Success = false,
                        Error = "Invalid request: Resource cannot be empty",
                        Query = null
                    }
                }
            }
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteBulkRequestAsync(bulkRequest.Requests))
        .ReturnsAsync(expectedResponse);

    // Act
    var result = await _controller.ExecuteBulkRequest(bulkRequest);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<BulkResponse>(okResult.Value);

    Assert.Equal(2, response.TotalProcessed);
    Assert.Equal(1, response.SuccessCount);
    Assert.Equal(1, response.FailCount);
    Assert.Equal(2, response.Responses.Count);
    Assert.True(response.Responses[0].Response.Success);
    Assert.False(response.Responses[1].Response.Success);
  }

  [Fact]
  public async Task ExecuteBulkRequest_Exception_ReturnsInternalServerError()
  {
    // Arrange
    var bulkRequest = new BulkRequest
    {
      Requests = new List<GraphQLRequest>
            {
                new()
                {
                    Operation = "query",
                    Resource = "products",
                    Fields = new List<string> { "id", "title" }
                }
            }
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteBulkRequestAsync(bulkRequest.Requests))
        .ThrowsAsync(new Exception("Unexpected error"));

    // Act
    var result = await _controller.ExecuteBulkRequest(bulkRequest);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);

    var response = Assert.IsType<GraphQLResponse>(statusCodeResult.Value);
    Assert.False(response.Success);
    Assert.Null(response.Data);
    Assert.Equal("An internal server error occurred during bulk processing", response.Error);
  }
}