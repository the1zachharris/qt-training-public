namespace QT.PizzaOrdering.Core.Features.Menu.Models;

/// <summary>
/// Everything a customer can order from, read in one piece. Sent to the UI as-is and used by
/// the pricer on both sides.
/// </summary>
public sealed record MenuCatalog(
    IReadOnlyList<PizzaSize> Sizes,
    IReadOnlyList<Topping> Toppings,
    IReadOnlyList<PresetPizza> PresetPizzas);

/// <summary>
/// A pizza size and the price of a plain cheese pizza in that size.
/// </summary>
public sealed record PizzaSize(int Id, string Name, int Inches, decimal Price);

/// <summary>
/// A topping and what it adds to one pizza. The price is the same on every size.
/// </summary>
public sealed record Topping(int Id, string Name, decimal Price);

/// <summary>
/// A named combination of toppings offered as a shortcut. A preset has no price of its own: it
/// is priced from its toppings exactly like a pizza built from scratch.
/// </summary>
public sealed record PresetPizza(int Id, string Name, string Description, List<int> ToppingIds);
