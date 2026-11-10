using QT.PizzaOrdering.Core.Features.Orders.Enums;
using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

namespace QT.PizzaOrdering.UI.Components.Pages.Checkout;

public partial class CheckoutPage
{
    [Inject] private IPizzaOrderingHttpClient Client { get; set; } = default!;
    [Inject] private CartState Cart { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string? CustomerName { get; set; }
    private string? Phone { get; set; }
    private FulfillmentType Fulfillment { get; set; } = FulfillmentType.Pickup;
    private string? DeliveryAddress { get; set; }
    private string? Notes { get; set; }

    private bool Loading { get; set; } = true;
    private bool Quoting { get; set; }
    private bool Submitting { get; set; }
    private OrderQuote? Quote { get; set; }
    private StatusMessage? Status { get; set; }

    /// <summary>Field-level messages from the API's last validation response, keyed by request property.</summary>
    private IReadOnlyDictionary<string, string[]> FieldErrors { get; set; } = new Dictionary<string, string[]>();

    /// <summary>
    /// Enough is filled in to try. The API has the final say; this only keeps the button from
    /// sending a request that is certain to be rejected.
    /// </summary>
    private bool CanPlaceOrder =>
        !Submitting
        && Quote is not null
        && !string.IsNullOrWhiteSpace(CustomerName)
        && !string.IsNullOrWhiteSpace(Phone)
        && (Fulfillment == FulfillmentType.Pickup || !string.IsNullOrWhiteSpace(DeliveryAddress));

    private IReadOnlyList<PizzaSelection> Pizzas => [.. Cart.Items.Select(item => item.Selection)];

    protected override async Task OnInitializedAsync()
    {
        await Cart.EnsureLoadedAsync();
        Loading = false;
        if (!Cart.IsEmpty)
            await RefreshQuoteAsync();
    }

    private async Task OnFulfillmentChangedAsync(FulfillmentType fulfillment)
    {
        Fulfillment = fulfillment;
        await RefreshQuoteAsync();
    }

    /// <summary>
    /// Asks the API what this cart costs. The cart's own prices are only for display; tax and the
    /// delivery fee are known only to the API.
    /// </summary>
    private async Task RefreshQuoteAsync()
    {
        Quoting = true;
        Quote = null;
        Status = null;

        ClientResult<OrderQuote> result = await Client.QuoteOrderAsync(new QuoteOrderRequest(Fulfillment, Pizzas));
        if (result.IsSuccess)
            Quote = result.Data;
        else
            Status = StatusMessage.Error($"Your order could not be priced. {result.Error?.Message}");

        Quoting = false;
    }

    private async Task PlaceOrderAsync()
    {
        if (!CanPlaceOrder)
            return;

        Submitting = true;
        Status = null;
        FieldErrors = new Dictionary<string, string[]>();

        PlaceOrderRequest request = new(CustomerName ?? string.Empty, Phone ?? string.Empty, Fulfillment, DeliveryAddress, Notes, Pizzas);
        ClientResult<OrderConfirmation> result = await Client.PlaceOrderAsync(request);

        if (result.IsFailure)
        {
            FieldErrors = result.Error?.Errors ?? FieldErrors;
            Status = StatusMessage.Error($"Your order was not placed. {result.Error?.Message}");
            Submitting = false;
            return;
        }

        await Cart.ClearAsync();
        Navigation.NavigateTo($"/orders/{result.Data!.Id}");
    }

    private bool HasError(string field) => FieldErrors.ContainsKey(field);

    private string? ErrorFor(string field) =>
        FieldErrors.TryGetValue(field, out string[]? messages) ? string.Join(" ", messages) : null;
}
