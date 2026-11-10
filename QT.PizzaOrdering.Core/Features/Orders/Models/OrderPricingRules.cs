namespace QT.PizzaOrdering.Core.Features.Orders.Models;

/// <summary>
/// The order-level charges applied on top of the pizzas.
/// </summary>
/// <param name="TaxRate">Sales tax as a fraction of the subtotal (0.085 = 8.5%).</param>
/// <param name="DeliveryFee">Flat fee added to delivery orders. Not taxed.</param>
public sealed record OrderPricingRules(decimal TaxRate, decimal DeliveryFee);
