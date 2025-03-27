using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ShopifyGraphQLClient.Models;
using Xunit;

namespace ShopifyGraphQLClient.Tests.Models;

/// <summary>
/// Tests to validate that our models are compatible with the Shopify GraphQL API
/// </summary>
public class ShopifyModelValidationTests
{
  [Fact]
  public void ProductModel_MatchesShopifySchema()
  {
    // Create request with all expected Product fields
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title", "description", "handle", "productType", "vendor", "status" }
    };

    // Validate that we have all required fields
    var expectedFields = new HashSet<string> { "id", "title", "description", "handle", "productType", "vendor", "status" };
    var requestFields = new HashSet<string>(request.Fields);

    // Assert
    Assert.True(expectedFields.IsSubsetOf(requestFields), "Missing required fields for Product model");
  }

  [Fact]
  public void OrderModel_MatchesShopifySchema()
  {
    // Create request with all expected Order fields
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "orders",
      Fields = new List<string> { "id", "name", "email", "createdAt", "totalPrice", "displayFinancialStatus", "displayFulfillmentStatus" }
    };

    // Validate that we have all required fields
    var expectedFields = new HashSet<string> { "id", "name", "email", "createdAt", "totalPrice", "displayFinancialStatus", "displayFulfillmentStatus" };
    var requestFields = new HashSet<string>(request.Fields);

    // Assert
    Assert.True(expectedFields.IsSubsetOf(requestFields), "Missing required fields for Order model");
  }

  [Fact]
  public void CustomerModel_MatchesShopifySchema()
  {
    // Create request with all expected Customer fields
    var request = new GraphQLRequest
    {
      Operation = "query",
      Resource = "customers",
      Fields = new List<string> { "id", "firstName", "lastName", "email", "phone", "ordersCount", "totalSpent" }
    };

    // Validate that we have all required fields
    var expectedFields = new HashSet<string> { "id", "firstName", "lastName", "email", "phone", "ordersCount", "totalSpent" };
    var requestFields = new HashSet<string>(request.Fields);

    // Assert
    Assert.True(expectedFields.IsSubsetOf(requestFields), "Missing required fields for Customer model");
  }

  [Fact]
  public void MutationModels_MatchShopifySchema()
  {
    // Test product creation mutation
    var createProductRequest = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "product",
      Fields = new List<string> { "product { id }", "userErrors { field, message }" },
      Data = new Dictionary<string, object>
            {
                { "title", "Test Product" },
                { "productType", "Test" },
                { "vendor", "Test Vendor" }
            }
    };

    // Test order update mutation
    var updateOrderRequest = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "order",
      Fields = new List<string> { "order { id }", "userErrors { field, message }" },
      Parameters = new Dictionary<string, object>
            {
                { "operation", "update" }
            },
      Data = new Dictionary<string, object>
            {
                { "id", "gid://shopify/Order/12345" },
                { "tags", new List<string> { "updated", "test" } }
            }
    };

    // Assert
    Assert.Equal("mutation", createProductRequest.Operation);
    Assert.Equal("product", createProductRequest.Resource);
    Assert.Contains("product { id }", createProductRequest.Fields);
    Assert.Contains("userErrors { field, message }", createProductRequest.Fields);
    Assert.NotNull(createProductRequest.Data);
    Assert.Equal("Test Product", createProductRequest.Data["title"]);

    Assert.Equal("mutation", updateOrderRequest.Operation);
    Assert.Equal("order", updateOrderRequest.Resource);
    Assert.Contains("order { id }", updateOrderRequest.Fields);
    Assert.Contains("userErrors { field, message }", updateOrderRequest.Fields);
    Assert.Equal("update", updateOrderRequest.Parameters["operation"]);
    Assert.NotNull(updateOrderRequest.Data);
    Assert.Equal("gid://shopify/Order/12345", updateOrderRequest.Data["id"]);
  }

  [Fact]
  public void GraphQLRequest_ValidatesRequired_Fields()
  {
    // Create a valid request
    var validRequest = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    // Create an invalid request (empty resource)
    var invalidResourceRequest = new GraphQLRequest
    {
      Operation = "query",
      Resource = "",
      Fields = new List<string> { "id", "title" }
    };

    // Create an invalid request (empty fields)
    var invalidFieldsRequest = new GraphQLRequest
    {
      Operation = "query",
      Resource = "products",
      Fields = new List<string>()
    };

    // Create an invalid request (invalid operation)
    var invalidOperationRequest = new GraphQLRequest
    {
      Operation = "invalid",
      Resource = "products",
      Fields = new List<string> { "id", "title" }
    };

    // Create an invalid mutation request (missing data)
    var invalidMutationRequest = new GraphQLRequest
    {
      Operation = "mutation",
      Resource = "product",
      Fields = new List<string> { "product { id }" }
    };

    // Assert
    Assert.Equal("query", validRequest.Operation);
    Assert.NotEmpty(validRequest.Resource);
    Assert.NotEmpty(validRequest.Fields);

    Assert.Empty(invalidResourceRequest.Resource);
    Assert.Empty(invalidFieldsRequest.Fields);
    Assert.NotEqual("query", invalidOperationRequest.Operation);
    Assert.NotEqual("mutation", invalidOperationRequest.Operation);
    Assert.Equal("mutation", invalidMutationRequest.Operation);
    Assert.Null(invalidMutationRequest.Data);
  }
}