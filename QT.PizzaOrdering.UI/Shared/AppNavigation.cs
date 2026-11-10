using Microsoft.AspNetCore.Components.Routing;

namespace QT.PizzaOrdering.UI.Shared;

public sealed record NavMenuItem(string Href, string Text, string Icon, NavLinkMatch Match);

/// <summary>
/// The links offered in the navigation drawer. The kitchen screen is listed so it is easy to reach
/// while testing. It shows every customer's name and can change order statuses, so it should be
/// put behind staff sign-in, or taken out of this list, before real customers use the site.
/// </summary>
public static class AppNavigation
{
    public static IReadOnlyList<NavMenuItem> Items { get; } =
    [
        new("/", "Menu", Icons.Material.Filled.LocalPizza, NavLinkMatch.All),
        new("/cart", "Your Order", Icons.Material.Filled.ShoppingCart, NavLinkMatch.Prefix),
        new("/kitchen", "Kitchen", Icons.Material.Filled.SoupKitchen, NavLinkMatch.Prefix)
    ];
}
