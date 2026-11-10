using Microsoft.AspNetCore.Http.HttpResults;

using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Menu.Models;

namespace QT.PizzaOrdering.API.Features.Menu;

/// <summary>
/// Handles retrieval of the menu.
/// This is the entry point for the GET /menu endpoint.
/// </summary>
public sealed class GetMenuHandler(IMenuQueries menuQueries)
{
    public async Task<Ok<MenuCatalog>> GetMenuAsync(CancellationToken ct = default)
    {
        MenuCatalog menu = await menuQueries.GetMenuAsync(ct);
        return TypedResults.Ok(menu);
    }
}
