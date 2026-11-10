using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Menu.Models;

namespace QT.PizzaOrdering.Infrastructure.Seeding;

/// <summary>
/// The starting menu. Ids are fixed because preset pizzas refer to crusts, sauces, and toppings by id.
/// </summary>
public static class MenuSeed
{
    public static MenuCatalog Create() => new(
        Sizes:
        [
            new PizzaSize(1, "Small", 10, 9.99m, 1.25m),
            new PizzaSize(2, "Medium", 12, 12.99m, 1.50m),
            new PizzaSize(3, "Large", 14, 15.99m, 1.75m),
            new PizzaSize(4, "Extra Large", 16, 18.99m, 2.00m)
        ],
        Crusts:
        [
            new Crust(1, "Hand Tossed", 0m),
            new Crust(2, "Thin & Crispy", 0m),
            new Crust(3, "Deep Dish", 2.00m),
            new Crust(4, "Gluten-Free", 3.00m)
        ],
        Sauces:
        [
            new Sauce(1, "Classic Marinara"),
            new Sauce(2, "Creamy Alfredo"),
            new Sauce(3, "Smoky BBQ"),
            new Sauce(4, "Garlic Olive Oil")
        ],
        Toppings:
        [
            new Topping(1, "Pepperoni", ToppingCategory.Meat),
            new Topping(2, "Italian Sausage", ToppingCategory.Meat),
            new Topping(3, "Bacon", ToppingCategory.Meat),
            new Topping(4, "Ham", ToppingCategory.Meat),
            new Topping(5, "Grilled Chicken", ToppingCategory.Meat),
            new Topping(6, "Beef", ToppingCategory.Meat),
            new Topping(7, "Mushrooms", ToppingCategory.Veggie),
            new Topping(8, "Red Onions", ToppingCategory.Veggie),
            new Topping(9, "Green Peppers", ToppingCategory.Veggie),
            new Topping(10, "Black Olives", ToppingCategory.Veggie),
            new Topping(11, "Tomatoes", ToppingCategory.Veggie),
            new Topping(12, "Jalapeños", ToppingCategory.Veggie),
            new Topping(13, "Pineapple", ToppingCategory.Veggie),
            new Topping(14, "Spinach", ToppingCategory.Veggie)
        ],
        PresetPizzas:
        [
            new PresetPizza(1, "Classic Cheese", "Marinara and plenty of mozzarella. Nothing else needed.", 1, 1, CheeseLevel.Extra, []),
            new PresetPizza(2, "Pepperoni", "The one everybody agrees on.", 1, 1, CheeseLevel.Regular, [1]),
            new PresetPizza(3, "Meat Lovers", "Pepperoni, sausage, bacon, ham, and beef.", 1, 1, CheeseLevel.Regular, [1, 2, 3, 4, 6]),
            new PresetPizza(4, "Supreme", "Pepperoni and sausage with mushrooms, onions, peppers, and olives.", 1, 1, CheeseLevel.Regular, [1, 2, 7, 8, 9, 10]),
            new PresetPizza(5, "Garden Veggie", "Mushrooms, onions, peppers, olives, tomatoes, and spinach.", 1, 1, CheeseLevel.Regular, [7, 8, 9, 10, 11, 14]),
            new PresetPizza(6, "Hawaiian", "Ham and pineapple. Sweet, salty, and still up for debate.", 1, 1, CheeseLevel.Regular, [4, 13]),
            new PresetPizza(7, "BBQ Chicken", "Grilled chicken, bacon, and red onions on smoky BBQ sauce.", 1, 3, CheeseLevel.Regular, [3, 5, 8]),
            new PresetPizza(8, "Chicken Alfredo", "Grilled chicken, mushrooms, and spinach on creamy Alfredo.", 1, 2, CheeseLevel.Regular, [5, 7, 14]),
            new PresetPizza(9, "Margherita", "Thin crust, garlic olive oil, tomatoes, and spinach.", 2, 4, CheeseLevel.Light, [11, 14])
        ]);
}
