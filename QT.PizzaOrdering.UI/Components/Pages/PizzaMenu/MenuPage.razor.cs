using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Components.Pages.PizzaMenu.Dialogs;
using QT.PizzaOrdering.UI.Shared;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

namespace QT.PizzaOrdering.UI.Components.Pages.PizzaMenu;

public partial class MenuPage : IDisposable
{
    [Inject] private IPizzaOrderingHttpClient Client { get; set; } = default!;
    [Inject] private CartState Cart { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MenuCatalog? Menu { get; set; }
    private bool Loading { get; set; } = true;
    private StatusMessage? Status { get; set; }

    /// <summary>The whole menu on one line: every size with its price, then every topping with its price.</summary>
    private string PriceList =>
        Menu is null
            ? string.Empty
            : string.Join(" · ", [
                .. Menu.Sizes.Select(size => $"{size.Name} ({size.Inches}\") {size.Price.ToCurrency()}"),
                .. Menu.Toppings.GroupBy(topping => topping.Price).Select(group => $"{string.Join(", ", group.Select(topping => topping.Name))} +{group.Key.ToCurrency()} each")]);

    protected override async Task OnInitializedAsync()
    {
        Cart.Changed += OnCartChanged;
        await Cart.EnsureLoadedAsync();

        ClientResult<MenuCatalog> result = await Client.GetMenuAsync();
        if (result.IsFailure)
            Status = StatusMessage.Error($"The menu could not be loaded. {result.Error?.Message}");
        else if (!PizzaSelections.CanOrderFrom(result.Data!))
            Status = StatusMessage.Warning("The menu is empty right now. Please check back soon.");
        else
            Menu = result.Data;

        Loading = false;
    }

    private Task BuildYourOwnAsync() =>
        Menu is null ? Task.CompletedTask : CustomizeAsync(PizzaSelections.BuildYourOwn(Menu), PizzaPricer.BuildYourOwnName);

    private async Task CustomizeAsync(PizzaSelection start, string title)
    {
        if (Menu is null)
            return;

        PizzaSelection? built = await PizzaBuilderDialog.ShowAsync(DialogService, Menu, start, title, "Add to Order");
        if (built is not null)
            await AddToCartAsync(built);
    }

    private async Task AddToCartAsync(PizzaSelection selection)
    {
        if (Menu is null)
            return;

        if (Cart.IsFull)
        {
            Snackbar.Add($"Your order already has {OrderPricer.MaxPizzaLines} different pizzas. Remove one to add another.", Severity.Warning);
            return;
        }

        Result<PricedPizza> priced = PizzaPricer.Price(Menu, selection);
        if (priced.IsFailure)
        {
            Status = StatusMessage.Error($"That pizza could not be added. {priced.Error.Message}");
            return;
        }

        await Cart.AddAsync(selection, priced.Value);
        Snackbar.Add($"{priced.Value.Quantity} × {priced.Value.Name} added to your order.", Severity.Success);
    }

    private void OnCartChanged() => InvokeAsync(StateHasChanged);

    public void Dispose()
    {
        Cart.Changed -= OnCartChanged;
    }
}
