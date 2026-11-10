using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace QT.PizzaOrdering.UI.Shared.ShoppingCart;

/// <summary>
/// Keeps the cart in the browser's session storage, so it survives a refresh and ends with the tab.
/// </summary>
public sealed partial class ProtectedSessionCartStorage(
    ILogger<ProtectedSessionCartStorage> logger,
    ProtectedSessionStorage sessionStorage) : ICartStorage
{
    private const string Key = "Cart";

    public async Task<IReadOnlyList<CartItem>?> LoadAsync()
    {
        try
        {
            ProtectedBrowserStorageResult<List<CartItem>> result = await sessionStorage.GetAsync<List<CartItem>>(Key);
            return result.Success ? result.Value : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            // Saved by an older build or protected with a key this server no longer has: start with an empty cart.
            LogCartUnreadable(ex);
            return null;
        }
    }

    public async Task SaveAsync(IReadOnlyList<CartItem> items)
    {
        await sessionStorage.SetAsync(Key, items);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The saved cart could not be read and was discarded.")]
    private partial void LogCartUnreadable(Exception exception);
}
