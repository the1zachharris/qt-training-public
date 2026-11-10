using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Infrastructure.Seeding;

namespace QT.PizzaOrdering.Infrastructure;

/// <summary>
/// DbContext bootstrap for the API host: registration, plus creating and seeding the local database.
/// </summary>
public static class DbContextRegistration
{
    public static void AddPizzaOrderingDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PizzaOrderingDbContext>(options => options.UseSqlite(connectionString));
    }

    /// <summary>
    /// Creates the database if it does not exist and seeds the menu the first time. This app has no
    /// migrations: the schema is created straight from the model, which suits a local SQLite file.
    /// </summary>
    public static async Task InitializePizzaOrderingDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PizzaOrderingDbContext dbContext = scope.ServiceProvider.GetRequiredService<PizzaOrderingDbContext>();

        await dbContext.Database.EnsureCreatedAsync(ct);
        if (await dbContext.Sizes.AnyAsync(ct))
            return;

        MenuCatalog menu = MenuSeed.Create();
        dbContext.Sizes.AddRange(menu.Sizes);
        dbContext.Toppings.AddRange(menu.Toppings);
        dbContext.PresetPizzas.AddRange(menu.PresetPizzas);
        await dbContext.SaveChangesAsync(ct);
    }
}
