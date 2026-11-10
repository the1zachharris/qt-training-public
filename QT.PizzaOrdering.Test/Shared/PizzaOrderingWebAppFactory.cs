using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.API;
using QT.PizzaOrdering.Infrastructure;

namespace QT.PizzaOrdering.Test.Shared;

/// <summary>
/// Hosts the real API against an in-memory SQLite database. The connection is held open for the
/// life of the factory because an in-memory database disappears when its last connection closes.
/// </summary>
public sealed class PizzaOrderingWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.UseEnvironment(DIConfiguration.TestEnvironmentName);
        builder.ConfigureServices(services =>
            services.AddDbContext<PizzaOrderingDbContext>(options => options.UseSqlite(_connection)));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
