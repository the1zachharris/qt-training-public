using System.Text.Json.Serialization;

namespace QT.PizzaOrdering.Core.Features.Orders.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<FulfillmentType>))]
public enum FulfillmentType
{
    Pickup = 0,
    Delivery = 1
}
