using QT.PizzaOrdering.Core.Features.Orders.Requests;
using QT.PizzaOrdering.Core.Features.Orders.Responses;

namespace QT.PizzaOrdering.UI.Shared.ShoppingCart;

/// <summary>
/// One line in the cart: what the customer chose, and how it was priced for display.
/// Only <see cref="Selection"/> is sent to the API, which prices the order again itself.
/// </summary>
public sealed record CartItem(Guid Id, PizzaSelection Selection, PricedPizza Pizza);
