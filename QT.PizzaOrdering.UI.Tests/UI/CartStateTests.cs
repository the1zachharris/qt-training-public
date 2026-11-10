using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

namespace QT.PizzaOrdering.UI.Tests.UI;

public class CartStateTests
{
    private readonly FakeCartStorage _storage = new();

    // Large (14.00) + Pepperoni (2.00) = 16.00 each
    private static PizzaSelection LargePepperoni(int quantity = 1) => new(null, 2, [1], quantity);

    private static PricedPizza Priced(int quantity = 1, decimal unitPrice = 16.00m) =>
        new("Build Your Own", "Large (14\")", ["Pepperoni"], quantity, unitPrice, unitPrice * quantity);

    [Fact]
    public async Task AddAsync_AddsTheItemSavesItAndNotifies()
    {
        CartState cart = new(_storage);
        int notifications = 0;
        cart.Changed += () => notifications++;

        await cart.AddAsync(LargePepperoni(2), Priced(2));

        CartItem item = Assert.Single(cart.Items);
        Assert.Equal(2, cart.PizzaCount);
        Assert.Equal(32.00m, cart.Total);
        Assert.Equal(1, notifications);
        Assert.Equal([item], _storage.Saved);
    }

    [Fact]
    public async Task Total_IsTheSumOfEveryPizza_WithNothingAdded()
    {
        CartState cart = new(_storage);

        await cart.AddAsync(new PizzaSelection(null, 1, [], 1), new PricedPizza("Cheese Pizza", "Small (10\")", [], 1, 10.00m, 10.00m));
        await cart.AddAsync(LargePepperoni(), Priced(unitPrice: 18.00m));

        Assert.Equal(28.00m, cart.Total);
    }

    [Fact]
    public async Task EnsureLoadedAsync_RestoresTheSavedCartOnce()
    {
        _storage.Saved = [new CartItem(Guid.NewGuid(), LargePepperoni(), Priced())];
        CartState cart = new(_storage);

        await cart.EnsureLoadedAsync();
        await cart.EnsureLoadedAsync();

        Assert.Single(cart.Items);
        Assert.Equal(1, _storage.LoadCount);
    }

    [Fact]
    public async Task AddAsync_WhenCartWasSaved_KeepsTheSavedItems()
    {
        _storage.Saved = [new CartItem(Guid.NewGuid(), LargePepperoni(), Priced())];
        CartState cart = new(_storage);

        await cart.AddAsync(LargePepperoni(), Priced());

        Assert.Equal(2, cart.Items.Count);
    }

    [Fact]
    public async Task SetQuantityAsync_UpdatesSelectionAndLineTotal()
    {
        CartState cart = new(_storage);
        await cart.AddAsync(LargePepperoni(), Priced());
        Guid itemId = cart.Items[0].Id;

        await cart.SetQuantityAsync(itemId, 3);

        CartItem item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Selection.Quantity);
        Assert.Equal(3, item.Pizza.Quantity);
        Assert.Equal(48.00m, item.Pizza.LineTotal);
        Assert.Equal(48.00m, cart.Total);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(500, PizzaPricer.MaxQuantity)]
    public async Task SetQuantityAsync_ClampsToTheAllowedRange(int requested, int expected)
    {
        CartState cart = new(_storage);
        await cart.AddAsync(LargePepperoni(), Priced());

        await cart.SetQuantityAsync(cart.Items[0].Id, requested);

        Assert.Equal(expected, cart.Items[0].Pizza.Quantity);
    }

    [Fact]
    public async Task ReplaceAsync_SwapsTheItemInPlace()
    {
        CartState cart = new(_storage);
        await cart.AddAsync(LargePepperoni(), Priced());
        await cart.AddAsync(LargePepperoni(), Priced());
        Guid firstId = cart.Items[0].Id;

        await cart.ReplaceAsync(firstId, LargePepperoni(4), Priced(4, 20m));

        Assert.Equal(firstId, cart.Items[0].Id);
        Assert.Equal(80m, cart.Items[0].Pizza.LineTotal);
        Assert.Equal(2, cart.Items.Count);
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyThatItem_AndTheTotalDrops()
    {
        CartState cart = new(_storage);
        await cart.AddAsync(LargePepperoni(), Priced());
        await cart.AddAsync(LargePepperoni(2), Priced(2));
        Guid keptId = cart.Items[1].Id;

        await cart.RemoveAsync(cart.Items[0].Id);

        Assert.Equal(keptId, Assert.Single(cart.Items).Id);
        Assert.Equal(32.00m, cart.Total);
    }

    [Fact]
    public async Task ClearAsync_EmptiesTheCartAndTheSavedCopy()
    {
        CartState cart = new(_storage);
        await cart.AddAsync(LargePepperoni(), Priced());

        await cart.ClearAsync();

        Assert.True(cart.IsEmpty);
        Assert.Empty(_storage.Saved!);
    }

    [Fact]
    public async Task IsFull_WhenAtTheLineLimit_IsTrue()
    {
        CartState cart = new(_storage);
        for (int line = 0; line < OrderPricer.MaxPizzaLines; line++)
            await cart.AddAsync(LargePepperoni(), Priced());

        Assert.True(cart.IsFull);
    }

    [Fact]
    public void SavedCart_UsesAKeyThePassOneBuildNeverWrote()
    {
        // The first version of the app saved carts under "Cart" in a different shape. Reading
        // one of those into the current shape would show pizzas with no size or toppings.
        Assert.Equal("Cart.v2", ProtectedSessionCartStorage.StorageKey);
    }

    private sealed class FakeCartStorage : ICartStorage
    {
        public IReadOnlyList<CartItem>? Saved { get; set; }
        public int LoadCount { get; private set; }

        public Task<IReadOnlyList<CartItem>?> LoadAsync()
        {
            LoadCount++;
            return Task.FromResult(Saved);
        }

        public Task SaveAsync(IReadOnlyList<CartItem> items)
        {
            Saved = [.. items];
            return Task.CompletedTask;
        }
    }
}
