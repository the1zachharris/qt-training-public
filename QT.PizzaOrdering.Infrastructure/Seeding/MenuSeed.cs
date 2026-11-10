using QT.PizzaOrdering.Core.Features.Menu.Models;

namespace QT.PizzaOrdering.Infrastructure.Seeding;

/// <summary>
/// The shop's menu, as given in requirements.md. Ids are fixed because preset pizzas refer to
/// toppings by id.
/// </summary>
public static class MenuSeed
{
    private const decimal ToppingPrice = 2.00m;

    public static MenuCatalog Create() => new(
        Sizes:
        [
            new PizzaSize(1, "Small", 10, 10.00m),
            new PizzaSize(2, "Large", 14, 14.00m)
        ],
        Toppings:
        [
            new Topping(1, "Pepperoni", ToppingPrice),
            new Topping(2, "Mushrooms", ToppingPrice),
            new Topping(3, "Onions", ToppingPrice),
            new Topping(4, "Green peppers", ToppingPrice)
        ],
        PresetPizzas:
        [
            new PresetPizza(1, "Cheese", "Just sauce and cheese.", []),
            new PresetPizza(2, "Pepperoni", "The one everybody agrees on.", [1]),
            new PresetPizza(3, "Veggie", "Mushrooms, onions, and green peppers.", [2, 3, 4]),
            new PresetPizza(4, "The Works", "Every topping we have.", [1, 2, 3, 4])
        ]);
}
