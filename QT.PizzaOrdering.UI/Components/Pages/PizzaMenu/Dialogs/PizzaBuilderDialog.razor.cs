using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.UI.Components.Pages.PizzaMenu.Dialogs;

/// <summary>
/// Builds or edits one pizza: a size, any toppings, and a quantity. Starts from
/// <see cref="Selection"/> (a preset, a plain pizza, or a line already in the order) and returns
/// the finished <see cref="PizzaSelection"/>. The price shown is recalculated on every change, so
/// the customer always sees what the pizza costs before adding it.
/// </summary>
public partial class PizzaBuilderDialog
{
    private int _sizeId;
    private int _quantity;
    private IReadOnlyCollection<int> _toppingIds = [];

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired] public MenuCatalog Menu { get; set; } = default!;
    [Parameter, EditorRequired] public PizzaSelection Selection { get; set; } = default!;
    [Parameter] public string ConfirmText { get; set; } = "Add to Order";

    /// <summary>
    /// Opens the builder and returns the pizza the customer confirmed, or null if they cancelled.
    /// </summary>
    public static async Task<PizzaSelection?> ShowAsync(
        IDialogService dialogService,
        MenuCatalog menu,
        PizzaSelection selection,
        string title,
        string confirmText)
    {
        DialogParameters<PizzaBuilderDialog> parameters = new()
        {
            { dialog => dialog.Menu, menu },
            { dialog => dialog.Selection, selection },
            { dialog => dialog.ConfirmText, confirmText }
        };
        DialogOptions options = new() { MaxWidth = MaxWidth.Small, FullWidth = true };

        IDialogReference reference = await dialogService.ShowAsync<PizzaBuilderDialog>(title, parameters, options);
        DialogResult? result = await reference.Result;
        return result is { Canceled: false, Data: PizzaSelection built } ? built : null;
    }

    private PizzaSelection Current => new(Selection.PresetPizzaId, _sizeId, [.. _toppingIds], _quantity);

    /// <summary>The pizza as it stands, or null while it breaks a rule (quantity out of range).</summary>
    private PricedPizza? Priced => PizzaPricer.Price(Menu, Current) is { IsSuccess: true } result ? result.Value : null;

    protected override void OnInitialized()
    {
        // A size or topping that has left the menu since this pizza was chosen is dropped here
        // rather than carried into an order the API would reject.
        _sizeId = Menu.Sizes.Any(size => size.Id == Selection.SizeId) ? Selection.SizeId : Menu.Sizes[0].Id;
        _quantity = Selection.Quantity;
        _toppingIds = [.. Menu.Toppings.Select(topping => topping.Id).Where(Selection.ToppingIds.Contains)];
    }

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private void Confirm()
    {
        if (Priced is not null)
            MudDialog.Close(DialogResult.Ok(Current));
    }
}
