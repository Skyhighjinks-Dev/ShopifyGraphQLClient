using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;

namespace ShopifyGraphQLClient.Tests.Services;

public class ShopifyServiceTests
{
  private readonly Mock<IGraphQLConverter> _converterMock;
  private readonly Mock<ILogger<ShopifyService>> _loggerMock;
  private readonly Mock<IOptions<ShopifySettings>> _optionsMock;
  private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
  private readonly ShopifySettings _settings;

  public ShopifyServiceTests()
  {
    _converterMock = new Mock<IGraphQLConverter>();
    _loggerMock = new Mock<ILogger<ShopifyService>>();

    _settings = new ShopifySettings
    {
      StoreUrl = "https://test-store.myshopify.com",
      ApiVersion = "2023-10",
      AccessToken = "test-token",
      GraphQLEndpoint = "https://test-store.myshopify.com/admin/api/2023-10/graphql.json"
    };

    _optionsMock = new Mock<IOptions<ShopifySettings>>();
    _optionsMock.Setup(o => o.Value).Returns(_settings);

    _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
  }

  private HttpClient GetMockHttpClient()
  {
    return new HttpClient(_httpMessageHandlerMock.Object);
  }

  [Fact]
  public async Task ExecuteRequestAsync_ValidRequest_ReturnsSuccessResponse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    string graphQLQuery = "query { products { id title } }";

    var responseContent = new
    {
      data = new
      {
        products = new[]
            {
                    new { id = "1", title = "Product 1" },
                    new { id = "2", title = "Product 2" }
                }
      }
    };

    var responseJson = JsonSerializer.Serialize(responseContent);

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(request)).Returns(graphQLQuery);

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Data);
    Assert.Null(result.Error);
    Assert.Equal(graphQLQuery, result.Query);
  }

  [Fact]
  public async Task ExecuteRequestAsync_InvalidRequest_ReturnsErrorResponse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "",
      Fields = new List<string> { "id", "title" }
    };

    var validationErrors = new List<string> { "Resource cannot be empty" };

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(false);
    _converterMock.Setup(c => c.GetValidationErrors(request)).Returns(validationErrors);

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.False(result.Success);
    Assert.Null(result.Data);
    Assert.Contains("Invalid request", result.Error);
    Assert.Contains("Resource cannot be empty", result.Error);
  }

  [Fact]
  public async Task ExecuteRequestAsync_GraphQLError_ReturnsErrorResponse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    string graphQLQuery = "query { products { id title } }";

    var responseContent = new
    {
      errors = new[]
        {
                new { message = "Access denied" }
            }
    };

    var responseJson = JsonSerializer.Serialize(responseContent);

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(request)).Returns(graphQLQuery);

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        });

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.False(result.Success);
    Assert.Null(result.Data);
    Assert.NotNull(result.Error);
    Assert.Equal(graphQLQuery, result.Query);
  }

  [Fact]
  public async Task ExecuteRequestAsync_HttpError_ReturnsErrorResponse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    string graphQLQuery = "query { products { id title } }";

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(request)).Returns(graphQLQuery);

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.Unauthorized,
          Content = new StringContent("Unauthorized", Encoding.UTF8, "application/json")
        });

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.False(result.Success);
    Assert.Null(result.Data);
    Assert.Contains("HTTP error", result.Error);
    Assert.Equal(graphQLQuery, result.Query);
  }

  [Fact]
  public async Task ExecuteBulkRequestAsync_MultipleRequests_ProcessesAll()
  {
    // Arrange
    var requests = new List<GraphQLRequest>
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
        };

    string graphQLQuery1 = "query { products { id title } }";
    string graphQLQuery2 = "query { orders { id name } }";

    var responseContent1 = new
    {
      data = new
      {
        products = new[]
            {
                    new { id = "1", title = "Product 1" }
                }
      }
    };

    var responseContent2 = new
    {
      data = new
      {
        orders = new[]
            {
                    new { id = "101", name = "Order 101" }
                }
      }
    };

    _converterMock.Setup(c => c.ValidateRequest(requests[0])).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(requests[0])).Returns(graphQLQuery1);

    _converterMock.Setup(c => c.ValidateRequest(requests[1])).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(requests[1])).Returns(graphQLQuery2);

    // Set up mock to handle different queries by checking request content
    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Content != null &&
                req.Content.ReadAsStringAsync().Result.Contains("products")),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(JsonSerializer.Serialize(responseContent1), Encoding.UTF8, "application/json")
        });

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Content != null &&
                req.Content.ReadAsStringAsync().Result.Contains("orders")),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(JsonSerializer.Serialize(responseContent2), Encoding.UTF8, "application/json")
        });

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteBulkRequestAsync(requests);

    // Assert
    Assert.Equal(2, result.TotalProcessed);
    Assert.Equal(2, result.SuccessCount);
    Assert.Equal(0, result.FailCount);
    Assert.Equal(2, result.Responses.Count);
    Assert.True(result.Responses[0].Response.Success);
    Assert.True(result.Responses[1].Response.Success);
  }

  [Fact]
  public async Task ExecuteBulkRequestAsync_MixedResults_CountsSuccessAndFailures()
  {
    // Arrange
    var requests = new List<GraphQLRequest>
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
        };

    string graphQLQuery1 = "query { products { id title } }";

    var responseContent1 = new
    {
      data = new
      {
        products = new[]
            {
                    new { id = "1", title = "Product 1" }
                }
      }
    };

    _converterMock.Setup(c => c.ValidateRequest(requests[0])).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(requests[0])).Returns(graphQLQuery1);

    _converterMock.Setup(c => c.ValidateRequest(requests[1])).Returns(false);
    _converterMock.Setup(c => c.GetValidationErrors(requests[1])).Returns(new List<string> { "Resource cannot be empty" });

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(JsonSerializer.Serialize(responseContent1), Encoding.UTF8, "application/json")
        });

    var httpClient = GetMockHttpClient();
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteBulkRequestAsync(requests);

    // Assert
    Assert.Equal(2, result.TotalProcessed);
    Assert.Equal(1, result.SuccessCount);
    Assert.Equal(1, result.FailCount);
    Assert.Equal(2, result.Responses.Count);
    Assert.True(result.Responses[0].Response.Success);
    Assert.False(result.Responses[1].Response.Success);
    Assert.Contains("Resource cannot be empty", result.Responses[1].Response.Error);
  }
}