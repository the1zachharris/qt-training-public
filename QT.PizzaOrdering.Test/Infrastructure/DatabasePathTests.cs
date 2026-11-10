using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.API.Adapters.Data;
using QT.PizzaOrdering.Infrastructure;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Infrastructure;

/// <summary>
/// The database must be the same file no matter which folder the app is launched from, or the
/// kitchen would open an empty order list after starting the app a different way.
/// </summary>
public class DatabasePathTests
{
    private static readonly string ContentRoot = Path.Combine(Path.GetTempPath(), "apps", "pizza");

    [Fact]
    public void ResolveSqliteConnectionString_RelativeFile_IsAnchoredToTheContentRoot()
    {
        string resolved = DbContextRegistration.ResolveSqliteConnectionString("Data Source=neighborhood-pizza.db", ContentRoot);

        Assert.Equal($"Data Source={Path.Combine(ContentRoot, "neighborhood-pizza.db")}", resolved);
    }

    [Fact]
    public void ResolveSqliteConnectionString_AbsoluteFile_IsUnchanged()
    {
        string absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "orders.db");

        string resolved = DbContextRegistration.ResolveSqliteConnectionString($"Data Source={absolute}", ContentRoot);

        Assert.Equal($"Data Source={absolute}", resolved);
    }

    [Fact]
    public void ResolveSqliteConnectionString_InMemory_IsUnchanged()
    {
        string resolved = DbContextRegistration.ResolveSqliteConnectionString("Data Source=:memory:", ContentRoot);

        Assert.Equal("Data Source=:memory:", resolved);
    }

    [Fact]
    public async Task ConfiguredDatabase_IsNotThePassOneFile()
    {
        // pizza-ordering.db was created without migrations by the first version of this app and
        // cannot be upgraded, so the app must not point at it.
        await using PizzaOrderingWebAppFactory factory = new();

        string? connectionString = factory.Services.GetRequiredService<IConfiguration>()
            .GetConnectionString(DatabaseConfiguration.ConnectionString);

        Assert.Equal("Data Source=neighborhood-pizza.db", connectionString);
    }
}
