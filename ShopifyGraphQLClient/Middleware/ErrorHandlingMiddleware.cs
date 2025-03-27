using System.Net;
using System.Text.Json;
using ShopifyGraphQLClient.Models;

namespace ShopifyGraphQLClient.Middleware;

/// <summary>
/// Middleware for global error handling
/// </summary>
public class ErrorHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ErrorHandlingMiddleware> _logger;

  public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    try
    {
      await _next(context);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception");
      await HandleExceptionAsync(context, ex);
    }
  }

  private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
  {
    context.Response.ContentType = "application/json";
    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

    var response = new GraphQLResponse
    {
      Success = false,
      Error = "An unexpected error occurred. Please try again later."
    };

    var json = JsonSerializer.Serialize(response);
    await context.Response.WriteAsync(json);
  }
}