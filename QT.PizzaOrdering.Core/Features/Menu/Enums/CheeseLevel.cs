using System.Text.Json.Serialization;

namespace QT.PizzaOrdering.Core.Features.Menu.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<CheeseLevel>))]
public enum CheeseLevel
{
    None = 0,
    Light = 1,
    Regular = 2,
    Extra = 3
}
