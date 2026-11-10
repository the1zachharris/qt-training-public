using System.Net;
using System.Net.Http.Json;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Endpoints;

/// <summary>
/// End-to-end through the real pipeline: routing, JSON, handlers, EF Core, and the seeded SQLite schema.
/// </summary>
public class PizzaOrderingEndpointsTests : IClassFixture<PizzaOrderingWebAppFactory>
{
    private readonly HttpClient _client;

    public PizzaOrderingEndpointsTests(PizzaOrderingWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMenu_ReturnsTheSeededMenu()
    {
        MenuCatalog? menu = await _client.GetFromJsonAsync<MenuCatalog>("api/v1/menu", TestContext.Current.CancellationToken);

        Assert.NotNull(menu);
        Assert.Equal(4, menu.Sizes.Count);
        Assert.Equal(4, menu.Crusts.Count);
        Assert.Equal(4, menu.Sauces.Count);
        Assert.Equal(14, menu.Toppings.Count);
        Assert.Equal(9, menu.PresetPizzas.Count);
        Assert.Equal([1, 2, 3, 4, 6], menu.PresetPizzas.Single(preset => preset.Name == "Meat Lovers").ToppingIds);
    }

    [Fact]
    public async Task QuoteOrder_PricesTheCart()
    {
        QuoteOrderRequest request = new(FulfillmentType.Delivery, [Pizzas.MediumPepperoni(2)]);

        HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/orders/quote", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        OrderQuote? quote = await response.Content.ReadFromJsonAsync<OrderQuote>(TestContext.Current.CancellationToken);
        Assert.Equal(35.43m, quote!.Total);
    }

    [Fact]
    public async Task PlaceOrder_ThenGetOrder_ReturnsTheSavedOrder()
    {
        PlaceOrderRequest request = Pizzas.DeliveryOrder(Pizzas.MediumPepperoni(2), Pizzas.BuildYourOwn(Pizzas.Large, Pizzas.DeepDish, toppingIds: [7, 9]));

        HttpResponseMessage placed = await _client.PostAsJsonAsync("api/v1/orders", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, placed.StatusCode);
        OrderConfirmation? confirmation = await placed.Content.ReadFromJsonAsync<OrderConfirmation>(TestContext.Current.CancellationToken);

        OrderConfirmation? fetched = await _client.GetFromJsonAsync<OrderConfirmation>($"api/v1/orders/{confirmation!.Id}", TestContext.Current.CancellationToken);

        Assert.NotNull(fetched);
        Assert.Equal(confirmation.OrderNumber, fetched.OrderNumber);
        Assert.Equal(FulfillmentType.Delivery, fetched.Fulfillment);
        Assert.Equal("100 Example St", fetched.DeliveryAddress);
        Assert.Equal(2, fetched.Pizzas.Count);
        Assert.Equal(["Mushrooms", "Green Peppers"], fetched.Pizzas[1].Toppings);
        // 28.98 + (15.99 + 2.00 + 3.50) = 50.47; tax 4.29; delivery 3.99
        Assert.Equal(50.47m, fetched.Subtotal);
        Assert.Equal(58.75m, fetched.Total);
    }

    [Fact]
    public async Task PlaceOrder_WhenInvalid_ReturnsBadRequest()
    {
        PlaceOrderRequest request = Pizzas.PickupOrder() with { CustomerName = "" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/orders", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"api/v1/orders/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
