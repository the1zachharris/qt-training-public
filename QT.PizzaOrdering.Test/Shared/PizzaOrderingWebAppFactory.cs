using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.API;
using QT.PizzaOrdering.Infrastructure;

namespace QT.PizzaOrdering.Test.Shared;

/// <summary>
/// Hosts the real API against SQLite. By default the database is in memory and lives as long as
/// the factory. Set <see cref="DatabasePath"/> to use a file instead, so that a second factory
/// can open the same data (a restart) or requests can run on separate connections (concurrency).
/// </summary>
public sealed class PizzaOrderingWebAppFactory : WebApplicationFactory<Program>
{
    // Held open for the life of the factory: an in-memory database disappears when its last connection closes.
    private readonly SqliteConnection _inMemoryConnection = new("Data Source=:memory:");

    public string? DatabasePath { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(DIConfiguration.TestEnvironmentName);
        builder.ConfigureServices(services =>
        {
            if (DatabasePath is null)
            {
                _inMemoryConnection.Open();
                services.AddDbContext<PizzaOrderingDbContext>(options => options.UseSqlite(_inMemoryConnection));
            }
            else
            {
                services.AddDbContext<PizzaOrderingDbContext>(options => options.UseSqlite($"Data Source={DatabasePath}"));
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _inMemoryConnection.Dispose();
    }
}
