using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.Core.Features.Menu.Models;
using QT.PizzaOrdering.Infrastructure.Seeding;

namespace QT.PizzaOrdering.Infrastructure;

/// <summary>
/// DbContext bootstrap for the API host: registration, plus migrating and seeding the database.
/// </summary>
public static class DbContextRegistration
{
    public static void AddPizzaOrderingDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PizzaOrderingDbContext>(options => options.UseSqlite(connectionString));
    }

    /// <summary>
    /// Makes a relative SQLite file path absolute against <paramref name="dataDirectory"/>.
    /// SQLite resolves a relative path against the process's working directory, which differs
    /// between Visual Studio, <c>dotnet run</c>, and the built executable; left alone, each way
    /// of starting the app would open its own, separate set of orders. Absolute paths and
    /// in-memory databases are returned unchanged.
    /// </summary>
    public static string ResolveSqliteConnectionString(string connectionString, string dataDirectory)
    {
        SqliteConnectionStringBuilder builder = new(connectionString);
        bool isFile = !string.IsNullOrEmpty(builder.DataSource)
            && !builder.DataSource.StartsWith(':')
            && !builder.DataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            && builder.Mode != SqliteOpenMode.Memory;

        if (!isFile || Path.IsPathRooted(builder.DataSource))
            return connectionString;

        builder.DataSource = Path.GetFullPath(builder.DataSource, dataDirectory);
        return builder.ConnectionString;
    }

    /// <summary>
    /// Creates the folder a file-based database lives in. SQLite creates the file itself, but not its folder.
    /// </summary>
    public static void EnsureDatabaseFolderExists(string connectionString)
    {
        string dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (Path.IsPathRooted(dataSource) && Path.GetDirectoryName(dataSource) is { Length: > 0 } folder)
            Directory.CreateDirectory(folder);
    }

    /// <summary>
    /// Brings the database up to the current schema and seeds the menu the first time. Migrations
    /// change an existing database in place, so the shop's orders carry over when the app is updated.
    /// </summary>
    public static async Task InitializePizzaOrderingDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        PizzaOrderingDbContext dbContext = scope.ServiceProvider.GetRequiredService<PizzaOrderingDbContext>();

        await dbContext.Database.MigrateAsync(ct);
        if (await dbContext.Sizes.AnyAsync(ct))
            return;

        MenuCatalog menu = MenuSeed.Create();
        dbContext.Sizes.AddRange(menu.Sizes);
        dbContext.Toppings.AddRange(menu.Toppings);
        dbContext.PresetPizzas.AddRange(menu.PresetPizzas);
        await dbContext.SaveChangesAsync(ct);
    }
}
