using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Requests;

namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// Starting points for a pizza before the customer changes anything.
/// </summary>
public static class PizzaSelections
{
    /// <summary>A menu a pizza can be built from has at least one size, crust, and sauce.</summary>
    public static bool CanOrderFrom(MenuCatalog menu) =>
        menu.Sizes.Count > 0 && menu.Crusts.Count > 0 && menu.Sauces.Count > 0;

    /// <summary>The second-smallest size (Medium on the seeded menu), or the only size there is.</summary>
    public static int DefaultSizeId(MenuCatalog menu) =>
        menu.Sizes[Math.Min(1, menu.Sizes.Count - 1)].Id;

    public static PizzaSelection BuildYourOwn(MenuCatalog menu) =>
        new(null, DefaultSizeId(menu), menu.Crusts[0].Id, menu.Sauces[0].Id, CheeseLevel.Regular, [], 1);

    public static PizzaSelection FromPreset(PresetPizza preset, int sizeId) =>
        new(preset.Id, sizeId, preset.CrustId, preset.SauceId, preset.Cheese, [.. preset.ToppingIds], 1);
}
