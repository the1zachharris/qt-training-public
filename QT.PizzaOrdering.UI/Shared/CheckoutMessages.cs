using QT.PizzaOrdering.Core.Shared;

namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// What the customer is told when placing an order does not come back as a success.
/// </summary>
public static class CheckoutMessages
{
    /// <summary>
    /// Only a rejection from the API (a 4xx answer) proves nothing was saved. With no answer at
    /// all, or a server error, the order may already be with the kitchen, so the message must not
    /// say it was not placed: that would invite a second, duplicate order.
    /// </summary>
    public static string OrderFailed(ClientError? error)
    {
        bool rejected = error?.Status is { } status && (int)status is >= 400 and < 500;

        return rejected
            ? $"Your order was not placed. {error?.Message}"
            : "We could not confirm whether your order went through. Please call the shop before ordering again, so your order is not made twice.";
    }
}
