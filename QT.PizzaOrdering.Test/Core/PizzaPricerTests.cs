using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.Infrastructure.Seeding;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Core;

public class PizzaPricerTests
{
    private readonly MenuCatalog _menu = MenuSeed.Create();

    [Fact]
    public void Price_WhenNoToppings_ReturnsSizeBasePrice()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.BuildYourOwn());

        Assert.True(result.IsSuccess);
        Assert.Equal(12.99m, result.Value.UnitPrice);
        Assert.Equal(PizzaPricer.BuildYourOwnName, result.Value.Name);
    }

    [Fact]
    public void Price_WhenToppingsAndCrustUpcharge_AddsThemAtTheSizeToppingPrice()
    {
        // Large 15.99 + deep dish 2.00 + 3 toppings at 1.75
        PizzaSelection selection = Pizzas.BuildYourOwn(Pizzas.Large, Pizzas.DeepDish, toppingIds: [1, 7, 9]);

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection);

        Assert.Equal(23.24m, result.Value.UnitPrice);
        Assert.Equal(["Pepperoni", "Mushrooms", "Green Peppers"], result.Value.Toppings);
    }

    [Fact]
    public void Price_WhenExtraCheese_ChargesItAsOneTopping()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.BuildYourOwn(cheese: CheeseLevel.Extra));

        Assert.Equal(14.49m, result.Value.UnitPrice);
    }

    [Fact]
    public void Price_WhenQuantityAboveOne_MultipliesLineTotal()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.MediumPepperoni(quantity: 3));

        Assert.Equal(14.49m, result.Value.UnitPrice);
        Assert.Equal(43.47m, result.Value.LineTotal);
    }

    [Fact]
    public void Price_WhenPresetIsUnchanged_UsesPresetName()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.MediumPepperoni());

        Assert.Equal("Pepperoni", result.Value.Name);
    }

    [Fact]
    public void Price_WhenPresetIsCustomized_NamesItCustom()
    {
        PizzaSelection selection = Pizzas.MediumPepperoni() with { ToppingIds = [Pizzas.Pepperoni, 7] };

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection);

        Assert.Equal("Custom Pepperoni", result.Value.Name);
        Assert.Equal(15.99m, result.Value.UnitPrice);
    }

    [Fact]
    public void Price_WhenIdsAreNotOnTheMenu_ReturnsEveryFailure()
    {
        PizzaSelection selection = new(99, 98, 97, 96, CheeseLevel.Regular, [95], 1);

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection, "Pizzas[0]");

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Equal(
            ["Pizzas[0].CrustId", "Pizzas[0].PresetPizzaId", "Pizzas[0].SauceId", "Pizzas[0].SizeId", "Pizzas[0].ToppingIds"],
            validation.Failures.Keys.Order());
    }

    [Fact]
    public void Price_WhenToppingRepeats_Fails()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.BuildYourOwn(toppingIds: [1, 1]));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Price_WhenMoreThanMaxToppings_Fails()
    {
        int[] elevenToppings = [.. Enumerable.Range(1, PizzaPricer.MaxToppings + 1)];

        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.BuildYourOwn(toppingIds: elevenToppings));

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PizzaPricer.MaxQuantity + 1)]
    public void Price_WhenQuantityOutOfRange_Fails(int quantity)
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.BuildYourOwn(quantity: quantity));

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Contains("Pizza.Quantity", validation.Failures.Keys);
    }

    [Fact]
    public void Price_WhenToppingListIsMissing_TreatsItAsNoToppings()
    {
        PizzaSelection selection = Pizzas.BuildYourOwn() with { ToppingIds = null! };

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Toppings);
    }
}
