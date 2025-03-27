# ShopifyGraphQLClient Documentation for Claude

## IMPORTANT: READ BEFORE PROVIDING SHOPIFY REQUESTS

This is a documentation guide for using the ShopifyGraphQLClient API to interact with Shopify via GraphQL. When a user asks you to perform operations on their Shopify store, you'll need to construct a JSON payload conforming to this API's structure.

Before responding to any Shopify-related request:
1. Review this documentation thoroughly
2. Format your response as a valid JSON payload that matches the examples below
3. Include only required fields for the operation type
4. For mutations, ensure all necessary data fields are present
5. Follow success/error handling guidance

## API Overview

The ShopifyGraphQLClient converts simplified JSON requests to GraphQL queries. Key features:
- JSON-to-GraphQL conversion
- Support for queries (data retrieval) and mutations (data changes)
- Bulk operations
- Consistent success/failure responses

## Request Format

### Single Request

```json
{
  "operation": "query|mutation",
  "resource": "products|orders|customers|etc",
  "fields": ["field1", "field2", "..."],
  "parameters": {
    "paramName": "value"
  },
  "filters": {
    "filterName": "value"
  },
  "data": {
    "fieldName": "value"
  }
}
```

### Bulk Request

```json
{
  "requests": [
    {
      "operation": "query",
      "resource": "products",
      "fields": ["id", "title"]
    },
    {
      "operation": "query",
      "resource": "orders",
      "fields": ["id", "name"]
    }
  ]
}
```

## Common Resources and Operations

### Products

**Query Example:**
```json
{
  "operation": "query",
  "resource": "products",
  "fields": ["id", "title", "description", "productType", "vendor"],
  "parameters": {
    "first": 5
  },
  "filters": {
    "title": "T-Shirt"
  }
}
```

**Create Example:**
```json
{
  "operation": "mutation",
  "resource": "product",
  "fields": [
    "product { id, title }",
    "userErrors { field, message }"
  ],
  "data": {
    "title": "New Product",
    "productType": "Clothing",
    "vendor": "My Brand"
  }
}
```

**Update Example:**
```json
{
  "operation": "mutation",
  "resource": "product",
  "fields": [
    "product { id, title }",
    "userErrors { field, message }"
  ],
  "parameters": {
    "operation": "update"
  },
  "data": {
    "id": "gid://shopify/Product/12345",
    "title": "Updated Product Name"
  }
}
```

### Orders

**Query Example:**
```json
{
  "operation": "query",
  "resource": "orders",
  "fields": ["id", "name", "totalPrice", "displayFinancialStatus"],
  "parameters": {
    "first": 3
  },
  "filters": {
    "displayFinancialStatus": "PAID"
  }
}
```

### Customers

**Query Example:**
```json
{
  "operation": "query",
  "resource": "customers",
  "fields": ["id", "firstName", "lastName", "email", "ordersCount"],
  "parameters": {
    "first": 10
  }
}
```

## Field Requirements

### Always Required
- `operation`: Either "query" or "mutation"
- `resource`: The Shopify resource to access
- `fields`: The fields to return in the response

### Sometimes Required
- `data`: Required for mutations, contains the data to update or create
- `parameters`: Optional for queries, required for some mutations
- `filters`: Optional for queries, helps filter results

## Response Handling

All responses include a `success` flag. If `success` is `false`, an `error` message will be provided.

Always check for both successful and error responses:

```json
// Success example
{
  "success": true,
  "data": { ... },
  "error": null
}

// Error example
{
  "success": false,
  "data": null,
  "error": "Resource not found: Product with ID 12345 does not exist"
}
```

## For Claude AI Integration

When a user asks you to help with their Shopify store, you should:

1. Understand their request clearly
2. Format a valid JSON payload following this documentation
3. Include all required fields for the operation
4. Return the response with appropriate success/error handling
5. If the operation fails, help troubleshoot based on the error message

Do not attempt to execute GraphQL directly - always use the provided JSON format which will be converted to GraphQL by the ShopifyGraphQLClient. When in doubt, refer back to this documentation for the correct format.