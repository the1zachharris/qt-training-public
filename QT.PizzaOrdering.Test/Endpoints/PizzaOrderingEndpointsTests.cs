using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

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
    public async Task GetMenu_ReturnsTheShopsMenu()
    {
        MenuCatalog? menu = await _client.GetFromJsonAsync<MenuCatalog>("api/v1/menu", TestContext.Current.CancellationToken);

        Assert.NotNull(menu);
        Assert.Equal([("Small", 10, 10.00m), ("Large", 14, 14.00m)], menu.Sizes.Select(size => (size.Name, size.Inches, size.Price)));
        Assert.Equal(["Pepperoni", "Mushrooms", "Onions", "Green peppers"], menu.Toppings.Select(topping => topping.Name));
        Assert.All(menu.Toppings, topping => Assert.Equal(2.00m, topping.Price));
        Assert.Equal(4, menu.PresetPizzas.Count);
        Assert.Equal([2, 3, 4], menu.PresetPizzas.Single(preset => preset.Name == "Veggie").ToppingIds);
    }

    [Fact]
    public async Task PlaceOrder_WithOnlyANameAndOnePizza_ReturnsANewOrder()
    {
        // The request body a customer's browser sends: a name and the pizzas, nothing else.
        var body = new { customerName = "Pat Sample", pizzas = new[] { new { sizeId = Pizzas.Small, toppingIds = new[] { Pizzas.Pepperoni }, quantity = 1 } } };

        HttpResponseMessage response = await _client.PostAsJsonAsync("api/v1/orders", body, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("New", json.GetProperty("status").GetString());
        Assert.Equal(12.00m, json.GetProperty("total").GetDecimal());
        Assert.Equal("Pat Sample", json.GetProperty("customerName").GetString());
    }

    [Fact]
    public async Task PlaceOrder_ThenGetOrder_ReturnsTheSavedOrder()
    {
        PlaceOrderRequest request = Pizzas.Order(
            Pizzas.Custom(Pizzas.Small, quantity: 2),
            Pizzas.Custom(Pizzas.Large, toppingIds: [Pizzas.Mushrooms, Pizzas.GreenPeppers]));

        HttpResponseMessage placed = await _client.PostAsJsonAsync("api/v1/orders", request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, placed.StatusCode);
        OrderDetails? order = await placed.Content.ReadFromJsonAsync<OrderDetails>(TestContext.Current.CancellationToken);

        OrderDetails? fetched = await _client.GetFromJsonAsync<OrderDetails>($"api/v1/orders/{order!.Id}", TestContext.Current.CancellationToken);

        Assert.NotNull(fetched);
        Assert.Equal(order.OrderNumber, fetched.OrderNumber);
        Assert.Equal("Pat Sample", fetched.CustomerName);
        Assert.Equal(OrderStatus.New, fetched.Status);
        Assert.Equal(2, fetched.Pizzas.Count);
        Assert.Equal(["Mushrooms", "Green peppers"], fetched.Pizzas[1].Toppings);
        // 2 x 10.00 + (14.00 + 2 x 2.00) = 38.00
        Assert.Equal(38.00m, fetched.Total);
    }

    [Theory]
    [InlineData("""{ "customerName": "", "pizzas": [ { "sizeId": 1, "toppingIds": [], "quantity": 1 } ] }""")]
    [InlineData("""{ "pizzas": [ { "sizeId": 1, "toppingIds": [], "quantity": 1 } ] }""")]
    [InlineData("""{ "customerName": "Pat Sample", "pizzas": [] }""")]
    [InlineData("""{ "customerName": "Pat Sample" }""")]
    public async Task PlaceOrder_WithoutANameOrWithoutPizzas_ReturnsBadRequest(string json)
    {
        using StringContent content = new(json, System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync("api/v1/orders", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.GetAsync($"api/v1/orders/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
