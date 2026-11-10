using System.Text.Json.Serialization;

namespace QT.PizzaOrdering.Core.Features.Menu.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ToppingCategory>))]
public enum ToppingCategory
{
    Meat = 0,
    Veggie = 1
}
