using System.ComponentModel.DataAnnotations;

using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.API.Configs;

public class OrderPricingConfig
{
    public const string SectionName = nameof(OrderPricingConfig);

    /// <summary>
    /// Sales tax as a fraction of the subtotal (0.085 = 8.5%).
    /// </summary>
    [Range(typeof(decimal), "0", "1")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Flat fee added to delivery orders.
    /// </summary>
    [Range(typeof(decimal), "0", "100")]
    public decimal DeliveryFee { get; set; }

    public OrderPricingRules ToRules() => new(TaxRate, DeliveryFee);
}
