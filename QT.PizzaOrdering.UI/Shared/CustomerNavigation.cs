using Microsoft.AspNetCore.Components.Routing;

namespace QT.PizzaOrdering.UI.Shared;

public sealed record NavMenuItem(string Href, string Text, string Icon, NavLinkMatch Match);

/// <summary>
/// The links offered in the navigation drawer. The kitchen screen is deliberately absent: it
/// shows every customer's name and can change order statuses, so staff reach it by its address
/// (<c>/kitchen</c>) rather than customers finding it in the menu.
/// </summary>
public static class CustomerNavigation
{
    public static IReadOnlyList<NavMenuItem> Items { get; } =
    [
        new("/", "Menu", Icons.Material.Filled.LocalPizza, NavLinkMatch.All),
        new("/cart", "Your Order", Icons.Material.Filled.ShoppingCart, NavLinkMatch.Prefix)
    ];
}
