# ShopifyGraphQLClient Reference Guide for Claude

## API Architecture
- REST API wrapper around Shopify GraphQL
- JSON request format -> GraphQL conversion -> Shopify API -> JSON response
- Endpoints: 
  - `/api/graphql` - Single requests
  - `/api/bulk` - Multiple requests
  - `/api/graphql/docs` - Documentation

## Request Structure
```json
{
  "operation": "query|mutation",  // Required
  "resource": "products|orders|customers|etc",  // Required
  "fields": ["field1", "field2"],  // Required - Handle connection patterns
  "parameters": { "first": 50 },  // Optional - Pagination/filters
  "filters": { "query": "status:open" },  // Optional - Search params
  "data": { "field": "value" }  // Required for mutations only
}
```

## Connection Pattern Requirements
- All collection queries use Shopify connection pattern
- Format: `edges { node { fields } }`
- Each connection REQUIRES `first` or `last` parameter
- This applies to NESTED connections (variants, lineItems, etc.)

## Examples for Product Queries
```json
{
  "operation": "query",
  "resource": "products",
  "fields": [
    "edges { node { id, title, description, variants(first: 20) { edges { node { id, sku, price } } } } }"
  ],
  "parameters": {
    "first": 50
  }
}
```

## Examples for Mutations
```json
{
  "operation": "mutation",
  "resource": "product",
  "fields": ["product { id }", "userErrors { field, message }"],
  "parameters": { "operation": "update" },  // For updates
  "data": {
    "id": "gid://shopify/Product/12345",
    "title": "Updated Name"
  }
}
```

## Common Errors
- "Invalid URI" - Absolute GraphQLEndpoint URL required in settings
- "Field doesn't exist on type ProductConnection" - Missing edges/node pattern
- "You must provide one of first or last" - Missing pagination parameter
- Authentication errors - Check token in settings

## Configuration
- Settings in appsettings.json:
  - StoreUrl
  - ApiVersion
  - AccessToken
  - GraphQLEndpoint (must be absolute URL)