using QT.PizzaOrdering.Core.Features.Menu.Enums;

namespace QT.PizzaOrdering.Core.Features.Menu.Models;

/// <summary>
/// Everything a customer can order from, read in one piece. Sent to the UI as-is and used by
/// the pricer on both sides.
/// </summary>
public sealed record MenuCatalog(
    IReadOnlyList<PizzaSize> Sizes,
    IReadOnlyList<Crust> Crusts,
    IReadOnlyList<Sauce> Sauces,
    IReadOnlyList<Topping> Toppings,
    IReadOnlyList<PresetPizza> PresetPizzas);

/// <summary>
/// A pizza size. The size sets both the base price and what each topping costs on that size.
/// </summary>
public sealed record PizzaSize(int Id, string Name, int Inches, decimal BasePrice, decimal ToppingPrice);

public sealed record Crust(int Id, string Name, decimal Upcharge);

public sealed record Sauce(int Id, string Name);

public sealed record Topping(int Id, string Name, ToppingCategory Category);

/// <summary>
/// A ready-made pizza on the menu. A preset has no price of its own: it is priced from its
/// crust and toppings exactly like a custom pizza, so customizing one never changes the rules.
/// </summary>
public sealed record PresetPizza(
    int Id,
    string Name,
    string Description,
    int CrustId,
    int SauceId,
    CheeseLevel Cheese,
    List<int> ToppingIds);
