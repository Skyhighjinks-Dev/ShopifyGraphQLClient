using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;
using static ShopifyGraphQLClient.Services.ShopifyService;

namespace ShopifyGraphQLClient.Tests.Services;

public class ShopifyServiceUriTests
{
  private readonly Mock<IGraphQLConverter> _converterMock;
  private readonly Mock<ILogger<ShopifyService>> _loggerMock;
  private readonly Mock<IOptions<ShopifySettings>> _optionsMock;
  private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

  public ShopifyServiceUriTests()
  {
    _converterMock = new Mock<IGraphQLConverter>();
    _loggerMock = new Mock<ILogger<ShopifyService>>();
    _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
    _optionsMock = new Mock<IOptions<ShopifySettings>>();
  }

  [Fact]
  public async Task ExecuteRequestAsync_WithAbsoluteUri_CallsCorrectEndpoint()
  {
    // Arrange
    var settings = new ShopifySettings
    {
      StoreUrl = "https://ze5j0r-dt.myshopify.com",
      ApiVersion = "2025-01",
      AccessToken = "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",
      GraphQLEndpoint = "https://ze5j0r-dt.myshopify.com/admin/api/2023-10/graphql.json"
    };

    _optionsMock.Setup(o => o.Value).Returns(settings);

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
                    new { id = "1", title = "Product 1" }
                }
      }
    };

    var responseJson = System.Text.Json.JsonSerializer.Serialize(responseContent);

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(request)).Returns(graphQLQuery);

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == settings.GraphQLEndpoint),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Data);

    _httpMessageHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Once(),
        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == settings.GraphQLEndpoint),
        ItExpr.IsAny<CancellationToken>()
    );
  }

  [Fact]
  public async Task ExecuteRequestAsync_WithRelativeUri_UsesBaseAddress()
  {
    // Arrange
    var settings = new ShopifySettings
    {
      StoreUrl = "https://test-store.myshopify.com",
      ApiVersion = "2023-10",
      AccessToken = "test-token",
      GraphQLEndpoint = "/admin/api/2023-10/graphql.json" // Relative URI
    };

    _optionsMock.Setup(o => o.Value).Returns(settings);

    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    string graphQLQuery = "query { products { id title } }";
    string expectedAbsoluteUri = "https://test-store.myshopify.com/admin/api/2023-10/graphql.json";

    var responseContent = new
    {
      data = new
      {
        products = new[]
            {
                    new { id = "1", title = "Product 1" }
                }
      }
    };

    var responseJson = System.Text.Json.JsonSerializer.Serialize(responseContent);

    _converterMock.Setup(c => c.ValidateRequest(request)).Returns(true);
    _converterMock.Setup(c => c.ConvertToGraphQL(request)).Returns(graphQLQuery);

    _httpMessageHandlerMock
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == expectedAbsoluteUri),
            ItExpr.IsAny<CancellationToken>()
        )
        .ReturnsAsync(new HttpResponseMessage
        {
          StatusCode = HttpStatusCode.OK,
          Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    var service = new ShopifyService(httpClient, _converterMock.Object, _optionsMock.Object, _loggerMock.Object);

    // Act
    var result = await service.ExecuteRequestAsync(request);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Data);

    _httpMessageHandlerMock.Protected().Verify(
        "SendAsync",
        Times.Once(),
        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsoluteUri == expectedAbsoluteUri),
        ItExpr.IsAny<CancellationToken>()
    );
  }
}