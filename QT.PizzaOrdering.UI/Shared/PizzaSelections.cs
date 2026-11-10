using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// Starting points for a pizza before the customer changes anything.
/// </summary>
public static class PizzaSelections
{
    /// <summary>A pizza needs a size; toppings are optional.</summary>
    public static bool CanOrderFrom(MenuCatalog menu) => menu.Sizes.Count > 0;

    /// <summary>One plain cheese pizza in the first size on the menu.</summary>
    public static PizzaSelection BuildYourOwn(MenuCatalog menu) =>
        new(null, menu.Sizes[0].Id, [], 1);

    public static PizzaSelection FromPreset(PresetPizza preset, int sizeId) =>
        new(preset.Id, sizeId, [.. preset.ToppingIds], 1);
}
