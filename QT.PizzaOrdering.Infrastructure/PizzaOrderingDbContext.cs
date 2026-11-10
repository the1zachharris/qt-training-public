using Microsoft.EntityFrameworkCore;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Infrastructure;

public sealed class PizzaOrderingDbContext(DbContextOptions<PizzaOrderingDbContext> options) : DbContext(options)
{
    public DbSet<PizzaSize> Sizes => Set<PizzaSize>();
    public DbSet<Crust> Crusts => Set<Crust>();
    public DbSet<Sauce> Sauces => Set<Sauce>();
    public DbSet<Topping> Toppings => Set<Topping>();
    public DbSet<PresetPizza> PresetPizzas => Set<PresetPizza>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PizzaOrderingDbContext).Assembly);
    }
}
