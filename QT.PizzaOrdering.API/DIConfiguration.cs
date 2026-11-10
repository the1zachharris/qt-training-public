using QT.PizzaOrdering.API.Adapters.Data;
using QT.PizzaOrdering.API.Features.Menu;
using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Abstractions;
using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Interfaces;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Infrastructure;

using Serilog;

namespace QT.PizzaOrdering.API;

public static class DIConfiguration
{
    public const string TestEnvironmentName = "Test";

    public static bool IsTest(this IHostEnvironment environment) => environment.IsEnvironment(TestEnvironmentName);

    public static void AddNonTestServices(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration), writeToProviders: true);
    }

    public static void AddDbContexts(this WebApplicationBuilder builder)
    {
        // Tests register their own in-memory SQLite context.
        if (builder.Environment.IsTest())
        {
            return;
        }

        string connectionString = builder.Configuration.GetConnectionString(DatabaseConfiguration.ConnectionString)
            ?? throw new InvalidOperationException($"Cannot find connection string '{DatabaseConfiguration.ConnectionString}' in appsettings.");
        builder.Services.AddPizzaOrderingDbContext(
            DbContextRegistration.ResolveSqliteConnectionString(connectionString, builder.Environment.ContentRootPath));
    }

    public static void AddMenuHandlers(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IMenuQueries, MenuQueries>();
        builder.Services.AddScoped<GetMenuHandler>();
    }

    public static void AddOrderHandlers(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        builder.Services.AddScoped<PlaceOrderHandler>();
        builder.Services.AddScoped<IValidator<PlaceOrderRequest>, PlaceOrderRequestValidator>();

        builder.Services.AddScoped<GetOrderHandler>();

        builder.Services.AddScoped<GetKitchenOrdersHandler>();
        builder.Services.AddScoped<MarkOrderReadyHandler>();
    }
}
