using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Infrastructure;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Infrastructure;

/// <summary>
/// AC-4.3: orders and their current statuses remain available after the application is closed
/// and reopened. Each "restart" here is a new host opening the same database file.
/// </summary>
public sealed class OrderPersistenceTests : IDisposable
{
    private readonly TempDatabase _database = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _database.Dispose();

    private PizzaOrderingWebAppFactory StartApp() => new() { DatabasePath = _database.Path };

    private static async Task<OrderDetails> PlaceAsync(HttpClient client, PlaceOrderRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/v1/orders", request, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDetails>(Ct))!;
    }

    [Fact]
    public async Task OrdersAndStatuses_SurviveARestart()
    {
        await using (PizzaOrderingWebAppFactory beforeRestart = StartApp())
        {
            HttpClient client = beforeRestart.CreateClient();
            OrderDetails first = await PlaceAsync(client, Pizzas.Order(Pizzas.Custom(Pizzas.Small)) with { CustomerName = "Pat Sample" });
            await PlaceAsync(client, Pizzas.Order(Pizzas.Custom(Pizzas.Large, quantity: 2, toppingIds: [Pizzas.Pepperoni, Pizzas.Onions])) with { CustomerName = "Sam Example" });
            (await client.PatchAsync($"api/v1/kitchen/orders/{first.Id}/ready", content: null, Ct)).EnsureSuccessStatusCode();
        }

        await using PizzaOrderingWebAppFactory afterRestart = StartApp();
        HttpClient reopened = afterRestart.CreateClient();
        KitchenOrders kitchen = (await reopened.GetFromJsonAsync<KitchenOrders>("api/v1/kitchen/orders", Ct))!;

        OrderDetails ready = Assert.Single(kitchen.Ready);
        Assert.Equal((1001, "Pat Sample", OrderStatus.Ready, 10.00m), (ready.OrderNumber, ready.CustomerName, ready.Status, ready.Total));
        Assert.NotNull(ready.ReadyAt);

        OrderDetails stillNew = Assert.Single(kitchen.New);
        Assert.Equal((1002, "Sam Example", OrderStatus.New, 36.00m), (stillNew.OrderNumber, stillNew.CustomerName, stillNew.Status, stillNew.Total));
        PricedPizza pizza = Assert.Single(stillNew.Pizzas);
        Assert.Equal(("Large (14\")", 2), (pizza.Size, pizza.Quantity));
        Assert.Equal(["Pepperoni", "Onions"], pizza.Toppings);

        // Numbering picks up where it left off rather than reusing a number.
        OrderDetails third = await PlaceAsync(reopened, Pizzas.Order(Pizzas.Custom()));
        Assert.Equal(1003, third.OrderNumber);
    }

    [Fact]
    public async Task Startup_IsRepeatable_AndDoesNotSeedTheMenuTwice()
    {
        await using (PizzaOrderingWebAppFactory firstStart = StartApp())
        {
            await firstStart.CreateClient().GetAsync("api/v1/health", Ct);
        }

        await using PizzaOrderingWebAppFactory secondStart = StartApp();
        MenuCatalog menu = (await secondStart.CreateClient().GetFromJsonAsync<MenuCatalog>("api/v1/menu", Ct))!;

        Assert.Equal((2, 4, 4), (menu.Sizes.Count, menu.Toppings.Count, menu.PresetPizzas.Count));
    }

    [Fact]
    public async Task Startup_CreatesTheSchemaThroughMigrations()
    {
        // A schema built by migrations can be upgraded in place by a later version. One built
        // any other way cannot, and upgrading would mean discarding the shop's orders.
        await using PizzaOrderingWebAppFactory app = StartApp();
        await app.CreateClient().GetAsync("api/v1/health", Ct);

        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        PizzaOrderingDbContext dbContext = scope.ServiceProvider.GetRequiredService<PizzaOrderingDbContext>();

        Assert.NotEmpty(await dbContext.Database.GetAppliedMigrationsAsync(Ct));
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync(Ct));
    }
}
