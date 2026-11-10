using System.Net;
using System.Net.Http.Json;

using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Endpoints;

/// <summary>
/// The kitchen's view of orders, end to end. Each test gets its own database so order numbers start at 1001.
/// </summary>
public class KitchenEndpointsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<OrderDetails> PlaceAsync(HttpClient client, PlaceOrderRequest request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("api/v1/orders", request, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDetails>(Ct))!;
    }

    private static async Task<KitchenOrders> GetKitchenAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<KitchenOrders>("api/v1/kitchen/orders", Ct))!;

    private static Task<HttpResponseMessage> MarkReadyAsync(HttpClient client, Guid orderId) =>
        client.PatchAsync($"api/v1/kitchen/orders/{orderId}/ready", content: null, Ct);

    [Fact]
    public async Task PlacedOrder_AppearsUnderNew_WithNameNumberPizzasAndTotal()
    {
        await using PizzaOrderingWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        await PlaceAsync(client, Pizzas.Order(Pizzas.Custom(Pizzas.Large, quantity: 2, toppingIds: [Pizzas.Pepperoni, Pizzas.Mushrooms])));

        KitchenOrders kitchen = await GetKitchenAsync(client);

        Assert.Empty(kitchen.Ready);
        OrderDetails order = Assert.Single(kitchen.New);
        Assert.Equal(1001, order.OrderNumber);
        Assert.Equal("Pat Sample", order.CustomerName);
        Assert.Equal(OrderStatus.New, order.Status);
        PricedPizza pizza = Assert.Single(order.Pizzas);
        Assert.Equal("Large (14\")", pizza.Size);
        Assert.Equal(["Pepperoni", "Mushrooms"], pizza.Toppings);
        Assert.Equal(2, pizza.Quantity);
        Assert.Equal(36.00m, order.Total);
    }

    [Fact]
    public async Task MarkReady_MovesTheOrderFromNewToReady()
    {
        await using PizzaOrderingWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        OrderDetails placed = await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));

        HttpResponseMessage response = await MarkReadyAsync(client, placed.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        OrderDetails marked = (await response.Content.ReadFromJsonAsync<OrderDetails>(Ct))!;
        Assert.Equal(OrderStatus.Ready, marked.Status);
        Assert.NotNull(marked.ReadyAt);

        KitchenOrders kitchen = await GetKitchenAsync(client);
        Assert.Empty(kitchen.New);
        Assert.Equal(placed.Id, Assert.Single(kitchen.Ready).Id);
    }

    [Fact]
    public async Task MarkReady_Twice_StillSucceeds()
    {
        await using PizzaOrderingWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        OrderDetails placed = await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));
        await MarkReadyAsync(client, placed.Id);

        HttpResponseMessage second = await MarkReadyAsync(client, placed.Id);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Single((await GetKitchenAsync(client)).Ready);
    }

    [Fact]
    public async Task NewOrders_AreOldestFirst_ReadyOrders_AreNewestFirst()
    {
        await using PizzaOrderingWebAppFactory factory = new();
        HttpClient client = factory.CreateClient();
        OrderDetails first = await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));
        OrderDetails second = await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));
        await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));
        await PlaceAsync(client, Pizzas.Order(Pizzas.Custom()));
        await MarkReadyAsync(client, first.Id);
        await MarkReadyAsync(client, second.Id);

        KitchenOrders kitchen = await GetKitchenAsync(client);

        Assert.Equal([1003, 1004], kitchen.New.Select(order => order.OrderNumber));
        Assert.Equal([1002, 1001], kitchen.Ready.Select(order => order.OrderNumber));
    }

    [Fact]
    public async Task MarkReady_UnknownOrder_Returns404()
    {
        await using PizzaOrderingWebAppFactory factory = new();

        HttpResponseMessage response = await MarkReadyAsync(factory.CreateClient(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OrdersPlacedAtTheSameTime_GetDifferentNumbers()
    {
        // A file database, so every request runs on its own connection the way it does in production.
        using TempDatabase database = new();
        await using PizzaOrderingWebAppFactory factory = new() { DatabasePath = database.Path };
        HttpClient client = factory.CreateClient();

        OrderDetails[] placed = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => PlaceAsync(client, Pizzas.Order(Pizzas.Custom()))));

        Assert.Equal(Enumerable.Range(1001, 20), placed.Select(order => order.OrderNumber).Order());
        Assert.Equal(20, (await GetKitchenAsync(client)).New.Count);
    }
}
