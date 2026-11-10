using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using QT.PizzaOrdering.Core.Features.Menu.Models;

namespace QT.PizzaOrdering.Infrastructure.Configurations.Menu;

// Menu ids are part of the seed data and are referenced by preset pizzas, so the database never generates them.

public sealed class PizzaSizeConfiguration : IEntityTypeConfiguration<PizzaSize>
{
    public void Configure(EntityTypeBuilder<PizzaSize> builder)
    {
        builder.ToTable("PizzaSize");
        builder.HasKey(size => size.Id);
        builder.Property(size => size.Id).ValueGeneratedNever();
        builder.Property(size => size.Name).HasMaxLength(50);
    }
}

public sealed class CrustConfiguration : IEntityTypeConfiguration<Crust>
{
    public void Configure(EntityTypeBuilder<Crust> builder)
    {
        builder.ToTable("Crust");
        builder.HasKey(crust => crust.Id);
        builder.Property(crust => crust.Id).ValueGeneratedNever();
        builder.Property(crust => crust.Name).HasMaxLength(50);
    }
}

public sealed class SauceConfiguration : IEntityTypeConfiguration<Sauce>
{
    public void Configure(EntityTypeBuilder<Sauce> builder)
    {
        builder.ToTable("Sauce");
        builder.HasKey(sauce => sauce.Id);
        builder.Property(sauce => sauce.Id).ValueGeneratedNever();
        builder.Property(sauce => sauce.Name).HasMaxLength(50);
    }
}

public sealed class ToppingConfiguration : IEntityTypeConfiguration<Topping>
{
    public void Configure(EntityTypeBuilder<Topping> builder)
    {
        builder.ToTable("Topping");
        builder.HasKey(topping => topping.Id);
        builder.Property(topping => topping.Id).ValueGeneratedNever();
        builder.Property(topping => topping.Name).HasMaxLength(50);
        builder.Property(topping => topping.Category).HasConversion<string>().HasMaxLength(20);
    }
}

public sealed class PresetPizzaConfiguration : IEntityTypeConfiguration<PresetPizza>
{
    public void Configure(EntityTypeBuilder<PresetPizza> builder)
    {
        builder.ToTable("PresetPizza");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Id).ValueGeneratedNever();
        builder.Property(preset => preset.Name).HasMaxLength(50);
        builder.Property(preset => preset.Description).HasMaxLength(250);
        builder.Property(preset => preset.Cheese).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<Crust>().WithMany().HasForeignKey(preset => preset.CrustId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sauce>().WithMany().HasForeignKey(preset => preset.SauceId).OnDelete(DeleteBehavior.Restrict);
    }
}
