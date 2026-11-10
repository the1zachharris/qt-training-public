namespace QT.PizzaOrdering.UI.Shared.ShoppingCart;

/// <summary>
/// Where the cart is kept between page loads.
/// </summary>
public interface ICartStorage
{
    /// <summary>The saved cart, or null when nothing usable is stored.</summary>
    Task<IReadOnlyList<CartItem>?> LoadAsync();

    Task SaveAsync(IReadOnlyList<CartItem> items);
}
