using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Shared.Validation;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Validators;

/// <summary>
/// AC-3.1: customers cannot submit an empty order or an order without providing their name.
/// </summary>
public class PlaceOrderRequestValidatorTests
{
    private readonly PlaceOrderRequestValidator _validator = new();

    private async Task<ValidationResult> ValidateAsync(PlaceOrderRequest request) =>
        await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ValidateAsync_NameAndOnePizza_IsValid()
    {
        ValidationResult result = await ValidateAsync(Pizzas.Order(Pizzas.Custom()));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_NameMissing_ReturnsError(string? name)
    {
        ValidationResult result = await ValidateAsync(Pizzas.Order(Pizzas.Custom()) with { CustomerName = name! });

        Assert.Contains(nameof(PlaceOrderRequest.CustomerName), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_NameTooLong_ReturnsError()
    {
        string name = new('x', PlaceOrderRequestValidator.MaxCustomerNameLength + 1);

        ValidationResult result = await ValidateAsync(Pizzas.Order(Pizzas.Custom()) with { CustomerName = name });

        Assert.Contains(nameof(PlaceOrderRequest.CustomerName), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_EmptyOrder_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.Order());

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_PizzasMissing_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.Order() with { Pizzas = null! });

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_TooManyPizzaLines_ReturnsError()
    {
        PizzaSelection[] pizzas = [.. Enumerable.Repeat(Pizzas.Custom(), OrderPricer.MaxPizzaLines + 1)];

        ValidationResult result = await ValidateAsync(Pizzas.Order(pizzas));

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }
}
