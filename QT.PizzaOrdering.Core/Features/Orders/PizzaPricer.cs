using QT.PizzaOrdering.Core.Features.Menu.Enums;
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
    public const int MaxToppings = 10;
    public const int MaxQuantity = 20;
    public const string BuildYourOwnName = "Build Your Own";

    /// <summary>
    /// Unit price = size base price + crust upcharge + the size's topping price for each topping,
    /// with extra cheese counting as one more topping.
    /// </summary>
    /// <param name="path">Prefix for failure keys, e.g. <c>Pizzas[2]</c>.</param>
    public static Result<PricedPizza> Price(MenuCatalog menu, PizzaSelection selection, string path = "Pizza")
    {
        // A request body can omit the list entirely.
        IReadOnlyList<int> toppingIds = selection.ToppingIds ?? [];
        Dictionary<string, string[]> failures = [];

        PizzaSize? size = menu.Sizes.FirstOrDefault(s => s.Id == selection.SizeId);
        Crust? crust = menu.Crusts.FirstOrDefault(c => c.Id == selection.CrustId);
        Sauce? sauce = menu.Sauces.FirstOrDefault(s => s.Id == selection.SauceId);
        PresetPizza? preset = selection.PresetPizzaId is int presetId
            ? menu.PresetPizzas.FirstOrDefault(p => p.Id == presetId)
            : null;
        // Menu order, not selection order, so the same pizza always reads the same way.
        List<Topping> toppings = [.. menu.Toppings.Where(t => toppingIds.Contains(t.Id))];

        if (size is null)
            failures[$"{path}.{nameof(PizzaSelection.SizeId)}"] = [$"Size '{selection.SizeId}' is not on the menu."];
        if (crust is null)
            failures[$"{path}.{nameof(PizzaSelection.CrustId)}"] = [$"Crust '{selection.CrustId}' is not on the menu."];
        if (sauce is null)
            failures[$"{path}.{nameof(PizzaSelection.SauceId)}"] = [$"Sauce '{selection.SauceId}' is not on the menu."];
        if (selection.PresetPizzaId is not null && preset is null)
            failures[$"{path}.{nameof(PizzaSelection.PresetPizzaId)}"] = [$"Pizza '{selection.PresetPizzaId}' is not on the menu."];
        if (!Enum.IsDefined(selection.Cheese))
            failures[$"{path}.{nameof(PizzaSelection.Cheese)}"] = ["Cheese amount is not valid."];
        if (selection.Quantity is < 1 or > MaxQuantity)
            failures[$"{path}.{nameof(PizzaSelection.Quantity)}"] = [$"Quantity must be between 1 and {MaxQuantity}."];

        string toppingsKey = $"{path}.{nameof(PizzaSelection.ToppingIds)}";
        if (toppingIds.Count > MaxToppings)
            failures[toppingsKey] = [$"A pizza can have at most {MaxToppings} toppings."];
        else if (toppingIds.Distinct().Count() != toppingIds.Count)
            failures[toppingsKey] = ["Each topping can only be added once."];
        else if (toppings.Count != toppingIds.Count)
            failures[toppingsKey] = ["One or more toppings are not on the menu."];

        if (failures.Count > 0 || size is null || crust is null || sauce is null)
            return Error.Validation.Of(failures);

        int pricedToppings = toppings.Count + (selection.Cheese == CheeseLevel.Extra ? 1 : 0);
        decimal unitPrice = size.BasePrice + crust.Upcharge + (size.ToppingPrice * pricedToppings);

        return new PricedPizza(
            DescribeName(preset, selection, toppingIds),
            $"{size.Name} ({size.Inches}\")",
            crust.Name,
            sauce.Name,
            selection.Cheese,
            [.. toppings.Select(t => t.Name)],
            selection.Quantity,
            unitPrice,
            unitPrice * selection.Quantity);
    }

    private static string DescribeName(PresetPizza? preset, PizzaSelection selection, IReadOnlyList<int> toppingIds)
    {
        if (preset is null)
            return BuildYourOwnName;

        bool unchanged = preset.CrustId == selection.CrustId
            && preset.SauceId == selection.SauceId
            && preset.Cheese == selection.Cheese
            && preset.ToppingIds.ToHashSet().SetEquals(toppingIds);

        return unchanged ? preset.Name : $"Custom {preset.Name}";
    }
}
