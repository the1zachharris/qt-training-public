using Microsoft.EntityFrameworkCore;

using QT.PizzaOrdering.Core.Features.Menu.Interfaces;
using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Infrastructure;

namespace QT.PizzaOrdering.API.Adapters.Data;

public class MenuQueries(PizzaOrderingDbContext dbContext) : IMenuQueries
{
    public async Task<MenuCatalog> GetMenuAsync(CancellationToken ct = default)
    {
        return new MenuCatalog(
            await dbContext.Sizes.AsNoTracking().OrderBy(size => size.Id).ToListAsync(ct),
            await dbContext.Toppings.AsNoTracking().OrderBy(topping => topping.Id).ToListAsync(ct),
            await dbContext.PresetPizzas.AsNoTracking().OrderBy(preset => preset.Id).ToListAsync(ct));
    }
}
