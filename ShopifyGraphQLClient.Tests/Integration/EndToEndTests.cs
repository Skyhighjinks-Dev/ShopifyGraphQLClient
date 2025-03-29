using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;
using static ShopifyGraphQLClient.Services.ShopifyService;

namespace ShopifyGraphQLClient.Tests.Integration;

public class EndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;
  private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

  public EndToEndTests(WebApplicationFactory<Program> factory)
  {
    _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

    // Create a custom WebApplicationFactory with mocked services
    _factory = factory.WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        // Remove the existing HttpClient
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(HttpClient));

        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        // Add our own HttpClient with mocked handler
        services.AddTransient(_ => new HttpClient(_httpMessageHandlerMock.Object));

        // Replace ShopifySettings with test values
        services.Configure<ShopifySettings>(opt =>
        {
          opt.StoreUrl = "https://ze5j0r-dt.myshopify.com";
          opt.ApiVersion = "2025-01";
          opt.AccessToken = "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";
          opt.GraphQLEndpoint = "https://ze5j0r-dt.myshopify.com/admin/api/2023-10/graphql.json";
        });
      });
    });
  }

  [Fact]
  public async Task GraphQLEndpoint_ReturnsSuccessResponse()
  {
    // Arrange
    var client = _factory.CreateClient();

    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

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
          Content = new StringContent(JsonSerializer.Serialize(responseContent))
        });

    // Act
    var response = await client.PostAsJsonAsync("/api/graphql", request);

    // Assert
    response.EnsureSuccessStatusCode();

    var responseBody = await response.Content.ReadFromJsonAsync<GraphQLResponse>();
    Assert.NotNull(responseBody);
    Assert.True(responseBody.Success);
    Assert.NotNull(responseBody.Data);
  }

  [Fact]
  public async Task GraphQLEndpoint_WithInvalidRequest_ReturnsErrorResponse()
  {
    // Arrange
    var client = _factory.CreateClient();

    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "", // Invalid
      Fields = new List<string> { "id", "title" }
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/graphql", request);

    // Assert
    // We're now returning 200 OK for valid API calls with GraphQL errors
    // as per GraphQLController's implementation, check the success flag instead
    response.EnsureSuccessStatusCode();

    var responseBody = await response.Content.ReadFromJsonAsync<GraphQLResponse>();
    Assert.NotNull(responseBody);
    Assert.False(responseBody.Success);
    Assert.NotNull(responseBody.Error);
    Assert.Contains("Resource cannot be empty", responseBody.Error);
  }

  [Fact]
  public async Task BulkEndpoint_ProcessesMultipleRequests()
  {
    // Arrange
    var client = _factory.CreateClient();

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

    // Set up the first response
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
          Content = new StringContent(JsonSerializer.Serialize(responseContent1))
        });

    // Set up the second response
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
          Content = new StringContent(JsonSerializer.Serialize(responseContent2))
        });

    // Act
    var response = await client.PostAsJsonAsync("/api/bulk", bulkRequest);

    // Assert
    response.EnsureSuccessStatusCode();

    var responseBody = await response.Content.ReadFromJsonAsync<BulkResponse>();
    Assert.NotNull(responseBody);
    Assert.Equal(2, responseBody.TotalProcessed);
    Assert.Equal(2, responseBody.SuccessCount);
    Assert.Equal(0, responseBody.FailCount);
    Assert.Equal(2, responseBody.Responses.Count);
  }

  [Fact]
  public async Task DocsEndpoint_ReturnsDocumentation()
  {
    // Arrange
    var client = _factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/graphql/docs");

    // Assert
    response.EnsureSuccessStatusCode();

    var content = await response.Content.ReadAsStringAsync();
    Assert.NotEmpty(content);

    // Parse the JSON to check for expected properties
    var jsonDoc = JsonDocument.Parse(content);
    var root = jsonDoc.RootElement;

    // Check if the properties exist directly
    Assert.True(root.TryGetProperty("resources", out _), "Property 'resources' not found");
    Assert.True(root.TryGetProperty("requestFormat", out _), "Property 'requestFormat' not found");
    Assert.True(root.TryGetProperty("forClaudeAi", out _), "Property 'forClaudeAi' not found");
  }
}