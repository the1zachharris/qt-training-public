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

    private decimal UnitPrice(PizzaSelection selection) => PizzaPricer.Price(_menu, selection).Value.UnitPrice;

    [Fact]
    public void Price_SmallPlain_Is10()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Custom(Pizzas.Small));

        Assert.Equal(10.00m, result.Value.UnitPrice);
        Assert.Equal(PizzaPricer.CheesePizzaName, result.Value.Name);
        Assert.Equal("Small (10\")", result.Value.Size);
        Assert.Empty(result.Value.Toppings);
    }

    [Fact]
    public void Price_LargePlain_Is14()
    {
        Assert.Equal(14.00m, UnitPrice(Pizzas.Custom(Pizzas.Large)));
    }

    [Theory]
    [InlineData(Pizzas.Small, 1, 12.00)]
    [InlineData(Pizzas.Large, 1, 16.00)]
    [InlineData(Pizzas.Small, 4, 18.00)]
    [InlineData(Pizzas.Large, 4, 22.00)]
    public void Price_EachToppingAdds2_OnEitherSize(int sizeId, int toppingCount, decimal expected)
    {
        PizzaSelection selection = Pizzas.Custom(sizeId, toppingIds: Pizzas.AllToppings[..toppingCount]);

        Assert.Equal(expected, UnitPrice(selection));
    }

    [Fact]
    public void Price_RemovingATopping_Drops2()
    {
        decimal two = UnitPrice(Pizzas.Custom(Pizzas.Large, toppingIds: [Pizzas.Pepperoni, Pizzas.Mushrooms]));
        decimal one = UnitPrice(Pizzas.Custom(Pizzas.Large, toppingIds: [Pizzas.Pepperoni]));

        Assert.Equal(2.00m, two - one);
    }

    [Fact]
    public void Price_Quantity3LargePepperoni_LineTotalIs48()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Custom(Pizzas.Large, quantity: 3, toppingIds: [Pizzas.Pepperoni]));

        Assert.Equal(16.00m, result.Value.UnitPrice);
        Assert.Equal(48.00m, result.Value.LineTotal);
        Assert.Equal(PizzaPricer.BuildYourOwnName, result.Value.Name);
    }

    [Fact]
    public void Price_ListsToppingsInMenuOrder()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Custom(toppingIds: [Pizzas.GreenPeppers, Pizzas.Pepperoni]));

        Assert.Equal(["Pepperoni", "Green peppers"], result.Value.Toppings);
    }

    [Fact]
    public void Price_PresetUnchanged_UsesPresetName()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Veggie());

        Assert.Equal("Veggie", result.Value.Name);
        Assert.Equal(16.00m, result.Value.UnitPrice);
    }

    [Fact]
    public void Price_PresetCustomized_NamesItCustom()
    {
        PizzaSelection selection = Pizzas.Veggie() with { ToppingIds = [Pizzas.Mushrooms] };

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection);

        Assert.Equal("Custom Veggie", result.Value.Name);
    }

    [Fact]
    public void Price_UnknownSizeToppingAndPreset_ReturnsEveryFailure()
    {
        PizzaSelection selection = new(99, 98, [97], 1);

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection, "Pizzas[0]");

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Equal(
            ["Pizzas[0].PresetPizzaId", "Pizzas[0].SizeId", "Pizzas[0].ToppingIds"],
            validation.Failures.Keys.Order());
    }

    [Fact]
    public void Price_RepeatedTopping_Fails()
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Custom(toppingIds: [Pizzas.Pepperoni, Pizzas.Pepperoni]));

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(PizzaPricer.MaxQuantity + 1)]
    public void Price_QuantityOutOfRange_Fails(int quantity)
    {
        Result<PricedPizza> result = PizzaPricer.Price(_menu, Pizzas.Custom(quantity: quantity));

        Error.Validation validation = Assert.IsType<Error.Validation>(result.Error);
        Assert.Contains("Pizza.Quantity", validation.Failures.Keys);
    }

    [Fact]
    public void Price_ToppingListMissing_TreatsItAsPlain()
    {
        PizzaSelection selection = Pizzas.Custom() with { ToppingIds = null! };

        Result<PricedPizza> result = PizzaPricer.Price(_menu, selection);

        Assert.Equal(10.00m, result.Value.UnitPrice);
    }
}
