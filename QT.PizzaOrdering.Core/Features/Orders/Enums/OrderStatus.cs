using System.Text.Json.Serialization;

namespace QT.PizzaOrdering.Core.Features.Orders.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    /// <summary>Submitted and waiting for the kitchen to prepare it.</summary>
    New = 0,

    /// <summary>Prepared and waiting for the customer to pick it up.</summary>
    Ready = 1
}
