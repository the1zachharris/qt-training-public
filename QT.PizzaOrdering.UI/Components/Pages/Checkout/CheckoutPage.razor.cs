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
    private bool Loading { get; set; } = true;
    private bool Submitting { get; set; }
    private StatusMessage? Status { get; set; }

    /// <summary>The API's message about the name from its last validation response, if it had one.</summary>
    private string? NameError { get; set; }

    /// <summary>
    /// An order needs at least one pizza and a name. The API enforces the same rule; this keeps
    /// the button from sending a request that is certain to be rejected.
    /// </summary>
    private bool CanPlaceOrder => !Submitting && !Cart.IsEmpty && !string.IsNullOrWhiteSpace(CustomerName);

    protected override async Task OnInitializedAsync()
    {
        await Cart.EnsureLoadedAsync();
        Loading = false;
    }

    private async Task PlaceOrderAsync()
    {
        if (!CanPlaceOrder)
            return;

        Submitting = true;
        Status = null;
        NameError = null;

        PlaceOrderRequest request = new(CustomerName ?? string.Empty, [.. Cart.Items.Select(item => item.Selection)]);
        ClientResult<OrderDetails> result = await Client.PlaceOrderAsync(request);

        if (result.IsFailure)
        {
            // Say plainly that nothing was submitted, so the customer knows to try again or call.
            if (result.Error?.Errors?.TryGetValue(nameof(PlaceOrderRequest.CustomerName), out string[]? messages) == true)
                NameError = string.Join(" ", messages);
            Status = StatusMessage.Error($"Your order was not placed. {result.Error?.Message}");
            Submitting = false;
            return;
        }

        await Cart.ClearAsync();
        Navigation.NavigateTo($"/orders/{result.Data!.Id}");
    }
}
