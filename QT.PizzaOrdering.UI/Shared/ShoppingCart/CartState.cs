using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.UI.Shared.ShoppingCart;

/// <summary>
/// The customer's cart for this browser session. Scoped, so the layout badge and every page share
/// one instance; components subscribe to <see cref="Changed"/> to re-render.
/// </summary>
public sealed class CartState(ICartStorage storage)
{
    private readonly List<CartItem> _items = [];
    private Task? _loadTask;

    public event Action? Changed;

    public IReadOnlyList<CartItem> Items => _items;
    public bool IsEmpty => _items.Count == 0;
    public bool IsFull => _items.Count >= OrderPricer.MaxPizzaLines;
    public int PizzaCount => _items.Sum(item => item.Pizza.Quantity);
    public decimal Subtotal => _items.Sum(item => item.Pizza.LineTotal);

    /// <summary>
    /// Restores the saved cart once. Safe to await from any number of components.
    /// </summary>
    public Task EnsureLoadedAsync() => _loadTask ??= LoadAsync();

    public async Task AddAsync(PizzaSelection selection, PricedPizza pizza)
    {
        await EnsureLoadedAsync();
        _items.Add(new CartItem(Guid.NewGuid(), selection, pizza));
        await SaveAsync();
    }

    public async Task ReplaceAsync(Guid itemId, PizzaSelection selection, PricedPizza pizza)
    {
        await EnsureLoadedAsync();
        int index = _items.FindIndex(item => item.Id == itemId);
        if (index < 0)
            return;

        _items[index] = new CartItem(itemId, selection, pizza);
        await SaveAsync();
    }

    public async Task SetQuantityAsync(Guid itemId, int quantity)
    {
        await EnsureLoadedAsync();
        int index = _items.FindIndex(item => item.Id == itemId);
        if (index < 0)
            return;

        quantity = Math.Clamp(quantity, 1, PizzaPricer.MaxQuantity);
        CartItem item = _items[index];
        _items[index] = item with
        {
            Selection = item.Selection with { Quantity = quantity },
            Pizza = item.Pizza with { Quantity = quantity, LineTotal = item.Pizza.UnitPrice * quantity }
        };
        await SaveAsync();
    }

    public async Task RemoveAsync(Guid itemId)
    {
        await EnsureLoadedAsync();
        if (_items.RemoveAll(item => item.Id == itemId) > 0)
            await SaveAsync();
    }

    public async Task ClearAsync()
    {
        await EnsureLoadedAsync();
        _items.Clear();
        await SaveAsync();
    }

    private async Task LoadAsync()
    {
        IReadOnlyList<CartItem>? saved = await storage.LoadAsync();
        if (saved is { Count: > 0 })
        {
            _items.AddRange(saved);
            Changed?.Invoke();
        }
    }

    private async Task SaveAsync()
    {
        Changed?.Invoke();
        await storage.SaveAsync(_items);
    }
}
