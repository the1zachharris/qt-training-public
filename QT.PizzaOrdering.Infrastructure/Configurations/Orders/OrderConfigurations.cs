using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using QT.PizzaOrdering.Core.Features.Orders.Models;

namespace QT.PizzaOrdering.Infrastructure.Configurations.Orders;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Order");
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.OrderNumber).HasMaxLength(20);
        builder.Property(order => order.CustomerName).HasMaxLength(100);
        builder.Property(order => order.Phone).HasMaxLength(30);
        builder.Property(order => order.Fulfillment).HasConversion<string>().HasMaxLength(20);
        builder.Property(order => order.DeliveryAddress).HasMaxLength(200);
        builder.Property(order => order.Notes).HasMaxLength(500);
        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey(line => line.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        // Lines is a read-only view over the aggregate's private list.
        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLine");
        builder.HasKey(line => line.Id);
        builder.Property(line => line.Name).HasMaxLength(100);
        builder.Property(line => line.Size).HasMaxLength(50);
        builder.Property(line => line.Crust).HasMaxLength(50);
        builder.Property(line => line.Sauce).HasMaxLength(50);
        builder.Property(line => line.Cheese).HasConversion<string>().HasMaxLength(20);
    }
}
