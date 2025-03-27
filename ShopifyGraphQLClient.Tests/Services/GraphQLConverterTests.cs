using Microsoft.Extensions.Logging;
using Moq;
using ShopifyGraphQLClient.Models;
using ShopifyGraphQLClient.Services;
using Xunit;

namespace ShopifyGraphQLClient.Tests.Services;

public class GraphQLConverterTests
{
  private readonly GraphQLConverter _converter;
  private readonly Mock<ILogger<GraphQLConverter>> _loggerMock;

  public GraphQLConverterTests()
  {
    _loggerMock = new Mock<ILogger<GraphQLConverter>>();
    _converter = new GraphQLConverter(_loggerMock.Object);
  }

  [Fact]
  public void ValidateRequest_ValidQueryRequest_ReturnsTrue()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ValidateRequest_EmptyResource_ReturnsFalse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "",
      Fields = new List<string> { "id", "title" }
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ValidateRequest_EmptyFields_ReturnsFalse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string>()
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ValidateRequest_InvalidOperation_ReturnsFalse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "invalid",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ValidateRequest_MutationWithoutData_ReturnsFalse()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "products",
      Fields = new List<string> { "product { id }" }
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.False(result);
  }

  [Fact]
  public void ValidateRequest_ValidMutationRequest_ReturnsTrue()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "products",
      Fields = new List<string> { "product { id }" },
      Data = new Dictionary<string, object>
            {
                { "title", "Test Product" }
            }
    };

    // Act
    var result = _converter.ValidateRequest(request);

    // Assert
    Assert.True(result);
  }

  [Fact]
  public void ConvertToGraphQL_SimpleQuery_ReturnsCorrectQuery()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    // Act
    var result = _converter.ConvertToGraphQL(request);

    // Assert
    Assert.Contains("query {", result);
    Assert.Contains("products {", result);
    Assert.Contains("id", result);
    Assert.Contains("title", result);
  }

  [Fact]
  public void ConvertToGraphQL_QueryWithParameters_ReturnsCorrectQuery()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" },
      Parameters = new Dictionary<string, object>
            {
                { "first", 5 }
            }
    };

    // Act
    var result = _converter.ConvertToGraphQL(request);

    // Assert
    Assert.Contains("products(first: 5)", result);
  }

  [Fact]
  public void ConvertToGraphQL_QueryWithFilters_ReturnsCorrectQuery()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" },
      Filters = new Dictionary<string, object>
            {
                { "title", "Test" }
            }
    };

    // Act
    var result = _converter.ConvertToGraphQL(request);

    // Assert
    Assert.Contains("products(query: { title: \"Test\" })", result);
  }

  [Fact]
  public void ConvertToGraphQL_SimpleMutation_ReturnsCorrectQuery()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "product",
      Fields = new List<string> { "product { id }", "userErrors { field, message }" },
      Data = new Dictionary<string, object>
            {
                { "title", "Test Product" },
                { "productType", "Test" }
            }
    };

    // Act
    var result = _converter.ConvertToGraphQL(request);

    // Assert
    Assert.Contains("mutation {", result);
    Assert.Contains("productCreate(input: {", result);
    Assert.Contains("title: \"Test Product\"", result);
    Assert.Contains("productType: \"Test\"", result);
    Assert.Contains("product { id }", result);
    Assert.Contains("userErrors { field, message }", result);
  }

  [Fact]
  public void ConvertToGraphQL_MutationWithSpecificOperation_ReturnsCorrectQuery()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "product",
      Fields = new List<string> { "product { id }" },
      Parameters = new Dictionary<string, object>
            {
                { "operation", "update" }
            },
      Data = new Dictionary<string, object>
            {
                { "id", "gid://shopify/Product/12345" },
                { "title", "Updated Product" }
            }
    };

    // Act
    var result = _converter.ConvertToGraphQL(request);

    // Assert
    Assert.Contains("mutation {", result);
    Assert.Contains("productUpdate(input: {", result);
    Assert.Contains("id: \"gid://shopify/Product/12345\"", result);
    Assert.Contains("title: \"Updated Product\"", result);
  }

  [Fact]
  public void ConvertToGraphQL_InvalidRequest_ThrowsArgumentException()
  {
    // Arrange
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "",
      Fields = new List<string> { "id", "title" }
    };

    // Act & Assert
    Assert.Throws<ArgumentException>(() => _converter.ConvertToGraphQL(request));
  }
}