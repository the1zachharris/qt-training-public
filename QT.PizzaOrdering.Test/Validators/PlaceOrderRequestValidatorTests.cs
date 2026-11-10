using QT.PizzaOrdering.API.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Shared.Validation;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Validators;

public class PlaceOrderRequestValidatorTests
{
    private readonly PlaceOrderRequestValidator _validator = new();

    private async Task<ValidationResult> ValidateAsync(PlaceOrderRequest request) =>
        await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ValidateAsync_WhenPickupOrderIsComplete_IsValid()
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WhenDeliveryOrderIsComplete_IsValid()
    {
        ValidationResult result = await ValidateAsync(Pizzas.DeliveryOrder(Pizzas.MediumPepperoni()));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateAsync_WhenNameIsMissing_ReturnsError(string? name)
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { CustomerName = name! });

        Assert.Contains(nameof(PlaceOrderRequest.CustomerName), result.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("555-0100")]
    [InlineData("call me maybe")]
    [InlineData("918-555-0100 ext 4")]
    public async Task ValidateAsync_WhenPhoneIsNotAPhoneNumber_ReturnsError(string phone)
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { Phone = phone });

        Assert.Contains(nameof(PlaceOrderRequest.Phone), result.Errors.Keys);
    }

    [Theory]
    [InlineData("9185550100")]
    [InlineData("(918) 555-0100")]
    [InlineData("+1 918.555.0100")]
    public async Task ValidateAsync_WhenPhoneIsFormatted_IsValid(string phone)
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { Phone = phone });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WhenDeliveryHasNoAddress_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.DeliveryOrder(Pizzas.MediumPepperoni()) with { DeliveryAddress = " " });

        Assert.Contains(nameof(PlaceOrderRequest.DeliveryAddress), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WhenPickupHasNoAddress_IsValid()
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { DeliveryAddress = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WhenCartIsEmpty_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder());

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WhenCartIsMissing_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder() with { Pizzas = null! });

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WhenTooManyPizzaLines_ReturnsError()
    {
        PizzaSelection[] pizzas = [.. Enumerable.Repeat(Pizzas.MediumPepperoni(), OrderPricer.MaxPizzaLines + 1)];

        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(pizzas));

        Assert.Contains(nameof(PlaceOrderRequest.Pizzas), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WhenFulfillmentIsUnknown_ReturnsError()
    {
        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { Fulfillment = (FulfillmentType)42 });

        Assert.Contains(nameof(PlaceOrderRequest.Fulfillment), result.Errors.Keys);
    }

    [Fact]
    public async Task ValidateAsync_WhenNotesAreTooLong_ReturnsError()
    {
        string notes = new('x', PlaceOrderRequestValidator.MaxNotesLength + 1);

        ValidationResult result = await ValidateAsync(Pizzas.PickupOrder(Pizzas.MediumPepperoni()) with { Notes = notes });

        Assert.Contains(nameof(PlaceOrderRequest.Notes), result.Errors.Keys);
    }
}
