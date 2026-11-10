using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Components.Pages.PizzaMenu.Dialogs;
using QT.PizzaOrdering.UI.Shared;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

namespace QT.PizzaOrdering.UI.Components.Pages.ShoppingCart;

public partial class CartPage : IDisposable
{
    [Inject] private IPizzaOrderingHttpClient Client { get; set; } = default!;
    [Inject] private CartState Cart { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;

    private MenuCatalog? Menu { get; set; }
    private bool Loading { get; set; } = true;
    private StatusMessage? Status { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Cart.Changed += OnCartChanged;
        await Cart.EnsureLoadedAsync();
        Loading = false;
    }

    private async Task EditAsync(CartItem item)
    {
        Status = null;

        // The menu is only needed to edit a pizza, so it is fetched the first time someone does.
        if (Menu is null)
        {
            ClientResult<MenuCatalog> menu = await Client.GetMenuAsync();
            if (menu.IsFailure)
            {
                Status = StatusMessage.Error($"The menu could not be loaded, so this pizza cannot be changed right now. {CustomerMessages.TryAgainSoon}");
                return;
            }
            Menu = menu.Data!;
        }

        PizzaSelection? edited = await PizzaBuilderDialog.ShowAsync(DialogService, Menu, item.Selection, $"Change {item.Pizza.Name}", "Update Order");
        if (edited is null)
            return;

        Result<PricedPizza> priced = PizzaPricer.Price(Menu, edited);
        if (priced.IsFailure)
        {
            Status = StatusMessage.Error($"That change could not be saved. {priced.Error.Message}");
            return;
        }

        await Cart.ReplaceAsync(item.Id, edited, priced.Value);
    }

    private void OnCartChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Cart.Changed -= OnCartChanged;
    }
}
