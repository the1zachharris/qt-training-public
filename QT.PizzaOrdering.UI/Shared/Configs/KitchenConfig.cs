using System.ComponentModel.DataAnnotations;

namespace QT.PizzaOrdering.UI.Shared.Configs;

public class KitchenConfig
{
    public const string SectionName = nameof(KitchenConfig);

    /// <summary>
    /// How often the kitchen screen reloads its orders, in seconds. Default = 10.
    /// </summary>
    [Range(2, 300)]
    public int RefreshSeconds { get; set; } = 10;
}
