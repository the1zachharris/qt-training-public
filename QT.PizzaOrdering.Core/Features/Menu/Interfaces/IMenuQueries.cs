using QT.PizzaOrdering.Core.Features.Menu.Models;

namespace QT.PizzaOrdering.Core.Features.Menu.Interfaces;

/// <summary>
/// Query side for the menu (read-only).
/// </summary>
public interface IMenuQueries
{
    Task<MenuCatalog> GetMenuAsync(CancellationToken ct = default);
}
