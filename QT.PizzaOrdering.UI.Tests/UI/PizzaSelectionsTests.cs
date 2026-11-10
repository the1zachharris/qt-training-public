using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.UI.Shared;

namespace QT.PizzaOrdering.UI.Tests.UI;

public class PizzaSelectionsTests
{
    private static readonly MenuCatalog Menu = new(
        [new PizzaSize(1, "Small", 10, 10.00m), new PizzaSize(2, "Large", 14, 14.00m)],
        [new Topping(1, "Pepperoni", 2.00m), new Topping(2, "Mushrooms", 2.00m)],
        [new PresetPizza(7, "Both", "Pepperoni and mushrooms.", [1, 2])]);

    [Fact]
    public void BuildYourOwn_StartsAsOnePlainPizzaInTheFirstSize()
    {
        PizzaSelection selection = PizzaSelections.BuildYourOwn(Menu);

        Assert.Equal(new { SizeId = 1, Quantity = 1 }, new { selection.SizeId, selection.Quantity });
        Assert.Null(selection.PresetPizzaId);
        Assert.Empty(selection.ToppingIds);
    }

    [Fact]
    public void FromPreset_CopiesThePresetsToppingsInTheChosenSize()
    {
        PizzaSelection selection = PizzaSelections.FromPreset(Menu.PresetPizzas[0], sizeId: 2);

        Assert.Equal(7, selection.PresetPizzaId);
        Assert.Equal(2, selection.SizeId);
        Assert.Equal([1, 2], selection.ToppingIds);
        Assert.Equal(1, selection.Quantity);
    }

    [Fact]
    public void CanOrderFrom_NeedsAtLeastOneSize()
    {
        Assert.True(PizzaSelections.CanOrderFrom(Menu));
        Assert.False(PizzaSelections.CanOrderFrom(Menu with { Sizes = [] }));
    }

    [Fact]
    public void Toppings_ReadsPlainCheeseWhenThereAreNone()
    {
        PricedPizza plain = new("Cheese Pizza", "Small (10\")", [], 1, 10.00m, 10.00m);
        PricedPizza topped = plain with { Toppings = ["Pepperoni", "Mushrooms"] };

        Assert.Equal("Plain cheese", PizzaText.Toppings(plain));
        Assert.Equal("Pepperoni, Mushrooms", PizzaText.Toppings(topped));
    }

    [Fact]
    public void ToCurrency_AlwaysFormatsAsUsDollars()
    {
        Assert.Equal("$14.00", 14m.ToCurrency());
        Assert.Equal("#1001", PizzaText.OrderNumber(1001));
    }
}
