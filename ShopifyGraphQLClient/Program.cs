using ShopifyGraphQLClient.Services;
using ShopifyGraphQLClient.Middleware;
using static ShopifyGraphQLClient.Services.ShopifyService;

namespace ShopifyGraphQLClient
{
  public class Program
  {
    static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      // Add services to the container
      builder.Services.AddControllers();
      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new() { Title = "Shopify GraphQL Client API", Version = "v1" });
      });

      // Configure Shopify settings
      builder.Services.Configure<ShopifySettings>(
          builder.Configuration.GetSection("ShopifySettings"));

      // Register services
      builder.Services.AddHttpClient();
      builder.Services.AddScoped<IGraphQLConverter, GraphQLConverter>();
      builder.Services.AddScoped<IShopifyService, ShopifyService>();

      // Configure CORS
      builder.Services.AddCors(options =>
      {
        options.AddPolicy("AllowAll", builder =>
        {
          builder.AllowAnyOrigin()
                 .AllowAnyMethod()
                 .AllowAnyHeader();
        });
      });

      var app = builder.Build();

      // Configure middleware
      if (app.Environment.IsDevelopment())
      {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseMiddleware<ErrorHandlingMiddleware>();
      }

      app.UseHttpsRedirection();
      app.UseCors("AllowAll");
      app.UseAuthorization();
      app.MapControllers();

      app.Run();
    }
  }
}