using System.ComponentModel.DataAnnotations;

namespace QT.PizzaOrdering.UI.Shared.Configs;

public class OrderStatusConfig
{
    public const string SectionName = nameof(OrderStatusConfig);

    /// <summary>
    /// How often an open order confirmation page checks whether the order is ready, in seconds. Default = 10.
    /// </summary>
    [Range(2, 300)]
    public int RefreshSeconds { get; set; } = 10;
}
