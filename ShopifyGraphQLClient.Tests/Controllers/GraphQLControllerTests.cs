using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ShopifyGraphQLClient.Controllers;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;

namespace ShopifyGraphQLClient.Tests.Controllers;

public class GraphQLControllerTests
{
  private readonly Mock<IShopifyService> _shopifyServiceMock;
  private readonly Mock<ILogger<GraphQLController>> _loggerMock;
  private readonly GraphQLController _controller;

  public GraphQLControllerTests()
  {
    _shopifyServiceMock = new Mock<IShopifyService>();
    _loggerMock = new Mock<ILogger<GraphQLController>>();
    _controller = new GraphQLController(_shopifyServiceMock.Object, _loggerMock.Object);
  }

  [Fact]
  public async Task ExecuteRequest_ValidRequest_ReturnsOkResult()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    var expectedResponse = new GraphQLResponse
    {
      Success = true,
      Data = new { products = new[] { new { id = "1", title = "Product 1" } } },
      Query = "query { products { id title } }"
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteRequestAsync(request))
        .ReturnsAsync(expectedResponse);

    // Act
    var result = await _controller.ExecuteRequest(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<GraphQLResponse>(okResult.Value);

    Assert.True(response.Success);
    Assert.NotNull(response.Data);
    Assert.Null(response.Error);
    Assert.Equal(expectedResponse.Query, response.Query);
  }

  [Fact]
  public async Task ExecuteRequest_ServiceReturnsError_ReturnsOkWithError()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    var expectedResponse = new GraphQLResponse
    {
      Success = false,
      Error = "GraphQL error: Access denied",
      Query = "query { products { id title } }"
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteRequestAsync(request))
        .ReturnsAsync(expectedResponse);

    // Act
    var result = await _controller.ExecuteRequest(request);

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var response = Assert.IsType<GraphQLResponse>(okResult.Value);

    Assert.False(response.Success);
    Assert.Null(response.Data);
    Assert.Equal("GraphQL error: Access denied", response.Error);
    Assert.Equal(expectedResponse.Query, response.Query);
  }

  [Fact]
  public async Task ExecuteRequest_ValidationException_ReturnsBadRequestWithError()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "",
      Fields = new List<string> { "id", "title" }
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteRequestAsync(request))
        .ThrowsAsync(new ArgumentException("Invalid request: Resource cannot be empty"));

    // Act
    var result = await _controller.ExecuteRequest(request);

    // Assert
    var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<GraphQLResponse>(badRequestResult.Value);

    Assert.False(response.Success);
    Assert.Null(response.Data);
    Assert.Equal("Invalid request: Resource cannot be empty", response.Error);
  }

  [Fact]
  public async Task ExecuteRequest_UnexpectedException_ReturnsInternalServerError()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    _shopifyServiceMock
        .Setup(s => s.ExecuteRequestAsync(request))
        .ThrowsAsync(new Exception("Unexpected error"));

    // Act
    var result = await _controller.ExecuteRequest(request);

    // Assert
    var statusCodeResult = Assert.IsType<ObjectResult>(result);
    Assert.Equal(500, statusCodeResult.StatusCode);

    var response = Assert.IsType<GraphQLResponse>(statusCodeResult.Value);
    Assert.False(response.Success);
    Assert.Null(response.Data);
    Assert.Equal("An internal server error occurred", response.Error);
  }

  [Fact]
  public void GetDocumentation_ReturnsOkResultWithDocs()
  {
    // Act
    var result = _controller.GetDocumentation();

    // Assert
    var okResult = Assert.IsType<OkObjectResult>(result);
    var docs = okResult.Value;

    // Verify docs contains expected data
    Assert.NotNull(docs);

    // Reflection to check properties without strongly typed object
    var docType = docs.GetType();

    // Check for Resources property
    var resourcesProp = docType.GetProperty("Resources");
    Assert.NotNull(resourcesProp);

    // Check for RequestFormat property
    var requestFormatProp = docType.GetProperty("RequestFormat");
    Assert.NotNull(requestFormatProp);

    // Check for ForClaudeAi property
    var clauseAiProp = docType.GetProperty("ForClaudeAi");
    Assert.NotNull(clauseAiProp);
  }
}