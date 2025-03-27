using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Tests.TestData;

/// <summary>
/// Provides sample GraphQL requests and responses for testing
/// </summary>
public static class GraphQLSamples
{
  /// <summary>
  /// Sample query to get products
  /// </summary>
  public static GraphQLRequest ProductsQuery => new()
  {
    Operation = "query",
    Resource = "products",
    Fields = new List<string> { "id", "title", "description", "productType" },
    Parameters = new Dictionary<string, object>
        {
            { "first", 5 }
        }
  };

  /// <summary>
  /// Sample query to get a specific product by ID
  /// </summary>
  public static GraphQLRequest ProductByIdQuery => new()
  {
    Operation = "query",
    Resource = "product",
    Fields = new List<string> { "id", "title", "description", "productType", "vendor" },
    Parameters = new Dictionary<string, object>
        {
            { "id", "gid://shopify/Product/12345" }
        }
  };

  /// <summary>
  /// Sample mutation to create a new product
  /// </summary>
  public static GraphQLRequest CreateProductMutation => new()
  {
    Operation = "mutation",
    Resource = "product",
    Fields = new List<string>
        {
            "product { id, title }",
            "userErrors { field, message }"
        },
    Data = new Dictionary<string, object>
        {
            { "title", "New Test Product" },
            { "productType", "Test" },
            { "vendor", "Test Vendor" },
            { "descriptionHtml", "<p>Test product description</p>" }
        }
  };

  /// <summary>
  /// Sample mutation to update an existing product
  /// </summary>
  public static GraphQLRequest UpdateProductMutation => new()
  {
    Operation = "mutation",
    Resource = "product",
    Fields = new List<string>
        {
            "product { id, title }",
            "userErrors { field, message }"
        },
    Parameters = new Dictionary<string, object>
        {
            { "operation", "update" }
        },
    Data = new Dictionary<string, object>
        {
            { "id", "gid://shopify/Product/12345" },
            { "title", "Updated Product Title" }
        }
  };

  /// <summary>
  /// Sample query to get orders
  /// </summary>
  public static GraphQLRequest OrdersQuery => new()
  {
    Operation = "query",
    Resource = "orders",
    Fields = new List<string>
        {
            "id",
            "name",
            "email",
            "totalPrice",
            "displayFinancialStatus"
        },
    Parameters = new Dictionary<string, object>
        {
            { "first", 10 }
        },
    Filters = new Dictionary<string, object>
        {
            { "displayFinancialStatus", "PAID" }
        }
  };

  /// <summary>
  /// Sample query to get customers
  /// </summary>
  public static GraphQLRequest CustomersQuery => new()
  {
    Operation = "query",
    Resource = "customers",
    Fields = new List<string>
        {
            "id",
            "firstName",
            "lastName",
            "email",
            "ordersCount",
            "totalSpent"
        },
    Parameters = new Dictionary<string, object>
        {
            { "first", 20 }
        }
  };

  /// <summary>
  /// Sample bulk request with multiple queries
  /// </summary>
  public static BulkRequest BulkSampleQueries => new()
  {
    Requests = new List<GraphQLRequest>
        {
            ProductsQuery,
            OrdersQuery,
            CustomersQuery
        }
  };
}