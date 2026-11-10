using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

namespace QT.PizzaOrdering.UI.Tests.UI;

/// <summary>
/// The real storage class over a fake browser session store, so what is tested is what a
/// customer's browser can actually hand back: nothing, a cart from an older build, or junk.
/// </summary>
public class ProtectedSessionCartStorageTests
{
    private readonly FakeBrowserSessionStorage _browser = new();
    private readonly ProtectedSessionCartStorage _storage;

    public ProtectedSessionCartStorageTests()
    {
        _storage = new ProtectedSessionCartStorage(
            NullLogger<ProtectedSessionCartStorage>.Instance,
            new ProtectedSessionStorage(_browser, new EphemeralDataProtectionProvider()));
    }

    private static CartItem Item() => new(
        Guid.NewGuid(),
        new PizzaSelection(null, 2, [1], 2),
        new PricedPizza("Build Your Own", "Large (14\")", ["Pepperoni"], 2, 16.00m, 32.00m));

    [Fact]
    public async Task SaveThenLoad_ReturnsTheSameCart()
    {
        CartItem item = Item();

        await _storage.SaveAsync([item]);
        IReadOnlyList<CartItem>? loaded = await _storage.LoadAsync();

        CartItem restored = Assert.Single(loaded!);
        Assert.Equal(item.Id, restored.Id);
        Assert.Equal((2, 2), (restored.Selection.SizeId, restored.Selection.Quantity));
        Assert.Equal([1], restored.Selection.ToppingIds);
        Assert.Equal(("Large (14\")", 32.00m), (restored.Pizza.Size, restored.Pizza.LineTotal));
    }

    [Fact]
    public async Task LoadAsync_WhenNothingWasSaved_ReturnsNull()
    {
        Assert.Null(await _storage.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_IgnoresACartSavedByTheFirstVersionOfTheApp()
    {
        // The first version saved carts under "Cart", in a shape with crusts and sauces.
        await _storage.SaveAsync([Item()]);
        _browser.Items["Cart"] = _browser.Items[ProtectedSessionCartStorage.StorageKey];
        _browser.Items.Remove(ProtectedSessionCartStorage.StorageKey);

        Assert.Null(await _storage.LoadAsync());
    }

    [Theory]
    [InlineData("this is not a protected value")]
    [InlineData("bm90IHByb3RlY3RlZA==")]
    [InlineData("")]
    public async Task LoadAsync_WhenTheSavedValueIsUnreadable_ReturnsNullInsteadOfThrowing(string stored)
    {
        _browser.Items[ProtectedSessionCartStorage.StorageKey] = stored;

        Assert.Null(await _storage.LoadAsync());
    }

    /// <summary>Stands in for the browser's sessionStorage, which the real class reaches through JS interop.</summary>
    private sealed class FakeBrowserSessionStorage : IJSRuntime
    {
        public Dictionary<string, string> Items { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            string key = (string)args![0]!;
            switch (identifier)
            {
                case "sessionStorage.setItem":
                    Items[key] = (string)args[1]!;
                    return ValueTask.FromResult(default(TValue)!);
                case "sessionStorage.getItem":
                    return ValueTask.FromResult((TValue)(object?)Items.GetValueOrDefault(key)!);
                default:
                    throw new NotSupportedException(identifier);
            }
        }
    }
}
