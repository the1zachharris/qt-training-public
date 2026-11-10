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

    // Buttons on each line, so keyboard focus can be put back somewhere sensible when a line
    // goes away or a button it was on becomes disabled (WCAG 2.4.3).
    private readonly Dictionary<Guid, MudIconButton> _fewerButtons = [];
    private readonly Dictionary<Guid, MudIconButton> _moreButtons = [];
    private readonly Dictionary<Guid, MudIconButton> _removeButtons = [];
    private MudButton? _seeMenuButton;

    /// <summary>Picks the control to focus once the next render has finished, if any.</summary>
    private Func<MudBaseButton?>? _focusAfterRender;

    private MenuCatalog? Menu { get; set; }
    private bool Loading { get; set; } = true;
    private StatusMessage? Status { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Cart.Changed += OnCartChanged;
        await Cart.EnsureLoadedAsync();
        Loading = false;
    }

    /// <summary>
    /// Names a line for its buttons ("Remove The Works, Large (14")") so each one makes sense out of
    /// context, for example in a screen reader's list of buttons.
    /// </summary>
    private static string LineName(CartItem item) => $"{item.Pizza.Name}, {item.Pizza.Size}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusAfterRender is not { } pick)
            return;

        _focusAfterRender = null;
        if (pick() is { } target)
            await target.FocusAsync();
    }

    private async Task ChangeQuantityAsync(CartItem item, int change)
    {
        Guid id = item.Id;
        int quantity = item.Pizza.Quantity + change;
        await Cart.SetQuantityAsync(id, quantity);

        // At 1 or the maximum the pressed button turns disabled and the browser drops focus, so it
        // moves to the other quantity button on the same line.
        bool reachedLimit = change < 0 ? quantity <= 1 : quantity >= PizzaPricer.MaxQuantity;
        if (reachedLimit)
            FocusAfterRender(() => (change < 0 ? _moreButtons : _fewerButtons).GetValueOrDefault(id));
    }

    /// <summary>
    /// Removes a line and moves focus to the Remove button that takes its place: the next line's,
    /// or the previous line's when it was the last one, or See the Menu when the cart is now empty.
    /// </summary>
    private async Task RemoveAsync(CartItem item)
    {
        int index = Cart.Items.Select(line => line.Id).ToList().IndexOf(item.Id);
        Guid? next = index + 1 < Cart.Items.Count ? Cart.Items[index + 1].Id
            : index > 0 ? Cart.Items[index - 1].Id
            : null;

        await Cart.RemoveAsync(item.Id);
        Forget(item.Id);

        FocusAfterRender(() => next is { } id && !Cart.IsEmpty ? _removeButtons.GetValueOrDefault(id) : _seeMenuButton);
    }

    private async Task StartOverAsync()
    {
        await Cart.ClearAsync();
        foreach (Guid id in _removeButtons.Keys.ToList())
            Forget(id);

        FocusAfterRender(() => _seeMenuButton);
    }

    private void FocusAfterRender(Func<MudBaseButton?> pick)
    {
        _focusAfterRender = pick;
        StateHasChanged();
    }

    private void Forget(Guid id)
    {
        _fewerButtons.Remove(id);
        _moreButtons.Remove(id);
        _removeButtons.Remove(id);
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
