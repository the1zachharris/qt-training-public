using QT.PizzaOrdering.Core.Features.Menu.Enums;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared;

namespace QT.PizzaOrdering.UI.Components.Pages.PizzaMenu.Dialogs;

/// <summary>
/// Builds or edits one pizza. Starts from <see cref="Selection"/> (a preset, a blank pizza, or a
/// cart line) and returns the finished <see cref="PizzaSelection"/>.
/// </summary>
public partial class PizzaBuilderDialog
{
    private int _sizeId;
    private int _crustId;
    private int _sauceId;
    private CheeseLevel _cheese;
    private int _quantity;
    private IReadOnlyCollection<int> _meatIds = [];
    private IReadOnlyCollection<int> _veggieIds = [];

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired] public MenuCatalog Menu { get; set; } = default!;
    [Parameter, EditorRequired] public PizzaSelection Selection { get; set; } = default!;
    [Parameter] public string ConfirmText { get; set; } = "Add to Cart";

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
        DialogOptions options = new() { MaxWidth = MaxWidth.Medium, FullWidth = true };

        IDialogReference reference = await dialogService.ShowAsync<PizzaBuilderDialog>(title, parameters, options);
        DialogResult? result = await reference.Result;
        return result is { Canceled: false, Data: PizzaSelection built } ? built : null;
    }

    private PizzaSelection Current =>
        new(Selection.PresetPizzaId, _sizeId, _crustId, _sauceId, _cheese, [.. _meatIds, .. _veggieIds], _quantity);

    /// <summary>The pizza as it stands, or null while it breaks a rule (too many toppings, quantity out of range).</summary>
    private PricedPizza? Priced => PizzaPricer.Price(Menu, Current) is { IsSuccess: true } result ? result.Value : null;

    private int ToppingCount => _meatIds.Count + _veggieIds.Count;

    private bool TooManyToppings => ToppingCount > PizzaPricer.MaxToppings;

    private decimal ToppingPrice => Menu.Sizes.FirstOrDefault(size => size.Id == _sizeId)?.ToppingPrice ?? 0m;

    protected override void OnInitialized()
    {
        _sizeId = Selection.SizeId;
        _crustId = Selection.CrustId;
        _sauceId = Selection.SauceId;
        _cheese = Selection.Cheese;
        _quantity = Selection.Quantity;
        _meatIds = [.. ToppingsIn(ToppingCategory.Meat).Select(topping => topping.Id).Where(Selection.ToppingIds.Contains)];
        _veggieIds = [.. ToppingsIn(ToppingCategory.Veggie).Select(topping => topping.Id).Where(Selection.ToppingIds.Contains)];
    }

    private IEnumerable<Topping> ToppingsIn(ToppingCategory category) =>
        Menu.Toppings.Where(topping => topping.Category == category);

    private string SizeLabel(int sizeId) =>
        Menu.Sizes.FirstOrDefault(size => size.Id == sizeId) is { } size
            ? $"{size.Name} ({size.Inches}\") - {size.BasePrice.ToCurrency()}"
            : string.Empty;

    private string CrustLabel(int crustId) =>
        Menu.Crusts.FirstOrDefault(crust => crust.Id == crustId) switch
        {
            null => string.Empty,
            { Upcharge: > 0 } crust => $"{crust.Name} (+{crust.Upcharge.ToCurrency()})",
            { } crust => crust.Name
        };

    private string SauceLabel(int sauceId) =>
        Menu.Sauces.FirstOrDefault(sauce => sauce.Id == sauceId)?.Name ?? string.Empty;

    private string CheeseLabel(CheeseLevel cheese) =>
        cheese == CheeseLevel.Extra
            ? $"{PizzaText.Cheese(cheese)} (+{ToppingPrice.ToCurrency()})"
            : PizzaText.Cheese(cheese);

    private void Cancel() => MudDialog.Close(DialogResult.Cancel());

    private void Confirm()
    {
        if (Priced is not null)
            MudDialog.Close(DialogResult.Ok(Current));
    }
}
