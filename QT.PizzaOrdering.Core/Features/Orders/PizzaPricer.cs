using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.Core.Features.Orders;

/// <summary>
/// Resolves a <see cref="PizzaSelection"/> against the menu and prices it. The one place the
/// per-pizza rules live: the UI calls it to show prices, the API calls it to decide them.
/// </summary>
public static class PizzaPricer
{
    public const int MaxQuantity = 20;
    public const string CheesePizzaName = "Cheese Pizza";
    public const string BuildYourOwnName = "Build Your Own";

    /// <summary>
    /// Unit price = the size's price + the price of each topping. Toppings cost the same on every size.
    /// </summary>
    /// <param name="path">Prefix for failure keys, e.g. <c>Pizzas[2]</c>.</param>
    public static Result<PricedPizza> Price(MenuCatalog menu, PizzaSelection selection, string path = "Pizza")
    {
        // A request body can omit the list entirely; that is a plain cheese pizza.
        IReadOnlyList<int> toppingIds = selection.ToppingIds ?? [];
        Dictionary<string, string[]> failures = [];

        PizzaSize? size = menu.Sizes.FirstOrDefault(s => s.Id == selection.SizeId);
        PresetPizza? preset = selection.PresetPizzaId is int presetId
            ? menu.PresetPizzas.FirstOrDefault(p => p.Id == presetId)
            : null;
        // Menu order, not selection order, so the same pizza always reads the same way.
        List<Topping> toppings = [.. menu.Toppings.Where(t => toppingIds.Contains(t.Id))];

        if (size is null)
            failures[$"{path}.{nameof(PizzaSelection.SizeId)}"] = [$"Size '{selection.SizeId}' is not on the menu."];
        if (selection.PresetPizzaId is not null && preset is null)
            failures[$"{path}.{nameof(PizzaSelection.PresetPizzaId)}"] = [$"Pizza '{selection.PresetPizzaId}' is not on the menu."];
        if (selection.Quantity is < 1 or > MaxQuantity)
            failures[$"{path}.{nameof(PizzaSelection.Quantity)}"] = [$"Quantity must be between 1 and {MaxQuantity}."];

        string toppingsKey = $"{path}.{nameof(PizzaSelection.ToppingIds)}";
        if (toppingIds.Distinct().Count() != toppingIds.Count)
            failures[toppingsKey] = ["Each topping can only be added once."];
        else if (toppings.Count != toppingIds.Count)
            failures[toppingsKey] = ["One or more toppings are not on the menu."];

        if (failures.Count > 0 || size is null)
            return Error.Validation.Of(failures);

        decimal unitPrice = size.Price + toppings.Sum(topping => topping.Price);

        return new PricedPizza(
            DescribeName(preset, toppingIds),
            $"{size.Name} ({size.Inches}\")",
            [.. toppings.Select(t => t.Name)],
            selection.Quantity,
            unitPrice,
            unitPrice * selection.Quantity);
    }

    private static string DescribeName(PresetPizza? preset, IReadOnlyList<int> toppingIds)
    {
        if (preset is null)
            return toppingIds.Count == 0 ? CheesePizzaName : BuildYourOwnName;

        return preset.ToppingIds.ToHashSet().SetEquals(toppingIds) ? preset.Name : $"Custom {preset.Name}";
    }
}
